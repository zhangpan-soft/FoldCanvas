# Goal

Complete the M03 pre-merge architecture audit and the deterministic `Roll`
operator on branch `feat/m03-roll-cup`. After reviewing the real Unity proof,
the user authorized a commit, push, and audit PR once the reported orbit-view
visibility defect is fixed. The PR must remain unapproved and unmerged, and
`CURRENT_TASK.md` must remain on M03 for the external audit.

This milestone also closes one M02 silent-approximation hole: a Fold crease
that requires triangle splitting must fail explicitly. M03 does not implement
that topology split.

# Delivery constraints

- Do not implement Stitch geometry, Solidify, welding, seam resampling,
  thickness, inner walls, rims, handles, or any M04 behavior.
- Do not silently approximate a Fold or Roll request.
- Commit and push only after the visibility regression, complete Edit Mode
  suite, and multi-angle Unity preview pass.
- Create an audit PR into `main`, but do not approve or merge it.
- Do not advance `CURRENT_TASK.md`.
- Keep all work on `feat/m03-roll-cup`; `main` remains the accepted M02 state.

# User-visible M03 proof

- One 2D appearance canvas displays a normal, readable `GPT 5.6` wall region,
  an emblem, and a `CODEX` bottom region.
- A separate derived cup mesh rolls the wall through a positive 360-degree
  sweep and rigidly places the disk on the bottom plane.
- The 2D source board and 3D result are visible together in a clean Unity
  scene so UV handedness cannot be hidden by a mirrored source image.
- The wall's minimum/maximum boundaries and the wall/bottom perimeter align
  spatially but remain topologically separate.
- The interactive proof uses an opaque, two-sided Unlit material so M03's
  intentionally zero-thickness wall and bottom remain visible while orbiting.
- The two-sided visualization is not a winding oracle: acceptance still checks
  generated mesh normals, triangle order, and a canonical outside view. It
  does not add an inner wall or any M04 topology.

# A. M02 Fold topology-split guard

## Contract

- A Fold crease is executable only when both authored endpoints are existing
  source vertices and the complete crease is covered, without gaps, by a
  deterministic chain of existing source-mesh edges.
- Triangle edges are collected from the target panel's retained triangle span,
  projected onto the directed crease, sorted by interval start/end, and merged
  in deterministic source order.
- If either endpoint needs insertion, or any positive-length interval crosses
  a triangle interior instead of an existing edge, compilation stops with
  `FC3011 FoldCreaseRequiresTopologySplit`.
- No vertex moves and no `Mesh` or compiled data is returned for that source.
- Existing grid-edge, diagonal-edge, and panel-boundary folds continue to use
  the accepted M02 executor.

## Required tests

- `Fold_OffGridCrease_ReturnsRequiresTopologySplitDiagnostic`
- All accepted M02 grid-edge and boundary tests remain enabled and green.
- Roadmap records deterministic crease topology splitting as an explicit
  future task rather than implied M03 behavior.

# B. Roll current-frame semantics

## Compatible current embedding

- The target must be a rectangle whose complete current vertex set is one
  finite, non-degenerate affine plane embedding of its authored source grid.
- M03's compatible affine subset is a congruent planar embedding. Translation,
  rotation, and orientation-reversing planar isometries may be accepted.
- In-plane metric-changing scale, shear, collapsed axes, prior non-planar
  Fold, non-planarity, and piecewise affine embeddings return
  `FC3021 UnsupportedRollEmbedding`.
- This deliberately preserves all required pre-Roll rigid placement while
  defining compatibility from the current geometry rather than operation
  history. Fold-after-Roll semantics remain deferred.

## Resolved frame

- `CurrentOrigin`: current position of the source rectangle center.
- `CurrentU`: unit current direction of increasing source U.
- `CurrentV`: unit current direction of increasing source V.
- `CurrentNormal`: normalized
  `Cross(CurrentU, CurrentV)`.
- Every current vertex must equal
  `CurrentOrigin + sourceX*CurrentU + sourceY*CurrentV` within centralized
  absolute/relative tolerances.
- Frame resolution is performed from ordered rectangle corners and validated
  against every target vertex. No local triangle frame is guessed.

## Mapping

Let `t` be normalized position along the selected direction and:

```text
theta = radians(startAngleDegrees + t * angleDegrees)
```

For U:

```text
position =
    CurrentOrigin
    + sourceY * CurrentV
    - R * cos(theta) * CurrentU
    + R * sin(theta) * CurrentNormal
```

For V:

```text
position =
    CurrentOrigin
    + sourceX * CurrentU
    - R * cos(theta) * CurrentV
    + R * sin(theta) * CurrentNormal
```

The non-roll source coordinate remains linear. UV0, source position, panel
ownership, provenance, and boundary vertex identities remain unchanged.

## Required tests

- `Roll_AfterRigidTransform_PreservesCurrentFrame`
- `Roll_AfterNonPlanarFold_ReturnsStableDiagnostic`
- `Roll_AfterUnitReflection_UsesReflectedCurrentFrame`
- `Roll_AfterInPlaneNonUniformScale_ReturnsUnsupportedEmbedding`
- `Roll_AfterCollapsedAxis_ReturnsUnsupportedEmbedding`

# C. Roll handedness, winding, and normals

- At start angle zero, the minimum roll boundary begins on the negative
  `CurrentU` radial axis for U or negative `CurrentV` radial axis for V.
- A positive angle advances from that radial axis toward
  `+CurrentNormal`.
- A positive sweep preserves source triangle order. Its generated Unity mesh
  normals face radially outward and its artwork reads in authored
  left-to-right order when viewed from outside.
- A negative sweep also preserves source triangle order. Its angular/artwork
  circulation is the predictable opposite of a positive sweep, so its
  generated geometric normals point radially inward.
- Connectivity, vertex IDs, boundary ordering, and UV values never change.
- Acceptance checks `result.Mesh.normals`, not only a hand-computed triangle
  cross product, and uses a one-sided preview material.

## Required tests

- `PositiveFullRoll_HasOutwardWinding`
- `NegativeFullRoll_ReversesOrientationPredictably`
- `RollU_And_RollV_HaveDocumentedHandedness`

# D. Seam declaration semantics

- `SeamDefinition` is inert declarative source data.
- Merely populating `asset.Seams` does not execute topology and does not add an
  error.
- `StitchOperationDefinition` remains unimplemented and returns exactly one
  `FC3001 UnsupportedOperation` root-cause diagnostic before M04.
- `FitTargetBoundary` returns exactly one dedicated
  `FC3016 UnsupportedFitTargetBoundary` diagnostic. A declared target seam
  does not add `UnsupportedSeam`.

## Required tests

- `DeclaredSeam_WithoutStitch_DoesNotFail`
- `FitTargetBoundary_ReturnsSingleStableDiagnostic`
- `UnsupportedStitch_ReturnsStableDiagnostic`

# E. Structured diagnostics

- `FoldCanvasDiagnostic` carries an ordered, copied, read-only list of
  `FoldCanvasDiagnosticValue` entries.
- Each value has a stable English key, finite `double` value, and optional
  unit. No dictionary enumeration contributes to order.
- Diagnostics also expose an ordered, copied, read-only
  `RepairSuggestions` string list for future repair guidance.
- Explicit-radius Roll always emits one `FC3018 RollStretchReport` diagnostic:
  `Info` inside guidance and `Warning` outside `[0.5, 2.0]`.
- Its values appear in this exact order:
  `sourceSpan`, `arcLength`, `stretchRatio`.
- Human-readable text may summarize the ratio, but structured values are the
  contract.
- Repeated compilation must produce identical diagnostic count, ordering,
  codes, severities, contexts, structured values, and suggestion ordering.

# F. Roll sampling validation

- A closed full-turn request is a nonzero sweep whose magnitude is 360 degrees
  within the centralized full-turn tolerance.
- A closed full turn requires at least three source segments in the selected
  roll direction. One segment collapses the minimum and maximum samples. Two
  segments sample 0, 180, and 360 degrees and produce two coincident planar
  panels even though their individual triangles have nonzero area. Both cases
  fail directly with `FC3022 InsufficientRollTessellation`.
- Three segments for a 360-degree sweep must compile into three distinct
  angular panels without zero-area or overlapping triangular surfaces.
- Partial rolls remain open.
- Full-turn minimum/maximum boundary positions coincide within the seam-proof
  tolerance, while their vertex indices remain distinct.

## Required tests

- `FullRoll_WithOneSegment_ReturnsInsufficientTessellation`
- `FullRoll_WithTwoSegments_ReturnsInsufficientTessellation`
- `FullRollV_WithTwoSegments_ReturnsInsufficientTessellation`
- `FullRoll_WithThreeSegments_DoesNotGenerateZeroAreaTriangles`
- `FullRoll_WithThreeSegments_ProducesNonOverlappingTriangularSurface`
- `PartialRoll_RemainsOpen`
- `FullRoll_MinAndMaxBoundariesCoincideButRemainTopologicallySeparate`

# G. Circular Roll turn limit

- M03 Circular Roll accepts signed sweeps only within
  `[-360 degrees, +360 degrees]`, using the centralized full-turn tolerance at
  the boundary.
- A larger magnitude would map multiple source intervals onto the same
  zero-thickness cylindrical surface. M03 has no pitch, layer spacing,
  thickness accumulation, or collision contract with which to make that
  geometry meaningful.
- Requests above the supported magnitude fail with exactly one
  `FC3023 UnsupportedMultiTurnRoll` diagnostic. The compiler does not clamp,
  truncate, or silently generate overlapping layers.
- `SpiralRoll` and `LayeredRoll` are future independent operations with their
  own geometry and validation contracts.

## Required tests

- `Roll_AbovePositiveFullTurn_ReturnsStableDiagnostic`
- `Roll_BelowNegativeFullTurn_ReturnsStableDiagnostic`
- `Roll_ExactlyPositiveFullTurn_Succeeds`
- `Roll_ExactlyNegativeFullTurn_Succeeds`

# H. Owned M03 proof

- `Create M03 Cup Proof` owns one `FoldCanvas M03 Preview Root`, tagged
  `EditorOnly`, in the active scene.
- The root owns exactly one `Cup Proof`, `Source Canvas`, and `Preview Camera`.
  It is found with an inactive-aware editor lookup and never inferred from
  `Camera.main` or a global object name alone.
- Re-running the command reuses the owned objects, including inactive ones.
  Existing objects and components are recorded with Undo before mutation.
- The owned camera is untagged and the command never reads or changes a user's
  MainCamera.

## Required tests

- `CreateM03CupProof_TwiceKeepsOneOwnedPreviewCamera`
- `CreateM03CupProof_TwiceKeepsStableObjectCount`
- `CreateM03CupProof_DoesNotModifyExistingMainCamera`
- `CreateM03CupProof_ReusesInactiveOwnedObjects`

# I. Cup wall/bottom fit proof

- The M03 wall bottom boundary and disk perimeter must coincide at all 64
  corresponding samples in the same bottom plane, within the centralized
  seam-proof tolerance.
- They deliberately remain separate vertex loops; enlarging the disk,
  overlapping surfaces, welding, or adding thickness is not an acceptable M03
  repair.
- The owned preview resets to a canonical transform and camera framing so a
  stale user rotation cannot be mistaken for generated geometry drift.
- Acceptance includes a sample-driven boundary-pair test and a real Unity
  oblique/bottom-view inspection.

# Diagnostic allocation

| Code | Name | Severity/condition |
| --- | --- | --- |
| `FC3011` | `FoldCreaseRequiresTopologySplit` | error: crease is not an existing continuous edge chain |
| `FC3012` | `RollTargetMissing` | error: target panel does not exist |
| `FC3013` | `NonFiniteRollParameter` | error: angle/start/radius or derived radius is invalid |
| `FC3014` | `NearZeroRollAngle` | error: angle magnitude is at/below minimum |
| `FC3015` | `InvalidExplicitRollRadius` | error: explicit radius is not positive |
| `FC3016` | `UnsupportedFitTargetBoundary` | error: depends on future seam solving |
| `FC3017` | `UnsupportedRollPanelShape` | error: target is not a rectangle |
| `FC3018` | `RollStretchReport` | info/warning: structured explicit-radius deformation report |
| `FC3019` | `InvalidRollDirection` | error: unknown direction enum |
| `FC3020` | `InvalidRollRadiusMode` | error: unknown radius-mode enum |
| `FC3021` | `UnsupportedRollEmbedding` | error: current target has no compatible single frame |
| `FC3022` | `InsufficientRollTessellation` | error: closed full turn has fewer than three roll segments |
| `FC3023` | `UnsupportedMultiTurnRoll` | error: circular sweep exceeds one signed turn |

# Radius and stretch

- `PreserveArcLength`:
  `R = sourceSpan / abs(radians(angleDegrees))`.
- `Explicit`: `R = explicitRadius`.
- Explicit `arcLength = R * abs(radians(angleDegrees))`.
- Explicit `stretchRatio = arcLength / sourceSpan`.
- No mode clamps, silently changes radius, or substitutes another mode.

# Proof dimensions

- Wall source: `2*pi*0.05 m` by `0.12 m`, `64 x 12` segments.
- Wall Roll: U, positive 360 degrees, preserve arc length, start angle zero.
- Bottom source: `0.10 m` disk, 64 radial segments, 8 rings.
- Bottom placement: rotate positive 90 degrees about X and translate to
  `y = -0.06 m`.
- Expected cup mesh: 1,358 vertices and 2,496 triangles.

# Files expected to change

- `Docs/Plans/active-plan.md`
- `Runtime/Compiler/FoldLineExecutor.cs`
- `Runtime/Compiler/RollExecutor.cs`
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- `Runtime/Compiler/FoldCanvasGeometryTolerances.cs`
- `Runtime/Compiler/MeshBuildBuffer.cs`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- `Editor/FoldCanvasSampleCreator.cs`
- `Editor/Shaders/FoldCanvasTwoSidedUnlit.shader`
- `Editor/FoldCanvasWindow.cs`
- `Tests/Editor/FoldLineCompilerTests.cs`
- `Tests/Editor/RollCompilerTests.cs`
- `Tests/Editor/SourceValidationTests.cs`
- `Tests/Editor/BootstrapEditorWorkflowTests.cs`
- M02/M03-relevant files under `Documentation~`, `Schema`, and `Samples~`
- `CHANGELOG.md`
- `package.json`

# Execution order

1. Record this audit contract before further implementation.
2. Add the M02 edge-chain guard and tests.
3. Implement/verify the current Roll frame and the non-planar rejection.
4. Lock positive/negative winding using generated Unity normals.
5. Make Seam declarations inert and enforce single-root diagnostics.
6. Add structured diagnostic values and explicit-radius reports.
7. Add closed-turn sampling validation and reject unsupported multi-turn
   circular rolls.
8. Lock the congruent-planar embedding contract with reflection, scale, and
   collapsed-axis tests.
9. Replace ambient-camera preview mutation with one inactive-aware,
   package-owned EditorOnly preview hierarchy.
10. Update field reference, compiler pipeline, diagnostics, and roadmap.
11. Run JSON/asmdef/repository/diff checks and the complete Edit Mode suite,
    then export `TestResults.xml`.
12. Build a clean Unity proof scene with the 2D source board and 3D cup, then
    verify the wall and bottom remain visible from multiple orbit directions.
13. Update the newest-first changelog entry.
14. Commit and push the branch, update audit PR #1 into `main`, and stop before
    approval, merge, or task-pointer advancement.

# Progress log

- 2026-07-27: M02 accepted on `main` at commit `fd7eeda`.
- 2026-07-27: Preliminary M03 Roll implementation reached 65/65 Edit Mode
  tests, but real preview exposed mirrored wall artwork.
- 2026-07-27: User paused final M03 delivery and supplied architecture audit
  requirements A-G.
- 2026-07-27: Moved all uncommitted M03 work from `main` to
  `feat/m03-roll-cup`.
- 2026-07-27: Replaced the preliminary plan with this audited Fold, Roll,
  Seam, diagnostic, sampling, and pre-merge stop contract.
- 2026-07-27: Implemented the audit guards and semantics without M04 topology;
  all 75 Edit Mode tests passed in Unity `6000.3.20f1`.
- 2026-07-27: Saved the clean host preview as
  `Project~/Assets/M03CupPreview.unity`. Its unchanged source image and
  positive-roll cup both display `GPT 5.6` left-to-right with a one-sided
  Back-Cull cup material.
- 2026-07-27: Human preview review found that arbitrary object rotation exposed
  the expected backface-culling holes of M03's zero-thickness wall and bottom.
- 2026-07-27: User authorized commit, push, and an unapproved/unmerged audit PR
  after that visibility defect is repaired; `CURRENT_TASK.md` remains on M03.
- 2026-07-27: Added a package-owned opaque two-sided Unlit preview shader and
  an Edit Mode regression check. Compiler winding, topology, normals, vertex
  counts, and M04 boundaries remain unchanged.
- 2026-07-27: Unity `6000.3.20f1` recompiled the package and passed 76/76 Edit
  Mode tests, including the new preview-culling regression.
- 2026-07-27: Regenerated the 1,358-vertex, 2,496-triangle proof and inspected
  the standard side view plus bottom-facing and reverse-bottom orientations.
  The wall and bottom remained opaque and complete.
- 2026-07-27: JSON parsing, assembly-definition inspection, `git diff --check`,
  package-version consistency, and the final source diff passed.
- 2026-07-27: Second review gate reopened M03 for the two-segment full-turn
  degeneracy, explicit one-turn limit, owned preview hierarchy, final-geometry
  embedding contract, and wall/bottom visual-fit audit. PR #1 remains open and
  unmerged; `CURRENT_TASK.md` remains unchanged.
- 2026-07-27: Second-review implementation passed all 90/90 Edit Mode tests in
  Unity `6000.3.20f1`; the exported result is
  `Project~/TestResults/M03SecondReview-TestResults.xml`.
- 2026-07-27: Regenerated the owned `EditorOnly` preview twice in the live
  editor. It retained one root, cup, source canvas, and untagged preview camera.
  The underside-oblique proof showed the wall meeting the disk, and the
  measured bidirectional boundary gap was `2.20391154E-08 m`.

# Final verification

Unity compilation, the 90/90 Edit Mode suite, exported XML, owned-preview
idempotency, and live wall/bottom fit proof are complete. JSON/asmdef parsing,
repository static checks, and `git diff --check` remain before the final branch
commit. Then push only to `feat/m03-roll-cup`, update PR #1, and leave it
unapproved/unmerged with `CURRENT_TASK.md` unchanged.
