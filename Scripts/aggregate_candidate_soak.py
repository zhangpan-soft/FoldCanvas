#!/usr/bin/env python3
"""Validate and aggregate M16 candidate-soak records for stable evaluation."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import pathlib
import re


ROOT = pathlib.Path(__file__).resolve().parents[1]
SHA256 = re.compile(r"[0-9a-f]{64}")


def canonical_bytes(value: object) -> bytes:
    return (json.dumps(value, sort_keys=True, separators=(",", ":")) + "\n").encode(
        "utf-8"
    )


def canonical_sha256(value: object) -> str:
    return hashlib.sha256(canonical_bytes(value)).hexdigest()


def load_object(path: pathlib.Path, label: str) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object")
    return value


def parse_utc(value: object, label: str) -> dt.datetime:
    if not isinstance(value, str) or not value.endswith("Z"):
        raise ValueError(f"{label} must be a UTC timestamp ending in Z")
    try:
        parsed = dt.datetime.fromisoformat(value[:-1] + "+00:00")
    except ValueError as error:
        raise ValueError(f"{label} is not a valid timestamp") from error
    if parsed.tzinfo != dt.timezone.utc:
        raise ValueError(f"{label} must use UTC")
    return parsed


def require_nonnegative_integer(value: object, label: str) -> int:
    if not isinstance(value, int) or isinstance(value, bool) or value < 0:
        raise ValueError(f"{label} must be a non-negative integer")
    return value


def validate_run(run: dict, control: dict) -> tuple[dict, bool]:
    if run.get("format") != "foldcanvas-candidate-soak-run":
        raise ValueError("soak run format is invalid")
    if run.get("version") != "1":
        raise ValueError("soak run version is unsupported")
    for run_key, control_key in (
        ("candidateVersion", "candidateVersion"),
        ("candidateTag", "candidateTag"),
        ("candidateCommit", "candidateCommit"),
        ("archiveSha256", "archiveSha256"),
    ):
        if run.get(run_key) != control.get(control_key):
            raise ValueError(f"soak run {run_key} does not match active candidate")

    run_id = run.get("runId")
    run_attempt = run.get("runAttempt")
    if not isinstance(run_id, int) or isinstance(run_id, bool) or run_id <= 0:
        raise ValueError("soak run runId must be a positive integer")
    if (
        not isinstance(run_attempt, int)
        or isinstance(run_attempt, bool)
        or run_attempt <= 0
    ):
        raise ValueError("soak run runAttempt must be a positive integer")

    event = run.get("event")
    if event not in {"schedule", "workflow_dispatch"}:
        raise ValueError("soak run event must be schedule or workflow_dispatch")
    qualifying = event == "schedule"
    if run.get("qualifiesForStableExit") is not qualifying:
        raise ValueError("soak run qualification flag does not match its event")
    if run.get("conclusion") != "success":
        raise ValueError("soak run conclusion is not success")

    failed = require_nonnegative_integer(run.get("failed"), "soak run failed")
    skipped = require_nonnegative_integer(run.get("skipped"), "soak run skipped")
    inconclusive = require_nonnegative_integer(
        run.get("inconclusive"), "soak run inconclusive"
    )
    if failed != 0 or skipped != 0 or inconclusive != 0:
        raise ValueError("soak run Unity counts are not fully green")

    completed_at_text = run.get("completedAt")
    completed_at = parse_utc(completed_at_text, "soak run completedAt")
    published_at = parse_utc(control.get("publishedAt"), "candidate publishedAt")
    if completed_at < published_at:
        raise ValueError("soak run completed before candidate publication")

    evidence_sha256 = run.get("evidenceSha256")
    if (
        not isinstance(evidence_sha256, str)
        or SHA256.fullmatch(evidence_sha256) is None
    ):
        raise ValueError("soak run evidenceSha256 must be lowercase SHA-256")

    validation = run.get("validation")
    if not isinstance(validation, dict):
        raise ValueError("soak run validation must be an object")
    test_total = require_nonnegative_integer(
        validation.get("testTotal"), "soak validation testTotal"
    )
    if (
        validation.get("format") != "foldcanvas-m13-long-run-validation"
        or validation.get("version") != "1"
        or validation.get("complete") is not True
        or validation.get("unityVersion") != control.get("unityVersion")
        or test_total < 1
        or validation.get("testPassed") != test_total
        or validation.get("testFailed") != failed
        or validation.get("testSkipped") != skipped
        or validation.get("testInconclusive") != inconclusive
        or validation.get("casesPerSuite")
        != control.get("longRun", {}).get("casesPerSuite")
        or validation.get("seedHex") != control.get("longRun", {}).get("seedHex")
        or validation.get("suiteCount") != 4
        or validation.get("caseCount")
        != 4 * control.get("longRun", {}).get("casesPerSuite", -1)
        or validation.get("unexpectedCount") != 0
        or validation.get("resourceScenarioCount") != 5
        or validation.get("resourcePassedScenarioCount") != 5
    ):
        raise ValueError("soak run validation is incomplete or does not match control")

    stable_value = {
        "runId": run_id,
        "event": event,
        "candidateCommit": run["candidateCommit"],
        "conclusion": run["conclusion"],
        "failed": failed,
        "skipped": skipped,
        "inconclusive": inconclusive,
        "completedAt": completed_at_text,
        "evidenceSha256": evidence_sha256,
    }
    return stable_value, qualifying


def aggregate(control: dict, runs: list[dict]) -> dict:
    if control.get("format") != "foldcanvas-active-release-candidate":
        raise ValueError("active candidate control is invalid")
    seen_run_ids: set[int] = set()
    qualifying_runs: list[dict] = []
    manual_run_ids: list[int] = []
    for run in runs:
        stable_value, qualifying = validate_run(run, control)
        run_id = stable_value["runId"]
        if run_id in seen_run_ids:
            raise ValueError(f"duplicate soak runId: {run_id}")
        seen_run_ids.add(run_id)
        if qualifying:
            qualifying_runs.append(stable_value)
        else:
            manual_run_ids.append(run_id)

    qualifying_runs.sort(key=lambda value: (value["completedAt"], value["runId"]))
    manual_run_ids.sort()
    evidence_identity = {
        "candidateVersion": control.get("candidateVersion"),
        "candidateTag": control.get("candidateTag"),
        "candidateCommit": control.get("candidateCommit"),
        "archiveSha256": control.get("archiveSha256"),
        "qualifyingRuns": qualifying_runs,
        "manualRunIds": manual_run_ids,
    }
    return {
        "format": "foldcanvas-candidate-soak-aggregate",
        "version": "1",
        "candidateVersion": control.get("candidateVersion"),
        "candidateTag": control.get("candidateTag"),
        "candidateCommit": control.get("candidateCommit"),
        "archiveSha256": control.get("archiveSha256"),
        "inputRunCount": len(runs),
        "qualifyingRunCount": len(qualifying_runs),
        "manualRunCount": len(manual_run_ids),
        "manualRunIds": manual_run_ids,
        "scheduledLongRuns": qualifying_runs,
        "evidenceSha256": canonical_sha256(evidence_identity),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--control",
        type=pathlib.Path,
        default=ROOT / ".github" / "foldcanvas-active-candidate.json",
    )
    parser.add_argument("--run", action="append", type=pathlib.Path, default=[])
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    control = load_object(args.control, "active candidate")
    runs = [load_object(path, f"soak run {path}") for path in args.run]
    result = aggregate(control, runs)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(
        "Candidate soak aggregation passed: "
        f"{result['qualifyingRunCount']} scheduled, "
        f"{result['manualRunCount']} manual."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"Candidate soak aggregation failed: {error}")
        raise SystemExit(1)
