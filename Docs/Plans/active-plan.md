# Goal

Deliver M05 on `codex/m05-spherical-wrap` and prove that explicit 2D panels,
curvature rules, the existing seam graph, Weld, and the deterministic compiler
produce a genuine closed sphere. The generated Mesh is derived evidence, never
the editable source.

PR #4 hardening additionally requires component-scoped closed-sphere
validation at the zero-thickness surface stage, before any Solidify that
touches that component; one cumulative geometry budget across panel
tessellation, Stitch subdivision/bridges, and Solidify; defensive native asset
validation; scale-aware pole classification; and a real Unity Edit Mode CI
job. Unrelated Stitch or Solidify operations must not change whether a
spherical component is validated.

The current review gate additionally requires every Stitch-selected seam
reference to fail through stable diagnostics before component planning, and
requires a sphere report to be captured only after the last Stitch whose seam
touches that spherical component. Component-forming seams and
component-touching Stitches are separate concepts: only spherical-to-spherical
seams form components, while a seam with either endpoint in a component delays
that component's validation.

The current ordering gate requires every enabled `SphericalWrap` targeting a
Stitch-selected seam endpoint to occur strictly before that Stitch. Invalid
ordering must fail during source preflight, before panel tessellation,
`StitchExecutor`, component planning, or sphere reporting. The existing
component-forming and last-touching-Stitch architecture remains unchanged.

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
- Build deterministic spherical components only from enabled SphericalWrap
  panels connected by Stitch-selected seams whose two endpoints are spherical.
- Validate each component after its final touching Stitch and before its first
  relevant Solidify. A touching Stitch selects any seam whose A or B panel is
  a component member, including spherical-to-ordinary Bridge or Weld seams.
  Preserve that pre-Solidify report even when the compiled result later
  contains a thick shell.
- Validate every selected seam ID, endpoint panel ID, endpoint boundary ID,
  referenced panel, and referenced built-in panel boundary before component
  planning. `SphereValidationPlan.Build` still guards all string dictionary
  keys independently so malformed native data cannot throw.
- Build an enabled panel-to-SphericalWrap operation-index map during source
  preflight. Every selected seam endpoint that has a wrap must satisfy
  `sphericalWrapOperationIndex < stitchOperationIndex`; otherwise return
  `FC2010 StitchMustBeTerminalForSelectedPanels`, no Mesh, and no sphere
  report. `SphereValidationPlan.Build` independently refuses to schedule a
  component whose last touching Stitch is not later than every member wrap.
- Treat the sphere validator as a topology, manifoldness, radius, pole, and
  winding proof. It is not a global triangle-triangle self-intersection proof.
- Enforce `MaxGeneratedVertices` and `MaxGeneratedTriangles` cumulatively in
  the build buffer. Geometry-producing operations preflight their exact or
  conservative additions before mutating geometry.
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

- pole classification requires both the centralized angular tolerance and
  `radius * angularDeviationRadians <= CompileSettings.WeldEpsilon`
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
11. Harden PR #4 with component-scoped pre-Solidify reports, cumulative
    GeometryBudget enforcement, native `sampleCount`/reference validation,
    scale-aware poles, three-pass golden determinism, and Unity CI artifacts.
12. Preflight every enabled SphericalWrap/Stitch endpoint dependency before
    tessellation and retain an independent component-plan ordering guard.

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
- open sphere followed by Solidify is rejected before shell generation
- unrelated Stitch and Solidify operations do not trigger or suppress sphere
  validation
- two independent spherical components emit two deterministic reports
- panel, Stitch, Bridge, and Solidify additions share one cumulative budget
- budget failures leave counts, topology, panel records, and boundaries
  unchanged
- null/empty/whitespace native references return diagnostics, not exceptions
- default, null, empty, whitespace, missing-panel, and missing-boundary
  Stitch-selected seam references return stable diagnostics before planning
- a later cross-type Bridge or Weld touching a spherical component cannot
  leave an earlier closed report; report operation ID/index identify the last
  touching Stitch
- post-closure boundary subdivision is either rejected or represented by a
  fresh failed/closed report from the modified topology
- Solidify after a cross-type Stitch consumes only fresh sphere evidence
- `ComponentFormingStitch_BeforeWrap_ReturnsOrderingDiagnostic`
- `StitchBetweenComponentWraps_ReturnsOrderingDiagnostic`
- `CrossTypeStitch_BeforeWrap_DoesNotEmitSphereReport`
- `InvalidWrapStitchOrder_DoesNotReturnSphereValidationFailed`
- `InvalidWrapStitchOrder_IsDeterministic`
- `ValidWrapThenStitch_StillProducesFreshPreSolidifyReport`
- `ValidWrapThenStitchThenSolidify_StillPasses`
- a radius `1e9` near-pole ring is not collapsed by fixed angular tolerance
- the golden asset compiles three times with stable geometry and report hashes
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
- Operation-level budget preflight must agree with build-buffer hard limits.
  Keep the hard guard authoritative and test disagreement as an internal
  failure rather than permitting over-allocation.
- Component identity and report ordering must be based on source panel order,
  operation order, and ordinal IDs; dictionary iteration cannot order reports.
- Full-list component planning can see a future wrap while execution is still
  at an earlier Stitch. Reject that dependency in source preflight and make
  the plan omit unschedulable components so it cannot emit an early report
  even if the upper preflight is bypassed.

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
- 2026-07-30: PR #4 hardening baseline re-ran repository validation and the
  complete Unity suite: 174/174 passed before changes.
- 2026-07-30: Source audit confirmed the asset-level
  `hasStitch && !hasSolidify` sphere gate, panel-only safety limits, unguarded
  null dictionary keys, fixed-angle pole classification, and Python-only CI.
- 2026-07-30: Replaced the asset-level gate with deterministic relevant-seam
  components and preserved staged pre-Solidify reports. Added full-compile
  GeometryBudget enforcement, Stitch/Solidify rollback transactions, native
  input guards, and scale-aware pole classification.
- 2026-07-30: Batched Stitch sample insertion with one edge-adjacency index;
  the `8192`-sample regression completed in Unity in about `0.42 s`.
- 2026-07-30: Added pinned GameCI Unity Edit Mode CI plus NUnit/Editor-log
  artifacts, and expanded repository validation for workflow, version, and
  Schema/native limit consistency.
- 2026-07-30: Unity `6000.3.20f1` passed the complete hardened suite:
  208/208 passed, with zero failed, skipped, or inconclusive tests.
- 2026-07-31: Began the second PR #4 blocker pass. Scope is limited to
  Stitch-selected seam reference diagnostics, defensive component planning,
  last-touching-Stitch sphere evidence, and a genuinely executed Unity CI run
  with uploaded XML and Editor log.
- 2026-07-31: Reproduced the blocker gate at 5/15 passing: malformed selected
  seam panel IDs threw from component planning, and cross-type Stitch reports
  still identified the earlier internal sphere Stitch.
- 2026-07-31: Added selected-seam source validation plus independent null-key
  guards, split component-forming seams from component-touching Stitches, and
  passed the focused gate at 15/15.
- 2026-07-31: Unity `6000.3.20f1` passed the complete local suite at 223/223,
  with zero failed, skipped, or inconclusive tests.
- 2026-07-31: Began the Wrap-before-Stitch ordering gate from exact PR #4 head
  `4f52925c51a32806dcdaad6ab844ca6aced9963a`. Scope is limited to source
  operation-order preflight, independent plan scheduling defense, seven
  regressions, and actual licensed Unity CI evidence.
- 2026-07-31: Reproduced the focused ordering gate at 3/7 passing. A
  component-forming Stitch before wraps reached `FC2006`, and direct plan use
  scheduled validation at the early Stitch index.
- 2026-07-31: Added the strict source-order preflight plus maximum-member-wrap
  plan guard. The focused ordering gate now passes 7/7.
- 2026-07-31: Unity `6000.3.20f1` passed the complete ordering-gate suite at
  230/230, with zero failed, skipped, or inconclusive tests. Repository
  validation, workflow YAML, JSON parsing, and `git diff --check` passed.

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
- `WeldEpsilon` is the existing compile-time positional tolerance used for
  scale-aware pole normalization; no second competing positional setting is
  introduced in M05.
- `SphereReport` remains the compatibility view while `SphereReports` preserves
  every component-scoped validation report and its stage.
- M05 CI uses the tracked `Project~` host so Runtime, Editor, package Tests, and
  the exact local-package dependency compile together.
- Spherical-to-spherical selected seams form components. Every selected Stitch
  seam with at least one endpoint in a formed component determines that
  component's final validation operation, even when the other endpoint is an
  ordinary panel.
- Invalid selected seam source references are rejected before tessellation,
  but `SphereValidationPlan` keeps its own null/whitespace guards as a
  defense-in-depth boundary.
- Reuse `FC2010 StitchMustBeTerminalForSelectedPanels` for a Stitch that
  selects a panel before its enabled SphericalWrap. The strict contract is
  `wrapIndex < stitchIndex`; diagnostic order follows operation order, seam
  selection order, then endpoint A before B.
- `SphereValidationPlan` computes each formed component's maximum member-wrap
  index and does not create a validation schedule unless the last touching
  Stitch is strictly later. This guard does not replace the source diagnostic.

# Current verification

- Package version: `0.1.0-preview.12`
- Unity Editor: `6000.3.20f1 (c9ba695d4f07)`
- Edit Mode: 230/230 passed, zero failed/skipped/inconclusive
- Test XML:
  `Project~/TestResults/M05WrapStitchOrderingFull.xml`
- Editor log:
  `Project~/TestResults/M05WrapStitchOrderingFull-Editor.log`
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
- Determinism: `Sphere_RegenerationIsDeterministic` and
  `GoldenSphere_ThreeCompilesHaveStableCountsReportsAndHash` passed for vertex
  order, triangle order, topology IDs, staged reports, and deterministic hash
- Unequal correspondence: `Sphere_UnequalSeamSamplesRemainOnRadius` passed
- Current frame/reflection and Solidify compatibility tests passed
- Proof captures:
  `Project~/TestResults/M05SphereProofViews/overview.png`,
  `textured.png`, `solid.png`, `wireframe.png`, `uv-stretch.png`, and
  `radius-error.png`
- Repository validation, all tracked JSON parsing, workflow YAML parsing,
  Draft 2020-12 FoldScript schema validation, Runtime `UnityEditor` isolation,
  asmdef inspection, prohibited-sphere-generator scan, and
  `git diff --check` passed
- GitHub Actions Unity status is recorded after the hardening commit is pushed;
  the workflow requires the documented Unity license secrets
- No Unity Sphere/UV Sphere/Icosphere, imported or fixed sphere Mesh, automatic
  repair, bevel, subdivision, remesh, mesh cleanup, or M06 behavior was
  implemented
