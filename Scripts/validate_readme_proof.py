#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import pathlib
import re
import sys
import tempfile

import generate_social_preview as social
import validate_proof_gallery as proof

ROOT = pathlib.Path(__file__).resolve().parents[1]
README_PATHS = ("README.md", "README.zh-CN.md")
PROOF_IMAGES = tuple(sorted(proof.EXPECTED_ARTIFACTS))
PACKAGE_PROOF_DIRECTORY = pathlib.Path("Documentation~/ProofGallery")
PUBLIC_PROVENANCE_PREFIX = (
    "https://github.com/zhangpan-soft/FoldCanvas/blob/main/"
    "Docs/Community/ProofGallery/"
)
SOCIAL_RELATIVE = pathlib.Path(
    "Docs/Community/ProofGallery/social-preview.png"
)


def validate(root: pathlib.Path = ROOT) -> list[str]:
    errors: list[str] = []
    package_version = __import__("json").loads(
        (root / "package.json").read_text(encoding="utf-8")
    ).get("version")
    version_source = (root / "Runtime/Data/FoldCanvasVersion.cs").read_text(
        encoding="utf-8"
    )
    if package_version != "1.1.0" or 'Package = "1.1.0"' not in version_source:
        errors.append("Current package/compiler version must be exact 1.1.0")
    if "## [1.0.1] - 2026-08-13" not in (root / "CHANGELOG.md").read_text(
        encoding="utf-8"
    ):
        errors.append("CHANGELOG lacks newest M22 patch heading")

    for relative in README_PATHS:
        path = root / relative
        text = path.read_text(encoding="utf-8")
        hero_markers = (
            ("2D canvas + FoldScript -> deterministic 3D geometry", "Closed cup", "Eight-gore sphere")
            if relative == "README.md"
            else ("二维画布 + FoldScript -> 确定性三维几何", "闭合杯体", "八瓣球体")
        )
        for marker in hero_markers:
            if marker not in text:
                errors.append(f"{relative} lacks proof hero marker: {marker}")
        if text.find(hero_markers[0]) > text.find("## Why" if relative == "README.md" else "## 为什么"):
            errors.append(f"{relative} proof hero is not before project background")
        for name in PROOF_IMAGES:
            target = f"{PACKAGE_PROOF_DIRECTORY.as_posix()}/{name}"
            if text.count(target) != 2:
                errors.append(f"{relative} must link proof image exactly twice: {name}")
        if text.count(PUBLIC_PROVENANCE_PREFIX + "manifest.json") != 1:
            errors.append(f"{relative} must link the proof manifest exactly once")
        if text.count(PUBLIC_PROVENANCE_PREFIX + "README.md") != 1:
            errors.append(f"{relative} must link the reproduction guide exactly once")
        if text.count(PUBLIC_PROVENANCE_PREFIX + "social-preview.png") != 1:
            errors.append(f"{relative} must link the social candidate exactly once")
        for alt, target in re.findall(r"!\[([^\]]*)\]\(([^)]+)\)", text):
            if target.startswith(PACKAGE_PROOF_DIRECTORY.as_posix() + "/") and len(alt.strip()) < 12:
                errors.append(f"{relative} has non-meaningful proof alt text: {target}")
        for target in re.findall(r"\]\(([^)]+)\)", text):
            if target.startswith(("http://", "https://", "#")):
                continue
            clean = target.split("#", 1)[0]
            if clean and not (root / clean).is_file() and not (root / clean).is_dir():
                errors.append(f"{relative} contains missing local link: {target}")

    gallery_readme = (root / "Docs/Community/ProofGallery/README.md").read_text(
        encoding="utf-8"
    )
    for fragment in (
        "social-preview.png",
        "1280 x 640",
        "Scripts/generate_social_preview.py",
        "Published `v1.0.0` and RC2 assets remain immutable",
    ):
        if fragment not in gallery_readme:
            errors.append(f"proof guide lacks M22 provenance: {fragment}")

    proof_errors = proof.validate(root, root / "Docs/Community/ProofGallery/manifest.json")
    errors.extend(proof_errors)
    manifest = __import__("json").loads(
        (root / "Docs/Community/ProofGallery/manifest.json").read_text(
            encoding="utf-8"
        )
    )
    artifact_hashes = {
        item["path"]: item["sha256"] for item in manifest.get("artifacts", [])
    }
    for name in PROOF_IMAGES:
        source_path = root / "Docs/Community/ProofGallery" / name
        packaged_path = root / PACKAGE_PROOF_DIRECTORY / name
        if not packaged_path.is_file():
            errors.append(f"packaged README proof is missing: {name}")
            continue
        packaged_digest = hashlib.sha256(packaged_path.read_bytes()).hexdigest()
        if (
            not source_path.is_file()
            or packaged_path.read_bytes() != source_path.read_bytes()
            or packaged_digest != artifact_hashes.get(name)
        ):
            errors.append(f"packaged README proof differs from audited M21 bytes: {name}")
    social_path = root / SOCIAL_RELATIVE
    if not social_path.is_file():
        errors.append("social-preview.png is missing")
    else:
        decoded_errors: list[str] = []
        decoded = proof.png_pixels(
            social_path.read_bytes(), "social-preview.png", decoded_errors
        )
        errors.extend(decoded_errors)
        if decoded is not None and decoded[:2] != (social.WIDTH, social.HEIGHT):
            errors.append("social-preview.png must be exactly 1280 x 640")
        with tempfile.TemporaryDirectory(prefix="foldcanvas-m22-social-") as temporary:
            regenerated = pathlib.Path(temporary) / "social-preview.png"
            expected = social.generate(regenerated)
            if expected != social_path.read_bytes():
                errors.append("social-preview.png differs from deterministic compositor")
    return sorted(set(errors))


def main() -> int:
    errors = validate()
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    social_path = ROOT / SOCIAL_RELATIVE
    digest = hashlib.sha256(social_path.read_bytes()).hexdigest()
    print(f"FoldCanvas README proof validated: 2 READMEs, 6 proof PNGs, social SHA256 {digest}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
