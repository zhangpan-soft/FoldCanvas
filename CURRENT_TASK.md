# Current task

Execute **M19: FoldScript schema-to-field-reference coverage**.

Authoritative task file:
[`Codex/M19_SCHEMA_FIELD_COVERAGE.md`](Codex/M19_SCHEMA_FIELD_COVERAGE.md)

M17 is complete. PR #29 merged stable package head
`b0d2a849ff2bb990a209ff3104390fcdb200fd42` as merge commit
`6ed32f1ed2a48796f5c0e015205cd47249e1bcef`. Annotated tag `v1.0.0`
peels to that exact merge. Release
[`v1.0.0`](https://github.com/zhangpan-soft/FoldCanvas/releases/tag/v1.0.0)
is public, non-draft, and non-prerelease with archive SHA-256
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

M18 is complete. PR #32 merged exact audited head
`5ea179df718c3f6bed391c05b08186e43cb20990` as merge commit
`b45b84ed6c97baae1f8ea1fef7bb532b24c40904`; issue #25 closed. Main runs
`31535304144`, `31535304112`, and `31535304118` passed repository checks,
477/477 Edit Mode tests, clean installs, producer/receiver handoff,
source-first upgrade, and 512/512 deterministic long-run cases. First-party
checkout/upload/download Actions now use reviewed signed Node 24 releases;
GameCI remains the documented upstream-tag exception.

M19 addresses GitHub issue #20. It adds a dependency-free, deterministic
repository gate that compares every public FoldScript JSON field in
`Schema/foldcanvas.schema.json` with the matching scoped row in
`Documentation~/foldscript-field-reference.md`. Missing, stale, duplicate, and
unreviewed structural-keyword cases must fail in stable sorted order. M19 does
not change schema semantics, geometry, Runtime, Editor, Unity, dependencies, or
the public `1.0.0` package bytes.

Implementation is active on `agent/m19-schema-field-coverage`.

The maintainer may research, plan, implement, audit, merge, and close this
repository-only milestone autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and external marketplace
publication remain owner escalation points.
