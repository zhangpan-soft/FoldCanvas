#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEFAULT_CONTRACT = ROOT / "Documentation~" / "m17-stable-release.json"
DEFAULT_REPORT = ROOT / "Documentation~" / "m17-stable-readiness-report.json"
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
COMMIT_PATTERN = re.compile(r"^[0-9a-f]{40}$")


class StableReadinessError(ValueError):
    pass


def load_object(path: pathlib.Path, label: str) -> dict:
    if path.is_symlink() or not path.is_file() or path.stat().st_size <= 0:
        raise StableReadinessError(f"{label} must be one non-empty regular file")
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exception:  # noqa: BLE001
        raise StableReadinessError(f"{label} must be valid UTF-8 JSON") from exception
    if not isinstance(value, dict):
        raise StableReadinessError(f"{label} must be a JSON object")
    return value


def sha256_file(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def canonical_json(value: dict) -> str:
    return json.dumps(
        value,
        ensure_ascii=False,
        indent=2,
        sort_keys=True,
        separators=(",", ": "),
    ) + "\n"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise StableReadinessError(message)


def validate(
    contract: dict,
    report: dict,
    report_sha256: str,
    run_metadata: dict,
    artifact_metadata: dict,
) -> dict:
    require(
        contract.get("format") == "foldcanvas-stable-release"
        and contract.get("version") == "1"
        and contract.get("packageVersion") == "1.0.0"
        and contract.get("tag") == "v1.0.0"
        and contract.get("stableRelease") is True,
        "Stable release contract identity is invalid",
    )
    qualification = contract.get("stableQualification")
    require(
        isinstance(qualification, dict),
        "Stable qualification contract is missing",
    )
    require(
        isinstance(report_sha256, str)
        and SHA256_PATTERN.fullmatch(report_sha256) is not None
        and report_sha256 == qualification.get("reportSha256"),
        "Stable readiness report SHA-256 differs",
    )

    expected_report = {
        "blockers": [],
        "candidateCommit": qualification.get("candidateCommit"),
        "candidateTag": qualification.get("candidateTag"),
        "candidateVersion": qualification.get("candidateVersion"),
        "evaluatedAt": qualification.get("evaluatedAt"),
        "format": "foldcanvas-stable-exit-report",
        "inputSha256": qualification.get("inputSha256"),
        "minimumScheduledLongRuns": qualification.get(
            "minimumScheduledLongRuns"
        ),
        "minimumSoakHours": qualification.get("minimumSoakHours"),
        "openReleaseBlockerCount": qualification.get(
            "openReleaseBlockerCount"
        ),
        "qualifyingScheduledLongRuns": qualification.get(
            "qualifyingScheduledLongRuns"
        ),
        "requiredGateCount": qualification.get("requiredGateCount"),
        "satisfiedGateCount": qualification.get("satisfiedGateCount"),
        "soakHours": qualification.get("soakHours"),
        "status": qualification.get("status"),
        "targetVersion": qualification.get("targetVersion"),
        "version": "1",
    }
    require(report == expected_report, "Stable readiness report content differs")
    require(
        report["status"] == "ready"
        and report["soakHours"] >= report["minimumSoakHours"]
        and report["qualifyingScheduledLongRuns"]
        >= report["minimumScheduledLongRuns"]
        and report["satisfiedGateCount"] == report["requiredGateCount"]
        and report["openReleaseBlockerCount"] == 0
        and not report["blockers"],
        "Stable readiness report is not ready",
    )

    run_id = qualification.get("workflowRunId")
    workflow_head = qualification.get("workflowHead")
    require(
        isinstance(run_id, int)
        and run_id > 0
        and isinstance(workflow_head, str)
        and COMMIT_PATTERN.fullmatch(workflow_head) is not None,
        "Stable workflow identity is invalid",
    )
    require(
        run_metadata.get("id") == run_id
        and run_metadata.get("path") == qualification.get("workflowPath")
        and run_metadata.get("event") == "workflow_dispatch"
        and run_metadata.get("status") == "completed"
        and run_metadata.get("conclusion") == "success"
        and run_metadata.get("head_sha") == workflow_head,
        "Stable readiness workflow metadata differs",
    )

    artifact_id = qualification.get("artifactId")
    artifact_digest = qualification.get("artifactSha256")
    require(
        artifact_metadata.get("id") == artifact_id
        and artifact_metadata.get("name") == qualification.get("artifactName")
        and artifact_metadata.get("expired") is False
        and artifact_metadata.get("digest") == "sha256:" + artifact_digest,
        "Stable readiness artifact metadata differs",
    )
    artifact_run = artifact_metadata.get("workflow_run")
    require(
        isinstance(artifact_run, dict)
        and artifact_run.get("id") == run_id
        and artifact_run.get("head_sha") == workflow_head,
        "Stable readiness artifact is not bound to the reviewed workflow run",
    )

    return {
        "format": "foldcanvas-stable-readiness-verification",
        "version": "1",
        "workflowRunId": run_id,
        "workflowHead": workflow_head,
        "artifactId": artifact_id,
        "artifactSha256": artifact_digest,
        "reportSha256": report_sha256,
        "candidateCommit": report["candidateCommit"],
        "candidateTag": report["candidateTag"],
        "candidateVersion": report["candidateVersion"],
        "targetVersion": report["targetVersion"],
        "evaluatedAt": report["evaluatedAt"],
        "soakHours": report["soakHours"],
        "qualifyingScheduledLongRuns": report[
            "qualifyingScheduledLongRuns"
        ],
        "satisfiedGateCount": report["satisfiedGateCount"],
        "requiredGateCount": report["requiredGateCount"],
        "openReleaseBlockerCount": report["openReleaseBlockerCount"],
        "ready": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate exact FoldCanvas M16 stable-readiness evidence."
    )
    parser.add_argument("--contract", type=pathlib.Path, default=DEFAULT_CONTRACT)
    parser.add_argument("--report", type=pathlib.Path, default=DEFAULT_REPORT)
    parser.add_argument("--run-metadata", required=True, type=pathlib.Path)
    parser.add_argument("--artifact-metadata", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()

    report_path = args.report.resolve()
    result = validate(
        load_object(args.contract.resolve(), "Stable release contract"),
        load_object(report_path, "Stable readiness report"),
        sha256_file(report_path),
        load_object(args.run_metadata.resolve(), "Workflow run metadata"),
        load_object(args.artifact_metadata.resolve(), "Artifact metadata"),
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        canonical_json(result),
        encoding="utf-8",
        newline="\n",
    )
    print(
        "Stable readiness verified: "
        f"run {result['workflowRunId']}, {result['soakHours']} hours, "
        f"{result['qualifyingScheduledLongRuns']} scheduled runs."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
