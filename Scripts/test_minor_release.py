#!/usr/bin/env python3
from __future__ import annotations

import copy
import hashlib
import json
import pathlib
import re
import shutil
import tempfile

from build_release_package import build_release_bundle
from verify_public_release import (
    PublicReleaseVerificationError,
    expected_asset_names,
    verify_public_release,
)


ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTRACT_PATH = ROOT / "Documentation~" / "m25-minor-release.json"
VERSION = "1.1.0"
TAG = "v1.1.0"
REPOSITORY = "zhangpan-soft/FoldCanvas"
TAG_COMMIT = "c" * 40


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def rejected(action, message: str) -> None:
    try:
        action()
    except (PublicReleaseVerificationError, ValueError):
        return
    raise AssertionError(message)


def metadata(path: pathlib.Path, assets: pathlib.Path, value: dict | None = None):
    if value is None:
        value = {
            "id": 500000001,
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


def main() -> int:
    contract = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
    package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    api = json.loads(
        (ROOT / "Documentation~" / "public-runtime-api.json").read_text(
            encoding="utf-8"
        )
    )
    corpus = json.loads(
        (ROOT / "Documentation~" / "m24-production-corpus.json").read_text(
            encoding="utf-8"
        )
    )
    require(
        package["version"] == VERSION
        and contract["format"] == "foldcanvas-minor-release"
        and contract["packageVersion"] == VERSION
        and contract["tag"] == TAG
        and contract["stableRelease"] is True
        and contract["minorRelease"] is True,
        "M25 minor contract header differs",
    )
    require(
        contract["stableBaseline"]["packageVersion"] == "1.0.1"
        and contract["stableBaseline"]["archiveSha256"]
        == "4188d23b18b924f6642f9e4eabbc15500fb60fb3d3916f23857a5a19966e1de5",
        "M25 immutable baseline differs",
    )
    normalized = [
        signature.replace(VERSION, contract["publicRuntimeApi"]["normalizedVersionToken"])
        for signature in api["signatures"]
    ]
    require(
        api["packageVersion"] == VERSION
        and api["signatureCount"] == 808
        and hashlib.sha256(("\n".join(normalized) + "\n").encode()).hexdigest()
        == contract["publicRuntimeApi"]["normalizedSha256"],
        "M25 Runtime API shape differs",
    )
    canonical_cases = json.dumps(
        corpus["cases"],
        ensure_ascii=True,
        sort_keys=True,
        separators=(",", ":"),
    ).encode()
    require(
        corpus["packageVersion"] == VERSION
        and [case["id"] for case in corpus["cases"]]
        == contract["productionCorpus"]["caseIds"]
        and hashlib.sha256(canonical_cases).hexdigest()
        == contract["productionCorpus"]["casesSha256"],
        "M25 production corpus differs",
    )
    off_grid = next(case for case in corpus["cases"] if case["id"] == "off-grid-fold")
    require(
        off_grid["success"] is True
        and off_grid["renderVertices"] == 7
        and off_grid["triangles"] == 6
        and off_grid["errorDiagnosticCode"] == "",
        "M25 off-grid feature evidence differs",
    )
    unity_workflow = (
        ROOT / ".github" / "workflows" / "unity-tests.yml"
    ).read_text(encoding="utf-8")
    source_upgrade_job = unity_workflow.split(
        "  unity-source-upgrade-tests:", 1
    )[1]
    require(
        source_upgrade_job.count("Documentation~/m25-minor-release.json") == 2
        and "Documentation~/m23-patch-release.json" not in source_upgrade_job,
        "M25 hosted source-first upgrade contract differs",
    )
    cases = 1

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m25-") as temporary:
        root = pathlib.Path(temporary)
        assets = root / "assets"
        assets.mkdir()
        archive, manifest, evidence = build_release_bundle(assets, TAG)
        require(
            sorted(path.name for path in assets.iterdir())
            == expected_asset_names(VERSION),
            "M25 release asset allowlist differs",
        )
        evidence_value = json.loads(evidence.read_text(encoding="utf-8"))
        require(
            evidence_value["format"] == "foldcanvas-minor-release-evidence"
            and evidence_value["minorRelease"] is True
            and evidence_value["patchRelease"] is False
            and evidence_value["contract"]["path"]
            == "Documentation~/m25-minor-release.json"
            and evidence_value["stableBaseline"] == contract["stableBaseline"],
            "M25 release evidence differs",
        )
        release_path = root / "release.json"
        metadata(release_path, assets)
        first = verify_public_release(
            assets, release_path, CONTRACT_PATH, REPOSITORY, TAG, TAG_COMMIT
        )
        second = verify_public_release(
            assets, release_path, CONTRACT_PATH, REPOSITORY, TAG, TAG_COMMIT
        )
        require(
            first == second
            and first["qualified"] is True
            and first["archiveSha256"] == sha256(archive),
            "M25 public verifier differs",
        )

        prerelease = json.loads(release_path.read_text(encoding="utf-8"))
        prerelease["prerelease"] = True
        prerelease_path = root / "prerelease.json"
        metadata(prerelease_path, assets, prerelease)
        rejected(
            lambda: verify_public_release(
                assets,
                prerelease_path,
                CONTRACT_PATH,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "M25 accepted prerelease metadata",
        )
        cases += 1

        stale = copy.deepcopy(contract)
        stale["stableBaseline"]["archiveSha256"] = "0" * 64
        stale_path = root / "stale-contract.json"
        stale_path.write_text(json.dumps(stale) + "\n", encoding="utf-8")
        rejected(
            lambda: verify_public_release(
                assets,
                release_path,
                stale_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "M25 accepted stale baseline evidence",
        )
        cases += 1

        for wrong_tag in ("v1.0.1", "v1.1.1", "1.1.0"):
            rejected(
                lambda wrong_tag=wrong_tag: build_release_bundle(root, wrong_tag),
                f"M25 accepted mismatched tag {wrong_tag}",
            )
            cases += 1

        drift_root = root / "drift-root"
        shutil.copytree(
            ROOT,
            drift_root,
            ignore=shutil.ignore_patterns(".git", "Project~", "Library", "Logs"),
        )
        drift_contract = drift_root / "Documentation~" / "m25-minor-release.json"
        drift_value = json.loads(drift_contract.read_text(encoding="utf-8"))
        drift_value["packageVersion"] = "1.1.1"
        drift_contract.write_text(json.dumps(drift_value) + "\n", encoding="utf-8")
        rejected(
            lambda: build_release_bundle(
                root / "drift-output", TAG, root=drift_root
            ),
            "M25 accepted contract/package drift",
        )
        cases += 1

    for field in (
        "requiredPrePublicationGates",
        "requiredPostPublicationGates",
        "escalations",
    ):
        require(
            contract[field] == sorted(set(contract[field])),
            f"M25 {field} must be unique and ordinal",
        )
    print(f"M25 minor release validation passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
