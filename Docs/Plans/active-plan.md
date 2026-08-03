# Goal

Deliver M08 on `codex/m08-foldscript-ai`: make FoldScript `0.1` a real,
deterministic interchange format that can round-trip through
`FoldCanvasAsset`, reject untrusted input before geometry allocation, serialize
compiler evidence into a provider-neutral repair request, and accept only a
corrected source that passes the same import and compiler gates.

# User-visible proof

The M06 workspace can import the checked-in cup FoldScript into an editable
source asset, compile its derived cup, and export canonical JSON. Reimporting
that output preserves panels, seams, operation order, compile settings, units,
UV canvas metadata, and geometry. An invalid source produces one stable repair
payload; a corrected second JSON document clears the targeted diagnostic and
compiles. No provider account or network connection is involved.

# Scope

## FoldScript data and canonical JSON

- public explicit DTOs for document, canvas, panel, seam, all M00-M07
  operations, and compile settings;
- a bounded JSON reader that rejects malformed syntax, duplicate properties,
  excessive depth/nodes/strings, non-standard NaN/Infinity tokens, and
  oversized input before document conversion;
- strict `0.1` structural validation, required/additional-property checks,
  identifier grammar, enum values, numeric bounds, collection limits, and
  cross-reference/duplicate-ID checks;
- canonical exporter with documented root/property order, preserved source
  array order, invariant round-trip numeric formatting, deterministic escaping,
  and one trailing newline;
- unknown schema versions and unknown operation types fail explicitly.

## Unity conversion and paths

- store portable asset/document identity, display name, units, appearance
  reference, pixel dimensions, and optional canonical extensions on
  `FoldCanvasAsset` source metadata;
- explicit DTO-to-native and native-to-DTO converters; no Unity internal
  serialization is part of FoldScript;
- convert centimeter/millimeter source lengths to native meters and export
  back using the declared document unit;
- Runtime performs no file I/O and accepts an appearance resolver contract;
- Editor resolves a relative appearance reference only from a FoldScript file
  under `Assets/` or `Packages/`, normalizes the result, rejects traversal or
  absolute/network paths, and then uses `AssetDatabase`;
- importer creates or replaces only the explicitly selected `.asset`, with
  Undo/dirty/save behavior appropriate to Editor workflows.

## Repair contract

- provider-neutral `IFoldCanvasSourceProposer` and
  `IFoldCanvasSourceRepairer` interfaces with no SDK dependency;
- immutable repair request/response types containing schema/compiler version,
  asset ID, canonical source JSON, stable diagnostics, compact numeric values,
  repair suggestions, and source/geometry context;
- no binary Mesh, raw vertex, triangle, texture, credential, or provider data;
- corrected responses re-enter the normal bounded importer and deterministic
  compiler; no privileged repair path exists.

## Editor proof

- add Import FoldScript and Export FoldScript actions to the existing M06
  toolbar without creating a second source editor;
- imported source becomes the selected editable `FoldCanvasAsset` and compiles
  through the existing preview;
- Diagnostics can create/copy or save the current canonical repair payload;
- status and failure notifications report stable diagnostic codes.

# Non-goals

- OpenAI, local-model, or other provider implementation, authentication,
  network transport, prompt design, or automatic request execution;
- executable extensions, arbitrary polymorphic type loading, embedded scripts,
  base64 geometry, binary Mesh import, or external URL fetching;
- automatic source repair, silent defaulting of invalid required fields, or
  geometry fallback;
- changing Fold, Roll, SphericalWrap, Stitch, Solidify, or M07 validator
  semantics;
- M09 handle/torus/cyclic topology, Bevel, subdivision, smoothing, remesh,
  Mesh cleanup, PBR inference, or runtime cloud generation.

# Files expected to change

- `CURRENT_TASK.md`
- `Docs/Plans/active-plan.md`
- `Runtime/Data/FoldCanvasAsset.cs`
- new `Runtime/FoldScript/` DTO, parser, validator, serializer, converter, and
  result files
- new `Runtime/AI/` provider-neutral repair contracts and payload builder
- `Runtime/Data/FoldCanvasLimits.cs`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- new `Editor/FoldScript/` project-path and asset import/export utilities
- existing M06 workspace UXML/controller/window diagnostics surfaces
- new `Tests/Editor/M08FoldScriptTests.cs` and hostile-input fixtures
- `Schema/foldcanvas.schema.json` only where executable M08 limits or defaults
  must be synchronized
- relevant architecture, pipeline, field reference, FoldScript, AI, diagnostic,
  roadmap, README, package, and changelog documentation

Any additional file is recorded in the progress log before final submission.

# Geometry invariants

- The FoldScript/2D canvas/panel/seam/operation document is source; imported or
  baked Unity Meshes remain disposable derived artifacts.
- Source arrays retain their authored order. Import/export never sorts panels,
  seams, or operations because ordering is semantic.
- Units affect physical lengths only. UV rectangles, normalized fold lines,
  angles, counts, booleans, IDs, and validation levels are unchanged.
- Every physical length reaches the compiler in meters. Repeated export/import
  through the same declared units is stable within native float precision.
- Canonical JSON is byte-identical for identical DTO content and compiler
  version, independent of locale, OS line endings, dictionary iteration, or
  Editor selection.
- Unknown fields outside the explicit `extensions` object are errors. Unknown
  extension data is preserved canonically but never changes geometry.
- Path normalization occurs before asset resolution. The normalized appearance
  path must remain under the source document's approved `Assets/` or
  `Packages/` project root; traversal, absolute paths, URI schemes, and
  backslash ambiguity are rejected.
- Limits are checked before building native panel/seam/operation collections;
  compiler geometry budgets still apply independently afterward.
- Repair responses have no direct access to Mesh buffers. They must parse,
  validate, convert, and compile as ordinary FoldScript.
- All import, conversion, diagnostic, and repair-payload ordering is
  deterministic.

# Implementation steps

1. Merge accepted M07, create the isolated M08 branch, read the milestone and
   relevant ADRs, and lock the no-provider/no-file-I/O Runtime boundary.
2. Inventory the current JSON Schema, sample documents, native source model,
   compiler diagnostics, M06 UI, and assembly references.
3. Add M08 limits/diagnostic codes and portable document metadata without
   changing geometry semantics.
4. Implement the bounded JSON value reader and stable syntax/limit errors.
5. Implement strict FoldScript `0.1` DTO decoding and semantic validation for
   every supported panel, seam, operation, and compile field.
6. Implement canonical DTO serialization and deterministic extension handling.
7. Implement explicit unit-aware DTO/native converters and resolver-based
   appearance binding.
8. Implement Editor project-path normalization and M06 import/export actions.
9. Implement immutable provider-neutral repair contracts, diagnostic payload
   serialization, and corrected-response application through the normal gates.
10. Add canonical, hostile-input, path, round-trip, unit, geometry, repair-loop,
    immutability, and Editor integration tests.
11. Update Schema/documentation/version/changelog and record compatibility and
    migration rules.
12. Run JSON parsing, assembly/runtime isolation, repository validation,
    `git diff --check`, targeted M08 tests, the complete Edit Mode suite, and a
    live Editor cup import/export/reimport/preview proof.
13. Commit, push, and open a non-auto-merged M08 review PR with exact evidence.

# Test matrix

## Canonical import/export

- `CupFoldScript_CanonicalRoundTrip_PreservesSemanticSource`
- `CanonicalExport_RepeatedCallsAreByteIdentical`
- `CanonicalExport_IsInvariantAcrossCulture`
- `Import_PreservesPanelSeamAndOperationOrder`
- `ImportExport_PreservesAssetMetadataAndExtensions`
- `CentimeterDocument_ConvertsPhysicalLengthsToMetersAndBack`
- `ImportedCup_CompilesToEquivalentGeometry`
- `BundledCupSample_ImportsAndCompiles`
- `ImportedSphere_CompilesWithFreshSphereAndStrictReports`

## Untrusted input

- `MalformedJson_ReturnsStableDiagnosticWithoutThrow`
- `DuplicateJsonProperty_ReturnsStableDiagnostic`
- `UnknownSchemaVersion_ReturnsStableDiagnostic`
- `UnknownOperation_ReturnsStableDiagnostic`
- `NaNAndInfinity_ReturnStableNonFiniteDiagnostic`
- `OversizedJson_IsRejectedBeforeParsing`
- `ExcessivePanels_IsRejectedBeforeNativeAllocation`
- `ExcessiveOperations_IsRejectedBeforeNativeAllocation`
- `ExcessiveDepthAndNodeCount_ReturnStableDiagnostics`
- `MissingRequiredProperty_ReturnsLocalizedDiagnostic`
- `UnknownPropertyOutsideExtensions_ReturnsStableDiagnostic`
- `DuplicatePanelSeamAndOperationIds_ReturnStableDiagnostics`
- `MissingReferences_ReturnStableDiagnosticsWithoutThrow`

## Appearance safety and Editor conversion

- `RelativeAppearancePath_ResolvesInsideSourceFolder`
- `AssetsAndPackagesPaths_AreAcceptedInsideApprovedRoots`
- `PathTraversal_ReturnsUnsafeAppearancePath`
- `AbsoluteAndUriPaths_ReturnUnsafeAppearancePath`
- `MissingAppearance_ReturnsStableResolutionDiagnostic`
- `EditorImport_CreatesEditableSourceAndPreservesAppearance`
- `EditorImport_OverwriteReusesExplicitSourceAsset`
- `EditorExport_WritesCanonicalFoldScript`

## Repair loop

- `RepairRequest_ContainsStableCompactDiagnosticsWithoutMesh`
- `RepairRequest_CollectionsAreReadOnly`
- `RepairRequest_NonCanonicalOverrideCannotReplaceDocument`
- `InvalidRepairResponse_ReentersImporterAndIsRejected`
- `CorrectedRepairResponse_ClearsTargetDiagnosticAndCompiles`
- `RepairPayload_IsDeterministicAcrossRepeatedCompiles`
- `RepairPayload_InvalidNativeSourceReturnsStableDiagnostic`
- `ProviderContracts_DoNotReferenceProviderOrNetworkAssemblies`

## Regression

- all existing M00-M07 tests remain enabled and unchanged;
- Runtime contains no `UnityEditor` reference and package dependencies remain
  empty;
- valid native assets compile exactly as before.

# Risks and rollback

- **JSON parser surface:** keep it deliberately small, bounded, strict, and
  covered by hostile syntax/depth/size tests; it is a data parser, not a general
  JSON framework.
- **Float canonicalization:** use invariant round-trip formatting, normalize
  negative zero, and test locale independence plus repeated export.
- **Schema/runtime drift:** centralize literal limits in `FoldCanvasLimits` and
  extend repository validation to compare executable limits with Schema.
- **Unit mistakes:** isolate scale conversion and assert cup geometry equivalence
  across meter/centimeter documents.
- **Path escape:** normalize separators and dot segments before resolution,
  reject rooted/URI paths, and verify the normalized project-relative result
  remains under the approved source root.
- **Asset mutation on failed import:** build and validate a detached temporary
  source first; only the Editor layer replaces/persists an asset after success.
- **Repair bypass:** expose no direct compiler-buffer mutation and run every
  response through the same importer/converter/compiler.
- Rollback is reverting the isolated M08 commits. M07 `main` remains intact;
  user-owned untracked Unity scenes/results are not touched.

# Progress log

- 2026-08-03: User explicitly approved PR #6 and authorized merging plus M08.
- 2026-08-03: Merged PR #6 into `main` as `9ca0d68`, fast-forwarded local
  `main`, and created `codex/m08-foldscript-ai` while preserving all user-owned
  untracked Unity scenes and test evidence.
- 2026-08-03: Read M08, architecture, roadmap, Schema, source model, compiler,
  M06 workspace, AI boundary, and ADRs 0002/0003/0005/0006. Chose a bounded
  in-package JSON reader instead of adding a package dependency.
- 2026-08-03: Implemented explicit FoldScript DTOs, bounded JSON parsing,
  strict `0.1` decoding/reference validation, canonical serialization,
  unit-aware native conversion, resolver-only Runtime appearance binding, and
  `FC7001`-`FC7012` diagnostics.
- 2026-08-03: Added safe Editor project-path resolution, explicit import/export
  persistence, M06 toolbar actions, Diagnostics repair-payload copy, and
  provider-neutral immutable proposal/repair contracts with no SDK or network.
- 2026-08-03: Added 45 M08 tests. A live import exposed stale handedness in the
  historical bundled cup JSON; corrected it to the current verified
  `startAngleDegrees=180` and `reverseB=false` contract and added a regression.
- 2026-08-03: Unity `6000.3.20f1` passed 45/45 targeted and 326/326 complete
  Edit Mode tests with zero failures, skips, or inconclusive results.
- 2026-08-03: In the live M06 workspace, imported the bundled cup into
  `M08ImportedCup`, compiled a Strict 2972-vertex/5120-triangle preview,
  exported `M08RoundTrip.foldcanvas.json`, reimported it into `M08RoundTrip`,
  and copied/parsed a repair payload with no Mesh key.

# Decisions made

- FoldScript DTOs are explicit public source types. Unity's internal serialized
  object layout is never the interchange format.
- Runtime parses strings and converts data but performs no file-system or
  `AssetDatabase` work. Appearance binding is a resolver contract implemented
  by the Editor layer.
- Canonical JSON property order is fixed by the writer. Only extension-object
  keys are ordinal-sorted; semantic source arrays retain authored order.
- FoldCanvas native geometry remains meter-based. Document units are converted
  at the DTO/native boundary.
- Provider-neutral interfaces and payloads live in the Runtime assembly but do
  not execute network requests or depend on any provider SDK.
- A repair is accepted only when its replacement FoldScript passes ordinary
  import validation and compilation. M08 does not mutate a failed document in
  place or guess a repair.

# Final verification

- Unity Editor: `6000.3.20f1` (`c9ba695d4f07`).
- Targeted M08: 45 passed, 0 failed, 0 skipped, 0 inconclusive;
  `/tmp/foldcanvas-m08-targeted-final2.xml` and
  `/tmp/foldcanvas-m08-targeted-final2.log`.
- Complete Edit Mode: 326 passed, 0 failed, 0 skipped, 0 inconclusive;
  `/tmp/foldcanvas-m08-full-final2.xml` and
  `/tmp/foldcanvas-m08-full-final2.log`.
- Repository validation, all tracked/project JSON parsing, Runtime no-
  `UnityEditor` isolation, and `git diff --check`: passed locally.
- Live M06 import/export/reimport/preview and repair-payload proof: passed;
  derived proof assets remain under ignored `Project~/Assets/FoldCanvasSamples`.
- GitHub Actions and uploaded hosted artifacts: pending the review PR.
- No provider integration, authentication, network transport, automatic
  repair, binary Mesh import, M09, Bevel, subdivision, smoothing, remesh, or
  Mesh cleanup was implemented.
