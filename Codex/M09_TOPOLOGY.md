# M09: Handle cup and torus

## Visible proof

1. A handle attaches to two regions of the cup without non-manifold seams.
2. A torus compiles from a 2D parameter domain with both directions closed.

## Goal

Demonstrate that FoldCanvas can encode non-trivial loops rather than only genus-zero shells.

## Required design work

Write an ADR choosing the minimal additional topology vocabulary. Candidate approaches:

- strip/tube panel with two attachment boundaries
- cyclic rectangle domain with U and V closure seams
- explicit loop boundary types
- sweep operation as a high-level source instruction

Do not smuggle in an arbitrary prebuilt torus or handle mesh.

## Tests

- torus Euler characteristic
- no open boundaries
- no non-manifold edges
- stable major/minor radius
- handle attachment seam integrity
- UV continuity behavior is documented
- deterministic topology across compiles

## Non-goals

- arbitrary constructive solid geometry
- mechanical Boolean solver
- skeletal deformation
