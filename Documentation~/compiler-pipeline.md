# Compiler pipeline

## Stage 0: source loading

- load ScriptableObject or FoldScript
- resolve appearance references
- normalize units to meters
- preserve stable source identifiers

No geometry is generated before source-level validation completes.

## Stage 1: source validation

Validate:

- schema version
- unique panel, seam, and operation IDs
- canvas regions inside `[0,1]`
- valid physical dimensions
- valid tessellation counts
- existing panel and boundary references
- finite numeric parameters
- operation ordering prerequisites

## Stage 2: panel tessellation

Each panel tessellator emits:

```text
currentPosition3D
sourcePanelPosition2D
sourceCanvasUv0
triangles
ordered boundaries
panel ownership
provenance ID
```

Output order must be stable and documented.

The compiler freezes this data into `FoldCanvasCompiledData` before creating a
Unity `Mesh`. Its vertex, triangle, panel, and boundary collections are
read-only snapshots. A rigid transform changes only `currentPosition3D`;
source position, UV, ownership, provenance, topology, and boundary order remain
unchanged.

Each internal panel record also retains its deterministic triangle-index span.
This lets later operations map source points through one panel's triangulation
without global searches or dictionary enumeration.

## Stage 3: operation execution

Operations modify explicit geometry buffers. The MVP uses a stable ordered list.

For every operation:

1. resolve target panels or regions
2. validate parameters
3. evaluate mapping
4. update 3D positions and any local frames
5. preserve UV0
6. record operation-specific diagnostics

### M02 rigid-crease Fold

A Fold line is authored in normalized panel coordinates. Before moving any
vertex, the compiler verifies that both endpoints are existing source vertices
and that the full parameter interval is covered without gaps by collinear
existing triangle edges. A crease that crosses triangle interiors returns
`FC3011 FoldCreaseRequiresTopologySplit`; M03 does not insert vertices or
silently approximate the fold.

For an existing edge chain, the compiler maps its endpoints, every
source-triangle-edge crossing, and every interval midpoint through barycentric
coordinates into the panel's current 3D embedding. Since the mapping inside
each source triangle is affine, these samples determine whether the full hinge
is one stable straight axis.

If the mapped samples are non-linear, reversed, collapsed, non-finite, or
outside the panel triangulation, compilation stops with
`FC3007 AmbiguousFoldHinge`. Otherwise the selected positive or negative source
side is rotated using `Quaternion.AngleAxis` around the directed current axis.
Hinge samples stay fixed, and all source/provenance/topology data is preserved.
Only zero falloff is supported in M02.

### M03 current-frame Roll

Roll accepts a rectangle only when its complete current vertex set is a finite,
non-degenerate, rigid planar embedding of its immutable source grid. The
compiler deterministically resolves `CurrentOrigin`, unit `CurrentU`, unit
`CurrentV`, and `CurrentNormal = cross(CurrentU, CurrentV)` from ordered
rectangle corners, then validates every target vertex against that frame.
Translation and rotation composed before Roll are retained. Scale, shear, or a
prior non-planar Fold returns `FC3021 UnsupportedRollEmbedding`.

The selected source coordinate maps to a circular arc in this current frame;
the other coordinate remains linear. Positive sweeps preserve source triangle
order and face radially outward. Negative sweeps preserve topology and reverse
the resulting radial orientation predictably. Full turns require at least two
samples in the selected direction, and coincident minimum/maximum boundaries
remain topologically separate.

Explicit-radius Roll emits an ordered structured
`FC3018 RollStretchReport` containing `sourceSpan`, `arcLength`, and
`stretchRatio`. Diagnostic value and repair-suggestion lists are copied,
read-only, and deterministic.

## Stage 4: seam resolution (planned for M04)

Seam definitions are declarative source records. Their presence alone does not
execute or reject geometry. Only a future Stitch operation starts the following
pipeline; before M04, Stitch returns one `FC3001 UnsupportedOperation`.
`FitTargetBoundary` returns one dedicated
`FC3016 UnsupportedFitTargetBoundary`.

For each requested seam:

1. extract ordered boundary curves
2. evaluate current 3D arc lengths
3. determine orientation
4. resample to common parameters when necessary
5. weld, bridge, or retain as requested
6. update ownership and boundary maps

Seams must be processed in a deterministic order, normally source array order after validation.

## Stage 5: thickness

- classify closed versus open boundaries
- generate inner shell
- reverse inner triangles
- create rim/side-wall topology only where needed
- maintain source-to-generated vertex provenance

## Stage 6: derived attributes

- triangle normals
- vertex normals according to smoothing policy
- bounds
- optional tangents
- optional secondary UVs in later versions

## Stage 7: validation

- finite coordinates
- non-zero triangle area
- consistent winding where expected
- open-boundary count
- manifold edge count
- duplicate/coincident vertices
- seam closure error
- self-intersection when enabled

## Stage 8: artifact creation

The runtime compiler returns an in-memory result. Editor code may save:

- `.mesh` asset
- material
- validation report
- prefab
- source hash metadata

Editor saving must not change geometry output.

Configured cumulative vertex and triangle limits are validated before panel
tessellation. Requests that exceed either limit return `FC1007` without
allocating partial geometry.

## Stage 9: feedback loop

Diagnostics are structured so humans and AI can repair the source:

```text
FC2104 SeamLengthMismatch
panel=wall boundary=vMin
panel=bottom boundary=perimeter
relativeDifference=0.032
suggestions=[increase wall width, choose fitTargetBoundary, enable resampling]
```
