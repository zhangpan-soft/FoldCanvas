#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import pathlib
import sys

import validate_proof_gallery as proof

ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTRACT_FIELDS = (
    "format",
    "version",
    "unityVersion",
    "foldScriptVersion",
    "generator",
    "generatorSha256",
    "runnerSha256",
    "testSha256",
    "projectBuilderSha256",
    "command",
    "sources",
    "geometry",
)


def compare(
    first_manifest: pathlib.Path,
    second_manifest: pathlib.Path,
    root: pathlib.Path = ROOT,
) -> list[str]:
    errors: list[str] = []
    tracked_path = root / "Docs/Community/ProofGallery/manifest.json"
    try:
        tracked = json.loads(tracked_path.read_text(encoding="utf-8"))
        first = json.loads(first_manifest.read_text(encoding="utf-8"))
        second = json.loads(second_manifest.read_text(encoding="utf-8"))
        current_version = json.loads(
            (root / "package.json").read_text(encoding="utf-8")
        )["version"]
    except (OSError, UnicodeError, json.JSONDecodeError, KeyError) as exc:
        return [f"proof comparison input cannot be read: {exc}"]

    errors.extend(
        "first regeneration: " + error
        for error in proof.validate(
            root,
            first_manifest,
            require_readme=False,
            expected_package_version=current_version,
        )
    )
    errors.extend(
        "second regeneration: " + error
        for error in proof.validate(
            root,
            second_manifest,
            require_readme=False,
            expected_package_version=current_version,
        )
    )
    errors.extend(compare_outputs(first_manifest, second_manifest))
    if tracked.get("packageVersion") != "1.0.0":
        errors.append("tracked M21 evidence must remain anchored to 1.0.0")
    if first.get("packageVersion") != current_version:
        errors.append("first regenerated package version differs from current package")
    if second.get("packageVersion") != current_version:
        errors.append("second regenerated package version differs from current package")

    for field in CONTRACT_FIELDS:
        if first.get(field) != tracked.get(field):
            errors.append(f"first regenerated proof contract drifted: {field}")
        if second.get(field) != tracked.get(field):
            errors.append(f"second regenerated proof contract drifted: {field}")
    if first.get("sourceRevision") != second.get("sourceRevision"):
        errors.append("regenerated source revisions differ")

    tracked_artifacts = {
        item.get("path"): item.get("sha256")
        for item in tracked.get("artifacts", [])
        if isinstance(item, dict)
    }
    for name in sorted(proof.EXPECTED_ARTIFACTS):
        tracked_png = tracked_path.parent / name
        packaged_png = root / "Documentation~/ProofGallery" / name
        if not tracked_png.is_file() or not packaged_png.is_file():
            errors.append(f"frozen M21 proof copy is missing: {name}")
            continue
        if tracked_png.read_bytes() != packaged_png.read_bytes():
            errors.append(f"packaged proof copy drifted from frozen M21 bytes: {name}")
        if proof.sha256_file(tracked_png) != tracked_artifacts.get(name):
            errors.append(f"frozen M21 artifact hash drifted: {name}")

    return sorted(set(errors))


def compare_outputs(first_manifest: pathlib.Path, second_manifest: pathlib.Path) -> list[str]:
    errors: list[str] = []
    try:
        first = json.loads(first_manifest.read_text(encoding="utf-8"))
        second = json.loads(second_manifest.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return [f"proof comparison input cannot be read: {exc}"]
    if first != second:
        errors.append("independent regenerated artifact manifests differ")
    for name in sorted(proof.EXPECTED_ARTIFACTS):
        first_path = first_manifest.parent / name
        second_path = second_manifest.parent / name
        if not first_path.is_file() or not second_path.is_file():
            errors.append(f"independent regenerated proof is missing: {name}")
        elif first_path.read_bytes() != second_path.read_bytes():
            errors.append(f"independent regenerated proof pixels differ: {name}")
    return sorted(set(errors))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare two independent FoldCanvas Unity proof regenerations."
    )
    parser.add_argument("--first", required=True, type=pathlib.Path)
    parser.add_argument("--second", required=True, type=pathlib.Path)
    args = parser.parse_args()
    first = args.first.resolve()
    second = args.second.resolve()
    errors = compare(first, second)
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print(
        "FoldCanvas proof regenerations match: 2 hosts, 6 PNGs, "
        "2 sources, 2 geometry reports."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
