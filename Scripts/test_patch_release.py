#!/usr/bin/env python3
from __future__ import annotations

import copy
import hashlib
import json
import pathlib
import re
import shutil
import sys
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from build_release_package import build_release_bundle  # noqa: E402
from verify_public_release import (  # noqa: E402
    PublicReleaseVerificationError,
    expected_asset_names,
    verify_public_release,
)

ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTRACT_PATH = ROOT / "Documentation~" / "m23-patch-release.json"
UNITY_WORKFLOW_PATH = ROOT / ".github" / "workflows" / "unity-tests.yml"
REPOSITORY = "zhangpan-soft/FoldCanvas"
VERSION = "1.0.1"
TAG = "v1.0.1"
TAG_COMMIT = "b" * 40


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def require_error(action, message: str) -> None:
    try:
        action()
    except (PublicReleaseVerificationError, ValueError):
        return
    raise AssertionError(message)


def write_metadata(path: pathlib.Path, assets: pathlib.Path, value: dict | None = None):
    if value is None:
        value = {
            "id": 400000001,
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
    cases = 1
    contract = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
    require(
        contract["format"] == "foldcanvas-patch-release"
        and contract["packageVersion"] == VERSION
        and contract["tag"] == TAG
        and contract["stableRelease"] is True
        and contract["patchRelease"] is True,
        "M23 patch contract header differs",
    )
    require(
        contract["publicAssets"] == expected_asset_names(VERSION),
        "M23 public asset allowlist differs",
    )
    require(
        contract["stableBaseline"]
        == {
            "packageVersion": "1.0.0",
            "tag": "v1.0.0",
            "tagCommit": "6ed32f1ed2a48796f5c0e015205cd47249e1bcef",
            "releaseId": 368700889,
            "publishedAt": "2026-08-11T16:22:02Z",
            "archiveSha256": "16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7",
            "checksumSha256": "a6a976f7fbc8b82d9d1aba232a7232ae86a25fbcf87a60008f968158d841e0d3",
            "manifestSha256": "06f223038b5f0e467cf7f1bc7010cd62add1704a59eadd62940b982ebe48aae1",
            "evidenceSha256": "7fe6d4cb2cc0294fcefe7c2cdbc5f061d9b361aecccdfca5a059b29d9e6630fd",
            "assetCount": 4,
            "immutable": True,
        },
        "M23 immutable stable baseline differs",
    )
    for field in (
        "requiredPrePublicationGates",
        "requiredPostPublicationGates",
        "escalations",
    ):
        require(
            contract[field] == sorted(set(contract[field])),
            f"M23 {field} must be unique and ordinal",
        )

    unity_workflow = UNITY_WORKFLOW_PATH.read_text(encoding="utf-8")
    source_upgrade_job = unity_workflow.split(
        "  unity-source-upgrade-tests:", 1
    )[1]
    require(
        source_upgrade_job.count("Documentation~/m23-patch-release.json") == 2,
        "Hosted source-first upgrade must use the M23 baseline and comparison contract",
    )
    require(
        "Documentation~/m17-stable-release.json" not in source_upgrade_job,
        "Hosted source-first patch upgrade retained the RC2-to-1.0.0 contract",
    )

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m23-") as temporary:
        root = pathlib.Path(temporary)
        assets = root / "assets"
        assets.mkdir()
        archive, manifest, evidence = build_release_bundle(assets, TAG)
        require(
            sorted(path.name for path in assets.iterdir())
            == expected_asset_names(VERSION),
            "M23 package bundle differs from exact four-asset allowlist",
        )
        evidence_value = json.loads(evidence.read_text(encoding="utf-8"))
        require(
            evidence_value["contract"]["path"]
            == "Documentation~/m23-patch-release.json"
            and evidence_value["stableBaseline"] == contract["stableBaseline"]
            and evidence_value["publication"] == contract["publication"],
            "M23 package evidence is not bound to its contract",
        )

        metadata_path = root / "release.json"
        write_metadata(metadata_path, assets)
        first = verify_public_release(
            assets, metadata_path, CONTRACT_PATH, REPOSITORY, TAG, TAG_COMMIT
        )
        second = verify_public_release(
            assets, metadata_path, CONTRACT_PATH, REPOSITORY, TAG, TAG_COMMIT
        )
        require(first == second, "M23 public verification is not deterministic")
        require(
            first["qualified"] is True
            and first["stableRelease"] is True
            and first["packageVersion"] == VERSION
            and first["archiveSha256"] == sha256(archive),
            "M23 public verification report differs",
        )

        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        prerelease = copy.deepcopy(metadata)
        prerelease["prerelease"] = True
        prerelease_path = root / "prerelease.json"
        write_metadata(prerelease_path, assets, prerelease)
        require_error(
            lambda: verify_public_release(
                assets,
                prerelease_path,
                CONTRACT_PATH,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "M23 accepted prerelease metadata",
        )
        cases += 1

        stale_contract = copy.deepcopy(contract)
        stale_contract["stableBaseline"]["archiveSha256"] = "0" * 64
        stale_contract_path = root / "stale-contract.json"
        stale_contract_path.write_text(json.dumps(stale_contract) + "\n")
        require_error(
            lambda: verify_public_release(
                assets,
                metadata_path,
                stale_contract_path,
                REPOSITORY,
                TAG,
                TAG_COMMIT,
            ),
            "M23 accepted a stale stable baseline contract",
        )
        cases += 1

        wrong_evidence = json.loads(evidence.read_text(encoding="utf-8"))
        wrong_evidence["stableBaseline"]["tag"] = "v1.0.0-rc.2"
        evidence.write_text(json.dumps(wrong_evidence) + "\n")
        write_metadata(metadata_path, assets)
        require_error(
            lambda: verify_public_release(
                assets, metadata_path, CONTRACT_PATH, REPOSITORY, TAG, TAG_COMMIT
            ),
            "M23 accepted stale patch evidence",
        )
        cases += 1

        for wrong_tag in ("v1.0.0", "v1.0.2", "1.0.1"):
            require_error(
                lambda wrong_tag=wrong_tag: build_release_bundle(root, wrong_tag),
                f"M23 accepted mismatched tag {wrong_tag}",
            )
            cases += 1

        drift_root = root / "drift-root"
        shutil.copytree(
            ROOT,
            drift_root,
            ignore=shutil.ignore_patterns(".git", "Project~", "Library", "Logs"),
        )
        drift_contract = drift_root / "Documentation~" / "m23-patch-release.json"
        drift_value = json.loads(drift_contract.read_text(encoding="utf-8"))
        drift_value["packageVersion"] = "1.0.2"
        drift_contract.write_text(json.dumps(drift_value) + "\n", encoding="utf-8")
        require_error(
            lambda: build_release_bundle(root / "drift-output", TAG, root=drift_root),
            "M23 accepted a patch contract that differs from package.json",
        )
        cases += 1

    require(
        re.fullmatch(r"[0-9a-f]{64}", contract["productionCorpus"]["casesSha256"])
        is not None,
        "M23 corpus digest is invalid",
    )
    print(f"M23 patch release validation passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
