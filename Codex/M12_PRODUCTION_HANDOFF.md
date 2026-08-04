# M12: Production asset handoff

## Visible proof

1. Export the production cup twice from authoritative FoldCanvas source into a
   byte-identical `.foldcanvas.zip` archive.
2. Inspect a fixed, human-readable archive layout containing canonical
   FoldScript, the exact PNG canvas, derived OBJ, compile/validation evidence,
   and rebuild instructions.
3. Transfer that archive to a second clean Unity `6000.3.20f1` project that
   installs the FoldCanvas release archive rather than the repository package.
4. Validate every handoff byte before project persistence, recompile the source,
   and require source/geometry/OBJ/diagnostic evidence to match the producer.
5. Create an editable FoldCanvas source asset plus derived Mesh, one-sided
   textured Material, runtime Prefab, and ownership receipt under one explicit
   new `Assets/` folder.
6. Delete the derived receiver outputs and rebuild them from the imported 2D
   source without reading the producer project or editing a Mesh.
7. Reject tampering, traversal, unsupported versions, incompatible compiler
   versions, missing files, limits, and occupied destinations with stable
   diagnostics and no partial imported folder.

## Goal

Make a FoldCanvas asset transferable between real Unity projects while keeping
the product's source-first ownership model intact. A receiver must be able to
audit what was handed over, reproduce the producer's result, ship a normal
Prefab, and later rebuild it from the same 2D canvas and construction program.

## Handoff archive v1

The archive is a ZIP written in fixed ordinal entry order, without compression,
with fixed timestamps and normalized UTF-8/LF text. The only accepted entries
are:

```text
manifest.json
source.foldcanvas.json
appearance.png
derived/model.obj
evidence/compile-report.json
README.md
```

- `source.foldcanvas.json` and `appearance.png` are authoritative source.
- `manifest.json` identifies the format, exact package/compiler/FoldScript
  versions, logical asset identity, texture import contract, payload entries,
  byte lengths, and SHA-256 digests.
- `derived/model.obj` and `evidence/compile-report.json` are review and
  reproducibility evidence. They never become compiler input.
- `README.md` explains ownership, compatible versions, import outputs, and the
  exact source-first rebuild path.
- The archive SHA-256 is returned beside the archive; it is not self-recorded
  inside `manifest.json`.

Handoff v1 accepts one exact PNG appearance file and ordinary FoldScript `0.1`
operations. Native custom-operation assets are rejected because FoldScript
`0.1` cannot canonically encode their `SerializeReference` definitions or
contributor assembly dependency.

## Export contract

- Export accepts one explicit `FoldCanvasAsset` and external destination file.
- The source must have a real PNG asset under `Assets/` or an installed package.
- FoldScript is converted with a bundle-relative `appearance.png` reference and
  canonicalized before hashing.
- Compilation must succeed at the source validation level; derived OBJ must
  export successfully.
- Evidence covers source, PNG, complete compiled data, OBJ, ordered diagnostics,
  validation level/counts, topology counts, and closed-volume state when
  present.
- Export writes a temporary file and atomically replaces the final destination
  only after the complete archive is valid.
- Repeated export of identical source/package inputs is byte-identical and does
  not mutate the source asset, appearance importer, scene, selection, or Mesh.

## Import contract

- Import treats the archive as untrusted and enforces archive bytes, entry
  count, per-entry bytes, total expanded bytes, decoded canvas pixels, exact
  names, ordinal uniqueness, no directories, no links, and no absolute/
  backslash/dot/traversal paths.
- It validates manifest structure, exact format/package/compiler/FoldScript
  compatibility, entry length/hash, canonical FoldScript, PNG decode/dimensions,
  and evidence structure before any `Assets/` write.
- It imports and compiles an in-memory detached source, recomputes geometry,
  OBJ, diagnostic, validation, and closed-volume evidence, and requires an exact
  match before persistence.
- A successful import owns one previously nonexistent explicit folder under
  `Assets/`. It writes source JSON/PNG, configures the recorded texture sampler,
  creates the editable source `.asset`, saves the derived Mesh and Material,
  builds a one-sided textured Prefab, and writes an immutable JSON receipt.
- Any persistence failure deletes only that newly created owned folder.
- Reimporting the exact archive into its intact owned folder is a no-write
  success. A different archive, missing owned output, or any unowned occupant
  returns a collision diagnostic and never overwrites.
- Rebuild consumes the imported source asset, never the archived OBJ or an
  existing generated Mesh, and refreshes only receipt-owned derived assets.

## Stable diagnostics

- `FC9301 HandoffInputMissing`
- `FC9302 UnsupportedHandoffVersion`
- `FC9303 InvalidHandoffManifest`
- `FC9304 UnsafeHandoffEntry`
- `FC9305 HandoffIntegrityMismatch`
- `FC9306 UnsupportedHandoffSource`
- `FC9307 HandoffCompileFailed`
- `FC9308 HandoffEvidenceMismatch`
- `FC9309 HandoffDestinationOccupied`
- `FC9310 HandoffAssetCreationFailed`
- `FC9311 HandoffCompatibilityMismatch`
- `FC9312 HandoffLimitExceeded`

One root cause produces one primary diagnostic. Messages may explain context;
machine-relevant expected/actual lengths, versions, entry roles, and hashes are
structured deterministic fields where numeric representation applies.

## Hosted proof

- Build the exact deterministic UPM archive.
- Generate independent producer and receiver Unity projects.
- Producer imports the reviewed production cup source/PNG, exports the handoff,
  and uploads its XML, Editor log, archive, and producer evidence.
- Receiver starts without producer project assets, imports only that handoff,
  rebuilds all Unity outputs, runs tests, and uploads XML, Editor log, receipt,
  canonical source, and receiver evidence.
- A repository script compares producer/receiver evidence and verifies that
  archive, source, geometry, OBJ, diagnostics, texture, validation, and
  closed-volume claims agree.

## Implementation status

The Editor implementation now provides deterministic export, bounded detached
verification, receipt-owned import/rebuild, one-sided runtime outputs, schema,
production fixture, Authoring/menu entry points, and independent clean-project
CI. Unity `6000.3.20f1` passes 30/30 focused M12 tests and 431/431 complete
package tests. Final `.20` clean producer and receiver projects pass 1/1 each
with complete matching evidence, and a foreground receiver proof shows the
editable source beside its rebuilt closed runtime Prefab. PR #11 hosted jobs and
artifacts were audited for exact head `5edcc23`; M12 was merged into `main` as
`0d4a576` on 2026-08-04.

## Tests

- export twice is byte-identical and source/appearance state is unchanged
- manifest layout, payload order, byte lengths, and hashes are canonical
- production cup source/PNG/OBJ/evidence are complete
- invalid source or unsupported custom operation produces no archive
- valid import creates source, PNG, Mesh, Material, Prefab, and receipt
- imported Prefab uses the rebuilt Mesh and one-sided textured Material
- imported compile and OBJ evidence equal producer evidence
- deleting derived outputs and rebuilding from source recreates the evidence
- exact same-bundle import is idempotent
- changed bundle or unowned destination is never overwritten
- tampered, missing, duplicate, extra, oversized, or traversal entries fail
  before persistence
- incompatible format/package/compiler/FoldScript versions fail stably
- importer failure leaves no partial destination folder
- all 401 M00-M11 tests remain enabled alongside the M12 suite

## Non-goals

- treating OBJ, Mesh, Prefab, or receipt as editable geometry source
- UnityPackage export or cross-project GUID preservation
- native custom-operation serialization or contributor binary bundling
- version migration, forward-compatibility guessing, signing, encryption, or DRM
- glTF, FBX, USD, Blender, marketplace, or cloud handoff services
- runtime filesystem/network import or runtime authoring
- new geometry, bevel, smoothing, subdivision, remesh, or cleanup
- M13 fuzz/scale work, M14 release-candidate declaration, or `1.0.0`
