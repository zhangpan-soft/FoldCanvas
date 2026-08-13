#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import shutil
import tempfile
import json

import compare_proof_gallery_evidence as comparator


def require_error(errors: list[str], text: str) -> None:
    if not any(text in error for error in errors):
        raise AssertionError(f"expected {text!r}, received {errors!r}")


def copy_gallery(source: pathlib.Path, target: pathlib.Path) -> pathlib.Path:
    target.mkdir(parents=True)
    for name in sorted(comparator.proof.EXPECTED_ARTIFACTS):
        shutil.copy2(source / name, target / name)
    manifest_path = target / "manifest.json"
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8"))
    manifest["packageVersion"] = json.loads(
        (comparator.ROOT / "package.json").read_text(encoding="utf-8")
    )["version"]
    manifest_path.write_text(
        json.dumps(manifest, indent=2, ensure_ascii=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return manifest_path


def main() -> int:
    source = comparator.ROOT / "Docs/Community/ProofGallery"
    cases = 0
    with tempfile.TemporaryDirectory(
        prefix="foldcanvas-proof-compare-tests-"
    ) as temporary:
        base = pathlib.Path(temporary)
        first = copy_gallery(source, base / "first")
        second = copy_gallery(source, base / "second")
        if comparator.compare(first, second):
            raise AssertionError("identical regenerations must compare cleanly")
        cases += 1

        drifted = base / "second" / "cup-source.png"
        payload = bytearray(drifted.read_bytes())
        payload[-1] ^= 1
        drifted.write_bytes(payload)
        require_error(
            comparator.compare_outputs(first, second),
            "independent regenerated proof pixels differ",
        )
        cases += 1

        shutil.copy2(source / "cup-source.png", drifted)
        manifest = base / "second" / "manifest.json"
        text = manifest.read_text(encoding="utf-8").replace(
            '"openEdgeCount": "0"',
            '"openEdgeCount": "1"',
            1,
        )
        manifest.write_text(text, encoding="utf-8")
        require_error(comparator.compare(first, manifest), "geometry")
        cases += 1

        first_result = comparator.compare(first, manifest)
        if first_result != comparator.compare(first, manifest):
            raise AssertionError("proof comparison diagnostics must be deterministic")
        cases += 1

    print(f"FoldCanvas proof comparison tests passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
