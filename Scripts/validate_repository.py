#!/usr/bin/env python3
from __future__ import annotations

import json
import hashlib
import os
import pathlib
import re
import sys
import urllib.parse

ROOT = pathlib.Path(__file__).resolve().parents[1]
errors: list[str] = []
excluded_directories = {
    ".git",
    "Library",
    "Logs",
    "Temp",
    "UserSettings",
}


def repository_files(pattern: str) -> list[pathlib.Path]:
    matches: list[pathlib.Path] = []
    for directory, directories, files in os.walk(ROOT):
        directories[:] = sorted(
            name for name in directories if name not in excluded_directories
        )
        base = pathlib.Path(directory)
        for name in sorted(files):
            if pathlib.Path(name).match(pattern):
                matches.append(base / name)
    return matches


def read_json(relative_path: str) -> dict:
    path = ROOT / relative_path
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        errors.append(f"Invalid JSON: {relative_path}: {exc}")
        return {}

    if not isinstance(value, dict):
        errors.append(f"Expected a JSON object: {relative_path}")
        return {}

    return value


for path in repository_files("*.json"):
    try:
        json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        errors.append(f"Invalid JSON: {path.relative_to(ROOT)}: {exc}")

for path in sorted((ROOT / "Runtime").rglob("*.cs")):
    text = path.read_text(encoding="utf-8")
    if re.search(r"\bUnityEditor\b", text):
        errors.append(f"Runtime references UnityEditor: {path.relative_to(ROOT)}")

package = read_json("package.json")
if package.get("name") != "com.foldcanvas.core":
    errors.append("package.json name must remain com.foldcanvas.core")
if package.get("unity") != "6000.3":
    errors.append("package.json Unity baseline must remain 6000.3 during bootstrap")
if package.get("unityRelease") != "20f1":
    errors.append("M14 package.json Unity release must be exactly 20f1")
if package.get("dependencies"):
    errors.append("The M00 core package must not add package dependencies")

package_version = package.get("version")
changelog_text = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
if not isinstance(package_version, str) or (
    f"## [{package_version}]" not in changelog_text
):
    errors.append(
        "package.json version must have a matching CHANGELOG.md release heading"
    )

project_manifest = read_json("Project~/Packages/manifest.json")
if project_manifest.get("dependencies", {}).get("com.foldcanvas.core") != "file:../../":
    errors.append("Project~ must reference the repository root through file:../../")

project_version = (
    ROOT / "Project~" / "ProjectSettings" / "ProjectVersion.txt"
).read_text(encoding="utf-8")
if "m_EditorVersion: 6000.3.20f1" not in project_version.splitlines():
    errors.append("Project~ Editor version must remain 6000.3.20f1 during M00")

runtime_asmdef = read_json("Runtime/FoldCanvas.Runtime.asmdef")
editor_asmdef = read_json("Editor/FoldCanvas.Editor.asmdef")
test_asmdef = read_json("Tests/Editor/FoldCanvas.Tests.Editor.asmdef")
if runtime_asmdef.get("name") != "FoldCanvas.Runtime":
    errors.append("Runtime asmdef name must be FoldCanvas.Runtime")
if runtime_asmdef.get("references") != []:
    errors.append("Runtime asmdef must not reference another assembly during M00")
if editor_asmdef.get("references") != ["FoldCanvas.Runtime"]:
    errors.append("Editor asmdef must reference only FoldCanvas.Runtime during M00")
if editor_asmdef.get("includePlatforms") != ["Editor"]:
    errors.append("Editor asmdef must be restricted to the Editor platform")
if test_asmdef.get("references") != ["FoldCanvas.Runtime", "FoldCanvas.Editor"]:
    errors.append("Test asmdef must reference the runtime and editor assemblies")
if test_asmdef.get("includePlatforms") != ["Editor"]:
    errors.append("Test asmdef must be restricted to the Editor platform")

required = [
    "AGENTS.md",
    "PLANS.md",
    "CURRENT_TASK.md",
    "Documentation~/architecture.md",
    "Schema/foldcanvas.schema.json",
    ".github/workflows/unity-tests.yml",
    ".github/workflows/m13-robustness-long-run.yml",
    ".github/workflows/package-release.yml",
    "Scripts/build_release_package.py",
    "Scripts/test_release_package.py",
    "Scripts/test_release_candidate.py",
    "Scripts/verify_public_release.py",
    "Scripts/test_public_release.py",
    "Scripts/create_upgrade_proof_project.py",
    "Scripts/advance_upgrade_proof_project.py",
    "Scripts/validate_upgrade_evidence.py",
    "Scripts/compare_upgrade_evidence.py",
    "Scripts/test_upgrade_proof.py",
    "Scripts/evaluate_stable_exit.py",
    "Scripts/test_stable_exit.py",
    "SUPPORT.md",
    "SECURITY.md",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "Documentation~/release-candidate.md",
    "Documentation~/m14-release-candidate.json",
    "Schema/foldcanvas-release-candidate.schema.json",
    "Documentation~/m15-public-distribution.json",
    "Schema/foldcanvas-public-distribution.schema.json",
    "Schema/foldcanvas-public-release-verification.schema.json",
    "Schema/foldcanvas-stable-exit.schema.json",
    ".github/workflows/public-release-qualification.yml",
    "Scripts/Templates~/M15UpgradeHost/Assets/FoldCanvas.M15.Upgrade.Tests.asmdef",
    "Scripts/Templates~/M15UpgradeHost/Assets/M15SourceUpgradeTests.cs",
    "Samples~/Gallery/gallery.json",
    "Schema/foldcanvas-gallery.schema.json",
    "Documentation~/m10-performance-baselines.json",
    "Documentation~/m13-resource-envelopes.json",
    "Documentation~/public-runtime-api.json",
    "Documentation~/m11-production-corpus.json",
    "Scripts/create_clean_install_project.py",
    "Scripts/validate_clean_install_evidence.py",
    "Scripts/compare_clean_install_evidence.py",
    "Scripts/test_clean_install_project.py",
    "Scripts/Templates~/M11CleanHost/Assets/FoldCanvas.M11.Consumer.Tests.asmdef",
    "Scripts/Templates~/M11CleanHost/Assets/M11CleanInstallConsumerTests.cs",
    "Schema/foldcanvas-handoff.schema.json",
    "Documentation~/production-handoff.md",
    "Samples~/BootstrapPanel/m12-production-cup.foldcanvas.json",
    "Scripts/create_handoff_proof_projects.py",
    "Scripts/compare_handoff_evidence.py",
    "Scripts/test_handoff_proof.py",
    "Scripts/validate_m13_long_run_evidence.py",
    "Scripts/Templates~/M12Handoff/Producer/Assets/FoldCanvas.M12.HandoffProducer.Tests.asmdef",
    "Scripts/Templates~/M12Handoff/Producer/Assets/M12HandoffProducerTests.cs",
    "Scripts/Templates~/M12Handoff/Receiver/Assets/FoldCanvas.M12.HandoffReceiver.Tests.asmdef",
    "Scripts/Templates~/M12Handoff/Receiver/Assets/M12HandoffReceiverTests.cs",
]
for relative in required:
    if not (ROOT / relative).exists():
        errors.append(f"Missing required file: {relative}")

schema = read_json("Schema/foldcanvas.schema.json")
schema_sample_count_maximum = (
    schema.get("$defs", {})
    .get("seam", {})
    .get("properties", {})
    .get("sampleCount", {})
    .get("maximum")
)
limits_source = (
    ROOT / "Runtime" / "Data" / "FoldCanvasLimits.cs"
).read_text(encoding="utf-8")


def literal_limit(name: str) -> int | None:
    match = re.search(rf"\b{name}\s*=\s*(\d+)\s*;", limits_source)
    if match is None:
        errors.append(f"FoldCanvasLimits.{name} must be a literal integer")
        return None
    return int(match.group(1))


limits_match = re.search(
    r"\bMaximumStitchSampleCount\s*=\s*(\d+)\s*;",
    limits_source,
)
if limits_match is None:
    errors.append(
        "FoldCanvasLimits.MaximumStitchSampleCount must be a literal integer"
    )
elif schema_sample_count_maximum != int(limits_match.group(1)):
    errors.append(
        "Schema seam.sampleCount maximum must match "
        "FoldCanvasLimits.MaximumStitchSampleCount"
    )

schema_definitions = schema.get("$defs", {})
m08_schema_limits = {
    "MaximumFoldScriptIdentifierLength": schema_definitions.get("id", {}).get(
        "maxLength"
    ),
    "MaximumFoldScriptDisplayNameLength": schema.get("properties", {})
    .get("displayName", {})
    .get("maxLength"),
    "MaximumFoldScriptAppearancePathLength": schema_definitions.get("canvas", {})
    .get("properties", {})
    .get("appearance", {})
    .get("maxLength"),
    "MaximumFoldScriptPanels": schema.get("properties", {})
    .get("panels", {})
    .get("maxItems"),
    "MaximumFoldScriptSeams": schema.get("properties", {})
    .get("seams", {})
    .get("maxItems"),
    "MaximumFoldScriptOperations": schema.get("properties", {})
    .get("operations", {})
    .get("maxItems"),
    "MaximumFoldScriptCanvasDimension": schema_definitions.get("canvas", {})
    .get("properties", {})
    .get("width", {})
    .get("maximum"),
}
for constant_name, schema_value in m08_schema_limits.items():
    runtime_value = literal_limit(constant_name)
    if runtime_value is not None and schema_value != runtime_value:
        errors.append(
            f"Schema value for {constant_name} must match FoldCanvasLimits"
        )

handoff_schema = read_json("Schema/foldcanvas-handoff.schema.json")
handoff_properties = handoff_schema.get("properties", {})
if (
    handoff_properties.get("format", {}).get("const")
    != "com.foldcanvas.handoff"
    or handoff_properties.get("version", {}).get("const") != "1"
    or handoff_properties.get("foldScriptVersion", {}).get("const") != "0.1"
):
    errors.append("M12 handoff schema format/version/FoldScript contract is invalid")
handoff_payloads = handoff_properties.get("payloads", {})
if (
    handoff_payloads.get("minItems") != 5
    or handoff_payloads.get("maxItems") != 5
    or len(handoff_payloads.get("prefixItems", [])) != 5
    or handoff_payloads.get("items") is not False
):
    errors.append("M12 handoff schema must lock five ordered payloads")

handoff_documentation = (
    ROOT / "Documentation~" / "production-handoff.md"
).read_text(encoding="utf-8")
for required_handoff_text in (
    "source.foldcanvas.json",
    "appearance.png",
    "geometrySha256",
    "handoff-receipt.json",
    "Tools > FoldCanvas > Handoff",
    "FC9301",
    "256 MiB",
):
    if required_handoff_text not in handoff_documentation:
        errors.append(
            "M12 production handoff documentation is missing: "
            + required_handoff_text
        )

version_source = (
    ROOT / "Runtime" / "Data" / "FoldCanvasVersion.cs"
).read_text(encoding="utf-8")
version_match = re.search(
    r'\bPackage\s*=\s*"([^"]+)"\s*;',
    version_source,
)
if version_match is None or version_match.group(1) != package_version:
    errors.append(
        "FoldCanvasVersion.Package must match package.json version"
    )

unity_workflow = (
    ROOT / ".github" / "workflows" / "unity-tests.yml"
).read_text(encoding="utf-8")
for required_fragment in [
    "game-ci/unity-test-runner@",
    "projectPath: Project~",
    "unityVersion: 6000.3.20f1",
    "testMode: EditMode",
    "artifactsPath: Project~/CIArtifacts/unity-editmode",
    "UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}",
    "steps.unity-tests.outputs.artifactsPath",
    "actions/upload-artifact@",
    "if: always()",
    "test-results.xml",
    "Editor.log",
    "if-no-files-found: error",
]:
    if required_fragment not in unity_workflow:
        errors.append(
            "Unity workflow is missing required configuration: "
            f"{required_fragment}"
        )

if "artifactsPath: artifacts/unity-editmode" in unity_workflow:
    errors.append(
        "GameCI live artifacts must not be written inside the repository-root "
        "UPM package"
    )

gitignore = (ROOT / ".gitignore").read_text(encoding="utf-8")
for required_pattern in [
    "[Ll]ibrary/",
    "[Tt]emp/",
    "[Oo]bj/",
    "[Ll]ogs/",
    "__pycache__/",
    "__pycache__.meta",
    "*.py[cod]",
    "Project~/Assets/FoldCanvasGenerated/",
    "Project~/Assets/FoldCanvasSamples/",
    "Project~/M11Evidence/",
]:
    if required_pattern not in gitignore.splitlines():
        errors.append(f".gitignore is missing required pattern: {required_pattern}")

if (ROOT / "Samples~.meta").exists():
    errors.append(
        "Samples~.meta must not exist because Unity hides tilde-suffixed package folders"
    )

operation_sample_asmdef = read_json(
    "Samples~/OperationExtension/FoldCanvas.Sample.OperationExtension.asmdef"
)
if operation_sample_asmdef.get("references") != ["FoldCanvas.Runtime"]:
    errors.append(
        "The contributor operation sample must reference only FoldCanvas.Runtime"
    )

gallery = read_json("Samples~/Gallery/gallery.json")
if gallery.get("format") != "foldcanvas-gallery" or gallery.get("version") != "1":
    errors.append("Gallery manifest format/version must remain foldcanvas-gallery/1")
gallery_entries = gallery.get("entries")
if not isinstance(gallery_entries, list) or not (1 <= len(gallery_entries) <= 128):
    errors.append("Gallery manifest must contain between 1 and 128 entries")
    gallery_entries = []
gallery_ids: set[str] = set()
editor_menu_source = "\n".join(
    path.read_text(encoding="utf-8")
    for path in sorted((ROOT / "Editor").rglob("*.cs"))
)
for index, entry in enumerate(gallery_entries):
    if not isinstance(entry, dict):
        errors.append(f"Gallery entry {index} must be an object")
        continue
    entry_id = entry.get("id")
    if not isinstance(entry_id, str) or not re.fullmatch(
        r"[a-z][a-z0-9]*(?:-[a-z0-9]+)*", entry_id
    ):
        errors.append(f"Gallery entry {index} has an invalid id")
    elif entry_id in gallery_ids:
        errors.append(f"Gallery entry ID is duplicated: {entry_id}")
    else:
        gallery_ids.add(entry_id)

    for path_key in ["samplePath", "foldScriptPath", "appearancePath"]:
        value = entry.get(path_key)
        if value is None and path_key != "samplePath":
            continue
        if not isinstance(value, str) or not value.startswith("Samples~/"):
            errors.append(f"Gallery entry {index} has invalid {path_key}")
            continue
        pure_path = pathlib.PurePosixPath(value)
        if ".." in pure_path.parts or "." in pure_path.parts or not (ROOT / value).exists():
            errors.append(
                f"Gallery entry {index} {path_key} must resolve inside Samples~: {value}"
            )

    proof_menu = entry.get("proofMenuPath")
    if proof_menu is not None:
        if not isinstance(proof_menu, str) or not proof_menu.startswith(
            "Tools/FoldCanvas/"
        ):
            errors.append(f"Gallery entry {index} has invalid proofMenuPath")
        elif f'MenuItem("{proof_menu}"' not in editor_menu_source:
            errors.append(
                f"Gallery proof menu is not implemented: {proof_menu}"
            )

expected_gallery_ids = {
    "bootstrap-panel",
    "sphere-gores",
    "cyclic-topology",
    "operation-extension",
}
if gallery_ids != expected_gallery_ids:
    errors.append(
        "Canonical gallery entries must cover bootstrap, sphere, topology, and extension proofs"
    )

performance_baselines = read_json(
    "Documentation~/m10-performance-baselines.json"
)
if (
    performance_baselines.get("format")
    != "foldcanvas-performance-baselines"
    or performance_baselines.get("version") != "1"
    or performance_baselines.get("unityVersion") != "6000.3.20f1"
):
    errors.append("M10 performance baseline format/version/Unity must remain locked")
performance_scenarios = performance_baselines.get("scenarios")
if not isinstance(performance_scenarios, list) or len(performance_scenarios) != 3:
    errors.append("M10 must retain its three maintained performance scenarios")
else:
    scenario_ids = [scenario.get("id") for scenario in performance_scenarios]
    if scenario_ids != [
        "planar-grid-64x32",
        "full-roll-64x8",
        "registered-wave-48x24",
    ]:
        errors.append("M10 performance scenario order or identity changed")

m13_resource_envelopes = read_json(
    "Documentation~/m13-resource-envelopes.json"
)
if (
    m13_resource_envelopes.get("format")
    != "foldcanvas-robustness-resource-envelopes"
    or m13_resource_envelopes.get("version") != "1"
    or m13_resource_envelopes.get("unityVersion") != "6000.3.20f1"
):
    errors.append("M13 resource envelope format/version/Unity is invalid")
m13_resource_scenarios = m13_resource_envelopes.get("scenarios")
expected_m13_resources = [
    ("large-planar", 18432, 18432, 36290),
    ("large-cup", 12804, 12290, 24576),
    ("large-sphere", 4496, 3970, 7936),
    ("large-torus", 4753, 4608, 9216),
    ("large-stitch", 4626, 3601, 6848),
]
if not isinstance(m13_resource_scenarios, list) or len(
    m13_resource_scenarios
) != len(expected_m13_resources):
    errors.append("M13 must retain five ordered resource scenarios")
else:
    digest_pattern = re.compile(r"[0-9a-f]{64}")
    for scenario, expected in zip(
        m13_resource_scenarios, expected_m13_resources
    ):
        expected_id, render_vertices, topology_vertices, triangles = expected
        if not isinstance(scenario, dict) or (
            scenario.get("id") != expected_id
            or scenario.get("expectedRenderVertices") != render_vertices
            or scenario.get("expectedTopologyVertices") != topology_vertices
            or scenario.get("expectedTriangles") != triangles
        ):
            errors.append(
                f"M13 resource scenario changed: expected {expected_id}"
            )
            continue
        digest = scenario.get("expectedGeometrySha256")
        if not isinstance(digest, str) or digest_pattern.fullmatch(digest) is None:
            errors.append(f"M13 resource scenario {expected_id} has invalid hash")
        if scenario.get("warmupIterations", -1) < 0 or scenario.get(
            "measuredIterations", 0
        ) < 1:
            errors.append(
                f"M13 resource scenario {expected_id} has invalid iterations"
            )
        if scenario.get("maximumMedianMilliseconds", 0) <= 0 or scenario.get(
            "maximumMedianManagedBytes", 0
        ) <= 0:
            errors.append(
                f"M13 resource scenario {expected_id} has invalid envelopes"
            )

public_api = read_json("Documentation~/public-runtime-api.json")
public_signatures = public_api.get("signatures")
if (
    public_api.get("format") != "foldcanvas-public-runtime-api"
    or public_api.get("version") != "1"
    or public_api.get("assembly") != "FoldCanvas.Runtime"
    or public_api.get("packageVersion") != package_version
):
    errors.append("M11 public Runtime API manifest header is invalid")
if not isinstance(public_signatures, list) or not public_signatures:
    errors.append("M11 public Runtime API manifest lacks signatures")
    public_signatures = []
else:
    if not all(isinstance(signature, str) for signature in public_signatures):
        errors.append("M11 public Runtime API signatures must all be strings")
    if public_signatures != sorted(public_signatures):
        errors.append("M11 public Runtime API signatures must be ordinal")
    if len(public_signatures) != len(set(public_signatures)):
        errors.append("M11 public Runtime API signatures must be unique")
    if public_api.get("signatureCount") != len(public_signatures):
        errors.append("M11 public Runtime API signatureCount is inconsistent")
    public_digest = hashlib.sha256(
        ("\n".join(public_signatures) + "\n").encode("utf-8")
    ).hexdigest()
    if public_api.get("sha256") != public_digest:
        errors.append("M11 public Runtime API signature digest is inconsistent")
    forbidden_api_fragments = (
        "FoldCanvas.Editor",
        "UnityEditor",
        "MeshBuildBuffer",
    )
    for fragment in forbidden_api_fragments:
        if any(fragment in signature for signature in public_signatures):
            errors.append(
                f"M11 public Runtime API manifest exposes forbidden type: {fragment}"
            )

production_corpus = read_json("Documentation~/m11-production-corpus.json")
if (
    production_corpus.get("format") != "foldcanvas-production-corpus"
    or production_corpus.get("version") != "1"
    or production_corpus.get("packageVersion") != package_version
    or production_corpus.get("foldScriptVersion") != "0.1"
    or production_corpus.get("unityVersion") != "6000.3.20f1"
):
    errors.append("M11 production corpus header is invalid")
corpus_cases = production_corpus.get("cases")
expected_corpus_ids = [
    "cyclic-torus",
    "invalid-off-grid-fold",
    "planar-artwork",
    "production-cup",
    "registered-wave",
    "sphere-gores",
]
if not isinstance(corpus_cases, list) or [
    case.get("id") if isinstance(case, dict) else None
    for case in (corpus_cases or [])
] != expected_corpus_ids:
    errors.append("M11 production corpus order or identity changed")
    corpus_cases = []
else:
    digest_pattern = re.compile(r"[0-9a-f]{64}")
    validation_levels = {
        case.get("validationLevel") for case in corpus_cases
    }
    if validation_levels != {"Basic", "Standard", "Strict"}:
        errors.append("M11 corpus must cover Basic, Standard, and Strict validation")
    if sum(case.get("success") is True for case in corpus_cases) != 5:
        errors.append("M11 corpus must retain five successful production cases")
    for case in corpus_cases:
        for field in (
            "sourceSha256",
            "geometrySha256",
            "objSha256",
            "diagnosticSha256",
        ):
            value = case.get(field)
            if not isinstance(value, str) or digest_pattern.fullmatch(value) is None:
                errors.append(
                    f"M11 corpus case {case.get('id')} has invalid {field}"
                )
    invalid_case = corpus_cases[1]
    if (
        invalid_case.get("success") is not False
        or invalid_case.get("errorDiagnosticCode") != "FC3011"
        or invalid_case.get("renderVertices") != 0
        or invalid_case.get("triangles") != 0
    ):
        errors.append("M11 expected-invalid corpus evidence changed")

m14_contract = read_json("Documentation~/m14-release-candidate.json")
expected_m14_gates = [
    "clean-install-a",
    "clean-install-b",
    "deterministic-release",
    "m13-robustness-long-run",
    "production-corpus",
    "production-handoff-producer",
    "production-handoff-receiver",
    "public-runtime-api",
    "repository-validation",
    "unity-editmode",
]
if (
    m14_contract.get("format") != "foldcanvas-release-candidate"
    or m14_contract.get("version") != "1"
    or m14_contract.get("packageName") != "com.foldcanvas.core"
    or m14_contract.get("candidateVersion") != "1.0.0-rc.1"
    or m14_contract.get("stableRelease") is not False
    or m14_contract.get("foldScriptVersion") != "0.1"
):
    errors.append("Historical M14 release-candidate header is invalid")
if not isinstance(package_version, str) or re.fullmatch(
    r"1\.0\.0-rc\.[1-9][0-9]*",
    package_version,
) is None:
    errors.append("M15 package version must remain a 1.0.0 release candidate")

m14_unity_matrix = m14_contract.get("unityMatrix")
if m14_unity_matrix != [
    {
        "editorVersion": "6000.3.20f1",
        "packageUnity": "6000.3",
        "packageUnityRelease": "20f1",
        "qualification": "required",
    }
]:
    errors.append("M14 Unity matrix must contain the exact qualified row")

m14_api = m14_contract.get("publicRuntimeApi", {})
if m14_api != {
    "assembly": "FoldCanvas.Runtime",
    "signatureCount": 808,
    "sha256": "2880e174fc20861971f1f059807296f5220a08cbb6cfca89f3259083981eb31a",
}:
    errors.append("Historical M14 Runtime API identity drifted")

m14_fixtures = m14_contract.get("foldScriptFixtures")
m14_fixture_ids: list[str] = []
if not isinstance(m14_fixtures, list) or not m14_fixtures:
    errors.append("M14 must retain canonical FoldScript compatibility fixtures")
else:
    for fixture in m14_fixtures:
        if not isinstance(fixture, dict):
            errors.append("M14 FoldScript fixture must be an object")
            continue
        fixture_id = fixture.get("id")
        fixture_path = fixture.get("path")
        m14_fixture_ids.append(fixture_id)
        if not isinstance(fixture_id, str) or not fixture_id:
            errors.append("M14 FoldScript fixture has an invalid id")
        if not isinstance(fixture_path, str) or not (ROOT / fixture_path).is_file():
            errors.append(f"M14 FoldScript fixture is missing: {fixture_path}")
            continue
        source_digest = hashlib.sha256((ROOT / fixture_path).read_bytes()).hexdigest()
        if fixture.get("sourceSha256") != source_digest:
            errors.append(f"M14 FoldScript source hash drifted: {fixture_id}")
        canonical_digest = fixture.get("canonicalSha256")
        if (
            not isinstance(canonical_digest, str)
            or re.fullmatch(r"[0-9a-f]{64}", canonical_digest) is None
            or canonical_digest == "0" * 64
        ):
            errors.append(f"M14 FoldScript canonical hash is not frozen: {fixture_id}")
    if m14_fixture_ids != sorted(set(m14_fixture_ids)):
        errors.append("M14 FoldScript fixture IDs must be unique and ordinal")

if m14_contract.get("productionCorpus", {}).get("caseIds") != expected_corpus_ids:
    errors.append("M14 production-corpus identity changed")
if m14_contract.get("requiredGates") != expected_m14_gates:
    errors.append("M14 required release gates changed or are not ordinal")
if m14_contract.get("rollback") != {
    "packageVersion": "0.1.0-preview.21",
    "gitCommit": "d9434be",
    "sourceAuthority": "2d-canvas-plus-foldscript",
}:
    errors.append("M14 rollback contract is invalid")
if m14_contract.get("escalations") != sorted(
    m14_contract.get("escalations", [])
):
    errors.append("M14 escalation list must be ordinal")

m15_contract = read_json("Documentation~/m15-public-distribution.json")
m15_distribution_schema = read_json(
    "Schema/foldcanvas-public-distribution.schema.json"
)
m15_schema_properties = m15_distribution_schema.get("properties", {})
if (
    m15_schema_properties.get("candidateVersion", {}).get("const")
    != package_version
    or "fixture"
    not in m15_schema_properties.get("upgrade", {}).get("required", [])
    or m15_schema_properties.get("stableRelease", {}).get("const") is not False
):
    errors.append("M15 public-distribution schema is stale")
stable_exit_schema = read_json("Schema/foldcanvas-stable-exit.schema.json")
stable_exit_properties = stable_exit_schema.get("properties", {})
if (
    stable_exit_properties.get("format", {}).get("const")
    != "foldcanvas-stable-exit-report"
    or stable_exit_properties.get("candidateVersion", {}).get("const")
    != package_version
    or stable_exit_properties.get("targetVersion", {}).get("const") != "1.0.0"
    or stable_exit_properties.get("minimumSoakHours", {}).get("minimum") != 168
    or stable_exit_properties.get("minimumScheduledLongRuns", {}).get("minimum")
    != 2
):
    errors.append("M15 stable-exit report schema is invalid")
expected_m15_gates = [
    "clean-install-a",
    "clean-install-b",
    "deterministic-release",
    "m13-robustness-long-run",
    "production-corpus",
    "production-handoff-producer",
    "production-handoff-receiver",
    "public-release-assets",
    "public-release-consumer-a",
    "public-release-consumer-b",
    "public-runtime-api",
    "repository-validation",
    "source-upgrade",
    "unity-editmode",
]
if (
    m15_contract.get("format") != "foldcanvas-public-distribution"
    or m15_contract.get("version") != "1"
    or m15_contract.get("packageName") != "com.foldcanvas.core"
    or m15_contract.get("candidateVersion") != package_version
    or m15_contract.get("candidateTag") != f"v{package_version}"
    or m15_contract.get("stableRelease") is not False
    or m15_contract.get("foldScriptVersion") != "0.1"
    or m15_contract.get("unityVersion") != "6000.3.20f1"
):
    errors.append("M15 public-distribution header is invalid")

m15_api = m15_contract.get("publicRuntimeApi", {})
if (
    m15_api.get("assembly") != public_api.get("assembly")
    or m15_api.get("signatureCount") != public_api.get("signatureCount")
    or m15_api.get("sha256") != public_api.get("sha256")
):
    errors.append("M15 Runtime API does not match the compiled baseline")

m15_fixtures = m15_contract.get("foldScriptFixtures")
m15_fixture_ids: list[str] = []
if not isinstance(m15_fixtures, list) or not m15_fixtures:
    errors.append("M15 must retain canonical FoldScript fixtures")
else:
    for fixture in m15_fixtures:
        if not isinstance(fixture, dict):
            errors.append("M15 FoldScript fixture must be an object")
            continue
        fixture_id = fixture.get("id")
        fixture_path = fixture.get("path")
        m15_fixture_ids.append(fixture_id)
        if not isinstance(fixture_id, str) or not fixture_id:
            errors.append("M15 FoldScript fixture has an invalid id")
        if not isinstance(fixture_path, str) or not (ROOT / fixture_path).is_file():
            errors.append(f"M15 FoldScript fixture is missing: {fixture_path}")
            continue
        source_digest = hashlib.sha256((ROOT / fixture_path).read_bytes()).hexdigest()
        if fixture.get("sourceSha256") != source_digest:
            errors.append(f"M15 FoldScript source hash drifted: {fixture_id}")
        canonical_digest = fixture.get("canonicalSha256")
        if (
            not isinstance(canonical_digest, str)
            or re.fullmatch(r"[0-9a-f]{64}", canonical_digest) is None
            or canonical_digest == "0" * 64
        ):
            errors.append(f"M15 FoldScript canonical hash is not frozen: {fixture_id}")
    if m15_fixture_ids != sorted(set(m15_fixture_ids)):
        errors.append("M15 FoldScript fixture IDs must be unique and ordinal")

expected_public_assets = [
    f"com.foldcanvas.core-{package_version}.evidence.json",
    f"com.foldcanvas.core-{package_version}.manifest.json",
    f"com.foldcanvas.core-{package_version}.tgz",
    f"com.foldcanvas.core-{package_version}.tgz.sha256",
]
if m15_contract.get("publicAssets") != expected_public_assets:
    errors.append("M15 public release asset allowlist is invalid")
if m15_contract.get("productionCorpus", {}).get("caseIds") != expected_corpus_ids:
    errors.append("M15 production-corpus identity changed")
if m15_contract.get("requiredGates") != expected_m15_gates:
    errors.append("M15 required gates changed or are not ordinal")
if m15_contract.get("priorRelease") != {
    "tag": "v1.0.0-rc.1",
    "mergeCommit": "a8c81e61175dafbc48d1750de7ef6823589517a6",
    "releaseId": 364684802,
    "archiveSha256": "ff3a065eec3a638701ff51d4f069684df4f075226305253ae04fd6ed2b250fdd",
    "checksumSha256": "eb835a1cac0ee0c5adb6b99511f6bb93739839543bee1fe0be3cbad989960725",
    "manifestSha256": "a0b9f19b9b69cace6b430b4546fc67b0f294dadb1efef6f8542b5e53ee5a9aca",
    "evidenceSha256": "c9603b475bbfa78300a27b64189188a337ca95ef333c8ad00effa7cf808e3c32",
    "assetCount": 4,
    "immutable": True,
}:
    errors.append("M15 immutable RC1 release identity drifted")
if m15_contract.get("rollback") != {
    "packageVersion": "1.0.0-rc.1",
    "tag": "v1.0.0-rc.1",
    "gitCommit": "a8c81e61175dafbc48d1750de7ef6823589517a6",
    "sourceAuthority": "2d-canvas-plus-foldscript",
}:
    errors.append("M15 rollback contract is invalid")
m15_upgrade = m15_contract.get("upgrade", {})
m15_upgrade_fixture = m15_upgrade.get("fixture", {})
if (
    m15_upgrade.get("fromPackageVersions")
    != ["0.1.0-preview.21", "1.0.0-rc.1"]
    or m15_upgrade.get("sourceAuthority") != "2d-canvas-plus-foldscript"
    or m15_upgrade.get("derivedInputsForbidden")
    != ["material", "mesh", "obj", "prefab", "receipt", "report", "screenshot"]
    or m15_upgrade.get("unknownVersionsFailClosed") is not True
):
    errors.append("M15 source-first upgrade contract is invalid")
expected_upgrade_fixture = {
    "id": "production-cup",
    "baselinePackageVersion": "0.1.0-preview.21",
    "baselineGitCommit": "d9434be9e30812fc367004b51cf285281713246b",
    "sourcePath": "Samples~/BootstrapPanel/m12-production-cup.foldcanvas.json",
    "appearancePath": "Samples~/BootstrapPanel/M04ProductionCupCanvas.png",
    "inputFileNames": [
        "M04ProductionCupCanvas.png",
        "m12-production-cup.foldcanvas.json",
    ],
    "sourceRawSha256": "44b3c474784736d5e47d974f94b87c56532db01cffc963330d7ae82b88457fc5",
    "canonicalSourceSha256": "ff9df3d482f1b73820f026093ef3094b0e17f313ebeb26055129af03c41844ff",
    "appearanceSha256": "2b9691733e89987d795e9dcbcd857902f6feb7109540d36fded1b481ded1a383",
    "renderVertices": 2972,
    "topologyVertices": 2562,
    "triangles": 5120,
    "closedVolume": True,
}
if m15_upgrade_fixture != expected_upgrade_fixture:
    errors.append("M15 source-first upgrade fixture identity drifted")
else:
    for path_key, digest_key in (
        ("sourcePath", "sourceRawSha256"),
        ("appearancePath", "appearanceSha256"),
    ):
        fixture_path = ROOT / m15_upgrade_fixture[path_key]
        if not fixture_path.is_file() or hashlib.sha256(
            fixture_path.read_bytes()
        ).hexdigest() != m15_upgrade_fixture[digest_key]:
            errors.append(f"M15 upgrade fixture bytes drifted: {path_key}")
if m15_contract.get("stableExit") != {
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
}:
    errors.append("M15 stable exit must remain explicitly blocked")
if m15_contract.get("escalations") != sorted(
    m15_contract.get("escalations", [])
):
    errors.append("M15 escalation list must be ordinal")

release_workflow = (
    ROOT / ".github" / "workflows" / "package-release.yml"
).read_text(encoding="utf-8")
for required_fragment in [
    "workflow_dispatch:",
    "pull_request:",
    'tags:',
    '"v*-rc.*"',
    'python3 Scripts/test_release_package.py',
    'python3 Scripts/test_release_candidate.py',
    'python3 Scripts/build_release_package.py',
    'actions/upload-artifact@',
    'if-no-files-found: error',
    "gh release create",
    "--verify-tag",
    "--prerelease",
    "contents: read",
    "publish-prerelease:",
    "contents: write",
    "actions/download-artifact@",
    "*.manifest.json",
    "*.evidence.json",
    "secrets.GITHUB_TOKEN",
]:
    if required_fragment not in release_workflow:
        errors.append(
            "Package release workflow is missing required configuration: "
            f"{required_fragment}"
        )

repository_workflow = (
    ROOT / ".github" / "workflows" / "repository-checks.yml"
).read_text(encoding="utf-8")
if "python3 Scripts/test_release_package.py" not in repository_workflow:
    errors.append("Repository checks must validate deterministic release archives")
if "python3 Scripts/test_clean_install_project.py" not in repository_workflow:
    errors.append("Repository checks must validate M11 clean-install contracts")
if "python3 Scripts/test_handoff_proof.py" not in repository_workflow:
    errors.append("Repository checks must validate M12 handoff contracts")
if "python3 Scripts/test_release_candidate.py" not in repository_workflow:
    errors.append("Repository checks must validate the current release candidate")
if "python3 Scripts/test_public_release.py" not in repository_workflow:
    errors.append("Repository checks must validate M15 public release evidence")
if "python3 Scripts/test_upgrade_proof.py" not in repository_workflow:
    errors.append("Repository checks must validate M15 source-first upgrade")
if "python3 Scripts/test_stable_exit.py" not in repository_workflow:
    errors.append("Repository checks must validate the M15 stable exit gate")

for required_fragment in [
    "python3 Scripts/test_upgrade_proof.py",
    "python3 Scripts/test_stable_exit.py",
]:
    if required_fragment not in release_workflow:
        errors.append(
            "Package release workflow is missing M15 validation: "
            + required_fragment
        )

public_release_workflow = (
    ROOT / ".github" / "workflows" / "public-release-qualification.yml"
).read_text(encoding="utf-8")
for required_fragment in [
    "release:",
    "workflow_dispatch:",
    "types:",
    "published",
    "contents: read",
    "gh release download",
    "Scripts/verify_public_release.py",
    "Scripts/create_clean_install_project.py",
    "game-ci/unity-test-runner@v4.3.1",
    "unityVersion: 6000.3.20f1",
    "FoldCanvas Public Release Consumer A",
    "FoldCanvas Public Release Consumer B",
    "Scripts/validate_clean_install_evidence.py",
    "Scripts/compare_clean_install_evidence.py",
    "source-first-upgrade:",
    "Scripts/create_upgrade_proof_project.py",
    "Scripts/advance_upgrade_proof_project.py",
    "Scripts/validate_upgrade_evidence.py",
    "Scripts/compare_upgrade_evidence.py",
    "unity-source-first-upgrade-results-and-logs",
    "stable-exit-snapshot:",
    "issues: read",
    "Scripts/evaluate_stable_exit.py",
    "foldcanvas-stable-exit-snapshot",
    "test-results.xml",
    "Editor.log",
    "if-no-files-found: error",
]:
    if required_fragment not in public_release_workflow:
        errors.append(
            "Public release workflow is missing required evidence: "
            f"{required_fragment}"
        )

m13_long_run_workflow = (
    ROOT / ".github" / "workflows" / "m13-robustness-long-run.yml"
).read_text(encoding="utf-8")
for required_fragment in [
    "workflow_dispatch:",
    "schedule:",
    'cron: "17 3 * * 1"',
    "game-ci/unity-test-runner@v4.3.1",
    "projectPath: Project~",
    "unityVersion: 6000.3.20f1",
    "testMode: EditMode",
    "FOLDCANVAS_M13_LONG_CASES_PER_SUITE",
    "FOLDCANVAS_M13_LONG_SEED_HEX",
    "foldCanvasM13CasesPerSuite",
    "foldCanvasM13SeedHex",
    "foldCanvasM13EvidenceDirectory",
    "Scripts/validate_m13_long_run_evidence.py",
    "robustness-report.json",
    "replay-records.json",
    "resource-report.json",
    "environment.json",
    "test-results.xml",
    "Editor.log",
    "if-no-files-found: error",
]:
    if required_fragment not in m13_long_run_workflow:
        errors.append(
            "M13 long-run workflow is missing required evidence: "
            f"{required_fragment}"
        )

for required_fragment in [
    "artifacts/m11-clean-host-a",
    "artifacts/m11-clean-host-b",
    "Scripts/compare_clean_install_evidence.py",
    "production-corpus-report.json",
    "unity-clean-install-results-and-logs",
]:
    if required_fragment not in unity_workflow:
        errors.append(
            "Unity workflow is missing M11 production evidence: "
            f"{required_fragment}"
        )

for required_fragment in [
    "unity-production-handoff-tests:",
    "Scripts/create_handoff_proof_projects.py",
    "FoldCanvas Production Handoff Producer",
    "FoldCanvas Production Handoff Receiver",
    "Scripts/compare_handoff_evidence.py",
    "production-cup.foldcanvas.zip",
    "handoff-receipt.json",
    "unity-production-handoff-results-and-evidence",
]:
    if required_fragment not in unity_workflow:
        errors.append(
            "Unity workflow is missing M12 handoff evidence: "
            f"{required_fragment}"
        )

for required_fragment in [
    "unity-source-upgrade-tests:",
    "Scripts/create_upgrade_proof_project.py",
    "Scripts/advance_upgrade_proof_project.py",
    "Scripts/validate_upgrade_evidence.py",
    "Scripts/compare_upgrade_evidence.py",
    "FoldCanvas Source Upgrade Before Candidate",
    "FoldCanvas Source Upgrade Candidate",
    "unity-source-upgrade-pre-release-results-and-logs",
]:
    if required_fragment not in unity_workflow:
        errors.append(
            "Unity workflow is missing M15 source-upgrade evidence: "
            f"{required_fragment}"
        )

sample_document_path = (
    ROOT
    / "Samples~"
    / "BootstrapPanel"
    / "gpt-cup.future-example.foldcanvas.json"
)
sample_document = read_json(
    "Samples~/BootstrapPanel/gpt-cup.future-example.foldcanvas.json"
)
sample_appearance = sample_document.get("canvas", {}).get("appearance")
if not isinstance(sample_appearance, str) or not (
    sample_document_path.parent / sample_appearance
).is_file():
    errors.append(
        "BootstrapPanel FoldScript example must reference an appearance file "
        "inside its sample folder"
    )

m12_sample_path = (
    ROOT
    / "Samples~"
    / "BootstrapPanel"
    / "m12-production-cup.foldcanvas.json"
)
m12_sample = read_json(
    "Samples~/BootstrapPanel/m12-production-cup.foldcanvas.json"
)
m12_appearance = m12_sample.get("canvas", {}).get("appearance")
if (
    m12_sample.get("assetId") != "m12-production-cup"
    or m12_sample.get("compile", {}).get("validationLevel") != "strict"
    or not isinstance(m12_appearance, str)
    or not (m12_sample_path.parent / m12_appearance).is_file()
):
    errors.append("M12 production cup source/PNG proof fixture is invalid")

markdown_link_pattern = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
for markdown_path in repository_files("*.md"):
    text = markdown_path.read_text(encoding="utf-8")
    for match in markdown_link_pattern.finditer(text):
        raw_target = match.group(1).strip()
        if raw_target.startswith("<") and raw_target.endswith(">"):
            raw_target = raw_target[1:-1]
        parsed = urllib.parse.urlparse(raw_target)
        if parsed.scheme or raw_target.startswith("#"):
            continue

        relative_target = urllib.parse.unquote(raw_target.split("#", 1)[0])
        if not relative_target:
            continue

        resolved = (markdown_path.parent / relative_target).resolve()
        if not resolved.exists():
            errors.append(
                "Broken Markdown link: "
                f"{markdown_path.relative_to(ROOT)} -> {raw_target}"
            )

issue_template_folder = ROOT / ".github" / "ISSUE_TEMPLATE"
for issue_form_path in sorted(issue_template_folder.glob("*.yml")):
    if issue_form_path.name == "config.yml":
        continue

    issue_form_text = issue_form_path.read_text(encoding="utf-8")
    for required_key in ["name", "description", "body"]:
        if not re.search(rf"^{required_key}:", issue_form_text, re.MULTILINE):
            errors.append(
                f"GitHub Issue Form is missing '{required_key}': "
                f"{issue_form_path.relative_to(ROOT)}"
            )
    if re.search(r"^about:", issue_form_text, re.MULTILINE):
        errors.append(
            "GitHub Issue Forms use 'description', not the Markdown-template "
            f"'about' field: {issue_form_path.relative_to(ROOT)}"
        )

if errors:
    print("Repository validation failed:")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("Repository validation passed.")
