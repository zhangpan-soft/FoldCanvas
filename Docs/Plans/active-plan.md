# Goal

Deliver M06 on `codex/m06-editor-workspace`: a Unity UI Toolkit authoring
workspace that lets a non-modeler create the established FoldCanvas cup from
an empty source asset without code edits or direct vertex manipulation.

The branch is stacked from verified M05 head `46aa02f` while PR #4 remains
open. M06 work may proceed, but neither PR #4 nor this branch is merged by this
plan without a separate human approval.

# User-visible proof

The Editor command `Tools > FoldCanvas > Open Authoring Workspace` opens one
owned window with:

- a left 2D appearance-canvas viewport;
- a right interactive 3D derived preview;
- asset, panel, operation, seam, diagnostic, and bake controls;
- a documented blank-asset-to-cup walkthrough requiring no code edits.

The walkthrough must create a rectangle wall and disk bottom, identify their
standard boundaries, add explicit Place/Roll/Stitch/Solidify operations,
resolve any diagnostics in the same window, preview the result in solid and
debug modes, and save a baked derived asset.

# Scope

## Workspace shell

- Unity UI Toolkit `EditorWindow` with persistent splitter proportions.
- Source-asset selector and explicit Create New action.
- Modular 2D canvas, 3D preview, source inspector, operation timeline, seam
  editor, diagnostics, and bake sections.
- Clear dirty, compiling, valid, invalid, and baked states.

## 2D canvas tools

- zoom centered on the cursor and bounded pan;
- appearance canvas display using FoldCanvas UV convention;
- rectangle and disk panel creation;
- selection, naming, canvas-rect numeric editing, and drag handles;
- standard boundary highlighting and selected seam endpoint highlighting;
- deterministic visual ordering derived from source list order.

## Source editing

- serialized edits with Undo/Redo for every authoritative source mutation;
- explicit forms for supported M06 cup operations rather than raw JSON or
  direct Mesh editing;
- operation reorder, enable/disable, target selection, and operation-specific
  fields;
- seam creation, endpoint boundary selection, mode selection, and removal;
- stable generated IDs that are visible and editable before references exist.

## Preview and diagnostics

- debounced compile after source changes, never once per repaint;
- stale compile requests ignored through a monotonically increasing revision;
- disposable preview Mesh/material ownership with deterministic replacement;
- local preview orbit, frame result, solid/wireframe/panel-color/seam/normal/
  thickness views, and diagnostic focus;
- compile diagnostics shown in deterministic order with structured context;
- selection synchronization between panels, seams, diagnostics, and preview.

## Bake

- explicit user-triggered bake using existing compiler output;
- no automatic asset saving from preview compilation;
- saved Mesh/prefab remains derived and carries a source reference where the
  current data model supports it;
- failed compilation cannot overwrite the last valid bake.

# Non-goals

- direct editing of generated vertices, triangles, UVs, normals, or topology;
- geometry-semantic changes to Fold, Roll, Stitch, Solidify, or SphericalWrap;
- a node graph or operation DAG;
- third-party graph/editor/UI dependencies;
- automatic image segmentation, AI generation, or provider integration;
- runtime/player authoring;
- M07 self-intersection or broken-geometry validation;
- M08 JSON importer/exporter and AI repair loop;
- bevel, subdivision, smoothing, remesh, or mesh cleanup;
- merging PR #4 or this branch without explicit human approval.

# Files expected to change

- `CURRENT_TASK.md`
- `Docs/Plans/active-plan.md`
- `Editor/FoldCanvasAuthoringWindow.cs`
- Editor-only UI/controller/view-model helpers under `Editor/Authoring/`
- modular UXML/USS resources under `Editor/Authoring/UI/`
- Editor assembly definitions only if required by new Editor tests/resources
- `Tests/Editor/M06AuthoringWorkspaceTests.cs`
- `Documentation~/authoring-workspace.md`
- `Documentation~/roadmap.md`
- `README.md`, `README.zh-CN.md`, `CHANGELOG.md`, and `package.json`

The inventory step may refine this list before implementation. Runtime files
change only if an existing public source-editing primitive is genuinely
missing and the decision is recorded first.

# Geometry invariants

- The 2D appearance canvas, panels, named boundaries, seams, and ordered
  operations remain the only authoritative editable geometry source.
- Preview and baked Meshes are compiler outputs and never feed source edits.
- M00-M05 coordinate systems, UV preservation, boundary order, winding,
  tolerance, geometry-budget, and diagnostic contracts remain unchanged.
- Rectangle boundaries remain `uMin/uMax` bottom-to-top and `vMin/vMax`
  left-to-right. Disk `perimeter` remains counter-clockwise from panel front.
- The 2D viewport maps normalized canvas UV with visual origin at the lower
  left after accounting for UI Toolkit's top-left screen coordinates.
- UI list order is source list order. No dictionary or scene-discovery order
  may affect source mutation, compilation, or diagnostic display.
- Stitch remains terminal for each selected panel until topology-group
  deformation propagation is implemented.
- Invalid edits produce or expose diagnostics; the workspace never silently
  substitutes approximate geometry.

# Implementation steps

1. Create the stacked M06 branch, switch the active task, and record UI/source/
   preview ownership contracts before implementation.
2. Inventory existing source definitions, custom inspectors, sample creators,
   compiler entry points, bake paths, and Editor tests. Reuse them instead of
   duplicating geometry logic.
3. Add the UI Toolkit window shell, source asset lifecycle, panel/operation/
   seam list selection model, and an Edit Mode smoke test.
4. Add the 2D canvas transform and rectangle/disk creation/editing with
   Undo/Redo, selection, naming, numeric fields, handles, and boundaries.
5. Add operation forms and deterministic timeline editing for the operations
   needed by the cup proof.
6. Add seam endpoint pairing, mode editing, endpoint highlight, and validation
   of references before mutation.
7. Add debounced deterministic compilation, owned derived preview resources,
   orbit/frame/debug overlays, and diagnostic navigation.
8. Add explicit Bake controls that save only a successful compiler result and
   preserve the source as authority.
9. Add Edit Mode coverage for idempotent window/preview ownership, serialized
   mutations, Undo/Redo, debounce behavior, no leaked derived objects, stable
   diagnostics, blank-to-cup workflow, and bake refusal on failure.
10. Write the manual blank-asset-to-cup walkthrough and validate it in a live
    Unity window using the solid and textured cup views.
11. Run tracked JSON parsing, assembly checks, repository validation,
    `git diff --check`, the complete Edit Mode suite, and live Editor proof.
12. Update package version/changelog, commit, push, and open a non-merged M06
    review PR with explicit dependency on PR #4 if it is still unmerged.

# Test matrix

## Window and ownership

- `OpenWorkspace_TwiceReusesOneWindow`
- `WorkspacePreview_IsEditorOnlyAndOwned`
- `WorkspaceSolidView_UsesOneSidedDiagnosticMaterial`
- `WorkspaceRecompile_ReplacesAndDestroysDerivedMesh`
- `WorkspaceClose_CleansPreviewResources`
- `Workspace_DoesNotModifyMainCamera`

## Source and Undo

- `CreateRectanglePanel_RecordsUndoAndStableId`
- `CreateDiskPanel_RecordsUndoAndStableId`
- `RenamePanel_UndoRedoRestoresReferences`
- `EditCanvasRect_UndoRedoRestoresExactValues`
- `CanvasZoom_KeepsSourcePointUnderCursor`
- `CanvasPan_IsBoundedToKeepCanvasVisible`
- `ReorderOperation_IsDeterministicAndUndoable`
- `CreateSeam_UsesNamedBoundaryReferencesAndUndo`

## Compilation and diagnostics

- `SourceChange_DebouncesCompileInsteadOfRepainting`
- `NewerRevision_DiscardsStalePreviewResult`
- `InvalidSource_ShowsStableOrderedDiagnostics`
- `DiagnosticFocus_SelectsPanelOrOperationContext`
- `FailedCompile_PreservesNoPreviewMesh`

## Cup workflow

- `BlankAsset_ToCupSource_CompilesWithoutCodeEdits`
- `WorkspaceCup_HasClosedVolumeEvidence`
- `BakeValidCup_CreatesDerivedMeshWithoutChangingSource`
- `BakeInvalidCup_DoesNotOverwriteExistingBake`

# Risks and rollback

- **UI Toolkit test fragility:** keep source mutations and preview orchestration
  in testable Editor controllers; reserve pixel behavior for manual proof.
- **SerializedReference operation editing:** use managed-reference-aware
  serialized APIs and tests; do not rebuild operation objects during repaint.
- **Undo reference breakage:** mutate stable IDs and dependent references in one
  Undo group, or reject an unsafe rename with a diagnostic.
- **Preview leaks:** centralize temporary Mesh/material ownership and destroy
  with `DestroyImmediate` on replacement, close, and assembly reload.
- **Compile storms:** revision-based debounce schedules at most one compile for
  the latest state and unregisters callbacks when the window closes.
- **Stacked-branch drift:** keep M06 commits isolated after `46aa02f`; after PR
  #4 merge, rebase or retarget without rewriting accepted M05 behavior.
- Rollback is a branch deletion or reverting isolated M06 commits. Existing
  user-created untracked scenes and test evidence remain untouched.

# Progress log

- 2026-07-31: User explicitly directed development to continue with M06.
- 2026-07-31: Confirmed PR #4 remains open and created stacked branch
  `codex/m06-editor-workspace` from verified M05 head `46aa02f` without merging.
- 2026-07-31: Locked M06 source-authority, UI Toolkit, Undo, debounce, preview
  ownership, diagnostics, and bake contracts before implementation.
- 2026-07-31: Implemented the UI Toolkit split workspace, panel/operation/seam
  forms, cursor-centered 2D editing, owned debug preview, diagnostics, and
  valid-only Bake workflow.
- 2026-07-31: M06 targeted Unity Edit Mode suite passed 23/23, including the
  controller-authored blank-to-closed-cup and protected-bake proofs.
- 2026-07-31: Unity `6000.3.20f1` full Edit Mode suite passed 253/253 with zero
  failures, skips, or inconclusive results. Repository validation, tracked JSON
  parsing, and `git diff --check` also passed.
- 2026-07-31: Live Metal Editor inspection loaded the production cup in the
  M06 window (2,972 vertices, 5,120 triangles, zero errors/warnings), then
  verified Panels, Operations, Seams, Diagnostics, Bake, logical wireframe,
  seam, normal, thickness, textured, and one-sided solid views. Bake reported
  one component with zero open and non-manifold edges.

# Decisions made

- M06 proceeds as a stacked branch because M05 merge authorization was not
  explicit. This preserves progress without changing `main` or PR #4.
- The workspace edits only authoritative FoldCanvas source. Derived preview and
  bake objects never become an alternative source representation.
- A controller layer will separate serialized mutations and compilation from
  UI Toolkit rendering so Edit Mode tests can validate behavior without relying
  on screenshots.
- M06 reuses existing deterministic compiler diagnostics and geometry reports;
  it does not add M07 validation semantics.

# Final verification

Implementation and local validation are complete. Review/hosted evidence must
distinguish:

- repository/static validation;
- complete Unity Edit Mode test results and XML/log locations;
- live-window blank-asset-to-cup walkthrough;
- solid/textured/debug preview inspection;
- manual Undo/Redo and bake checks;
- any remaining limitations.

Local evidence:

- Unity XML: `Project~/TestResults/M06AuthoringFullFinal2.xml`
- Unity log: `Project~/TestResults/M06AuthoringFullFinal2-Editor.log`
- Unity result: 253 passed, 0 failed, 0 skipped, 0 inconclusive
- targeted M06 result: 23 passed, 0 failed, 0 skipped, 0 inconclusive
- repository validation: passed
- tracked/source JSON parsing: passed
- `git diff --check`: passed
- live Editor proof: passed for the existing production-cup source and all
  M06 primary/debug panels; the documented blank-source walkthrough remains a
  required human review exercise before merge
