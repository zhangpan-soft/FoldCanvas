# M24: Deterministic off-grid crease topology split

## Outcome

Compile a straight Fold crease that crosses rectangle triangle interiors by
deterministically inserting source vertices and crease edges before executing
the existing exact rigid-hinge deformation.

## In scope

- enabled rigid `Fold` operations on rectangle panels;
- finite, non-degenerate straight creases whose endpoints lie on the rectangle
  perimeter and whose open segment partitions the panel;
- deterministic triangle clipping on both sides of the directed crease;
- exact source-position and source-UV interpolation at every inserted vertex;
- ordered updates to any crossed named rectangle boundary;
- existing-edge-chain Fold behavior without topology churn;
- exact zero-angle Fold identity without unnecessary topology churn;
- geometry-budget accounting, stable diagnostics, and repeated-compile proof;
- one Editor proof that shows the source grid, inserted crease, and folded
  result from the same 2D canvas plus FoldScript source.

## Acceptance

1. A `1 x 1` rectangle with one U and V segment and crease `(0.3, 0)` to
   `(0.3, 1)` compiles at `90` degrees without any triangle spanning the
   crease.
2. Each inserted crease vertex has deterministic source coordinates and UVs,
   and shared crossed edges reuse one render/topology vertex.
3. Crossed `vMin` and `vMax` boundaries contain the new vertices in their
   original direction; unaffected boundaries remain byte-identical.
4. Existing on-grid, boundary, signed-angle, ordered-fold, box, Roll, Stitch,
   Solidify, SphericalWrap, and ToroidalWrap behavior remains green.
5. Identical input produces identical vertices, topology identities,
   boundaries, triangle order, diagnostics, and Mesh indices.
6. The split is planned before panel tessellation, so every panel retains one
   contiguous vertex and triangle range and later operations consume the
   refined topology normally.
7. Required new geometry is included in the same fail-closed compiler budget;
   limit failure returns no Mesh and never leaves a partial split.
8. Exact-head repository checks, full Unity Edit Mode tests, proof generation,
   and maintainer audit pass before protected-main merge.

## Non-goals

- curved, branched, finite interior-ending, or collinear-overlap creases;
- smooth falloff, subdivision, bevel, remesh, or mesh-cleanup post-processing;
- splitting disk, spherical, or toroidal parameter domains;
- deforming a panel after Stitch, or propagating deformation across topology
  groups;
- SpiralRoll, LayeredRoll, or any generated Mesh becoming source;
- changing the public FoldScript `0.1` field shape.

## Diagnostics

`FC3011 FoldCreaseRequiresTopologySplit` remains a stable fail-closed
diagnostic for crease domains outside this milestone's supported straight
rectangle partition contract. It no longer applies to the accepted
boundary-to-boundary off-grid rectangle case.

## Rollback

The immutable public release `v1.0.1` remains the rollback. M24 work stays on
its branch until exact-head audit and hosted evidence pass. A failed refinement
plan returns diagnostics before Mesh construction; operation execution never
mutates authored panels, FoldScript, or appearance bytes.
