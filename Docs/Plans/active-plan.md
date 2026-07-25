# Goal

Complete M00 bootstrap and repository-health acceptance without implementing any
later FoldCanvas operation.

# User-visible proof

- The root is a valid `com.foldcanvas.core` UPM package and `Project~` resolves it
  through a local file dependency.
- Unity 6.3 LTS can compile the runtime, editor, and test assemblies when a
  compatible Editor is available.
- Bootstrap Edit Mode tests prove rectangle and disk compilation, UV retention,
  target-only rigid transforms, stable diagnostics, idempotent sample creation,
  and GUID-preserving mesh rebakes.
- The editor menu can create the bootstrap sample and bake under
  `Assets/FoldCanvasGenerated`.

# Scope

- Inspect all package metadata, JSON, asmdefs, C# source, tests, schemas,
  documentation links, ignore rules, and the local host project.
- Repair only defects required by M00 acceptance.
- Add or tighten Edit Mode tests where an M00 acceptance criterion is not
  covered.
- Run the strongest available static and Unity validation.

# Non-goals

- Fold, Roll, Stitch, or Solidify geometry.
- FoldScript import.
- A 3D preview workspace.
- Runtime generation optimization, render-pipeline integration, networking, or
  AI-provider integration.
- Broad API or repository refactors.

# Files expected to change

- `Docs/Plans/active-plan.md`
- Existing M00 runtime, editor, test, package, project, schema, or documentation
  files only when inspection exposes an acceptance-blocking defect.
- New Edit Mode test files only when needed to cover M00 behavior.

# Geometry invariants

- Source panels begin in local XY space, centered on the origin, with front
  normal `+Z`.
- Triangle winding is deterministic and faces `+Z` before an explicit
  transform.
- UV0 is copied from the normalized source canvas rect with lower-left origin;
  operations must not rewrite UV order or values.
- Rectangle boundaries remain ordered as `uMin`/`uMax` bottom-to-top and
  `vMin`/`vMax` left-to-right. Disk perimeter remains counter-clockwise when
  viewed from the front.
- Vertex, triangle, UV, panel, operation, and diagnostic ordering is stable for
  identical serialized input.
- M00 introduces no welding. The existing default `weldEpsilon` of `1e-5`
  remains configuration-only in this milestone; geometric validity uses the
  compiler's documented finite-value and minimum-area checks.

# Implementation steps

1. Map every M00 acceptance criterion to the current implementation and tests.
2. Parse all JSON and validate UPM/package/project references, asmdefs, ignore
   rules, and documentation links.
3. Inspect every C# file for serialization, accessibility, deterministic
   ordering, and Editor/Runtime boundary defects.
4. Make the smallest repairs required for package recognition, compilation,
   diagnostics, sample idempotence, and GUID-preserving baking.
5. Add focused Edit Mode coverage for any unproven M00 acceptance criterion.
6. Run static checks, then Unity Edit Mode tests and editor/bake verification if
   a compatible Unity Editor is installed.
7. Record exact results and leave `CURRENT_TASK.md` on M00 unless every
   acceptance criterion is actually proven.

# Test matrix

| Acceptance area | Automated evidence |
| --- | --- |
| Package identity and local resolution | JSON parse plus package/manifest path checks |
| Runtime isolation | asmdef inspection and search for `UnityEditor` references |
| Rectangle and disk meshes | vertex/index/UV/winding Edit Mode assertions |
| Target-only rigid transform | two-panel before/after coordinate and UV assertions |
| Stable diagnostics | duplicate IDs, invalid canvas rect, unsupported operations, and repeated-compile ordering assertions |
| Sample idempotence | invoke creator twice and compare asset/object counts |
| GUID-preserving rebake | bake twice and compare `.meta` GUID |
| Repository hygiene | ignored/generated/cache path scan |
| Assembly health | Unity compilation and Edit Mode suite |

# Risks and rollback

- Unity may be absent or a different version may be installed. Static checks
  will still run, and unexecuted Unity proof will be reported explicitly.
- Editor tests create assets in the disposable host project. Tests must clean up
  only their own explicit paths.
- AssetDatabase behavior is stateful. Repairs will preserve existing public
  paths and update assets in place; no generated artifact will be committed.
- Any unexpected need for a dependency or architecture change stops
  implementation and requires an ADR plus explicit authorization.

# Progress log

- 2026-07-25: Read `CURRENT_TASK.md`, `PLANS.md`,
  `Documentation~/architecture.md`, `Codex/M00_BOOTSTRAP.md`, and ADRs
  0001-0006.
- 2026-07-25: Confirmed M00 is active and later milestones are out of scope.
- 2026-07-25: Began repository and implementation audit. No Git metadata was
  present at the workspace root during the initial probe.
- 2026-07-25: Unity `6000.3.20f1` compiled all three assemblies and passed the
  original 6/6 Edit Mode tests.
- 2026-07-25: Added M00 coverage for deterministic geometry, front winding, UV
  retention, target-only transforms, all deferred operation types, stable
  diagnostic ordering, sample idempotence, and GUID-preserving rebakes.
- 2026-07-25: Removed the invalid `Samples~.meta`, repaired the future sample
  appearance reference, aligned diagnostic documentation, and strengthened
  repository/GitHub metadata validation.
- 2026-07-25: Final Unity batch run passed 13/13 Edit Mode tests with no C#
  compile errors or package-metadata warning.
- 2026-07-25: Added the `0.1.0-preview.2` changelog entry and initialized the
  local `main` Git repository after the user explicitly requested GitHub
  publication.
- 2026-07-25: After explicit acceptance of Unity's updated software terms, the
  real editor created the bootstrap sample twice, compiled it in memory, and
  baked it twice with stable asset GUIDs.
- 2026-07-25: M00 acceptance passed and `CURRENT_TASK.md` was deliberately
  advanced to the M01 entry point without implementing M01 behavior.

# Decisions made

- Treat existing rectangle, disk, and rigid-transform code as bootstrap scope
  because M00 explicitly requires those behaviors.
- Git was initially left untouched, then initialized only after the user
  explicitly requested GitHub publication.
- Do not advance `CURRENT_TASK.md` until Unity-dependent acceptance is proven.

# Final verification

- `python3 Scripts/validate_repository.py`: passed after parsing repository JSON,
  checking UPM identity/local resolution, asmdef boundaries, Markdown links,
  ignore rules, sample references, and GitHub Issue Form fields.
- All `.github/**/*.yml` files parsed successfully with the local YAML parser.
- Unity `6000.3.20f1` final Edit Mode run: 13 total, 13 passed, 0 failed,
  0 skipped.
- The editor-workflow tests executed sample creation twice without extra assets
  or GameObjects and baked the same mesh path twice without changing its GUID.
- The host project's `Library`, `Logs`, `UserSettings`, and package lock are
  ignored; menu-generated sample and mesh artifacts are also excluded from
  source control.
- In the real Unity GUI, `Tools > FoldCanvas > Create Bootstrap Sample` ran
  twice with the same asset/texture GUIDs. `Window > FoldCanvas > FoldCanvas`
  compiled successfully with 192 vertices and 320 triangles and reported no
  diagnostics.
- The real GUI baked `BootstrapFoldCanvas_Generated.asset` twice while its GUID
  remained `621cf59899f7448d98c418004e4f3609`.
- Fold, Roll, Stitch, Solidify, FoldScript import, and 3D preview behavior were
  not implemented.
