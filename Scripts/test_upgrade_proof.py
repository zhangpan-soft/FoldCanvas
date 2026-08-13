#!/usr/bin/env python3
from __future__ import annotations

import argparse
import copy
import io
import json
import pathlib
import sys
import tarfile
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from advance_upgrade_proof_project import advance_project  # noqa: E402
from build_release_package import build_archive  # noqa: E402
from compare_upgrade_evidence import compare  # noqa: E402
from create_upgrade_proof_project import create_project  # noqa: E402
from validate_upgrade_evidence import validate  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTRACT_PATH = ROOT / "Documentation~" / "m25-minor-release.json"


def write_json(path: pathlib.Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(value, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def synthetic_package(path: pathlib.Path, version: str) -> pathlib.Path:
    package_json = json.dumps(
        {"name": "com.foldcanvas.core", "version": version},
        sort_keys=True,
    ).encode("utf-8")
    with tarfile.open(path, mode="w:gz") as archive:
        info = tarfile.TarInfo("package/package.json")
        info.size = len(package_json)
        info.mode = 0o644
        info.mtime = 0
        archive.addfile(info, io.BytesIO(package_json))
    return path


def proof_report(
    expected: dict,
    project: pathlib.Path,
    phase: str,
    resolved_suffix: str,
    contract: dict,
) -> dict:
    fixture = contract["upgrade"]["fixture"]
    return {
        "format": "foldcanvas-m15-source-upgrade-proof",
        "version": "1",
        "phase": phase,
        "packageName": "com.foldcanvas.core",
        "packageVersion": expected["packageVersion"],
        "packageSha256": expected["packageSha256"],
        "unityVersion": "6000.3.20f1",
        "packageSource": "LocalTarball",
        "resolvedPackagePath": str(
            project / "Library" / "PackageCache" / resolved_suffix
        ),
        "sourceInputFiles": fixture["inputFileNames"],
        "sourceInputCount": 2,
        "derivedInputCount": 0,
        "sourceRawSha256": fixture["sourceRawSha256"],
        "canonicalSourceSha256": fixture["canonicalSourceSha256"],
        "appearanceSha256": fixture["appearanceSha256"],
        "geometrySha256": "1" * 64,
        "objSha256": "2" * 64,
        "diagnosticSha256": "3" * 64,
        "validationSha256": "4" * 64,
        "renderVertices": 2972,
        "topologyVertices": 2562,
        "triangles": 5120,
        "diagnostics": 0,
        "openEdges": 0,
        "nonManifoldEdges": 0,
        "components": 1,
        "isClosedVolume": True,
        "isSingleClosedVolume": True,
    }


def expect_value_error(action, expected_text: str) -> None:
    try:
        action()
    except ValueError as exception:
        if expected_text not in str(exception):
            raise
    else:
        raise AssertionError(
            f"expected ValueError containing {expected_text!r}"
        )


def write_unity_evidence(root: pathlib.Path) -> tuple[pathlib.Path, pathlib.Path]:
    results = root / "test-results.xml"
    log = root / "Editor.log"
    results.parent.mkdir(parents=True, exist_ok=True)
    results.write_text(
        '<test-run total="1" passed="1" failed="0" skipped="0" '
        'inconclusive="0" result="Passed" />\n',
        encoding="utf-8",
        newline="\n",
    )
    log.write_text(
        "Unity Editor version: 6000.3.20f1 (c9ba695d4f07)\n"
        "Saving results to: test-results.xml\n"
        "Test run completed. Exiting with code 0 (Ok). Run completed.\n",
        encoding="utf-8",
        newline="\n",
    )
    return results, log


def write_package_lock(project: pathlib.Path) -> pathlib.Path:
    manifest = json.loads(
        (project / "Packages" / "manifest.json").read_text(encoding="utf-8")
    )
    dependency = manifest["dependencies"]["com.foldcanvas.core"]
    path = project / "Packages" / "packages-lock.json"
    write_json(
        path,
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
    return path


def main() -> int:
    contract = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
    fixture = contract["upgrade"]["fixture"]
    source = ROOT / fixture["sourcePath"]
    appearance = ROOT / fixture["appearancePath"]
    with tempfile.TemporaryDirectory(prefix="foldcanvas-m23-upgrade-") as temp:
        temp_root = pathlib.Path(temp)
        baseline_version = fixture["baselinePackageVersion"]
        baseline_archive = synthetic_package(
            temp_root / f"com.foldcanvas.core-{baseline_version}.tgz",
            baseline_version,
        )
        current_archive = build_archive(temp_root / "current-package")
        project = temp_root / "upgrade-host"
        before_expected = create_project(
            project,
            baseline_archive,
            source,
            appearance,
        )
        if before_expected["packageVersion"] != baseline_version:
            raise AssertionError("upgrade generator used the wrong baseline")
        input_names = sorted(path.name for path in (project / "M15Input").iterdir())
        if input_names != sorted(fixture["inputFileNames"]):
            raise AssertionError("upgrade input contains a derived artifact")
        asmdef = json.loads(
            (
                project
                / "Assets"
                / "FoldCanvas.M15.Upgrade.Tests.asmdef"
            ).read_text(encoding="utf-8")
        )
        if asmdef.get("references") != ["FoldCanvas.Runtime"]:
            raise AssertionError(
                "upgrade host must compile against only the Runtime assembly"
            )

        before = proof_report(
            before_expected,
            project,
            "before",
            "com.foldcanvas.core@baseline",
            contract,
        )
        write_json(project / "M15Evidence" / "before.json", before)
        before_results, before_log = write_unity_evidence(
            temp_root / "before-unity"
        )
        before_lock = write_package_lock(project)
        before_validation = validate(
            argparse.Namespace(
                phase="before",
                report=project / "M15Evidence" / "before.json",
                manifest=project / "Packages" / "manifest.json",
                lock=before_lock,
                test_results=before_results,
                editor_log=before_log,
                package_version=before_expected["packageVersion"],
                package_sha256=before_expected["packageSha256"],
            )
        )
        if before_validation["tests"]["passed"] != 1:
            raise AssertionError("before-phase Unity evidence did not validate")

        for invalid_version in ("1.0.01", "1.0.2", "2.0.0"):
            invalid_archive = synthetic_package(
                temp_root / f"com.foldcanvas.core-{invalid_version}.tgz",
                invalid_version,
            )
            invalid_project = temp_root / f"invalid-target-{invalid_version}"
            invalid_expected = create_project(
                invalid_project,
                baseline_archive,
                source,
                appearance,
            )
            write_json(
                invalid_project / "M15Evidence" / "before.json",
                proof_report(
                    invalid_expected,
                    invalid_project,
                    "before",
                    "com.foldcanvas.core@invalid-baseline",
                    contract,
                ),
            )
            expect_value_error(
                lambda project=invalid_project, archive=invalid_archive: advance_project(
                    project, archive
                ),
                "stable lineage",
            )
        for directory in ("Library", "Logs", "Temp", "obj"):
            generated = project / directory
            generated.mkdir(parents=True)
            (generated / "derived.bin").write_bytes(b"derived")
        outside_sentinel = temp_root / "outside-sentinel.txt"
        outside_sentinel.write_text("preserve\n", encoding="utf-8")
        source_before = (project / "M15Input" / source.name).read_bytes()
        appearance_before = (project / "M15Input" / appearance.name).read_bytes()

        after_expected = advance_project(project, current_archive)
        current_version = json.loads(
            (ROOT / "package.json").read_text(encoding="utf-8")
        )["version"]
        if after_expected["packageVersion"] != current_version:
            raise AssertionError("upgrade advance used the wrong patch target")
        if after_expected["phase"] != "after":
            raise AssertionError("upgrade advance did not enter after phase")
        for directory in ("Library", "Logs", "Temp", "obj"):
            if (project / directory).exists():
                raise AssertionError(f"owned derived path survived: {directory}")
        if (project / "Packages" / "packages-lock.json").exists():
            raise AssertionError("prior package lock survived the advance")
        if not outside_sentinel.is_file():
            raise AssertionError("advance removed a path outside the owned host")
        if (project / "M15Input" / source.name).read_bytes() != source_before:
            raise AssertionError("advance changed authoritative FoldScript bytes")
        if (
            project / "M15Input" / appearance.name
        ).read_bytes() != appearance_before:
            raise AssertionError("advance changed authoritative PNG bytes")
        if not (project / "M15Evidence" / "before.json").is_file():
            raise AssertionError("advance removed before-phase evidence")

        after = proof_report(
            after_expected,
            project,
            "after",
            "com.foldcanvas.core@candidate",
            contract,
        )
        after_path = project / "M15Evidence" / "after.json"
        write_json(after_path, after)
        after_results, after_log = write_unity_evidence(
            temp_root / "after-unity"
        )
        after_lock = write_package_lock(project)
        after_validation = validate(
            argparse.Namespace(
                phase="after",
                report=after_path,
                manifest=project / "Packages" / "manifest.json",
                lock=after_lock,
                test_results=after_results,
                editor_log=after_log,
                package_version=after_expected["packageVersion"],
                package_sha256=after_expected["packageSha256"],
            )
        )
        if after_validation["tests"]["passed"] != 1:
            raise AssertionError("after-phase Unity evidence did not validate")
        comparison = compare(
            project / "M15Evidence" / "before.json",
            after_path,
            CONTRACT_PATH,
        )
        if comparison.get("derivedInputCount") != 0:
            raise AssertionError("comparison accepted a derived upgrade input")

        tampered = copy.deepcopy(after)
        tampered["geometrySha256"] = "9" * 64
        write_json(after_path, tampered)
        expect_value_error(
            lambda: compare(
                project / "M15Evidence" / "before.json",
                after_path,
                CONTRACT_PATH,
            ),
            "geometrySha256",
        )
        write_json(after_path, after)

        extra_project = temp_root / "extra-input-host"
        extra_expected = create_project(
            extra_project,
            baseline_archive,
            source,
            appearance,
        )
        write_json(
            extra_project / "M15Evidence" / "before.json",
            proof_report(
                extra_expected,
                extra_project,
                "before",
                "com.foldcanvas.core@extra-baseline",
                contract,
            ),
        )
        (extra_project / "M15Input" / "old.mesh").write_bytes(b"mesh")
        expect_value_error(
            lambda: advance_project(extra_project, current_archive),
            "non-source",
        )

        unowned = temp_root / "unowned"
        unowned.mkdir()
        expect_value_error(
            lambda: advance_project(unowned, current_archive),
            "ownership marker",
        )

        unknown_archive = synthetic_package(
            temp_root / "com.foldcanvas.core-9.9.9.tgz",
            "9.9.9",
        )
        expect_value_error(
            lambda: create_project(
                temp_root / "unknown-host",
                unknown_archive,
                source,
                appearance,
            ),
            "unsupported",
        )

        archive_link = temp_root / "baseline-link.tgz"
        archive_link.symlink_to(baseline_archive)
        expect_value_error(
            lambda: create_project(
                temp_root / "archive-link-host",
                archive_link,
                source,
                appearance,
            ),
            "symlink",
        )

        source_link_root = temp_root / "source-link"
        source_link_root.mkdir()
        source_link = source_link_root / source.name
        source_link.symlink_to(source)
        expect_value_error(
            lambda: create_project(
                temp_root / "source-link-host",
                baseline_archive,
                source_link,
                appearance,
            ),
            "symlink",
        )

    print(
        "M15 source-first upgrade project and comparison validation passed."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
