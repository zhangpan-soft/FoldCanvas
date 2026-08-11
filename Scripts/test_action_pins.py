#!/usr/bin/env python3
"""Deterministic positive and negative tests for workflow Action pins."""

from __future__ import annotations

from validate_action_pins import (
    APPROVED_ACTIONS,
    NODE24_ACTIONS,
    REVIEWED_RUNTIME_EXCEPTIONS,
    collect_action_pin_errors,
    load_local_actions,
    load_workflows,
)


STALE_ACTIONS = {
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
}


def approved_fixture() -> str:
    lines = ["jobs:", "  validate:", "    steps:"]
    for action in sorted(APPROVED_ACTIONS):
        version, sha = APPROVED_ACTIONS[action]
        lines.append(f"      - uses: {action}@{sha} # {version}")
    return "\n".join(lines) + "\n"


def local_manifest(*uses: str) -> str:
    lines = [
        "name: Local proof",
        "runs:",
        "  using: composite",
        "  steps:",
    ]
    if not uses:
        lines.extend(["    - shell: bash", "      run: echo safe"])
    else:
        lines.extend(f"    - uses: {target}" for target in uses)
    return "\n".join(lines) + "\n"


def assert_has_error(
    text: str,
    fragment: str,
    *,
    local_actions: dict[str, str] | None = None,
) -> None:
    errors = collect_action_pin_errors(
        {"fixture.yml": text},
        local_actions,
    )
    if not any(fragment in error for error in errors):
        raise AssertionError(f"missing expected error {fragment!r}: {errors}")


def assert_passes(
    text: str,
    *,
    local_actions: dict[str, str] | None = None,
) -> None:
    errors = collect_action_pin_errors(
        {"fixture.yml": text},
        local_actions,
    )
    if errors:
        raise AssertionError(f"expected Action pin fixture to pass: {errors}")


def main() -> int:
    if NODE24_ACTIONS != frozenset(STALE_ACTIONS):
        raise AssertionError("M18 Node 24 Action set drifted")
    if REVIEWED_RUNTIME_EXCEPTIONS != {
        "game-ci/unity-test-runner": (
            "node20",
            "https://github.com/game-ci/unity-test-runner/pull/304",
        )
    }:
        raise AssertionError("M18 reviewed runtime exception drifted")
    if NODE24_ACTIONS | REVIEWED_RUNTIME_EXCEPTIONS.keys() != APPROVED_ACTIONS.keys():
        raise AssertionError("every approved Action needs a reviewed runtime status")

    repository_errors = collect_action_pin_errors(
        load_workflows(),
        load_local_actions(),
    )
    if repository_errors:
        raise AssertionError(f"repository Action pins are invalid: {repository_errors}")

    valid = approved_fixture()
    assert_passes(valid)

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
    for action in sorted(STALE_ACTIONS):
        current_version, current_sha = APPROVED_ACTIONS[action]
        stale_version, stale_sha = STALE_ACTIONS[action]
        current_line = f"{action}@{current_sha} # {current_version}"
        assert_has_error(
            valid.replace(
                current_line,
                f"{action}@{stale_sha} # {stale_version}",
                1,
            ),
            "is not the approved",
        )
        assert_has_error(
            valid.replace(
                current_line,
                f"{action}@{current_sha} # {stale_version}",
                1,
            ),
            "must retain exact version comment",
        )
        assert_has_error(
            valid.replace(
                current_line,
                f"{action}@{stale_sha} # {current_version}",
                1,
            ),
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

    unreviewed_target = "external/unreviewed@" + "1" * 40
    assert_has_error(
        valid + f'      - "uses": {unreviewed_target}\n',
        "quoted mapping keys are not allowed",
    )
    assert_has_error(
        valid + f"      - 'uses': {unreviewed_target}\n",
        "quoted mapping keys are not allowed",
    )
    assert_has_error(
        valid + rf'      - "\x75ses": {unreviewed_target}' + "\n",
        "quoted mapping keys are not allowed",
    )
    assert_has_error(
        valid + f"      - {{uses: {unreviewed_target}}}\n",
        "flow-style YAML collections are not allowed",
    )
    assert_has_error(
        valid + f"      - ? uses\n        : {unreviewed_target}\n",
        "explicit YAML mapping keys are not allowed",
    )
    assert_has_error(
        valid
        + "      key-name: &uses-key uses\n"
        + f"      - *uses-key: {unreviewed_target}\n",
        "YAML anchors and aliases are not allowed",
    )
    assert_has_error(
        valid + "      key-name: &锚点 safe\n",
        "YAML anchors and aliases are not allowed",
    )
    assert_has_error(
        valid + "\t- uses: external/unreviewed@main\n",
        "tabs are not allowed in YAML indentation",
    )

    local_workflow = valid + "      - uses: ./.github/actions/local-proof\n"
    local_actions = {
        ".github/actions/local-proof/action.yml": local_manifest(),
    }
    assert_passes(local_workflow, local_actions=local_actions)

    assert_has_error(
        local_workflow,
        "unapproved remote Action external/unreviewed",
        local_actions={
            ".github/actions/local-proof/action.yml": local_manifest(
                unreviewed_target
            )
        },
    )
    assert_has_error(
        local_workflow,
        "lowercase 40-character commit SHA",
        local_actions={
            ".github/actions/local-proof/action.yml": local_manifest(
                "actions/checkout@v4.2.2"
            )
        },
    )
    assert_has_error(
        local_workflow,
        "unapproved remote Action external/unreviewed",
        local_actions={
            ".github/actions/local-proof/action.yml": local_manifest(
                "./.github/actions/nested"
            ),
            ".github/actions/nested/action.yml": local_manifest(
                unreviewed_target
            ),
        },
    )
    assert_has_error(
        valid,
        "unapproved remote Action external/unreviewed",
        local_actions={
            ".github/actions/unreferenced/action.yml": local_manifest(
                unreviewed_target
            )
        },
    )
    assert_has_error(
        valid + "      - uses: ./.github/actions/../escape\n",
        "local Action must stay under",
    )
    assert_has_error(
        valid + "      - uses: ./tools/action\n",
        "local Action must stay under",
    )
    assert_has_error(
        valid + "      - uses: ./.github/actions/missing\n",
        "has no action.yml or action.yaml manifest",
    )
    assert_has_error(
        local_workflow,
        "must have exactly one manifest",
        local_actions={
            ".github/actions/local-proof/action.yml": local_manifest(),
            ".github/actions/local-proof/action.yaml": local_manifest(),
        },
    )
    assert_has_error(
        valid,
        "must have exactly one manifest",
        local_actions={
            ".github/actions/unreferenced/action.yml": local_manifest(),
            ".github/actions/unreferenced/action.yaml": local_manifest(),
        },
    )
    assert_has_error(
        local_workflow,
        "local Action reference cycle",
        local_actions={
            ".github/actions/local-proof/action.yml": local_manifest(
                "./.github/actions/nested"
            ),
            ".github/actions/nested/action.yml": local_manifest(
                "./.github/actions/local-proof"
            ),
        },
    )

    assert_passes(
        valid
        + "      - name: Shell text is not YAML structure\n"
        + "        run: |\n"
        + "          echo '- uses: external/unreviewed@main'\n"
    )
    assert_passes(valid + "permissions: {}\n")
    assert_passes(valid + "      - run: echo safe &\n")
    assert_passes(valid + "      - run: echo *\n")
    assert_passes(valid + "      - run: echo *.txt\n")

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

    print("Immutable Action pin validation tests passed (44 cases).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
