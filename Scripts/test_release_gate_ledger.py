#!/usr/bin/env python3
from __future__ import annotations

import copy
import json
import pathlib
import sys


sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from validate_release_gate_ledger import validate  # noqa: E402


ROOT = pathlib.Path(__file__).resolve().parents[1]
LEDGER = json.loads(
    (ROOT / ".github" / "foldcanvas-rc2-gates.json").read_text(encoding="utf-8")
)
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


def assert_rejected(mutator, expected: str) -> None:
    messages = []
    for _ in range(2):
        value = copy.deepcopy(LEDGER)
        mutator(value)
        try:
            validate(value, CONTROL, CONTRACT)
        except ValueError as error:
            messages.append(str(error))
    if messages != [expected, expected]:
        raise AssertionError(f"ledger case was not deterministically rejected: {messages}")


def main() -> int:
    result = validate(LEDGER, CONTROL, CONTRACT)
    if result["satisfiedGates"] != CONTRACT["requiredGates"]:
        raise AssertionError("validated ledger did not preserve required gate order")
    if len(result["runIds"]) != 5:
        raise AssertionError("validated ledger run count drifted")

    assert_rejected(
        lambda value: value.update(candidateCommit="b" * 40),
        "release gate ledger candidateCommit does not match candidate",
    )
    assert_rejected(
        lambda value: value.update(sourceTree="c" * 40),
        "reviewed source and candidate merge trees are not identical",
    )
    assert_rejected(
        lambda value: value["audit"].update(decision="missing"),
        "release gate ledger audit is incomplete",
    )
    assert_rejected(
        lambda value: value["runs"][1].update(runId=value["runs"][0]["runId"]),
        "release gate ledger run IDs must be unique positive integers",
    )
    assert_rejected(
        lambda value: value["runs"][0].update(expectedHead="d" * 40),
        "release gate ledger run head is not reviewed",
    )
    assert_rejected(
        lambda value: value["runs"][0].update(gates=[]),
        "release gate ledger run gates are invalid",
    )
    assert_rejected(
        lambda value: value["runs"][0]["gates"].append("unity-editmode"),
        "release gate ledger does not cover every gate exactly once",
    )

    print("M16 release gate ledger validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
