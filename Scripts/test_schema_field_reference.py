#!/usr/bin/env python3
"""Deterministic fixture tests for schema-to-field-reference coverage."""

from __future__ import annotations

import json
import pathlib
import shutil
import tempfile

from validate_schema_field_reference import (
    DEFAULT_FIELD_REFERENCE,
    DEFAULT_SCHEMA,
    collect_coverage_errors,
    collect_schema_fields,
    load_inputs,
)


def assert_exact_error(errors: list[str], expected: str) -> None:
    if errors != [expected]:
        raise AssertionError(f"expected {expected!r}, got {errors!r}")


def append_after_field(markdown: str, field: str, new_rows: str) -> str:
    lines = markdown.splitlines(keepends=True)
    prefix = f"| `{field}` |"
    matches = [index for index, line in enumerate(lines) if line.startswith(prefix)]
    if len(matches) != 1:
        raise AssertionError(
            f"fixture requires exactly one {field!r} field row, got {matches}"
        )
    lines.insert(matches[0] + 1, new_rows)
    return "".join(lines)


def remove_field(markdown: str, field: str) -> str:
    lines = markdown.splitlines(keepends=True)
    prefix = f"| `{field}` |"
    matches = [index for index, line in enumerate(lines) if line.startswith(prefix)]
    if len(matches) != 1:
        raise AssertionError(
            f"fixture requires exactly one {field!r} field row, got {matches}"
        )
    del lines[matches[0]]
    return "".join(lines)


def main() -> int:
    schema, markdown = load_inputs(DEFAULT_SCHEMA, DEFAULT_FIELD_REFERENCE)
    repository_errors = collect_coverage_errors(schema, markdown)
    if repository_errors:
        raise AssertionError(
            f"real repository coverage must pass: {repository_errors}"
        )
    schema_fields, operation_types, schema_errors = collect_schema_fields(schema)
    if schema_errors:
        raise AssertionError(f"real schema discovery must pass: {schema_errors}")
    if len(schema_fields) != 72:
        raise AssertionError(
            f"reviewed FoldScript 0.1 field count changed: {len(schema_fields)}"
        )
    if operation_types != {
        "fold",
        "rigidTransform",
        "roll",
        "solidify",
        "sphericalWrap",
        "stitch",
        "toroidalWrap",
    }:
        raise AssertionError(f"implemented operation set drifted: {operation_types}")
    for required_field in {
        "canvas.appearance",
        "panels[].rectangle.tessellation.uSegments",
        "panels[].disk.tessellation.radialSegments",
        "boundaryRef.span",
        "seams[].sampleCount",
        "operations[].enabled",
        "operations[].roll.radiusMode",
        "operations[].sphericalWrap.poleMode",
        "operations[].toroidalWrap.majorRadius",
        "operations[].stitch.seams",
        "operations[].solidify.thickness",
        "compile.maxGeneratedVertices",
    }:
        if required_field not in schema_fields:
            raise AssertionError(f"required field scope is not covered: {required_field}")

    with tempfile.TemporaryDirectory(
        prefix="foldcanvas-schema-reference-"
    ) as temporary_directory:
        root = pathlib.Path(temporary_directory)
        schema_path = root / "Schema" / "foldcanvas.schema.json"
        reference_path = (
            root / "Documentation~" / "foldscript-field-reference.md"
        )
        schema_path.parent.mkdir(parents=True)
        reference_path.parent.mkdir(parents=True)
        shutil.copyfile(DEFAULT_SCHEMA, schema_path)
        shutil.copyfile(DEFAULT_FIELD_REFERENCE, reference_path)

        fixture_schema, fixture_markdown = load_inputs(
            schema_path,
            reference_path,
        )
        original_schema_bytes = schema_path.read_bytes()
        original_reference_bytes = reference_path.read_bytes()

        missing_markdown = remove_field(fixture_markdown, "canvas.height")
        assert_exact_error(
            collect_coverage_errors(fixture_schema, missing_markdown),
            "missing field-reference row: canvas.height",
        )

        stale_markdown = append_after_field(
            fixture_markdown,
            "extensions",
            "| `retiredField` | string | no | Negative fixture only. |\n",
        )
        assert_exact_error(
            collect_coverage_errors(fixture_schema, stale_markdown),
            "stale field-reference row: retiredField",
        )

        duplicate_row = next(
            line + "\n"
            for line in fixture_markdown.splitlines()
            if line.startswith("| `schemaVersion` |")
        )
        duplicate_markdown = fixture_markdown.replace(
            duplicate_row,
            duplicate_row + duplicate_row,
            1,
        )
        duplicate_errors = collect_coverage_errors(
            fixture_schema,
            duplicate_markdown,
        )
        if len(duplicate_errors) != 1 or not duplicate_errors[0].startswith(
            "duplicate field-reference row: schemaVersion (lines "
        ):
            raise AssertionError(
                f"duplicate row was not reported deterministically: {duplicate_errors}"
            )
        if duplicate_errors != collect_coverage_errors(
            fixture_schema,
            duplicate_markdown,
        ):
            raise AssertionError("duplicate diagnostics changed across repeated runs")

        missing_schema_field = json.loads(json.dumps(fixture_schema))
        missing_schema_field["$defs"]["roll"]["properties"][
            "futureRadiusPolicy"
        ] = {"type": "string"}
        assert_exact_error(
            collect_coverage_errors(missing_schema_field, fixture_markdown),
            "missing field-reference row: operations[].roll.futureRadiusPolicy",
        )

        unknown_keyword_schema = json.loads(json.dumps(fixture_schema))
        unknown_keyword_schema["x-opaque-policy"] = True
        assert_exact_error(
            collect_coverage_errors(unknown_keyword_schema, fixture_markdown),
            "unreviewed schema structural keyword: #/x-opaque-policy",
        )

        two_stale_rows = append_after_field(
            fixture_markdown,
            "extensions",
            "| `zRetired` | string | no | Negative fixture only. |\n"
            "| `aRetired` | string | no | Negative fixture only. |\n",
        )
        sorted_errors = collect_coverage_errors(fixture_schema, two_stale_rows)
        if sorted_errors != sorted(sorted_errors) or sorted_errors != [
            "stale field-reference row: aRetired",
            "stale field-reference row: zRetired",
        ]:
            raise AssertionError(f"errors are not sorted: {sorted_errors}")

        if (
            schema_path.read_bytes() != original_schema_bytes
            or reference_path.read_bytes() != original_reference_bytes
        ):
            raise AssertionError("validator mutated its schema or documentation input")

    print("Schema field-reference validation tests passed: 7 cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
