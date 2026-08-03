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
    ".github/workflows/package-release.yml",
    "Scripts/build_release_package.py",
    "Scripts/test_release_package.py",
    "Samples~/Gallery/gallery.json",
    "Schema/foldcanvas-gallery.schema.json",
    "Documentation~/m10-performance-baselines.json",
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

release_workflow = (
    ROOT / ".github" / "workflows" / "package-release.yml"
).read_text(encoding="utf-8")
for required_fragment in [
    "workflow_dispatch:",
    'tags:',
    'python3 Scripts/test_release_package.py',
    'python3 Scripts/build_release_package.py',
    'actions/upload-artifact@',
    'if-no-files-found: error',
    "gh release create",
    "--verify-tag",
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
