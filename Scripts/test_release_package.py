#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import pathlib
import sys
import tarfile
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from build_release_package import build_release_bundle, package_version  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[1]


def main() -> int:
    version = package_version()
    with tempfile.TemporaryDirectory(prefix="foldcanvas-release-a-") as first_dir:
        with tempfile.TemporaryDirectory(
            prefix="foldcanvas-release-b-"
        ) as second_dir:
            first, first_manifest, first_evidence = build_release_bundle(
                pathlib.Path(first_dir),
                f"v{version}",
            )
            second, second_manifest, second_evidence = build_release_bundle(
                pathlib.Path(second_dir),
                f"v{version}",
            )
            first_bytes = first.read_bytes()
            second_bytes = second.read_bytes()
            if first_bytes != second_bytes:
                raise AssertionError("Release archives are not byte-identical")
            if first_manifest.read_bytes() != second_manifest.read_bytes():
                raise AssertionError("Release file manifests are not byte-identical")
            if first_evidence.read_bytes() != second_evidence.read_bytes():
                raise AssertionError("Release evidence is not byte-identical")

            with tarfile.open(first, mode="r:gz") as archive:
                names = archive.getnames()
            if names != sorted(names):
                raise AssertionError("Release archive entries are not sorted")
            if len(names) != len(set(names)):
                raise AssertionError("Release archive contains duplicate entries")
            required = {
                "package/package.json",
                "package/Runtime/FoldCanvas.Runtime.asmdef",
                "package/Editor/FoldCanvas.Editor.asmdef",
                "package/Samples~/Gallery/gallery.json",
                "package/Samples~/OperationExtension/README.md",
                "package/Documentation~/index.md",
                "package/Documentation~/ProofGallery/cup-source.png",
                "package/Documentation~/ProofGallery/cup-textured.png",
                "package/Documentation~/ProofGallery/cup-topology.png",
                "package/Documentation~/ProofGallery/sphere-source.png",
                "package/Documentation~/ProofGallery/sphere-textured.png",
                "package/Documentation~/ProofGallery/sphere-topology.png",
                "package/Documentation~/m14-release-candidate.json",
                "package/Documentation~/m15-public-distribution.json",
                "package/Documentation~/m17-stable-readiness-report.json",
                "package/Documentation~/m17-stable-release.json",
                "package/Documentation~/m23-patch-release.json",
                "package/Documentation~/m25-minor-release.json",
                "package/Documentation~/minor-release.md",
                "package/Documentation~/patch-release.md",
                "package/Documentation~/release-candidate.md",
                "package/Documentation~/stable-release.md",
                "package/LICENSE.md",
                "package/SECURITY.md",
                "package/SUPPORT.md",
                "package/CONTRIBUTING.md",
                "package/CODE_OF_CONDUCT.md",
            }
            missing = sorted(required.difference(names))
            if missing:
                raise AssertionError(f"Release archive is missing: {missing}")

            forbidden_parts = {
                ".git",
                ".github",
                "Project~",
                "Library",
                "Logs",
                "artifacts",
                "Codex",
                "Docs",
            }
            for name in names:
                parts = set(pathlib.PurePosixPath(name).parts)
                overlap = sorted(parts.intersection(forbidden_parts))
                if overlap:
                    raise AssertionError(
                        f"Release archive contains forbidden path {name}: {overlap}"
                    )

            digest = hashlib.sha256(first_bytes).hexdigest()
            digest_line = first.with_suffix(first.suffix + ".sha256").read_text(
                encoding="utf-8"
            )
            if digest not in digest_line:
                raise AssertionError("Release digest file does not match archive")

            manifest = json.loads(
                first_manifest.read_text(encoding="utf-8")
            )
            if manifest.get("archiveSha256") != digest:
                raise AssertionError("Release file manifest archive hash is stale")
            manifest_paths = [entry.get("path") for entry in manifest.get("files", [])]
            if manifest_paths != names:
                raise AssertionError("Release file manifest does not match archive entries")
            if manifest_paths != sorted(manifest_paths):
                raise AssertionError("Release file manifest entries are not sorted")
            if manifest.get("fileCount") != len(names):
                raise AssertionError("Release file manifest count is inconsistent")
            with tarfile.open(first, mode="r:gz") as archive:
                for entry in manifest.get("files", []):
                    name = entry.get("path")
                    member = archive.getmember(name)
                    extracted = archive.extractfile(member)
                    if extracted is None:
                        raise AssertionError(
                            f"Release manifest path is not a file: {name}"
                        )
                    data = extracted.read()
                    if entry.get("size") != len(data):
                        raise AssertionError(
                            f"Release manifest size is stale: {name}"
                        )
                    if entry.get("sha256") != hashlib.sha256(data).hexdigest():
                        raise AssertionError(
                            f"Release manifest hash is stale: {name}"
                        )

            evidence = json.loads(
                first_evidence.read_text(encoding="utf-8")
            )
            is_minor = version == "1.1.0"
            expected_format = (
                "foldcanvas-minor-release-evidence"
                if is_minor
                else "foldcanvas-patch-release-evidence"
            )
            contract_path = (
                ROOT / "Documentation~" / "m25-minor-release.json"
                if is_minor
                else ROOT / "Documentation~" / "m23-patch-release.json"
            )
            contract = json.loads(contract_path.read_text(encoding="utf-8"))
            if (
                evidence.get("format") != expected_format
                or evidence.get("state") != "built-unverified"
                or evidence.get("packageVersion") != version
                or evidence.get("stableRelease") is not True
                or evidence.get("patchRelease") is not False
                or evidence.get("minorRelease") is not is_minor
                or evidence.get("archive", {}).get("sha256") != digest
                or evidence.get("fileManifest", {}).get("file")
                != first_manifest.name
                or evidence.get("fileManifest", {}).get("sha256")
                != hashlib.sha256(first_manifest.read_bytes()).hexdigest()
                or evidence.get("publication", {}).get("githubPrerelease")
                is not False
                or evidence.get("publication", {}).get("stableMinorRelease")
                is not True
                or evidence.get("publication", {}).get("externalMarketplace")
                is not False
                or evidence.get("contract", {}).get("path")
                != "Documentation~/m25-minor-release.json"
                or evidence.get("stableBaseline")
                != contract["stableBaseline"]
                or "exact-head-audit"
                not in evidence.get("requiredPrePublicationGates", [])
            ):
                raise AssertionError("Release evidence contract is invalid")

    print(f"Release package validation passed for {version}.")
    print(f"Deterministic SHA256 {digest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
