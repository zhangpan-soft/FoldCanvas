# Goal

Deliver M04 on `feat/m04-stitch-solidify` as a sequence of independently
reviewable geometry gates:

1. reusable topology and ordered-boundary infrastructure
2. deterministic unequal-count seam correspondence and surface subdivision
3. reusable Weld and Bridge processing
4. Solidify outer/inner shell construction
5. true-open-boundary rim generation
6. manifold validation and a live thick-cup proof

M03 was human-approved and merged through PR #1 into `main` at merge commit
`c7b1e61`, retaining reviewed head `96d1688`. Its accepted baseline is Unity
`6000.3.20f1`, 103/103 Edit Mode tests, and the live zero-gap cup proof.

M04 prioritizes visible and topological correctness of Solidify, thickness,
inner walls, rim generation, and reusable seam processing. It does not reopen
M03 for additional defensive parameter validation.

# User-visible proof

The same decorated source canvas produces:

- a cylindrical cup wall whose exterior text remains readable
- a bottom joined through the reusable seam solver
- an outer shell and a correctly wound inner shell
- measurable, configurable wall and base thickness
- one generated top rim joining the outer and inner lip
- no hidden wall across the welded wall-to-bottom seam
- no crack at the wall closure or wall-to-bottom attachment

The proof is viewed in Unity with a one-sided material from exterior, interior,
rim-oblique, and underside angles. Two-sided preview rendering, overlapping
surfaces, enlarged disks, or coincident but topologically separate faces are
not acceptance substitutes.

# Scope

## Reusable ordered boundaries

- Represent every active boundary as an explicit ordered loop or chain over
  current render vertices and logical topology identities.
- Preserve panel ownership, source position, source UV0, and deterministic
  provenance at every existing or inserted boundary sample.
- Treat a repeated closed-loop endpoint as redundant only when its first and
  last samples already share one logical topology identity.
- Compute current-space cumulative arc length in stable boundary order.
- Reject missing, collapsed, non-finite, or ambiguous boundaries with one
  stable root-cause diagnostic and no Mesh.

## Deterministic seam correspondence

- Normalize cumulative arc length to `[0,1]`.
- Apply `ReverseB` before pairing.
- Create a common parameter sequence that retains the sorted union of both
  boundaries' existing normalized breakpoints.
- When `sampleCount > 0`, add a deterministic uniform parameter grid as a
  minimum requested correspondence density. Existing authored breakpoints are
  never discarded, so the final paired count may be greater than
  `sampleCount`. When `sampleCount = 0`, use the natural breakpoint union.
- When a parameter is absent on one side, insert a sample by subdividing its
  boundary edge and the adjacent source-surface triangle.
- Interpolate current position, immutable source position, source UV0, panel
  ownership, and generated provenance deterministically.
- Never create an unattached render vertex merely to satisfy seam counts.
- Produce identical vertices, triangle order, topology IDs, and diagnostics
  for identical input.

The required unequal-count proof pairs 32 and 64 source samples. It must not
depend on a special cup path or equal-count fallback.

This M04 meaning of `sampleCount` must be synchronized in the schema, FoldScript
specification, and field reference before implementation. It avoids destructive
decimation of authored boundary vertices while still providing an explicit
minimum tessellation request.

## Reusable Stitch

- A `SeamDefinition` remains inert until selected by a
  `StitchOperationDefinition`.
- Stitch consumes its seam IDs in declared order.
- Weld reuses the shared correspondence solver, verifies positional tolerance,
  unions logical topology identities, and snaps render copies to the
  deterministic representative.
- Attribute splits remain legal when UV0, provenance, or hard normals differ.
  Manifold checks use `TopologyVertexId`, not raw render index.
- Bridge reuses the same correspondence but emits a deterministic strip when
  direct topology identity is not intended.
- `Hinge` and `KeepOpen` may remain declarative in M04 if documentation and
  diagnostics make that explicit.

## Terminal-Stitch operation rule

Until topology-group deformation propagation exists, Stitch must be the
terminal position-deforming operation for every panel selected by that Stitch.

- For each Stitch, resolve the union of panels referenced by all selected
  seams.
- Any later `RigidTransform`, `Fold`, or `Roll` targeting one of those panels
  returns exactly one
  `FC2010 StitchMustBeTerminalForSelectedPanels` error and no Mesh.
- A later deformation targeting only unrelated panels remains valid.
- This check is based on ordered operation targets and selected seam
  membership; it does not guess from spatial coincidence.
- Solidify is allowed after Stitch because it consumes the complete final
  topology as a shell-construction stage. It must operate on whole logical
  topology groups and cannot move one render copy or one stitched panel side
  independently.
- A future topology-group deformation propagation milestone may relax this
  rule. M04 does not implement that propagation.

## Solidify

Solidify consumes the final stitched topology and creates two consistently
oriented shell surfaces.

For source position `p`, resolved unit normal `n`, and positive thickness `t`:

| Direction | Outer position | Inner position |
| --- | --- | --- |
| `Inward` | `p` | `p - t*n` |
| `Outward` | `p + t*n` | `p` |
| `Centered` | `p + 0.5*t*n` | `p - 0.5*t*n` |

Contracts:

- The source surface winding defines the outer orientation.
- Inner triangles use reversed winding.
- Outer normals face away from the material volume.
- Inner normals face into the cup cavity.
- Smooth topology groups use a deterministic area-weighted normal.
- At a welded hard corner, including the cup wall-to-bottom join, the offset
  position is solved from the incident oriented face-offset planes. This
  produces one shared mitered topology position rather than independently
  offsetting the wall and bottom copies.
- An unbounded, singular, or incompatible corner offset returns a stable
  diagnostic; it is never replaced with an arbitrary averaged-normal guess.
- Every render copy with the same topology identity uses the same solved
  offset position so UV/provenance splits cannot crack.
- Thickness is checked at deterministic source samples against a centralized
  absolute/relative tolerance.
- Solidify rejects non-finite or non-positive thickness.
- Solidify targets must be closed under welded topology. If a selected panel
  shares a topology group with an unselected panel, compilation fails rather
  than separating the stitched component or silently expanding the target.
- Local self-overlap may return a stable warning or error according to whether
  a valid shell can still be proven. M04 does not attempt global repair.

## Open-boundary classification and rim

Open boundaries are classified only after Stitch and logical-topology
normalization:

- a topology edge with incidence one is open
- incidence two is an interior manifold edge
- incidence greater than two is non-manifold and fails
- degenerate topology edges fail

For every true open edge, Solidify connects the corresponding outer and inner
copies exactly once with consistently wound side-wall triangles. Therefore:

- the cup top lip receives one rim strip
- the welded wall closure receives no hidden wall
- the welded wall-to-bottom loop receives no hidden wall
- a closed source surface receives no side wall

After rim construction, the thick cup must have manifold edge incidence two
everywhere unless an explicitly supported open-shell mode says otherwise.

## UV and provenance policy

- Outer and inner shell copies retain source UV0 exactly.
- Inserted seam vertices linearly interpolate source UV0 along their source
  boundary edge.
- M04 rim/side-wall copies retain the UV0 of their corresponding source
  boundary endpoints. The texture therefore stretches through thickness
  deterministically without inventing an un-authored atlas region.
- A future dedicated rim atlas or UV1 policy is out of scope.
- Every generated vertex records stable provenance sufficient to identify its
  source vertex or source edge endpoints plus interpolation parameter.

# Non-goals

- no additional M03 defensive parameter-validation pass
- no change to accepted Roll handedness, radius, or one-turn contracts
- no Fold crease topology split
- no post-Stitch topology-group deformation propagation
- no robust global self-intersection repair
- no bevels or rounded rims
- no variable thickness field
- no cup handle
- no SpiralRoll or LayeredRoll
- no M05 sphere-gores work
- no authoring-window redesign
- no dependency addition

# Architecture changes permitted

M04 may make a narrow data-structure change required by real boundary
subdivision:

- replace assumptions that a panel is represented only by immutable contiguous
  vertex/triangle spans with mutable explicit index collections during compile
- keep the public compiled result read-only
- retain deterministic final ordering
- keep runtime code free of `UnityEditor`
- keep package core independent of render pipelines, third parties, and
  network services

This is not authorization for a broad compiler refactor.

# Files expected to change

- `CURRENT_TASK.md` only when the milestone is eventually completed
- `Docs/Plans/active-plan.md`
- `Codex/M04_STITCH_SOLIDIFY.md`
- `Runtime/Compiler/MeshBuildBuffer.cs`
- `Runtime/Compiler/StitchExecutor.cs`
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- new focused runtime helpers for boundary correspondence, topology
  subdivision, or Solidify when separation improves testability
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- `Runtime/Data/SeamDefinition.cs` for the explicit Bridge mode
- `Runtime/Data/FoldOperationDefinition.cs` only if the accepted Solidify data
  contract requires a narrow change
- `Tests/Editor/StitchCompilerTests.cs`
- new focused Solidify/topology Edit Mode tests
- `Editor/FoldCanvasSampleCreator.cs`
- M04 sample assets under `Samples~`
- M04-relevant files under `Documentation~` and `Schema`, including the
  precise `sampleCount` meaning
- `CHANGELOG.md`
- `package.json` only for the eventual package-version iteration

# Geometry invariants

1. Identical source and options produce byte-for-byte stable logical geometry
   ordering and identical diagnostic order.
2. UV0 on every retained source-derived outer/inner vertex equals its authored
   source UV0.
3. A resampled boundary vertex is part of its adjacent surface triangulation.
4. Welded render copies may remain distinct but share one topology identity
   and one current position.
5. No output triangle contains repeated logical topology vertices.
6. No output triangle has zero area within centralized tolerance.
7. No undirected logical topology edge has incidence greater than two.
8. A true open boundary edge is rimmed exactly once during Solidify.
9. Welded internal seams are never mistaken for open boundaries.
10. Inner-shell winding is the exact reverse of its corresponding outer
    triangle.
11. Measured shell separation matches the requested direction and thickness.
12. Generated normals are validated from the emitted Unity Mesh, not inferred
    only from preview material settings.
13. Invalid geometry returns diagnostics and no Mesh; no stage silently emits
    an approximation.

# Implementation and acceptance gates

Each gate is implemented, tested, reviewed in its own focused diff, and then
accepted before the next gate begins.

## Gate M04.1: ordering guard and mutable topology foundation

Implement:

- reserve `FC2010 StitchMustBeTerminalForSelectedPanels`
- scan later deformation targets for panels selected by Stitch
- introduce the minimum mutable index/boundary representation required for
  boundary-edge subdivision
- preserve the accepted equal-count M03 Weld behavior

Required tests:

- `PostStitchRigidTransform_OnSelectedPanel_ReturnsTerminalOperationDiagnostic`
- `PostStitchFold_OnSelectedPanel_ReturnsTerminalOperationDiagnostic`
- `PostStitchRoll_OnSelectedPanel_ReturnsTerminalOperationDiagnostic`
- `PostStitchDeformation_OnUnselectedPanel_RemainsAllowed`
- all existing 103 tests remain enabled

Acceptance:

- one stable ordering diagnostic and no Mesh on violation
- no change to accepted M03 cup topology or visible proof
- repeated compile has identical diagnostics

## Gate M04.2: normalized correspondence and real surface subdivision

Implement:

- ordered current-space arc-length parameterization
- deterministic normalized parameter union
- explicit minimum-density `sampleCount` parameter grid
- boundary-edge sample insertion
- adjacent triangle subdivision with preserved winding
- source-position, UV0, ownership, and provenance interpolation

Required tests:

- `Stitch_ThirtyTwoToSixtyFour_ResamplesDeterministically`
- `Stitch_SampleCount_AddsMinimumDensityWithoutDroppingBreakpoints`
- `Stitch_ResampledVertex_IsPartOfAdjacentSurface`
- `Stitch_Resampling_PreservesInterpolatedSourceUv`
- `Stitch_Resampling_PreservesTriangleWinding`
- `Stitch_RepeatedCompile_ProducesIdenticalTopology`
- `Stitch_ZeroLengthBoundary_ReturnsSingleStableDiagnostic`

Acceptance:

- 32↔64 correspondence is not an equal-count special case
- no unattached seam vertices
- no zero-area triangles or source-surface holes

## Gate M04.3: reusable Weld and Bridge

Implement:

- migrate Weld to the shared correspondence result
- preserve attribute seams through shared topology IDs
- implement deterministic Bridge strip generation and winding
- maintain declared seam order and reversal

Required tests:

- `Weld_ThirtyTwoToSixtyFour_ProducesSharedTopology`
- `Weld_ReverseB_ChangesCorrespondencePredictably`
- `Weld_LeavesNoCrackAboveEpsilon`
- `Weld_PreservesDistinctUvRenderCopies`
- `Bridge_ThirtyTwoToSixtyFour_ProducesDeterministicStrip`
- `Bridge_HasConsistentWindingAndNoZeroAreaTriangles`
- `DeclaredSeam_WithoutStitch_RemainsInert`

Acceptance:

- the same solver supports cup seams and a non-cup fixture
- raw render-index splits do not appear as topological cracks
- unsupported modes still return one stable root cause

## Gate M04.4: Solidify outer and inner shells

Implement:

- consume final stitched topology
- resolve deterministic smooth normals and hard-corner offset-plane miters
- generate inward, outward, and centered shell copies
- reverse inner winding
- retain source UV0 and stable provenance

Required tests:

- `Solidify_Inward_PreservesOuterAndOffsetsInner`
- `Solidify_Outward_PreservesInnerAndOffsetsOuter`
- `Solidify_Centered_SplitsRequestedThickness`
- `Solidify_InnerTrianglesReverseOuterWinding`
- `Solidify_SharedTopologyCopies_DoNotCrack`
- `Solidify_WallBottomCorner_MeetsAtSharedMiter`
- `Solidify_PartialStitchedComponent_ReturnsStableDiagnostic`
- `Solidify_InvalidThickness_ReturnsStableDiagnostic`
- `StitchThenSolidify_UsesFinalTopology`

Acceptance:

- requested thickness is met at deterministic samples
- outer and inner Unity Mesh normals point in documented directions
- the shell cannot separate at UV/provenance attribute splits

## Gate M04.5: true-open-boundary rim generation

Implement:

- classify topology-edge incidence after Stitch
- order true open edges into deterministic boundary loops/chains
- generate one rim/side wall per true open edge
- apply deterministic rim winding, UV0, and provenance

Required tests:

- `Solidify_Cup_GeneratesOnlyTopRim`
- `Solidify_WeldedBottomSeam_HasNoInternalWall`
- `Solidify_WeldedWallClosure_HasNoInternalWall`
- `Solidify_RimWindingFacesOutward`
- `Solidify_RimUvPolicy_IsDeterministic`
- `Solidify_ClosedSurface_GeneratesNoRim`
- `Solidify_NonManifoldInput_ReturnsStableDiagnostic`

Acceptance:

- no internal walls across either cup Weld
- top rim is complete from every oblique view
- final logical topology is manifold

## Gate M04.6: sample, live proof, and release evidence

Implement:

- update the decorated cup sample to use reusable Stitch plus Solidify
- create or update one package-owned EditorOnly M04 proof hierarchy without
  modifying user cameras
- expose requested and measured thickness/topology evidence
- update documentation, schema if needed, changelog, and package version

Required verification:

- full Edit Mode suite in Unity `6000.3.20f1`
- exported `TestResults.xml`
- repository static validation
- changed JSON parse
- assembly-definition inspection
- runtime `UnityEditor` exclusion
- `git diff --check`
- live exterior/interior/rim/underside review with one-sided material
- measured seam gap, thickness error, open-edge count, non-manifold count,
  zero-area count, render-vertex count, topology-vertex count, and triangle
  count

Acceptance:

- source board and finished thick cup are visible together
- exterior art reads correctly
- inner wall, bottom interior, rim, and underside are all visible and correctly
  wound
- final cup has no unintended open or non-manifold topology edge

# Planned diagnostics

Final codes are added only with their implementation and tests. `FC2010` is
reserved by this plan; subsequent M04 allocations must be checked against the
runtime registry before use.

| Code | Name | Condition |
| --- | --- | --- |
| `FC2010` | `StitchMustBeTerminalForSelectedPanels` | later position deformation targets a panel selected by Stitch |
| TBD | `ZeroLengthStitchBoundary` | selected boundary has no usable current-space length |
| TBD | `SeamLengthMismatch` | boundary lengths exceed the documented compatibility threshold |
| TBD | `StitchOrientationConflict` | declared correspondence cannot preserve required orientation |
| TBD | `StitchWeldCollapse` | correspondence would collapse a triangle or edge |
| TBD | `InvalidSolidifyThickness` | thickness is non-finite or not positive |
| TBD | `IncompleteSolidifyTopologySelection` | selected panels do not include a complete welded topology component |
| TBD | `UnsupportedSolidifyCorner` | incident offset planes have no stable bounded corner solution |
| TBD | `SolidifySelfOverlap` | requested local offset produces detected overlap |
| existing `FC5003` or focused successor | `NonManifoldTopology` | topology edge incidence exceeds two or shell construction is non-manifold |

Diagnostics include ordered structural context such as operation ID, seam ID,
panel ID, boundary ID, sample parameter, requested thickness, and measured
error when applicable. Do not encode required repair data only inside the
message string.

# Risks and rollback

## Boundary subdivision corrupts adjacent surfaces

Risk: inserted samples are appended to a seam but not incorporated into the
panel, or triangle replacement changes winding.

Control: Gate M04.2 requires adjacency, UV, winding, and repeated-compile tests
before reusable Stitch consumes the result.

Rollback: keep the accepted M03 equal-count solver intact behind the previous
path until M04.2 passes; do not ship a partial unequal-count approximation.

## Panel contiguous-span assumptions leak

Risk: mutable insertion invalidates later panel range calculations.

Control: identify every range consumer before changing `MeshBuildBuffer`, move
only required consumers to explicit lists, and lock deterministic order with
tests.

Rollback: isolate the new representation behind focused helpers rather than
rewriting unrelated compiler stages.

## Attribute seams are mistaken for cracks

Risk: raw render-index validation reports false open edges or offset copies
move apart.

Control: topology incidence and Solidify offsets operate on
`TopologyVertexId`; outer/inner render copies retain independent UV data but
share position decisions.

## Rim generation closes welded seams internally

Risk: render duplicates at the wall closure or bottom loop look open.

Control: classify edges after topology union and test that only the cup top lip
is open before rim generation.

## Thick offsets self-intersect

Risk: large thickness or concave regions produce overlap.

Control: detect deterministically where practical, report a stable diagnostic,
and never claim robust global repair.

# Decisions made

- M03 remains accepted; no extra defensive Roll/Fold parameter pass is part of
  M04.
- `FC2010 StitchMustBeTerminalForSelectedPanels` is the planned stable guard
  until topology-group deformation propagation exists.
- Solidify may follow Stitch because it consumes whole final topology; it is
  not a later isolated panel deformation.
- Unequal-count Weld requires actual boundary-edge and adjacent-triangle
  subdivision, not free-floating interpolated samples.
- M04 `sampleCount` is a minimum correspondence density; existing normalized
  boundary breakpoints remain mandatory in the final common schedule.
- Logical manifold checks use topology identities; render splits remain valid
  for UV, provenance, and hard normals.
- Inward keeps the source as outer, Outward keeps it as inner, and Centered
  splits thickness equally.
- Outer/inner retain source UV0. Rim UV0 derives deterministically from source
  boundary endpoints for the M04 MVP.
- Welded hard corners use a shared incident-plane miter solution. Solidify
  rejects partial stitched-component selection instead of cracking or silently
  changing target scope.
- True open topology edges alone generate rim/side-wall faces.
- Implementation proceeds gate by gate; later gates do not begin until the
  preceding gate's tests and diff are reviewed.

# Progress log

- 2026-07-27: M03 human audit approved.
- 2026-07-27: PR #1 merged four commits into `main` with merge commit
  `c7b1e61`; reviewed head `96d1688` remains an ancestor of `main`.
- 2026-07-27: Local `main` fast-forwarded to the merged result.
- 2026-07-27: Created `feat/m04-stitch-solidify`.
- 2026-07-27: Advanced `CURRENT_TASK.md` to M04 and recorded the
  terminal-Stitch ordering constraint.
- 2026-07-27: Replaced the completed M03 plan with this gated M04 plan. No M04
  geometry implementation has started.
- 2026-07-27: Repository validation and `git diff --check` passed for the
  planning transition.

# Final verification for planning change

Before committing this planning transition:

- confirm `HEAD` contains `96d1688`
- confirm `CURRENT_TASK.md` points to M04
- confirm the active plan and compiler pipeline state the terminal-Stitch rule
- run repository static validation
- run `git diff --check`
- leave host-project M03 proof/test artifacts untracked and untouched

Unity tests are not rerun for a documentation-only planning commit. The
starting implementation baseline remains the accepted 103/103 M03 suite.
