# Goal

Deliver M19 on `agent/m19-schema-field-coverage`: make schema-to-field-reference
drift a deterministic repository failure without changing FoldScript semantics
or stable package bytes.

# User-visible proof

The repository reports a reviewed count of scoped public FoldScript fields and
fails in sorted order when a schema field lacks documentation, documentation
names a stale field, or a field row is duplicated.

The validator expresses these as canonical scoped fields so reused names in
different operations never collapse into one undocumented ambiguity.

# Scope

- GitHub issue #20
- dependency-free schema public-field discovery
- scoped Markdown `Field` table parsing
- deterministic positive and negative temporary fixtures
- repository-check wiring and contributor documentation
- deterministic archive and hosted exact-head verification

# Non-goals

- changing schema fields or field semantics
- Runtime, Editor, Tests, geometry, topology, FoldScript decoder/compiler,
  source assets, diagnostics, tolerances, dependencies, or Unity Editor changes
- generated Markdown or automatic documentation rewriting
- issues #19, #21, #26, package release, registry, or marketplace publication

# Files expected to change

- `Scripts/validate_schema_field_reference.py`
- `Scripts/test_schema_field_reference.py`
- `Scripts/validate_repository.py`
- `.github/workflows/repository-checks.yml`
- `CURRENT_TASK.md`, `Codex/M19_SCHEMA_FIELD_COVERAGE.md`, and this plan

# Geometry invariants

- The 2D canvas plus FoldScript remain authoritative and generated Meshes
  remain derived.
- Geometry equations, operation order, coordinate systems, winding, boundary
  order, seam topology, source UVs, tolerances, diagnostics, and deterministic
  compiler output do not change.
- Package version remains `1.0.0`, FoldScript remains `0.1`, and Unity remains
  `6000.3.20f1` throughout M19.
- The rebuilt archive must remain exactly byte-identical to public stable
  `v1.0.0` with SHA-256
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

# Implementation steps

1. Define canonical scopes for top-level, canvas, panel, boundary, seam,
   operation, and compile fields.
2. Parse only Markdown tables whose first column is exactly `Field`, using the
   surrounding numbered section to determine scope.
3. Discover schema properties and operation/panel variants through local
   references while explicitly reviewing structural keywords.
4. Compare sets and duplicate occurrences in ordinal order without editing
   either source.
5. Add real-repository, missing, stale, duplicate, new-schema-field,
   structural-keyword, and multi-error-order fixtures under temporary folders.
6. Wire validator and tests into repository checks and static self-validation.
7. Run the complete local validation matrix and prove the stable archive hash
   remains unchanged.
8. Run exact-head hosted checks, record an exact-head audit, and merge only
   when required gates are green.

# Test matrix

## Coverage identity

- every public schema property maps to exactly one canonical scoped field
- every field-reference row maps back to an existing canonical field
- fields reused by different operations remain distinct by operation type
- panel-common fields are not duplicated for each panel shape

## Negative fixtures

- missing field, stale field, duplicate field row
- newly added operation property without a reference row
- unreviewed schema structural keyword
- multiple errors returned in stable sorted order

## Product regression

- the deterministic main archive SHA-256 equals public stable
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`
- the complete repository static matrix and required hosted checks remain green
- no Unity test rerun is needed for release-excluded Python-only tooling unless
  the required workflow filters trigger Unity independently

# Risks and rollback

- **False positives from reused names:** compare canonical scoped paths rather
  than bare field names.
- **False positives from Markdown diagrams:** parse only tables headed `Field`.
- **Schema feature blindness:** reject structural keywords not in the reviewed
  explicit allowlist.
- **Package-byte contamination:** verify the stable archive hash before merge.
- **User scratch contamination:** never stage untracked `Project~` scenes,
  results, or generated `.meta` files.

Rollback is one revert of the M19 integration commit. It removes only the
release-excluded gate and related documentation; no source migration is needed.

# Progress log

- 2026-08-12: verified M18 merge `b45b84e`, issue #25 closure, and green main
  repository, Unity, clean-install, handoff, upgrade, and M13 runs.
- 2026-08-12: selected issue #20 because schema/documentation drift directly
  impairs reliable human and AI FoldScript authoring; no external CLAIM exists.
- 2026-08-12: implemented scoped discovery for 72 current public fields and
  seven deterministic positive/negative cases. Initial focused tests pass.
- 2026-08-12: wired the gate into hosted repository checks and the repository's
  self-validation. The complete local static/release/maintenance matrix passes;
  the stable archive remains exactly
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

# Decisions made

- Canonical paths include operation and panel variants, so reused bare names do
  not collapse distinct semantics.
- Common operation fields are the intersection across all operation schemas;
  variant-only fields retain the operation type in their path.
- Only `Field` tables are contract rows. Boundary-name tables and convention
  tables remain prose evidence, not JSON field declarations.
- Structural-keyword review fails closed. The legacy `operationBaseProperties`
  property map is one explicit reviewed exception.
- The validator is repository tooling, not a Runtime or Unity dependency.
- The public schema, field reference, changelog, and every other package path
  stay untouched so M19 does not mutate the already published stable archive.

# Final verification

Focused validator and fixture tests, complete local validation, JSON/YAML/Python
checks, and exact stable-archive identity pass. Hosted checks, exact-head audit,
merge, and issue closure remain. No schema, compiler, geometry, or later
milestone behavior has been implemented.
