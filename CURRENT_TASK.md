# Current task

Execute **M15: public distribution and upgrade qualification**.

Authoritative task file:
[`Codex/M15_PUBLIC_DISTRIBUTION.md`](Codex/M15_PUBLIC_DISTRIBUTION.md)

M14 PR #13 was autonomously maintainer-audited at exact head
`7311771a2907b8ef58851185698ab4d62d91e6b4` and merged into `main` as
`a8c81e61175dafbc48d1750de7ef6823589517a6`. The exact-head repository,
package, Unity, clean-install, source-handoff, production-corpus, and M13
long-run gates were green. Unity `6000.3.20f1` passed 466/466 Edit Mode tests;
the long run passed 512/512 generated cases and 5/5 resource envelopes.

Tag `v1.0.0-rc.1` was created from the reviewed merge and published as a
GitHub pre-release by run `30888055335`. Its public archive SHA-256 is
`ff3a065eec3a638701ff51d4f069684df4f075226305253ae04fd6ed2b250fdd`.
The release also contains the matching checksum, per-file manifest, and
`built-unverified` candidate evidence. M15 development occurs on
`codex/m15-public-distribution`, created from the M14 merge.

M15 changes the acceptance question from “can CI construct and qualify an
immutable release candidate?” to “can a user consume the exact public release
assets and upgrade authoritative source without repository access, source
mutation, package-version ambiguity, or retained generated Meshes?”

The active proof covers:

- a `1.0.0-rc.2` package iteration for every packaged M15 change, preserving
  the immutability of the already published `1.0.0-rc.1` bytes;
- independent verification and installation of public GitHub release assets;
- two clean public-archive consumers with real Unity XML and Editor logs;
- a source-first upgrade rehearsal from the M14 rollback baseline through the
  current candidate, with generated artifacts discarded and rebuilt;
- a machine-readable stable-release exit gate and an explicit soak period;
- hosted full Unity, clean-consumer, handoff, corpus, and robustness evidence
  before autonomous merge or publication.

The maintainer may plan, implement, audit, merge, triage issues, publish GitHub
pre-releases, and continue the roadmap autonomously. Credentials, paid
services, irreversible permission changes, legal decisions, and external
marketplace publication remain owner escalation points.

M15 does not add geometry behavior, change FoldScript semantics, add a package
dependency, publish final `1.0.0`, claim an untested Unity version, accept a
generated Mesh as upgrade input, or begin a later milestone.

Current local implementation evidence: Unity `6000.3.20f1` passed 472/472
package Edit Mode tests. One marked clean host passed 1/1 with exact rollback
`0.1.0-preview.21`, cleared only owned derived state, then passed 1/1 with RC2
while reproducing identical source, appearance, geometry, OBJ, diagnostic,
topology, validation, and single-closed-volume evidence. Hosted PR and public
release evidence are still required and must not be inferred from this local
run.
