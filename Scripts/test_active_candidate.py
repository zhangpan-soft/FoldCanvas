#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import sys
import tempfile


sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from validate_active_candidate import validate, write_github_output  # noqa: E402


ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTROL = json.loads(
    (ROOT / ".github" / "foldcanvas-active-candidate.json").read_text(
        encoding="utf-8"
    )
)
CONTRACT = json.loads(
    (ROOT / "Documentation~" / "m15-public-distribution.json").read_text(
        encoding="utf-8"
    )
)
PACKAGE = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))


def assert_rejected(mutator) -> None:
    value = copy.deepcopy(CONTROL)
    mutator(value)
    first = None
    second = None
    try:
        validate(value, CONTRACT, PACKAGE)
    except ValueError as error:
        first = str(error)
    try:
        validate(copy.deepcopy(value), CONTRACT, PACKAGE)
    except ValueError as error:
        second = str(error)
    if first is None or first != second:
        raise AssertionError("invalid candidate control was not deterministically rejected")


def main() -> int:
    values = validate(CONTROL, CONTRACT, PACKAGE)
    if values["candidate_tag"] != "v1.0.0-rc.2":
        raise AssertionError("active candidate tag was not preserved")

    historical_package = copy.deepcopy(PACKAGE)
    historical_package["version"] = CONTROL["candidateVersion"]
    if validate(CONTROL, CONTRACT, historical_package) != values:
        raise AssertionError("RC2 and stable lineage validation differ")

    for invalid_version in ("1.0.01", "1.1.0", "2.0.0", "9.9.9"):
        invalid_package = copy.deepcopy(PACKAGE)
        invalid_package["version"] = invalid_version
        first = second = None
        try:
            validate(CONTROL, CONTRACT, invalid_package)
        except ValueError as error:
            first = str(error)
        try:
            validate(CONTROL, CONTRACT, copy.deepcopy(invalid_package))
        except ValueError as error:
            second = str(error)
        if first is None or first != second:
            raise AssertionError(
                f"invalid package lineage was accepted: {invalid_version}"
            )

    assert_rejected(lambda value: value.update(active=False))
    assert_rejected(lambda value: value.update(candidateVersion="1.0.0-rc.1"))
    assert_rejected(lambda value: value.update(candidateTag="v1.0.0-rc.1"))
    assert_rejected(lambda value: value.update(candidateCommit="main"))
    assert_rejected(lambda value: value.update(archiveSha256="0" * 63))
    assert_rejected(lambda value: value.update(publishedAt="2026-08-04"))
    assert_rejected(
        lambda value: value["longRun"].update(casesPerSuite=0)
    )
    assert_rejected(lambda value: value["longRun"].update(seedHex="ABC"))
    assert_rejected(
        lambda value: value["stableExit"].update(minimumSoakHours=1)
    )

    with tempfile.TemporaryDirectory() as temporary:
        output = pathlib.Path(temporary) / "github-output"
        write_github_output(output, values)
        lines = output.read_text(encoding="utf-8").splitlines()
        if lines != sorted(lines) or len(lines) != len(values):
            raise AssertionError("GitHub outputs are not complete and deterministic")

    print("M16 active candidate control validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
