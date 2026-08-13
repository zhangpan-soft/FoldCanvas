# Current task

Execute **M22: proof-first README patch**.

Authoritative task file:
[`Codex/M22_README_PROOF_PATCH.md`](Codex/M22_README_PROOF_PATCH.md)

M17 is complete. PR #29 merged stable package head
`b0d2a849ff2bb990a209ff3104390fcdb200fd42` as merge commit
`6ed32f1ed2a48796f5c0e015205cd47249e1bcef`. Annotated tag `v1.0.0`
peels to that exact merge. Release
[`v1.0.0`](https://github.com/zhangpan-soft/FoldCanvas/releases/tag/v1.0.0)
is public, non-draft, and non-prerelease with archive SHA-256
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

M18 is complete. PR #32 merged exact audited head
`5ea179df718c3f6bed391c05b08186e43cb20990` as merge commit
`b45b84ed6c97baae1f8ea1fef7bb532b24c40904`; issue #25 is closed.

M19 is complete. PR #33 merged exact audited head
`dc04e6b5d59efbc7084973ab358c07c9aaa98a54` as merge commit
`6fbdb3b012fe5dde9328376e38c8a8b5d6bb1bdc`; issue #20 is closed. The
schema-to-field-reference gate covers 72 canonical scoped public fields and
eight deterministic positive/adversarial cases. Main runs `31559362738` and
`31559362706` passed repository checks, 477/477 Edit Mode tests, two clean
installs, producer/receiver handoff, and source-first upgrade.

M20 is complete. PR #34 merged exact audited head
`a6844150ed38bdce62d9c39b07eb92df7ca2370e` as merge commit
`eb310e06e6686942b191ffdb6967567418f79d37`; issue #19 is closed. Main runs
`31607803239` and `31607803162` passed repository checks, 477/477 Edit Mode
tests, two clean installs, producer/receiver handoff, and source-first upgrade.

M21 is complete. PR #35 merged exact audited head
`334fb01cc273888d3dadfbf188431cd49c23eb7d` as merge commit
`67fb659ff53e13f21e464b8a4e837b72bdc60c50`. Main runs `31642947182` and
`31642947199` passed repository checks, proof regeneration, 477/477 Edit Mode
tests, repeated clean installs, producer/receiver handoff, and source-first
upgrade. The six proof PNGs and canonical manifest are release-excluded, and
the published stable archive stayed byte-identical.

M22 is GitHub issue #26's separately versioned second gate. It integrates the
audited M21 proof into both package READMEs and produces a deterministic
`1280 x 640` social-preview candidate. Because both root READMEs are explicit
UPM archive members, the package advances to compatible patch `1.0.1` with a
new deterministic archive identity. Published `v1.0.0` and RC2 assets remain
immutable; M22 does not publish a new tag, GitHub release, or external listing.

Implementation is active on `agent/m22-readme-proof-patch`.

The maintainer may research, plan, implement, audit, merge, and close this
repository-only milestone autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and external marketplace
publication remain owner escalation points.
