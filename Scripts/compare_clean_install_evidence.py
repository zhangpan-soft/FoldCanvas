#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib


STABLE_FIELDS = (
    "format",
    "version",
    "packageName",
    "packageVersion",
    "packageSha256",
    "unityVersion",
    "packageSource",
    "sourceSha256",
    "geometrySha256",
    "objSha256",
    "diagnosticSha256",
    "renderVertices",
    "topologyVertices",
    "triangles",
    "diagnostics",
)


def load_report(path: pathlib.Path) -> dict:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"clean-install report is missing or empty: {path}")
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"clean-install report must be a JSON object: {path}")
    return value


def compare(first_path: pathlib.Path, second_path: pathlib.Path) -> dict:
    first = load_report(first_path)
    second = load_report(second_path)
    first_stable = {field: first.get(field) for field in STABLE_FIELDS}
    second_stable = {field: second.get(field) for field in STABLE_FIELDS}
    if first_stable != second_stable:
        differences = [
            field
            for field in STABLE_FIELDS
            if first_stable[field] != second_stable[field]
        ]
        raise ValueError(
            "independent clean installations produced different evidence: "
            + ", ".join(differences)
        )

    first_resolved = first.get("resolvedPackagePath")
    second_resolved = second.get("resolvedPackagePath")
    for label, resolved in (
        ("first", first_resolved),
        ("second", second_resolved),
    ):
        if not isinstance(resolved, str) or "/Library/PackageCache/" not in resolved:
            raise ValueError(
                f"{label} clean installation was not resolved from PackageCache"
            )
    if first_resolved == second_resolved:
        raise ValueError(
            "clean-install comparison requires two independent project paths"
        )

    canonical = json.dumps(
        first_stable,
        ensure_ascii=True,
        separators=(",", ":"),
        sort_keys=True,
    ).encode("utf-8")
    return {
        "format": "foldcanvas-clean-install-comparison",
        "version": "1",
        "installations": 2,
        "packageVersion": first_stable["packageVersion"],
        "packageSha256": first_stable["packageSha256"],
        "stableEvidenceSha256": hashlib.sha256(canonical).hexdigest(),
        "geometrySha256": first_stable["geometrySha256"],
        "objSha256": first_stable["objSha256"],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Compare two independent FoldCanvas clean-install reports while "
            "ignoring their intentionally different PackageCache paths."
        )
    )
    parser.add_argument("--first", required=True, type=pathlib.Path)
    parser.add_argument("--second", required=True, type=pathlib.Path)
    args = parser.parse_args()
    print(json.dumps(compare(args.first, args.second), sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
