# M19: FoldScript schema-to-field-reference coverage

## Visible proof

1. One dependency-free command compares every public FoldScript `0.1` JSON
   field with the matching programming field-reference row.
2. The real repository reports the exact number of scoped public fields and
   passes.
3. Missing, stale, duplicate, and newly added undocumented fields fail with a
   stable sorted list.
4. JSON Schema structural keywords are ignored only through an explicit
   reviewed allowlist; an unknown keyword fails instead of disappearing.
5. Repository checks run both the validator and its deterministic fixtures.

## Goal

Keep the machine-readable FoldScript contract and the human/AI programming
reference synchronized so programmable 3D assets remain authorable without
guessing field semantics.

## Scope

- GitHub issue #20;
- `Schema/foldcanvas.schema.json` public-field discovery;
- `Documentation~/foldscript-field-reference.md` scoped `Field` tables;
- deterministic standard-library validation and temporary-fixture tests;
- repository-check integration and contributor guidance;
- stable-package byte preservation.

## Non-goals

- schema, decoder, compiler, geometry, topology, UV, diagnostic, Runtime,
  Editor, Unity-version, dependency, package-version, or public-release change;
- generated documentation, Markdown rewriting, prose-quality inference, or a
  generic JSON Schema implementation;
- work on issues #19, #21, or #26.

## Acceptance

- top-level, canvas, panel-common, rectangle/disk tessellation, boundary,
  seam, operation-common, every implemented operation, and compile fields are
  covered;
- common operation fields and per-operation fields have distinct canonical
  scopes;
- missing and stale fields are reported in ordinal sorted order;
- duplicate Markdown field rows identify stable line numbers;
- at least missing, stale, and duplicate negative fixtures pass;
- validator inputs are read-only and implementation uses Python standard
  library only;
- repository validation, deterministic stable archive, workflow YAML, Python
  compilation, and `git diff --check` pass;
- hosted required checks are green at the exact audited head before merge.

## Architecture boundary

The validator is release-excluded maintenance tooling. The schema and field
reference remain package source; FoldCanvas source authority remains the 2D
canvas plus FoldScript, and generated Meshes remain derived artifacts.

## Implementation status

Complete. PR #33 merged exact audited head
`dc04e6b5d59efbc7084973ab358c07c9aaa98a54` as merge commit
`6fbdb3b012fe5dde9328376e38c8a8b5d6bb1bdc`, and issue #20 is closed. The
validator discovers 72 canonical scoped fields across all implemented
FoldScript `0.1` panels, boundaries, seams, operations, and compile settings.
Eight deterministic cases cover the real repository,
missing/stale/duplicate documentation, a newly undocumented schema field, an
unreviewed structural keyword, an invalid operation reference, and sorted
multi-error output.

Exact-head hosted run `31558708430` passed 477/477 Edit Mode tests, two clean
archive installs, producer/receiver handoff, and source-first upgrade. Main
runs `31559362738` and `31559362706` passed after merge. The rebuilt stable
archive remains exactly
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.
