#!/usr/bin/env python3
"""Validate the release-excluded Roll handedness review deterministically."""

from __future__ import annotations

import argparse
import pathlib
import re
import sys
import xml.etree.ElementTree as ET
from collections.abc import Mapping


ROOT = pathlib.Path(__file__).resolve().parents[1]
DEFAULT_SVG = (
    ROOT / "Docs" / "Community" / "GeometryReviews" / "roll-handedness.svg"
)
DEFAULT_GUIDE = (
    ROOT / "Docs" / "Community" / "GeometryReviews" / "roll-handedness.md"
)

FORMULA = "startAngleDegrees - t * angleDegrees"
DEGREES_FORMULA = "thetaDegrees = " + FORMULA
RADIANS_FORMULA = "thetaRadians = radians(thetaDegrees)"
EXPECTED_SWEEPS: Mapping[str, Mapping[str, str]] = {
    "roll-u-negative": {
        "data-angle-degrees": "-360",
        "data-cylinder-axis": "CurrentV",
        "data-direction": "u",
        "data-min-radial": "-CurrentU",
        "data-quarter-radial": "+CurrentNormal",
        "data-radial-orientation": "inward",
        "data-selected-axis": "CurrentU",
        "data-start-angle-degrees": "0",
        "data-theta-degrees-expression": FORMULA,
    },
    "roll-u-positive": {
        "data-angle-degrees": "+360",
        "data-cylinder-axis": "CurrentV",
        "data-direction": "u",
        "data-min-radial": "-CurrentU",
        "data-quarter-radial": "-CurrentNormal",
        "data-radial-orientation": "outward",
        "data-selected-axis": "CurrentU",
        "data-start-angle-degrees": "0",
        "data-theta-degrees-expression": FORMULA,
    },
    "roll-v-negative": {
        "data-angle-degrees": "-360",
        "data-cylinder-axis": "CurrentU",
        "data-direction": "v",
        "data-min-radial": "-CurrentV",
        "data-quarter-radial": "+CurrentNormal",
        "data-radial-orientation": "inward",
        "data-selected-axis": "CurrentV",
        "data-start-angle-degrees": "0",
        "data-theta-degrees-expression": FORMULA,
    },
    "roll-v-positive": {
        "data-angle-degrees": "+360",
        "data-cylinder-axis": "CurrentU",
        "data-direction": "v",
        "data-min-radial": "-CurrentV",
        "data-quarter-radial": "-CurrentNormal",
        "data-radial-orientation": "outward",
        "data-selected-axis": "CurrentV",
        "data-start-angle-degrees": "0",
        "data-theta-degrees-expression": FORMULA,
    },
}

EXPECTED_CLAIMS: Mapping[str, tuple[str, ...]] = {
    "MATERIAL-SEPARATION": (
        "Roll_PreservesSourceUvProvenanceTopologyAndBoundaries",
    ),
    "NEGATIVE-WINDING": (
        "NegativeFullRoll_ReversesOrientationPredictably",
    ),
    "POSITIVE-WINDING": (
        "PositiveFullRoll_HasOutwardWinding",
    ),
    "ROLL-FORMULA": (
        "foldscript-field-reference.md",
        "compiler-pipeline.md",
        "Runtime/Compiler/RollExecutor.cs",
    ),
    "ROLL-U-AXIS": ("RollU_And_RollV_HaveDocumentedHandedness",),
    "ROLL-V-AXIS": ("RollU_And_RollV_HaveDocumentedHandedness",),
    "UV-READABILITY": (
        "PositiveRoll_CanonicalExteriorReadsSourceUFromLeftToRight",
    ),
}

UNSAFE_TAGS = frozenset(
    {
        "audio",
        "embed",
        "foreignObject",
        "iframe",
        "image",
        "object",
        "script",
        "style",
        "video",
    }
)
EXTERNAL_REFERENCE = re.compile(
    r"(?i)(?:https?:|file:|data:|javascript:|//|@font-face)"
)
LOCAL_URL = re.compile(r"url\(#[A-Za-z_][A-Za-z0-9_.:-]*\)")
CLAIM_ROW = re.compile(
    r"^\|\s*(?P<claim>[A-Z]+(?:-[A-Z]+)+)\s*\|.*?\|(?P<evidence>.*?)\|\s*$"
)
MARKDOWN_IMAGE = re.compile(r"!\[[^\]]*\]\((?P<target>[^)]+)\)")


def local_name(value: str) -> str:
    return value.rsplit("}", 1)[-1]


def collect_review_errors(svg_text: str, guide_text: str) -> list[str]:
    errors: list[str] = []
    if re.search(r"(?i)<!DOCTYPE|<!ENTITY", svg_text):
        errors.append("SVG contains forbidden XML declaration")
    try:
        root = ET.fromstring(svg_text)
    except ET.ParseError:
        errors.append("SVG XML is invalid")
        return sorted(set(errors))

    if local_name(root.tag) != "svg":
        errors.append("SVG root element must be svg")
    if root.get("viewBox") != "0 0 1600 1180":
        errors.append("SVG viewBox must remain 0 0 1600 1180")
    if root.get("data-contract-version") != "1":
        errors.append("SVG data-contract-version must remain 1")

    ids: dict[str, ET.Element] = {}
    for element in root.iter():
        tag = local_name(element.tag)
        if tag in UNSAFE_TAGS:
            errors.append(f"SVG contains forbidden element: {tag}")

        element_id = element.get("id")
        if element_id:
            if element_id in ids:
                errors.append(f"SVG id is duplicated: {element_id}")
            else:
                ids[element_id] = element

        for raw_name, value in sorted(element.attrib.items()):
            name = local_name(raw_name)
            if name.lower().startswith("on"):
                errors.append(f"SVG contains event handler attribute: {name}")
            if name == "href" and not value.startswith("#"):
                errors.append(f"SVG contains non-local href reference: {name}")
            if EXTERNAL_REFERENCE.search(value):
                errors.append(
                    f"SVG contains external or executable reference: {name}"
                )
            if "url(" in value and not LOCAL_URL.fullmatch(value):
                errors.append(f"SVG contains non-local url reference: {name}")
        if EXTERNAL_REFERENCE.search(element.text or ""):
            errors.append(f"SVG contains external or executable text: {tag}")

    for panel_id, expected in EXPECTED_SWEEPS.items():
        element = ids.get(panel_id)
        if element is None:
            errors.append(f"SVG is missing signed-sweep panel: {panel_id}")
            continue
        for name, value in expected.items():
            if element.get(name) != value:
                errors.append(
                    f"SVG {panel_id} {name} must be {value!r}"
                )

    source = ids.get("source-frame")
    if source is None:
        errors.append("SVG is missing source-frame")
    else:
        if source.get("data-front-normal") != "CurrentNormal":
            errors.append("SVG source-frame front normal must be CurrentNormal")
        if source.get("data-boundary-order") != (
            "uMin:bottom-to-top;uMax:bottom-to-top;"
            "vMin:left-to-right;vMax:left-to-right"
        ):
            errors.append("SVG source-frame boundary order is invalid")

    visible_text = " ".join(part.strip() for part in root.itertext() if part.strip())
    for token in (
        "CurrentU",
        "CurrentV",
        "CurrentNormal",
        "FRONT / 正面",
        "Cull Off",
        "radial OUT",
        "radial IN",
        "source +U / UV reads",
        "uMin",
        "uMax",
        "vMin",
        "vMax",
        "radians(",
    ):
        if token not in visible_text:
            errors.append(f"SVG visible text is missing: {token}")

    formula_footer = ids.get("formula-footer")
    if formula_footer is None:
        errors.append("SVG formula metadata is missing or stale")
    else:
        if formula_footer.get("data-degrees-formula") != DEGREES_FORMULA:
            errors.append("SVG degrees formula metadata is missing or stale")
        if formula_footer.get("data-radians-formula") != RADIANS_FORMULA:
            errors.append("SVG radians formula metadata is missing or stale")

    if DEGREES_FORMULA not in guide_text:
        errors.append("guide is missing the exact Roll theta formula")
    if "theta = radians(thetaDegrees)" not in guide_text:
        errors.append("guide is missing the Roll degrees-to-radians conversion")
    if "Cull Off" not in guide_text or "does not repair triangle winding" not in guide_text:
        errors.append("guide must separate two-sided visibility from winding")
    if "[FoldCanvas Roll-U and Roll-V signed-sweep handedness diagram](roll-handedness.svg)" not in guide_text:
        errors.append("guide must embed the local Roll SVG with meaningful alt text")
    for match in MARKDOWN_IMAGE.finditer(guide_text):
        if match.group("target").strip() != "roll-handedness.svg":
            errors.append("guide contains non-local image reference")

    claim_rows: dict[str, str] = {}
    for line in guide_text.splitlines():
        match = CLAIM_ROW.match(line)
        if match is None:
            continue
        claim = match.group("claim")
        if claim in claim_rows:
            errors.append(f"guide claim is duplicated: {claim}")
        else:
            claim_rows[claim] = match.group("evidence")

    for claim, evidence_tokens in EXPECTED_CLAIMS.items():
        evidence = claim_rows.get(claim)
        if evidence is None:
            errors.append(f"guide is missing evidence claim: {claim}")
            continue
        for token in evidence_tokens:
            if token not in evidence:
                errors.append(
                    f"guide claim {claim} is missing evidence: {token}"
                )
    for claim in sorted(set(claim_rows) - set(EXPECTED_CLAIMS)):
        errors.append(f"guide contains unreviewed evidence claim: {claim}")

    return sorted(set(errors))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--svg", type=pathlib.Path, default=DEFAULT_SVG)
    parser.add_argument("--guide", type=pathlib.Path, default=DEFAULT_GUIDE)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    errors = collect_review_errors(
        args.svg.read_text(encoding="utf-8"),
        args.guide.read_text(encoding="utf-8"),
    )
    if errors:
        print("Roll handedness review validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print("Roll handedness review validation passed: 4 signed sweeps, 7 evidence claims.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
