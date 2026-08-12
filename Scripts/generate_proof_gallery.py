#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import os
import pathlib
import shutil
import subprocess
import tempfile

from create_proof_gallery_project import ROOT, create_project

EXPECTED_OUTPUTS = tuple(sorted((
    "cup-source.png",
    "cup-textured.png",
    "cup-topology.png",
    "sphere-source.png",
    "sphere-textured.png",
    "sphere-topology.png",
    "manifest.json",
)))

GENERATOR_RELATIVE_PATH = (
    "Scripts/Templates~/M21ProofGallery/Assets/"
    "FoldCanvasProofGalleryGenerator.cs"
)
TEST_RELATIVE_PATH = (
    "Scripts/Templates~/M21ProofGallery/Assets/"
    "FoldCanvasProofGalleryTests.cs"
)


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Regenerate FoldCanvas cup and sphere proof-gallery evidence."
    )
    parser.add_argument("--unity", required=True, type=pathlib.Path)
    parser.add_argument(
        "--output",
        type=pathlib.Path,
        default=ROOT / "Docs" / "Community" / "ProofGallery",
    )
    parser.add_argument("--revision")
    args = parser.parse_args()
    unity = args.unity.resolve()
    if not unity.is_file():
        raise FileNotFoundError(f"Unity executable is missing: {unity}")
    revision = args.revision or subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True
    ).strip()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m21-") as temporary:
        temporary_root = pathlib.Path(temporary)
        project = temporary_root / "Project"
        staging = temporary_root / "Evidence"
        staging.mkdir()
        log_path = temporary_root / "Editor.log"
        create_project(project, ROOT)
        environment = os.environ.copy()
        environment["FOLDCANVAS_PROOF_OUTPUT"] = str(staging)
        environment["FOLDCANVAS_PROOF_SOURCE_REVISION"] = revision
        environment["FOLDCANVAS_PROOF_GENERATOR_SHA256"] = sha256(
            ROOT / GENERATOR_RELATIVE_PATH
        )
        environment["FOLDCANVAS_PROOF_TEST_SHA256"] = sha256(
            ROOT / TEST_RELATIVE_PATH
        )
        environment["FOLDCANVAS_PROOF_RUNNER_SHA256"] = sha256(
            ROOT / "Scripts" / "generate_proof_gallery.py"
        )
        environment["FOLDCANVAS_PROOF_PROJECT_BUILDER_SHA256"] = sha256(
            ROOT / "Scripts" / "create_proof_gallery_project.py"
        )
        command = [
            str(unity),
            "-batchmode",
            "-quit",
            "-projectPath",
            str(project),
            "-executeMethod",
            "FoldCanvas.M21Proof.FoldCanvasProofGalleryGenerator.GenerateBatch",
            "-logFile",
            str(log_path),
        ]
        subprocess.run(command, check=True, env=environment, cwd=ROOT)
        actual = tuple(sorted(path.name for path in staging.iterdir() if path.is_file()))
        if actual != EXPECTED_OUTPUTS:
            raise ValueError(
                "Unity proof output set differs from the seven-file contract: "
                + ", ".join(actual)
            )
        for name in EXPECTED_OUTPUTS:
            source = staging / name
            if not source.is_file():
                raise FileNotFoundError(f"Unity omitted proof output: {name}")
            shutil.copy2(source, output / name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
