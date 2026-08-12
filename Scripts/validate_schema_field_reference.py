#!/usr/bin/env python3
"""Validate FoldScript schema fields against the programming field reference."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import defaultdict
from collections.abc import Iterable, Mapping


ROOT = pathlib.Path(__file__).resolve().parents[1]
DEFAULT_SCHEMA = ROOT / "Schema" / "foldcanvas.schema.json"
DEFAULT_FIELD_REFERENCE = (
    ROOT / "Documentation~" / "foldscript-field-reference.md"
)

# These are the reviewed JSON Schema 2020-12 structural keywords used by the
# public FoldScript schema. Property names and $defs names are data, not schema
# keywords, and are handled by the scoped field collector below. Adding a new
# schema feature requires an explicit review here instead of being silently
# ignored by the documentation gate.
SCHEMA_ONLY_STRUCTURAL_KEYWORDS = frozenset(
    {
        "$defs",
        "$id",
        "$ref",
        "$schema",
        "additionalProperties",
        "allOf",
        "const",
        "default",
        "description",
        "else",
        "enum",
        "exclusiveMaximum",
        "exclusiveMinimum",
        "if",
        "items",
        "maxItems",
        "maxLength",
        "maximum",
        "minItems",
        "minLength",
        "minimum",
        "not",
        "oneOf",
        "pattern",
        "prefixItems",
        "properties",
        "required",
        "then",
        "title",
        "type",
    }
)

# FoldScript 0.1 retains this reviewed reusable property-map definition even
# though each operation currently expands the common fields explicitly. Its
# children are public property names, not custom JSON Schema keywords.
PROPERTY_MAP_DEFINITIONS = frozenset({"operationBaseProperties"})

FIELD_ROW = re.compile(r"^\s*\|\s*`(?P<field>[^`]+)`\s*\|")
HEADING = re.compile(r"^(?P<marks>#{2,3})\s+(?P<title>.+?)\s*$")
OPERATION_HEADING = re.compile(r"^6\.[0-9]+\s+`(?P<type>[^`]+)`")


def local_ref_name(reference: object) -> str | None:
    if not isinstance(reference, str) or not reference.startswith("#/$defs/"):
        return None
    name = reference.removeprefix("#/$defs/")
    return name if name and "/" not in name else None


def object_property_paths(schema: object) -> set[tuple[str, ...]]:
    """Return declared object-property paths without expanding $ref targets."""

    if not isinstance(schema, Mapping):
        return set()
    properties = schema.get("properties")
    if not isinstance(properties, Mapping):
        return set()

    paths: set[tuple[str, ...]] = set()
    for name in sorted(properties):
        if not isinstance(name, str):
            continue
        path = (name,)
        paths.add(path)
        for descendant in object_property_paths(properties[name]):
            paths.add(path + descendant)
    return paths


def collect_structural_keyword_errors(schema: object) -> list[str]:
    errors: list[str] = []

    def visit(value: object, context: str, location: str) -> None:
        if isinstance(value, list):
            for index, item in enumerate(value):
                visit(item, "schema", f"{location}[{index}]")
            return
        if not isinstance(value, Mapping):
            return

        if context in {"properties", "$defs"}:
            for name in sorted(value, key=str):
                child_location = f"{location}/{name}"
                child_context = (
                    "properties"
                    if context == "$defs" and name in PROPERTY_MAP_DEFINITIONS
                    else "schema"
                )
                visit(value[name], child_context, child_location)
            return

        for key in sorted(value, key=str):
            child_location = f"{location}/{key}"
            if key not in SCHEMA_ONLY_STRUCTURAL_KEYWORDS:
                errors.append(
                    "unreviewed schema structural keyword: " + child_location
                )
            child_context = key if key in {"properties", "$defs"} else "schema"
            visit(value[key], child_context, child_location)

    visit(schema, "schema", "#")
    return errors


def referenced_definitions(
    schema: Mapping[str, object], path: tuple[str, ...]
) -> list[str]:
    value: object = schema
    for component in path:
        if not isinstance(value, Mapping) or component not in value:
            return []
        value = value[component]
    if not isinstance(value, list):
        return []

    names: list[str] = []
    for item in value:
        if not isinstance(item, Mapping):
            continue
        name = local_ref_name(item.get("$ref"))
        if name is not None:
            names.append(name)
    return names


def definition(
    definitions: Mapping[str, object], name: str, errors: list[str]
) -> Mapping[str, object] | None:
    value = definitions.get(name)
    if not isinstance(value, Mapping):
        errors.append(f"missing or invalid schema definition: {name}")
        return None
    return value


def add_paths(
    target: set[str], prefix: str, paths: Iterable[tuple[str, ...]]
) -> None:
    for path in paths:
        target.add(prefix + "." + ".".join(path))


def collect_schema_fields(schema: object) -> tuple[set[str], set[str], list[str]]:
    """Return canonical fields, operation types, and stable schema errors."""

    errors = collect_structural_keyword_errors(schema)
    if not isinstance(schema, Mapping):
        return set(), set(), errors + ["FoldScript schema root must be an object"]

    definitions_value = schema.get("$defs")
    if not isinstance(definitions_value, Mapping):
        return set(), set(), errors + ["FoldScript schema must define $defs"]
    definitions: Mapping[str, object] = definitions_value

    fields: set[str] = set()
    root_properties = schema.get("properties")
    if not isinstance(root_properties, Mapping):
        errors.append("FoldScript schema root must declare properties")
    else:
        fields.update(str(name) for name in root_properties)

    canvas = definition(definitions, "canvas", errors)
    if canvas is not None:
        add_paths(fields, "canvas", object_property_paths(canvas))

    panel_names = referenced_definitions(
        schema,
        ("properties", "panels", "items", "oneOf"),
    )
    panel_paths: dict[str, set[tuple[str, ...]]] = {}
    for name in panel_names:
        panel = definition(definitions, name, errors)
        if panel is None:
            continue
        shape_schema = panel.get("properties", {}).get("shape", {})
        shape = shape_schema.get("const") if isinstance(shape_schema, Mapping) else None
        if not isinstance(shape, str) or not shape:
            errors.append(f"panel definition lacks a string shape const: {name}")
            continue
        if shape in panel_paths:
            errors.append(f"duplicate panel shape const: {shape}")
            continue
        panel_paths[shape] = object_property_paths(panel)
    if not panel_paths:
        errors.append("FoldScript schema must reference at least one panel definition")
    else:
        common_panel_paths = set.intersection(*panel_paths.values())
        add_paths(fields, "panels[]", common_panel_paths)
        for shape in sorted(panel_paths):
            add_paths(
                fields,
                f"panels[].{shape}",
                panel_paths[shape] - common_panel_paths,
            )

    boundary = definition(definitions, "boundaryRef", errors)
    if boundary is not None:
        add_paths(fields, "boundaryRef", object_property_paths(boundary))

    seam = definition(definitions, "seam", errors)
    if seam is not None:
        add_paths(fields, "seams[]", object_property_paths(seam))

    operation_names = referenced_definitions(
        schema,
        ("properties", "operations", "items", "oneOf"),
    )
    operation_paths: dict[str, set[tuple[str, ...]]] = {}
    for name in operation_names:
        operation = definition(definitions, name, errors)
        if operation is None:
            continue
        type_schema = operation.get("properties", {}).get("type", {})
        operation_type = (
            type_schema.get("const") if isinstance(type_schema, Mapping) else None
        )
        if not isinstance(operation_type, str) or not operation_type:
            errors.append(f"operation definition lacks a string type const: {name}")
            continue
        if operation_type in operation_paths:
            errors.append(f"duplicate operation type const: {operation_type}")
            continue
        operation_paths[operation_type] = object_property_paths(operation)
    operation_types = set(operation_paths)
    if not operation_paths:
        errors.append("FoldScript schema must reference at least one operation")
    else:
        common_operation_paths = set.intersection(*operation_paths.values())
        add_paths(fields, "operations[]", common_operation_paths)
        for operation_type in sorted(operation_paths):
            add_paths(
                fields,
                f"operations[].{operation_type}",
                operation_paths[operation_type] - common_operation_paths,
            )

    compile_settings = definition(definitions, "compileSettings", errors)
    if compile_settings is not None:
        add_paths(fields, "compile", object_property_paths(compile_settings))

    return fields, operation_types, sorted(set(errors))


def split_markdown_row(line: str) -> list[str]:
    stripped = line.strip()
    if not stripped.startswith("|") or not stripped.endswith("|"):
        return []
    return [cell.strip() for cell in stripped[1:-1].split("|")]


def documentation_scope(
    level: int,
    title: str,
    current_h2: str | None,
    operation_types: set[str],
) -> str | None:
    if level == 2:
        if title.startswith("2. "):
            return "top"
        if title.startswith("3. "):
            return "canvas"
        if title.startswith("5. "):
            return "boundary-seam"
        if title.startswith("6. "):
            return "operation-common"
        if title.startswith("7. "):
            return "compile"
        return None

    if current_h2 is not None and current_h2.startswith("4. "):
        if title.startswith("4.1 "):
            return "panel-common"
        if title.startswith("4.2 "):
            return "panel:rectangle"
        if title.startswith("4.3 "):
            return "panel:disk"
        return "unmapped-panel"

    if current_h2 is not None and current_h2.startswith("6. "):
        match = OPERATION_HEADING.match(title)
        if match is None:
            return "unmapped-operation"
        operation_type = match.group("type")
        if operation_type not in operation_types:
            return f"stale-operation:{operation_type}"
        return f"operation:{operation_type}"

    return None


def canonical_documentation_field(scope: str, field: str) -> str | None:
    if scope == "top":
        return field
    if scope == "canvas":
        return field if field.startswith("canvas.") else f"canvas.{field}"
    if scope == "panel-common":
        return field if field.startswith("panels[].") else f"panels[].{field}"
    if scope.startswith("panel:"):
        shape = scope.split(":", 1)[1]
        if field.startswith("panels[]."):
            return field
        return f"panels[].{shape}.tessellation.{field}"
    if scope == "boundary-seam":
        return field if field.startswith("seams[].") else f"boundaryRef.{field}"
    if scope == "operation-common":
        return field if field.startswith("operations[].") else f"operations[].{field}"
    if scope.startswith("operation:"):
        operation_type = scope.split(":", 1)[1]
        if field.startswith("operations[]."):
            return field
        return f"operations[].{operation_type}.{field}"
    if scope == "compile":
        return field if field.startswith("compile.") else f"compile.{field}"
    return None


def collect_documentation_fields(
    markdown: str, operation_types: set[str]
) -> tuple[dict[str, list[int]], list[str]]:
    occurrences: dict[str, list[int]] = defaultdict(list)
    errors: list[str] = []
    current_h2: str | None = None
    scope: str | None = None
    table_is_field_table = False
    expecting_separator = False

    for line_number, line in enumerate(markdown.splitlines(), start=1):
        heading = HEADING.match(line)
        if heading is not None:
            level = len(heading.group("marks"))
            title = heading.group("title")
            if level == 2:
                current_h2 = title
            scope = documentation_scope(level, title, current_h2, operation_types)
            table_is_field_table = False
            expecting_separator = False
            continue

        cells = split_markdown_row(line)
        if not cells:
            table_is_field_table = False
            expecting_separator = False
            continue

        if cells[0].casefold() == "field":
            table_is_field_table = True
            expecting_separator = True
            if scope is None or scope.startswith(("unmapped-", "stale-operation:")):
                errors.append(
                    f"field table at line {line_number} has unmapped scope: "
                    f"{scope or 'none'}"
                )
            continue
        if expecting_separator:
            expecting_separator = False
            continue
        if not table_is_field_table:
            continue

        field_match = FIELD_ROW.match(line)
        if field_match is None:
            errors.append(f"field table row at line {line_number} lacks a code field")
            continue
        if scope is None:
            continue
        canonical = canonical_documentation_field(
            scope,
            field_match.group("field").strip(),
        )
        if canonical is not None:
            occurrences[canonical].append(line_number)

    return dict(occurrences), errors


def collect_coverage_errors(schema: object, markdown: str) -> list[str]:
    schema_fields, operation_types, errors = collect_schema_fields(schema)
    documentation_fields, documentation_errors = collect_documentation_fields(
        markdown,
        operation_types,
    )
    errors.extend(documentation_errors)

    documented = set(documentation_fields)
    for field in sorted(schema_fields - documented):
        errors.append(f"missing field-reference row: {field}")
    for field in sorted(documented - schema_fields):
        errors.append(f"stale field-reference row: {field}")
    for field in sorted(documentation_fields):
        lines = documentation_fields[field]
        if len(lines) > 1:
            line_list = ", ".join(str(line) for line in lines)
            errors.append(
                f"duplicate field-reference row: {field} (lines {line_list})"
            )
    return sorted(errors)


def load_inputs(
    schema_path: pathlib.Path,
    field_reference_path: pathlib.Path,
) -> tuple[object, str]:
    schema = json.loads(schema_path.read_text(encoding="utf-8"))
    markdown = field_reference_path.read_text(encoding="utf-8")
    return schema, markdown


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate FoldScript schema-to-field-reference coverage."
    )
    parser.add_argument("--schema", type=pathlib.Path, default=DEFAULT_SCHEMA)
    parser.add_argument(
        "--field-reference",
        type=pathlib.Path,
        default=DEFAULT_FIELD_REFERENCE,
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        schema, markdown = load_inputs(args.schema, args.field_reference)
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        print(f"Schema field-reference validation failed: {exc}")
        return 1

    errors = collect_coverage_errors(schema, markdown)
    if errors:
        print("Schema field-reference validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    schema_fields, _, _ = collect_schema_fields(schema)
    print(
        "Schema field-reference validation passed: "
        f"{len(schema_fields)} scoped public fields."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
