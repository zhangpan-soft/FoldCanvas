#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import pathlib
import sys
import tarfile
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from build_release_package import build_archive, package_version  # noqa: E402


def main() -> int:
    version = package_version()
    with tempfile.TemporaryDirectory(prefix="foldcanvas-release-a-") as first_dir:
        with tempfile.TemporaryDirectory(
            prefix="foldcanvas-release-b-"
        ) as second_dir:
            first = build_archive(pathlib.Path(first_dir), f"v{version}")
            second = build_archive(pathlib.Path(second_dir), f"v{version}")
            first_bytes = first.read_bytes()
            second_bytes = second.read_bytes()
            if first_bytes != second_bytes:
                raise AssertionError("Release archives are not byte-identical")

            with tarfile.open(first, mode="r:gz") as archive:
                names = archive.getnames()
            if names != sorted(names):
                raise AssertionError("Release archive entries are not sorted")
            required = {
                "package/package.json",
                "package/Runtime/FoldCanvas.Runtime.asmdef",
                "package/Editor/FoldCanvas.Editor.asmdef",
                "package/Samples~/Gallery/gallery.json",
                "package/Samples~/OperationExtension/README.md",
                "package/Documentation~/index.md",
                "package/LICENSE.md",
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

    print(f"Release package validation passed for {version}.")
    print(f"Deterministic SHA256 {digest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
