#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import shutil
import tempfile

import validate_public_release_identities as validator


ROOT = pathlib.Path(__file__).resolve().parents[1]


def rejected(value: dict, label: str) -> None:
    try:
        validator.validate_ledger(value)
    except validator.PublicReleaseIdentityError:
        return
    raise AssertionError(f"release ledger accepted {label}")


def main() -> int:
    ledger = validator.load_object(validator.DEFAULT_LEDGER, "ledger")
    releases = validator.validate_ledger(ledger)
    if [release["tag"] for release in releases] != [
        "v1.0.0-rc.1",
        "v1.0.0-rc.2",
        "v1.0.0",
        "v1.0.1",
    ]:
        raise AssertionError("public release ledger order differs")
    cases = 1

    report = validator.validate(ROOT, validator.DEFAULT_LEDGER, rebuild_all=True)
    if (
        report["releaseCount"] != 4
        or report["rebuiltReleaseCount"] != 4
        or report["currentPackageVersion"] != "1.1.0"
        or report["currentVersionPublished"] is not False
        or report["valid"] is not True
    ):
        raise AssertionError("public release identity report differs")
    cases += 1

    for mutate, label in (
        (lambda value: value["releases"].reverse(), "non-semantic order"),
        (
            lambda value: value["releases"][1].update(
                packageVersion=value["releases"][0]["packageVersion"]
            ),
            "duplicate version",
        ),
    ):
        changed = copy.deepcopy(ledger)
        mutate(changed)
        rejected(changed, label)
        cases += 1

    for invalid_version in ("1.0.0-rc.01", "1.0.0-rc..1"):
        changed = copy.deepcopy(ledger)
        changed["releases"][0]["packageVersion"] = invalid_version
        changed["releases"][0]["tag"] = "v" + invalid_version
        rejected(changed, f"invalid SemVer prerelease {invalid_version}")
        cases += 1

    if not (
        validator.semantic_key("1.0.0-rc.2")
        < validator.semantic_key("1.0.0-rc.10")
        < validator.semantic_key("1.0.0")
    ):
        raise AssertionError("release ledger does not use SemVer prerelease order")
    cases += 1

    rebuild_mutations = [
        (
            lambda value: value["releases"][0].update(tagCommit="0" * 40),
            "wrong commit",
        )
    ]
    rebuild_mutations.extend(
        (
            lambda value, field=field: value["releases"][0].update(
                {field: "0" * 64}
            ),
            f"wrong {field}",
        )
        for field in validator.DIGEST_FIELDS
    )
    for mutate, label in rebuild_mutations:
        with tempfile.TemporaryDirectory(
            prefix="foldcanvas-ledger-negative-"
        ) as temporary:
            changed = copy.deepcopy(ledger)
            mutate(changed)
            path = pathlib.Path(temporary) / "ledger.json"
            path.write_text(
                json.dumps(changed, indent=2) + "\n",
                encoding="utf-8",
            )
            try:
                validator.validate(ROOT, path, rebuild_all=True)
            except validator.PublicReleaseIdentityError:
                pass
            else:
                raise AssertionError(f"release rebuild accepted {label}")
        cases += 1

    with tempfile.TemporaryDirectory(prefix="foldcanvas-identity-drift-") as temporary:
        root = pathlib.Path(temporary) / "root"
        shutil.copytree(
            ROOT,
            root,
            ignore=shutil.ignore_patterns(".git", "Project~", "artifacts"),
        )
        package_path = root / "package.json"
        package = json.loads(package_path.read_text(encoding="utf-8"))
        package["version"] = "1.0.1"
        package_path.write_text(json.dumps(package, indent=2) + "\n", encoding="utf-8")
        version_path = root / "Runtime" / "Data" / "FoldCanvasVersion.cs"
        version_path.write_text(
            version_path.read_text(encoding="utf-8").replace("1.1.0", "1.0.1"),
            encoding="utf-8",
        )
        # package builder only requires the current heading, which already exists.
        try:
            validator.validate(
                root,
                root / "Docs" / "Release" / "public-release-identities.json",
                rebuild_all=False,
            )
        except validator.PublicReleaseIdentityError as error:
            if "reuses immutable version" not in str(error):
                raise
        else:
            raise AssertionError("current tree reused published 1.0.1 bytes")
        cases += 1

    print(f"Immutable public release identity tests passed: {cases} cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
