# Current task

Execute **M18: Node 24 CI runtime modernization**.

Authoritative task file:
[`Codex/M18_NODE24_CI.md`](Codex/M18_NODE24_CI.md)

M17 is complete. PR #29 merged stable package head
`b0d2a849ff2bb990a209ff3104390fcdb200fd42` as merge commit
`6ed32f1ed2a48796f5c0e015205cd47249e1bcef`. Annotated tag `v1.0.0`
peels to that exact merge. Release
[`v1.0.0`](https://github.com/zhangpan-soft/FoldCanvas/releases/tag/v1.0.0)
is public, non-draft, and non-prerelease with archive SHA-256
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

Release workflow run `31511961850` succeeded after recovery PR #30 corrected
ambiguous recursive readiness-report discovery without moving the tag or
changing package bytes. Public qualification run
[`31512005281`](https://github.com/zhangpan-soft/FoldCanvas/actions/runs/31512005281)
then passed exact-asset verification, two independent clean Unity consumers,
an RC2-to-stable source-first upgrade, and the final stable-publication proof.
The proof artifact `9109614640` is `qualified: true`.

M18 addresses GitHub issue #25. It must move eligible official Actions to
reviewed Node 24-compatible releases while preserving immutable SHA pins,
workflow permissions and guards, Unity `6000.3.20f1`, all required evidence,
and the exact stable package archive. GameCI may change only through a
supported official upstream path; no unreviewed fork or warning suppression is
allowed.

Implementation is active on `agent/m18-node24-ci`. The three first-party
Actions have reviewed signed Node 24 release selections. GameCI remains on its
signed `v4.3.1` release because its merged Node 24 change is not yet tagged.
The complete upstream record is `Docs/M18_NODE24_ACTION_REVIEW.md`. Local
repository validation and deterministic package hashing are green; hosted
full-matrix evidence, exact-head audit, merge, and issue closure remain.

The maintainer may research, plan, implement, audit, merge, and close this
repository-only milestone autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and external marketplace
publication remain owner escalation points.
