#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib

from verify_minor_release_authorization import (
    AUDIT_PREFIX,
    MinorReleaseAuthorizationError,
    REQUIRED_MAIN_WORKFLOWS,
    authorize,
)


ROOT = pathlib.Path(__file__).resolve().parents[1]
TAG_COMMIT = "a" * 40
HEAD = "b" * 40


def fixtures() -> dict:
    return {
        "contract": json.loads(
            (ROOT / "Documentation~" / "m25-minor-release.json").read_text(
                encoding="utf-8"
            )
        ),
        "pull_requests": [
            {
                "number": 42,
                "state": "closed",
                "merged_at": "2026-08-13T16:00:00Z",
                "merge_commit_sha": TAG_COMMIT,
                "head": {"sha": HEAD},
                "base": {"ref": "main"},
            }
        ],
        "comments": [
            {
                "id": 9100,
                "body": f"{AUDIT_PREFIX}\n\nAudited head: `{HEAD}`\n\nAll gates green.",
                "created_at": "2026-08-13T15:59:00Z",
                "author_association": "OWNER",
                "user": {"login": "zhangpan-soft"},
            }
        ],
        "workflow_runs": {
            "workflow_runs": [
                {
                    "id": index + 200,
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
        tag="v1.1.0",
        tag_commit=TAG_COMMIT,
    )


def rejected(mutate, label: str) -> None:
    value = fixtures()
    mutate(value)
    try:
        run(value)
    except MinorReleaseAuthorizationError:
        return
    raise AssertionError(f"minor authorization accepted {label}")


def main() -> int:
    report = run(fixtures())
    if report["authorized"] is not True or report["auditedHead"] != HEAD:
        raise AssertionError("valid minor authorization differs")
    cases = 1

    tag_routed = fixtures()
    tag_routed["workflow_runs"]["workflow_runs"] = [
        item
        for item in tag_routed["workflow_runs"]["workflow_runs"]
        if item["name"] != "M13 robustness long run"
    ]
    tag_routed["workflow_runs"]["workflow_runs"].append(
        {
            "id": 999,
            "name": "M13 robustness long run",
            "event": "push",
            "head_branch": "v1.1.0",
            "head_sha": TAG_COMMIT,
            "status": "completed",
            "conclusion": "success",
        }
    )
    if run(tag_routed)["mainWorkflowRuns"][0]["runId"] != 999:
        raise AssertionError("minor exact-tag long-run evidence differs")
    cases += 1

    for mutate, label in (
        (lambda value: value["pull_requests"].clear(), "missing PR"),
        (lambda value: value["comments"].clear(), "missing audit"),
        (
            lambda value: value["comments"][0]["user"].update(login="outsider"),
            "non-owner audit",
        ),
        (
            lambda value: value["workflow_runs"]["workflow_runs"][0].update(
                conclusion="failure"
            ),
            "failed workflow",
        ),
        (
            lambda value: value["blockers"].update(
                total_count=1, items=[{"number": 99}]
            ),
            "release blocker",
        ),
        (
            lambda value: value["contract"].update(packageVersion="1.1.1"),
            "wrong contract version",
        ),
    ):
        rejected(mutate, label)
        cases += 1

    if run(fixtures()) != run(copy.deepcopy(fixtures())):
        raise AssertionError("minor authorization is not deterministic")
    cases += 1
    print(f"M25 minor authorization tests passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
