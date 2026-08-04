# Current task

Execute **M14: 1.0 release candidate**.

Authoritative task file:
[`Codex/M14_RELEASE_CANDIDATE.md`](Codex/M14_RELEASE_CANDIDATE.md)

M13 PR #12 was autonomously maintainer-audited at exact head
`461fd792bdd66c5605c5f919503c1cc72479dda6` and merged into `main` as
`d9434be`. Repository run `30880541772`, Unity run `30880541775`, and the
exact-head long-run `30880076543` were green. Unity `6000.3.20f1` passed
460/460 Edit Mode tests; the long run passed 512/512 generated cases and 5/5
resource envelopes. Artifact `8881073036` has digest
`sha256:3bda2604adfe17d03d3f258b778ee3b2e3ef4fa3236e9e472745e302aacb12d1`.
M14 development occurs on `codex/m14-release-candidate`, created from that
merged commit.

M14 changes the acceptance question from “does the compiler remain bounded and
deterministic under replayable stress?” to “can a precisely versioned release
candidate be installed, supported, audited, and rolled back without changing
the source-first architecture?”

The active proof covers:

- a `1.0.0-rc.1` package candidate, not the final `1.0.0` decision;
- a frozen Runtime API and FoldScript `0.1` compatibility contract;
- an exact Unity `6000.3.20f1` minimum and qualification row;
- deterministic release archive, manifest, checksum, license, security,
  clean-install, production-corpus, handoff, and robustness evidence;
- issue intake, severity ordering, autonomous exact-head audit, release, and
  rollback procedures;
- hosted full Unity and clean consumer evidence before self-merge.

The maintainer may plan, implement, audit, merge, triage issues, and continue
the roadmap autonomously. Credentials, paid services, irreversible permission
changes, legal decisions, and external marketplace publication remain owner
escalation points.

M14 does not add geometry behavior, change FoldScript semantics, add a package
dependency, publish final `1.0.0`, publish to an external marketplace, or begin
a later milestone.
