#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import zipfile

ORDERED_ENTRIES = [
    "manifest.json",
    "source.foldcanvas.json",
    "appearance.png",
    "derived/model.obj",
    "evidence/compile-report.json",
    "README.md",
]

EQUAL_FIELDS = [
    "packageVersion",
    "compilerVersion",
    "archiveSha256",
    "sourceSha256",
    "appearanceSha256",
    "geometrySha256",
    "objSha256",
    "diagnosticSha256",
    "validationSha256",
    "closedVolumeSha256",
    "renderVertexCount",
    "topologyVertexCount",
    "triangleCount",
    "isClosedVolume",
    "isSingleClosedVolume",
]


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def load_json(path: pathlib.Path) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"JSON root must be an object: {path}")
    return value


def validate_report(report: dict, role: str) -> None:
    if report.get("format") != "foldcanvas-m12-handoff-proof":
        raise ValueError(f"{role} report format is invalid")
    if report.get("version") != "1" or report.get("role") != role:
        raise ValueError(f"{role} report version or role is invalid")
    for field in (
        "archiveSha256",
        "sourceSha256",
        "appearanceSha256",
        "geometrySha256",
        "objSha256",
        "diagnosticSha256",
        "validationSha256",
        "closedVolumeSha256",
    ):
        if not re.fullmatch(r"[0-9a-f]{64}", str(report.get(field, ""))):
            raise ValueError(f"{role} report {field} is not SHA-256")
    for field in (
        "renderVertexCount",
        "topologyVertexCount",
        "triangleCount",
    ):
        if not isinstance(report.get(field), int) or report[field] <= 0:
            raise ValueError(f"{role} report {field} must be positive")
    if report.get("isClosedVolume") is not True:
        raise ValueError(f"{role} report is not a closed volume")
    if report.get("isSingleClosedVolume") is not True:
        raise ValueError(f"{role} report is not one closed volume")
    project_path = report.get("projectPath")
    if not isinstance(project_path, str) or not project_path:
        raise ValueError(f"{role} report lacks a project path")


def validate_archive(path: pathlib.Path) -> tuple[dict, dict]:
    archive_bytes = path.read_bytes()
    with zipfile.ZipFile(path, mode="r") as archive:
        infos = archive.infolist()
        if [item.filename for item in infos] != ORDERED_ENTRIES:
            raise ValueError("handoff archive entry order is invalid")
        for item in infos:
            if item.compress_type != zipfile.ZIP_STORED:
                raise ValueError(f"handoff entry is compressed: {item.filename}")
            if item.date_time != (1980, 1, 1, 0, 0, 0):
                raise ValueError(f"handoff entry timestamp drift: {item.filename}")
            if item.extra or item.comment or item.is_dir():
                raise ValueError(f"handoff entry metadata is noncanonical: {item.filename}")
        payload = {name: archive.read(name) for name in ORDERED_ENTRIES}

    manifest = json.loads(payload["manifest.json"].decode("utf-8"))
    if manifest.get("format") != "com.foldcanvas.handoff":
        raise ValueError("handoff manifest format is invalid")
    if manifest.get("version") != "1":
        raise ValueError("handoff manifest version is invalid")
    expected_payloads = ORDERED_ENTRIES[1:]
    described = manifest.get("payloads")
    if not isinstance(described, list) or len(described) != 5:
        raise ValueError("handoff manifest payload list is invalid")
    for expected_name, item in zip(expected_payloads, described, strict=True):
        if item.get("path") != expected_name:
            raise ValueError("handoff manifest payload order is invalid")
        if item.get("byteLength") != len(payload[expected_name]):
            raise ValueError(f"handoff payload length mismatch: {expected_name}")
        if item.get("sha256") != sha256_bytes(payload[expected_name]):
            raise ValueError(f"handoff payload hash mismatch: {expected_name}")
    return manifest, {
        "archiveSha256": sha256_bytes(archive_bytes),
        "entryCount": len(ORDERED_ENTRIES),
        "sourceSha256": sha256_bytes(payload["source.foldcanvas.json"]),
        "appearanceSha256": sha256_bytes(payload["appearance.png"]),
        "objSha256": sha256_bytes(payload["derived/model.obj"]),
        "evidenceSha256": sha256_bytes(payload["evidence/compile-report.json"]),
    }


def compare(
    producer_path: pathlib.Path,
    receiver_path: pathlib.Path,
    archive_path: pathlib.Path,
    receipt_path: pathlib.Path,
    source_path: pathlib.Path,
) -> dict:
    producer = load_json(producer_path)
    receiver = load_json(receiver_path)
    receipt = load_json(receipt_path)
    validate_report(producer, "producer")
    validate_report(receiver, "receiver")
    if producer["projectPath"] == receiver["projectPath"]:
        raise ValueError("producer and receiver project paths must differ")
    for field in EQUAL_FIELDS:
        if producer.get(field) != receiver.get(field):
            raise ValueError(f"producer/receiver mismatch: {field}")

    manifest, archive = validate_archive(archive_path)
    if producer["archiveSha256"] != archive["archiveSha256"]:
        raise ValueError("reported archive SHA does not match archive bytes")
    if producer["sourceSha256"] != archive["sourceSha256"]:
        raise ValueError("reported source SHA does not match archive source")
    if producer["appearanceSha256"] != archive["appearanceSha256"]:
        raise ValueError("reported appearance SHA does not match archive PNG")
    if producer["objSha256"] != archive["objSha256"]:
        raise ValueError("reported OBJ SHA does not match archive OBJ")

    if receipt.get("format") != "com.foldcanvas.handoff.receipt":
        raise ValueError("receiver receipt format is invalid")
    for report_field, receipt_field in (
        ("archiveSha256", "archiveSha256"),
        ("sourceSha256", "sourceSha256"),
        ("appearanceSha256", "appearanceSha256"),
        ("geometrySha256", "geometrySha256"),
        ("objSha256", "objSha256"),
        ("packageVersion", "packageVersion"),
        ("compilerVersion", "compilerVersion"),
    ):
        if receiver.get(report_field) != receipt.get(receipt_field):
            raise ValueError(f"receiver receipt mismatch: {receipt_field}")
    if sha256_bytes(source_path.read_bytes()) != receipt.get("sourceSha256"):
        raise ValueError("receiver canonical source does not match receipt")
    if manifest.get("asset", {}).get("id") != receipt.get("assetId"):
        raise ValueError("manifest and receipt asset identity differ")

    return {
        "format": "foldcanvas-m12-handoff-proof-comparison",
        "version": "1",
        "packageVersion": producer["packageVersion"],
        "compilerVersion": producer["compilerVersion"],
        "producerProjectPath": producer["projectPath"],
        "receiverProjectPath": receiver["projectPath"],
        **archive,
        "geometrySha256": producer["geometrySha256"],
        "diagnosticSha256": producer["diagnosticSha256"],
        "validationSha256": producer["validationSha256"],
        "closedVolumeSha256": producer["closedVolumeSha256"],
        "renderVertexCount": producer["renderVertexCount"],
        "topologyVertexCount": producer["topologyVertexCount"],
        "triangleCount": producer["triangleCount"],
        "isClosedVolume": True,
        "isSingleClosedVolume": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare M12 producer and receiver handoff evidence."
    )
    parser.add_argument("--producer", required=True, type=pathlib.Path)
    parser.add_argument("--receiver", required=True, type=pathlib.Path)
    parser.add_argument("--archive", required=True, type=pathlib.Path)
    parser.add_argument("--receipt", required=True, type=pathlib.Path)
    parser.add_argument("--source", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    result = compare(
        args.producer.resolve(),
        args.receiver.resolve(),
        args.archive.resolve(),
        args.receipt.resolve(),
        args.source.resolve(),
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(json.dumps(result, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
