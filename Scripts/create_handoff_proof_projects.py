#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import pathlib
import shutil
import sys

sys.dont_write_bytecode = True

ROOT = pathlib.Path(__file__).resolve().parents[1]
TEMPLATE_ROOT = ROOT / "Scripts" / "Templates~" / "M12Handoff"
SAMPLE_ROOT = ROOT / "Samples~" / "BootstrapPanel"

from create_clean_install_project import archive_metadata  # noqa: E402


def create_project(
    project: pathlib.Path,
    archive: pathlib.Path,
    role: str,
) -> dict:
    project = project.resolve()
    archive = archive.resolve()
    if role not in {"producer", "receiver"}:
        raise ValueError("role must be producer or receiver")
    if project.exists():
        raise FileExistsError(f"handoff proof project already exists: {project}")

    version, digest = archive_metadata(archive)
    template = TEMPLATE_ROOT / role.capitalize()
    if not template.is_dir():
        raise FileNotFoundError(f"M12 handoff template is missing: {template}")

    shutil.copytree(template, project)
    (project / "Packages").mkdir()
    (project / "ProjectSettings").mkdir()
    relative_archive = pathlib.Path(
        os.path.relpath(archive, project / "Packages")
    ).as_posix()
    dependency = f"file:{relative_archive}"
    manifest = {
        "dependencies": {
            "com.foldcanvas.core": dependency,
            "com.unity.test-framework": "1.6.0",
        }
    }
    (project / "Packages" / "manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    (project / "ProjectSettings" / "ProjectVersion.txt").write_text(
        "m_EditorVersion: 6000.3.20f1\n"
        "m_EditorVersionWithRevision: 6000.3.20f1 (c9ba695d4f07)\n",
        encoding="utf-8",
        newline="\n",
    )

    if role == "producer":
        fixture = project / "Assets" / "Fixture"
        fixture.mkdir()
        shutil.copy2(
            SAMPLE_ROOT / "m12-production-cup.foldcanvas.json",
            fixture / "m12-production-cup.foldcanvas.json",
        )
        shutil.copy2(
            SAMPLE_ROOT / "M04ProductionCupCanvas.png",
            fixture / "M04ProductionCupCanvas.png",
        )
    else:
        (project / "M12Input").mkdir()

    expected = {
        "format": "foldcanvas-m12-handoff-proof-input",
        "version": "1",
        "role": role,
        "packageName": "com.foldcanvas.core",
        "packageVersion": version,
        "packageArchive": archive.name,
        "packageSha256": digest,
        "manifestDependency": dependency,
    }
    (project / "M12Expected.json").write_text(
        json.dumps(expected, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return expected


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Create independent M12 handoff producer and receiver projects."
    )
    parser.add_argument("--producer", required=True, type=pathlib.Path)
    parser.add_argument("--receiver", required=True, type=pathlib.Path)
    parser.add_argument("--package", required=True, type=pathlib.Path)
    args = parser.parse_args()
    producer = create_project(args.producer, args.package, "producer")
    receiver = create_project(args.receiver, args.package, "receiver")
    print(json.dumps({"producer": producer, "receiver": receiver}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
