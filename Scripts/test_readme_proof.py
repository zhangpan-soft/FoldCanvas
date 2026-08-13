#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import shutil
import tempfile

import validate_readme_proof as validator


def require_error(errors: list[str], text: str) -> None:
    if not any(text in error for error in errors):
        raise AssertionError(f"expected {text!r}, received {errors!r}")


def fixture(target: pathlib.Path) -> None:
    paths = {
        "package.json",
        "CHANGELOG.md",
        "Runtime/Data/FoldCanvasVersion.cs",
        "README.md",
        "README.zh-CN.md",
        "Docs/Community/ProofGallery/README.md",
        "Docs/Community/ProofGallery/manifest.json",
        "Docs/Community/ProofGallery/social-preview.png",
        *validator.proof.TOOL_PATHS.values(),
        *(f"Docs/Community/ProofGallery/{name}" for name in validator.PROOF_IMAGES),
        *(f"Documentation~/ProofGallery/{name}" for name in validator.PROOF_IMAGES),
        *(path for pair in validator.proof.EXPECTED_SOURCES.values() for path in pair),
    }
    for relative in sorted(paths):
        source = validator.ROOT / relative
        destination = target / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(source.read_bytes())
    for readme_name in ("README.md", "README.zh-CN.md"):
        text = (target / readme_name).read_text(encoding="utf-8")
        for link in validator.re.findall(r"\]\(([^)]+)\)", text):
            clean = link.split("#", 1)[0]
            if not clean or clean.startswith(("http://", "https://")):
                continue
            destination = target / clean
            if destination.exists():
                continue
            source = validator.ROOT / clean
            if source.is_file():
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(source.read_bytes())
            elif source.is_dir():
                destination.mkdir(parents=True, exist_ok=True)


def main() -> int:
    if validator.validate():
        raise AssertionError("real M22 README proof must validate")
    cases = 1
    with tempfile.TemporaryDirectory(prefix="foldcanvas-m22-tests-") as temporary:
        base = pathlib.Path(temporary)

        missing = base / "missing"
        fixture(missing)
        (missing / "Docs/Community/ProofGallery/cup-topology.png").unlink()
        require_error(validator.validate(missing), "missing")
        cases += 1

        bad_alt = base / "alt"
        fixture(bad_alt)
        readme = bad_alt / "README.md"
        readme.write_text(
            readme.read_text(encoding="utf-8").replace(
                "Production cup 2D canvas containing the full wall rectangle and matching bottom disk",
                "cup",
            ),
            encoding="utf-8",
        )
        require_error(validator.validate(bad_alt), "non-meaningful")
        cases += 1

        stale = base / "stale"
        fixture(stale)
        social = stale / "Docs/Community/ProofGallery/social-preview.png"
        payload = bytearray(social.read_bytes())
        payload[-1] ^= 1
        social.write_bytes(payload)
        require_error(validator.validate(stale), "bad PNG chunk CRC")
        cases += 1

        packaged = base / "packaged"
        fixture(packaged)
        packaged_image = packaged / "Documentation~/ProofGallery/cup-source.png"
        packaged_image.write_bytes(packaged_image.read_bytes() + b"drift")
        require_error(validator.validate(packaged), "differs from audited M21 bytes")
        cases += 1

        version = base / "version"
        fixture(version)
        package = version / "package.json"
        package.write_text(
            package.read_text(encoding="utf-8").replace('"1.0.1"', '"1.0.0"'),
            encoding="utf-8",
        )
        require_error(validator.validate(version), "exact 1.0.1")
        cases += 1

        first = validator.validate()
        if first != validator.validate():
            raise AssertionError("M22 README validation must be deterministic")
        cases += 1

    print(f"FoldCanvas README proof validator tests passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
