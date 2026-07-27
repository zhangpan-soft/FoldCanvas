# Goal

Complete M02 by implementing a deterministic rigid-crease `Fold` operation
and an editor-generated six-face box proof, without implementing Roll, Stitch,
Solidify, smooth bending, seam welding, or thickness.

# User-visible proof

- One generated appearance canvas contains six clearly different face regions.
- Six rectangle panels select those regions and compile into one closed box.
- The box is produced from the FoldCanvas source and ordered 90-degree fold
  operations, not Unity cube primitives.
- Every face retains its source artwork and orientation after folding.
- The real Unity Editor displays the generated box with its six distinct
  textured faces.

# Scope

- Execute enabled `FoldLineOperationDefinition` entries in source-list order.
- Resolve a normalized panel-space line through the panel's deterministic
  source triangulation into its current 3D embedding.
- Classify source vertices on the positive or negative side of the directed
  line and rotate only the selected side about the current hinge axis.
- Keep hinge vertices fixed and preserve source position, source UV, panel
  ownership, provenance IDs, triangle order, and boundary order.
- Reject non-finite, degenerate, out-of-range, unsupported-falloff, missing
  target, invalid-side, and non-linear/ambiguous hinge requests with stable
  diagnostics.
- Add a GUID-stable editor-generated M02 box source, canvas, bake, and preview
  workflow.
- Add focused Edit Mode tests for every M02 numerical and diagnostic
  acceptance criterion.
- Update M02 documentation, package version, changelog, and task pointer only
  after automated and real-editor acceptance pass.

# Non-goals

- Roll, Stitch, Solidify, thickness, seam welding, or topology changes.
- Smooth Bend fields or any nonzero fold falloff.
- Collision, self-intersection correction, or automatic fold planning.
- JSON import/export or AI-provider integration.
- A general-purpose modeling workspace; that remains M06.
- Any direct text-to-mesh, image-to-mesh, voxel, NeRF, Gaussian splat, or
  opaque model-generation path.

# Files expected to change

- `Docs/Plans/active-plan.md`
- `Runtime/Compiler/FoldLineExecutor.cs`
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- `Runtime/Compiler/FoldCanvasGeometryTolerances.cs`
- `Runtime/Compiler/MeshBuildBuffer.cs`
- `Runtime/Compiler/PanelTessellator.cs`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- `Editor/FoldCanvasSampleCreator.cs`
- `Editor/FoldCanvasWindow.cs`
- `Tests/Editor/FoldLineCompilerTests.cs`
- `Tests/Editor/BootstrapEditorWorkflowTests.cs`
- M02-relevant files under `Documentation~` and `Schema`
- package/version, changelog, and current-task files only after acceptance

# Geometry invariants

- Panel source space remains local XY in meters with normalized coordinates
  `u = sourceX / width + 0.5` and `v = sourceY / height + 0.5`.
- Directed-line side classification is
  `cross2(lineEnd - lineStart, point - lineStart)`.
- `FoldSide.Positive` selects values greater than the normalized hinge
  tolerance; `FoldSide.Negative` selects values less than its negative.
- Values within the hinge tolerance are hinge samples and remain byte-for-byte
  stationary.
- Positive angle handedness is exactly
  `Quaternion.AngleAxis(angleDegrees, currentLineEnd - currentLineStart)`.
- A normalized fold line must be finite, remain inside `[0,1]^2`, and have
  length greater than the centralized normalized-line tolerance.
- Line embedding is evaluated through deterministic barycentric interpolation
  over the target panel's source triangles. First source-order triangle wins
  on shared edges.
- Every crossing between the line and a source triangle edge, plus interval
  midpoints, is checked. All mapped samples must lie on one non-collapsed,
  order-preserving current 3D axis within centralized absolute and relative
  tolerances.
- Rotation changes only current 3D position. UV, source position, ownership,
  provenance, topology, winding, and named-boundary order remain unchanged.
- Operations execute strictly in serialized list order. No dictionary
  enumeration contributes to geometry or diagnostics.
- The M02 proof box occupies `x,y ∈ [-0.5,0.5]` and `z ∈ [-1,0]`. Expected
  outward normals are top `+Z`, bottom `-Z`, front `-Y`, back `+Y`, left
  `-X`, and right `+X`.

# Implementation steps

1. Record M02 semantics, tolerances, diagnostics, and proof geometry in this
   plan before implementation.
2. Retain each panel's deterministic triangle span in the internal build
   record so source-to-current interpolation never relies on global searches
   or dictionary order.
3. Implement fold validation, source-line embedding, linearity checks, side
   classification, and rigid axis rotation.
4. Route `Fold` through the compiler while leaving all later operations as
   explicit unsupported diagnostics.
5. Generate a six-region appearance canvas and six-panel box source with four
   planar layout transforms and six ordered 90-degree folds.
6. Add numerical, preservation, determinism, box-normal, operation-order, and
   diagnostic Edit Mode tests.
7. Run JSON parsing, asmdef inspection, repository validation, diff checks,
   and the complete Unity Edit Mode suite.
8. Create, bake, and inspect the textured proof in the real Unity Editor,
   including multiple visible faces and no compile diagnostics.
9. Only after M02 acceptance passes, update docs, changelog, package version,
   advance `CURRENT_TASK.md` to M03, commit, push, and confirm GitHub CI.

# Test matrix

| Acceptance area | Automated evidence |
| --- | --- |
| 0-degree identity | every current position remains exactly unchanged |
| Signed handedness | `+90` and `-90` match `Quaternion.AngleAxis` around A→B |
| Hinge stability | all on-line samples retain their exact current positions |
| Rigid rotation | every selected vertex preserves distance to the hinge axis |
| Side isolation | only the requested positive or negative source side moves |
| Source preservation | UV, source position, panel index, provenance, indices, and boundaries remain unchanged |
| Operation order | a later fold resolves its hinge from the already-deformed current embedding |
| Ambiguous hinge | a line crossing a prior crease returns the stable non-linear-hinge diagnostic |
| Determinism | repeated compiles produce equal diagnostics and ordered compiled/Unity geometry |
| Box proof | bounds close at one unit and all six first-triangle normals point outward |
| Artwork mapping | each panel retains the exact corners of its distinct canvas region |
| Invalid source | stable codes cover missing target, non-finite/out-of-range/degenerate line, non-finite angle, invalid side, and nonzero falloff |
| Editor sample | repeated creation preserves source/canvas GUIDs and compiles successfully |

# Risks and rollback

- A hinge may cross many triangles after earlier folds. Checking all
  line/triangle-edge breakpoints makes the straight-axis decision explicit and
  deterministic; ambiguous embeddings fail instead of choosing an arbitrary
  local segment.
- Floating-point comparisons can make hinge ownership unstable. All normalized
  and current-space thresholds are centralized and covered on both sides of
  the boundary.
- Public source fields already exist, so M02 adds behavior without deleting or
  renaming public API. Internal triangle-span metadata is not exposed.
- The box proof uses disconnected panels because seam topology is M04. Spatial
  coincidence closes the visible shell but does not weld vertices.
- If a dependency, topology change, or architecture exception becomes
  necessary, stop M02 and require an ADR plus explicit authorization.
- Rollback is limited to the new executor, compiler route, internal triangle
  span, M02 sample workflow, tests, and documentation; M01 planar compilation
  remains independently usable.

# Progress log

- 2026-07-27: Re-read `CURRENT_TASK.md`, `PLANS.md`,
  `Documentation~/architecture.md`, `Codex/M02_FOLD_BOX.md`, and ADRs
  0001-0006.
- 2026-07-27: Audited the M01 compiler, build buffer, tessellators,
  diagnostics, Fold data fields, editor sample workflow, tests, schema, and
  documentation.
- 2026-07-27: Confirmed the worktree was clean at M02 start and that Fold was
  still routed to `FC3001 UnsupportedOperation`.
- 2026-07-27: Recorded the M02 source-to-current hinge resolution, tolerance,
  diagnostic, and proof-box decisions before implementation.
- 2026-07-27: Added internal panel triangle spans and the deterministic
  source-to-current `FoldLineExecutor`, then routed Fold while keeping M03+
  operations explicitly unsupported.
- 2026-07-27: Added all required fold diagnostics and centralized normalized,
  barycentric, minimum-axis, absolute, and relative hinge tolerances.
- 2026-07-27: Added the six-region box canvas/source/proof commands and 16
  focused Fold Edit Mode tests, bringing the complete suite to 43 tests.
- 2026-07-27: Fixed exact NPOT texture import after the first real Unity run
  exposed 512-pixel resizing of the intended 384-pixel canvas.
- 2026-07-27: Replaced a brittle M00 rebake assertion that assumed
  `AssetDatabase.Refresh` preserves one managed object wrapper; stable path,
  GUID, and updated data remain verified.
- 2026-07-27: Completed static checks, Unity compilation, final 43/43 tests,
  multi-orientation artwork inspection, and saved the local M02 proof scene.

# Decisions made

- Hinge embedding uses the existing deterministic source triangulation rather
  than rectangle-only grid assumptions. This keeps the algorithm inspectable
  and permits any panel whose complete authored line lies inside its source
  triangulation.
- Current hinge validation samples exact line/triangle-edge crossings and the
  midpoint of every resulting interval. Because the mapping is affine inside
  each source triangle, these samples are sufficient to detect a piecewise
  line that is no longer one straight 3D axis.
- The proof is a closed but unwelded shell. Five source panels fold once; the
  bottom panel folds twice around opposite source edges, while the top remains
  the reference face. Seam welding remains exclusively M04.
- The editor-generated proof may dynamically choose an available unlit shader
  for visualization, but Runtime remains render-pipeline independent.

# Final verification

- `package.json`, the FoldScript schema, and all three changed/inspected asmdef
  JSON files parsed successfully.
- `python3 Scripts/validate_repository.py`: passed.
- `git diff --check`: passed.
- Assembly inspection confirmed Runtime still has no assembly references and no
  `UnityEditor` usage; editor-only APIs remain under `Editor`.
- Unity `6000.3.20f1` imported and compiled the final package without C# errors.
- The final complete Edit Mode run passed 43/43 tests, 0 failed, 0 skipped, in
  0.233 seconds.
- The real Unity Editor generated the M02 source, baked a 24-vertex /
  12-triangle mesh, and displayed a closed box with six distinct colored,
  lettered, direction-marked artwork regions.
- Multiple preview orientations exposed all six regions and retained readable
  direction arrows; automated checks independently verified exact UV corners,
  one-unit bounds, and all six outward local face normals.
- The proof scene is saved locally at
  `Assets/FoldCanvasGenerated/M02BoxPreview.unity`; source, texture, mesh,
  material, and scene remain ignored local/generated artifacts.
- Roll, Stitch, Solidify, seam welding, smooth falloff, thickness, collision,
  and all later-milestone behavior were not implemented.
- Git commit, push, and GitHub CI result are recorded in the release handoff
  after this plan is finalized.
