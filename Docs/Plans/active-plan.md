# Goal

Deliver M09 on `codex/m09-topology`: prove that FoldCanvas can encode a torus
and a cup with a real topological handle while keeping the 2D source document,
explicit seam graph, deterministic compiler, and derived-Mesh architecture.

# User-visible proof

One M09 proof command creates two source assets and derived previews:

1. a torus compiled from a rectangular canvas region, one `ToroidalWrap`, and
   two explicit Weld seams that close the U and V cycles;
2. a cup whose rectangular strip handle is positioned and folded by existing
   operations, welded to two distinct cup-rim boundary spans, and Solidified
   with the cup into one closed manifold volume.

The proof includes textured and solid-color views plus wireframe/topology
evidence. No torus primitive, imported handle Mesh, Boolean solver, or manual
generated-Mesh edit participates in compilation.

# Scope

## Minimal topology vocabulary

- extend `BoundaryReference` with an optional normalized non-wrapping span
  `[startT,endT]` in authored boundary order;
- omitted spans preserve the existing complete-boundary behavior byte for
  byte;
- when either seam endpoint uses a span, both correspondence paths are open
  chains; `reverseB` reverses B only after its authored span is selected;
- arbitrary span endpoints are inserted through the existing deterministic
  triangle-edge subdivision path and remain subject to geometry budgets;
- add `ToroidalWrap` as an explicit rectangle deformation, not a Mesh source;
- keep cycle closure explicit: `ToroidalWrap` makes coincident boundary
  positions, while Stitch-selected Weld seams create topology identity.

## ToroidalWrap

- accept only one finite, congruent, non-degenerate planar rectangle embedding;
- resolve the current local frame after earlier unit rigid transforms;
- map the source rectangle by authored major/minor angle ranges and radii;
- support major angle along U or V;
- allow at most one signed turn on each angular range;
- require `majorRadius > minorRadius > 0` to avoid a self-intersecting spindle
  torus in M09;
- require at least three authored segments for every full-turn parameter axis;
- orient every triangle toward the tube-outward direction without relying on a
  double-sided material;
- preserve source UV and render duplicates at both closure seams.

## Handle cup

- use the existing rolled wall and disk bottom source;
- use one ordinary rectangle strip with three longitudinal cells;
- position the strip in the cup's current frame, then apply two grid-aligned
  rigid folds to form a deterministic arch;
- select two distinct one-edge spans on the cup's top rim and Weld the strip's
  `vMin` and `vMax` boundaries to them;
- run Stitch after all position deformation and Solidify only after every
  attachment seam;
- require zero attachment gap, no open/non-manifold final topology edge, one
  connected closed volume, and non-zero volume.

## Portable source and Editor proof

- add `toroidalWrap` plus optional boundary-reference `span` to FoldScript
  DTOs, strict decoder, canonical serializer, unit converter, and JSON Schema;
- expose M09 fields through the existing M06 authoring workspace rather than a
  parallel editor;
- add reproducible torus/handle sample creation and owned preview objects;
- update English/Chinese field documentation, pipeline, architecture,
  diagnostics, roadmap, README, sample guide, package version, and changelog.

# Non-goals

- arbitrary parametric surfaces or arbitrary 3D sweep curves;
- circular or spline holes cut into panel interiors;
- general-purpose Boolean union, subtraction, intersection, or CSG;
- attaching a tube end to an uncut interior face;
- implicit proximity welding or automatic topology cleanup;
- multiple angular turns, spindle/horn tori, layered torus surfaces;
- bevel, subdivision, smoothing, remesh, Mesh cleanup, or PBR inference;
- skeletal deformation or runtime/network generation;
- any milestone after M09.

# Files expected to change

- `CURRENT_TASK.md`
- `Docs/Plans/active-plan.md`
- new `Docs/ADR/0007-cyclic-topology.md`
- `Runtime/Data/FoldOperationDefinition.cs`
- `Runtime/Data/SeamDefinition.cs`
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- `Runtime/Compiler/FoldCanvasSourceValidator.cs`
- `Runtime/Compiler/BoundaryCorrespondenceSolver.cs`
- new `Runtime/Compiler/ToroidalWrapExecutor.cs`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- FoldScript DTO/decoder/serializer/converter files
- M06 operation and seam authoring surfaces
- new M09 Editor sample/proof creator
- new M09 Edit Mode tests and fixtures
- `Schema/foldcanvas.schema.json`
- relevant documentation, README, sample guide, `package.json`, and
  `CHANGELOG.md`

Any additional file is recorded in the progress log before final submission.

# Geometry invariants

- The appearance canvas, panels, boundary references/spans, seams, and ordered
  operations are source; all Unity Meshes are disposable compiler outputs.
- Source UV0 and provenance are unchanged by ToroidalWrap, folds, boundary
  subdivision, welding, and Solidify.
- A boundary span is evaluated by normalized current-space arc length in the
  authored boundary direction. It is selected before `reverseB` is applied.
- M09 spans are finite, non-wrapping, and satisfy
  `0 <= startT < endT <= 1`. Invalid spans fail with one stable diagnostic.
- Spatial coincidence alone never closes a cycle. A full toroidal angle leaves
  distinct render samples until an explicit Stitch-selected Weld seam unions
  their topology identities.
- Let `CurrentU`, `CurrentV`, and
  `CurrentNormal = normalize(cross(CurrentU,CurrentV))` be the resolved frame.
  For major angle `a`, minor angle `b`, major radius `R`, and minor radius `r`:

  ```text
  radial(a) = cos(a) * CurrentU + sin(a) * CurrentNormal
  P(a,b) = CurrentOrigin
         + (R + r*cos(b)) * radial(a)
         + r*sin(b) * CurrentV
  ```

- Positive authored major/minor ranges use their documented signed mapping;
  final triangle winding is normalized so geometric normals point along
  `cos(b)*radial(a) + sin(b)*CurrentV`.
- Identical input produces identical vertices, indices, topology IDs,
  diagnostics, boundary-span insertions, and report ordering.
- A fully stitched torus has one component, Euler characteristic `0`, zero
  open topology edges, and zero non-manifold topology edges.
- The Solidified handle cup has one connected closed oriented component. The
  attachment seams do not overlap geometry and do not rely on epsilon offsets.
- UV closure is intentionally discontinuous in attribute space: closure-side
  render vertices retain their distinct canvas UV while sharing one logical
  topology identity after Weld.

# Implementation steps

1. Merge accepted M08, update local `main`, create `codex/m09-topology`, read
   M09/architecture/ADRs, and record ADR 0007 plus this plan.
2. Add boundary-span source data, validation, correspondence extraction,
   subdivision, rollback, and structured diagnostics without changing omitted-
   span behavior.
3. Add ToroidalWrap source data and executor with current-frame validation,
   finite/radius/range/tessellation checks, deterministic mapping, and outward
   winding.
4. Integrate ToroidalWrap with operation ordering, Stitch-terminal enforcement,
   geometry validation, FoldScript import/export, JSON Schema, and M06 forms.
5. Add torus fixtures/tests for both closure cycles, Euler characteristic,
   manifoldness, radii, winding, UV seams, invalid inputs, and determinism.
6. Add the folded-strip handle cup fixture, two rim-span attachment seams,
   Solidify, attachment/closed-volume tests, and regression tests for full
   boundary Stitch behavior.
7. Add M09 sample/proof creation with solid, textured, wireframe, exterior,
   underside, and topology views owned by one EditorOnly preview root.
8. Update documentation, package version, Schema, README, sample guide, task
   state, and newest-first changelog.
9. Parse all JSON, inspect asmdefs/runtime isolation, run repository validation,
   `git diff --check`, targeted M09 tests, the complete Edit Mode suite, and
   live Unity proof inspection.
10. Commit and push the isolated branch, create a non-auto-merged review PR,
    verify hosted repository/Unity jobs and artifacts, and wait for human audit.

# Test matrix

## Boundary spans

- `BoundarySpan_OmittedPreservesFullBoundaryBehavior`
- `BoundarySpan_SelectsDeterministicOpenSubchain`
- `BoundarySpan_OffGridEndpointsSubdivideDeterministically`
- `BoundarySpan_ReverseBAppliesAfterSelection`
- `BoundarySpan_InvalidRangeReturnsStableDiagnostic`
- `BoundarySpan_FailedStitchRollsBackInsertedGeometry`

## Torus

- `ToroidalWrap_RectangleMapsToStableMajorAndMinorRadii`
- `ToroidalWrap_AfterRigidTransformPreservesCurrentFrame`
- `ToroidalWrap_FullCyclesRequireThreeSegmentsPerAxis`
- `ToroidalWrap_MultiTurnReturnsStableDiagnostic`
- `ToroidalWrap_InvalidRadiiReturnStableDiagnostic`
- `ToroidalWrap_InvalidDirectionReturnsStableDiagnostic`
- `ToroidalWrap_NonPlanarEmbeddingReturnsStableDiagnostic`
- `ToroidalWrap_MajorAlongVClosesBothCycles`
- `ToroidalWrap_NegativeFullTurnsRemainOutwardAndClosed`
- `ToroidalWrap_FullTurnsRemainOpenWithoutExplicitStitch`
- `ToroidalWrap_AfterSelectedStitchReturnsOrderingDiagnostic`
- `Torus_TwoExplicitWeldsProduceEulerCharacteristicZero`
- `Torus_HasNoOpenOrNonManifoldTopologyEdges`
- `Torus_HasOutwardWinding`
- `Torus_UvSeamsRetainDistinctRenderVerticesAndSharedTopology`
- `Torus_RepeatedCompilesAreDeterministic`

## Handle cup

- `HandleCup_AttachmentSpansHaveZeroPositionGap`
- `HandleCup_AttachmentEdgesHaveTwoOppositeUses`
- `HandleCup_SolidifyProducesOneClosedVolume`
- `HandleCup_HasNoOpenOrNonManifoldTopologyEdges`
- `HandleCup_HasNonZeroOrientedVolume`
- `HandleCup_RepeatedCompilesAreDeterministic`

## FoldScript and Editor integration

- `FoldScript_RoundTripPreservesBoundarySpanAndToroidalWrap`
- `FoldScript_ImportedTorusCompilesClosedEulerZero`
- `FoldScript_ToroidalRadiiFollowDocumentUnits`
- `FoldScript_InvalidBoundarySpanReturnsStableDiagnostic`
- M09 sample creation twice preserves source GUIDs and successful compiles
- M09 proof creation is owned, idempotent, inactive-object safe, and does not
  modify an existing MainCamera

## Regression

- all existing M00-M08 tests remain enabled and unchanged;
- complete-boundary Stitch behavior is byte/topology compatible;
- current Roll/SphericalWrap/Stitch/Solidify contracts do not weaken;
- Runtime remains free of `UnityEditor`, provider, network, and new package
  dependencies.

# Risks and rollback

- **Span endpoint insertion:** reuse the proven boundary triangle-edge split,
  reserve before mutation, and keep the whole Stitch transactional.
- **Closed boundary parameter 1:** canonicalize it to the authored terminal or
  first topology sample instead of inserting a duplicate zero-length edge.
- **Torus winding:** derive expected outward direction analytically, normalize
  one panel-wide winding decision, then validate every triangle.
- **Torus self-intersection:** require `majorRadius > minorRadius`; horn/spindle
  surfaces return a stable diagnostic.
- **Handle corner offset:** align strip and cup front normals at attachments and
  test the existing bounded Solidify miter rather than adding a bevel.
- **Stitch ordering:** ToroidalWrap and handle folds must precede any Stitch
  selecting their panels; existing terminal-Stitch rules remain authoritative.
- **Compatibility:** optional span omission is the exact legacy path; FoldScript
  only writes `span` when explicitly authored.
- Rollback is reverting isolated M09 commits. M08 `main` and user-owned
  untracked Unity scenes/test evidence remain untouched.

# Progress log

- 2026-08-03: User authorized M09, which constitutes M08 human approval.
- 2026-08-03: Confirmed all PR #7 checks had passed, merged it into `main` as
  `dcc8574`, fast-forwarded local `main`, and created
  `codex/m09-topology` without touching user-owned untracked files.
- 2026-08-03: Read M09, architecture, M08 handoff, compiler/seam/Solidify
  implementation, FoldScript boundary, roadmap, and ADRs 0001-0006.
- 2026-08-03: Selected normalized boundary spans plus explicit
  `ToroidalWrap` and existing Weld seams as the smallest M09 vocabulary.
- 2026-08-03: Implemented transactional boundary spans, deterministic off-grid
  source-triangle subdivision, ToroidalWrap, FoldScript/Schema conversion, M06
  forms, diagnostics, and terminal-Stitch ordering.
- 2026-08-03: Added the explicit two-cycle torus and folded-strip handled-cup
  source/proof generators. The handle attaches to two distinct top-rim spans
  and the final Solidify reports one closed connected volume.
- 2026-08-03: Added 32 focused M09 tests plus Bootstrap workflow and M06
  authoring regressions. A fresh temporary host project passed all 360 Edit
  Mode tests under Unity `6000.3.20f1`; the proof command compiled both M09
  sources successfully.
- 2026-08-03: Updated the English/Chinese source-field, pipeline, architecture,
  geometry, diagnostics, roadmap, authoring, README, sample, version, Schema,
  and changelog contracts for `0.1.0-preview.17`.
- 2026-08-03: Final fresh-host verification passed all 360/360 Edit Mode tests
  on Unity `6000.3.20f1` with zero failures, skips, or inconclusive results.
  Evidence: `/tmp/foldcanvas-m09-final3-results.xml` and
  `/tmp/foldcanvas-m09-final3-editor.log`; repository validation and
  `git diff --check` also passed.
- 2026-08-03: Generated the proof in the foreground Unity Editor and inspected
  overview, torus-only, and handle-cup-only views. Textured, one-sided solid,
  and logical-wireframe torus views expose both cyclic holes; the cup handle
  connects at both authored rim spans and its solid/wireframe views show no
  visible background crack at the attachments.

# Decisions made

- Torus cycles remain explicit seam graph operations. ToroidalWrap changes
  positions only; it never silently unions U or V boundaries.
- The handle proof is a folded rectangular strip attached to two cup-rim
  spans. M09 does not claim support for punching tube sockets into interior
  faces.
- Boundary spans are normalized current-space arc-length intervals in authored
  order. They do not store raw vertex indices and therefore survive authored
  tessellation changes.
- A span is non-wrapping in M09. A future cyclic-span syntax requires a new ADR
  rather than overloading `startT > endT`.
- ToroidalWrap accepts congruent planar embeddings and may preserve a unit
  reflection, matching Roll/SphericalWrap current-frame philosophy.
- Full-turn axes require at least three source segments. Two segments create
  overlapping coplanar angular slabs even when individual triangles have area.

# Final verification

Pending implementation. Record exact Unity version, targeted/full test counts,
result/log paths, live proof measurements, repository checks, hosted CI, and
artifact links here before opening the M09 audit gate.
