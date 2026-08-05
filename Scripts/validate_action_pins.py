#!/usr/bin/env python3
"""Validate immutable, reviewed GitHub Action references in every workflow."""

from __future__ import annotations

import pathlib
import re
import sys
from collections.abc import Mapping


ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKFLOW_DIRECTORY = ROOT / ".github" / "workflows"

# Resolve these version tags only through the Action's official GitHub
# repository. The version remains a review label; workflows execute the SHA.
APPROVED_ACTIONS: dict[str, tuple[str, str]] = {
    "actions/checkout": (
        "v4.2.2",
        "11bd71901bbe5b1630ceea73d27597364c9af683",
    ),
    "actions/download-artifact": (
        "v4.1.8",
        "fa0a91b85d4f404e444e00e005971372dc801d16",
    ),
    "actions/upload-artifact": (
        "v4.6.2",
        "ea165f8d65b6e75b540449e92b4886f43607fa02",
    ),
    "game-ci/unity-test-runner": (
        "v4.3.1",
        "0ff419b913a3630032cbe0de48a0099b5a9f0ed9",
    ),
}

USES_PREFIX = re.compile(r"^\s*(?:-\s+)?uses\s*:")
USES_LINE = re.compile(
    r"^\s*(?:-\s+)?uses\s*:\s*(?P<target>[^\s#]+)"
    r"(?:\s+#\s*(?P<comment>.*?))?\s*$"
)
REMOTE_ACTION = re.compile(
    r"^(?P<action>[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*)"
    r"@(?P<ref>.+)$"
)
FULL_COMMIT_SHA = re.compile(r"^[0-9a-f]{40}$")


def load_workflows(root: pathlib.Path = ROOT) -> dict[str, str]:
    workflow_directory = root / ".github" / "workflows"
    paths = sorted(
        set(workflow_directory.glob("*.yml"))
        | set(workflow_directory.glob("*.yaml"))
    )
    return {
        path.relative_to(root).as_posix(): path.read_text(encoding="utf-8")
        for path in paths
    }


def collect_action_pin_errors(
    workflows: Mapping[str, str],
    *,
    require_all_approved: bool = True,
) -> list[str]:
    errors: list[str] = []
    seen_actions: set[str] = set()

    for workflow_name in sorted(workflows):
        text = workflows[workflow_name]
        for line_number, line in enumerate(text.splitlines(), start=1):
            if USES_PREFIX.match(line) is None:
                continue

            match = USES_LINE.match(line)
            location = f"{workflow_name}:{line_number}"
            if match is None:
                errors.append(f"{location}: malformed Action reference")
                continue

            target = match.group("target")
            if target.startswith("./"):
                continue

            remote_match = REMOTE_ACTION.match(target)
            if remote_match is None:
                errors.append(
                    f"{location}: remote Action must use owner/repository@commit"
                )
                continue

            action = remote_match.group("action")
            reference = remote_match.group("ref")
            comment = (match.group("comment") or "").strip()
            approved = APPROVED_ACTIONS.get(action)
            if approved is None:
                errors.append(f"{location}: unapproved remote Action {action}")
                continue

            seen_actions.add(action)
            expected_version, expected_sha = approved
            if FULL_COMMIT_SHA.fullmatch(reference) is None:
                errors.append(
                    f"{location}: {action} must use a lowercase 40-character "
                    "commit SHA"
                )
            elif reference != expected_sha:
                errors.append(
                    f"{location}: {action} commit {reference} is not the "
                    f"approved {expected_sha}"
                )

            if comment != expected_version:
                errors.append(
                    f"{location}: {action} must retain exact version comment "
                    f"# {expected_version}"
                )

    if require_all_approved:
        for action in sorted(set(APPROVED_ACTIONS) - seen_actions):
            errors.append(f"approved Action is unused: {action}")

    return errors


def main() -> int:
    workflows = load_workflows()
    errors = collect_action_pin_errors(workflows)
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    print(
        "Immutable Action pin validation passed for "
        f"{len(workflows)} workflows and {len(APPROVED_ACTIONS)} approved Actions."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
