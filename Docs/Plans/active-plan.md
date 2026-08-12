# Goal

Deliver M20 on `agent/m20-roll-handedness-diagram`: make the implemented Roll
coordinate, sweep, winding, and texture-direction contract visually reviewable
without changing stable package bytes.

# User-visible proof

A bilingual review pairs one source rectangle with Roll-U and Roll-V positive
and negative cross-sections. Each claim is traceable to the compiler equation,
the executor, or a named existing Edit Mode test.

# Scope

- GitHub issue #19
- repository-native SVG and bilingual Markdown guide
- dependency-free deterministic validation and adversarial fixtures
- contributor entry-point link and repository-check wiring
- exact stable archive and hosted exact-head verification

# Non-goals

- Runtime, Editor, Tests, Schema, compiler, geometry, topology, diagnostics,
  tolerances, UV behavior, winding behavior, material implementation, Unity
  version, dependencies, package version, or release assets
- generated Meshes, raster screenshots, external fonts/assets, ImageGen, or
  any attempt to hide winding with two-sided rendering
- issues #21 or #26

# Files expected to change

- `Docs/Community/GeometryReviews/roll-handedness.svg`
- `Docs/Community/GeometryReviews/roll-handedness.md`
- `Scripts/validate_roll_handedness_review.py`
- `Scripts/test_roll_handedness_review.py`
- `Scripts/validate_repository.py`
- `.github/workflows/repository-checks.yml`
- `Docs/Community/START_HERE.md`
- `CURRENT_TASK.md`, `Codex/M19_SCHEMA_FIELD_COVERAGE.md`,
  `Codex/M20_ROLL_HANDEDNESS_DIAGRAM.md`, and this plan

# Geometry invariants

- `thetaDegrees = startAngleDegrees - t * angleDegrees`, with selected source
  coordinate `t` increasing from its minimum boundary to maximum boundary; the
  executor converts it to radians before evaluating `sin` and `cos`.
- Roll-U uses cylinder axis `CurrentV` and radial basis
  `(-CurrentU, CurrentNormal)`; Roll-V uses cylinder axis `CurrentU` and radial
  basis `(-CurrentV, CurrentNormal)`.
- For start angle zero, a positive quarter sweep maps the selected minimum
  radial direction to `-CurrentNormal`; a negative quarter sweep maps it to
  `+CurrentNormal`.
- The executor reverses every target triangle once. Positive full turns are
  radially outward; negative full turns are radially inward by the documented
  contract.
- UV0, source provenance, logical topology, and named-boundary order do not
  change during Roll. A two-sided material changes visibility only.
- Package version remains `1.0.0`, FoldScript remains `0.1`, and Unity remains
  `6000.3.20f1`.
- The rebuilt archive must remain byte-identical to public stable `v1.0.0` at
  SHA-256
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

# Implementation steps

1. Freeze the source-plane axes, boundary order, selected-coordinate mapping,
   signed sweep, and winding claims from maintained code and tests.
2. Draw the source rectangle, current-frame triad, Roll-U/Roll-V cross-sections,
   both signs, and canonical readable-exterior note as native SVG primitives.
3. Write a concise bilingual explanation and claim-to-evidence table.
4. Add a standard-library validator for XML safety, embedded convention
   metadata, required labels, and evidence mappings.
5. Add real-repository and adversarial in-memory fixtures with stable sorted
   errors; wire them into repository checks.
6. Render and visually inspect the SVG, then run the complete local validation
   matrix and stable archive comparison.
7. Run exact-head hosted checks, record an exact-head audit, and merge only
   when protected required gates are green.

# Test matrix

## Contract coverage

- source `CurrentU`, `CurrentV`, `CurrentNormal`, front face, and all four
  rectangle boundaries are present
- Roll-U and Roll-V identify the correct cylinder axis and selected coordinate
- positive and negative first-quarter directions match the equation
- positive/outward and negative/inward winding are not conflated with culling
- canonical positive Roll-U exterior reading direction is identified

## Safety and deterministic failures

- malformed XML, external raster/reference, executable SVG, missing sweep
  metadata, missing formula, and missing named-test evidence fail
- multiple failures are returned in ordinal sorted order
- validation inputs are never modified

## Product regression

- complete repository validation and required hosted checks stay green
- the deterministic main archive SHA-256 remains
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`
- no Unity rerun is required by the implementation itself because M20 changes
  only release-excluded documentation/tooling; protected CI remains authority

# Risks and rollback

- **Perspective ambiguity:** use orthographic cross-sections with named basis
  directions rather than a decorative 3D sketch.
- **Sign ambiguity:** embed machine-checked start and first-quarter metadata for
  each of four sweeps.
- **Texture/winding confusion:** give UV reading and triangle orientation
  separate legend rows.
- **Unsafe SVG content:** fail on image, foreignObject, script, event handlers,
  external hrefs, remote URLs, data URLs, and font-face rules.
- **Package-byte contamination:** rebuild and compare the stable archive before
  merge.
- **User scratch contamination:** never stage untracked `Project~` scenes,
  results, or generated `.meta` files.

Rollback is one revert of the M20 integration commit. It removes only
release-excluded documentation, validation tooling, and task records; no source
migration is needed.

# Progress log

- 2026-08-12: verified M19 merge `6fbdb3b`, issue #20 closure, green main
  repository/Unity runs, zero open PRs, and no external claims on issues #19,
  #21, or #26.
- 2026-08-12: selected #19 because handedness, outward winding, and readable
  texture direction directly address recurrent mirrored-artwork and hidden-face
  mistakes while preserving the public package byte-for-byte.
- 2026-08-12: derived both mappings from the field reference, pipeline,
  `RollExecutor`, and the named `RollCompilerTests` proofs.
- 2026-08-12: implemented the native SVG, bilingual evidence map, fail-closed
  validator, and fourteen deterministic fixtures; rendered and visually inspected
  the final `1600 x 1180` SVG.
- 2026-08-12: complete local repository, release, installation, upgrade,
  handoff, JSON, Python, link, and diff checks pass. The archive remains exactly
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

# Decisions made

- Use orthographic selected-axis/normal cross-sections; they express the exact
  equation more reliably than a perspective cylinder illustration.
- The canonical readable-text claim is limited to positive Roll-U with
  `startAngleDegrees = 180`, matching the existing executable proof.
- Negative Roll is shown as radial-inward because that is the implemented and
  documented contract; the diagram does not silently relabel it outward.
- The SVG uses only local vector primitives and generic system fonts.
- M20 is release-excluded and therefore does not create a CHANGELOG package
  entry or version bump.

# Final verification

Focused review validation (four signed sweeps and seven evidence claims), fourteen
adversarial cases, complete local repository/release/maintenance checks, native
SVG visual inspection, JSON/Python checks, link validation, `git diff --check`,
and exact stable-archive identity pass. Hosted checks, exact-head audit, merge,
and issue closure remain. No compiler, geometry, material, Unity-test, package,
or later milestone behavior has been implemented.
