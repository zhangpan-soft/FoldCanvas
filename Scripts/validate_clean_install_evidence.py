#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True

SHA256_PATTERN = re.compile(r"[0-9a-f]{64}")


def load_json(path: pathlib.Path) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"expected JSON object: {path}")
    return value


def require_file(path: pathlib.Path) -> None:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"required evidence file is missing or empty: {path}")


def validate(args: argparse.Namespace) -> dict:
    evidence_paths = (
        args.report,
        args.manifest,
        args.lock,
        args.test_results,
        args.editor_log,
    )
    for path in evidence_paths:
        require_file(path)

    report = load_json(args.report)
    manifest = load_json(args.manifest)
    lock = load_json(args.lock)
    expected_report = {
        "format": "foldcanvas-clean-install-report",
        "version": "1",
        "packageName": "com.foldcanvas.core",
        "packageVersion": args.package_version,
        "packageSha256": args.package_sha256,
        "unityVersion": "6000.3.20f1",
        "packageSource": "LocalTarball",
        "renderVertices": 15,
        "topologyVertices": 15,
        "triangles": 16,
        "diagnostics": 0,
    }
    for key, expected in expected_report.items():
        if report.get(key) != expected:
            raise ValueError(
                f"consumer report {key!r} was {report.get(key)!r}; "
                f"expected {expected!r}"
            )

    for key in (
        "sourceSha256",
        "geometrySha256",
        "objSha256",
        "diagnosticSha256",
    ):
        value = report.get(key)
        if not isinstance(value, str) or SHA256_PATTERN.fullmatch(value) is None:
            raise ValueError(f"consumer report {key} is not a SHA-256 digest")

    dependency = manifest.get("dependencies", {}).get("com.foldcanvas.core")
    if (
        not isinstance(dependency, str)
        or not dependency.startswith("file:")
        or not dependency.endswith(".tgz")
    ):
        raise ValueError("clean host manifest did not reference the .tgz archive")
    if dependency == "file:../../" or "com.foldcanvas.core" not in dependency:
        raise ValueError("clean host manifest fell back to the repository package")

    locked = lock.get("dependencies", {}).get("com.foldcanvas.core")
    if not isinstance(locked, dict):
        raise ValueError("packages-lock lacks com.foldcanvas.core")
    locked_version = locked.get("version")
    if not isinstance(locked_version, str) or not locked_version.endswith(".tgz"):
        raise ValueError("packages-lock did not resolve the local archive")
    if locked.get("source") != "local-tarball":
        raise ValueError("packages-lock did not record a local-tarball source")

    resolved_path = report.get("resolvedPackagePath")
    if not isinstance(resolved_path, str) or not resolved_path:
        raise ValueError("consumer report lacks the resolved package path")
    normalized_resolved_path = resolved_path.replace("\\", "/")
    if "/Library/PackageCache/com.foldcanvas.core@" not in normalized_resolved_path:
        raise ValueError(
            "consumer report did not resolve FoldCanvas under host PackageCache"
        )

    test_run = ET.parse(args.test_results).getroot()
    total = int(test_run.attrib.get("total", "0"))
    passed = int(test_run.attrib.get("passed", "0"))
    failed = int(test_run.attrib.get("failed", "0"))
    skipped = int(test_run.attrib.get("skipped", "0"))
    inconclusive = int(test_run.attrib.get("inconclusive", "0"))
    if (
        total != 1
        or passed != 1
        or failed
        or skipped
        or inconclusive
        or test_run.attrib.get("result") != "Passed"
    ):
        raise ValueError(
            "clean install NUnit evidence is not a complete pass: "
            f"total={total} passed={passed} failed={failed} "
            f"skipped={skipped} inconclusive={inconclusive}"
        )

    editor_log = args.editor_log.read_text(encoding="utf-8", errors="replace")
    if "6000.3.20f1" not in editor_log:
        raise ValueError("Editor.log does not prove Unity 6000.3.20f1 started")
    if "Saving results to:" not in editor_log or "Test run completed." not in editor_log:
        raise ValueError("Editor.log does not prove the Unity test run completed")

    return {
        "format": "foldcanvas-clean-install-validation",
        "version": "1",
        "packageVersion": args.package_version,
        "packageSha256": args.package_sha256,
        "tests": {
            "total": total,
            "passed": passed,
            "failed": failed,
            "skipped": skipped,
            "inconclusive": inconclusive,
        },
        "geometrySha256": report["geometrySha256"],
        "objSha256": report["objSha256"],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate FoldCanvas M11 clean-install evidence."
    )
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
    result = validate(args)
    print(json.dumps(result, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
