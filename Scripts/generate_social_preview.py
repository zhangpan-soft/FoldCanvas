#!/usr/bin/env python3
from __future__ import annotations

import argparse
import pathlib
import struct
import zlib

import validate_proof_gallery as proof

ROOT = pathlib.Path(__file__).resolve().parents[1]
GALLERY = ROOT / "Docs" / "Community" / "ProofGallery"
OUTPUT = GALLERY / "social-preview.png"
WIDTH = 1280
HEIGHT = 640

BACKGROUND = (5, 8, 16, 255)
CARD = (16, 27, 44, 255)
CYAN = (56, 235, 255, 255)
MAGENTA = (255, 46, 184, 255)
WHITE = (235, 245, 255, 255)
MUTED = (137, 166, 196, 255)

FONT = {
    " ": (0, 0, 0, 0, 0, 0, 0),
    "+": (0, 4, 4, 31, 4, 4, 0),
    "-": (0, 0, 0, 31, 0, 0, 0),
    ">": (16, 8, 4, 2, 4, 8, 16),
    "2": (14, 17, 16, 8, 4, 2, 31),
    "3": (30, 1, 1, 14, 1, 1, 30),
    "D": (30, 17, 17, 17, 17, 17, 30),
    "F": (31, 16, 16, 30, 16, 16, 16),
    "O": (14, 17, 17, 17, 17, 17, 14),
    "L": (16, 16, 16, 16, 16, 16, 31),
    "C": (15, 16, 16, 16, 16, 16, 15),
    "A": (14, 17, 17, 31, 17, 17, 17),
    "N": (17, 25, 21, 19, 17, 17, 17),
    "V": (17, 17, 17, 17, 17, 10, 4),
    "S": (15, 16, 16, 14, 1, 1, 30),
    "R": (30, 17, 17, 30, 20, 18, 17),
    "E": (31, 16, 16, 30, 16, 16, 31),
    "U": (17, 17, 17, 17, 17, 17, 14),
    "T": (31, 4, 4, 4, 4, 4, 4),
    "M": (17, 27, 21, 21, 17, 17, 17),
    "I": (31, 4, 4, 4, 4, 4, 31),
    "G": (15, 16, 16, 19, 17, 17, 14),
    "Y": (17, 17, 10, 4, 4, 4, 4),
    "P": (30, 17, 17, 30, 16, 16, 16),
    "H": (17, 17, 17, 31, 17, 17, 17),
    "X": (17, 17, 10, 4, 10, 17, 17),
}


def set_pixel(canvas: bytearray, x: int, y: int, color: tuple[int, ...]) -> None:
    if 0 <= x < WIDTH and 0 <= y < HEIGHT:
        offset = (y * WIDTH + x) * 4
        canvas[offset : offset + 4] = bytes(color)


def fill(
    canvas: bytearray,
    x: int,
    y: int,
    width: int,
    height: int,
    color: tuple[int, ...],
) -> None:
    row = bytes(color) * width
    for target_y in range(max(0, y), min(HEIGHT, y + height)):
        start = (target_y * WIDTH + max(0, x)) * 4
        end = start + min(width, WIDTH - max(0, x)) * 4
        canvas[start:end] = row[: end - start]


def text(
    canvas: bytearray,
    value: str,
    x: int,
    y: int,
    scale: int,
    color: tuple[int, ...],
) -> None:
    cursor = x
    for character in value.upper():
        glyph = FONT[character]
        for row_index, row_bits in enumerate(glyph):
            for column in range(5):
                if row_bits & (1 << (4 - column)):
                    fill(
                        canvas,
                        cursor + column * scale,
                        y + row_index * scale,
                        scale,
                        scale,
                        color,
                    )
        cursor += 6 * scale


def decode(name: str) -> tuple[int, int, bytes]:
    errors: list[str] = []
    value = proof.png_pixels((GALLERY / name).read_bytes(), name, errors)
    if errors or value is None:
        raise ValueError("; ".join(errors) or f"could not decode {name}")
    return value


def blit_cover(
    canvas: bytearray,
    image: tuple[int, int, bytes],
    x: int,
    y: int,
    width: int,
    height: int,
) -> None:
    source_width, source_height, pixels = image
    source_ratio = source_width / source_height
    target_ratio = width / height
    if source_ratio > target_ratio:
        crop_height = source_height
        crop_width = int(round(crop_height * target_ratio))
        crop_x = (source_width - crop_width) // 2
        crop_y = 0
    else:
        crop_width = source_width
        crop_height = int(round(crop_width / target_ratio))
        crop_x = 0
        crop_y = (source_height - crop_height) // 2
    for target_y in range(height):
        source_y = crop_y + min(
            crop_height - 1,
            (target_y * crop_height) // height,
        )
        for target_x in range(width):
            source_x = crop_x + min(
                crop_width - 1,
                (target_x * crop_width) // width,
            )
            source_offset = (source_y * source_width + source_x) * 4
            set_pixel(
                canvas,
                x + target_x,
                y + target_y,
                tuple(pixels[source_offset : source_offset + 4]),
            )


def chunk(kind: bytes, payload: bytes) -> bytes:
    return (
        struct.pack(">I", len(payload))
        + kind
        + payload
        + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)
    )


def encode(canvas: bytes) -> bytes:
    raw = b"".join(
        b"\x00" + canvas[y * WIDTH * 4 : (y + 1) * WIDTH * 4]
        for y in range(HEIGHT)
    )
    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", WIDTH, HEIGHT, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(raw, 9))
        + chunk(b"IEND", b"")
    )


def generate(output: pathlib.Path = OUTPUT) -> bytes:
    manifest_errors = proof.validate()
    if manifest_errors:
        raise ValueError("M21 proof validation failed: " + "; ".join(manifest_errors))

    canvas = bytearray(BACKGROUND * (WIDTH * HEIGHT))
    fill(canvas, 0, 0, WIDTH, 6, CYAN)
    text(canvas, "FOLDCANVAS", 48, 34, 5, WHITE)
    text(canvas, "2D CANVAS + FOLDSCRIPT -> DETERMINISTIC 3D", 48, 86, 3, CYAN)
    text(canvas, "REAL UNITY PROOF", 925, 42, 3, MAGENTA)

    names = (
        "cup-source.png",
        "cup-textured.png",
        "cup-topology.png",
        "sphere-source.png",
        "sphere-textured.png",
        "sphere-topology.png",
    )
    decoded = {name: decode(name) for name in names}
    columns = (48, 444, 840)
    labels = ("2D SOURCE", "TEXTURED 3D", "TOPOLOGY")
    for x, label in zip(columns, labels):
        fill(canvas, x, 138, 364, 206, CARD)
        fill(canvas, x, 370, 364, 206, CARD)
        text(canvas, label, x + 8, 592, 2, MUTED)
    for index, name in enumerate(names[:3]):
        blit_cover(canvas, decoded[name], columns[index] + 8, 146, 348, 190)
    for index, name in enumerate(names[3:]):
        blit_cover(canvas, decoded[name], columns[index] + 8, 378, 348, 190)
    text(canvas, "CUP", 58, 318, 2, WHITE)
    text(canvas, "SPHERE", 58, 550, 2, WHITE)

    payload = encode(bytes(canvas))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_bytes(payload)
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Generate FoldCanvas' deterministic repository social preview."
    )
    parser.add_argument("--output", type=pathlib.Path, default=OUTPUT)
    args = parser.parse_args()
    payload = generate(args.output.resolve())
    print(f"Generated {args.output.resolve()} ({len(payload)} bytes).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
