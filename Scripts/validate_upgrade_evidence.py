#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from compare_upgrade_evidence import (  # noqa: E402
    load_object,
    validate_report,
)

SHA256_PATTERN = re.compile(r"[0-9a-f]{64}")


def require_file(path: pathlib.Path) -> None:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"required upgrade evidence is missing or empty: {path}")


def validate(args: argparse.Namespace) -> dict:
    for path in (
        args.report,
        args.manifest,
        args.lock,
        args.test_results,
        args.editor_log,
    ):
        require_file(path)

    report = load_object(args.report, "M15 upgrade report")
    validate_report(report, args.phase)
    if report.get("packageVersion") != args.package_version:
        raise ValueError("M15 upgrade report package version is stale")
    if report.get("packageSha256") != args.package_sha256:
        raise ValueError("M15 upgrade report package digest is stale")

    manifest = load_object(args.manifest, "M15 upgrade manifest")
    dependency = manifest.get("dependencies", {}).get("com.foldcanvas.core")
    if (
        not isinstance(dependency, str)
        or not dependency.startswith("file:")
        or not dependency.endswith(".tgz")
        or dependency == "file:../../"
        or "com.foldcanvas.core" not in dependency
    ):
        raise ValueError("M15 upgrade manifest did not reference one archive")

    lock = load_object(args.lock, "M15 upgrade package lock")
    locked = lock.get("dependencies", {}).get("com.foldcanvas.core")
    if not isinstance(locked, dict):
        raise ValueError("M15 upgrade package lock lacks FoldCanvas")
    if (
        not isinstance(locked.get("version"), str)
        or not locked["version"].endswith(".tgz")
        or locked.get("source") != "local-tarball"
    ):
        raise ValueError("M15 upgrade package lock is not a local tarball")

    test_run = ET.parse(args.test_results).getroot()
    counts = {
        key: int(test_run.attrib.get(key, "0"))
        for key in ("total", "passed", "failed", "skipped", "inconclusive")
    }
    if (
        counts != {
            "total": 1,
            "passed": 1,
            "failed": 0,
            "skipped": 0,
            "inconclusive": 0,
        }
        or test_run.attrib.get("result") != "Passed"
    ):
        raise ValueError(
            f"M15 {args.phase} Unity evidence is not one complete pass: "
            + " ".join(f"{key}={value}" for key, value in counts.items())
        )

    editor_log = args.editor_log.read_text(
        encoding="utf-8",
        errors="replace",
    )
    if "6000.3.20f1" not in editor_log:
        raise ValueError("M15 upgrade Editor.log does not prove Unity started")
    if "Saving results to:" not in editor_log or "Test run completed." not in editor_log:
        raise ValueError(
            "M15 upgrade Editor.log does not prove the test run completed"
        )

    return {
        "format": "foldcanvas-m15-source-upgrade-validation",
        "version": "1",
        "phase": args.phase,
        "packageVersion": args.package_version,
        "packageSha256": args.package_sha256,
        "tests": counts,
        "sourceRawSha256": report["sourceRawSha256"],
        "geometrySha256": report["geometrySha256"],
        "validationSha256": report["validationSha256"],
        "isSingleClosedVolume": report["isSingleClosedVolume"],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate one real Unity phase of the M15 upgrade proof."
    )
    parser.add_argument("--phase", required=True, choices=("before", "after"))
    parser.add_argument("--report", required=True, type=pathlib.Path)
    parser.add_argument("--manifest", required=True, type=pathlib.Path)
    parser.add_argument("--lock", required=True, type=pathlib.Path)
    parser.add_argument("--test-results", required=True, type=pathlib.Path)
    parser.add_argument("--editor-log", required=True, type=pathlib.Path)
    parser.add_argument("--package-version", required=True)
    parser.add_argument("--package-sha256", required=True)
    args = parser.parse_args()
    if SHA256_PATTERN.fullmatch(args.package_sha256) is None:
        raise ValueError("--package-sha256 must be one lowercase SHA-256 digest")
    print(json.dumps(validate(args), sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
