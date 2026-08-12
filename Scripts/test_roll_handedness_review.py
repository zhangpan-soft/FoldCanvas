#!/usr/bin/env python3
"""Deterministic fixtures for the Roll handedness review validator."""

from __future__ import annotations

from validate_roll_handedness_review import (
    DEFAULT_GUIDE,
    DEFAULT_SVG,
    collect_review_errors,
)


def require_error(errors: list[str], expected: str) -> None:
    if expected not in errors:
        raise AssertionError(f"missing expected error {expected!r}: {errors}")


def main() -> int:
    svg = DEFAULT_SVG.read_text(encoding="utf-8")
    guide = DEFAULT_GUIDE.read_text(encoding="utf-8")
    original_svg = DEFAULT_SVG.read_bytes()
    original_guide = DEFAULT_GUIDE.read_bytes()

    if collect_review_errors(svg, guide):
        raise AssertionError("real repository Roll review must pass")

    if collect_review_errors("<svg", guide) != ["SVG XML is invalid"]:
        raise AssertionError("malformed XML must return one stable diagnostic")

    external_image = svg.replace(
        "</svg>",
        '<image href="https://example.invalid/proof.png"/></svg>',
    )
    external_errors = collect_review_errors(external_image, guide)
    require_error(external_errors, "SVG contains forbidden element: image")
    require_error(
        external_errors,
        "SVG contains non-local href reference: href",
    )
    require_error(
        external_errors,
        "SVG contains external or executable reference: href",
    )

    executable = svg.replace("</svg>", "<script>unsafe()</script></svg>")
    require_error(
        collect_review_errors(executable, guide),
        "SVG contains forbidden element: script",
    )

    external_font = svg.replace(
        "</svg>",
        "<style>@font-face { src: url(https://example.invalid/font.woff2); }</style></svg>",
    )
    external_font_errors = collect_review_errors(external_font, guide)
    require_error(external_font_errors, "SVG contains forbidden element: style")
    require_error(
        external_font_errors,
        "SVG contains external or executable text: style",
    )

    wrong_quarter = svg.replace(
        'id="roll-u-positive"',
        'id="roll-u-positive-mutated"',
        1,
    )
    require_error(
        collect_review_errors(wrong_quarter, guide),
        "SVG is missing signed-sweep panel: roll-u-positive",
    )

    missing_formula = guide.replace(
        "theta = startAngleDegrees - t * angleDegrees",
        "theta = undocumented",
    )
    require_error(
        collect_review_errors(svg, missing_formula),
        "guide is missing the exact Roll theta formula",
    )

    missing_test = guide.replace(
        "PositiveFullRoll_HasOutwardWinding",
        "RemovedPositiveWindingEvidence",
    )
    require_error(
        collect_review_errors(svg, missing_test),
        "guide claim POSITIVE-WINDING is missing evidence: "
        "PositiveFullRoll_HasOutwardWinding",
    )

    unknown_claim = guide.replace(
        "| MATERIAL-SEPARATION |",
        "| FUTURE-CLAIM |",
    )
    unknown_errors = collect_review_errors(svg, unknown_claim)
    require_error(
        unknown_errors,
        "guide contains unreviewed evidence claim: FUTURE-CLAIM",
    )
    require_error(
        unknown_errors,
        "guide is missing evidence claim: MATERIAL-SEPARATION",
    )

    multiple = collect_review_errors(executable, missing_test)
    if multiple != sorted(set(multiple)) or len(multiple) < 2:
        raise AssertionError(f"multiple diagnostics are not stable: {multiple}")

    if DEFAULT_SVG.read_bytes() != original_svg or DEFAULT_GUIDE.read_bytes() != original_guide:
        raise AssertionError("Roll review validator mutated its inputs")

    print("Roll handedness review validation tests passed: 9 cases.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
