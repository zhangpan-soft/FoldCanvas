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
        (ROOT / "Documentation~" / "m14-release-candidate.json").read_text(
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

    require(package["version"] == "1.0.0-rc.1", "M14 candidate version drifted")
    require(package["unity"] == "6000.3", "Unity major/minor minimum drifted")
    require(package["unityRelease"] == "20f1", "Unity exact release drifted")
    require(
        contract["candidateVersion"] == package["version"],
        "Contract version mismatch",
    )
    require(contract["stableRelease"] is False, "M14 must remain a pre-release")
    require(contract["foldScriptVersion"] == "0.1", "FoldScript version drifted")
    require(len(contract["unityMatrix"]) == 1, "M14 must claim one Unity row")
    unity = contract["unityMatrix"][0]
    require(
        unity["editorVersion"] == "6000.3.20f1",
        "Qualified Unity row drifted",
    )
    require(unity["packageUnity"] == package["unity"], "Unity minimum mismatch")
    require(
        unity["packageUnityRelease"] == package["unityRelease"],
        "Unity release mismatch",
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
            "packageVersion": "0.1.0-preview.21",
            "gitCommit": "d9434be",
            "sourceAuthority": "2d-canvas-plus-foldscript",
        },
        "Rollback contract drifted",
    )

    with tempfile.TemporaryDirectory(prefix="foldcanvas-m14-a-") as first_dir:
        with tempfile.TemporaryDirectory(prefix="foldcanvas-m14-b-") as second_dir:
            first = build_release_bundle(pathlib.Path(first_dir), "v1.0.0-rc.1")
            second = build_release_bundle(pathlib.Path(second_dir), "v1.0.0-rc.1")
            for first_path, second_path in zip(first, second):
                require(
                    first_path.read_bytes() == second_path.read_bytes(),
                    f"M14 release output is not deterministic: {first_path.name}",
                )
            try:
                build_release_bundle(pathlib.Path(second_dir), "v1.0.0")
            except ValueError:
                pass
            else:
                raise AssertionError(
                    "The M14 workflow must reject a final stable 1.0.0 tag"
                )

    print("M14 release-candidate validation passed.")
    print("Candidate 1.0.0-rc.1; Unity 6000.3.20f1; FoldScript 0.1.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
