#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import os
import pathlib
import shutil
import sys
import tarfile

sys.dont_write_bytecode = True

ROOT = pathlib.Path(__file__).resolve().parents[1]
TEMPLATE_ROOT = ROOT / "Scripts" / "Templates~" / "M15UpgradeHost"
CONTRACT_PATH = ROOT / "Documentation~" / "m15-public-distribution.json"
MARKER_NAME = ".foldcanvas-m15-upgrade-project.json"
EXPECTED_NAME = "M15Expected.json"
SOURCE_NAME = "m12-production-cup.foldcanvas.json"
APPEARANCE_NAME = "M04ProductionCupCanvas.png"
OWNED_GENERATED_PATHS = ("Library", "Logs", "Temp", "obj")


def sha256_file(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: pathlib.Path, value: dict) -> None:
    path.write_text(
        json.dumps(value, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def archive_metadata(archive_path: pathlib.Path) -> tuple[str, str]:
    archive_path = archive_path.absolute()
    if archive_path.is_symlink():
        raise ValueError("upgrade package archive cannot be a symlink")
    archive_path = archive_path.resolve()
    if not archive_path.is_file() or archive_path.suffix != ".tgz":
        raise ValueError("--package must identify one existing .tgz archive")

    digest = sha256_file(archive_path)
    with tarfile.open(archive_path, mode="r:gz") as archive:
        try:
            member = archive.getmember("package/package.json")
        except KeyError as exception:
            raise ValueError(
                "upgrade package lacks package/package.json"
            ) from exception
        if not member.isfile():
            raise ValueError("upgrade package.json is not a regular file")
        package_file = archive.extractfile(member)
        if package_file is None:
            raise ValueError("upgrade package.json could not be read")
        package = json.loads(package_file.read().decode("utf-8"))

    if package.get("name") != "com.foldcanvas.core":
        raise ValueError("upgrade archive package name is invalid")
    version = package.get("version")
    if not isinstance(version, str) or not version:
        raise ValueError("upgrade archive package version is invalid")
    return version, digest


def load_upgrade_contract() -> dict:
    contract = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
    upgrade = contract.get("upgrade")
    if not isinstance(upgrade, dict) or not isinstance(
        upgrade.get("fixture"), dict
    ):
        raise ValueError("M15 upgrade fixture contract is missing")
    return contract


def validate_fixture(
    source_path: pathlib.Path,
    appearance_path: pathlib.Path,
    contract: dict,
) -> dict:
    source_path = source_path.absolute()
    appearance_path = appearance_path.absolute()
    if source_path.is_symlink() or appearance_path.is_symlink():
        raise ValueError("upgrade source inputs cannot be symlinks")
    source_path = source_path.resolve()
    appearance_path = appearance_path.resolve()
    if source_path.name != SOURCE_NAME or not source_path.is_file():
        raise ValueError(f"upgrade source must be the exact {SOURCE_NAME} file")
    if appearance_path.name != APPEARANCE_NAME or not appearance_path.is_file():
        raise ValueError(
            f"upgrade appearance must be the exact {APPEARANCE_NAME} file"
        )

    fixture = contract["upgrade"]["fixture"]
    source_digest = sha256_file(source_path)
    appearance_digest = sha256_file(appearance_path)
    if source_digest != fixture.get("sourceRawSha256"):
        raise ValueError("upgrade FoldScript bytes do not match the contract")
    if appearance_digest != fixture.get("appearanceSha256"):
        raise ValueError("upgrade PNG bytes do not match the contract")
    return {
        "sourceRawSha256": source_digest,
        "canonicalSourceSha256": fixture.get("canonicalSourceSha256"),
        "appearanceSha256": appearance_digest,
    }


def manifest_dependency(
    project_path: pathlib.Path,
    archive_path: pathlib.Path,
) -> str:
    relative = pathlib.Path(
        os.path.relpath(archive_path, project_path / "Packages")
    ).as_posix()
    dependency = f"file:{relative}"
    if not dependency.endswith(".tgz"):
        raise ValueError("upgrade host dependency must reference a .tgz archive")
    return dependency


def create_project(
    project_path: pathlib.Path,
    archive_path: pathlib.Path,
    source_path: pathlib.Path,
    appearance_path: pathlib.Path,
) -> dict:
    project_path = project_path.resolve()
    archive_path = archive_path.absolute()
    if archive_path.is_symlink():
        raise ValueError("upgrade package archive cannot be a symlink")
    archive_path = archive_path.resolve()
    if project_path.exists():
        raise FileExistsError(
            f"upgrade proof project already exists: {project_path}"
        )
    if not TEMPLATE_ROOT.is_dir():
        raise FileNotFoundError(
            f"M15 upgrade template is missing: {TEMPLATE_ROOT}"
        )

    contract = load_upgrade_contract()
    source_identity = validate_fixture(source_path, appearance_path, contract)
    package_version, package_digest = archive_metadata(archive_path)
    supported = contract["upgrade"].get("fromPackageVersions", [])
    if package_version not in supported:
        raise ValueError(
            f"upgrade baseline version is unsupported: {package_version}"
        )

    project_path.mkdir(parents=True)
    shutil.copytree(TEMPLATE_ROOT / "Assets", project_path / "Assets")
    input_root = project_path / "M15Input"
    input_root.mkdir()
    shutil.copyfile(source_path, input_root / SOURCE_NAME)
    shutil.copyfile(appearance_path, input_root / APPEARANCE_NAME)
    (project_path / "Packages").mkdir()
    (project_path / "ProjectSettings").mkdir()

    dependency = manifest_dependency(project_path, archive_path)
    manifest = {
        "dependencies": {
            "com.foldcanvas.core": dependency,
            "com.unity.test-framework": "1.6.0",
        }
    }
    write_json(project_path / "Packages" / "manifest.json", manifest)
    (project_path / "ProjectSettings" / "ProjectVersion.txt").write_text(
        "m_EditorVersion: 6000.3.20f1\n"
        "m_EditorVersionWithRevision: 6000.3.20f1 (c9ba695d4f07)\n",
        encoding="utf-8",
        newline="\n",
    )

    expected = {
        "format": "foldcanvas-m15-source-upgrade-input",
        "version": "1",
        "phase": "before",
        "packageName": "com.foldcanvas.core",
        "packageVersion": package_version,
        "packageSha256": package_digest,
        "manifestDependency": dependency,
        **source_identity,
    }
    write_json(project_path / EXPECTED_NAME, expected)
    marker = {
        "format": "foldcanvas-m15-owned-upgrade-project",
        "version": "1",
        "projectPath": str(project_path),
        "inputDirectory": "M15Input",
        "evidenceDirectory": "M15Evidence",
        "ownedGeneratedPaths": list(OWNED_GENERATED_PATHS),
        "ownedGeneratedFiles": ["Packages/packages-lock.json"],
    }
    write_json(project_path / MARKER_NAME, marker)
    return expected


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Create a new Unity host for the M15 source-first upgrade proof."
        )
    )
    parser.add_argument("--project", required=True, type=pathlib.Path)
    parser.add_argument("--package", required=True, type=pathlib.Path)
    parser.add_argument("--source", required=True, type=pathlib.Path)
    parser.add_argument("--appearance", required=True, type=pathlib.Path)
    args = parser.parse_args()
    expected = create_project(
        args.project,
        args.package,
        args.source,
        args.appearance,
    )
    print(json.dumps(expected, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
