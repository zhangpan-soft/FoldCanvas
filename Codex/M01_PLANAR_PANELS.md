# M01: Planar panels and source provenance

## Visible proof

A decorated rectangle and decorated ellipse compile flat, with artwork pixels mapped to the expected corners and perimeter positions.

## Scope

- harden rectangle and disk/ellipse tessellation
- expose immutable compiled panel metadata for later operations
- preserve source 2D coordinates in addition to UV0
- standardize ordered boundaries
- centralize tolerances and source validation
- deterministic output tests

## Required representation changes

Introduce a compiler-owned mesh-data structure separate from the final Unity `Mesh`. It must retain, per generated vertex:

- current 3D position
- source panel-local 2D coordinate
- source canvas UV
- panel ownership
- provenance identifier sufficient for future duplication/welding

The Unity `Mesh` remains a final conversion step.

## Boundary contract

Rectangle:

- `uMin`: bottom to top
- `uMax`: bottom to top
- `vMin`: left to right
- `vMax`: left to right

Disk/ellipse:

- `perimeter`: counter-clockwise viewed from +Z

Tests must assert both vertex index order and boundary order.

## Validation

Add or complete diagnostics for:

- empty panel ID
- duplicate panel ID
- unsupported shape
- non-finite physical size
- non-positive size
- out-of-range canvas rect
- excessive tessellation that would exceed configured safety limits

## Non-goals

- no Fold
- no Roll
- no seam welding
- no thickness
- no polygon-with-holes
- no adaptive tessellation

## Acceptance criteria

- identical compiles produce byte-for-byte-equivalent vertex, UV, and index arrays within Unity serialization constraints
- no dictionary enumeration controls output order
- rectangle corner UVs match canvas rect exactly
- disk center maps to canvas rect center
- ellipse perimeter reaches expected physical radii
- all triangle fronts face +Z
- compiled panel metadata can retrieve named boundaries without exposing mutable compiler internals
