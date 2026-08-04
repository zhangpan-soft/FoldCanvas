# Current task

Execute **M16: stable-candidate soak and contributor on-ramp**.

Authoritative task file:
[`Codex/M16_STABLE_SOAK_AND_COMMUNITY.md`](Codex/M16_STABLE_SOAK_AND_COMMUNITY.md)

M15 PR #14 was autonomously audited at exact head
`4729ab93ed37f0bc59546eb3e1b2464f9b310959` and merged into `main` as
`4db988ffac6dad4362d126001e5c9a67081ef2b7`. Unity `6000.3.20f1` passed
472/472 Edit Mode tests, two public-package consumers, source-first upgrade,
512/512 deterministic long-run cases, and 5/5 resource envelopes.

Public pre-release [`v1.0.0-rc.2`](https://github.com/zhangpan-soft/FoldCanvas/releases/tag/v1.0.0-rc.2)
was published from that merge. Its public archive SHA-256 is
`72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`.
Recovery run `30898280828` independently verified the four public assets, two
clean public consumers, and the source-first upgrade. PR #15 then fixed the
workflow-to-workflow dispatch boundary and merged as
`0d06450f1f1f309b605b1e25f1833ef993d2abe7` after 19 checks passed.

M16 PR #16 was audited at exact head `82defcb8`, passed 472/472 Edit Mode plus
all clean consumer, handoff, and upgrade gates, and merged as `211d55c`.
`main` is protected. Manual soak `30910305230` proved the full candidate path
without qualifying for stable; evaluator `30910883290` remains correctly
blocked until 168 hours and two genuine scheduled runs are complete.

M16 changes the acceptance question from “can the exact public RC be consumed
and upgraded?” to “can that immutable RC survive a candidate-pinned soak while
an unfamiliar contributor can find, reproduce, and complete a bounded task?”

The active proof covers:

- one non-packaged active-candidate record binding RC2 tag, commit, archive,
  publication time, Unity version, and long-run parameters;
- scheduled Unity runs that orchestrate from `main` but check out and test the
  exact immutable candidate tag rather than the moving default branch;
- stable-exit evidence collection that accepts only distinct successful
  `schedule` runs on the candidate commit;
- a contributor start page, bounded issue forms, discoverable repository
  metadata, and several genuinely small tasks with numerical acceptance;
- a fork/PR-only external AI-agent policy with protected `main`, no shared
  credentials, and maintainer-owned integration PRs for the privileged Unity
  evidence that GitHub correctly withholds from fork runs;
- no package-byte change while RC2 is soaking.

The maintainer may plan, implement, audit, merge, triage issues, publish GitHub
pre-releases, and continue the roadmap autonomously. Credentials, paid
services, irreversible permission changes, legal decisions, and external
marketplace publication remain owner escalation points.

M16 does not change geometry, FoldScript, Runtime API, package dependencies,
or the published RC2 bytes. It does not publish final `1.0.0` before the
machine-readable gate is ready and exact evidence is audited.
