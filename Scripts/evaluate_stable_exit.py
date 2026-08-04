#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import pathlib
import re
import sys

sys.dont_write_bytecode = True

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEFAULT_CONTRACT = ROOT / "Documentation~" / "m15-public-distribution.json"
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
COMMIT_PATTERN = re.compile(r"^[0-9a-f]{40}$")

BLOCKER_ORDER = (
    "candidate-not-published",
    "public-release-assets-unverified",
    "public-consumer-evidence-missing",
    "source-upgrade-evidence-missing",
    "minimum-soak-incomplete",
    "scheduled-long-runs-incomplete",
    "open-release-blocker",
    "exact-head-audit-missing",
    "required-gates-incomplete",
)


def load_object(path: pathlib.Path, label: str) -> dict:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"{label} is missing or empty: {path}")
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object: {path}")
    return value


def parse_time(value: object, label: str) -> dt.datetime:
    if not isinstance(value, str) or not value:
        raise ValueError(f"{label} must be an ISO-8601 timestamp")
    normalized = value[:-1] + "+00:00" if value.endswith("Z") else value
    try:
        parsed = dt.datetime.fromisoformat(normalized)
    except ValueError as exception:
        raise ValueError(f"{label} must be an ISO-8601 timestamp") from exception
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        raise ValueError(f"{label} must include a timezone")
    return parsed.astimezone(dt.timezone.utc)


def valid_sha256(value: object) -> bool:
    return isinstance(value, str) and SHA256_PATTERN.fullmatch(value) is not None


def valid_commit(value: object) -> bool:
    return isinstance(value, str) and COMMIT_PATTERN.fullmatch(value) is not None


def canonical_sha256(value: dict) -> str:
    payload = json.dumps(
        value,
        ensure_ascii=True,
        separators=(",", ":"),
        sort_keys=True,
    ).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def evaluate(input_value: dict, contract: dict) -> dict:
    if (
        input_value.get("format") != "foldcanvas-stable-exit-input"
        or input_value.get("version") != "1"
    ):
        raise ValueError("stable-exit input identity is invalid")
    candidate_version = input_value.get("candidateVersion")
    candidate_tag = input_value.get("candidateTag")
    candidate_commit = input_value.get("candidateCommit")
    if candidate_version != contract.get("candidateVersion"):
        raise ValueError("stable-exit candidate version does not match contract")
    if candidate_tag != contract.get("candidateTag"):
        raise ValueError("stable-exit candidate tag does not match contract")
    if not valid_commit(candidate_commit):
        raise ValueError("stable-exit candidate commit is invalid")

    stable_contract = contract.get("stableExit")
    if not isinstance(stable_contract, dict):
        raise ValueError("stable-exit policy is missing")
    minimum_soak = stable_contract.get("minimumSoakHours")
    minimum_runs = stable_contract.get("minimumScheduledLongRuns")
    if not isinstance(minimum_soak, int) or minimum_soak < 168:
        raise ValueError("stable-exit minimum soak policy is invalid")
    if not isinstance(minimum_runs, int) or minimum_runs < 2:
        raise ValueError("stable-exit scheduled-run policy is invalid")

    evaluated_at = parse_time(input_value.get("evaluatedAt"), "evaluatedAt")
    blockers: set[str] = set()
    published_value = input_value.get("releasePublishedAt")
    published_at: dt.datetime | None = None
    soak_hours = 0.0
    if published_value is None:
        blockers.add("candidate-not-published")
        blockers.add("minimum-soak-incomplete")
    else:
        published_at = parse_time(published_value, "releasePublishedAt")
        if published_at > evaluated_at:
            raise ValueError("releasePublishedAt cannot be after evaluatedAt")
        soak_hours = (evaluated_at - published_at).total_seconds() / 3600.0
        if soak_hours < minimum_soak:
            blockers.add("minimum-soak-incomplete")

    public_release = input_value.get("publicRelease")
    if not isinstance(public_release, dict) or not (
        public_release.get("verified") is True
        and public_release.get("candidateVersion") == candidate_version
        and public_release.get("candidateTag") == candidate_tag
        and public_release.get("candidateCommit") == candidate_commit
        and valid_sha256(public_release.get("archiveSha256"))
        and valid_sha256(public_release.get("evidenceSha256"))
    ):
        blockers.add("public-release-assets-unverified")

    consumers = input_value.get("publicConsumers")
    if not isinstance(consumers, dict) or not (
        consumers.get("candidateVersion") == candidate_version
        and consumers.get("candidateCommit") == candidate_commit
        and consumers.get("installations") == 2
        and consumers.get("passed") == 2
        and consumers.get("failed") == 0
        and consumers.get("skipped") == 0
        and consumers.get("inconclusive") == 0
        and valid_sha256(consumers.get("evidenceSha256"))
    ):
        blockers.add("public-consumer-evidence-missing")

    upgrade = input_value.get("sourceUpgrade")
    approved_baselines = contract.get("upgrade", {}).get(
        "fromPackageVersions", []
    )
    if not isinstance(upgrade, dict) or not (
        upgrade.get("passed") is True
        and upgrade.get("baselineVersion") in approved_baselines
        and upgrade.get("candidateVersion") == candidate_version
        and upgrade.get("candidateCommit") == candidate_commit
        and upgrade.get("derivedInputCount") == 0
        and valid_sha256(upgrade.get("evidenceSha256"))
    ):
        blockers.add("source-upgrade-evidence-missing")

    runs = input_value.get("scheduledLongRuns")
    valid_runs = True
    qualifying_run_ids: list[int] = []
    seen_run_ids: set[int] = set()
    if not isinstance(runs, list):
        valid_runs = False
        runs = []
    for run in runs:
        if not isinstance(run, dict):
            valid_runs = False
            continue
        run_id = run.get("runId")
        if not isinstance(run_id, int) or run_id <= 0 or run_id in seen_run_ids:
            valid_runs = False
            continue
        seen_run_ids.add(run_id)
        completed_at = run.get("completedAt")
        try:
            completed = parse_time(completed_at, "scheduled run completedAt")
        except ValueError:
            valid_runs = False
            continue
        if (
            run.get("event") != "schedule"
            or run.get("candidateCommit") != candidate_commit
            or run.get("conclusion") != "success"
            or run.get("failed") != 0
            or run.get("skipped") != 0
            or run.get("inconclusive") != 0
            or (published_at is not None and completed < published_at)
            or completed > evaluated_at
            or not valid_sha256(run.get("evidenceSha256"))
        ):
            valid_runs = False
            continue
        qualifying_run_ids.append(run_id)
    if not valid_runs or len(qualifying_run_ids) < minimum_runs:
        blockers.add("scheduled-long-runs-incomplete")

    issues = input_value.get("releaseBlockingIssues")
    if not isinstance(issues, list):
        raise ValueError("releaseBlockingIssues must be an array")
    open_issue_numbers: list[int] = []
    for issue in issues:
        if not isinstance(issue, dict):
            raise ValueError("release blocker entry must be an object")
        number = issue.get("number")
        state = issue.get("state")
        if not isinstance(number, int) or number <= 0 or state not in (
            "open",
            "closed",
        ):
            raise ValueError("release blocker entry is invalid")
        if state == "open":
            open_issue_numbers.append(number)
    if open_issue_numbers:
        blockers.add("open-release-blocker")

    audit = input_value.get("exactHeadAudit")
    audit_valid = isinstance(audit, dict) and (
        audit.get("decision") == "approved"
        and audit.get("candidateCommit") == candidate_commit
    )
    if audit_valid:
        try:
            recorded_at = parse_time(audit.get("recordedAt"), "audit recordedAt")
            if recorded_at > evaluated_at:
                audit_valid = False
        except ValueError:
            audit_valid = False
    if not audit_valid:
        blockers.add("exact-head-audit-missing")

    required_gates = contract.get("requiredGates")
    satisfied_gates = input_value.get("satisfiedGates")
    if not isinstance(required_gates, list) or not isinstance(satisfied_gates, list):
        raise ValueError("stable-exit gate lists are invalid")
    if any(
        not isinstance(gate, str) or not gate
        for gate in required_gates + satisfied_gates
    ):
        raise ValueError("stable-exit gate names are invalid")
    if (
        len(satisfied_gates) != len(set(satisfied_gates))
        or any(gate not in required_gates for gate in satisfied_gates)
        or set(satisfied_gates) != set(required_gates)
    ):
        blockers.add("required-gates-incomplete")

    ordered_blockers = [
        blocker for blocker in BLOCKER_ORDER if blocker in blockers
    ]
    status = "ready" if not ordered_blockers else "blocked"
    return {
        "format": "foldcanvas-stable-exit-report",
        "version": "1",
        "targetVersion": stable_contract.get("targetVersion"),
        "candidateVersion": candidate_version,
        "candidateTag": candidate_tag,
        "candidateCommit": candidate_commit,
        "evaluatedAt": input_value["evaluatedAt"],
        "status": status,
        "blockers": ordered_blockers,
        "soakHours": round(soak_hours, 6),
        "minimumSoakHours": minimum_soak,
        "qualifyingScheduledLongRuns": len(qualifying_run_ids),
        "minimumScheduledLongRuns": minimum_runs,
        "openReleaseBlockerCount": len(open_issue_numbers),
        "satisfiedGateCount": len(set(satisfied_gates).intersection(required_gates)),
        "requiredGateCount": len(required_gates),
        "inputSha256": canonical_sha256(input_value),
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Evaluate the fail-closed FoldCanvas stable-release gate."
    )
    parser.add_argument("--input", required=True, type=pathlib.Path)
    parser.add_argument(
        "--contract",
        type=pathlib.Path,
        default=DEFAULT_CONTRACT,
    )
    parser.add_argument("--output", type=pathlib.Path)
    parser.add_argument(
        "--require-ready",
        action="store_true",
        help="Return a failure exit code when the evaluated gate is blocked.",
    )
    args = parser.parse_args()
    input_value = load_object(args.input, "stable-exit input")
    contract = load_object(args.contract, "M15 distribution contract")
    report = evaluate(input_value, contract)
    payload = json.dumps(report, indent=2, sort_keys=True) + "\n"
    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(payload, encoding="utf-8", newline="\n")
    else:
        print(payload, end="")
    if args.require_ready and report["status"] != "ready":
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
