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
TEMPLATE_ROOT = ROOT / "Scripts" / "Templates~" / "M11CleanHost"


def archive_metadata(archive_path: pathlib.Path) -> tuple[str, str]:
    if not archive_path.is_file() or archive_path.suffix != ".tgz":
        raise ValueError("--package must identify one existing .tgz archive")

    digest = hashlib.sha256(archive_path.read_bytes()).hexdigest()
    with tarfile.open(archive_path, mode="r:gz") as archive:
        try:
            package_member = archive.getmember("package/package.json")
        except KeyError as exception:
            raise ValueError(
                "release archive lacks package/package.json"
            ) from exception
        package_file = archive.extractfile(package_member)
        if package_file is None:
            raise ValueError("release package.json could not be read")
        package = json.loads(package_file.read().decode("utf-8"))

    if package.get("name") != "com.foldcanvas.core":
        raise ValueError("archive package name is not com.foldcanvas.core")
    version = package.get("version")
    if not isinstance(version, str) or not version:
        raise ValueError("archive package version is invalid")
    return version, digest


def create_project(
    project_path: pathlib.Path,
    archive_path: pathlib.Path,
    *,
    include_package_tests: bool = False,
    include_consumer_fixture: bool = True,
) -> dict:
    project_path = project_path.resolve()
    archive_path = archive_path.resolve()
    if project_path.exists():
        raise FileExistsError(
            f"clean host target already exists: {project_path}"
        )
    if not TEMPLATE_ROOT.is_dir():
        raise FileNotFoundError(f"clean host template is missing: {TEMPLATE_ROOT}")

    version, digest = archive_metadata(archive_path)
    project_path.mkdir(parents=True)
    if include_consumer_fixture:
        shutil.copytree(TEMPLATE_ROOT / "Assets", project_path / "Assets")
    else:
        (project_path / "Assets").mkdir()
    (project_path / "Packages").mkdir()
    (project_path / "ProjectSettings").mkdir()

    relative_archive = pathlib.Path(
        os.path.relpath(archive_path, project_path / "Packages")
    ).as_posix()
    dependency = f"file:{relative_archive}"
    if not dependency.endswith(".tgz"):
        raise ValueError("clean host dependency must reference the archive")

    manifest = {
        "dependencies": {
            "com.foldcanvas.core": dependency,
            "com.unity.test-framework": "1.6.0",
        }
    }
    if include_package_tests:
        manifest["testables"] = ["com.foldcanvas.core"]
    (project_path / "Packages" / "manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    (project_path / "ProjectSettings" / "ProjectVersion.txt").write_text(
        "m_EditorVersion: 6000.3.20f1\n"
        "m_EditorVersionWithRevision: 6000.3.20f1 (c9ba695d4f07)\n",
        encoding="utf-8",
        newline="\n",
    )

    expected = {
        "format": "foldcanvas-clean-install-input",
        "version": "1",
        "packageName": "com.foldcanvas.core",
        "packageVersion": version,
        "packageArchive": archive_path.name,
        "packageSha256": digest,
        "manifestDependency": dependency,
        "includesPackageTests": include_package_tests,
        "includesConsumerFixture": include_consumer_fixture,
    }
    (project_path / "M11Expected.json").write_text(
        json.dumps(expected, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return expected


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Create a clean Unity consumer host for one FoldCanvas release archive."
        )
    )
    parser.add_argument("--project", required=True, type=pathlib.Path)
    parser.add_argument("--package", required=True, type=pathlib.Path)
    parser.add_argument(
        "--include-package-tests",
        action="store_true",
        help="Expose the package Tests assembly through manifest.testables.",
    )
    parser.add_argument(
        "--without-consumer-fixture",
        action="store_true",
        help="Create an empty Assets folder instead of the consumer smoke test.",
    )
    args = parser.parse_args()
    expected = create_project(
        args.project,
        args.package,
        include_package_tests=args.include_package_tests,
        include_consumer_fixture=not args.without_consumer_fixture,
    )
    print(json.dumps(expected, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
