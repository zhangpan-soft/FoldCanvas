#!/usr/bin/env python3
"""Validate the immutable control record used by scheduled RC soak runs."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import re


ROOT = pathlib.Path(__file__).resolve().parents[1]
SHA256 = re.compile(r"[0-9a-f]{64}")
COMMIT = re.compile(r"[0-9a-f]{40}")
VERSION = re.compile(r"1\.0\.0-rc\.[1-9][0-9]*")
PACKAGE_VERSION = re.compile(
    r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)"
)
SUPPORTED_POST_RC_PACKAGE_VERSIONS = frozenset(("1.0.0", "1.0.1", "1.1.0"))
SEED = re.compile(r"[0-9a-f]{16}")


def load_object(path: pathlib.Path, label: str) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object")
    return value


def parse_utc(value: object, label: str) -> str:
    if not isinstance(value, str) or not value.endswith("Z"):
        raise ValueError(f"{label} must be a UTC timestamp ending in Z")
    try:
        parsed = dt.datetime.fromisoformat(value[:-1] + "+00:00")
    except ValueError as error:
        raise ValueError(f"{label} is not a valid timestamp") from error
    if parsed.tzinfo != dt.timezone.utc:
        raise ValueError(f"{label} must use UTC")
    return value


def validate(control: dict, contract: dict, package: dict) -> dict:
    if control.get("format") != "foldcanvas-active-release-candidate":
        raise ValueError("active candidate format is invalid")
    if control.get("version") != "1":
        raise ValueError("active candidate version is unsupported")
    if control.get("active") is not True:
        raise ValueError("active candidate is disabled")
    if control.get("packageName") != package.get("name"):
        raise ValueError("active candidate package name does not match package.json")

    candidate_version = control.get("candidateVersion")
    stable_target = contract.get("stableExit", {}).get("targetVersion")
    package_version = package.get("version")
    stable_descendant = package_version in SUPPORTED_POST_RC_PACKAGE_VERSIONS
    if (
        not isinstance(candidate_version, str)
        or VERSION.fullmatch(candidate_version) is None
        or candidate_version != contract.get("candidateVersion")
        or not isinstance(package_version, str)
        or (
            package_version not in {candidate_version, stable_target}
            and not stable_descendant
        )
    ):
        raise ValueError(
            "active candidate version does not match the RC/stable lineage"
        )

    candidate_tag = control.get("candidateTag")
    if (
        candidate_tag != f"v{candidate_version}"
        or candidate_tag != contract.get("candidateTag")
    ):
        raise ValueError("active candidate tag does not match the RC version")

    candidate_commit = control.get("candidateCommit")
    if not isinstance(candidate_commit, str) or COMMIT.fullmatch(candidate_commit) is None:
        raise ValueError("active candidate commit must be a full lowercase Git SHA")

    archive_sha256 = control.get("archiveSha256")
    if not isinstance(archive_sha256, str) or SHA256.fullmatch(archive_sha256) is None:
        raise ValueError("active candidate archiveSha256 must be lowercase SHA-256")

    published_at = parse_utc(control.get("publishedAt"), "publishedAt")
    unity_version = control.get("unityVersion")
    if (
        unity_version != "6000.3.20f1"
        or unity_version != contract.get("unityVersion")
    ):
        raise ValueError("active candidate Unity version does not match qualification")

    long_run = control.get("longRun")
    if not isinstance(long_run, dict):
        raise ValueError("active candidate longRun must be an object")
    cases_per_suite = long_run.get("casesPerSuite")
    seed_hex = long_run.get("seedHex")
    if not isinstance(cases_per_suite, int) or not 1 <= cases_per_suite <= 256:
        raise ValueError("longRun casesPerSuite must be between 1 and 256")
    if not isinstance(seed_hex, str) or SEED.fullmatch(seed_hex) is None:
        raise ValueError("longRun seedHex must be exactly 16 lowercase hex digits")

    stable_exit = control.get("stableExit")
    contract_exit = contract.get("stableExit")
    if not isinstance(stable_exit, dict) or not isinstance(contract_exit, dict):
        raise ValueError("active candidate stableExit must be an object")
    expected_exit = {
        "targetVersion": contract_exit.get("targetVersion"),
        "minimumSoakHours": contract_exit.get("minimumSoakHours"),
        "minimumScheduledLongRuns": contract_exit.get(
            "minimumScheduledLongRuns"
        ),
    }
    if stable_exit != expected_exit:
        raise ValueError("active candidate stable-exit policy drifted from M15")

    return {
        "candidate_version": candidate_version,
        "candidate_tag": candidate_tag,
        "candidate_commit": candidate_commit,
        "archive_sha256": archive_sha256,
        "published_at": published_at,
        "unity_version": unity_version,
        "cases_per_suite": str(cases_per_suite),
        "seed_hex": seed_hex,
    }


def write_github_output(path: pathlib.Path, values: dict) -> None:
    lines = []
    for key in sorted(values):
        value = values[key]
        if not isinstance(value, str) or "\n" in value or "\r" in value:
            raise ValueError(f"unsafe GitHub output value: {key}")
        lines.append(f"{key}={value}")
    with path.open("a", encoding="utf-8", newline="\n") as output:
        output.write("\n".join(lines) + "\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--control",
        type=pathlib.Path,
        default=ROOT / ".github" / "foldcanvas-active-candidate.json",
    )
    parser.add_argument(
        "--contract",
        type=pathlib.Path,
        default=ROOT / "Documentation~" / "m15-public-distribution.json",
    )
    parser.add_argument(
        "--package",
        type=pathlib.Path,
        default=ROOT / "package.json",
    )
    parser.add_argument("--github-output", type=pathlib.Path)
    args = parser.parse_args()
    values = validate(
        load_object(args.control, "active candidate"),
        load_object(args.contract, "M15 contract"),
        load_object(args.package, "package.json"),
    )
    if args.github_output is not None:
        write_github_output(args.github_output, values)
    print(
        "Active candidate validation passed: "
        f"{values['candidate_tag']} at {values['candidate_commit']}."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, json.JSONDecodeError) as error:
        print(f"Active candidate validation failed: {error}")
        raise SystemExit(1)
