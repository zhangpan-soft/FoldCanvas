#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import re

SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
REQUIRED_MAIN_WORKFLOWS = (
    "M13 robustness long run",
    "Repository checks",
    "Unity Edit Mode tests",
)
AUDIT_PREFIX = "M23 exact-head maintainer audit: APPROVED"


class PatchReleaseAuthorizationError(ValueError):
    pass


def load_json(path: pathlib.Path, label: str):
    if path.is_symlink() or not path.is_file() or path.stat().st_size <= 0:
        raise PatchReleaseAuthorizationError(f"{label} must be one regular JSON file")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exception:  # noqa: BLE001
        raise PatchReleaseAuthorizationError(f"{label} is invalid JSON") from exception


def parse_time(value: object, label: str) -> dt.datetime:
    if not isinstance(value, str):
        raise PatchReleaseAuthorizationError(f"{label} must be a UTC timestamp")
    try:
        parsed = dt.datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exception:
        raise PatchReleaseAuthorizationError(
            f"{label} must be a UTC timestamp"
        ) from exception
    if parsed.tzinfo is None or parsed.utcoffset() != dt.timedelta(0):
        raise PatchReleaseAuthorizationError(f"{label} must be a UTC timestamp")
    return parsed


def authorize(
    *,
    contract: dict,
    pull_requests: object,
    comments: object,
    workflow_runs: object,
    blockers: object,
    repository: str,
    maintainer: str,
    tag: str,
    tag_commit: str,
) -> dict:
    if (
        contract.get("format") != "foldcanvas-patch-release"
        or contract.get("packageVersion") != "1.0.1"
        or contract.get("tag") != tag
        or contract.get("stableRelease") is not True
        or contract.get("patchRelease") is not True
    ):
        raise PatchReleaseAuthorizationError("Patch release contract identity differs")
    if tag != "v1.0.1" or SHA_PATTERN.fullmatch(tag_commit) is None:
        raise PatchReleaseAuthorizationError("Patch tag or commit identity differs")
    if repository != "zhangpan-soft/FoldCanvas" or maintainer != "zhangpan-soft":
        raise PatchReleaseAuthorizationError("Repository maintainer identity differs")

    if not isinstance(pull_requests, list) or len(pull_requests) != 1:
        raise PatchReleaseAuthorizationError(
            "Patch merge commit must resolve to exactly one pull request"
        )
    pull = pull_requests[0]
    if not isinstance(pull, dict):
        raise PatchReleaseAuthorizationError("Patch pull request metadata is invalid")
    head = pull.get("head", {})
    base = pull.get("base", {})
    head_sha = head.get("sha") if isinstance(head, dict) else None
    number = pull.get("number")
    if (
        not isinstance(number, int)
        or number <= 0
        or SHA_PATTERN.fullmatch(str(head_sha)) is None
        or pull.get("merge_commit_sha") != tag_commit
        or pull.get("state") != "closed"
        or not isinstance(base, dict)
        or base.get("ref") != "main"
    ):
        raise PatchReleaseAuthorizationError("Patch pull request identity differs")
    merged_at = parse_time(pull.get("merged_at"), "Pull request merged_at")

    if not isinstance(comments, list):
        raise PatchReleaseAuthorizationError("Patch audit comments must be an array")
    marker = f"{AUDIT_PREFIX}\n\nAudited head: `{head_sha}`"
    matching_audits = []
    for comment in comments:
        if not isinstance(comment, dict):
            continue
        user = comment.get("user", {})
        if (
            isinstance(user, dict)
            and user.get("login") == maintainer
            and comment.get("author_association") == "OWNER"
            and isinstance(comment.get("body"), str)
            and marker in comment["body"]
        ):
            matching_audits.append(comment)
    if len(matching_audits) != 1:
        raise PatchReleaseAuthorizationError(
            "Patch requires exactly one owner exact-head approval"
        )
    audit = matching_audits[0]
    audit_at = parse_time(audit.get("created_at"), "Audit created_at")
    if audit_at > merged_at:
        raise PatchReleaseAuthorizationError("Patch audit must precede merge")

    if not isinstance(workflow_runs, dict) or not isinstance(
        workflow_runs.get("workflow_runs"), list
    ):
        raise PatchReleaseAuthorizationError("Main workflow evidence is invalid")
    successful = set()
    run_ids: dict[str, int] = {}
    for run in workflow_runs["workflow_runs"]:
        if not isinstance(run, dict):
            continue
        name = run.get("name")
        if (
            name in REQUIRED_MAIN_WORKFLOWS
            and run.get("event") == "push"
            and run.get("head_branch") == "main"
            and run.get("head_sha") == tag_commit
            and run.get("status") == "completed"
            and run.get("conclusion") == "success"
            and isinstance(run.get("id"), int)
        ):
            successful.add(name)
            run_ids[name] = run["id"]
    # The long-run workflow is path-filtered on main. An annotated tag at the
    # already-proven protected-main tip reruns the identical tree, so accept
    # that exact-SHA tag push as the long-run evidence when main did not route
    # a run for a CI-only merge.
    if "M13 robustness long run" not in successful:
        for run in workflow_runs["workflow_runs"]:
            if (
                isinstance(run, dict)
                and run.get("name") == "M13 robustness long run"
                and run.get("event") == "push"
                and run.get("head_branch") == tag
                and run.get("head_sha") == tag_commit
                and run.get("status") == "completed"
                and run.get("conclusion") == "success"
                and isinstance(run.get("id"), int)
            ):
                successful.add("M13 robustness long run")
                run_ids["M13 robustness long run"] = run["id"]
    missing = sorted(set(REQUIRED_MAIN_WORKFLOWS).difference(successful))
    if missing:
        raise PatchReleaseAuthorizationError(
            "Patch lacks successful exact-merge main workflows: " + ", ".join(missing)
        )

    if (
        not isinstance(blockers, dict)
        or blockers.get("total_count") != 0
        or blockers.get("items") != []
    ):
        raise PatchReleaseAuthorizationError("Open release-blocker issues prevent patch")

    return {
        "format": "foldcanvas-patch-release-authorization",
        "version": "1",
        "repository": repository,
        "packageVersion": contract["packageVersion"],
        "tag": tag,
        "tagCommit": tag_commit,
        "pullRequest": number,
        "auditedHead": head_sha,
        "auditCommentId": audit.get("id"),
        "auditRecordedAt": audit_at.isoformat().replace("+00:00", "Z"),
        "mergedAt": merged_at.isoformat().replace("+00:00", "Z"),
        "mainWorkflowRuns": [
            {"name": name, "runId": run_ids[name]}
            for name in REQUIRED_MAIN_WORKFLOWS
        ],
        "openReleaseBlockerCount": 0,
        "authorized": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Verify fail-closed authorization for FoldCanvas v1.0.1."
    )
    parser.add_argument("--contract", required=True, type=pathlib.Path)
    parser.add_argument("--pull-requests", required=True, type=pathlib.Path)
    parser.add_argument("--comments", required=True, type=pathlib.Path)
    parser.add_argument("--workflow-runs", required=True, type=pathlib.Path)
    parser.add_argument("--blockers", required=True, type=pathlib.Path)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--maintainer", required=True)
    parser.add_argument("--tag", required=True)
    parser.add_argument("--tag-commit", required=True)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    report = authorize(
        contract=load_json(args.contract, "Patch contract"),
        pull_requests=load_json(args.pull_requests, "Associated pull requests"),
        comments=load_json(args.comments, "Audit comments"),
        workflow_runs=load_json(args.workflow_runs, "Main workflow runs"),
        blockers=load_json(args.blockers, "Release blockers"),
        repository=args.repository,
        maintainer=args.maintainer,
        tag=args.tag,
        tag_commit=args.tag_commit,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(report, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(
        "Patch release authorization passed: "
        f"PR #{report['pullRequest']} {report['tagCommit']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
