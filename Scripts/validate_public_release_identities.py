#!/usr/bin/env python3
"""Validate immutable FoldCanvas GitHub release identities offline."""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import sys
import tempfile


ROOT = pathlib.Path(__file__).resolve().parents[1]
DEFAULT_LEDGER = ROOT / "Docs" / "Release" / "public-release-identities.json"
SEMVER = re.compile(
    r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)"
    r"(?:-[0-9A-Za-z.-]+)?"
)
SHA256 = re.compile(r"[0-9a-f]{64}")
COMMIT = re.compile(r"[0-9a-f]{40}")
DIGEST_FIELDS = (
    "archiveSha256",
    "checksumSha256",
    "manifestSha256",
    "evidenceSha256",
)


class PublicReleaseIdentityError(ValueError):
    pass


def load_object(path: pathlib.Path, label: str) -> dict:
    if path.is_symlink() or not path.is_file() or path.stat().st_size <= 0:
        raise PublicReleaseIdentityError(f"{label} must be one regular JSON file")
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exception:  # noqa: BLE001
        raise PublicReleaseIdentityError(f"{label} is invalid JSON") from exception
    if not isinstance(value, dict):
        raise PublicReleaseIdentityError(f"{label} must be a JSON object")
    return value


def validate_ledger(value: dict) -> list[dict]:
    if (
        value.get("format") != "foldcanvas-public-release-identities"
        or value.get("version") != "1"
        or value.get("packageName") != "com.foldcanvas.core"
    ):
        raise PublicReleaseIdentityError("release identity ledger header differs")
    releases = value.get("releases")
    if not isinstance(releases, list) or not releases:
        raise PublicReleaseIdentityError("release identity ledger is empty")
    versions: list[str] = []
    tags: list[str] = []
    commits: list[str] = []
    release_ids: list[int] = []
    for index, release in enumerate(releases):
        if not isinstance(release, dict):
            raise PublicReleaseIdentityError(f"release[{index}] is invalid")
        version = release.get("packageVersion")
        tag = release.get("tag")
        commit = release.get("tagCommit")
        release_id = release.get("releaseId")
        if (
            not isinstance(version, str)
            or SEMVER.fullmatch(version) is None
            or tag != "v" + version
            or not isinstance(commit, str)
            or COMMIT.fullmatch(commit) is None
            or not isinstance(release_id, int)
            or release_id <= 0
            or release.get("assetCount") != 4
            or release.get("immutable") is not True
            or not isinstance(release.get("publishedAt"), str)
            or not release["publishedAt"].endswith("Z")
        ):
            raise PublicReleaseIdentityError(f"release[{index}] identity differs")
        for field in DIGEST_FIELDS:
            if SHA256.fullmatch(str(release.get(field, ""))) is None:
                raise PublicReleaseIdentityError(
                    f"release[{index}] has invalid {field}"
                )
        versions.append(version)
        tags.append(tag)
        commits.append(commit)
        release_ids.append(release_id)
    if (
        versions != sorted(set(versions), key=semantic_key)
        or len(tags) != len(set(tags))
        or len(commits) != len(set(commits))
        or len(release_ids) != len(set(release_ids))
    ):
        raise PublicReleaseIdentityError(
            "release identities must be unique and semantic-version ordered"
        )
    return releases


def semantic_key(
    version: str,
) -> tuple[int, int, int, int, tuple[tuple[int, int | str], ...]]:
    core, separator, prerelease = version.partition("-")
    major, minor, patch = (int(value) for value in core.split("."))
    # SemVer prereleases sort before the corresponding final release. Numeric
    # identifiers compare numerically and sort before non-numeric identifiers.
    # The regex rejects empty identifiers, so one canonical key is sufficient.
    identifiers = prerelease.split(".") if separator else []
    if any(
        not identifier
        or re.fullmatch(r"[0-9A-Za-z-]+", identifier) is None
        or (
            identifier.isdigit()
            and len(identifier) > 1
            and identifier.startswith("0")
        )
        for identifier in identifiers
    ):
        raise PublicReleaseIdentityError(
            f"release version has invalid SemVer prerelease: {version}"
        )
    suffix = tuple(
        (0, int(identifier))
        if identifier.isdigit()
        else (1, identifier)
        for identifier in identifiers
    )
    return major, minor, patch, 0 if separator else 1, suffix


def git_text(root: pathlib.Path, *args: str) -> str:
    completed = subprocess.run(
        ["git", *args],
        cwd=root,
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
    )
    return completed.stdout.strip()


def rebuild_release(root: pathlib.Path, release: dict) -> dict[str, str]:
    tag = release["tag"]
    resolved = git_text(root, "rev-list", "-n", "1", tag)
    if resolved != release["tagCommit"]:
        raise PublicReleaseIdentityError(f"{tag} peeled commit differs")
    if git_text(root, "cat-file", "-t", tag) != "tag":
        raise PublicReleaseIdentityError(f"{tag} must be an annotated tag")
    with tempfile.TemporaryDirectory(prefix="foldcanvas-release-ledger-") as temporary:
        temporary_path = pathlib.Path(temporary)
        source = temporary_path / "source"
        source.mkdir()
        archive_process = subprocess.Popen(
            ["git", "archive", tag],
            cwd=root,
            stdout=subprocess.PIPE,
        )
        assert archive_process.stdout is not None
        extract = subprocess.run(
            ["tar", "-x", "-C", str(source)],
            stdin=archive_process.stdout,
            check=True,
        )
        archive_process.stdout.close()
        if archive_process.wait() != 0 or extract.returncode != 0:
            raise PublicReleaseIdentityError(f"could not export {tag}")
        builder = source / "Scripts" / "build_release_package.py"
        if not builder.is_file():
            raise PublicReleaseIdentityError(f"{tag} lacks its release builder")
        output = temporary_path / "output"
        subprocess.run(
            [sys.executable, str(builder), "--output", str(output), "--tag", tag],
            cwd=source,
            check=True,
            stdout=subprocess.DEVNULL,
        )
        prefix = output / f"com.foldcanvas.core-{release['packageVersion']}"
        paths = {
            "archiveSha256": pathlib.Path(str(prefix) + ".tgz"),
            "checksumSha256": pathlib.Path(str(prefix) + ".tgz.sha256"),
            "manifestSha256": pathlib.Path(str(prefix) + ".manifest.json"),
            "evidenceSha256": pathlib.Path(str(prefix) + ".evidence.json"),
        }
        if any(not path.is_file() for path in paths.values()):
            raise PublicReleaseIdentityError(
                f"{tag} did not build the exact four release assets"
            )
        return {
            field: hashlib.sha256(path.read_bytes()).hexdigest()
            for field, path in paths.items()
        }


def current_archive_digest(root: pathlib.Path) -> tuple[str, str]:
    package = load_object(root / "package.json", "package.json")
    version = package.get("version")
    if not isinstance(version, str) or SEMVER.fullmatch(version) is None:
        raise PublicReleaseIdentityError("current package version is invalid")
    with tempfile.TemporaryDirectory(prefix="foldcanvas-current-package-") as temporary:
        output = pathlib.Path(temporary)
        subprocess.run(
            [
                sys.executable,
                str(root / "Scripts" / "build_release_package.py"),
                "--output",
                str(output),
            ],
            cwd=root,
            check=True,
            stdout=subprocess.DEVNULL,
        )
        archive = output / f"com.foldcanvas.core-{version}.tgz"
        return version, hashlib.sha256(archive.read_bytes()).hexdigest()


def validate(
    root: pathlib.Path = ROOT,
    ledger_path: pathlib.Path = DEFAULT_LEDGER,
    *,
    rebuild_all: bool = True,
) -> dict:
    releases = validate_ledger(load_object(ledger_path, "release identity ledger"))
    rebuilt: list[dict] = []
    if rebuild_all:
        for release in releases:
            digests = rebuild_release(root, release)
            for field in DIGEST_FIELDS:
                if digests[field] != release[field]:
                    raise PublicReleaseIdentityError(
                        f"{release['tag']} rebuilt {field} differs from "
                        "immutable identity"
                    )
            rebuilt.append({"tag": release["tag"], **digests})
    current_version, current_digest = current_archive_digest(root)
    recorded = next(
        (release for release in releases if release["packageVersion"] == current_version),
        None,
    )
    if recorded is not None and current_digest != recorded["archiveSha256"]:
        raise PublicReleaseIdentityError(
            "current package reuses immutable version "
            f"{current_version} with different archive bytes"
        )
    return {
        "format": "foldcanvas-public-release-identity-validation",
        "version": "1",
        "releaseCount": len(releases),
        "rebuiltReleaseCount": len(rebuilt),
        "currentPackageVersion": current_version,
        "currentArchiveSha256": current_digest,
        "currentVersionPublished": recorded is not None,
        "releases": rebuilt,
        "valid": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=pathlib.Path, default=ROOT)
    parser.add_argument("--ledger", type=pathlib.Path, default=DEFAULT_LEDGER)
    parser.add_argument("--skip-historical-rebuilds", action="store_true")
    parser.add_argument("--output", type=pathlib.Path)
    args = parser.parse_args()
    report = validate(
        args.root.resolve(),
        args.ledger.resolve(),
        rebuild_all=not args.skip_historical_rebuilds,
    )
    payload = json.dumps(report, indent=2, sort_keys=True) + "\n"
    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(payload, encoding="utf-8", newline="\n")
    print(
        "Immutable public release identities passed: "
        f"{report['rebuiltReleaseCount']}/{report['releaseCount']} rebuilt; "
        f"current {report['currentPackageVersion']}."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, subprocess.CalledProcessError, PublicReleaseIdentityError) as error:
        print(f"Immutable public release identity validation failed: {error}", file=sys.stderr)
        raise SystemExit(1)
