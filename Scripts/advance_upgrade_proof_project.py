#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import pathlib
import shutil
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from create_upgrade_proof_project import (  # noqa: E402
    APPEARANCE_NAME,
    EXPECTED_NAME,
    MARKER_NAME,
    OWNED_GENERATED_PATHS,
    SOURCE_NAME,
    archive_metadata,
    load_upgrade_contract,
    manifest_dependency,
    sha256_file,
    validate_fixture,
    write_json,
)


def load_object(path: pathlib.Path, label: str) -> dict:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"{label} is missing or empty: {path}")
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object: {path}")
    return value


def assert_owned_project(project_path: pathlib.Path) -> dict:
    marker = load_object(
        project_path / MARKER_NAME,
        "M15 upgrade ownership marker",
    )
    expected_paths = list(OWNED_GENERATED_PATHS)
    if (
        marker.get("format") != "foldcanvas-m15-owned-upgrade-project"
        or marker.get("version") != "1"
        or marker.get("projectPath") != str(project_path)
        or marker.get("inputDirectory") != "M15Input"
        or marker.get("evidenceDirectory") != "M15Evidence"
        or marker.get("ownedGeneratedPaths") != expected_paths
        or marker.get("ownedGeneratedFiles")
        != ["Packages/packages-lock.json"]
    ):
        raise ValueError("M15 upgrade ownership marker is invalid")
    return marker


def assert_child(project_path: pathlib.Path, target: pathlib.Path) -> None:
    project_path = project_path.resolve()
    target = target.resolve(strict=False)
    if target == project_path or project_path not in target.parents:
        raise ValueError(f"refusing to mutate path outside upgrade host: {target}")


def remove_owned_tree(project_path: pathlib.Path, relative: str) -> None:
    target = project_path / relative
    assert_child(project_path, target)
    if target.is_symlink():
        raise ValueError(f"owned generated path cannot be a symlink: {target}")
    if target.exists():
        if not target.is_dir():
            raise ValueError(f"owned generated path is not a directory: {target}")
        shutil.rmtree(target)


def remove_owned_file(project_path: pathlib.Path, relative: str) -> None:
    target = project_path / relative
    assert_child(project_path, target)
    if target.is_symlink():
        raise ValueError(f"owned generated file cannot be a symlink: {target}")
    if target.exists():
        if not target.is_file():
            raise ValueError(f"owned generated file is not regular: {target}")
        target.unlink()


def advance_project(
    project_path: pathlib.Path,
    archive_path: pathlib.Path,
) -> dict:
    project_path = project_path.resolve()
    archive_path = archive_path.absolute()
    if archive_path.is_symlink():
        raise ValueError("upgrade target archive cannot be a symlink")
    archive_path = archive_path.resolve()
    if not project_path.is_dir():
        raise ValueError(f"upgrade project does not exist: {project_path}")
    assert_owned_project(project_path)

    expected_path = project_path / EXPECTED_NAME
    expected = load_object(expected_path, "M15 upgrade input")
    if (
        expected.get("format") != "foldcanvas-m15-source-upgrade-input"
        or expected.get("version") != "1"
        or expected.get("phase") != "before"
    ):
        raise ValueError("M15 upgrade host is not in the before phase")

    before_report = project_path / "M15Evidence" / "before.json"
    report = load_object(before_report, "M15 before-phase report")
    if (
        report.get("format") != "foldcanvas-m15-source-upgrade-proof"
        or report.get("version") != "1"
        or report.get("phase") != "before"
        or report.get("packageVersion") != expected.get("packageVersion")
        or report.get("packageSha256") != expected.get("packageSha256")
    ):
        raise ValueError("M15 before-phase report identity is invalid")

    contract = load_upgrade_contract()
    input_root = project_path / "M15Input"
    source_path = input_root / SOURCE_NAME
    appearance_path = input_root / APPEARANCE_NAME
    source_identity = validate_fixture(source_path, appearance_path, contract)
    for field, value in source_identity.items():
        if expected.get(field) != value:
            raise ValueError(f"upgrade source identity changed before advance: {field}")

    input_entries = sorted(path.name for path in input_root.iterdir())
    if input_entries != sorted((SOURCE_NAME, APPEARANCE_NAME)):
        raise ValueError("M15Input contains a non-source or missing input")
    if any(not path.is_file() or path.is_symlink() for path in input_root.iterdir()):
        raise ValueError("M15Input must contain only two regular source files")

    package_version, package_digest = archive_metadata(archive_path)
    target_version = (
        contract.get("packageVersion")
        if contract.get("stableRelease") is True
        else contract.get("candidateVersion")
    )
    if package_version != target_version:
        raise ValueError(
            "upgrade target archive does not match the contract target version"
        )
    if package_version == expected.get("packageVersion"):
        raise ValueError("upgrade target must differ from the baseline version")

    dependency = manifest_dependency(project_path, archive_path)
    manifest_path = project_path / "Packages" / "manifest.json"
    manifest = load_object(manifest_path, "upgrade host manifest")
    dependencies = manifest.get("dependencies")
    if not isinstance(dependencies, dict):
        raise ValueError("upgrade host manifest dependencies are invalid")
    dependencies["com.foldcanvas.core"] = dependency
    write_json(manifest_path, manifest)

    for relative in OWNED_GENERATED_PATHS:
        remove_owned_tree(project_path, relative)
    remove_owned_file(project_path, "Packages/packages-lock.json")

    updated = dict(expected)
    updated.update(
        {
            "phase": "after",
            "packageVersion": package_version,
            "packageSha256": package_digest,
            "manifestDependency": dependency,
            "previousPackageVersion": expected["packageVersion"],
            "previousPackageSha256": expected["packageSha256"],
        }
    )
    write_json(expected_path, updated)

    if sha256_file(source_path) != source_identity["sourceRawSha256"]:
        raise ValueError("upgrade FoldScript changed during package advance")
    if sha256_file(appearance_path) != source_identity["appearanceSha256"]:
        raise ValueError("upgrade appearance changed during package advance")
    if not before_report.is_file():
        raise ValueError("before-phase evidence was removed during advance")
    return updated


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Replace the package in an owned M15 upgrade host and remove only "
            "that host's derived Unity state."
        )
    )
    parser.add_argument("--project", required=True, type=pathlib.Path)
    parser.add_argument("--package", required=True, type=pathlib.Path)
    args = parser.parse_args()
    updated = advance_project(args.project, args.package)
    print(json.dumps(updated, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
