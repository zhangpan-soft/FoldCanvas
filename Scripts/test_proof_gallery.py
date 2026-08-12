#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import tempfile

import validate_proof_gallery as validator


def require_error(errors: list[str], text: str) -> None:
    if not any(text in error for error in errors):
        raise AssertionError(f"expected {text!r}, received {errors!r}")


def fixture(root: pathlib.Path, manifest: dict) -> pathlib.Path:
    required = {
        "package.json",
        *validator.TOOL_PATHS.values(),
        *(path for pair in validator.EXPECTED_SOURCES.values() for path in pair),
    }
    for relative in sorted(required):
        source = validator.ROOT / relative
        target = root / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(source.read_bytes())
    gallery = root / "Docs" / "Community" / "ProofGallery"
    gallery.mkdir(parents=True, exist_ok=True)
    for name in validator.EXPECTED_ARTIFACTS:
        (gallery / name).write_bytes((validator.GALLERY / name).read_bytes())
    path = gallery / "manifest.json"
    path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return path


def main() -> int:
    manifest = json.loads(validator.MANIFEST.read_text(encoding="utf-8"))
    cases = 0
    before = validator.snapshot_bytes()
    if validator.validate():
        raise AssertionError("real proof gallery must validate")
    if before != validator.snapshot_bytes():
        raise AssertionError("proof validation mutated repository inputs")
    cases += 1
    with tempfile.TemporaryDirectory(prefix="foldcanvas-m21-tests-") as temporary:
        base = pathlib.Path(temporary)

        stale = copy.deepcopy(manifest)
        stale["artifacts"][0]["sha256"] = "0" * 64
        path = fixture(base / "stale", stale)
        require_error(validator.validate(base / "stale", path), "SHA-256 is stale")
        cases += 1

        remote = copy.deepcopy(manifest)
        remote["sources"][0]["sourceCanvasPath"] = "https://example.invalid/a.png"
        path = fixture(base / "remote", remote)
        require_error(validator.validate(base / "remote", path), "repository-relative")
        cases += 1

        reordered = copy.deepcopy(manifest)
        reordered["sources"].reverse()
        path = fixture(base / "order", reordered)
        require_error(validator.validate(base / "order", path), "source order")
        cases += 1

        weakened = copy.deepcopy(manifest)
        weakened["geometry"][1]["values"]["openEdgeCount"] = "1"
        path = fixture(base / "weakened", weakened)
        require_error(validator.validate(base / "weakened", path), "openEdgeCount")
        cases += 1

        tool = copy.deepcopy(manifest)
        tool["generatorSha256"] = "f" * 64
        path = fixture(base / "tool", tool)
        require_error(validator.validate(base / "tool", path), "does not match")
        cases += 1

        identical = copy.deepcopy(manifest)
        root = base / "identical"
        path = fixture(root, identical)
        pixels = (path.parent / "cup-source.png").read_bytes()
        (path.parent / "sphere-source.png").write_bytes(pixels)
        identical["artifacts"][3]["sha256"] = validator.sha256_bytes(pixels)
        path.write_text(json.dumps(identical, indent=2) + "\n", encoding="utf-8")
        require_error(validator.validate(root, path), "byte-distinct")
        cases += 1

        truncated = copy.deepcopy(manifest)
        root = base / "truncated"
        path = fixture(root, truncated)
        image = path.parent / "cup-source.png"
        image.write_bytes(image.read_bytes()[:-12])
        truncated["artifacts"][0]["sha256"] = validator.sha256_file(image)
        path.write_text(json.dumps(truncated, indent=2) + "\n", encoding="utf-8")
        require_error(validator.validate(root, path), "missing required PNG chunks")
        cases += 1

        first = validator.validate()
        if first != validator.validate():
            raise AssertionError("validation result must be deterministic")
        cases += 1

    print(f"FoldCanvas proof gallery validator tests passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
