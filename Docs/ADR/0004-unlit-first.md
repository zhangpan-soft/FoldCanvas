# ADR 0004: Unlit appearance is the MVP reference

- Status: Accepted
- Date: 2026-07-25

## Decision

The first proof uses a single appearance canvas and an Unlit-compatible material. Geometric normals are still derived for validation and optional lighting, but PBR authoring is not a prerequisite.

## Rationale

The central hypothesis concerns 2D source geometry and compilation, not material inference. Unlit output makes artwork preservation easy to verify and avoids coupling to URP or HDRP.
