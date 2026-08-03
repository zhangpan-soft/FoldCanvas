# Current task

Execute **M07: Geometry validator**.

Authoritative task file:
[`Codex/M07_VALIDATOR.md`](Codex/M07_VALIDATOR.md)

M05 PR #4 and M06 PR #5 were merged into `main` in that order. M07 development
occurs on `codex/m07-geometry-validator`, created from merged `main` commit
`f5c8116`.

M07 turns generated-geometry failures into deterministic, localized compiler
diagnostics. Validation must cover structural mesh safety, topology, winding,
seam closure, connected components, and strict triangle self-intersection
without editing or repairing the derived Mesh.

Validation levels are part of the source contract:

- `Basic`: finite/index/degeneracy/duplicate and fatal manifold safety;
- `Standard`: Basic plus boundaries, components, seam closure, bow-tie and
  closed-component orientation evidence;
- `Strict`: Standard plus deterministic broad-phase candidates and exact
  triangle-intersection confirmation.

Diagnostics must have stable code/order and structured panel, seam, operation,
component, triangle, topology-vertex, or edge context where available. One
root-cause diagnostic per issue category is preferred over cascaded floods.

M07 must reuse the deterministic M00-M06 compiler and Editor diagnostics UI.
It must not silently repair topology, edit generated Meshes as source, add a
cleanup/remesh stage, implement M08 FoldScript/AI round-tripping, or begin M09
non-trivial topology.
