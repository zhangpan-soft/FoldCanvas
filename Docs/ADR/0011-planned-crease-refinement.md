# ADR 0011: Off-grid Fold creases refine source topology before tessellation

- Status: Accepted
- Date: 2026-08-13

## Context

M02 Fold rotates existing vertices around a current 3D hinge. M03 correctly
rejected a crease crossing triangle interiors because rotating only the
pre-existing vertices stretches triangles across the hinge. Adding vertices at
operation-execution time would solve that local symptom but break a broader
compiler invariant: each panel owns one contiguous vertex and triangle range,
which Roll, Stitch, Solidify, extension operations, metadata, and exports all
consume.

The compiler already knows every enabled operation before it emits panels.
Crease coordinates are immutable normalized source coordinates, so compatible
straight cuts can be planned without evaluating current 3D deformation.

## Decision

Before allocating the `MeshBuildBuffer`, the compiler builds an ordered,
immutable refinement plan from supported enabled Fold operations. Each affected
rectangle is tessellated into local source geometry, then clipped in operation
order by its off-grid crease segments. Intersections are canonicalized on
undirected local edges, source UVs are evaluated from the panel `CanvasRect`,
crossed boundary lists are updated in their original direction, and each side
is triangulated with a stable source-order fan.

The final refined local panel is appended once to the global buffer. Its
vertices and triangles therefore remain contiguous, and all later operations
use the same explicit topology. Fold execution still resolves the hinge in the
panel's current embedding and applies `Quaternion.AngleAxis`; planning creates
edges but performs no 3D deformation.

M24 accepts straight rectangle creases whose endpoints lie on the perimeter
and whose open segment partitions the panel. Existing edge chains are not
refined. A zero-angle Fold is an exact identity and performs neither refinement
nor hinge resolution because no source point can move. Unsupported curved,
branched, collinear-overlap, disk, or
interior-ending cases continue to fail as `FC3011` rather than approximate.

## Consequences

- Off-grid folds preserve the source-first 2D canvas plus FoldScript model.
- Panel range and downstream-operation contracts remain intact.
- Multiple compatible Fold creases refine deterministically in authored order.
- Geometry estimates must include refinement before any buffer mutation.
- The public operation schema does not change, but successful behavior expands;
  release versioning is handled by a later explicit release milestone.
