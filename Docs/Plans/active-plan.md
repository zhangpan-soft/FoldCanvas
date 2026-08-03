# Goal

Deliver M12 on `codex/m12-production-handoff`: export one authoritative
FoldCanvas production asset into a deterministic, bounded source-first handoff
archive and prove that a separate clean Unity project can validate it, rebuild
matching runtime assets, retain source ownership, and reject unsafe or changed
input without partial writes.

# User-visible proof

From the FoldCanvas authoring workspace, export the production cup to one
`.foldcanvas.zip`. Hand that file to a clean receiver project and import it into
an explicit empty `Assets/` folder. The receiver shows the editable FoldCanvas
source and PNG beside a generated Mesh, one-sided textured Material, Prefab,
and receipt. Deleting the derived outputs and choosing Rebuild recreates the
same geometry/OBJ evidence from source. A tampered copy is rejected before any
folder is created.

# Scope

- deterministic fixed-layout handoff ZIP writer and bounded reader
- versioned canonical manifest plus JSON Schema
- canonical FoldScript and exact PNG source packaging
- deterministic OBJ, compile/validation evidence, and rebuild instructions
- exact package/compiler/FoldScript compatibility gate for v1
- detached in-memory verification before Unity project persistence
- source asset, texture, Mesh, Material, Prefab, and receipt creation
- receipt-owned, source-driven rebuild and same-bundle idempotency
- Authoring-window and menu export/import/rebuild entry points
- producer/receiver clean-project scripts, hosted tests, and artifacts
- package version, public Runtime diagnostic baseline, docs, and changelog

# Non-goals

- Mesh, OBJ, Prefab, receipt, or archive evidence as editable source
- UnityPackage export, AssetDatabase GUID transfer, or producer scene transfer
- arbitrary archive layouts, multiple canvases, or non-PNG appearance formats
- native custom-operation definition/binary bundling
- package/compiler/FoldScript migration or compatibility guessing
- signing, encryption, DRM, cloud storage, or network transport
- runtime file I/O, runtime authoring, or runtime asset persistence
- glTF, FBX, USD, Blender, marketplace, or DCC integration
- new panel/operation/topology behavior, CSG, bevel, subdivision, smoothing,
  remesh, or cleanup
- M13 robustness/scale implementation, M14 release decision, or `1.0.0`

# Files expected to change

- `CURRENT_TASK.md`, `Codex/M12_PRODUCTION_HANDOFF.md`
- `Docs/ADR/0010-source-first-handoff-archive.md`
- `Docs/Plans/active-plan.md`, roadmap, architecture, governance, compatibility,
  field/pipeline/diagnostics, and Editor workflow documentation
- Runtime diagnostic codes only; no Runtime geometry or I/O implementation
- Editor handoff manifest, evidence, archive, export, import, persistence,
  rebuild, and UI adapters
- `Schema/foldcanvas-handoff.schema.json`
- M12 Edit Mode fixtures and tests
- clean producer/receiver templates and repository comparison scripts
- GitHub Actions workflows and mandatory handoff artifacts
- public Runtime API and production-corpus baselines only as required by the
  reviewed diagnostic/version additions
- package version, `CHANGELOG.md`, English/Chinese README

# Geometry invariants

- The PNG appearance and canonical FoldScript are the only authoritative bundle
  entries. Every Unity Mesh, Material, Prefab, OBJ, report, hash, and receipt is
  derived and replaceable.
- Export and import do not change panel equations, operation order, coordinates,
  winding, boundaries, topology IDs, source positions, UVs, provenance, or
  compiler tolerances.
- Producer and receiver compile identical canonical FoldScript, PNG dimensions,
  texture contract, validation level, package version, compiler version, and
  FoldScript version.
- Evidence hashes include complete ordered compiled vertices and triangle
  indices, not only Unity Mesh bounds or counts.
- Import never accepts archived OBJ as geometry. It recompiles source and
  compares its own deterministic OBJ text to the archive evidence.
- The receiver source asset retains the same logical asset ID and canonical
  source hash. Unity GUIDs and local asset paths are intentionally receiver-owned.
- Runtime remains free of `UnityEditor`, filesystem/network behavior, and new
  dependencies. Archive and AssetDatabase work remains in `FoldCanvas.Editor`.
- Invalid or unsupported handoff input returns stable diagnostics and creates no
  approximate Mesh or partially owned destination.

# Archive and persistence decisions

- v1 archive entry order is exactly:
  `manifest.json`, `source.foldcanvas.json`, `appearance.png`,
  `derived/model.obj`, `evidence/compile-report.json`, `README.md`.
- ZIP entries use no compression, a fixed DOS-compatible timestamp, normalized
  UTF-8/LF text, no directories, and ordinal names.
- `manifest.json` hashes every payload entry; the result object and hosted
  evidence record the final archive hash separately to avoid self-reference.
- The reader rejects extra, missing, duplicate, directory, link, absolute,
  backslash, empty, dot, or traversal entries and enforces count/per-entry/total
  byte limits before allocation.
- v1 requires an exact package version, compiler version, FoldScript `0.1`, and
  PNG canvas dimensions. It does not silently test or guess migration.
- Import verifies/decompresses into bounded memory, decodes a temporary PNG,
  imports detached canonical source through an in-memory appearance resolver,
  compiles, exports OBJ, and compares all evidence before `Assets/` writes.
- A successful import owns one new explicit destination folder. If persistence
  fails, rollback deletes only that newly created folder.
- An intact receipt with the same archive/source/evidence hashes makes repeated
  import a no-write success. Any mismatch or unowned occupant fails.
- Rebuild reads the receiver `FoldCanvasAsset` and receipt, recompiles, compares
  source and evidence identity, then updates only receipt-owned derived assets.

# Implementation steps

1. Close M11 with exact PR/run/artifact evidence; create the M12 branch, task,
   ADR 0010, specification, roadmap entry, and this plan.
2. Define bounded manifest/evidence/receipt DTOs, canonical serializers, limits,
   export/import/rebuild results, and FC9301-FC9312 diagnostics.
3. Implement deterministic fixed-entry ZIP writing and untrusted archive reading
   with complete pre-allocation path/size/duplicate/link validation.
4. Implement exporter conversion, exact PNG acquisition, source/geometry/OBJ/
   diagnostic/validation evidence, README generation, temp-file validation, and
   atomic destination replacement.
5. Implement detached importer verification using PNG bytes and canonical
   FoldScript, exact compatibility/evidence gates, and no-persistence failures.
6. Implement receiver persistence and rollback: source JSON/PNG/import settings,
   editable asset, Mesh, one-sided textured Material, Prefab, and JSON receipt.
7. Implement receipt-owned idempotent import and source-driven rebuild without
   using archived OBJ or existing generated topology as compiler input.
8. Add Authoring workspace buttons and explicit menu/file-panel paths with clear
   diagnostics, ownership, and overwrite behavior.
9. Add Edit Mode security, determinism, integration, ownership, texture,
   Prefab, rebuild, and rollback tests; retain all existing tests.
10. Add clean producer/receiver project fixtures, comparison/normalization
    scripts, hosted Unity jobs, and mandatory XML/log/archive/source/receipt/
    evidence artifacts.
11. Update architecture, field/pipeline/diagnostic/Editor/compatibility docs,
    README files, schemas, public API baseline, corpus version fields, package
    version, and newest-first changelog.
12. Run JSON/asmdef/runtime-isolation checks, focused tests, full Edit Mode
    tests, deterministic archive/package tests, two clean-project handoffs, and
    foreground imported-Prefab/source ownership proof.
13. Commit/push, open a PR, audit exact hosted artifacts and review threads,
    record a transparent maintainer audit, and merge only with no blocker.

# Test matrix

## Export and archive determinism

- production cup exports twice to byte-identical archive SHA-256
- canonical manifest and fixed ZIP entry order/timestamps/methods are stable
- canonical source uses `appearance.png` and retains all authored semantics
- PNG bytes and recorded texture contract match the source asset
- complete compiled-data/OBJ/diagnostic/validation evidence is stable
- export does not mutate source, texture importer, scene, selection, or Mesh
- invalid compile, missing/unsupported appearance, and custom operations create
  no final archive and preserve any prior valid destination

## Untrusted import

- missing archive, invalid ZIP, unsupported manifest version, and incompatible
  package/compiler/FoldScript versions return one stable primary diagnostic
- missing, extra, duplicate, directory, link, absolute, backslash, dot, and
  traversal entries fail before project writes
- archive, entry, and expanded byte limits fail before unbounded allocation
- changed entry bytes/length/hash and noncanonical FoldScript fail integrity
- PNG decode/dimension mismatch fails before AssetDatabase mutation
- producer/receiver source, geometry, OBJ, diagnostics, validation, and closed
  volume evidence must match exactly

## Receiver ownership and rebuild

- successful import creates exact documented source and derived assets
- source asset references the receiver PNG with recorded sampler settings
- Mesh and Prefab topology/UV evidence match the producer
- Material is one-sided, textured, and referenced by the Prefab
- receipt records logical ID, archive/source/appearance/geometry/OBJ hashes,
  versions, and exact owned paths
- same archive into intact folder is idempotent with stable GUID/object count
- changed archive, damaged owned folder, and unowned destination never overwrite
- simulated persistence failure removes the new folder and nothing else
- deleting receipt-owned derived assets then rebuilding from source recreates
  matching evidence and leaves source unchanged

## Hosted producer/receiver

- both projects resolve the same freshly built `.tgz` as `LocalTarball` under
  their own `Library/PackageCache`
- producer emits real XML, Editor log, handoff archive, and producer evidence
- receiver starts without producer Assets, imports only the archive, emits real
  XML/log/source/receipt/evidence, and builds a runtime-ready Prefab
- repository comparison proves archive/package/source/geometry/OBJ/diagnostic/
  validation identity and distinct project paths
- missing evidence or Unity startup fails the workflow

## Regression

- all 401 M00-M11 package tests remain enabled
- Runtime has no Editor, archive, filesystem, or network implementation
- release package remains byte-reproducible
- public Runtime API additions are limited to reviewed diagnostics/version data
- M11 production corpus retains all six expected cases and hashes unless the
  package-version field requires a reviewed baseline refresh
- user-owned untracked `Project~` scenes and historical results remain untouched

# Risks and rollback

- **ZIP bomb/path escape:** fixed six-entry allowlist plus central-directory
  count, link/type, compressed/uncompressed, per-entry, total, and normalized
  path gates run before reading payloads.
- **Partial Unity writes:** validate and compile in memory first; persist only to
  a nonexistent folder; delete only that newly owned folder on failure.
- **Silent overwrite:** immutable receipt ownership and exact content hashes;
  same bundle is idempotent, everything else requires a new destination.
- **Archive nondeterminism:** no compression, fixed metadata, ordinal entry
  order, normalized bytes, and cross-platform hosted SHA comparison.
- **Texture drift:** copy original PNG bytes and record/import the sampler values
  that materially affect the one-sided reference result.
- **Compiler drift:** v1 exact-version gate plus full evidence comparison; later
  migration needs a new format/version decision.
- **Native extension ambiguity:** reject custom operations with FC9306 rather
  than losing contributor source or silently baking only their Mesh result.
- **Prefab becomes source:** receipt and docs label it derived; rebuild requires
  the FoldCanvas source and does not inspect the existing Mesh.
- Rollback is reverting isolated M12 commits. M11 remains merged at `b757792`.

# Progress log

- 2026-08-03: Audited M11 exact head `e1204ff`; hosted repository run
  `30812427551` and Unity run `30812427595` were green.
- 2026-08-03: Downloaded and audited both hosted artifacts. Full XML was
  401/401; clean hosts were 1/1 each; archive SHA, corpus baseline, geometry,
  OBJ, diagnostics, and pair evidence matched local evidence.
- 2026-08-03: Recorded the transparent maintainer self-audit, marked PR #10
  ready, merged it as `b757792`, and fast-forwarded local `main`.
- 2026-08-03: Created `codex/m12-production-handoff`, ADR 0010, the M12
  specification, roadmap contract, and this execution plan.

# Decisions made

- Production handoff transfers source ownership and reproducibility, not a
  frozen Unity Mesh or producer project identity.
- A deterministic fixed-layout ZIP is the smallest reviewable one-file handoff;
  UnityPackage/GUID semantics would obscure source and project ownership.
- Exact PNG bytes are portable v1 appearance source. Other texture formats and
  multi-canvas assets need later versioned contracts.
- Import must prove evidence before persistence. Cleanup after blindly writing
  an untrusted archive is not an acceptable security boundary.
- Exact package/compiler/FoldScript equality is required in v1. Upgrade
  migration is explicit future work, not a warning that continues silently.
- Receiver Mesh/Material/Prefab assets are useful runtime outputs but remain
  disposable and rebuildable from the received FoldCanvas source.

# Final verification

Planning is complete. Required implementation evidence:

- exact M12 head, package version, archive format, and deterministic SHA
- focused M12 and full package Edit Mode totals under Unity `6000.3.20f1`
- producer and receiver clean-project test totals and distinct resolution paths
- archive entry list/method/timestamp/byte/hash audit
- producer/receiver source, appearance, geometry, OBJ, diagnostic, validation,
  closed-volume, Prefab, Material, and receipt evidence comparison
- tamper/traversal/limit/collision/rollback tests with no partial output
- foreground receiver proof showing editable source and rebuilt runtime Prefab
- repository/release/static results and hosted run/artifact IDs
- explicit statement that M13/M14 and later geometry were not implemented
