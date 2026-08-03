#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import pathlib
import sys
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from build_release_package import build_archive, package_version  # noqa: E402
from compare_clean_install_evidence import compare  # noqa: E402
from create_clean_install_project import create_project  # noqa: E402
from validate_clean_install_evidence import validate  # noqa: E402


def write_json(path: pathlib.Path, value: dict) -> None:
    path.write_text(
        json.dumps(value, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def main() -> int:
    version = package_version()
    with tempfile.TemporaryDirectory(prefix="foldcanvas-m11-static-") as temp:
        root = pathlib.Path(temp)
        archive = build_archive(root / "package", f"v{version}")
        project = root / "consumer"
        expected = create_project(project, archive)

        manifest = json.loads(
            (project / "Packages" / "manifest.json").read_text(encoding="utf-8")
        )
        dependency = manifest["dependencies"]["com.foldcanvas.core"]
        if not dependency.startswith("file:") or not dependency.endswith(".tgz"):
            raise AssertionError("clean host does not reference the release archive")
        if expected["packageVersion"] != version:
            raise AssertionError("clean host package version is inconsistent")
        if len(expected["packageSha256"]) != 64:
            raise AssertionError("clean host package digest is invalid")
        if not (
            project / "Assets" / "M11CleanInstallConsumerTests.cs"
        ).is_file():
            raise AssertionError("consumer-owned test fixture was not copied")
        consumer_asmdef = json.loads(
            (
                project
                / "Assets"
                / "FoldCanvas.M11.Consumer.Tests.asmdef"
            ).read_text(encoding="utf-8")
        )
        if consumer_asmdef.get("references") != ["FoldCanvas.Runtime"]:
            raise AssertionError(
                "consumer fixture must reference only FoldCanvas.Runtime"
            )
        if "testables" in manifest:
            raise AssertionError(
                "consumer clean host must not expose package-owned tests"
            )

        package_test_project = root / "package-tests"
        create_project(
            package_test_project,
            archive,
            include_package_tests=True,
            include_consumer_fixture=False,
        )
        package_test_manifest = json.loads(
            (package_test_project / "Packages" / "manifest.json").read_text(
                encoding="utf-8"
            )
        )
        if package_test_manifest.get("testables") != ["com.foldcanvas.core"]:
            raise AssertionError("package-test host does not expose package tests")
        if any((package_test_project / "Assets").iterdir()):
            raise AssertionError("package-test host unexpectedly copied consumer code")

        evidence = root / "evidence"
        evidence.mkdir()
        report_path = evidence / "consumer-report.json"
        manifest_path = evidence / "manifest.json"
        lock_path = evidence / "packages-lock.json"
        test_results_path = evidence / "test-results.xml"
        editor_log_path = evidence / "Editor.log"
        sha = expected["packageSha256"]
        write_json(
            report_path,
            {
                "format": "foldcanvas-clean-install-report",
                "version": "1",
                "packageName": "com.foldcanvas.core",
                "packageVersion": version,
                "packageSha256": sha,
                "unityVersion": "6000.3.20f1",
                "packageSource": "LocalTarball",
                "resolvedPackagePath": (
                    "/tmp/first-clean-project/Library/PackageCache/"
                    "com.foldcanvas.core@first"
                ),
                "sourceSha256": "0" * 64,
                "geometrySha256": "1" * 64,
                "objSha256": "2" * 64,
                "diagnosticSha256": "3" * 64,
                "renderVertices": 15,
                "topologyVertices": 15,
                "triangles": 16,
                "diagnostics": 0,
            },
        )
        write_json(manifest_path, manifest)
        write_json(
            lock_path,
            {
                "dependencies": {
                    "com.foldcanvas.core": {
                        "version": dependency,
                        "depth": 0,
                        "source": "local-tarball",
                        "dependencies": {},
                    }
                }
            },
        )
        test_results_path.write_text(
            '<test-run total="1" passed="1" failed="0" skipped="0" '
            'inconclusive="0" result="Passed" />\n',
            encoding="utf-8",
        )
        editor_log_path.write_text(
            "Unity Editor version: 6000.3.20f1 (c9ba695d4f07)\n"
            "Saving results to: test-results.xml\n"
            "Test run completed. Exiting with code 0 (Ok). Run completed.\n",
            encoding="utf-8",
        )
        validated = validate(
            argparse.Namespace(
                report=report_path,
                manifest=manifest_path,
                lock=lock_path,
                test_results=test_results_path,
                editor_log=editor_log_path,
                package_version=version,
                package_sha256=sha,
            )
        )
        if validated["tests"]["passed"] != 1:
            raise AssertionError("synthetic clean-install evidence did not validate")

        second_report_path = evidence / "consumer-report-second.json"
        second_report = json.loads(report_path.read_text(encoding="utf-8"))
        second_report["resolvedPackagePath"] = (
            "/tmp/second-clean-project/Library/PackageCache/"
            "com.foldcanvas.core@second"
        )
        first_report = json.loads(report_path.read_text(encoding="utf-8"))
        write_json(report_path, first_report)
        write_json(second_report_path, second_report)
        comparison = compare(report_path, second_report_path)
        if comparison["installations"] != 2:
            raise AssertionError("clean-install pair comparison did not run")

        second_report["geometrySha256"] = "4" * 64
        write_json(second_report_path, second_report)
        try:
            compare(report_path, second_report_path)
        except ValueError as exception:
            if "geometrySha256" not in str(exception):
                raise
        else:
            raise AssertionError(
                "clean-install comparison accepted changed geometry evidence"
            )

    print(f"M11 clean-install static validation passed for {version}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
