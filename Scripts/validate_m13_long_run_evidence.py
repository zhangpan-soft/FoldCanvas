#!/usr/bin/env python3
"""Validate hosted M13 Unity long-run evidence and emit a compact summary."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET


UNITY_VERSION = "6000.3.20f1"
SHA256 = re.compile(r"[0-9a-f]{64}")
SEED = re.compile(r"[0-9a-f]{16}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--test-results", required=True, type=pathlib.Path)
    parser.add_argument("--editor-log", required=True, type=pathlib.Path)
    parser.add_argument("--report", required=True, type=pathlib.Path)
    parser.add_argument("--replays", required=True, type=pathlib.Path)
    parser.add_argument("--resource-report", required=True, type=pathlib.Path)
    parser.add_argument("--environment", required=True, type=pathlib.Path)
    parser.add_argument("--expected-cases-per-suite", required=True, type=int)
    parser.add_argument("--expected-seed", required=True)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    return parser.parse_args()


def require_file(path: pathlib.Path) -> None:
    if not path.is_file() or path.stat().st_size <= 0:
        raise ValueError(f"required evidence is missing or empty: {path}")


def read_json(path: pathlib.Path) -> dict:
    require_file(path)
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"expected a JSON object: {path}")
    return value


def require_sha(value: object, field: str) -> str:
    if not isinstance(value, str) or SHA256.fullmatch(value) is None:
        raise ValueError(f"{field} must be a lowercase SHA-256")
    return value


def validate_test_results(path: pathlib.Path) -> dict[str, int | str]:
    require_file(path)
    root = ET.parse(path).getroot()
    values: dict[str, int | str] = {"result": root.get("result", "")}
    for name in ("total", "passed", "failed", "skipped", "inconclusive"):
        values[name] = int(root.get(name, "-1"))
    if (
        values["result"] != "Passed"
        or values["total"] < 1
        or values["passed"] != values["total"]
        or values["failed"] != 0
        or values["skipped"] != 0
        or values["inconclusive"] != 0
    ):
        raise ValueError(f"Unity Edit Mode XML is not fully green: {values}")
    return values


def validate_editor_log(path: pathlib.Path) -> None:
    require_file(path)
    text = path.read_text(encoding="utf-8", errors="replace")
    if f"Unity Editor version:    {UNITY_VERSION}" not in text:
        raise ValueError("Editor.log does not prove the required Unity version")
    if "Test run completed. Exiting with code 0" not in text:
        raise ValueError("Editor.log does not prove a completed green test run")


def replay_key(value: dict) -> tuple[str, str, int]:
    generator = value.get("generatorVersion")
    suite = value.get("suiteId")
    seed = value.get("seedHex")
    ordinal = value.get("ordinal")
    if (
        not isinstance(generator, str)
        or not generator.strip()
        or not isinstance(suite, str)
        or not suite.strip()
        or not isinstance(seed, str)
        or SEED.fullmatch(seed) is None
        or not isinstance(ordinal, int)
        or ordinal < 0
    ):
        raise ValueError("an unexpected case lacks a complete replay identity")
    return suite, seed, ordinal


def validate_report(
    report: dict,
    expected_cases_per_suite: int,
    expected_seed: str,
) -> tuple[str, set[tuple[str, str, int]]]:
    if (
        report.get("format") != "foldcanvas-robustness-report"
        or report.get("version") != "1"
        or report.get("complete") is not True
        or report.get("unityVersion") != UNITY_VERSION
        or report.get("casesPerSuite") != expected_cases_per_suite
        or report.get("seedHex") != expected_seed
    ):
        raise ValueError("robustness report header does not match the run")
    cases = report.get("cases")
    if not isinstance(cases, list):
        raise ValueError("robustness report cases must be an array")
    suite_count = report.get("suiteCount")
    case_count = report.get("caseCount")
    passed = report.get("passedCount")
    unexpected = report.get("unexpectedCount")
    if (
        not isinstance(suite_count, int)
        or suite_count < 1
        or case_count != suite_count * expected_cases_per_suite
        or len(cases) != case_count
        or passed + unexpected != case_count
    ):
        raise ValueError("robustness report counts are inconsistent")
    failed_keys = {
        replay_key(item)
        for item in cases
        if isinstance(item, dict) and item.get("passed") is not True
    }
    if len(failed_keys) != unexpected:
        raise ValueError("unexpected cases are duplicated or miscounted")
    return require_sha(report.get("semanticSha256"), "report semanticSha256"), failed_keys


def validate_replays(
    replays: dict,
    report_sha: str,
    failed_keys: set[tuple[str, str, int]],
) -> None:
    records = replays.get("records")
    if (
        replays.get("format") != "foldcanvas-robustness-replays"
        or replays.get("version") != "1"
        or replays.get("reportSemanticSha256") != report_sha
        or not isinstance(records, list)
        or replays.get("unexpectedCount") != len(records)
    ):
        raise ValueError("replay document header or counts are inconsistent")
    replay_keys = {replay_key(item) for item in records if isinstance(item, dict)}
    if len(replay_keys) != len(records) or replay_keys != failed_keys:
        raise ValueError("replay records do not exactly cover unexpected cases")


def validate_resources(resource: dict) -> tuple[str, str]:
    scenarios = resource.get("scenarios")
    if (
        resource.get("format") != "foldcanvas-robustness-resource-report"
        or resource.get("version") != "1"
        or resource.get("complete") is not True
        or resource.get("unityVersion") != UNITY_VERSION
        or resource.get("scenarioCount") != 5
        or resource.get("passedScenarioCount") != 5
        or resource.get("failedScenarioCount") != 0
        or not isinstance(scenarios, list)
        or len(scenarios) != 5
    ):
        raise ValueError("resource report is not a complete 5/5 result")
    for scenario in scenarios:
        if (
            not isinstance(scenario, dict)
            or scenario.get("withinEnvelope") is not True
            or scenario.get("countsMatch") is not True
            or scenario.get("geometryHashMatches") is not True
            or scenario.get("managedMeasurementAvailable") is not True
            or scenario.get("medianMilliseconds", -1) < 0
            or scenario.get("medianManagedBytes", 0) <= 0
        ):
            raise ValueError("a resource scenario lacks complete green evidence")
    return (
        require_sha(resource.get("envelopeSha256"), "resource envelopeSha256"),
        require_sha(resource.get("semanticSha256"), "resource semanticSha256"),
    )


def validate_environment(
    environment: dict,
    report: dict,
    report_sha: str,
    envelope_sha: str,
    resource_sha: str,
) -> None:
    if (
        environment.get("format")
        != "foldcanvas-robustness-long-run-environment"
        or environment.get("version") != "1"
        or environment.get("complete") is not True
        or environment.get("unityVersion") != UNITY_VERSION
        or environment.get("generatorVersion") != report.get("generatorVersion")
        or environment.get("casesPerSuite") != report.get("casesPerSuite")
        or environment.get("suiteCount") != report.get("suiteCount")
        or environment.get("caseCount") != report.get("caseCount")
        or environment.get("seedHex") != report.get("seedHex")
        or environment.get("reportSemanticSha256") != report_sha
        or environment.get("resourceEnvelopeSha256") != envelope_sha
        or environment.get("resourceSemanticSha256") != resource_sha
    ):
        raise ValueError("environment metadata does not match report evidence")


def main() -> int:
    args = parse_args()
    if not 1 <= args.expected_cases_per_suite <= 256:
        raise ValueError("expected cases per suite must be between 1 and 256")
    expected_seed = args.expected_seed.lower()
    if SEED.fullmatch(expected_seed) is None:
        raise ValueError("expected seed must be exactly 16 hexadecimal digits")

    tests = validate_test_results(args.test_results)
    validate_editor_log(args.editor_log)
    report = read_json(args.report)
    replays = read_json(args.replays)
    resources = read_json(args.resource_report)
    environment = read_json(args.environment)
    report_sha, failed_keys = validate_report(
        report,
        args.expected_cases_per_suite,
        expected_seed,
    )
    validate_replays(replays, report_sha, failed_keys)
    envelope_sha, resource_sha = validate_resources(resources)
    validate_environment(
        environment,
        report,
        report_sha,
        envelope_sha,
        resource_sha,
    )

    summary = {
        "format": "foldcanvas-m13-long-run-validation",
        "version": "1",
        "unityVersion": UNITY_VERSION,
        "testTotal": tests["total"],
        "testPassed": tests["passed"],
        "testFailed": tests["failed"],
        "testSkipped": tests["skipped"],
        "testInconclusive": tests["inconclusive"],
        "casesPerSuite": report["casesPerSuite"],
        "suiteCount": report["suiteCount"],
        "caseCount": report["caseCount"],
        "unexpectedCount": report["unexpectedCount"],
        "seedHex": report["seedHex"],
        "reportSemanticSha256": report_sha,
        "resourceScenarioCount": resources["scenarioCount"],
        "resourcePassedScenarioCount": resources["passedScenarioCount"],
        "resourceEnvelopeSha256": envelope_sha,
        "resourceSemanticSha256": resource_sha,
        "complete": True,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(summary, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    print(
        "M13 long-run evidence passed: "
        f"{summary['testPassed']}/{summary['testTotal']} Unity tests, "
        f"{summary['caseCount']} robustness cases, "
        f"{summary['resourcePassedScenarioCount']}/"
        f"{summary['resourceScenarioCount']} resource envelopes."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, json.JSONDecodeError, ET.ParseError) as error:
        print(f"M13 long-run evidence validation failed: {error}", file=sys.stderr)
        raise SystemExit(1)
