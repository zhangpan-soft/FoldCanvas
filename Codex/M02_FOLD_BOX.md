# M02: Fold operator and box proof

## Visible proof

Six rectangular regions from one 2D appearance canvas form a box. Each face retains its original artwork and orientation.

## Source design

Use six rectangle panels located on one appearance canvas. Their topology and fold relationships are explicit. Do not create a cube with Unity primitives and pretend it came from the canvas.

A Fold operation is authored at a high level. Users do not author vertices, matrices, or trigonometric expressions.

## Required Fold semantics

For MVP, a Fold operation targets one panel and a line in that panel's normalized source coordinates.

Inputs:

- panel ID
- line start/end in normalized panel coordinates
- positive or negative side
- angle in degrees
- zero falloff for this milestone

Behavior:

1. Resolve the fold line into the panel's current 3D embedding.
2. Classify source vertices by signed 2D side of the line.
3. Rotate the selected vertices around the current 3D hinge axis.
4. Vertices exactly on the hinge remain fixed within tolerance.
5. Preserve UV and source provenance.
6. Execute folds in list order.

If a fold line cannot be represented as one stable 3D axis after earlier deformation, return a diagnostic rather than guessing.

## Box sample

Create an editor-generated sample canvas with six visibly different face regions and a FoldCanvas source asset that folds into a box through ordered 90-degree operations.

## Numerical tests

- 0-degree fold is identity
- +90 and -90 obey documented handedness
- hinge vertices remain fixed
- off-hinge distances to axis are preserved
- two repeated compiles are deterministic
- box face normals point outward after full sequence
- artwork UVs remain unchanged

## Diagnostics

Add stable codes for:

- degenerate fold line
- fold target missing
- ambiguous/non-linear hinge in current embedding
- non-finite angle
- unsupported nonzero falloff

## Non-goals

- no smooth Bend field
- no collision/self-intersection correction
- no seam welding
- no thickness
