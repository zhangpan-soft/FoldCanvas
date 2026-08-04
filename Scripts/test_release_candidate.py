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


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    contract = json.loads(
        (ROOT / "Documentation~" / "m15-public-distribution.json").read_text(
            encoding="utf-8"
        )
    )
    public_api = json.loads(
        (ROOT / "Documentation~" / "public-runtime-api.json").read_text(
            encoding="utf-8"
        )
    )
    corpus = json.loads(
        (ROOT / "Documentation~" / "m11-production-corpus.json").read_text(
            encoding="utf-8"
        )
    )

    require(package["version"] == "1.0.0-rc.2", "M15 candidate version drifted")
    require(package["unity"] == "6000.3", "Unity major/minor minimum drifted")
    require(package["unityRelease"] == "20f1", "Unity exact release drifted")
    require(
        contract["candidateVersion"] == package["version"],
        "Contract version mismatch",
    )
    require(contract["candidateTag"] == "v" + package["version"], "Tag mismatch")
    require(contract["stableRelease"] is False, "M15 must remain a pre-release")
    require(contract["foldScriptVersion"] == "0.1", "FoldScript version drifted")
    require(
        contract["unityVersion"] == "6000.3.20f1",
        "Qualified Unity row drifted",
    )
    require(
        public_api["packageVersion"] == package["version"],
        "API version mismatch",
    )
    require(
        corpus["packageVersion"] == package["version"],
        "Corpus version mismatch",
    )
    require(
        contract["publicRuntimeApi"]["signatureCount"] == public_api["signatureCount"]
        and contract["publicRuntimeApi"]["sha256"] == public_api["sha256"],
        "Frozen public Runtime API evidence mismatch",
    )
    require(
        contract["productionCorpus"]["caseIds"]
        == [case["id"] for case in corpus["cases"]],
        "Production corpus identity mismatch",
    )

    digest_pattern = re.compile(r"[0-9a-f]{64}")
    fixture_ids: list[str] = []
    for fixture in contract["foldScriptFixtures"]:
        fixture_ids.append(fixture["id"])
        path = ROOT / fixture["path"]
        require(path.is_file(), f"Missing FoldScript fixture: {fixture['path']}")
        require(
            sha256(path) == fixture["sourceSha256"],
            f"Fixture source hash drifted: {fixture['id']}",
        )
        canonical = fixture["canonicalSha256"]
        require(
            digest_pattern.fullmatch(canonical) is not None
            and canonical != "0" * 64,
            f"Fixture canonical hash is not frozen: {fixture['id']}",
        )
    require(
        fixture_ids == sorted(set(fixture_ids)),
        "Fixture IDs must be unique and ordinal",
    )

    required_gates = contract["requiredGates"]
    require(
        required_gates == sorted(set(required_gates)),
        "Required gates must be unique and ordinal",
    )
    require(
        contract["rollback"]
        == {
            "packageVersion": "1.0.0-rc.1",
            "tag": "v1.0.0-rc.1",
            "gitCommit": "a8c81e61175dafbc48d1750de7ef6823589517a6",
            "sourceAuthority": "2d-canvas-plus-foldscript",
        },
        "Rollback contract drifted",
    )
    require(
        contract["priorRelease"]
        == {
            "tag": "v1.0.0-rc.1",
            "mergeCommit": "a8c81e61175dafbc48d1750de7ef6823589517a6",
            "releaseId": 364684802,
            "archiveSha256": "ff3a065eec3a638701ff51d4f069684df4f075226305253ae04fd6ed2b250fdd",
            "checksumSha256": "eb835a1cac0ee0c5adb6b99511f6bb93739839543bee1fe0be3cbad989960725",
            "manifestSha256": "a0b9f19b9b69cace6b430b4546fc67b0f294dadb1efef6f8542b5e53ee5a9aca",
            "evidenceSha256": "c9603b475bbfa78300a27b64189188a337ca95ef333c8ad00effa7cf808e3c32",
            "assetCount": 4,
            "immutable": True,
        },
        "Published RC1 identity drifted",
    )
    require(
        contract["stableExit"] == {
            "targetVersion": "1.0.0",
            "minimumSoakHours": 168,
            "minimumScheduledLongRuns": 2,
            "status": "blocked",
            "blockers": [
                "candidate-not-published",
                "public-release-assets-unverified",
                "public-consumer-evidence-missing",
                "source-upgrade-evidence-missing",
                "minimum-soak-incomplete",
                "scheduled-long-runs-incomplete",
                "exact-head-audit-missing",
                "required-gates-incomplete",
            ],
        },
        "Stable exit must remain explicitly blocked",
    )

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m15-a-") as first_dir:
        with tempfile.TemporaryDirectory(prefix="foldcanvas-m15-b-") as second_dir:
            first = build_release_bundle(pathlib.Path(first_dir), "v1.0.0-rc.2")
            second = build_release_bundle(pathlib.Path(second_dir), "v1.0.0-rc.2")
            for first_path, second_path in zip(first, second):
                require(
                    first_path.read_bytes() == second_path.read_bytes(),
                    f"M15 release output is not deterministic: {first_path.name}",
                )
            try:
                build_release_bundle(pathlib.Path(second_dir), "v1.0.0")
            except ValueError:
                pass
            else:
                raise AssertionError(
                    "The M15 workflow must reject a final stable 1.0.0 tag"
                )
            for wrong_tag in ("v1.0.0-rc.1", "v1.0.0-rc.3", "v2.0.0-rc.2"):
                try:
                    build_release_bundle(pathlib.Path(second_dir), wrong_tag)
                except ValueError:
                    pass
                else:
                    raise AssertionError(
                        "The M15 workflow must reject mismatched tag " + wrong_tag
                    )

    print("M15 public-distribution candidate validation passed.")
    print("Candidate 1.0.0-rc.2; Unity 6000.3.20f1; FoldScript 0.1.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
