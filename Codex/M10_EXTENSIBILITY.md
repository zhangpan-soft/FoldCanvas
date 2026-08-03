# M10: Extensibility and ecosystem

## Visible proof

1. A contributor-defined operation in a separate assembly deforms an ordinary
   rectangular source panel through an explicit per-compile registry.
2. The same source fails with one stable diagnostic when the registry is
   omitted; the compiler never silently skips the operation.
3. A package gallery lists the maintained samples from one versioned manifest.
4. A compiled proof exports to byte-stable OBJ text and the release script
   creates a reproducible UPM archive.
5. Editor performance evidence reports the maintained scenarios without
   becoming part of deterministic geometry output.

## Goal

Make FoldCanvas practical for contributors and package releases while
preserving the source/compiler/derived-artifact architecture proven by M00-M09.

## Operation extension contract

- Registration is explicit and scoped to one call to
  `FoldCanvasCompiler.Compile`; there is no mutable global registry, reflection
  scan, scene discovery, or execution-order dependence on registration order.
- M10 custom operations are native `SerializeReference` definitions whose
  `Type` is `Custom` and whose exact CLR definition type is registered once
  under a stable reverse-domain operation type ID.
- A registration declares one target panel during preflight. The target must
  exist and is frozen into the compile plan before tessellation.
- The public execution context exposes the current panel vertices read-only
  except for finite position replacement. It does not expose triangle,
  topology, boundary, budget, UV, source-position, or provenance mutation.
- Failed, invalid, or throwing extension execution rolls back every position
  change and returns stable diagnostics. It never returns a partial Mesh.
- Registered position deformation follows the existing terminal-Stitch rule.
- The default `Compile(asset)` path remains unchanged for all M00-M09 assets.
- FoldScript `0.1` continues to reject unknown operation types. Portable custom
  operation codecs require a future versioned interchange design.

## Gallery and contributor template

- Add a bounded `foldcanvas-gallery` manifest version `1` with stable entry
  IDs, localized-neutral titles/descriptions, package-relative sample paths,
  optional FoldScript/appearance paths, proof menu paths, tags, and minimum
  package versions.
- Add Runtime parsing/validation with deterministic entry and diagnostic order,
  plus a JSON Schema and one canonical package manifest.
- Add an Editor gallery window that reads the package manifest and invokes only
  explicitly declared FoldCanvas proof menu items.
- Add a sample contributor operation template showing definition, executor,
  explicit registry construction, compile, diagnostics, and source/derived
  ownership.

## Export

- Add a deterministic OBJ text exporter over immutable
  `FoldCanvasCompiledData`.
- Emit one `v` and one `vt` for each render vertex and ordered `f` records from
  the compiled triangle index buffer.
- Use invariant round-trip numeric formatting, deterministic line endings, and
  a sanitized object name.
- Preserve source UV seams as distinct OBJ indices. Export never rewrites the
  source asset, compiled data, or Unity Mesh.

## Performance and release

- Add maintained Editor performance scenarios with warmup, measured iteration
  count, expected vertex/triangle counts, and generous regression ceilings.
- Timing and allocation numbers are derived reports; they never feed geometry
  or tests that require exact machine speed.
- Add a deterministic release builder that packages the UPM surface under a
  `package/` root, normalizes archive metadata, writes SHA-256 evidence, and
  validates package/runtime/changelog version agreement.
- Add a GitHub Actions workflow that validates and uploads a release archive on
  manual runs, and creates a GitHub release only for an exact matching `vX.Y.Z`
  tag. Credentials remain GitHub-managed secrets.

## Tests

- default compilation remains identical without a registry
- registered custom operation executes and preserves UV/topology/provenance
- registration order does not alter descriptors or compiled output
- missing, duplicate, invalid, failed, non-finite, and throwing extensions
  return stable diagnostics and roll back
- post-Stitch custom deformation is rejected
- gallery manifest parses, rejects malformed/duplicate/unsafe entries, and is
  deterministic
- contributor fixture compiles through the public API
- OBJ output is culture-independent, deterministic, correctly indexed, and
  preserves UV seams
- release archives are byte-stable and contain only the approved package
  surface
- all existing M00-M09 Edit Mode tests remain enabled and unchanged

## Non-goals

- extension access to triangle, topology, seam, boundary, or budget mutation
- reflection or assembly auto-discovery
- a global service locator or singleton registry
- custom FoldScript operation codecs in schema version `0.1`
- arbitrary sweep, CSG, panel-interior holes, bevel, remesh, or new geometry
- glTF, FBX, USD, Blender integration, or runtime filesystem/network export
- publishing an unreviewed tag or automatically merging the M10 PR
