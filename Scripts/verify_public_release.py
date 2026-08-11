#!/usr/bin/env python3
from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import pathlib
import re
import tarfile
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
SHA256_PATTERN = re.compile(r"[0-9a-f]{64}")
FORBIDDEN_ARCHIVE_PARTS = {
    ".git",
    ".github",
    "Codex",
    "Docs",
    "Library",
    "Logs",
    "Project~",
    "artifacts",
}
MAXIMUM_ARCHIVE_BYTES = 256 * 1024 * 1024
MAXIMUM_JSON_BYTES = 16 * 1024 * 1024
MAXIMUM_CHECKSUM_BYTES = 4096
MAXIMUM_ARCHIVE_FILES = 10000
MAXIMUM_MEMBER_BYTES = 64 * 1024 * 1024
MAXIMUM_UNPACKED_BYTES = 512 * 1024 * 1024


class PublicReleaseVerificationError(ValueError):
    pass


def sha256_file(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while True:
            chunk = stream.read(1024 * 1024)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def canonical_json(value: dict) -> str:
    return json.dumps(
        value,
        ensure_ascii=False,
        indent=2,
        sort_keys=True,
        separators=(",", ": "),
    ) + "\n"


def load_object(path: pathlib.Path, label: str) -> dict:
    if (
        path.is_symlink()
        or not path.is_file()
        or path.stat().st_size <= 0
        or path.stat().st_size > MAXIMUM_JSON_BYTES
    ):
        raise PublicReleaseVerificationError(
            f"{label} must be one bounded regular JSON file"
        )
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exception:  # noqa: BLE001
        raise PublicReleaseVerificationError(
            f"{label} is not valid UTF-8 JSON"
        ) from exception
    if not isinstance(value, dict):
        raise PublicReleaseVerificationError(f"{label} must be a JSON object")
    return value


def expected_asset_names(version: str) -> list[str]:
    prefix = f"com.foldcanvas.core-{version}"
    return sorted(
        [
            f"{prefix}.evidence.json",
            f"{prefix}.manifest.json",
            f"{prefix}.tgz",
            f"{prefix}.tgz.sha256",
        ]
    )


def validate_asset_directory(
    asset_directory: pathlib.Path,
    expected_names: list[str],
) -> dict[str, pathlib.Path]:
    if asset_directory.is_symlink() or not asset_directory.is_dir():
        raise PublicReleaseVerificationError(
            "Public release asset directory must be one real directory"
        )
    entries = sorted(asset_directory.iterdir(), key=lambda path: path.name)
    names = [path.name for path in entries]
    if names != expected_names:
        raise PublicReleaseVerificationError(
            f"Public release assets differ from exact allowlist: {names}"
        )
    for path in entries:
        if path.is_symlink() or not path.is_file():
            raise PublicReleaseVerificationError(
                f"Public release asset must be one regular file: {path.name}"
            )
        if path.stat().st_size <= 0:
            raise PublicReleaseVerificationError(
                f"Public release asset is empty: {path.name}"
            )
        maximum = (
            MAXIMUM_ARCHIVE_BYTES
            if path.name.endswith(".tgz")
            else MAXIMUM_CHECKSUM_BYTES
            if path.name.endswith(".sha256")
            else MAXIMUM_JSON_BYTES
        )
        if path.stat().st_size > maximum:
            raise PublicReleaseVerificationError(
                f"Public release asset exceeds its byte limit: {path.name}"
            )
    return {path.name: path for path in entries}


def validate_release_metadata(
    metadata: dict,
    repository: str,
    tag: str,
    tag_commit: str,
    assets: dict[str, pathlib.Path],
    expected_prerelease: bool,
) -> int:
    if metadata.get("tag_name") != tag:
        raise PublicReleaseVerificationError("GitHub release tag does not match")
    if (
        metadata.get("draft") is not False
        or metadata.get("prerelease") is not expected_prerelease
    ):
        raise PublicReleaseVerificationError(
            "GitHub release publication state differs from the contract"
        )
    release_id = metadata.get("id")
    if not isinstance(release_id, int) or release_id <= 0:
        raise PublicReleaseVerificationError("GitHub release id is invalid")
    if re.fullmatch(r"[0-9a-f]{40}", tag_commit) is None:
        raise PublicReleaseVerificationError("Resolved tag commit must be 40 hex digits")
    html_url = metadata.get("html_url")
    if html_url != f"https://github.com/{repository}/releases/tag/{tag}":
        raise PublicReleaseVerificationError("GitHub release repository identity differs")

    metadata_assets = metadata.get("assets")
    if not isinstance(metadata_assets, list):
        raise PublicReleaseVerificationError("GitHub release assets must be an array")
    by_name: dict[str, dict] = {}
    for entry in metadata_assets:
        if not isinstance(entry, dict) or not isinstance(entry.get("name"), str):
            raise PublicReleaseVerificationError("GitHub release asset is invalid")
        name = entry["name"]
        if name in by_name:
            raise PublicReleaseVerificationError(
                f"GitHub release contains duplicate asset: {name}"
            )
        by_name[name] = entry
    if sorted(by_name) != sorted(assets):
        raise PublicReleaseVerificationError(
            "GitHub release metadata differs from downloaded asset allowlist"
        )
    for name, path in assets.items():
        entry = by_name[name]
        if entry.get("state") not in (None, "uploaded"):
            raise PublicReleaseVerificationError(
                f"GitHub release asset is not uploaded: {name}"
            )
        if entry.get("size") != path.stat().st_size:
            raise PublicReleaseVerificationError(
                f"GitHub release asset size differs: {name}"
            )
        if entry.get("digest") != "sha256:" + sha256_file(path):
            raise PublicReleaseVerificationError(
                f"GitHub release asset digest differs: {name}"
            )
    return release_id


def validate_checksum(
    checksum_path: pathlib.Path,
    archive_path: pathlib.Path,
) -> str:
    try:
        checksum = checksum_path.read_text(encoding="ascii")
    except Exception as exception:  # noqa: BLE001
        raise PublicReleaseVerificationError(
            "Archive checksum must be ASCII"
        ) from exception
    expected_line = f"{sha256_file(archive_path)}  {archive_path.name}\n"
    if checksum != expected_line:
        raise PublicReleaseVerificationError(
            "Archive checksum content does not match the downloaded archive"
        )
    return expected_line[:64]


def validate_archive_and_manifest(
    archive_path: pathlib.Path,
    manifest: dict,
    package_version: str,
) -> int:
    archive_digest = sha256_file(archive_path)
    if (
        manifest.get("format") != "foldcanvas-release-file-manifest"
        or manifest.get("version") != "1"
        or manifest.get("packageName") != "com.foldcanvas.core"
        or manifest.get("packageVersion") != package_version
        or manifest.get("archiveFile") != archive_path.name
        or manifest.get("archiveSha256") != archive_digest
    ):
        raise PublicReleaseVerificationError("Release file manifest header differs")
    files = manifest.get("files")
    if (
        not isinstance(files, list)
        or not files
        or len(files) > MAXIMUM_ARCHIVE_FILES
    ):
        raise PublicReleaseVerificationError("Release file manifest is empty")
    manifest_names = [entry.get("path") for entry in files if isinstance(entry, dict)]
    if len(manifest_names) != len(files):
        raise PublicReleaseVerificationError("Release file manifest entry is invalid")
    if manifest_names != sorted(set(manifest_names)):
        raise PublicReleaseVerificationError(
            "Release file manifest paths must be unique and ordinal"
        )
    if manifest.get("fileCount") != len(files):
        raise PublicReleaseVerificationError("Release file manifest count differs")

    try:
        with tempfile.TemporaryFile() as canonical_raw:
            with gzip.GzipFile(
                filename="",
                mode="wb",
                fileobj=canonical_raw,
                compresslevel=9,
                mtime=0,
            ) as compressed:
                canonical_archive = tarfile.open(
                    fileobj=compressed,
                    mode="w",
                    format=tarfile.USTAR_FORMAT,
                )
                validate_archive_members(
                    archive_path,
                    files,
                    manifest_names,
                    canonical_archive,
                    package_version,
                )
                canonical_archive.close()
            canonical_raw.seek(0)
            with archive_path.open("rb") as original:
                while True:
                    expected_chunk = canonical_raw.read(1024 * 1024)
                    actual_chunk = original.read(1024 * 1024)
                    if expected_chunk != actual_chunk:
                        raise PublicReleaseVerificationError(
                            "Release archive bytes are not canonical deterministic tgz"
                        )
                    if not expected_chunk:
                        break
    except (tarfile.TarError, OSError) as exception:
        raise PublicReleaseVerificationError(
            "Release archive is not a readable deterministic tgz"
        ) from exception
    return len(files)


def validate_archive_members(
    archive_path: pathlib.Path,
    files: list[dict],
    manifest_names: list[str],
    canonical_archive: tarfile.TarFile,
    package_version: str,
) -> None:
    total_unpacked = 0
    semantic_files: dict[str, bytes] = {}
    with tarfile.open(archive_path, mode="r:gz") as archive:
        members = archive.getmembers()
        names = [member.name for member in members]
        if names != sorted(set(names)):
            raise PublicReleaseVerificationError(
                "Release archive entries must be unique and ordinal"
            )
        if names != manifest_names:
            raise PublicReleaseVerificationError(
                "Release archive entries differ from file manifest"
            )
        for index, member in enumerate(members):
            pure = pathlib.PurePosixPath(member.name)
            if (
                pure.is_absolute()
                or not pure.parts
                or pure.parts[0] != "package"
                or ".." in pure.parts
                or set(pure.parts).intersection(FORBIDDEN_ARCHIVE_PARTS)
            ):
                raise PublicReleaseVerificationError(
                    f"Release archive path is unsafe: {member.name}"
                )
            if not member.isfile() or member.issym() or member.islnk():
                raise PublicReleaseVerificationError(
                    "Release archive member is not a regular file: "
                    + member.name
                )
            if member.size < 0 or member.size > MAXIMUM_MEMBER_BYTES:
                raise PublicReleaseVerificationError(
                    "Release archive member exceeds its byte limit: "
                    + member.name
                )
            total_unpacked += member.size
            if total_unpacked > MAXIMUM_UNPACKED_BYTES:
                raise PublicReleaseVerificationError(
                    "Release archive exceeds its unpacked byte limit"
                )
            if (
                member.mode != 0o644
                or member.mtime != 0
                or member.uid != 0
                or member.gid != 0
                or member.uname != ""
                or member.gname != ""
            ):
                raise PublicReleaseVerificationError(
                    "Release archive metadata is not normalized: "
                    + member.name
                )
            extracted = archive.extractfile(member)
            if extracted is None:
                raise PublicReleaseVerificationError(
                    f"Release archive member cannot be read: {member.name}"
                )
            entry = files[index]
            if entry.get("size") != member.size:
                raise PublicReleaseVerificationError(
                    f"Release manifest size differs: {member.name}"
                )
            if not isinstance(entry.get("size"), int) or not re.fullmatch(
                r"[0-9a-f]{64}", str(entry.get("sha256", ""))
            ):
                raise PublicReleaseVerificationError(
                    f"Release manifest entry is invalid: {member.name}"
                )
            digest = hashlib.sha256()
            with tempfile.SpooledTemporaryFile(
                max_size=4 * 1024 * 1024
            ) as payload:
                copied = 0
                while True:
                    chunk = extracted.read(1024 * 1024)
                    if not chunk:
                        break
                    copied += len(chunk)
                    if copied > member.size:
                        raise PublicReleaseVerificationError(
                            "Release archive member expanded beyond its "
                            f"header: {member.name}"
                        )
                    digest.update(chunk)
                    payload.write(chunk)
                if copied != member.size:
                    raise PublicReleaseVerificationError(
                        f"Release archive member length differs: {member.name}"
                    )
                if entry.get("sha256") != digest.hexdigest():
                    raise PublicReleaseVerificationError(
                        f"Release manifest digest differs: {member.name}"
                    )
                if member.name in (
                    "package/package.json",
                    "package/Runtime/Data/FoldCanvasVersion.cs",
                ):
                    payload.seek(0)
                    semantic_files[member.name] = payload.read()
                payload.seek(0)
                canonical_info = tarfile.TarInfo(member.name)
                canonical_info.size = member.size
                canonical_info.mode = 0o644
                canonical_info.mtime = 0
                canonical_info.uid = 0
                canonical_info.gid = 0
                canonical_info.uname = ""
                canonical_info.gname = ""
                canonical_archive.addfile(canonical_info, payload)

    try:
        package = json.loads(
            semantic_files["package/package.json"].decode("utf-8")
        )
        version_source = semantic_files[
            "package/Runtime/Data/FoldCanvasVersion.cs"
        ].decode("utf-8")
    except (KeyError, UnicodeDecodeError, json.JSONDecodeError) as exception:
        raise PublicReleaseVerificationError(
            "Release archive lacks readable package version identity"
        ) from exception
    if package.get("name") != "com.foldcanvas.core" or package.get(
        "version"
    ) != package_version:
        raise PublicReleaseVerificationError(
            "Release archive package.json version identity differs"
        )
    runtime_match = re.search(
        r'\bPackage\s*=\s*"([^"]+)"\s*;',
        version_source,
    )
    if runtime_match is None or runtime_match.group(1) != package_version:
        raise PublicReleaseVerificationError(
            "Release archive runtime version identity differs"
        )


def validate_release_evidence(
    evidence: dict,
    contract: dict,
    contract_path: pathlib.Path,
    archive_path: pathlib.Path,
    manifest_path: pathlib.Path,
    package_version: str,
) -> None:
    stable_release = contract.get("stableRelease") is True
    if stable_release:
        if (
            evidence.get("format") != "foldcanvas-stable-release-evidence"
            or evidence.get("version") != "1"
            or evidence.get("state") != "built-unverified"
            or evidence.get("packageName") != "com.foldcanvas.core"
            or evidence.get("packageVersion") != package_version
            or evidence.get("stableRelease") is not True
            or evidence.get("foldScriptVersion") != "0.1"
        ):
            raise PublicReleaseVerificationError(
                "Stable release evidence header differs"
            )
        expected_contract_path = "Documentation~/m17-stable-release.json"
        expected_fields = (
            "publication",
            "releaseCandidate",
            "requiredPostPublicationGates",
            "requiredPrePublicationGates",
            "rollback",
            "stableQualification",
            "upgrade",
        )
    else:
        if (
            evidence.get("format") != "foldcanvas-release-candidate-evidence"
            or evidence.get("version") != "1"
            or evidence.get("state") != "built-unverified"
            or evidence.get("packageName") != "com.foldcanvas.core"
            or evidence.get("packageVersion") != package_version
            or evidence.get("stableRelease") is not False
            or evidence.get("foldScriptVersion") != "0.1"
        ):
            raise PublicReleaseVerificationError("Candidate evidence header differs")
        expected_contract_path = "Documentation~/m15-public-distribution.json"
        expected_fields = (
            "priorRelease",
            "requiredGates",
            "rollback",
            "stableExit",
            "upgrade",
        )

    if evidence.get("archive") != {
        "file": archive_path.name,
        "sha256": sha256_file(archive_path),
    }:
        raise PublicReleaseVerificationError("Candidate archive evidence differs")
    if evidence.get("fileManifest") != {
        "file": manifest_path.name,
        "sha256": sha256_file(manifest_path),
    }:
        raise PublicReleaseVerificationError("Candidate manifest evidence differs")
    if evidence.get("contract") != {
        "path": expected_contract_path,
        "sha256": sha256_file(contract_path),
    }:
        raise PublicReleaseVerificationError("Release contract evidence differs")
    for field in expected_fields:
        if evidence.get(field) != contract.get(field):
            raise PublicReleaseVerificationError(
                f"Release evidence field differs from contract: {field}"
            )
    if not stable_release:
        publication = evidence.get("publication", {})
        if (
            publication.get("githubPrereleaseOnly") is not True
            or publication.get("finalStableRelease") is not False
            or publication.get("externalMarketplace") is not False
        ):
            raise PublicReleaseVerificationError(
                "Candidate publication scope differs"
            )


def verify_public_release(
    asset_directory: pathlib.Path,
    release_metadata_path: pathlib.Path,
    contract_path: pathlib.Path,
    repository: str,
    tag: str,
    tag_commit: str,
) -> dict:
    contract = load_object(contract_path, "Public distribution contract")
    stable_release = contract.get("stableRelease") is True
    package_version = (
        contract.get("packageVersion")
        if stable_release
        else contract.get("candidateVersion")
    )
    if not isinstance(package_version, str) or tag != "v" + package_version:
        raise PublicReleaseVerificationError(
            "Public release tag must exactly match the candidate version"
        )
    expected_names = expected_asset_names(package_version)
    if contract.get("publicAssets") != expected_names:
        raise PublicReleaseVerificationError(
            "Public distribution contract asset allowlist differs"
        )
    assets = validate_asset_directory(asset_directory, expected_names)
    metadata = load_object(release_metadata_path, "GitHub release metadata")
    release_id = validate_release_metadata(
        metadata,
        repository,
        tag,
        tag_commit,
        assets,
        not stable_release,
    )

    prefix = f"com.foldcanvas.core-{package_version}"
    archive_path = assets[f"{prefix}.tgz"]
    checksum_path = assets[f"{prefix}.tgz.sha256"]
    manifest_path = assets[f"{prefix}.manifest.json"]
    evidence_path = assets[f"{prefix}.evidence.json"]
    archive_digest = validate_checksum(checksum_path, archive_path)
    manifest = load_object(manifest_path, "Release file manifest")
    file_count = validate_archive_and_manifest(
        archive_path,
        manifest,
        package_version,
    )
    evidence = load_object(evidence_path, "Candidate evidence")
    validate_release_evidence(
        evidence,
        contract,
        contract_path,
        archive_path,
        manifest_path,
        package_version,
    )
    return {
        "format": "foldcanvas-public-release-verification",
        "version": "1",
        "repository": repository,
        "tag": tag,
        "tagCommit": tag_commit,
        "releaseId": release_id,
        "packageName": "com.foldcanvas.core",
        "packageVersion": package_version,
        "stableRelease": stable_release,
        "unityVersion": contract["unityVersion"],
        "foldScriptVersion": contract["foldScriptVersion"],
        "assetCount": len(assets),
        "archiveSha256": archive_digest,
        "checksumSha256": sha256_file(checksum_path),
        "manifestSha256": sha256_file(manifest_path),
        "evidenceSha256": sha256_file(evidence_path),
        "fileCount": file_count,
        "qualified": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Verify exact public FoldCanvas GitHub release assets."
    )
    parser.add_argument("--asset-directory", required=True, type=pathlib.Path)
    parser.add_argument("--release-metadata", required=True, type=pathlib.Path)
    parser.add_argument(
        "--contract",
        type=pathlib.Path,
        default=ROOT / "Documentation~" / "m15-public-distribution.json",
    )
    parser.add_argument("--repository", required=True)
    parser.add_argument("--tag", required=True)
    parser.add_argument("--tag-commit", required=True)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    report = verify_public_release(
        args.asset_directory.absolute(),
        args.release_metadata.absolute(),
        args.contract.absolute(),
        args.repository,
        args.tag,
        args.tag_commit,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        canonical_json(report),
        encoding="utf-8",
        newline="\n",
    )
    print(
        "Public release verification passed: "
        f"{report['tag']} {report['archiveSha256']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
