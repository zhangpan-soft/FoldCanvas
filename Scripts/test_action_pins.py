#!/usr/bin/env python3
"""Deterministic positive and negative tests for workflow Action pins."""

from __future__ import annotations

from validate_action_pins import (
    APPROVED_ACTIONS,
    collect_action_pin_errors,
    load_workflows,
)


def approved_fixture() -> str:
    lines = ["jobs:", "  validate:", "    steps:"]
    for action in sorted(APPROVED_ACTIONS):
        version, sha = APPROVED_ACTIONS[action]
        lines.append(f"      - uses: {action}@{sha} # {version}")
    return "\n".join(lines) + "\n"


def assert_has_error(text: str, fragment: str) -> None:
    errors = collect_action_pin_errors({"fixture.yml": text})
    if not any(fragment in error for error in errors):
        raise AssertionError(f"missing expected error {fragment!r}: {errors}")


def main() -> int:
    repository_errors = collect_action_pin_errors(load_workflows())
    if repository_errors:
        raise AssertionError(f"repository Action pins are invalid: {repository_errors}")

    valid = approved_fixture()
    if collect_action_pin_errors({"fixture.yml": valid}):
        raise AssertionError("approved fixture must pass")

    checkout_version, checkout_sha = APPROVED_ACTIONS["actions/checkout"]
    assert_has_error(
        valid.replace(checkout_sha, checkout_version, 1),
        "lowercase 40-character commit SHA",
    )
    assert_has_error(
        valid.replace(checkout_sha, checkout_sha[:12], 1),
        "lowercase 40-character commit SHA",
    )
    assert_has_error(
        valid.replace(checkout_sha, checkout_sha.upper(), 1),
        "lowercase 40-character commit SHA",
    )
    assert_has_error(
        valid.replace(f" # {checkout_version}", "", 1),
        "must retain exact version comment",
    )
    assert_has_error(
        valid.replace(f"# {checkout_version}", "# v4", 1),
        "must retain exact version comment",
    )
    assert_has_error(
        valid.replace(checkout_sha, "0" * 40, 1),
        "is not the approved",
    )
    assert_has_error(
        valid + "      - uses: external/unreviewed@" + "1" * 40 + " # v1.0.0\n",
        "unapproved remote Action external/unreviewed",
    )
    assert_has_error(
        valid + "      - uses: docker://example/image:latest\n",
        "remote Action must use owner/repository@commit",
    )

    local_action = valid + "      - uses: ./.github/actions/local-proof\n"
    if collect_action_pin_errors({"fixture.yml": local_action}):
        raise AssertionError("local repository Actions must remain allowed")

    unordered_workflows = {
        "z.yml": "steps:\n  - uses: actions/checkout@v4.2.2\n",
        "a.yml": "steps:\n  - uses: external/unreviewed@main\n",
    }
    ordered_errors = collect_action_pin_errors(
        unordered_workflows,
        require_all_approved=False,
    )
    reversed_errors = collect_action_pin_errors(
        dict(reversed(list(unordered_workflows.items()))),
        require_all_approved=False,
    )
    if ordered_errors != reversed_errors:
        raise AssertionError(
            "Action pin errors depend on input order: "
            f"{ordered_errors} != {reversed_errors}"
        )

    print("Immutable Action pin validation tests passed (12 cases).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
