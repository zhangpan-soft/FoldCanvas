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
non-degenerate, congruent planar embedding of its immutable source grid. The
compiler deterministically resolves `CurrentOrigin`, unit `CurrentU`, unit
`CurrentV`, and `CurrentNormal = cross(CurrentU, CurrentV)` from ordered
rectangle corners, then validates every target vertex against that frame.
Translation, rotation, and orientation-reversing planar isometries are
retained. In-plane metric-changing scale, shear, collapsed axes, or a prior
non-planar Fold returns `FC3021 UnsupportedRollEmbedding`. This is a
final-geometry contract and does not inspect which operation types occurred.

The selected source coordinate maps to a circular arc in this current frame;
the other coordinate remains linear. Its angle is
`startAngleDegrees - t * angleDegrees`. Roll reverses each target triangle's
winding once without changing connectivity, so a positive sweep faces
radially outward and authored source U reads left-to-right at the canonical
exterior view. Negative sweeps reverse the resulting radial orientation
predictably. Full turns require at least three source segments in the selected
direction. With two segments the
0/180/360-degree samples form two coincident planar panels, so
`FC3022 InsufficientRollTessellation` is emitted directly rather than relying
on zero-area-triangle validation. Coincident minimum/maximum boundaries remain
separate until an explicit Stitch operation selects their seam.

M03 Circular Roll is limited to one signed turn. A sweep magnitude above 360
degrees returns `FC3023 UnsupportedMultiTurnRoll`; it is never clamped and
never emitted as overlapping cylindrical layers. Spiral and layered roll
geometry are separate future operations.

Explicit-radius Roll emits an ordered structured
`FC3018 RollStretchReport` containing `sourceSpan`, `arcLength`, and
`stretchRatio`. Diagnostic value and repair-suggestion lists are copied,
read-only, and deterministic.

### M05 current-frame SphericalWrap

`SphericalWrap` is a deformation of one explicit rectangle parameter panel,
not a request to generate a sphere. Before mapping, the compiler verifies the
complete current panel is one finite, non-degenerate, congruent planar
embedding and resolves:

```text
CurrentOrigin
CurrentU
CurrentV
CurrentNormal = normalize(cross(CurrentU, CurrentV))
```

Prior translation, rotation, and unit reflection are retained. Metric-changing
scale, shear, axis collapse, or earlier non-planarity returns
`FC6010 UnsupportedSphericalEmbedding` and no Mesh.

For normalized source coordinates, `wrapDirection` selects which axis supplies
longitude and which supplies latitude. The mapped position is:

```text
P = CurrentOrigin
  + radius * cos(latitude) * cos(longitude) * CurrentU
  + radius * sin(latitude)                  * CurrentV
  + radius * cos(latitude) * sin(longitude) * CurrentNormal
```

M05 accepts latitude endpoints only inside `[-90, 90]` and at most one signed
longitude turn. The panel's authored `PanelGrid` segment counts remain the
sampling contract. If both latitude endpoints are poles, at least two latitude
segments are required so a non-pole row exists.

Pole topology is decided during tessellation. `Merge` emits one render pole
per panel fan. `KeepFan` retains one render copy per adjacent longitude cell
for UV/provenance continuity while assigning every copy the same logical
topology identity. No later vertex-collapse or mesh-cleanup stage is used.

Each panel's first non-degenerate triangle determines whether its indices must
be reversed; every emitted triangle is then checked for positive radial dot
product. The compiler stores immutable frame, range, pole, source, radius, and
area-stretch metadata for seam projection, validation, and Editor
visualization.

## Stage 4: explicit seam resolution

Seam definitions are declarative source records. Their presence alone does not
execute or reject geometry. A Stitch operation resolves only its ordered seam
ID list. M04 processes each selected seam as follows:

1. resolve both ordered boundaries and their open/closed state
2. apply the declared B orientation
3. parameterize each boundary by normalized current-space cumulative arc
   length
4. retain the sorted union of both authored breakpoint sets
5. when `sampleCount > 0`, add a uniform minimum-density parameter grid
6. insert every missing sample by splitting its boundary edge and exactly one
   adjacent source-surface triangle
7. interpolate current position, immutable source position, UV0, panel
   ownership, and deterministic provenance
8. for `Weld`, require every pair within `compile.weldEpsilon`, union logical
   topology IDs, and snap render copies to the deterministic representative
9. for `Bridge`, emit a consistently wound strip without unioning the two
   boundary topology sets

When a selected boundary belongs to a spherical surface, inserted samples
retain the interpolated immutable source coordinate and UV but recompute their
current position through that panel's recorded spherical evaluator. Meridian
boundary distances use exact spherical arc length. This prevents unequal
sample counts from leaving new vertices on straight chords inside the sphere,
and it preserves the authored minimum/maximum longitude side even where both
sides meet at an exact pole.

Topology and manifold validation use `TopologyVertexId`; raw render indices are
not a reliable topological oracle at attribute seams. `Hinge` and `KeepOpen`
remain declarative. `FitTargetBoundary` returns one dedicated
`FC3016 UnsupportedFitTargetBoundary`.

Seams are processed in the Stitch operation's listed order. A later seam may
therefore consume the closed-loop topology created by an earlier seam.

### Terminal-Stitch ordering

Until shared topology groups participate in deformation propagation, the
compiler treats every panel selected by a Stitch as position-final. A later
`RigidTransform`, `Fold`, `Roll`, or `SphericalWrap` targeting any such panel fails with
`FC2010 StitchMustBeTerminalForSelectedPanels` and returns no Mesh. Operations
on unrelated panels remain legal.

`Solidify` is not a post-Stitch per-panel deformation. It consumes the complete
final stitched topology in Stage 5 and must construct both shell sides and
their rims without separating render copies that share one logical topology
identity.

## Stage 5: thickness

M04 Solidify consumes complete selected logical-topology components:

1. reject non-finite/non-positive thickness, missing panels, partial welded
   groups, partial Bridge triangles, and non-manifold source topology
2. collect oriented incident face planes per logical topology vertex
3. solve one deterministic bounded offset-plane miter at smooth and hard
   corners
4. position the outer and inner layers according to `inward`, `outward`, or
   `centered`
5. preserve UV0/provenance on both shell copies and reverse inner winding
6. classify source logical edges by incidence after Stitch
7. generate a rim strip exactly once for every incidence-one edge and never
   across an already welded seam
8. record paired outer/inner segments for deterministic material hard corners
9. validate only the selected generated shell as a closed oriented volume

The final M04 cup therefore has one top rim, no internal wall at the wall
closure or wall-to-bottom seam, and no open or non-manifold topology edge.

The M04.1 closed-volume check requires every selected logical edge to have
exactly two oppositely directed triangle uses, every logical topology identity
to resolve to one position, and every connected component to have non-zero
absolute signed volume. It reports
`FC4007 SolidifyClosedVolumeValidationFailed` and returns no Mesh if the
selected Solidify shell violates that contract. Unrelated unsolidified panels
are not included in the operation-scoped check.

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
- logical-topology position agreement
- connected component count
- signed and absolute component volume
- duplicate/coincident vertices
- seam closure error
- self-intersection when enabled

Every successful compile exposes a read-only
`FoldCanvasClosedVolumeReport`. Open source panels remain valid compile
results, but their report has `IsClosedVolume = false`. A cup produced by
Solidify must have `IsSingleClosedVolume = true`. This bounded M04.1 report
does not claim global self-intersection detection or mesh repair.

When multiple spherical panels are joined by Stitch and no later Solidify
replaces their zero-thickness surface, the compiler also requires a read-only
`FoldCanvasSphereReport` to satisfy all of:

- one connected spherical component
- zero open, non-manifold, orientation-conflict, and isolated logical vertices
- Euler characteristic `V - E + F = 2`
- one logical north pole and one logical south pole
- zero inward triangles and one consistent center/radius frame
- maximum radial error within the centralized absolute-plus-relative tolerance

Failure returns `FC6014 SphereValidationFailed` and no Mesh. A later Solidify
is allowed and is validated by the existing operation-scoped closed-volume
contract because its inner/outer positions intentionally no longer lie on the
original zero-thickness radius.

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
