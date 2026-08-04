#!/usr/bin/env python3
"""Fail closed if fork intake can reach privileged Unity credentials."""

from __future__ import annotations

import pathlib


ROOT = pathlib.Path(__file__).resolve().parents[1]
TRUSTED_GATE = ROOT / ".github" / "workflows" / "trusted-contribution-gate.yml"
UNITY_WORKFLOW = ROOT / ".github" / "workflows" / "unity-tests.yml"


def privileged_unity_allowed(
    event_name: str,
    ref: str,
    repository: str,
    head_repository: str,
    pr_author: str,
    repository_owner: str,
) -> bool:
    return (event_name == "push" and ref == "refs/heads/main") or (
        event_name == "pull_request"
        and head_repository == repository
        and pr_author == repository_owner
    )


def main() -> int:
    gate = TRUSTED_GATE.read_text(encoding="utf-8")
    unity = UNITY_WORKFLOW.read_text(encoding="utf-8")

    required_gate_fragments = (
        "pull_request_target:",
        "permissions: {}",
        "HEAD_REPOSITORY: ${{ github.event.pull_request.head.repo.full_name }}",
        "PR_AUTHOR: ${{ github.event.pull_request.user.login }}",
        "TRUSTED_OWNER: ${{ github.repository_owner }}",
        'if [[ "$HEAD_REPOSITORY" != "$BASE_REPOSITORY" || "$PR_AUTHOR" != "$TRUSTED_OWNER" ]]',
        "Maintainer integration required",
        "exit 1",
    )
    missing = [fragment for fragment in required_gate_fragments if fragment not in gate]
    if missing:
        raise AssertionError(f"trusted contribution gate is incomplete: {missing}")
    for forbidden in ("actions/checkout", "secrets.", "pull_request_target: write"):
        if forbidden in gate:
            raise AssertionError(
                f"trusted contribution gate contains forbidden behavior: {forbidden}"
            )
    if "uses:" in gate:
        raise AssertionError("trusted contribution gate must not execute an action")

    fork_guard = (
        "if: >-\n"
        "      (github.event_name == 'push' && github.ref == 'refs/heads/main') ||\n"
        "      (github.event_name == 'pull_request' &&\n"
        "      github.event.pull_request.head.repo.full_name == github.repository &&\n"
        "      github.event.pull_request.user.login == github.repository_owner)"
    )
    if unity.count(fork_guard) != 4:
        raise AssertionError("every privileged Unity job must use the fork guard")
    if unity.count("      checks: write") != 4:
        raise AssertionError("checks: write must be scoped to the four Unity jobs")
    if "permissions:\n  contents: read\n  checks: write\n\njobs:" in unity:
        raise AssertionError("Unity workflow must not grant checks:write globally")
    if "push:\n    branches:\n      - main" not in unity:
        raise AssertionError("privileged Unity push trigger must be main-only")

    repository = "zhangpan-soft/FoldCanvas"
    owner = "zhangpan-soft"
    cases = (
        ("protected main push", ("push", "refs/heads/main", repository, "", "", owner), True),
        ("owner feature push", ("push", "refs/heads/feature", repository, "", owner, owner), False),
        ("non-owner feature push", ("push", "refs/heads/feature", repository, "", "agent", owner), False),
        ("owner internal PR", ("pull_request", "refs/pull/1/merge", repository, repository, owner, owner), True),
        ("owner fork PR", ("pull_request", "refs/pull/2/merge", repository, "owner/FoldCanvas", owner, owner), False),
        ("same-repo bot PR", ("pull_request", "refs/pull/3/merge", repository, repository, "agent[bot]", owner), False),
        ("unrelated event", ("workflow_dispatch", "refs/heads/main", repository, repository, owner, owner), False),
    )
    for label, arguments, expected in cases:
        actual = privileged_unity_allowed(*arguments)
        if actual is not expected:
            raise AssertionError(f"unexpected privileged Unity decision for {label}")

    print("Trusted external-contribution gate validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
