#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from verify_patch_release_authorization import (  # noqa: E402
    AUDIT_PREFIX,
    PatchReleaseAuthorizationError,
    REQUIRED_MAIN_WORKFLOWS,
    authorize,
)

ROOT = pathlib.Path(__file__).resolve().parents[1]
TAG_COMMIT = "a" * 40
HEAD = "b" * 40


def fixtures() -> dict:
    return {
        "contract": json.loads(
            (ROOT / "Documentation~" / "m23-patch-release.json").read_text(
                encoding="utf-8"
            )
        ),
        "pull_requests": [
            {
                "number": 37,
                "state": "closed",
                "merged_at": "2026-08-13T06:00:00Z",
                "merge_commit_sha": TAG_COMMIT,
                "head": {"sha": HEAD},
                "base": {"ref": "main"},
            }
        ],
        "comments": [
            {
                "id": 9001,
                "body": f"{AUDIT_PREFIX}\n\nAudited head: `{HEAD}`\n\nAll gates green.",
                "created_at": "2026-08-13T05:59:00Z",
                "author_association": "OWNER",
                "user": {"login": "zhangpan-soft"},
            }
        ],
        "workflow_runs": {
            "workflow_runs": [
                {
                    "id": index + 100,
                    "name": name,
                    "event": "push",
                    "head_branch": "main",
                    "head_sha": TAG_COMMIT,
                    "status": "completed",
                    "conclusion": "success",
                }
                for index, name in enumerate(REQUIRED_MAIN_WORKFLOWS)
            ]
        },
        "blockers": {"total_count": 0, "items": []},
    }


def run(value: dict) -> dict:
    return authorize(
        **value,
        repository="zhangpan-soft/FoldCanvas",
        maintainer="zhangpan-soft",
        tag="v1.0.1",
        tag_commit=TAG_COMMIT,
    )


def rejected(mutate, label: str) -> None:
    value = fixtures()
    mutate(value)
    try:
        run(value)
    except PatchReleaseAuthorizationError:
        return
    raise AssertionError(f"authorization accepted {label}")


def main() -> int:
    report = run(fixtures())
    if (
        report["authorized"] is not True
        or report["auditedHead"] != HEAD
        or [item["name"] for item in report["mainWorkflowRuns"]]
        != list(REQUIRED_MAIN_WORKFLOWS)
    ):
        raise AssertionError("valid authorization report differs")
    cases = 1

    tag_routed = fixtures()
    tag_routed["workflow_runs"]["workflow_runs"] = [
        run
        for run in tag_routed["workflow_runs"]["workflow_runs"]
        if run["name"] != "M13 robustness long run"
    ]
    tag_routed["workflow_runs"]["workflow_runs"].append(
        {
            "id": 777,
            "name": "M13 robustness long run",
            "event": "push",
            "head_branch": "v1.0.1",
            "head_sha": TAG_COMMIT,
            "status": "completed",
            "conclusion": "success",
        }
    )
    tag_report = run(tag_routed)
    if tag_report["mainWorkflowRuns"][0]["runId"] != 777:
        raise AssertionError("exact-tag long-run evidence differs")
    cases += 1

    rejected_tag = copy.deepcopy(tag_routed)
    rejected_tag["workflow_runs"]["workflow_runs"][-1]["head_branch"] = "v1.0.2"
    try:
        run(rejected_tag)
    except PatchReleaseAuthorizationError:
        pass
    else:
        raise AssertionError("authorization accepted another tag's long run")
    cases += 1

    rejected(lambda value: value["pull_requests"].clear(), "missing PR")
    cases += 1
    rejected(
        lambda value: value["pull_requests"][0].update(
            merge_commit_sha="c" * 40
        ),
        "wrong merge commit",
    )
    cases += 1
    rejected(lambda value: value["comments"].clear(), "missing audit")
    cases += 1
    rejected(
        lambda value: value["comments"][0]["user"].update(login="outsider"),
        "non-owner audit",
    )
    cases += 1
    rejected(
        lambda value: value["comments"][0].update(
            created_at="2026-08-13T06:01:00Z"
        ),
        "post-merge audit",
    )
    cases += 1
    rejected(
        lambda value: value["workflow_runs"]["workflow_runs"].pop(),
        "missing main workflow",
    )
    cases += 1
    rejected(
        lambda value: value["workflow_runs"]["workflow_runs"][0].update(
            conclusion="failure"
        ),
        "failed main workflow",
    )
    cases += 1
    rejected(
        lambda value: value["blockers"].update(
            total_count=1, items=[{"number": 99}]
        ),
        "open release blocker",
    )
    cases += 1
    rejected(
        lambda value: value["blockers"].update(total_count=0, items=None),
        "malformed blocker payload",
    )
    cases += 1
    rejected(
        lambda value: value["contract"].update(packageVersion="1.0.2"),
        "wrong contract version",
    )
    cases += 1

    first = run(fixtures())
    if first != run(copy.deepcopy(fixtures())):
        raise AssertionError("authorization output is not deterministic")
    cases += 1
    print(f"M23 patch authorization tests passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
