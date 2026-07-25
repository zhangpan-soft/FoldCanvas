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
]
for relative in required:
    if not (ROOT / relative).exists():
        errors.append(f"Missing required file: {relative}")

gitignore = (ROOT / ".gitignore").read_text(encoding="utf-8")
for required_pattern in [
    "[Ll]ibrary/",
    "[Tt]emp/",
    "[Oo]bj/",
    "[Ll]ogs/",
    "Project~/Assets/FoldCanvasGenerated/",
    "Project~/Assets/FoldCanvasSamples/",
]:
    if required_pattern not in gitignore.splitlines():
        errors.append(f".gitignore is missing required pattern: {required_pattern}")

if (ROOT / "Samples~.meta").exists():
    errors.append(
        "Samples~.meta must not exist because Unity hides tilde-suffixed package folders"
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
