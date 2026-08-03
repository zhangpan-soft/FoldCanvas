#!/usr/bin/env python3
from __future__ import annotations

import json
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
