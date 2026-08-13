# Current task

Execute **M25: 1.1.0 minor release qualification**.

Authoritative task file:
[`Codex/M25_MINOR_RELEASE_QUALIFICATION.md`](Codex/M25_MINOR_RELEASE_QUALIFICATION.md)

M24 is complete. PR #41 retained audited head
`c9845d758359b385ca9c861e2b38b68cc950fc6a` and merged as
`7ffa350b139be7183bb4c414d1dd817398a56b6b`. Exact protected-main repository,
Unity, and long-run workflows were green; Unity `6000.3.20f1` passed 491/491
Edit Mode tests with zero failures, skips, or inconclusive results.

M25 resolves the package-identity boundary exposed by that backward-compatible
geometry addition. Public `v1.0.1` remains immutable at archive SHA-256
`4188d23b18b924f6642f9e4eabbc15500fb60fb3d3916f23857a5a19966e1de5`;
the M24-capable package advances to `1.1.0`. A checked-in immutable-release
ledger and offline rebuild gate prevent one semantic version from ever naming
different package bytes again.

The maintainer may research, plan, implement, audit, merge, tag, publish the
GitHub release, and qualify it autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and registry or external
marketplace publication remain owner escalation points.
