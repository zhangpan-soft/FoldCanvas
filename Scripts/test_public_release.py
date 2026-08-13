#!/usr/bin/env python3
from __future__ import annotations

import copy
import gzip
import hashlib
import io
import json
import pathlib
import shutil
import subprocess
import sys
import tarfile
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from build_release_package import build_release_bundle  # noqa: E402
from verify_public_release import (  # noqa: E402
    PublicReleaseVerificationError,
    expected_asset_names,
    validate_archive_and_manifest,
    validate_release_metadata,
    verify_public_release,
)

ROOT = pathlib.Path(__file__).resolve().parents[1]
REPOSITORY = "zhangpan-soft/FoldCanvas"
VERSION = "1.0.0"
TAG = "v" + VERSION
TAG_COMMIT = "a" * 40
STABLE_SOURCE_COMMIT = "6ed32f1ed2a48796f5c0e015205cd47249e1bcef"
STABLE_ARCHIVE_SHA256 = (
    "16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7"
)


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require_error(action, message: str) -> None:
    try:
        action()
    except PublicReleaseVerificationError:
        return
    raise AssertionError(message)


def write_metadata(path: pathlib.Path, assets: pathlib.Path, value: dict | None = None):
    if value is None:
        value = {
            "id": 123456,
            "tag_name": TAG,
            "draft": False,
            "prerelease": False,
            "html_url": f"https://github.com/{REPOSITORY}/releases/tag/{TAG}",
            "assets": [
                {
                    "name": item.name,
                    "size": item.stat().st_size,
                    "digest": "sha256:" + sha256(item),
                    "state": "uploaded",
                }
                for item in sorted(assets.iterdir(), key=lambda item: item.name)
            ],
        }
    path.write_text(
        json.dumps(value, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def export_stable_source(destination: pathlib.Path) -> None:
    archive = subprocess.run(
        ["git", "archive", STABLE_SOURCE_COMMIT],
        cwd=ROOT,
        check=True,
        stdout=subprocess.PIPE,
    ).stdout
    subprocess.run(
        ["tar", "-x", "-C", str(destination)],
        input=archive,
        check=True,
    )


def main() -> int:
    contract_path = ROOT / "Documentation~" / "m17-stable-release.json"
    contract = json.loads(contract_path.read_text(encoding="utf-8"))
    if contract["packageVersion"] != VERSION:
        raise AssertionError("M17 stable release version drifted")
    if contract["releaseCandidate"]["archiveSha256"] != (
        "72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707"
    ):
        raise AssertionError("Published RC2 archive identity drifted")

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m17-public-") as temporary:
        temporary_path = pathlib.Path(temporary)
        asset_directory = temporary_path / "assets"
        asset_directory.mkdir()
        stable_source = temporary_path / "stable-source"
        stable_source.mkdir()
        export_stable_source(stable_source)
        archive, manifest, evidence = build_release_bundle(
            asset_directory,
            TAG,
            root=stable_source,
        )
        if sha256(archive) != STABLE_ARCHIVE_SHA256:
            raise AssertionError("rebuilt stable archive differs from public v1.0.0")
        checksum = archive.with_suffix(archive.suffix + ".sha256")
        if sorted(path.name for path in asset_directory.iterdir()) != (
            expected_asset_names(VERSION)
        ):
            raise AssertionError("Built public asset allowlist differs")

        metadata_path = temporary_path / "release.json"
        write_metadata(metadata_path, asset_directory)
        first = verify_public_release(
            asset_directory,
            metadata_path,
            contract_path,
            REPOSITORY,
            TAG,
            TAG_COMMIT,
        )
        second = verify_public_release(
            asset_directory,
            metadata_path,
            contract_path,
            REPOSITORY,
            TAG,
            TAG_COMMIT,
        )
        if first != second or first["qualified"] is not True:
            raise AssertionError("Public release verification is not deterministic")
        if (
            first["assetCount"] != 4
            or first["archiveSha256"] != sha256(archive)
            or first["stableRelease"] is not True
        ):
            raise AssertionError("Public release verification report differs")
        if "url" in json.dumps(first).lower():
            raise AssertionError("Public report must not retain a transport URL")

        noncanonical_archive = temporary_path / "noncanonical.tgz"
        with tarfile.open(archive, mode="r:gz") as source_archive:
            with noncanonical_archive.open("wb") as raw:
                with gzip.GzipFile(
                    filename="noncanonical.tgz",
                    mode="wb",
                    fileobj=raw,
                    compresslevel=9,
                    mtime=1,
                ) as compressed:
                    with tarfile.open(
                        fileobj=compressed,
                        mode="w",
                        format=tarfile.USTAR_FORMAT,
                    ) as target_archive:
                        for member in source_archive.getmembers():
                            source = source_archive.extractfile(member)
                            if source is None:
                                raise AssertionError("Could not read source archive")
                            target_archive.addfile(
                                member,
                                io.BytesIO(source.read()),
                            )
        noncanonical_manifest = json.loads(
            manifest.read_text(encoding="utf-8")
        )
        noncanonical_manifest["archiveFile"] = noncanonical_archive.name
        noncanonical_manifest["archiveSha256"] = sha256(noncanonical_archive)
        require_error(
            lambda: validate_archive_and_manifest(
                noncanonical_archive,
                noncanonical_manifest,
                VERSION,
            ),
            "Noncanonical gzip metadata was accepted",
        )

        require_error(
            lambda: verify_public_release(
                asset_directory,
                metadata_path,
                contract_path,
                REPOSITORY,
                "v1.0.0-rc.2",
                TAG_COMMIT,
            ),
            "Wrong public tag was accepted",
        )

        asset_directory_link = temporary_path / "assets-link"
        asset_directory_link.symlink_to(asset_directory, target_is_directory=True)
        require_error(
            lambda: verify_public_release(
                asset_directory_link,
                metadata_path,
                contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "Symlinked public asset directory was accepted",
        )

        missing_directory = temporary_path / "missing"
        shutil.copytree(asset_directory, missing_directory)
        (missing_directory / checksum.name).unlink()
        require_error(
            lambda: verify_public_release(
                missing_directory,
                metadata_path,
                contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "Missing public asset was accepted",
        )

        extra_directory = temporary_path / "extra"
        shutil.copytree(asset_directory, extra_directory)
        (extra_directory / "unexpected.txt").write_text("unexpected\n")
        require_error(
            lambda: verify_public_release(
                extra_directory,
                metadata_path,
                contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "Extra public asset was accepted",
        )

        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        duplicate_metadata = copy.deepcopy(metadata)
        duplicate_metadata["assets"].append(copy.deepcopy(metadata["assets"][0]))
        duplicate_path = temporary_path / "duplicate.json"
        write_metadata(duplicate_path, asset_directory, duplicate_metadata)
        require_error(
            lambda: verify_public_release(
                asset_directory,
                duplicate_path,
                contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "Duplicate GitHub asset metadata was accepted",
        )

        bad_digest_metadata = copy.deepcopy(metadata)
        bad_digest_metadata["assets"][0]["digest"] = "sha256:" + "0" * 64
        bad_digest_path = temporary_path / "bad-digest.json"
        write_metadata(bad_digest_path, asset_directory, bad_digest_metadata)
        require_error(
            lambda: verify_public_release(
                asset_directory,
                bad_digest_path,
                contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "Bad GitHub asset digest was accepted",
        )

        stale_directory = temporary_path / "stale"
        shutil.copytree(asset_directory, stale_directory)
        stale_evidence = stale_directory / evidence.name
        value = json.loads(stale_evidence.read_text(encoding="utf-8"))
        value["state"] = "verified"
        stale_evidence.write_text(json.dumps(value) + "\n", encoding="utf-8")
        stale_metadata_path = temporary_path / "stale-release.json"
        write_metadata(stale_metadata_path, stale_directory)
        require_error(
            lambda: verify_public_release(
                stale_directory,
                stale_metadata_path,
                contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "Stale stable evidence was accepted",
        )

        prerelease_metadata = copy.deepcopy(metadata)
        prerelease_metadata["tag_name"] = "v1.0.0-rc.2"
        prerelease_metadata["prerelease"] = True
        prerelease_metadata["html_url"] = (
            f"https://github.com/{REPOSITORY}/releases/tag/v1.0.0-rc.2"
        )
        release_id = validate_release_metadata(
            prerelease_metadata,
            REPOSITORY,
            "v1.0.0-rc.2",
            TAG_COMMIT,
            {item.name: item for item in asset_directory.iterdir()},
            True,
        )
        if release_id != prerelease_metadata["id"]:
            raise AssertionError("Historical prerelease metadata was not accepted")
        require_error(
            lambda: validate_release_metadata(
                metadata,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
                {item.name: item for item in asset_directory.iterdir()},
                True,
            ),
            "Stable metadata was accepted as a prerelease",
        )

    print("M17 public release verifier validation passed.")
    print(f"Stable {TAG}; exact four-asset allowlist; RC2 identity frozen.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
