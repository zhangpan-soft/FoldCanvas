#!/usr/bin/env python3
"""Validate the reviewed RC2 gate ledger used by M16 stable evaluation."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import re


ROOT = pathlib.Path(__file__).resolve().parents[1]
SHA40 = re.compile(r"[0-9a-f]{40}")


def load_object(path: pathlib.Path, label: str) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object")
    return value


def valid_utc(value: object) -> bool:
    if not isinstance(value, str) or not value.endswith("Z"):
        return False
    try:
        parsed = dt.datetime.fromisoformat(value[:-1] + "+00:00")
    except ValueError:
        return False
    return parsed.tzinfo == dt.timezone.utc


def validate(ledger: dict, control: dict, contract: dict) -> dict:
    if ledger.get("format") != "foldcanvas-release-gate-ledger":
        raise ValueError("release gate ledger format is invalid")
    if ledger.get("version") != "1":
        raise ValueError("release gate ledger version is unsupported")
    for key in ("candidateVersion", "candidateTag", "candidateCommit"):
        if ledger.get(key) != control.get(key):
            raise ValueError(f"release gate ledger {key} does not match candidate")

    candidate_commit = ledger.get("candidateCommit")
    candidate_tree = ledger.get("candidateTree")
    source_head = ledger.get("sourceHead")
    source_tree = ledger.get("sourceTree")
    if any(
        not isinstance(value, str) or SHA40.fullmatch(value) is None
        for value in (candidate_commit, candidate_tree, source_head, source_tree)
    ):
        raise ValueError("release gate ledger Git identities must be full SHAs")
    if candidate_tree != source_tree:
        raise ValueError("reviewed source and candidate merge trees are not identical")

    audit = ledger.get("audit")
    if (
        not isinstance(audit, dict)
        or audit.get("decision") != "approved"
        or audit.get("candidateCommit") != candidate_commit
        or audit.get("reviewedHead") != source_head
        or not isinstance(audit.get("pullRequest"), int)
        or audit.get("pullRequest") <= 0
        or not valid_utc(audit.get("recordedAt"))
    ):
        raise ValueError("release gate ledger audit is incomplete")

    required_gates = contract.get("requiredGates")
    if not isinstance(required_gates, list) or not required_gates:
        raise ValueError("M15 required gates are invalid")
    runs = ledger.get("runs")
    if not isinstance(runs, list) or not runs:
        raise ValueError("release gate ledger must contain runs")
    seen_run_ids: set[int] = set()
    seen_gates: list[str] = []
    normalized_runs = []
    for run in runs:
        if not isinstance(run, dict):
            raise ValueError("release gate ledger run must be an object")
        run_id = run.get("runId")
        if (
            not isinstance(run_id, int)
            or isinstance(run_id, bool)
            or run_id <= 0
            or run_id in seen_run_ids
        ):
            raise ValueError("release gate ledger run IDs must be unique positive integers")
        seen_run_ids.add(run_id)
        workflow_path = run.get("workflowPath")
        if (
            not isinstance(workflow_path, str)
            or not workflow_path.startswith(".github/workflows/")
            or not workflow_path.endswith(".yml")
            or not (ROOT / workflow_path).is_file()
        ):
            raise ValueError("release gate ledger workflow path is invalid")
        if run.get("event") not in {"pull_request", "workflow_dispatch"}:
            raise ValueError("release gate ledger event is invalid")
        expected_head = run.get("expectedHead")
        if expected_head not in {source_head, candidate_commit}:
            raise ValueError("release gate ledger run head is not reviewed")
        gates = run.get("gates")
        if (
            not isinstance(gates, list)
            or not gates
            or gates != sorted(set(gates))
            or any(gate not in required_gates for gate in gates)
        ):
            raise ValueError("release gate ledger run gates are invalid")
        seen_gates.extend(gates)
        normalized_runs.append(
            {
                "runId": run_id,
                "workflowPath": workflow_path,
                "event": run["event"],
                "expectedHead": expected_head,
                "gates": gates,
            }
        )

    if sorted(seen_gates) != sorted(required_gates) or len(seen_gates) != len(
        set(seen_gates)
    ):
        raise ValueError("release gate ledger does not cover every gate exactly once")
    if normalized_runs != sorted(normalized_runs, key=lambda value: value["runId"]):
        raise ValueError("release gate ledger runs must be ordered by runId")

    return {
        "candidateCommit": candidate_commit,
        "candidateTree": candidate_tree,
        "sourceHead": source_head,
        "sourceTree": source_tree,
        "audit": audit,
        "runIds": [run["runId"] for run in normalized_runs],
        "runs": normalized_runs,
        "satisfiedGates": list(required_gates),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--ledger",
        type=pathlib.Path,
        default=ROOT / ".github" / "foldcanvas-rc2-gates.json",
    )
    parser.add_argument(
        "--control",
        type=pathlib.Path,
        default=ROOT / ".github" / "foldcanvas-active-candidate.json",
    )
    parser.add_argument(
        "--contract",
        type=pathlib.Path,
        default=ROOT / "Documentation~" / "m15-public-distribution.json",
    )
    parser.add_argument("--output", type=pathlib.Path)
    args = parser.parse_args()
    result = validate(
        load_object(args.ledger, "release gate ledger"),
        load_object(args.control, "active candidate"),
        load_object(args.contract, "M15 contract"),
    )
    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(
            json.dumps(result, indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
            newline="\n",
        )
    print(
        "Release gate ledger validation passed: "
        f"{len(result['satisfiedGates'])} gates across {len(result['runIds'])} runs."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"Release gate ledger validation failed: {error}")
        raise SystemExit(1)
