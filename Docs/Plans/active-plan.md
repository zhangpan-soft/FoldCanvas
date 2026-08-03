# Goal

Deliver M07 on `codex/m07-geometry-validator`: a deterministic geometry
validation stage that converts malformed or self-intersecting generated
geometry into stable, localized diagnostics suitable for a human author and a
future provider-neutral AI repair loop.

# User-visible proof

The existing FoldCanvas authoring workspace compiles intentionally broken
fixtures and displays one clear primary diagnostic for each defect, including
its available source and geometry context. A valid panel, cup, and sphere still
compile through the unchanged source-to-derived-Mesh pipeline.

The proof includes fixtures for invalid indices, non-finite coordinates,
zero-area and duplicate triangles, a bow-tie vertex, inverted winding, an open
seam, a zero-length boundary, self-intersecting rolled surfaces, and intersecting
thickness shells.

# Scope

## Validation report

- add one read-only geometry-validation report to `FoldCanvasCompileResult`;
- report the executed level and deterministic counts for vertices, triangles,
  topology edges, components, seams, candidates, and confirmed intersections;
- retain stable evidence records for components and confirmed triangle pairs;
- expose no mutable Mesh or repair API.

## Structured diagnostics

- preserve existing `FC5001`-`FC5007` behavior where it already names the
  correct root cause;
- add stable FC5xxx codes for incomplete/invalid index buffers, duplicate
  triangles, open topology, inconsistent winding, disconnected components,
  seam closure, bow-tie vertices, topology-position conflicts, inverted closed
  components, strict self-intersection, degenerate compiled boundaries, and a
  bounded strict-check budget;
- extend diagnostic context with optional vertex, topology-vertex, component,
  triangle-pair, and topology-edge identities;
- keep structured values in deterministic key order.

## Validation levels

- `Basic` runs structural safety, finite coordinates, index range, collapsed
  topology, zero area, duplicate triangles, non-manifold incidence, and
  orientation-conflict checks;
- `Standard` adds open-boundary/component evidence, seam closure, boundary
  length, bow-tie vertex fans, topology-position consistency, connectivity, and
  closed-component orientation;
- `Strict` adds deterministic sweep-and-prune broad phase and exact
  triangle-triangle intersection confirmation;
- lower levels never run higher-level expensive checks.

## Editor proof

- show geometry context in the existing Diagnostics tab;
- show validation-level and report-count evidence without introducing a second
  authoring model;
- preserve diagnostic focus for panel/operation/seam source context.

# Non-goals

- automatic repair, vertex welding, remesh, cleanup, decimation, smoothing,
  subdivision, bevel, or collision resolution;
- changing Fold, Roll, Stitch, Solidify, or SphericalWrap geometry semantics;
- global continuous-collision detection or animation-time validation;
- treating intentional open geometry as a compile error solely because it is
  open; Standard reports it as localized warning evidence;
- M08 JSON round-trip, AI adapter, or repair-payload orchestration;
- M09 handles, torus, cyclic topology, or multiple-hole panel domains.

# Files expected to change

- `CURRENT_TASK.md`
- `Docs/Plans/active-plan.md`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- `Runtime/Compiler/FoldCanvasGeometryValidator.cs`
- `Runtime/Compiler/FoldCanvasCompileResult.cs`
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- `Runtime/Compiler/MeshBuildBuffer.cs`
- `Runtime/Compiler/StitchExecutor.cs`
- `Runtime/Compiler/FoldCanvasGeometryTolerances.cs`
- `Runtime/Compiler/FoldCanvasSourceValidator.cs`
- `Editor/Authoring/FoldCanvasWindow.DiagnosticsAndBake.cs`
- `Tests/Editor/M07GeometryValidatorTests.cs`
- `Tests/Editor/Fixtures/M07AdversarialGeometryFixtures.cs`
- relevant bilingual documentation, `README.md`, `README.zh-CN.md`,
  `CHANGELOG.md`, and `package.json`

The inventory may narrow this list. Any additional file is recorded in the
progress log before final submission.

# Geometry invariants

- The 2D appearance canvas, panels, named boundaries, seams, and ordered
  operations remain authoritative. Validation consumes generated geometry but
  never mutates it.
- Identical source/settings/compiler version produce identical report values,
  diagnostic order, diagnostic context, and confirmed intersection-pair order.
- All edge incidence uses `TopologyVertexId`, not raw render indices, so UV and
  provenance splits do not create false cracks.
- A topology edge is the ordered-independent pair `(minId,maxId)`. Orientation
  is recorded from the directed triangle edge relative to that order.
- Triangle and component identities use source index order. Components are
  sorted by minimum topology vertex ID before assigning component indices.
- Existing source UVs, vertex/index order, boundary order, winding, weld
  tolerance, and geometry-budget behavior are unchanged.
- Strict self-intersection ignores triangle pairs sharing a topology vertex,
  because they are adjacent surface incidence rather than global crossings.
- Broad-phase pairs are sorted lexicographically by triangle indices before
  exact testing. Exact evidence uses the same order.
- An intentional open sheet may remain a successful compile; its open-edge and
  open-boundary evidence is a Standard warning. Non-manifold, inconsistent,
  inverted closed, or intersecting geometry is an error.
- No validator stage performs a repair or substitutes approximate geometry.

# Implementation steps

1. Merge the accepted M05/M06 stack, create the isolated M07 branch, and lock
   level/ordering/no-repair contracts.
2. Inventory existing compiler validation, topology metadata, seam execution,
   closed-volume evidence, diagnostics, and Editor presentation.
3. Add immutable geometry-context and validation-report types without removing
   existing diagnostic fields or codes.
4. Record executed Weld seam sample pairs in the build buffer so final closure
   validation can identify seam and operation without reconstructing topology.
5. Implement staged Basic and Standard validation with deterministic category
   precedence and root-cause suppression.
6. Implement bounded Strict broad phase and exact triangle intersection,
   excluding topology-adjacent pairs and sorting all evidence.
7. Integrate the report before Mesh creation, preserve source/operation errors,
   and keep valid open sheets successful.
8. Extend the M06 Diagnostics/Bake view with geometry context and report counts.
9. Add adversarial fixture builders and Edit Mode tests for every required
   defect, level gating, determinism, no-repair, and valid legacy assets.
10. Update field reference, pipeline, diagnostics, roadmap, architecture,
    bilingual README, package version, and newest-first changelog.
11. Run repository validation, JSON/YAML parsing, assembly/runtime isolation,
    `git diff --check`, the targeted M07 suite, and the complete Edit Mode suite.
12. Inspect diagnostics in a live Unity window, then commit, push, and create a
    non-auto-merged M07 review PR.

# Test matrix

## Structural and Basic

- `InvalidTriangleIndex_ReturnsStableDiagnosticWithoutThrow`
- `IncompleteTriangleIndexBuffer_ReturnsStableDiagnostic`
- `NonFiniteVertex_ReturnsLocalizedStableDiagnostic`
- `ZeroAreaTriangle_ReturnsLocalizedStableDiagnostic`
- `DuplicateFace_ReturnsDuplicateTriangleWithoutNonManifoldFlood`
- `NonManifoldEdge_ReturnsSortedEdgeContext`
- `InvertedFace_ReturnsInconsistentWinding`

## Standard

- `BowTieVertex_ReturnsTopologyVertexContext`
- `OpenSeam_ReturnsWarningAndKeepsIntentionalSheetSuccessful`
- `ZeroLengthBoundary_ReturnsStableBoundaryDiagnostic`
- `DisconnectedComponents_ReturnStableComponentEvidence`
- `WeldSeamClosureGap_ReturnsSeamAndOperationContext`
- `TopologyPositionConflict_ReturnsStableDiagnostic`
- `InvertedClosedComponent_ReturnsStableDiagnostic`
- `BasicLevel_DoesNotEmitStandardDiagnostics`

## Strict

- `SelfIntersectingRoll_ReturnsConfirmedTrianglePair`
- `ThicknessOverlap_ReturnsConfirmedTrianglePair`
- `StrictLevel_ReportsBroadPhaseAndExactCounts`
- `StrictCandidateBudgetExceeded_ReturnsStableDiagnostic`
- `StandardLevel_DoesNotRunExactIntersection`
- `TopologyAdjacentTriangles_AreNotSelfIntersections`
- `StrictValidation_IsDeterministicAcrossRepeatedRuns`

## Regression and integration

- `GeometryValidator_DoesNotMutateInputBuffer`
- `GeometryValidationReport_CollectionsAreReadOnly`
- `ValidRectangle_BasicStillCompiles`
- `ValidProductionCup_StrictHasNoConfirmedIntersection`
- `ValidSphere_StrictHasNoConfirmedIntersection`
- `InvalidValidationLevel_ReturnsStableSourceDiagnostic`
- all pre-existing M00-M06 tests remain enabled and unchanged.

# Risks and rollback

- **False self-intersections:** skip topology-adjacent triangle pairs, use a
  tolerance-scaled SAT test, and keep exact pair fixtures for coplanar and
  non-coplanar cases.
- **Strict performance:** use deterministic X-axis sweep-and-prune, bound exact
  candidates, and emit an explicit diagnostic instead of hanging or silently
  skipping work.
- **Intentional open assets:** open/disconnected evidence is Warning at Standard
  so a valid sheet remains usable.
- **Diagnostic regressions:** retain existing FC5001/FC5002/FC5003 meanings for
  current root causes and stop lower-confidence stages after fatal structural
  errors.
- **Dictionary nondeterminism:** sort edge keys, component representatives,
  seam records, candidate pairs, and evidence before report/diagnostic output.
- Rollback is reverting the isolated M07 commit. Generated Meshes remain
  disposable, and user-owned untracked Unity scenes/results remain untouched.

# Progress log

- 2026-08-03: User authorized merging and continuing development.
- 2026-08-03: Merged PR #4 into `main`, retargeted PR #5 from the merged M05
  branch to `main`, verified its one-commit/38-file M06 diff and green checks,
  then merged PR #5.
- 2026-08-03: Fast-forwarded local `main` to `f5c8116` and created
  `codex/m07-geometry-validator` while preserving all existing untracked Unity
  scenes and test evidence.
- 2026-08-03: Read the M07 milestone, current architecture, relevant ADRs, and
  inventoried existing compiler, topology, closed-volume, seam, diagnostic,
  and Editor validation surfaces.
- 2026-08-03: Added the immutable final-geometry report and context, staged
  Basic/Standard/Strict validator, transactional executed-Weld evidence,
  Diagnostics-tab summary, and 28 adversarial/regression tests.
- 2026-08-03: Unity `6000.3.20f1` passed the targeted M07 suite `28/28` and the
  complete Edit Mode suite `281/281`; repository validation and
  `git diff --check` passed.
- 2026-08-03: Refreshed the live Unity package and inspected the M06
  Diagnostics tab on the production cup. It displayed Basic, one component,
  zero open/non-manifold edges, zero confirmed intersections, and the existing
  valid 2972-vertex/5120-triangle preview.

# Decisions made

- M07 is a report-and-diagnostic stage inside the existing deterministic
  compiler, not a Mesh postprocessor.
- Basic retains current fatal safety behavior. Standard adds author-facing
  topology/seam evidence. Strict is the only level that performs global
  triangle-pair intersection work.
- Diagnostic category precedence suppresses cascades: structural defects stop
  topology analysis; degenerate/duplicate faces stop edge analysis; fatal
  topology defects stop strict intersection.
- Open and disconnected geometry are evidence, not inherently invalid, because
  FoldCanvas supports intentional sheets and multi-part assets.
- Executed Weld seam pairs are recorded during Stitch rather than recomputed by
  a validator that could accidentally subdivide or mutate boundaries.
- Exact self-intersection uses deterministic triangle SAT with additional
  coplanar in-plane axes after sweep-and-prune broad phase.

# Final verification

- Targeted M07: `28 passed, 0 failed, 0 skipped, 0 inconclusive`.
- Complete Edit Mode: `281 passed, 0 failed, 0 skipped, 0 inconclusive`.
- Unity: `6000.3.20f1 (c9ba695d4f07)`.
- Local XML: temporary isolated host
  `TestResults/m07-full-results.xml`; Editor log:
  `Project~/M07FullTestRun.log` beneath the same temporary host.
- `python3 Scripts/validate_repository.py`: passed.
- `git diff --check`: passed.
- Live Diagnostics-tab inspection passed; evidence was saved outside the
  repository at `/tmp/FoldCanvas-M07-Diagnostics-2026-08-03.jpg`.
- Hosted GitHub Actions/artifacts remain to be recorded after opening the
  review PR.
- M08, M09, automatic repair, Bevel, subdivision, smoothing, remesh, and Mesh
  cleanup were not implemented.
