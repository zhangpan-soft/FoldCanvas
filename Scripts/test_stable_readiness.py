#!/usr/bin/env python3
from __future__ import annotations

import copy
import hashlib
import json
import pathlib
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from validate_stable_readiness import (  # noqa: E402
    StableReadinessError,
    validate,
)

ROOT = pathlib.Path(__file__).resolve().parents[1]


def require_rejected(action, expected: str) -> None:
    try:
        action()
    except StableReadinessError as exception:
        if str(exception) != expected:
            raise AssertionError(
                f"Unexpected readiness diagnostic: {exception}"
            ) from exception
    else:
        raise AssertionError("Invalid stable readiness evidence was accepted")


def main() -> int:
    contract_path = ROOT / "Documentation~" / "m17-stable-release.json"
    report_path = ROOT / "Documentation~" / "m17-stable-readiness-report.json"
    contract = json.loads(contract_path.read_text(encoding="utf-8"))
    report = json.loads(report_path.read_text(encoding="utf-8"))
    report_sha256 = hashlib.sha256(report_path.read_bytes()).hexdigest()
    qualification = contract["stableQualification"]
    run = {
        "id": qualification["workflowRunId"],
        "path": qualification["workflowPath"],
        "event": "workflow_dispatch",
        "status": "completed",
        "conclusion": "success",
        "head_sha": qualification["workflowHead"],
    }
    artifact = {
        "id": qualification["artifactId"],
        "name": qualification["artifactName"],
        "expired": False,
        "digest": "sha256:" + qualification["artifactSha256"],
        "workflow_run": {
            "id": qualification["workflowRunId"],
            "head_sha": qualification["workflowHead"],
        },
    }

    first = validate(contract, report, report_sha256, run, artifact)
    second = validate(contract, report, report_sha256, run, artifact)
    if first != second or first.get("ready") is not True:
        raise AssertionError("Stable readiness validation is not deterministic")

    changed = copy.deepcopy(report)
    changed["status"] = "blocked"
    require_rejected(
        lambda: validate(contract, changed, report_sha256, run, artifact),
        "Stable readiness report content differs",
    )
    changed = copy.deepcopy(report)
    changed["blockers"] = ["minimum-soak-incomplete"]
    require_rejected(
        lambda: validate(contract, changed, report_sha256, run, artifact),
        "Stable readiness report content differs",
    )
    changed = copy.deepcopy(run)
    changed["id"] += 1
    require_rejected(
        lambda: validate(contract, report, report_sha256, changed, artifact),
        "Stable readiness workflow metadata differs",
    )
    changed = copy.deepcopy(run)
    changed["conclusion"] = "failure"
    require_rejected(
        lambda: validate(contract, report, report_sha256, changed, artifact),
        "Stable readiness workflow metadata differs",
    )
    changed = copy.deepcopy(artifact)
    changed["digest"] = "sha256:" + "0" * 64
    require_rejected(
        lambda: validate(contract, report, report_sha256, run, changed),
        "Stable readiness artifact metadata differs",
    )
    require_rejected(
        lambda: validate(contract, report, "0" * 64, run, artifact),
        "Stable readiness report SHA-256 differs",
    )
    changed = copy.deepcopy(contract)
    changed["stableQualification"]["candidateCommit"] = "0" * 40
    require_rejected(
        lambda: validate(changed, report, report_sha256, run, artifact),
        "Stable readiness report content differs",
    )

    print("M17 stable readiness validation passed (8 deterministic cases).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
