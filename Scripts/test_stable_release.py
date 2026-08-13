#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import pathlib
import re
import sys
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from build_release_package import build_release_bundle  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[1]


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def canonical_sha256(value: object) -> str:
    payload = json.dumps(
        value,
        ensure_ascii=True,
        sort_keys=True,
        separators=(",", ":"),
    ).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def main() -> int:
    package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    contract = json.loads(
        (ROOT / "Documentation~" / "m17-stable-release.json").read_text(
            encoding="utf-8"
        )
    )
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
    report_path = ROOT / "Documentation~" / "m17-stable-readiness-report.json"
    report = json.loads(report_path.read_text(encoding="utf-8"))

    require(package["version"] == "1.1.0", "Current minor version drifted")
    require(package["unity"] == "6000.3", "Unity major/minor minimum drifted")
    require(package["unityRelease"] == "20f1", "Unity exact release drifted")
    require(
        contract["format"] == "foldcanvas-stable-release"
        and contract["version"] == "1"
        and contract["packageName"] == package["name"]
        and contract["packageVersion"] == "1.0.0"
        and contract["tag"] == "v1.0.0"
        and contract["stableRelease"] is True,
        "Stable release contract header drifted",
    )
    require(
        contract["foldScriptVersion"] == "0.1"
        and contract["unityVersion"] == "6000.3.20f1",
        "Stable compatibility row drifted",
    )
    require(
        hashlib.sha256(report_path.read_bytes()).hexdigest()
        == contract["stableQualification"]["reportSha256"],
        "Stable readiness report digest drifted",
    )
    require(
        report["status"] == "ready"
        and report["targetVersion"] == contract["packageVersion"]
        and report["candidateCommit"]
        == contract["releaseCandidate"]["candidateCommit"]
        and report["soakHours"] >= 168
        and report["qualifyingScheduledLongRuns"] >= 2
        and report["satisfiedGateCount"] == report["requiredGateCount"]
        and report["openReleaseBlockerCount"] == 0
        and report["blockers"] == [],
        "Stable readiness proof is not ready",
    )

    require(
        api["packageVersion"] == package["version"]
        and api["signatureCount"]
        == contract["publicRuntimeApi"]["signatureCount"],
        "Stable public Runtime API header drifted",
    )
    token = contract["publicRuntimeApi"]["normalizedVersionToken"]
    normalized = [
        signature.replace(package["version"], token)
        for signature in api["signatures"]
    ]
    normalized_digest = hashlib.sha256(
        ("\n".join(normalized) + "\n").encode("utf-8")
    ).hexdigest()
    require(
        normalized_digest == contract["publicRuntimeApi"]["normalizedSha256"],
        "Stable public Runtime API shape differs from RC2",
    )

    case_ids = [case["id"] for case in corpus["cases"]]
    require(
        corpus["packageVersion"] == package["version"]
        and corpus["foldScriptVersion"] == "0.1"
        and corpus["unityVersion"] == "6000.3.20f1"
        and case_ids
        == [
            "cyclic-torus",
            "off-grid-fold",
            "planar-artwork",
            "production-cup",
            "registered-wave",
            "sphere-gores",
        ],
        "Current production corpus header drifted",
    )

    release_candidate = contract["releaseCandidate"]
    require(
        release_candidate
        == {
            "packageVersion": "1.0.0-rc.2",
            "tag": "v1.0.0-rc.2",
            "candidateCommit": "4db988ffac6dad4362d126001e5c9a67081ef2b7",
            "releaseId": 364762780,
            "publishedAt": "2026-08-04T09:51:46Z",
            "archiveSha256": "72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707",
            "checksumSha256": "9201b1aea39f8aa74532e74d46824b5386dcfd196e6ef5048a24f9db4219868a",
            "manifestSha256": "8c8873104a55810b596b40cf154ea45a98c8cbe55def8624dc05dddff03e08e7",
            "evidenceSha256": "501aba0dffbc870e0fadce5195f178aac654ee2c29fa20be80dae1fcbb4590bc",
            "assetCount": 4,
            "immutable": True,
        },
        "Immutable RC2 identity drifted",
    )
    require(
        contract["publicAssets"]
        == [
            "com.foldcanvas.core-1.0.0.evidence.json",
            "com.foldcanvas.core-1.0.0.manifest.json",
            "com.foldcanvas.core-1.0.0.tgz",
            "com.foldcanvas.core-1.0.0.tgz.sha256",
        ],
        "Stable public asset allowlist drifted",
    )
    for field in (
        "requiredPrePublicationGates",
        "requiredPostPublicationGates",
        "escalations",
    ):
        values = contract[field]
        require(values == sorted(set(values)), f"{field} must be unique and ordinal")
    require(
        contract["publication"]
        == {
            "githubPrerelease": False,
            "finalStableRelease": True,
            "externalMarketplace": False,
        },
        "Stable publication scope drifted",
    )

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m17-a-") as first_dir:
        with tempfile.TemporaryDirectory(prefix="foldcanvas-m17-b-") as second_dir:
            first = build_release_bundle(pathlib.Path(first_dir), "v1.1.0")
            second = build_release_bundle(pathlib.Path(second_dir), "v1.1.0")
            for first_path, second_path in zip(first, second):
                require(
                    first_path.read_bytes() == second_path.read_bytes(),
                    f"M17 release output is not deterministic: {first_path.name}",
                )
            evidence = json.loads(first[2].read_text(encoding="utf-8"))
            minor_contract = json.loads(
                (ROOT / "Documentation~" / "m25-minor-release.json").read_text(
                    encoding="utf-8"
                )
            )
            require(
                evidence["format"] == "foldcanvas-minor-release-evidence"
                and evidence["stableRelease"] is True
                and evidence["minorRelease"] is True
                and evidence["stableBaseline"]
                == minor_contract["stableBaseline"],
                "Stable baseline or minor evidence drifted",
            )
            require(
                evidence["publication"] == minor_contract["publication"],
                "Minor publication evidence differs from contract",
            )
            for wrong_tag in (
                "v1.0.0-rc.2",
                "v1.0.0",
                "v2.0.0",
                "1.1.0",
            ):
                try:
                    build_release_bundle(pathlib.Path(second_dir), wrong_tag)
                except ValueError:
                    pass
                else:
                    raise AssertionError(
                        "M17 release build accepted mismatched tag " + wrong_tag
                    )

    require(
        re.fullmatch(r"[0-9a-f]{64}", normalized_digest) is not None,
        "Normalized Runtime API digest is invalid",
    )
    print("M17 stable baseline and M25 minor compatibility validation passed.")
    print("Minor 1.1.0; stable baseline 1.0.1; Unity 6000.3.20f1; FoldScript 0.1.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
