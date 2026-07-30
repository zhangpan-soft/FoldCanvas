# M05: Sphere gores and spherical surface reconstruction

## Milestone state

M05 is active after M04/M04.1 human approval and PR #3 merge at `ef36808`.
Implementation occurs on `codex/m05-spherical-wrap`. The executable plan is
[`Docs/Plans/active-plan.md`](../Docs/Plans/active-plan.md).

## Core proof

M05 does not add a traditional sphere generator. It proves this source path:

```text
2D canvas and explicit panels
        ↓
SphericalWrap curvature mapping
        ↓
existing seam graph and arc-length correspondence
        ↓
Weld
        ↓
deterministic validation and Mesh compilation
        ↓
derived closed sphere
```

The source remains the canvas, panels, boundaries, seams, ordered operations,
and compile settings. A Unity Sphere primitive, UV Sphere, Icosphere, imported
or pre-generated sphere Mesh, and hard-coded final sphere vertex table are not
valid generation paths.

## Golden asset

`Samples~/Sphere/` contains an eight-gore golden source. Every gore is an
independent rectangle parameter panel with:

- a stable panel ID
- its own canvas rectangle
- authored U/V tessellation
- ordered longitude-side boundaries
- one neighboring Weld seam on each side through the shared seam graph
- one `SphericalWrapOperationDefinition`

The source artwork visibly labels `NORTH`, `FOLDCANVAS`, and `SOUTH` so the
Unity proof can detect reversed or mirrored mapping.

## SphericalWrap source contract

`SphericalWrapOperationDefinition` contains:

- `PanelId`
- `Radius`
- `LatitudeRange`
- `LongitudeRange`
- `WrapDirection`
- `PoleMode`
- `SubdivisionMode`

M05 supports rectangle panels and `PanelGrid` subdivision. The panel's authored
segment counts remain the tessellation authority. Adaptive subdivision is a
future milestone.

### Current-frame requirement

Before wrapping, the target must remain a non-degenerate, congruent affine
embedding of its 2D source plane. M05 resolves:

- `CurrentOrigin`
- `CurrentU`
- `CurrentV`
- `CurrentNormal = normalize(cross(CurrentU, CurrentV))`

Translation, rotation, and orientation-reversing unit isometries are preserved.
Metric-changing scale, shear, collapsed axes, non-planarity, and a previously
curved target return one stable diagnostic and no Mesh.

The sphere center is `CurrentOrigin`. The radius is measured in meters in the
resolved current frame.

### Parameter mapping

For normalized longitude parameter `s` and latitude parameter `t`:

```text
longitude = lerp(LongitudeRange.x, LongitudeRange.y, s)
latitude  = lerp(LatitudeRange.x,  LatitudeRange.y,  t)

x = Radius * cos(latitude) * cos(longitude)
y = Radius * sin(latitude)
z = Radius * cos(latitude) * sin(longitude)

world = CurrentOrigin
      + x * CurrentU
      + y * CurrentV
      + z * CurrentNormal
```

`LongitudeAlongU` maps source U to longitude and source V to latitude.
`LongitudeAlongV` maps source V to longitude and source U to latitude.
Ranges may be increasing or decreasing. After mapping, the executor evaluates a
deterministic non-degenerate triangle against the radial direction and reverses
the complete panel triangle range only when required for outward winding.

Latitude endpoints must remain within `[-90, 90]` degrees and longitude span
must not exceed one full turn. Non-finite, zero-radius, collapsed-range, and
multi-turn inputs fail with diagnostics.

## Pole topology

Exact `-90` or `+90` latitude endpoints receive pole-aware source
tessellation before deformation. This is part of SphericalWrap topology
construction, not a post-generation mesh cleanup.

- `Merge` uses one render pole vertex per panel fan.
- `KeepFan` retains one render pole vertex per adjacent longitude cell for UV
  continuity, but every fan copy shares one logical topology identity.
- Pole rows emit one valid triangle per adjacent interior edge rather than a
  collapsed quad pair.
- Neighboring gore side Welds transitively merge the panel pole identities into
  exactly one north and one south `TopologyVertexId`.
- No pole triangle may contain the same logical topology identity twice.
- No unused duplicate pole vertex is emitted.

Full-pole panels require at least one non-pole latitude row.

## Seam graph

Sphere construction reuses `SeamDefinition`, `StitchOperationDefinition`, and
the M04 normalized current-space arc-length correspondence solver.

- unequal boundary counts retain all authored normalized breakpoints
- missing correspondence samples subdivide the actual adjacent surface
- interpolated source position, source UV, panel ownership, and provenance are
  preserved
- a boundary sample inserted on a wrapped panel is re-evaluated through that
  panel's recorded spherical mapping instead of remaining on a straight chord
- Weld shares logical topology while UV/provenance render splits may remain
- no spatial-proximity weld is inferred

SphericalWrap must occur before Stitch. The existing rule that Stitch is the
terminal position-deforming operation for selected panels remains active.
Solidify may consume the final welded sphere in a later operation.

Enabled SphericalWrap panels form components through Stitch-selected seams
whose two endpoints are spherical. Validate each component after its last
relevant Stitch and before the first Solidify selecting that component.
Unrelated Stitch and Solidify operations must not affect this lifecycle, and
Solidify cannot replace the zero-thickness sphere proof. Preserve the
pre-Solidify report in `SphereReports`; `SphereReport` is the first-report
compatibility view.

## Validation

The golden sphere must report:

- one connected closed component
- zero open logical edges
- zero non-manifold logical edges
- zero orientation conflicts
- zero collapsed logical edges
- non-zero oriented volume
- Euler characteristic `V - E + F = 2`
- exactly one north and one south pole topology identity
- every surface position at the requested radius within the documented
  tolerance
- preserved source UV, canvas coordinates, panel ownership, and provenance
- deterministic vertices, triangles, topology IDs, reports, and diagnostics

This validation does not include a global triangle-triangle
self-intersection test. Do not describe the M05 report as a universal
no-self-intersection proof.

The compiled result also exposes immutable SphericalWrap metadata needed by the
Editor proof, including center, frame, radius, ranges, pole mode, maximum
radius error, and deterministic UV-stretch samples.

## Required tests

- `Sphere_HasClosedTopology`
- `Sphere_HasNoNonManifoldEdges`
- `Sphere_HasEulerCharacteristicTwo`
- `Sphere_RadiusErrorWithinTolerance`
- `Sphere_PolesAreMerged`
- `Sphere_SourceUvPreserved`
- `Sphere_RegenerationIsDeterministic`
- `Sphere_UnequalSeamSamplesRemainOnRadius`
- `Sphere_HasOutwardWinding`
- `SphericalWrap_AfterRigidTransform_PreservesCurrentFrame`
- `SphericalWrap_AfterUnitReflection_HasOutwardWinding`
- `SphericalWrap_InvalidEmbedding_ReturnsStableDiagnostic`
- `SphericalWrap_InvalidRadius_ReturnsStableDiagnostic`
- `SphericalWrap_MultiTurnLongitude_ReturnsStableDiagnostic`
- `SphericalWrap_InsufficientPoleTessellation_ReturnsStableDiagnostic`
- `SphericalWrap_KeepFan_PreservesRenderUvSplitsAndOneTopologyPole`
- `Sphere_CanContinueSolidify`
- `OpenSphere_FollowedBySolidify_IsRejected`
- `UnrelatedSolidify_DoesNotSuppressSphereValidation`
- `UnrelatedStitch_DoesNotTriggerSphereValidation`
- `TwoIndependentSphereComponents_AreValidatedIndependently`
- `SphereReport_PreservesPreSolidifyEvidence`
- `GoldenSphere_ThreeCompilesHaveStableCountsReportsAndHash`
- cumulative Panel/Stitch/Solidify budget and rollback coverage
- native `sampleCount` range and scale-aware pole coverage

Existing M00-M04.1 tests must remain intact.

## Editor proof

`Tools > FoldCanvas > Create Sphere Sample` creates the source asset.
`Tools > FoldCanvas > Create Sphere Proof` compiles it and creates or reuses
one inactive-aware `EditorOnly` hierarchy:

```text
FoldCanvas Sphere Root
├── Source Canvas Preview
├── Generated Sphere
├── Solid Validation
├── Wireframe Debug
├── Seam Debug
├── Pole Debug
├── UV Stretch Debug
├── Radius Error Debug
├── Validation Report
└── Preview Camera
```

The visible proof must include:

- the 2D gore canvas
- a texture-free one-sided solid sphere
- the source-textured sphere with readable artwork
- unique logical-topology wireframe
- seam lines
- north/south pole markers
- UV-stretch visualization
- radius-error visualization/report

Repeated execution must reuse the owned hierarchy and must not read or modify
`Camera.main`.

## Diagnostics

M05 adds stable diagnostics for:

- missing or unsupported target panel
- non-finite or invalid spherical parameters
- unsupported current embedding
- collapsed latitude or longitude range
- unsupported multi-turn longitude span
- insufficient pole tessellation
- unsupported subdivision mode
- radius error outside tolerance
- invalid pole topology
- sphere validation failure
- Solidify ordered before required sphere validation
- cumulative generated vertex/triangle budget exhaustion or arithmetic
  overflow
- native Stitch `sampleCount` above `8192`

Invalid geometry returns diagnostics and no Mesh. The compiler must never
substitute a hidden sphere generator or automatic topology repair.

## Non-goals

- no Unity Sphere primitive, UV Sphere, Icosphere, imported sphere, or fixed
  final sphere vertex table
- no arbitrary spherical unfolding
- no adaptive tessellation
- no geodesic optimality claim
- no texture seam painting algorithm
- no Bevel
- no subdivision-surface smoothing
- no Remesh
- no Mesh Cleanup
- no automatic topology repair
- no M06 or later milestone

M05 completion waits for human audit.
