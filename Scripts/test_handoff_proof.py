#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import pathlib
import tempfile
import zipfile

from build_release_package import build_archive
from compare_handoff_evidence import compare
from create_handoff_proof_projects import create_project


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def write_stored_archive(path: pathlib.Path, payload: dict[str, bytes]) -> None:
    roles = [
        "source",
        "appearance",
        "derived-obj",
        "compile-evidence",
        "instructions",
    ]
    paths = list(payload)
    manifest = {
        "format": "com.foldcanvas.handoff",
        "version": "1",
        "asset": {"id": "proof", "displayName": "Proof"},
        "payloads": [
            {
                "path": name,
                "role": role,
                "byteLength": len(payload[name]),
                "sha256": digest(payload[name]),
            }
            for name, role in zip(paths, roles, strict=True)
        ],
    }
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_STORED) as archive:
        entries = {
            "manifest.json": json.dumps(manifest).encode(),
            **payload,
        }
        for name, data in entries.items():
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_STORED
            info.external_attr = 0
            archive.writestr(info, data)


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="foldcanvas-m12-static-") as raw:
        root = pathlib.Path(raw)
        package = build_archive(root / "package")
        producer_path = root / "producer-project"
        receiver_path = root / "receiver-project"
        producer_input = create_project(producer_path, package, "producer")
        receiver_input = create_project(receiver_path, package, "receiver")
        assert producer_input["role"] == "producer"
        assert receiver_input["role"] == "receiver"
        assert producer_input["packageSha256"] == receiver_input["packageSha256"]
        assert (producer_path / "Assets" / "Fixture" / "m12-production-cup.foldcanvas.json").is_file()
        assert (producer_path / "Assets" / "Fixture" / "M04ProductionCupCanvas.png").is_file()
        assert list((receiver_path / "M12Input").iterdir()) == []

        source = b'{"source":true}\n'
        appearance = b"png-proof"
        obj = b"o proof\n"
        evidence = b'{"evidence":true}\n'
        readme = b"# proof\n"
        archive_path = root / "proof.foldcanvas.zip"
        write_stored_archive(
            archive_path,
            {
                "source.foldcanvas.json": source,
                "appearance.png": appearance,
                "derived/model.obj": obj,
                "evidence/compile-report.json": evidence,
                "README.md": readme,
            },
        )
        archive_sha = digest(archive_path.read_bytes())
        shared = {
            "format": "foldcanvas-m12-handoff-proof",
            "version": "1",
            "packageVersion": producer_input["packageVersion"],
            "compilerVersion": producer_input["packageVersion"],
            "archiveSha256": archive_sha,
            "sourceSha256": digest(source),
            "appearanceSha256": digest(appearance),
            "geometrySha256": "1" * 64,
            "objSha256": digest(obj),
            "diagnosticSha256": "2" * 64,
            "validationSha256": "3" * 64,
            "closedVolumeSha256": "4" * 64,
            "renderVertexCount": 3,
            "topologyVertexCount": 3,
            "triangleCount": 1,
            "isClosedVolume": True,
            "isSingleClosedVolume": True,
        }
        producer_report = {**shared, "role": "producer", "projectPath": str(producer_path)}
        receiver_report = {**shared, "role": "receiver", "projectPath": str(receiver_path)}
        producer_report_path = root / "producer.json"
        receiver_report_path = root / "receiver.json"
        producer_report_path.write_text(json.dumps(producer_report), encoding="utf-8")
        receiver_report_path.write_text(json.dumps(receiver_report), encoding="utf-8")
        receipt = {
            "format": "com.foldcanvas.handoff.receipt",
            "assetId": "proof",
            "archiveSha256": archive_sha,
            "sourceSha256": digest(source),
            "appearanceSha256": digest(appearance),
            "geometrySha256": "1" * 64,
            "objSha256": digest(obj),
            "packageVersion": producer_input["packageVersion"],
            "compilerVersion": producer_input["packageVersion"],
        }
        receipt_path = root / "receipt.json"
        receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
        source_path = root / "source.foldcanvas.json"
        source_path.write_bytes(source)
        result = compare(
            producer_report_path,
            receiver_report_path,
            archive_path,
            receipt_path,
            source_path,
        )
        assert result["archiveSha256"] == archive_sha
        assert result["producerProjectPath"] != result["receiverProjectPath"]

    print("M12 handoff project and comparison validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
