#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import pathlib
import shutil

ROOT = pathlib.Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "Scripts" / "Templates~" / "M21ProofGallery"
ALLOWED_TEMPLATE_FILES = (
    "Assets/FoldCanvas.M21.ProofGallery.asmdef",
    "Assets/FoldCanvasProofGalleryGenerator.cs",
    "Assets/FoldCanvasProofGalleryTests.cs",
)
ALLOWED_SOURCE_TARGETS = (
    "Assets/Source/FoldCanvasOneSidedUnlitTexture.shader",
    "Assets/Source/FoldCanvasTopologyWireframe.shader",
    "Assets/Source/cup-canvas.png",
    "Assets/Source/cup.foldcanvas.json",
    "Assets/Source/sphere-canvas.png",
    "Assets/Source/sphere.foldcanvas.json",
)
ALLOWED_PROJECT_FILES = tuple(
    sorted(
        ALLOWED_TEMPLATE_FILES
        + ALLOWED_SOURCE_TARGETS
        + ("Packages/manifest.json", "ProjectSettings/ProjectVersion.txt")
    )
)


def create_project(project: pathlib.Path, package_root: pathlib.Path) -> None:
    project = project.resolve()
    package_root = package_root.resolve()
    if project.exists():
        raise FileExistsError(f"proof-gallery project already exists: {project}")
    if not (package_root / "package.json").is_file():
        raise FileNotFoundError(f"package root is invalid: {package_root}")

    actual_template_files = tuple(
        sorted(path.relative_to(TEMPLATE).as_posix() for path in TEMPLATE.rglob("*") if path.is_file())
    )
    if actual_template_files != ALLOWED_TEMPLATE_FILES:
        raise ValueError(
            "M21 template contains unexpected files: "
            + ", ".join(actual_template_files)
        )

    shutil.copytree(TEMPLATE, project)
    (project / "Packages").mkdir()
    (project / "ProjectSettings").mkdir()
    source = project / "Assets" / "Source"
    source.mkdir()

    manifest = {
        "dependencies": {
            "com.foldcanvas.core": (
                "file:"
                + pathlib.Path(
                    os.path.relpath(package_root, project / "Packages")
                ).as_posix()
            ),
            "com.unity.test-framework": "1.6.0",
        },
        "testables": ["com.foldcanvas.core"],
    }
    (project / "Packages" / "manifest.json").write_text(
        json.dumps(manifest, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    (project / "ProjectSettings" / "ProjectVersion.txt").write_text(
        "m_EditorVersion: 6000.3.20f1\n"
        "m_EditorVersionWithRevision: 6000.3.20f1 (c9ba695d4f07)\n",
        encoding="utf-8",
        newline="\n",
    )
    copies = {
        package_root
        / "Samples~"
        / "BootstrapPanel"
        / "m12-production-cup.foldcanvas.json": source
        / "cup.foldcanvas.json",
        package_root
        / "Samples~"
        / "BootstrapPanel"
        / "M04ProductionCupCanvas.png": source / "cup-canvas.png",
        package_root / "Samples~" / "Sphere" / "sphere-golden.foldcanvas.json": source
        / "sphere.foldcanvas.json",
        package_root / "Samples~" / "Sphere" / "sphere-canvas.png": source
        / "sphere-canvas.png",
        package_root
        / "Editor"
        / "Shaders"
        / "FoldCanvasOneSidedUnlitTexture.shader": source
        / "FoldCanvasOneSidedUnlitTexture.shader",
        package_root
        / "Editor"
        / "Shaders"
        / "FoldCanvasTopologyWireframe.shader": source
        / "FoldCanvasTopologyWireframe.shader",
    }
    for source_path, target_path in copies.items():
        if not source_path.is_file():
            raise FileNotFoundError(f"proof source is missing: {source_path}")
        shutil.copy2(source_path, target_path)

    actual_source_targets = tuple(
        sorted(path.relative_to(project).as_posix() for path in source.rglob("*") if path.is_file())
    )
    if actual_source_targets != ALLOWED_SOURCE_TARGETS:
        raise ValueError("M21 proof source allowlist changed")
    actual_project_files = tuple(
        sorted(path.relative_to(project).as_posix() for path in project.rglob("*") if path.is_file())
    )
    if actual_project_files != ALLOWED_PROJECT_FILES:
        raise ValueError("M21 clean host contains unexpected files")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Create a clean FoldCanvas proof-gallery Unity project."
    )
    parser.add_argument("--project", required=True, type=pathlib.Path)
    parser.add_argument("--package", default=ROOT, type=pathlib.Path)
    args = parser.parse_args()
    create_project(args.project, args.package)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
