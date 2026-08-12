# Current task

Execute **M20: Roll handedness review diagram**.

Authoritative task file:
[`Codex/M20_ROLL_HANDEDNESS_DIAGRAM.md`](Codex/M20_ROLL_HANDEDNESS_DIAGRAM.md)

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

M20 addresses GitHub issue #19. It derives one source-controlled Roll-U/Roll-V
handedness diagram from the implemented equation, executor, and named Edit Mode
tests. The review distinguishes source UV direction, geometric winding,
outward/inward radial orientation, and two-sided material behavior. M20 changes
only release-excluded community documentation, validation tooling, and task
records; it does not change compiler behavior, tests, package version, or the
public `1.0.0` package bytes.

Implementation is active on `agent/m20-roll-handedness-diagram`.

The maintainer may research, plan, implement, audit, merge, and close this
repository-only milestone autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and external marketplace
publication remain owner escalation points.
