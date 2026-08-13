#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re


SHA_FIELDS = (
    "packageSha256",
    "sourceRawSha256",
    "canonicalSourceSha256",
    "appearanceSha256",
    "geometrySha256",
    "objSha256",
    "diagnosticSha256",
    "validationSha256",
)

STABLE_FIELDS = (
    "format",
    "version",
    "packageName",
    "unityVersion",
    "packageSource",
    "sourceInputFiles",
    "sourceInputCount",
    "derivedInputCount",
    "sourceRawSha256",
    "canonicalSourceSha256",
    "appearanceSha256",
    "geometrySha256",
    "objSha256",
    "diagnosticSha256",
    "validationSha256",
    "renderVertices",
    "topologyVertices",
    "triangles",
    "diagnostics",
    "openEdges",
    "nonManifoldEdges",
    "components",
    "isClosedVolume",
    "isSingleClosedVolume",
)
SEMANTIC_VERSION = re.compile(
    r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)"
)


def is_sha256(value: object) -> bool:
    return isinstance(value, str) and len(value) == 64 and all(
        character in "0123456789abcdef" for character in value
    )


def load_object(path: pathlib.Path, label: str) -> dict:
    if not path.is_file() or path.stat().st_size == 0:
        raise ValueError(f"{label} is missing or empty: {path}")
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object: {path}")
    return value


def validate_report(report: dict, phase: str) -> None:
    if (
        report.get("format") != "foldcanvas-m15-source-upgrade-proof"
        or report.get("version") != "1"
        or report.get("phase") != phase
        or report.get("packageName") != "com.foldcanvas.core"
        or report.get("unityVersion") != "6000.3.20f1"
        or report.get("packageSource") != "LocalTarball"
    ):
        raise ValueError(f"M15 {phase} report identity is invalid")
    for field in SHA_FIELDS:
        if not is_sha256(report.get(field)):
            raise ValueError(f"M15 {phase} report has invalid {field}")
    if report.get("sourceInputFiles") != [
        "M04ProductionCupCanvas.png",
        "m12-production-cup.foldcanvas.json",
    ]:
        raise ValueError(f"M15 {phase} report input allowlist is invalid")
    if report.get("sourceInputCount") != 2 or report.get("derivedInputCount") != 0:
        raise ValueError(f"M15 {phase} report consumed a derived input")
    expected_counts = {
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
    for field, expected in expected_counts.items():
        if report.get(field) != expected:
            raise ValueError(
                f"M15 {phase} report has unexpected {field}: "
                f"{report.get(field)!r}"
            )
    resolved = report.get("resolvedPackagePath")
    if not isinstance(resolved, str) or "/Library/PackageCache/" not in resolved:
        raise ValueError(
            f"M15 {phase} package was not resolved from PackageCache"
        )


def compare(
    before_path: pathlib.Path,
    after_path: pathlib.Path,
    contract_path: pathlib.Path,
) -> dict:
    before = load_object(before_path, "M15 before report")
    after = load_object(after_path, "M15 after report")
    contract = load_object(contract_path, "M15 distribution contract")
    validate_report(before, "before")
    validate_report(after, "after")

    upgrade = contract.get("upgrade")
    fixture = upgrade.get("fixture") if isinstance(upgrade, dict) else None
    if not isinstance(fixture, dict):
        raise ValueError("M15 upgrade fixture contract is missing")
    if before.get("packageVersion") not in upgrade.get(
        "fromPackageVersions", []
    ):
        raise ValueError("M15 before package version is not an approved baseline")
    target_version = after.get("packageVersion")
    if contract.get("stableRelease") is True:
        stable_version = str(contract.get("packageVersion"))
        stable_parts = stable_version.split(".")
        target_parts = str(target_version).split(".")
        if (
            not isinstance(target_version, str)
            or SEMANTIC_VERSION.fullmatch(target_version) is None
            or len(stable_parts) != 3
            or len(target_parts) != 3
            or stable_parts[:2] != target_parts[:2]
            or not stable_parts[2].isdigit()
            or not target_parts[2].isdigit()
            or int(target_parts[2]) < int(stable_parts[2])
        ):
            raise ValueError("M15 after package version is not the stable lineage")
    elif target_version != contract.get("candidateVersion"):
        raise ValueError("M15 after package version is not the contract target")
    if before.get("packageVersion") == after.get("packageVersion"):
        raise ValueError("M15 upgrade did not replace the package version")
    if before.get("packageSha256") == after.get("packageSha256"):
        raise ValueError("M15 upgrade did not replace the package bytes")
    if before.get("resolvedPackagePath") == after.get("resolvedPackagePath"):
        raise ValueError("M15 upgrade reused the prior PackageCache directory")

    for field in STABLE_FIELDS:
        if before.get(field) != after.get(field):
            raise ValueError(
                f"M15 source-first upgrade changed semantic evidence: {field}"
            )
    for report in (before, after):
        if report.get("sourceRawSha256") != fixture.get("sourceRawSha256"):
            raise ValueError("M15 report does not identify the contracted FoldScript")
        if report.get("canonicalSourceSha256") != fixture.get(
            "canonicalSourceSha256"
        ):
            raise ValueError("M15 report canonical FoldScript identity changed")
        if report.get("appearanceSha256") != fixture.get("appearanceSha256"):
            raise ValueError("M15 report appearance identity changed")

    stable = {field: before.get(field) for field in STABLE_FIELDS}
    canonical = json.dumps(
        stable,
        ensure_ascii=True,
        separators=(",", ":"),
        sort_keys=True,
    ).encode("utf-8")
    return {
        "format": "foldcanvas-m15-source-upgrade-comparison",
        "version": "1",
        "baselineVersion": before["packageVersion"],
        "candidateVersion": after["packageVersion"],
        "sourceAuthority": upgrade.get("sourceAuthority"),
        "derivedInputCount": 0,
        "stableEvidenceSha256": hashlib.sha256(canonical).hexdigest(),
        "geometrySha256": before["geometrySha256"],
        "objSha256": before["objSha256"],
        "validationSha256": before["validationSha256"],
        "isSingleClosedVolume": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare before/after M15 source-first Unity evidence."
    )
    parser.add_argument("--before", required=True, type=pathlib.Path)
    parser.add_argument("--after", required=True, type=pathlib.Path)
    parser.add_argument("--contract", required=True, type=pathlib.Path)
    parser.add_argument("--output", type=pathlib.Path)
    args = parser.parse_args()
    result = compare(args.before, args.after, args.contract)
    payload = json.dumps(result, indent=2, sort_keys=True) + "\n"
    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(payload, encoding="utf-8", newline="\n")
    else:
        print(payload, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
