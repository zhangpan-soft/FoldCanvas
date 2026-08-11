#!/usr/bin/env python3
from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import json
import pathlib
import re
import tarfile

ROOT = pathlib.Path(__file__).resolve().parents[1]

PACKAGE_DIRECTORIES = (
    "Documentation~",
    "Editor",
    "Runtime",
    "Samples~",
    "Schema",
    "Tests",
)

PACKAGE_FILES = (
    "CHANGELOG.md",
    "CHANGELOG.md.meta",
    "CODE_OF_CONDUCT.md",
    "CODE_OF_CONDUCT.md.meta",
    "CONTRIBUTING.md",
    "CONTRIBUTING.md.meta",
    "LICENSE.md",
    "LICENSE.md.meta",
    "NOTICE",
    "NOTICE.meta",
    "README.md",
    "README.md.meta",
    "README.zh-CN.md",
    "README.zh-CN.md.meta",
    "SECURITY.md",
    "SECURITY.md.meta",
    "SUPPORT.md",
    "SUPPORT.md.meta",
    "Editor.meta",
    "Runtime.meta",
    "Schema.meta",
    "Tests.meta",
    "package.json",
    "package.json.meta",
)

EXCLUDED_NAMES = {
    ".DS_Store",
}


def package_version() -> str:
    package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    version = package.get("version")
    if not isinstance(version, str) or not re.fullmatch(
        r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)"
        r"(?:-[0-9A-Za-z.-]+)?",
        version,
    ):
        raise ValueError("package.json contains an invalid package version")

    runtime_source = (
        ROOT / "Runtime" / "Data" / "FoldCanvasVersion.cs"
    ).read_text(encoding="utf-8")
    runtime_match = re.search(r'\bPackage\s*=\s*"([^"]+)"\s*;', runtime_source)
    if runtime_match is None or runtime_match.group(1) != version:
        raise ValueError("FoldCanvasVersion.Package must match package.json")

    changelog = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
    if f"## [{version}]" not in changelog:
        raise ValueError("CHANGELOG.md lacks the package version heading")
    return version


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def canonical_json_bytes(value: dict) -> bytes:
    return (
        json.dumps(
            value,
            ensure_ascii=False,
            indent=2,
            separators=(",", ": "),
        )
        + "\n"
    ).encode("utf-8")


def collect_package_files() -> list[pathlib.Path]:
    files: list[pathlib.Path] = []
    for relative in PACKAGE_FILES:
        path = ROOT / relative
        if not path.is_file():
            raise FileNotFoundError(f"Required package file is missing: {relative}")
        files.append(path)

    for directory_name in PACKAGE_DIRECTORIES:
        directory = ROOT / directory_name
        if not directory.is_dir():
            raise FileNotFoundError(
                f"Required package directory is missing: {directory_name}"
            )
        for path in directory.rglob("*"):
            if path.is_symlink():
                raise ValueError(
                    f"Release package cannot contain symlinks: {path.relative_to(ROOT)}"
                )
            if path.is_file() and path.name not in EXCLUDED_NAMES:
                files.append(path)

    unique = {path.relative_to(ROOT).as_posix(): path for path in files}
    return [unique[key] for key in sorted(unique)]


def build_archive(output_directory: pathlib.Path, tag: str | None = None) -> pathlib.Path:
    version = package_version()
    if tag is not None and tag != f"v{version}":
        raise ValueError(
            f"Release tag '{tag}' must exactly equal package version tag 'v{version}'"
        )

    output_directory.mkdir(parents=True, exist_ok=True)
    archive_path = output_directory / f"com.foldcanvas.core-{version}.tgz"
    temporary_path = archive_path.with_suffix(".tgz.tmp")
    files = collect_package_files()

    with temporary_path.open("wb") as raw:
        with gzip.GzipFile(
            filename="",
            mode="wb",
            fileobj=raw,
            compresslevel=9,
            mtime=0,
        ) as compressed:
            with tarfile.open(
                fileobj=compressed,
                mode="w",
                format=tarfile.USTAR_FORMAT,
            ) as archive:
                for path in files:
                    relative = path.relative_to(ROOT).as_posix()
                    data = path.read_bytes()
                    info = tarfile.TarInfo(f"package/{relative}")
                    info.size = len(data)
                    info.mode = 0o644
                    info.mtime = 0
                    info.uid = 0
                    info.gid = 0
                    info.uname = ""
                    info.gname = ""
                    archive.addfile(info, io.BytesIO(data))

    temporary_path.replace(archive_path)
    digest = hashlib.sha256(archive_path.read_bytes()).hexdigest()
    digest_path = archive_path.with_suffix(archive_path.suffix + ".sha256")
    digest_path.write_text(
        f"{digest}  {archive_path.name}\n",
        encoding="utf-8",
        newline="\n",
    )
    return archive_path


def build_file_manifest(
    output_directory: pathlib.Path,
    archive_path: pathlib.Path,
) -> pathlib.Path:
    version = package_version()
    archive_bytes = archive_path.read_bytes()
    entries = []
    for path in collect_package_files():
        relative = path.relative_to(ROOT).as_posix()
        data = path.read_bytes()
        entries.append(
            {
                "path": f"package/{relative}",
                "size": len(data),
                "sha256": sha256_bytes(data),
            }
        )

    document = {
        "format": "foldcanvas-release-file-manifest",
        "version": "1",
        "packageName": "com.foldcanvas.core",
        "packageVersion": version,
        "archiveFile": archive_path.name,
        "archiveSha256": sha256_bytes(archive_bytes),
        "fileCount": len(entries),
        "files": entries,
    }
    path = output_directory / f"com.foldcanvas.core-{version}.manifest.json"
    path.write_bytes(canonical_json_bytes(document))
    return path


def build_release_evidence(
    output_directory: pathlib.Path,
    archive_path: pathlib.Path,
    manifest_path: pathlib.Path,
) -> pathlib.Path:
    package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    contract_path = ROOT / "Documentation~" / "m17-stable-release.json"
    contract_bytes = contract_path.read_bytes()
    contract = json.loads(contract_bytes)
    public_api_path = ROOT / "Documentation~" / "public-runtime-api.json"
    public_api = json.loads(public_api_path.read_text(encoding="utf-8"))
    corpus_path = ROOT / "Documentation~" / "m11-production-corpus.json"
    corpus_bytes = corpus_path.read_bytes()
    corpus = json.loads(corpus_bytes)
    archive_bytes = archive_path.read_bytes()
    manifest_bytes = manifest_path.read_bytes()

    document = {
        "format": "foldcanvas-stable-release-evidence",
        "version": "1",
        "state": "built-unverified",
        "packageName": package["name"],
        "packageVersion": package["version"],
        "stableRelease": True,
        "unity": {
            "packageMinimum": package["unity"],
            "packageRelease": package["unityRelease"],
            "qualifiedEditorVersion": contract["unityVersion"],
        },
        "foldScriptVersion": contract["foldScriptVersion"],
        "publicRuntimeApi": {
            "signatureCount": public_api["signatureCount"],
            "sha256": public_api["sha256"],
        },
        "productionCorpus": {
            "caseCount": len(corpus["cases"]),
            "sha256": sha256_bytes(corpus_bytes),
        },
        "contract": {
            "path": "Documentation~/m17-stable-release.json",
            "sha256": sha256_bytes(contract_bytes),
        },
        "archive": {
            "file": archive_path.name,
            "sha256": sha256_bytes(archive_bytes),
        },
        "fileManifest": {
            "file": manifest_path.name,
            "sha256": sha256_bytes(manifest_bytes),
        },
        "requiredPrePublicationGates": contract[
            "requiredPrePublicationGates"
        ],
        "requiredPostPublicationGates": contract[
            "requiredPostPublicationGates"
        ],
        "rollback": contract["rollback"],
        "releaseCandidate": contract["releaseCandidate"],
        "stableQualification": contract["stableQualification"],
        "upgrade": contract["upgrade"],
        "publication": contract["publication"],
    }
    version = package["version"]
    path = output_directory / f"com.foldcanvas.core-{version}.evidence.json"
    path.write_bytes(canonical_json_bytes(document))
    return path


def build_release_bundle(
    output_directory: pathlib.Path,
    tag: str | None = None,
) -> tuple[pathlib.Path, pathlib.Path, pathlib.Path]:
    archive = build_archive(output_directory, tag)
    manifest = build_file_manifest(output_directory, archive)
    evidence = build_release_evidence(output_directory, archive, manifest)
    return archive, manifest, evidence


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Build a deterministic FoldCanvas UPM release archive."
    )
    parser.add_argument(
        "--output",
        type=pathlib.Path,
        default=ROOT / "artifacts" / "package",
    )
    parser.add_argument("--tag")
    args = parser.parse_args()
    archive, manifest, evidence = build_release_bundle(
        args.output.resolve(),
        args.tag,
    )
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    print(f"Built {archive}")
    print(f"SHA256 {digest}")
    print(f"File manifest {manifest}")
    print(f"Release evidence {evidence}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
