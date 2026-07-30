# Goal

Deliver M05 on `codex/m05-spherical-wrap` and prove that explicit 2D panels,
curvature rules, the existing seam graph, Weld, and the deterministic compiler
produce a genuine closed sphere. The generated Mesh is derived evidence, never
the editable source.

M04/M04.1 passed human audit and merged through PR #3 into `main` at merge
commit `ef36808`. The M05 baseline is Unity `6000.3.20f1` with 152/152 Edit
Mode tests.

# User-visible proof

The Editor command `Tools > FoldCanvas > Create Sphere Proof` must show:

1. the authored 2D eight-gore canvas
2. the generated sphere with readable `NORTH`, `FOLDCANVAS`, and `SOUTH`
3. the same sphere with a texture-free one-sided material
4. unique logical-topology wireframe
5. neighboring seam lines
6. north and south pole markers
7. UV-stretch visualization
8. radius-error visualization and validation report

The preview is owned by one inactive-aware `EditorOnly` root and is idempotent.
It does not use or modify `Camera.main`.

# Scope

## Source model

- Add `SphericalWrapOperationDefinition`, not a sphere generator.
- Use eight explicit rectangle parameter panels in the golden source.
- Keep panel CanvasRects, source positions, source UVs, provenance, boundaries,
  seams, operations, and compile settings authoritative.
- Reuse `SeamDefinition`, arc-length correspondence, Stitch, and Weld.
- Keep the sphere Mesh, wireframe, seam lines, pole markers, and heatmaps as
  derived artifacts.

## Mapping

- Resolve a congruent current panel frame after any preceding rigid isometry.
- Map normalized panel parameters to documented latitude/longitude ranges.
- Transform the local spherical coordinates through the resolved current frame.
- Support longitude along source U or source V.
- Correct final winding by testing a stable non-degenerate triangle against its
  radial direction.
- Record immutable per-panel spherical metadata for validation and Editor
  debugging.

## Pole topology

- Select pole-aware source tessellation before executing operations when an
  enabled SphericalWrap reaches exact north or south latitude.
- `Merge`: one render pole per panel fan.
- `KeepFan`: one referenced render pole per longitude cell, all sharing one
  logical topology identity.
- Emit a single fan triangle per pole-adjacent cell.
- Merge north and south topology transitively through neighboring gore side
  Welds.
- Emit no unused pole vertex and no triangle with a repeated logical vertex.

## Seam subdivision on curved panels

- Keep M04 normalized current-space arc-length pairing.
- Preserve every authored breakpoint.
- When correspondence inserts a sample, interpolate immutable 2D source data
  and evaluate current position with the owning panel's recorded spherical map.
- Never leave inserted seam points on a linear chord through the sphere.

## Validation

- Reuse the M04.1 closed-volume report.
- Add sphere-specific radius, pole, Euler characteristic, winding, and
  determinism checks.
- Return stable diagnostics and no Mesh for invalid parameters, embeddings,
  pole topology, or final sphere invariants.

# Non-goals

- Unity Sphere primitive
- UV Sphere or Icosphere generation
- imported or pre-generated sphere Mesh
- fixed final sphere vertex tables
- arbitrary spherical unfolding
- adaptive tessellation
- Bevel
- subdivision-surface smoothing
- Remesh
- Mesh Cleanup
- automatic topology repair
- texture seam painting
- M06 or later milestones

# Files expected to change

- `CURRENT_TASK.md`
- `Codex/M05_SPHERE_GORES.md`
- `Docs/Plans/active-plan.md`
- `Runtime/Data/FoldOperationDefinition.cs`
- `Runtime/Compiler/PanelTessellator.cs`
- `Runtime/Compiler/MeshBuildBuffer.cs`
- `Runtime/Compiler/SphericalWrapExecutor.cs`
- `Runtime/Compiler/BoundaryCorrespondenceSolver.cs`
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- compiled-data and diagnostic definitions
- JSON Schema and FoldScript documentation
- `Editor/FoldCanvasM05SphereSampleCreator.cs`
- `Samples~/Sphere/`
- `Tests/Editor/SphereCompilerTests.cs`
- Editor workflow tests
- `README.md`, `README.zh-CN.md`, `CHANGELOG.md`, and `package.json`

# Geometry invariants

## Coordinate frame

- source panels begin in XY with front `+Z`
- `CurrentOrigin` is the transformed source origin
- `CurrentU` and `CurrentV` are orthonormal unit axes
- `CurrentNormal = normalize(cross(CurrentU, CurrentV))`
- the sphere center is `CurrentOrigin`
- metric-changing scale, shear, collapse, or non-planarity is rejected

## Parameterization

- `LongitudeAlongU`: U is longitude and V is latitude
- `LongitudeAlongV`: V is longitude and U is latitude
- latitude is restricted to `[-90, 90]` degrees
- one operation longitude span is at most `360` degrees
- ranges may be increasing or decreasing
- output radius is measured from `CurrentOrigin`

## Winding

- compiled fronts point away from the recorded center
- signed parameter ranges and wrap direction do not change the outward-normal
  guarantee
- correction swaps the second and third index for the complete authored panel
  triangle range at most once

## Boundaries and poles

- rectangle boundary ordering remains source-coordinate ordered
- gore longitude sides run from the lower latitude endpoint to the upper
  latitude endpoint
- pole render splits may preserve UV/provenance, but all copies at one panel
  pole share one logical topology identity
- the neighboring seam graph, not spatial coincidence, connects gores
- after all Welds there is exactly one north and one south topology identity

## Tolerances

- angular pole/full-turn comparisons use explicit centralized tolerances
- current-frame congruence uses the established Roll frame scale-aware
  tolerance
- radius validation uses absolute plus relative tolerance
- zero-area tests continue to use the established triangle-area tolerance
- dictionary iteration never determines emitted order or diagnostic order

# Implementation steps

1. Advance the active task and lock mapping, winding, pole, seam, validation,
   and non-goal contracts in documentation.
2. Add serialized SphericalWrap enums/data, schema fields, and stable
   diagnostics.
3. Add deterministic pole-aware rectangle tessellation selected from enabled
   wrap operations before geometry operations execute.
4. Implement current-frame resolution, parameter mapping, outward winding,
   and immutable spherical metadata.
5. Re-evaluate inserted seam samples through spherical metadata during the M04
   boundary subdivision path.
6. Build the eight-gore golden asset and run neighboring Welds into one closed
   sphere.
7. Add closed topology, manifold, Euler, radius, pole, UV, outward winding,
   unequal-seam, current-frame, and determinism tests.
8. Add the owned solid, textured, wireframe, seam, pole, UV-stretch,
   radius-error, and report proof hierarchy.
9. Run the complete Edit Mode suite, repository checks, schema parsing,
   `git diff --check`, and actual Unity proof rendering.
10. Update package version/changelog, commit, push, and open a non-merged review
    PR. Keep `CURRENT_TASK.md` on M05.

# Test matrix

## Golden sphere

- `Sphere_HasClosedTopology`
- `Sphere_HasNoNonManifoldEdges`
- `Sphere_HasEulerCharacteristicTwo`
- `Sphere_RadiusErrorWithinTolerance`
- `Sphere_PolesAreMerged`
- `Sphere_SourceUvPreserved`
- `Sphere_RegenerationIsDeterministic`
- `Sphere_UnequalSeamSamplesRemainOnRadius`
- `Sphere_HasOutwardWinding`

## Operation contracts

- `SphericalWrap_AfterRigidTransform_PreservesCurrentFrame`
- `SphericalWrap_AfterUnitReflection_HasOutwardWinding`
- `SphericalWrap_InvalidEmbedding_ReturnsStableDiagnostic`
- `SphericalWrap_InvalidRadius_ReturnsStableDiagnostic`
- `SphericalWrap_MultiTurnLongitude_ReturnsStableDiagnostic`
- `SphericalWrap_InsufficientPoleTessellation_ReturnsStableDiagnostic`
- `SphericalWrap_KeepFan_PreservesRenderUvSplitsAndOneTopologyPole`

## Editor proof

- create/recreate keeps one owned root and stable object count
- inactive owned objects are reused
- existing MainCamera remains unchanged
- source, solid, textured, wireframe, seam, pole, UV-stretch, radius-error, and
  report objects exist

## Regression

- all existing M00-M04.1 Edit Mode tests remain enabled and unchanged
- repository validation
- JSON and asmdef parsing
- Runtime `UnityEditor` isolation
- `git diff --check`

# Risks and rollback

- Pole optimization could accidentally become generic cleanup. Keep it confined
  to pre-operation SphericalWrap tessellation and test exact emitted counts.
- Linear seam insertion could pull vertices inside the sphere. Store the
  spherical evaluator on the build buffer and test unequal seam counts.
- Existing panel range assumptions could break specialized topology. Keep each
  specialized panel contiguous and freeze the actual counts/ranges.
- Multiple unrelated spheres could be merged by position. Never infer pole
  identity from position; union only within one panel's declared pole mode and
  through explicit seams.
- Artwork can appear geometrically correct but mirrored. Use readable source
  labels plus numerical UV/orientation tests.
- Solidify may expose new corner cases after sphere Weld. Treat Solidify
  compatibility as a regression proof, not permission for mesh repair.

Rollback is one branch revert. No M05 source behavior is added to `main` until
human audit.

# Progress log

- 2026-07-30: PR #3 passed human audit and merged into `main` at `ef36808`.
- 2026-07-30: Created `codex/m05-spherical-wrap`.
- 2026-07-30: Selected explicit multi-panel gores, existing seams, and
  SphericalWrap rather than template replication or a sphere generator.
- 2026-07-30: Locked current-frame, outward-winding, pole-fan, curved seam
  insertion, radius, topology, and non-goal contracts before coding.
- 2026-07-30: Implemented SphericalWrap data, pole-aware tessellation,
  current-frame mapping, curved seam projection, compiled spherical metadata,
  closed-sphere validation, and diagnostics `FC6001`-`FC6015`.
- 2026-07-30: Added the deterministic eight-gore package sample, source
  canvas, idempotent owned Editor proof, one-sided solid, logical wireframe,
  seam/pole overlays, UV-stretch heatmap, radius-error heatmap, and report.
- 2026-07-30: Unity rendered and visually inspected all six proof captures.
  The readable labels are not mirrored; the radius-error surface is uniformly
  within tolerance.
- 2026-07-30: Unity `6000.3.20f1` passed the complete 174/174 Edit Mode suite.

# Decisions made

- Use eight explicit panels so the golden source exercises the real seam graph.
- Use rectangle parameter charts; the milestone explicitly allows
  longitude/latitude regions as the bounded first spherical source.
- `PanelGrid` is the only M05 subdivision mode. The field is serialized now so
  future adaptive policies can be added without redefining SphericalWrap.
- Pole topology is created before deformation. Removing degenerate triangles
  after sphere generation would be forbidden mesh cleanup.
- `Merge` and `KeepFan` differ only in render-chart preservation; both expose
  one logical pole per panel before neighboring Welds.
- Spherical mapping records stay on the in-memory build buffer so M04 seam
  subdivision can reconstruct inserted points from immutable source
  coordinates.
- Outwardness is verified from emitted geometry rather than hidden by a
  two-sided material.

# Final verification

- Package version: `0.1.0-preview.9`
- Unity Editor: `6000.3.20f1 (c9ba695d4f07)`
- Edit Mode: 174/174 passed, zero failed/skipped/inconclusive
- Test XML:
  `Project~/TestResults/M05SphereEditMode.xml`
- Golden source: 8 explicit rectangle gore panels and 8 explicit Weld seams
- Surface: 616 render vertices, 482 logical topology vertices, 960 triangles,
  and 1,440 unique logical edges
- Topology: one connected component, zero open edges, zero non-manifold edges,
  zero orientation-conflict edges, and zero isolated logical vertices
- Euler characteristic: `482 - 1440 + 960 = 2`
- Pole topology: north `1`, south `1`
- Winding: zero inward triangles
- Maximum radius error: `0 m`; tolerance
  `5.9999998711646185e-6 m`
- Determinism: `Sphere_RegenerationIsDeterministic` passed for vertex count,
  triangle order, topology IDs, and diagnostics
- Unequal correspondence: `Sphere_UnequalSeamSamplesRemainOnRadius` passed
- Current frame/reflection and Solidify compatibility tests passed
- Proof captures:
  `Project~/TestResults/M05SphereProofViews/overview.png`,
  `textured.png`, `solid.png`, `wireframe.png`, `uv-stretch.png`, and
  `radius-error.png`
- Repository validation, all JSON parsing, Draft 2020-12 FoldScript schema
  validation, Runtime `UnityEditor` isolation, asmdef inspection, and
  `git diff --check` passed
- No Unity Sphere/UV Sphere/Icosphere, imported or fixed sphere Mesh, automatic
  repair, bevel, subdivision, remesh, mesh cleanup, or M06 behavior was
  implemented
