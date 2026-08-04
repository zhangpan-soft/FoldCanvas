#!/usr/bin/env python3
"""Fail closed if fork intake can reach privileged Unity credentials."""

from __future__ import annotations

import pathlib


ROOT = pathlib.Path(__file__).resolve().parents[1]
TRUSTED_GATE = ROOT / ".github" / "workflows" / "trusted-contribution-gate.yml"
UNITY_WORKFLOW = ROOT / ".github" / "workflows" / "unity-tests.yml"


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
        "      github.event_name != 'pull_request' ||\n"
        "      (github.event.pull_request.head.repo.full_name == github.repository &&\n"
        "      github.event.pull_request.user.login == github.repository_owner)"
    )
    if unity.count(fork_guard) != 4:
        raise AssertionError("every privileged Unity job must use the fork guard")
    if unity.count("      checks: write") != 4:
        raise AssertionError("checks: write must be scoped to the four Unity jobs")
    if "permissions:\n  contents: read\n  checks: write\n\njobs:" in unity:
        raise AssertionError("Unity workflow must not grant checks:write globally")

    print("Trusted external-contribution gate validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
