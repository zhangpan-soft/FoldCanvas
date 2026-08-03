# Goal

Deliver M10 on `codex/m10-extensibility`: provide a bounded contributor API,
versioned sample gallery, deterministic OBJ export, performance evidence, and
reproducible release automation without changing M00-M09 geometry semantics.

# User-visible proof

`Tools > FoldCanvas > Create M10 Ecosystem Proof` creates an authoritative
rectangular canvas asset containing a contributor-style custom wave operation.
The proof passes an explicit registry to the compiler, renders the derived
surface with its artwork, writes a deterministic OBJ beside other generated
artifacts, and displays registry/export evidence under an owned EditorOnly
preview root. `Tools > FoldCanvas > Open Sample Gallery` lists all maintained
sample entries from the package manifest; performance evidence has its own
explicit runner and derived report.

# Scope

- explicit per-compile custom operation registry
- stable registration descriptors and preflight plan
- single-panel, position-only public execution context with rollback
- gallery manifest parser, Schema, canonical manifest, and Editor window
- contributor operation template and compiling test fixture
- deterministic OBJ text exporter and Editor save helper
- maintained Editor performance baseline runner
- deterministic UPM archive builder and tag-gated release workflow
- English/Chinese documentation, package version, CHANGELOG, tests, and proof

# Non-goals

- public topology/triangle/boundary/seam/budget mutation
- registration auto-discovery, reflection scanning, or global mutable state
- FoldScript `0.1` custom operation serialization
- new panel shapes or geometry families
- arbitrary sweep, CSG, holes, bevel, smoothing, remesh, or cleanup
- glTF/FBX/USD, runtime file/network I/O, or cloud publishing
- any milestone after M10

# Files expected to change

- `CURRENT_TASK.md`, `Codex/M10_EXTENSIBILITY.md`
- `Docs/ADR/0008-explicit-extension-boundary.md`
- `Docs/Plans/active-plan.md`
- new Runtime ecosystem registry, gallery, and OBJ exporter files
- `Runtime/Compiler/FoldCanvasCompiler.cs`
- `Runtime/Data/FoldOperationDefinition.cs`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- new Editor gallery, performance, export, and M10 proof files
- new `Samples~/Gallery` and `Samples~/OperationExtension` content
- new gallery and performance JSON Schemas/data
- new M10 Edit Mode tests and fixtures
- release builder/validator and GitHub Actions workflow
- relevant documentation, README files, roadmap, package version, CHANGELOG

# Geometry invariants

- The 2D canvas, panels, seams, and ordered operations remain authoritative.
- A custom operation can change only finite positions of vertices belonging to
  its one preflight-resolved panel.
- Vertex count/order, triangle count/order, boundaries, topology IDs,
  source positions, UV0, panel ownership, and provenance remain unchanged by a
  custom M10 operation.
- Custom operations execute in authored operation-list order and obey the same
  terminal-Stitch restriction as built-in position deformations.
- Missing registration, invalid target, failed validation, failed execution,
  non-finite mutation, or exception produces stable diagnostics and no Mesh.
- Registry descriptor order is ordinal by stable operation type ID, independent
  of registration call order.
- Default compilation with no custom operations follows the existing M00-M09
  code path and produces identical compiled data.
- OBJ vertex/UV/face order follows immutable compiled render-vertex and triangle
  order. Export formatting uses invariant culture and `\n` line endings.
- Gallery and release paths are normalized package-relative paths and cannot
  escape their declared roots.
- Performance timing is evidence only and never participates in compilation.

# Implementation steps

1. Merge approved M09, fast-forward `main`, create
   `codex/m10-extensibility`, and record M10/ADR 0008/this plan.
2. Add custom operation enum value, registry/descriptors, validation and
   execution contexts, deterministic snapshot, and compile overload.
3. Add preflight planning, terminal-Stitch enforcement, transactional execution,
   and FC9001-FC9008 diagnostics without refactoring built-in geometry.
4. Add gallery DTO/parser/diagnostics, JSON Schema, canonical manifest, and
   Editor window.
5. Add contributor template plus the M10 wave proof implemented outside the
   Runtime compiler assembly and compiled through the public registry API.
6. Add deterministic OBJ exporter and Editor helper/proof evidence.
7. Add performance scenarios/runner and baseline data with topology/count
   assertions and non-brittle timing ceilings.
8. Add deterministic UPM archive builder, release validation, tests, and a
   tag-gated GitHub Actions workflow.
9. Update package/version/docs/roadmap/README/CHANGELOG and repository checks.
10. Run JSON/asmdef/runtime-isolation checks, release determinism checks,
    focused tests, the full Unity Edit Mode suite, and foreground proof.
11. Commit/push one isolated M10 branch, open an unmerged audit PR, verify hosted
    repository/Unity checks and artifacts, then wait for human review.

# Test matrix

## Registry and execution

- `Compile_WithoutCustomOperations_IsIdenticalWithEmptyRegistry`
- `RegisteredPositionOperation_ExecutesInAuthoredOrder`
- `RegisteredPositionOperation_PreservesUvTopologyAndProvenance`
- `Registry_DescriptorsAreOrdinalAndRegistrationOrderIndependent`
- `UnregisteredCustomOperation_ReturnsStableDiagnosticWithoutMesh`
- `DuplicateRegistration_ReturnsStableDiagnostic`
- `InvalidRegistration_ReturnsStableDiagnostic`
- `CustomOperation_MissingPanelReturnsStableDiagnosticBeforeTessellation`
- `CustomOperation_FailedValidationDoesNotExecute`
- `CustomOperation_FailedExecutionRollsBack`
- `CustomOperation_NonFiniteMutationRollsBack`
- `CustomOperation_ExceptionReturnsStableDiagnosticAndRollsBack`
- `CustomOperation_AfterSelectedStitchReturnsTerminalDiagnostic`
- `ContributorFixture_CompilesThroughPublicApi`

## Gallery

- `GalleryManifest_CanonicalPackageManifestParses`
- `GalleryManifest_EntryOrderIsPreserved`
- `GalleryManifest_DuplicateIdReturnsStableDiagnostic`
- `GalleryManifest_UnsafePathReturnsStableDiagnostic`
- `GalleryManifest_UnknownVersionReturnsStableDiagnostic`
- `GalleryWindow_OpensCanonicalManifest`

## OBJ

- `ObjExporter_RepeatedExportsAreByteIdentical`
- `ObjExporter_IsInvariantAcrossCurrentCulture`
- `ObjExporter_EmitsOrderedPositionsUvsAndFaces`
- `ObjExporter_PreservesDistinctUvSeamVertices`
- `ObjExporter_DoesNotMutateCompiledDataOrMesh`
- `ObjExporter_InvalidOptionsReturnStableDiagnostic`

## Performance and release

- baseline scenarios compile successfully with expected counts
- repeated scenario geometry hashes remain stable
- performance report preserves scenario order and finite measurements
- release archive builder produces identical bytes twice
- release archive contains approved roots and no `Project~`, `.git`, secrets,
  generated Meshes, logs, or test results
- release version must agree across tag, package, Runtime version, and CHANGELOG

## Editor proof

- `CreateM10EcosystemProof_UsesRegisteredOperationAndExportsObj`
- `CreateM10EcosystemProof_TwiceReusesInactiveOwnedHierarchy`
- `CreateM10EcosystemProof_OneSidedSurfacesFaceOwnedCamera`
- `CreateM10EcosystemProof_DoesNotModifyExistingMainCamera`

## Regression

- all 360 existing M00-M09 tests remain enabled and unchanged
- Runtime remains free of `UnityEditor`, network, and new package dependencies
- default FoldScript `0.1` unknown-operation rejection remains unchanged

# Risks and rollback

- **Global-state nondeterminism:** registry is an explicit instance and compiler
  snapshots it before source tessellation.
- **Partial extension mutation:** positions are snapshotted and restored for
  every unsuccessful or exceptional extension execution.
- **Hidden topology mutation:** the public context exposes no topology,
  triangle, boundary, geometry-addition, or budget APIs.
- **Assembly portability:** native custom definitions are documented as
  requiring their contributor assembly; FoldScript does not pretend to carry
  them.
- **Flaky timings:** tests assert successful measurement and deterministic
  geometry counts, not a narrow machine-specific duration.
- **Release leakage:** archive paths come from an allowlist and are inspected by
  repository validation before upload.
- Rollback is reverting isolated M10 commits. M09 remains merged and user-owned
  untracked `Project~` scenes/test evidence remain untouched.

# Progress log

- 2026-08-03: User instructed continued progression, constituting M09 human
  approval. Confirmed PR #8 retained four successful checks plus one neutral
  wrapper, merged it, and fast-forwarded `main` to `7be4117`.
- 2026-08-03: Created `codex/m10-extensibility` without touching existing
  user-owned untracked host-project evidence.
- 2026-08-03: Read M10 roadmap, architecture, ADRs 0001-0007, current compiler,
  package, samples, CI, release validation, and Editor bake surfaces.
- 2026-08-03: Chose an explicit per-compile, position-only extension boundary;
  recorded ADR 0008 and rejected global discovery/public topology mutation.
- 2026-08-03: Implemented the Runtime registry, exact-type preflight plan,
  single-panel execution context, transactional rollback, compile/bake
  overloads, and stable `FC9001`-`FC9008` diagnostics.
- 2026-08-03: Added the bounded gallery manifest/Schema/window, Runtime-only
  contributor template, deterministic OBJ exporter/Editor adapter, three
  maintained performance cases, and owned M10 wave proof.
- 2026-08-03: Added the allowlisted reproducible UPM builder, archive
  comparison test, SHA-256 evidence, and manual/tag-gated release workflow.
- 2026-08-03: Advanced the package to `0.1.0-preview.18`; updated architecture,
  pipeline, diagnostics, roadmap, English/Chinese README, FoldScript boundary,
  contributor guide, and CHANGELOG.
- 2026-08-03: Unity `6000.3.20f1` passed 34/34 focused M10 Edit Mode tests in
  an isolated host, including owned inactive-object reuse, MainCamera
  non-interference, front-facing one-sided proof geometry,
  unregistered-operation failure, and deterministic OBJ.
- 2026-08-03: Foreground Unity inspection caught and fixed the initial proof
  camera/backface presentation. The final owned perspective camera visibly
  shows the solid one-sided surface and the textured contributor wave without
  modifying `Camera.main` or saving the user's scene.
- 2026-08-03: A clean isolated Unity `6000.3.20f1` host passed all 394/394
  M00-M10 Edit Mode tests with 0 failed, 0 skipped, and 0 inconclusive.
- 2026-08-03: All three five-iteration performance cases stayed inside their
  maintained ceilings with stable geometry hashes. Repository validation,
  JSON parsing, Runtime/Editor isolation, `git diff --check`, and byte-identical
  release-package validation passed. Final archive SHA-256:
  `4855412d230ec7238d2f9673dc3c998ecaf493c5c6ae0336ccced1ba36d1c7f8`.

# Decisions made

- M10 registration is native-asset extensibility, not a silent escape hatch in
  FoldScript `0.1`.
- Built-in operations keep their existing specialized executors. M10 adds a
  safe registered fallback rather than broadly rewriting proven geometry code.
- The first public extension context is single-panel and position-only. This is
  sufficiently useful for procedural deformation while enforceably preserving
  the compiler's topology and provenance contracts.
- OBJ is the first optional exporter because it can be deterministic and
  dependency-free. Rich material/scene exporters remain future optional
  packages.
- Release archives and performance reports are evidence/derived outputs, never
  editable geometry sources.

# Final verification

- Unity Editor: `6000.3.20f1 (c9ba695d4f07)`.
- Focused M10: 34 passed, 0 failed/skipped/inconclusive.
- Full M00-M10: 394 passed, 0 failed/skipped/inconclusive, duration 9.941 s.
- Full XML: `/tmp/foldcanvas-m10-final.TY1UTD-results.xml`.
- Full Editor log: `/tmp/foldcanvas-m10-final.TY1UTD-editor.log`.
- Performance report:
  `/tmp/foldcanvas-m10-final.TY1UTD/Project~/Library/FoldCanvas/M10PerformanceReport.json`.
- Median performance: planar `23.9247 ms`, Roll `4.8031 ms`, registered wave
  `12.4958 ms`; all `withinBaseline=true`.
- Geometry SHA-256: planar `23bcc144e616d55d381f4be808deae158c71616944e7e212ff4bcc4bab470e18`,
  Roll `aa2f1df96b8e9252868df48de5fdda179de5bc2cbe9442d5840b86f777b3ba4b`,
  wave `a43f71c613e85961003c51963702bfee957af1b6d4ed8b1e5fc016eddc3979d1`.
- Deterministic release SHA-256:
  `4855412d230ec7238d2f9673dc3c998ecaf493c5c6ae0336ccced1ba36d1c7f8`.
- Foreground proof: textured wave and one-sided solid visible through one
  owned perspective camera; type ID/count/OBJ digest report visible; existing
  cameras unchanged; scene not saved.
- Local repository/release/static checks: passed.
- Hosted GitHub checks/artifacts: pending branch push and audit PR creation.
