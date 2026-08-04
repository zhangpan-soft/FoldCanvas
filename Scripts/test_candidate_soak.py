#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import sys


sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from aggregate_candidate_soak import aggregate  # noqa: E402


ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTROL = json.loads(
    (ROOT / ".github" / "foldcanvas-active-candidate.json").read_text(
        encoding="utf-8"
    )
)


def run_record(run_id: int, event: str, completed_at: str) -> dict:
    return {
        "format": "foldcanvas-candidate-soak-run",
        "version": "1",
        "candidateVersion": CONTROL["candidateVersion"],
        "candidateTag": CONTROL["candidateTag"],
        "candidateCommit": CONTROL["candidateCommit"],
        "archiveSha256": CONTROL["archiveSha256"],
        "runId": run_id,
        "runAttempt": 1,
        "event": event,
        "qualifiesForStableExit": event == "schedule",
        "conclusion": "success",
        "failed": 0,
        "skipped": 0,
        "inconclusive": 0,
        "completedAt": completed_at,
        "evidenceSha256": format(run_id, "064x"),
        "validation": {
            "format": "foldcanvas-m13-long-run-validation",
            "version": "1",
            "unityVersion": CONTROL["unityVersion"],
            "testTotal": 472,
            "testPassed": 472,
            "testFailed": 0,
            "testSkipped": 0,
            "testInconclusive": 0,
            "casesPerSuite": CONTROL["longRun"]["casesPerSuite"],
            "suiteCount": 4,
            "caseCount": 512,
            "unexpectedCount": 0,
            "seedHex": CONTROL["longRun"]["seedHex"],
            "resourceScenarioCount": 5,
            "resourcePassedScenarioCount": 5,
            "complete": True,
        },
    }


def assert_rejected(runs: list[dict], expected: str) -> None:
    messages = []
    for value in (runs, copy.deepcopy(runs)):
        try:
            aggregate(CONTROL, value)
        except ValueError as error:
            messages.append(str(error))
    if messages != [expected, expected]:
        raise AssertionError(f"case did not fail deterministically: {messages}")


def main() -> int:
    manual = run_record(100, "workflow_dispatch", "2026-08-04T12:00:00Z")
    scheduled_b = run_record(102, "schedule", "2026-08-10T03:30:00Z")
    scheduled_a = run_record(101, "schedule", "2026-08-06T03:30:00Z")
    first = aggregate(CONTROL, [scheduled_b, manual, scheduled_a])
    second = aggregate(CONTROL, [scheduled_b, manual, scheduled_a])
    if first != second:
        raise AssertionError("candidate soak aggregation is not deterministic")
    if first["qualifyingRunCount"] != 2 or first["manualRunIds"] != [100]:
        raise AssertionError("manual and scheduled runs were not separated")
    if [run["runId"] for run in first["scheduledLongRuns"]] != [101, 102]:
        raise AssertionError("scheduled runs were not ordered deterministically")

    assert_rejected(
        [scheduled_a, copy.deepcopy(scheduled_a)], "duplicate soak runId: 101"
    )
    wrong_commit = copy.deepcopy(scheduled_a)
    wrong_commit["candidateCommit"] = "b" * 40
    assert_rejected(
        [wrong_commit],
        "soak run candidateCommit does not match active candidate",
    )
    wrong_archive = copy.deepcopy(scheduled_a)
    wrong_archive["archiveSha256"] = "c" * 64
    assert_rejected(
        [wrong_archive],
        "soak run archiveSha256 does not match active candidate",
    )
    fake_schedule = copy.deepcopy(manual)
    fake_schedule["qualifiesForStableExit"] = True
    assert_rejected(
        [fake_schedule],
        "soak run qualification flag does not match its event",
    )
    skipped = copy.deepcopy(scheduled_a)
    skipped["skipped"] = 1
    skipped["validation"]["testSkipped"] = 1
    assert_rejected([skipped], "soak run Unity counts are not fully green")
    early = copy.deepcopy(scheduled_a)
    early["completedAt"] = "2026-08-04T09:00:00Z"
    assert_rejected([early], "soak run completed before candidate publication")
    incomplete = copy.deepcopy(scheduled_a)
    incomplete["validation"]["caseCount"] = 511
    assert_rejected(
        [incomplete],
        "soak run validation is incomplete or does not match control",
    )

    print("M16 candidate soak aggregation validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
