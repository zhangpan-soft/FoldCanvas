#!/usr/bin/env python3
"""Validate immutable, reviewed GitHub Action references in every workflow."""

from __future__ import annotations

import pathlib
import re
import sys
from collections.abc import Mapping


ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKFLOW_DIRECTORY = ROOT / ".github" / "workflows"
LOCAL_ACTION_DIRECTORY = ".github/actions"

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
LOCAL_ACTION_TARGET = re.compile(
    r"^\./\.github/actions/[A-Za-z0-9_.-]+"
    r"(?:/[A-Za-z0-9_.-]+)*$"
)
BLOCK_SCALAR_HEADER = re.compile(r":[ \t]*[|>][0-9+-]*[ \t]*$")
QUOTED_MAPPING_KEY = re.compile(
    r"^\s*(?:-\s*)?(?:\"(?:\\.|[^\"])*\"|'(?:''|[^'])*')\s*:"
)
EXPLICIT_MAPPING_KEY = re.compile(r"^\s*(?:-\s*)?\?(?:\s|$)")
TAGGED_MAPPING_KEY = re.compile(r"^\s*(?:-\s*)?![^=]")
MERGE_MAPPING_KEY = re.compile(r"^\s*(?:-\s*)?<<\s*:")
YAML_ANCHOR_OR_ALIAS = re.compile(r"(?:^|[\s\[\]{},:-])[&*]")
GITHUB_EXPRESSION = re.compile(r"\$\{\{.*?\}\}")
CANONICAL_EMPTY_COLLECTION = re.compile(
    r"^\s*(?:-\s*)?[A-Za-z0-9_.-]+\s*:\s*(?:\{\s*\}|\[\s*\])\s*$"
)


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


def load_local_actions(root: pathlib.Path = ROOT) -> dict[str, str]:
    action_directory = root / LOCAL_ACTION_DIRECTORY
    if not action_directory.is_dir():
        return {}

    paths = sorted(
        set(action_directory.rglob("action.yml"))
        | set(action_directory.rglob("action.yaml"))
    )
    return {
        path.relative_to(root).as_posix(): path.read_text(encoding="utf-8")
        for path in paths
    }


def strip_yaml_comment(line: str) -> str:
    """Remove a YAML comment without treating hashes inside quotes as comments."""

    quote: str | None = None
    index = 0
    while index < len(line):
        character = line[index]
        if quote == "'":
            if character == "'":
                if index + 1 < len(line) and line[index + 1] == "'":
                    index += 2
                    continue
                quote = None
        elif quote == '"':
            if character == "\\":
                index += 2
                continue
            if character == '"':
                quote = None
        elif character in ("'", '"'):
            quote = character
        elif character == "#" and (
            index == 0 or line[index - 1].isspace()
        ):
            return line[:index].rstrip()
        index += 1
    return line.rstrip()


def mask_quoted_scalars(line: str) -> str:
    """Mask quoted YAML content before checking structural metacharacters."""

    characters = list(line)
    quote: str | None = None
    index = 0
    while index < len(characters):
        character = characters[index]
        if quote == "'":
            characters[index] = " "
            if character == "'":
                if index + 1 < len(characters) and characters[index + 1] == "'":
                    characters[index + 1] = " "
                    index += 2
                    continue
                quote = None
        elif quote == '"':
            characters[index] = " "
            if character == "\\" and index + 1 < len(characters):
                characters[index + 1] = " "
                index += 2
                continue
            if character == '"':
                quote = None
        elif character in ("'", '"'):
            quote = character
            characters[index] = " "
        index += 1
    return "".join(characters)


def collect_document_action_references(
    document_name: str,
    document_text: str,
    errors: list[str],
    seen_actions: set[str],
    local_references: list[tuple[str, str, str]],
) -> None:
    block_scalar_indent: int | None = None

    for line_number, line in enumerate(document_text.splitlines(), start=1):
        stripped = line.strip()
        indentation = len(line) - len(line.lstrip(" "))
        leading_whitespace_length = len(line) - len(line.lstrip(" \t"))
        if block_scalar_indent is not None:
            if not stripped or indentation > block_scalar_indent:
                continue
            block_scalar_indent = None

        code = strip_yaml_comment(line)
        if not code.strip():
            continue

        location = f"{document_name}:{line_number}"
        if "\t" in line[:leading_whitespace_length]:
            errors.append(f"{location}: tabs are not allowed in YAML indentation")

        if QUOTED_MAPPING_KEY.match(code):
            errors.append(
                f"{location}: quoted mapping keys are not allowed; use "
                "canonical block-style keys"
            )
        if EXPLICIT_MAPPING_KEY.match(code):
            errors.append(
                f"{location}: explicit YAML mapping keys are not allowed"
            )
        if TAGGED_MAPPING_KEY.match(code) or code.lstrip().startswith("%"):
            errors.append(f"{location}: YAML tags and directives are not allowed")
        if MERGE_MAPPING_KEY.match(code):
            errors.append(f"{location}: YAML merge keys are not allowed")

        masked = mask_quoted_scalars(code)
        masked = GITHUB_EXPRESSION.sub("", masked)
        if YAML_ANCHOR_OR_ALIAS.search(masked):
            errors.append(f"{location}: YAML anchors and aliases are not allowed")
        if (
            any(character in masked for character in "{}[]")
            and CANONICAL_EMPTY_COLLECTION.match(code) is None
        ):
            errors.append(
                f"{location}: flow-style YAML collections are not allowed; "
                "use canonical block style"
            )

        if BLOCK_SCALAR_HEADER.search(code):
            block_scalar_indent = indentation

        if USES_PREFIX.match(line) is None:
            continue

        match = USES_LINE.match(line)
        if match is None:
            errors.append(f"{location}: malformed Action reference")
            continue

        target = match.group("target")
        if target.startswith("./"):
            local_references.append((document_name, location, target))
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


def resolve_local_action_references(
    local_actions: Mapping[str, str],
    local_references: list[tuple[str, str, str]],
    errors: list[str],
) -> None:
    manifests_by_directory: dict[str, list[str]] = {}
    for manifest_name in sorted(local_actions):
        manifest_path = pathlib.PurePosixPath(manifest_name)
        if (
            manifest_path.name not in ("action.yml", "action.yaml")
            or manifest_path.parts[:2] != (".github", "actions")
            or len(manifest_path.parts) < 4
        ):
            errors.append(
                f"{manifest_name}: local Action manifests must be canonical "
                f"{LOCAL_ACTION_DIRECTORY}/<name>/action.yml paths"
            )
            continue
        directory = manifest_path.parent.as_posix()
        manifests_by_directory.setdefault(directory, []).append(manifest_name)

    graph: dict[str, set[str]] = {
        manifest_name: set() for manifest_name in sorted(local_actions)
    }
    for source_name, location, target in local_references:
        target_parts = target[2:].split("/") if target.startswith("./") else []
        if (
            LOCAL_ACTION_TARGET.fullmatch(target) is None
            or any(part in ("", ".", "..") for part in target_parts)
        ):
            errors.append(
                f"{location}: local Action must stay under "
                f"./{LOCAL_ACTION_DIRECTORY} with a canonical path"
            )
            continue

        target_directory = target[2:]
        candidates = manifests_by_directory.get(target_directory, [])
        if not candidates:
            errors.append(
                f"{location}: local Action {target} has no action.yml or "
                "action.yaml manifest"
            )
            continue
        if len(candidates) != 1:
            errors.append(
                f"{location}: local Action {target} must have exactly one "
                "manifest"
            )
            continue
        if source_name in graph:
            graph[source_name].add(candidates[0])

    state: dict[str, int] = {}
    stack: list[str] = []
    emitted_cycles: set[tuple[str, ...]] = set()

    def visit(node: str) -> None:
        node_state = state.get(node, 0)
        if node_state == 2:
            return
        if node_state == 1:
            cycle_start = stack.index(node)
            cycle = tuple(stack[cycle_start:] + [node])
            if cycle not in emitted_cycles:
                emitted_cycles.add(cycle)
                errors.append("local Action reference cycle: " + " -> ".join(cycle))
            return

        state[node] = 1
        stack.append(node)
        for dependency in sorted(graph.get(node, set())):
            visit(dependency)
        stack.pop()
        state[node] = 2

    for manifest_name in sorted(graph):
        visit(manifest_name)


def collect_action_pin_errors(
    workflows: Mapping[str, str],
    local_actions: Mapping[str, str] | None = None,
    *,
    require_all_approved: bool = True,
) -> list[str]:
    errors: list[str] = []
    seen_actions: set[str] = set()
    local_references: list[tuple[str, str, str]] = []
    action_documents = dict(local_actions or {})

    for document_name, document_text in sorted(
        {**dict(workflows), **action_documents}.items()
    ):
        collect_document_action_references(
            document_name,
            document_text,
            errors,
            seen_actions,
            local_references,
        )

    resolve_local_action_references(
        action_documents,
        local_references,
        errors,
    )

    if require_all_approved:
        for action in sorted(set(APPROVED_ACTIONS) - seen_actions):
            errors.append(f"approved Action is unused: {action}")

    return errors


def main() -> int:
    workflows = load_workflows()
    local_actions = load_local_actions()
    errors = collect_action_pin_errors(workflows, local_actions)
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    print(
        "Immutable Action pin validation passed for "
        f"{len(workflows)} workflows, {len(local_actions)} local manifests, "
        f"and {len(APPROVED_ACTIONS)} approved remote Actions."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
