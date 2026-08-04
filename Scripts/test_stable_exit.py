#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from evaluate_stable_exit import evaluate  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTRACT = json.loads(
    (ROOT / "Documentation~" / "m15-public-distribution.json").read_text(
        encoding="utf-8"
    )
)
COMMIT = "a" * 40


def ready_input() -> dict:
    gates = list(CONTRACT["requiredGates"])
    return {
        "format": "foldcanvas-stable-exit-input",
        "version": "1",
        "candidateVersion": CONTRACT["candidateVersion"],
        "candidateTag": CONTRACT["candidateTag"],
        "candidateCommit": COMMIT,
        "releasePublishedAt": "2026-08-01T00:00:00Z",
        "evaluatedAt": "2026-08-08T01:00:00Z",
        "publicRelease": {
            "verified": True,
            "candidateVersion": CONTRACT["candidateVersion"],
            "candidateTag": CONTRACT["candidateTag"],
            "candidateCommit": COMMIT,
            "archiveSha256": "1" * 64,
            "evidenceSha256": "2" * 64,
        },
        "publicConsumers": {
            "candidateVersion": CONTRACT["candidateVersion"],
            "candidateCommit": COMMIT,
            "installations": 2,
            "passed": 2,
            "failed": 0,
            "skipped": 0,
            "inconclusive": 0,
            "evidenceSha256": "3" * 64,
        },
        "sourceUpgrade": {
            "passed": True,
            "baselineVersion": "0.1.0-preview.21",
            "candidateVersion": CONTRACT["candidateVersion"],
            "candidateCommit": COMMIT,
            "derivedInputCount": 0,
            "evidenceSha256": "4" * 64,
        },
        "scheduledLongRuns": [
            {
                "runId": 101,
                "event": "schedule",
                "candidateCommit": COMMIT,
                "conclusion": "success",
                "failed": 0,
                "skipped": 0,
                "inconclusive": 0,
                "completedAt": "2026-08-04T00:00:00Z",
                "evidenceSha256": "5" * 64,
            },
            {
                "runId": 102,
                "event": "schedule",
                "candidateCommit": COMMIT,
                "conclusion": "success",
                "failed": 0,
                "skipped": 0,
                "inconclusive": 0,
                "completedAt": "2026-08-07T00:00:00Z",
                "evidenceSha256": "6" * 64,
            },
        ],
        "releaseBlockingIssues": [],
        "exactHeadAudit": {
            "decision": "approved",
            "candidateCommit": COMMIT,
            "recordedAt": "2026-08-08T00:00:00Z",
        },
        "satisfiedGates": gates,
    }


def assert_blocked(value: dict, blocker: str) -> None:
    first = evaluate(value, CONTRACT)
    second = evaluate(copy.deepcopy(value), CONTRACT)
    if first != second:
        raise AssertionError("stable-exit evaluation is not deterministic")
    if first["status"] != "blocked" or blocker not in first["blockers"]:
        raise AssertionError(
            f"stable-exit case did not block on {blocker}: {first}"
        )


def main() -> int:
    ready = ready_input()
    report = evaluate(ready, CONTRACT)
    if report["status"] != "ready" or report["blockers"]:
        raise AssertionError("complete synthetic stable evidence was not ready")
    if report["soakHours"] != 169.0:
        raise AssertionError("stable-exit soak duration is incorrect")

    no_release = copy.deepcopy(ready)
    no_release["releasePublishedAt"] = None
    assert_blocked(no_release, "candidate-not-published")

    short_soak = copy.deepcopy(ready)
    short_soak["evaluatedAt"] = "2026-08-07T23:00:00Z"
    assert_blocked(short_soak, "minimum-soak-incomplete")

    one_run = copy.deepcopy(ready)
    one_run["scheduledLongRuns"] = one_run["scheduledLongRuns"][:1]
    assert_blocked(one_run, "scheduled-long-runs-incomplete")

    duplicate_run = copy.deepcopy(ready)
    duplicate_run["scheduledLongRuns"][1]["runId"] = 101
    assert_blocked(duplicate_run, "scheduled-long-runs-incomplete")

    wrong_commit_run = copy.deepcopy(ready)
    wrong_commit_run["scheduledLongRuns"][1]["candidateCommit"] = "b" * 40
    assert_blocked(wrong_commit_run, "scheduled-long-runs-incomplete")

    failed_run = copy.deepcopy(ready)
    failed_run["scheduledLongRuns"][1]["conclusion"] = "failure"
    failed_run["scheduledLongRuns"][1]["failed"] = 1
    assert_blocked(failed_run, "scheduled-long-runs-incomplete")

    pre_release_run = copy.deepcopy(ready)
    pre_release_run["scheduledLongRuns"][0]["completedAt"] = (
        "2026-07-31T23:00:00Z"
    )
    assert_blocked(pre_release_run, "scheduled-long-runs-incomplete")

    skipped_run = copy.deepcopy(ready)
    skipped_run["scheduledLongRuns"][1]["skipped"] = 1
    assert_blocked(skipped_run, "scheduled-long-runs-incomplete")

    open_issue = copy.deepcopy(ready)
    open_issue["releaseBlockingIssues"] = [
        {"number": 14, "state": "open"}
    ]
    assert_blocked(open_issue, "open-release-blocker")

    stale_audit = copy.deepcopy(ready)
    stale_audit["exactHeadAudit"]["candidateCommit"] = "b" * 40
    assert_blocked(stale_audit, "exact-head-audit-missing")

    consumer_skip = copy.deepcopy(ready)
    consumer_skip["publicConsumers"]["skipped"] = 1
    assert_blocked(consumer_skip, "public-consumer-evidence-missing")

    derived_upgrade = copy.deepcopy(ready)
    derived_upgrade["sourceUpgrade"]["derivedInputCount"] = 1
    assert_blocked(derived_upgrade, "source-upgrade-evidence-missing")

    missing_gate = copy.deepcopy(ready)
    missing_gate["satisfiedGates"] = missing_gate["satisfiedGates"][:-1]
    assert_blocked(missing_gate, "required-gates-incomplete")

    print("M15 stable-release exit evaluator validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
