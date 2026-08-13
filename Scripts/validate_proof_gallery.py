#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import argparse
import json
import pathlib
import re
import struct
import sys
import zlib
from collections.abc import Mapping

ROOT = pathlib.Path(__file__).resolve().parents[1]
GALLERY = ROOT / "Docs" / "Community" / "ProofGallery"
MANIFEST = GALLERY / "manifest.json"
HASH_RE = re.compile(r"[0-9a-f]{64}\Z")
REVISION_RE = re.compile(r"[0-9a-f]{40}\Z")

TOP_LEVEL_FIELDS = (
    "format",
    "version",
    "sourceRevision",
    "unityVersion",
    "packageVersion",
    "foldScriptVersion",
    "generator",
    "generatorSha256",
    "runnerSha256",
    "testSha256",
    "projectBuilderSha256",
    "command",
    "sources",
    "artifacts",
    "geometry",
)
SOURCE_FIELDS = (
    "id",
    "foldScriptPath",
    "foldScriptSha256",
    "sourceCanvasPath",
    "sourceCanvasSha256",
)
ARTIFACT_FIELDS = ("path", "width", "height", "sha256")
GEOMETRY_FIELDS = ("id", "geometrySha256", "values")
EXPECTED_ARTIFACTS = {
    "cup-source.png": (960, 540),
    "cup-textured.png": (720, 720),
    "cup-topology.png": (720, 720),
    "sphere-source.png": (960, 540),
    "sphere-textured.png": (720, 720),
    "sphere-topology.png": (720, 720),
}
EXPECTED_SOURCES = {
    "cup": (
        "Samples~/BootstrapPanel/m12-production-cup.foldcanvas.json",
        "Samples~/BootstrapPanel/M04ProductionCupCanvas.png",
    ),
    "sphere": (
        "Samples~/Sphere/sphere-golden.foldcanvas.json",
        "Samples~/Sphere/sphere-canvas.png",
    ),
}
EXPECTED_VALUES = {
    "cup": {
        "componentCount": "1",
        "isSingleClosedVolume": "true",
        "nonManifoldEdgeCount": "0",
        "openEdgeCount": "0",
        "orientationConflictEdgeCount": "0",
    },
    "sphere": {
        "eulerCharacteristic": "2",
        "inwardTriangleCount": "0",
        "isClosedSphere": "true",
        "nonManifoldEdgeCount": "0",
        "northPoleTopologyCount": "1",
        "openEdgeCount": "0",
        "orientationConflictEdgeCount": "0",
        "southPoleTopologyCount": "1",
        "sphericalPanelCount": "8",
    },
}
TOOL_PATHS = {
    "generatorSha256": (
        "Scripts/Templates~/M21ProofGallery/Assets/"
        "FoldCanvasProofGalleryGenerator.cs"
    ),
    "runnerSha256": "Scripts/generate_proof_gallery.py",
    "testSha256": (
        "Scripts/Templates~/M21ProofGallery/Assets/"
        "FoldCanvasProofGalleryTests.cs"
    ),
    "projectBuilderSha256": "Scripts/create_proof_gallery_project.py",
}


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: pathlib.Path) -> str:
    return sha256_bytes(path.read_bytes())


def fields(value: object, expected: tuple[str, ...], label: str, errors: list[str]) -> bool:
    if not isinstance(value, Mapping):
        errors.append(f"{label} must be an object")
        return False
    actual = tuple(value.keys())
    if actual != expected:
        errors.append(f"{label} fields/order must be {','.join(expected)}")
        return False
    return True


def safe_relative(value: object, label: str, errors: list[str]) -> pathlib.Path | None:
    if not isinstance(value, str) or not value:
        errors.append(f"{label} must be a non-empty relative path")
        return None
    if "\\" in value or ":" in value or value.startswith(("/", "~")):
        errors.append(f"{label} must be repository-relative")
        return None
    path = pathlib.PurePosixPath(value)
    if path.is_absolute() or ".." in path.parts or "." in path.parts:
        errors.append(f"{label} must be confined and normalized")
        return None
    return pathlib.Path(*path.parts)


def png_pixels(data: bytes, label: str, errors: list[str]) -> tuple[int, int, bytes] | None:
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        errors.append(f"{label} has an invalid PNG signature")
        return None
    offset = 8
    width = height = bit_depth = color_type = None
    compressed = bytearray()
    seen_iend = False
    while offset + 12 <= len(data):
        length = struct.unpack(">I", data[offset : offset + 4])[0]
        chunk_type = data[offset + 4 : offset + 8]
        start = offset + 8
        end = start + length
        if end + 4 > len(data):
            errors.append(f"{label} contains a truncated PNG chunk")
            return None
        payload = data[start:end]
        expected_crc = struct.unpack(">I", data[end : end + 4])[0]
        if zlib.crc32(chunk_type + payload) & 0xFFFFFFFF != expected_crc:
            errors.append(f"{label} contains a bad PNG chunk CRC")
            return None
        if chunk_type == b"IHDR":
            if len(payload) != 13:
                errors.append(f"{label} has an invalid IHDR")
                return None
            width, height, bit_depth, color_type, compression, filtering, interlace = (
                struct.unpack(">IIBBBBB", payload)
            )
            if (bit_depth, color_type, compression, filtering, interlace) != (8, 6, 0, 0, 0):
                errors.append(f"{label} must be non-interlaced 8-bit RGBA PNG")
                return None
        elif chunk_type == b"IDAT":
            compressed.extend(payload)
        elif chunk_type == b"IEND":
            seen_iend = True
            if end + 4 != len(data):
                errors.append(f"{label} contains bytes after IEND")
            break
        elif chunk_type not in {b"IHDR"}:
            errors.append(f"{label} contains unsupported PNG chunk {chunk_type!r}")
        offset = end + 4
    if not seen_iend or width is None or height is None:
        errors.append(f"{label} is missing required PNG chunks")
        return None
    try:
        filtered = zlib.decompress(bytes(compressed))
    except zlib.error:
        errors.append(f"{label} IDAT data cannot be decompressed")
        return None
    stride = width * 4
    if len(filtered) != height * (stride + 1):
        errors.append(f"{label} decompressed PNG size is invalid")
        return None
    rows: list[bytearray] = []
    prior = bytearray(stride)
    cursor = 0
    for _ in range(height):
        filter_type = filtered[cursor]
        cursor += 1
        source = filtered[cursor : cursor + stride]
        cursor += stride
        row = bytearray(stride)
        for index, byte in enumerate(source):
            left = row[index - 4] if index >= 4 else 0
            up = prior[index]
            up_left = prior[index - 4] if index >= 4 else 0
            if filter_type == 0:
                value = byte
            elif filter_type == 1:
                value = byte + left
            elif filter_type == 2:
                value = byte + up
            elif filter_type == 3:
                value = byte + ((left + up) // 2)
            elif filter_type == 4:
                prediction = left + up - up_left
                distances = (
                    abs(prediction - left),
                    abs(prediction - up),
                    abs(prediction - up_left),
                )
                predictor = (left, up, up_left)[distances.index(min(distances))]
                value = byte + predictor
            else:
                errors.append(f"{label} uses unknown PNG filter {filter_type}")
                return None
            row[index] = value & 0xFF
        rows.append(row)
        prior = row
    return width, height, b"".join(rows)


def validate(
    root: pathlib.Path = ROOT,
    manifest_path: pathlib.Path = MANIFEST,
    *,
    require_readme: bool = True,
    expected_package_version: str | None = None,
) -> list[str]:
    errors: list[str] = []
    try:
        raw_manifest = manifest_path.read_text(encoding="utf-8")
        manifest = json.loads(raw_manifest)
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return [f"proof manifest cannot be read: {exc}"]
    if raw_manifest != json.dumps(manifest, indent=2, ensure_ascii=True) + "\n":
        errors.append("proof manifest is not canonical sorted-free JSON")
    if not fields(manifest, TOP_LEVEL_FIELDS, "manifest", errors):
        return sorted(set(errors))
    if manifest["format"] != "foldcanvas-proof-gallery" or manifest["version"] != "1":
        errors.append("proof manifest format/version is unsupported")
    if not isinstance(manifest["sourceRevision"], str) or not REVISION_RE.fullmatch(
        manifest["sourceRevision"]
    ):
        errors.append("sourceRevision must be a lowercase 40-character commit SHA")
    if manifest["unityVersion"] != "6000.3.20f1":
        errors.append("proof Unity version must be 6000.3.20f1")
    package = json.loads((root / "package.json").read_text(encoding="utf-8"))
    package_version = package.get("version")
    if expected_package_version is None:
        tracked_manifest = root / "Docs" / "Community" / "ProofGallery" / "manifest.json"
        expected_package_version = (
            "1.0.0"
            if manifest_path.resolve() == tracked_manifest.resolve()
            else package_version
        )
    if manifest["packageVersion"] != expected_package_version:
        errors.append(
            "proof package version must be "
            f"{expected_package_version} for this evidence context"
        )
    if package_version not in {"1.0.0", "1.0.1"}:
        errors.append("proof consumer package version is unsupported")
    if manifest["foldScriptVersion"] != "0.1":
        errors.append("proof FoldScript version must be 0.1")
    if manifest["generator"] != (
        "FoldCanvas.M21Proof.FoldCanvasProofGalleryGenerator.GenerateBatch"
    ):
        errors.append("proof generator identity is invalid")
    if manifest["command"] != (
        "python3 Scripts/generate_proof_gallery.py --unity /path/to/Unity"
    ):
        errors.append("proof regeneration command is invalid")
    for field, relative in TOOL_PATHS.items():
        value = manifest[field]
        if not isinstance(value, str) or not HASH_RE.fullmatch(value):
            errors.append(f"{field} must be lowercase SHA-256")
            continue
        path = root / relative
        if not path.is_file() or sha256_file(path) != value:
            errors.append(f"{field} does not match {relative}")

    gallery_files = {
        path.name
        for path in manifest_path.parent.iterdir()
        if path.is_file() and path.suffix != ".meta"
    }
    expected_gallery_files = {"manifest.json", *EXPECTED_ARTIFACTS}
    if require_readme:
        expected_gallery_files.add("README.md")
        social_preview = manifest_path.parent / "social-preview.png"
        if social_preview.is_file():
            expected_gallery_files.add("social-preview.png")
    if gallery_files != expected_gallery_files:
        errors.append("proof gallery contains unexpected or missing files")

    sources = manifest["sources"]
    if not isinstance(sources, list) or [item.get("id") for item in sources if isinstance(item, dict)] != [
        "cup",
        "sphere",
    ]:
        errors.append("proof source order must be cup,sphere")
    else:
        for index, source in enumerate(sources):
            label = f"source[{index}]"
            if not fields(source, SOURCE_FIELDS, label, errors):
                continue
            source_id = source["id"]
            expected = EXPECTED_SOURCES[source_id]
            for field, expected_path, hash_field in (
                ("foldScriptPath", expected[0], "foldScriptSha256"),
                ("sourceCanvasPath", expected[1], "sourceCanvasSha256"),
            ):
                relative = safe_relative(source[field], f"{label}.{field}", errors)
                if source[field] != expected_path:
                    errors.append(f"{label}.{field} does not use maintained source")
                if relative is None:
                    continue
                path = root / relative
                if not path.is_file():
                    errors.append(f"{label}.{field} is missing")
                elif source[hash_field] != sha256_file(path):
                    errors.append(f"{label}.{hash_field} is stale")

    artifacts = manifest["artifacts"]
    artifact_hashes: list[str] = []
    if not isinstance(artifacts, list) or [item.get("path") for item in artifacts if isinstance(item, dict)] != sorted(
        EXPECTED_ARTIFACTS
    ):
        errors.append("proof artifact order or identity is invalid")
    else:
        for index, artifact in enumerate(artifacts):
            label = f"artifact[{index}]"
            if not fields(artifact, ARTIFACT_FIELDS, label, errors):
                continue
            relative = safe_relative(artifact["path"], f"{label}.path", errors)
            if relative is None or len(relative.parts) != 1:
                errors.append(f"{label}.path must be a direct gallery PNG")
                continue
            path = manifest_path.parent / relative
            expected_dimensions = EXPECTED_ARTIFACTS[artifact["path"]]
            if (artifact["width"], artifact["height"]) != expected_dimensions:
                errors.append(f"{label} dimensions differ from contract")
            if not path.is_file():
                errors.append(f"{label} PNG is missing")
                continue
            data = path.read_bytes()
            digest = sha256_bytes(data)
            artifact_hashes.append(digest)
            if artifact["sha256"] != digest:
                errors.append(f"{label} SHA-256 is stale")
            decoded = png_pixels(data, label, errors)
            if decoded is not None:
                width, height, pixels = decoded
                if (width, height) != expected_dimensions:
                    errors.append(f"{label} IHDR dimensions differ from contract")
                colors = {pixels[offset : offset + 4] for offset in range(0, len(pixels), 4)}
                minimum_colors = 2 if artifact["path"].endswith("-topology.png") else 16
                if len(colors) < minimum_colors:
                    errors.append(f"{label} has insufficient non-background pixel evidence")
        if len(set(artifact_hashes)) != len(EXPECTED_ARTIFACTS):
            errors.append("proof artifact images must be byte-distinct")

    geometry = manifest["geometry"]
    if not isinstance(geometry, list) or [item.get("id") for item in geometry if isinstance(item, dict)] != [
        "cup",
        "sphere",
    ]:
        errors.append("proof geometry order must be cup,sphere")
    else:
        for index, item in enumerate(geometry):
            label = f"geometry[{index}]"
            if not fields(item, GEOMETRY_FIELDS, label, errors):
                continue
            if not isinstance(item["geometrySha256"], str) or not HASH_RE.fullmatch(
                item["geometrySha256"]
            ):
                errors.append(f"{label}.geometrySha256 is invalid")
            values = item["values"]
            if not isinstance(values, dict) or tuple(values) != tuple(sorted(values)):
                errors.append(f"{label}.values must use ordinal key order")
                continue
            for key, expected in EXPECTED_VALUES[item["id"]].items():
                if values.get(key) != expected:
                    errors.append(f"{label}.{key} differs from maintained proof")
            if item["id"] == "cup":
                try:
                    volume = float(values.get("totalAbsoluteVolume", "nan"))
                except ValueError:
                    volume = float("nan")
                if not volume > 0:
                    errors.append("cup totalAbsoluteVolume must be positive")

    return sorted(set(errors))


def snapshot_bytes(root: pathlib.Path = ROOT) -> dict[str, bytes]:
    relative_paths = {
        "package.json",
        *TOOL_PATHS.values(),
        *(path for pair in EXPECTED_SOURCES.values() for path in pair),
    }
    relative_paths.update(
        f"Docs/Community/ProofGallery/{name}" for name in EXPECTED_ARTIFACTS
    )
    relative_paths.add("Docs/Community/ProofGallery/manifest.json")
    return {
        relative: (root / relative).read_bytes()
        for relative in sorted(relative_paths)
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate FoldCanvas proof-gallery evidence."
    )
    parser.add_argument("--manifest", type=pathlib.Path, default=MANIFEST)
    parser.add_argument("--allow-missing-readme", action="store_true")
    args = parser.parse_args()
    errors = validate(
        ROOT,
        args.manifest.resolve(),
        require_readme=not args.allow_missing_readme,
    )
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print("FoldCanvas proof gallery validated: 6 PNGs, 2 sources, 2 geometry reports.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
