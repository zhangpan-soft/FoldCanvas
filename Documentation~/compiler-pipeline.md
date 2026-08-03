# Compiler pipeline

## Stage 0: source loading

- a native `FoldCanvasAsset` enters directly, or M08 parses bounded untrusted
  FoldScript JSON into explicit `0.1` DTOs;
- reject malformed/duplicate JSON properties, unsupported versions or
  operations, unknown/missing fields, invalid identifiers and references, and
  size/depth/node/string/collection limit violations;
- normalize and resolve appearance references through the host-supplied
  resolver; Runtime itself performs no file or network I/O;
- convert physical values from meter/centimeter/millimeter documents into
  native meters while preserving stable source IDs and authored array order;
- retain canonical portable metadata and compile settings on the native source.

No geometry is generated before source-level validation completes. Canonical
export reverses the explicit DTO/native conversion; it never serializes Unity's
private object layout as the interchange format.

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
- native and JSON seam `sampleCount` inside the shared `[0,8192]` contract

One `GeometryBudget` is created for the compile. Panel tessellation, Stitch
boundary subdivision and Bridge triangles, and Solidify shell/rim geometry
all consume the same cumulative vertex and triangle limits. Each
geometry-producing operation computes its exact additional counts and reserves
them before mutation; the build buffer also guards every vertex and triangle
append. Failed operation transactions restore vertices, triangles, topology,
panel boundaries, spherical-surface membership, and budget usage.

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
Pole classification is scale-aware: an endpoint must satisfy both the small
angular deviation check and
`radius * angularDeviationRadians <= compile.weldEpsilon`. Exact `+/-90`
degrees always has zero deviation. A near-pole latitude on a very large sphere
therefore remains an ordinary ring instead of being silently collapsed.

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
5. when `sampleCount > 0`, add a uniform minimum-density parameter grid;
   native and JSON inputs share the maximum value `8192`
6. sort all missing parameters, build triangle-edge adjacency once, and split
   each affected boundary segment as one deterministic triangle fan
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
therefore consume the closed-loop topology created by an earlier seam. The
complete Stitch operation is transactional: if any selected seam fails,
earlier seams from that same operation are rolled back.

Before component planning or tessellation, every Stitch-selected seam must
resolve to one non-empty seam ID. Both endpoint panel IDs and boundary IDs must
be non-empty, each panel must exist, and each boundary must be a built-in
boundary of that panel shape. Invalid native references return `FC2001`,
`FC2003`, `FC2004`, or `FC2008`; component planning independently guards all
string dictionary keys as defense in depth.

### Terminal-Stitch ordering

Until shared topology groups participate in deformation propagation, the
compiler treats every panel selected by a Stitch as position-final. A later
`RigidTransform`, `Fold`, `Roll`, or `SphericalWrap` targeting any such panel fails with
`FC2010 StitchMustBeTerminalForSelectedPanels` and returns no Mesh. Operations
on unrelated panels remain legal.

The same terminal contract is preflighted in the forward direction before
panel tessellation: when a selected seam endpoint has an enabled
`SphericalWrap`, its operation index must be strictly less than the selecting
Stitch index. A violation returns `FC2010` with
`sphericalWrapOperationIndex` and `stitchOperationIndex`; neither
`StitchExecutor` nor sphere validation runs. Component planning independently
requires the last touching Stitch to be later than the maximum member-wrap
index, so bypassing source preflight still cannot schedule an early report.

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

After all enabled operations and before Unity Mesh creation, M07 validates the
explicit build buffer according to `compile.validationLevel`:

- Basic: index-buffer structure and bounds, finite positions, collapsed and
  zero-area triangles, duplicate topology faces, edge incidence, and local
  winding;
- Standard: logical-topology position agreement, bow-tie fans, compiled
  boundary length, executed-Weld closure, closed-component orientation, plus
  open-boundary and disconnected-component warnings;
- Strict: deterministic sweep-and-prune candidates followed by exact
  separating-axis triangle tests for pairs that share no topology vertex.

Fatal checks use stable root-cause precedence and stop their dependent checks.
Components are ordered by minimum topology ID, edges and triangle pairs are
lexicographically ordered, and the validator never edits the buffer. Strict
fails with `FC5019` rather than degrading after 250000 candidates. Confirmed
overlap returns `FC5018`; broad-phase candidates alone are not errors.

The compiler exposes this evidence through the read-only
`GeometryValidationReport`. Open sheets and deliberately disconnected assets
remain valid results, so their Standard diagnostics are warnings. Full details
are in [M07 geometry validation](geometry-validation.md).

Every successful compile exposes a read-only
`FoldCanvasClosedVolumeReport`. Open source panels remain valid compile
results, but their report has `IsClosedVolume = false`. A cup produced by
Solidify must have `IsSingleClosedVolume = true`. This bounded M04.1 report
does not claim global self-intersection detection or mesh repair.

Enabled `SphericalWrap` panels form a spherical component only through seams
selected by enabled Stitch operations where both seam endpoints are spherical
panels. These are component-forming seams. After formation, every Stitch whose
selected seam has either endpoint in a component is component-touching,
including a Bridge or Weld to an ordinary panel. Each component is validated
immediately after its last touching Stitch and before the first Solidify that
selects any panel in that component. A Solidify on unrelated panels neither
triggers nor suppresses the check. A Solidify ordered before the component's
last touching Stitch returns
`FC6016 SphereValidationRequiredBeforeSolidify`.

The compiler stores one read-only report per component in `SphereReports`.
`SphereReport` remains a compatibility view of the first report. Every report
records the component ID, ordered panel and SphericalWrap operation IDs,
validation stage, validation operation ID, and operation index. The
pre-Solidify zero-thickness evidence is never overwritten by a later shell
report. Each component must satisfy:

- one connected spherical component
- zero open, non-manifold, orientation-conflict, and isolated logical vertices
- Euler characteristic `V - E + F = 2`
- one logical north pole and one logical south pole
- zero inward triangles and one consistent center/radius frame
- maximum radial error within the centralized absolute-plus-relative tolerance

Failure returns `FC6014 SphereValidationFailed` and no Mesh. Solidify cannot
turn an open spherical surface into acceptable M05 evidence: the original
surface must pass first. A later Solidify is still allowed and is independently
validated by the operation-scoped closed-volume contract because its
inner/outer positions intentionally no longer lie on the original radius.

The sphere report proves the listed topological, radius, frame, and winding
properties at its operation-specific stage. It is not by itself a global
self-intersection proof. A successful final Strict M07 report adds exact
non-adjacent triangle-intersection evidence over the final build buffer.

## Stage 8: artifact creation

The runtime compiler returns an in-memory result. Editor code may save:

- `.mesh` asset
- material
- validation report
- prefab
- source hash metadata

Editor saving must not change geometry output.

Configured cumulative vertex and triangle limits cover the full compile, not
only panel tessellation. Unsafe source tessellation returns `FC1007`;
operation-level overages return `FC5005`, `FC5006`, or `FC5007`. No partial
Mesh is returned and the failing operation transaction does not retain partial
geometry or consumed budget.

## Stage 9: feedback loop

M08 copies ordered compiler diagnostics into a provider-neutral repair request:

```text
schemaVersion=0.1
compilerVersion=0.1.0-preview.16
assetId=cup
source=<canonical complete FoldScript>
diagnostics[0].code=FC3022
diagnostics[0].operation=roll-wall
diagnostics[0].suggestions=[increase source tessellation]
```

The request contains no Mesh, vertex/triangle buffer, appearance pixels,
credentials, or provider metadata. An external adapter may return complete
replacement FoldScript JSON. `FoldCanvasRepairCoordinator` sends that response
back through Stage 0 and every ordinary compile stage; there is no privileged
patch or geometry-mutation path. M08 does not execute a network request or
automatically accept a repair.
