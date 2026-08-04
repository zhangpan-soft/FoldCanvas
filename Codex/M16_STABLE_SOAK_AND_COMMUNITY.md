# M16: stable-candidate soak and contributor on-ramp

## Visible proof

1. A scheduled workflow launched from a later `main` revision checks out and
   runs Unity against exact RC2 commit
   `4db988ffac6dad4362d126001e5c9a67081ef2b7`, not the moving branch head.
2. Every run binds public archive digest, GitHub run identity, event type,
   complete Unity XML/log counts, 512-case robustness evidence, and 5/5
   resource envelopes into one machine-readable soak record.
3. Manual or pull-request runs can prove the workflow, but only distinct
   successful `schedule` events on the candidate commit qualify for stable.
4. A first-time contributor can understand the source model, install the RC,
   choose a bounded task, run the relevant checks, and submit evidence without
   reverse-engineering milestone history.
5. The public repository exposes focused `good first issue`, `help wanted`,
   and `geometry-case` entry points instead of waiting for accidental traffic.

## Goal

Qualify the immutable public RC for stable promotion and remove the practical
barriers that make a technically public repository feel like a private
construction site. M16 preserves every accepted M00-M15 source-authority,
geometry, topology, determinism, compatibility, and distribution contract.

## Candidate-pinned soak contract

- `.github/foldcanvas-active-candidate.json` is orchestration control, not
  package source. It binds the exact candidate tag, commit, archive hash,
  publication time, Unity row, fixed seed, and fixed cases-per-suite.
- Scheduled workflows are defined on the default branch but must check out the
  exact candidate tag before public-asset verification or Unity execution.
  Testing a later `main` commit cannot qualify an older candidate.
- Tag, commit, package version, and public archive SHA-256 are verified before
  Unity. Any mismatch fails; the runner does not fall back to `main`.
- A qualifying soak record has `event == schedule`, a unique GitHub run ID,
  the exact candidate commit, `conclusion == success`, and zero failed,
  skipped, or inconclusive Unity tests.
- Manual dispatch is a non-qualifying rehearsal. It cannot be relabeled as a
  scheduled run and cannot satisfy the two-run stable policy.
- The schedule runs Monday and Thursday. This supplies two independent weekly
  observations without converting the long-run suite into a daily resource
  drain. The separate 168-hour minimum still controls earliest promotion.
- RC2 bytes are immutable. M16 control, workflow, planning, and community files
  stay outside the UPM archive. A packaged correction would require a new RC
  version and restart the candidate identity and soak.

## Contributor on-ramp contract

- Starter tasks must be independently useful, bounded to one concept, and
  include exact files, non-goals, acceptance criteria, and a reproducible check.
- “Good first issue” does not mean unreviewed core geometry. First tasks favor
  examples, deterministic fixtures, documentation, diagnostics, and isolated
  Editor UX where existing architecture already defines the behavior.
- Geometry proposals begin with 2D panels, named boundaries, seams, ordered
  operations, UV expectations, winding, invalid cases, and a visible proof.
  A final-shape screenshot alone is insufficient.
- Contributor code follows the same evidence ladder as maintainer code. A
  smaller task can run fewer local checks. Fork PRs run only non-secret checks
  and cannot merge directly; an approved exact patch enters a maintainer-owned
  integration PR that must pass hosted repository and Unity gates before merge.
- Generated Meshes remain derived. No starter task may introduce direct
  text-to-mesh, opaque model generation, network behavior, or a hidden
  dependency.
- Issues must not be manufactured merely to increase counts. Each published
  task must correspond to roadmap value the maintainer would otherwise do.

## Work packages

### A. Close M15 accurately

- record PR #14 audit/merge, RC2 release, public qualification, and PR #15
  dispatch fix in non-packaged task evidence;
- preserve RC2 archive identity and do not rewrite published release assets.

### B. Active candidate control

- add a strict JSON control record outside the package;
- validate format, version, tag/version relation, full commit, archive digest,
  publication time, Unity row, long-run seed/count, and stable policy;
- add deterministic positive and negative Python tests;
- reject disabled, malformed, drifted, or unsafe GitHub output values.

### C. Exact-candidate scheduled soak

- add a Monday/Thursday schedule plus manual rehearsal;
- validate control on the orchestration revision, then check out the exact tag;
- re-verify the public release bytes before Unity;
- run the full Unity Edit Mode suite, 512 generated cases, and 5 resource
  envelopes using candidate code;
- upload XML, Editor.log, reports, public verification, and one bound soak
  record even though the default branch has advanced.

### D. Stable evidence aggregation

- collect public qualification and distinct soak artifacts without trusting
  labels or run names alone;
- reject manual, duplicate, stale, wrong-commit, failed, skipped,
  inconclusive, pre-publication, or malformed records;
- query open `release-blocker` issues and record an exact-candidate audit;
- evaluate the existing stable gate without weakening its 168-hour/two-run
  requirements;
- keep final publication separate until the report is `ready`.

### E. Community entry and discoverability

- publish one concise contributor start page with installation, architecture,
  task lanes, evidence, and review expectations;
- add a contributor-task issue form and ensure existing bug/feature/geometry
  forms lead to reproducible source-first reports;
- improve repository description/topics and publish bounded starter issues;
- measure the initial baseline honestly: zero external contributors at M16
  start is a discovery/on-ramp problem to improve, not a quality claim.
- keep all external agents on fork-to-PR access; community platforms receive
  no repository credential, collaborator role, Unity license, or execution
  authority.
- add one base-owned metadata-only trust check that never checks out fork code;
  skip credentialed Unity jobs on fork events and require an audited internal
  integration PR for complete privileged evidence.

### F. Acceptance and autonomous audit

- run JSON/YAML, repository, release-package, candidate-control, stable-exit,
  and `git diff --check` validation;
- run the full hosted Unity suite for the PR head;
- after merge, manually rehearse the candidate-soak workflow, then retain only
  genuine scheduled events for stable qualification;
- audit exact head, open issue/PR state, artifacts, and package-byte identity
  before every merge or promotion.

## Tests

- valid active candidate produces deterministic safe outputs;
- disabled candidate, wrong version/tag, short commit/hash, invalid UTC,
  invalid seed/count, and drifted stable policy fail;
- scheduled orchestration tests exact tag/commit even after `main` advances;
- public release archive mismatch fails before Unity;
- manual run is marked non-qualifying;
- scheduled run contains real full-suite XML/log and long-run evidence;
- duplicate or wrong-commit soak records cannot satisfy stable exit;
- no packaged file or deterministic RC2 archive byte changes;
- all existing M00-M15 tests remain enabled.

## Non-goals

- new geometry behavior, topology mutation, FoldScript version, Runtime API,
  dependency, or automatic repair;
- changing or republishing RC2 assets under the same version;
- fake stars, automated unsolicited outreach, contributor-count inflation, or
  external marketplace submission;
- weakening the seven-day/two-scheduled-run stable policy;
- treating manual runs as scheduled evidence;
- final `1.0.0` publication before a complete ready report and exact audit.

## Governance

Routine planning, implementation, issue triage, repository metadata, GitHub
pre-releases, exact-head audit, merge, and roadmap continuation are delegated
to the autonomous maintainer. Credentials, paid services, irreversible
permissions, legal decisions, and external marketplace publication remain
owner escalation points.

## Implementation status

PR #16 exact head `82defcb8` passed 472/472 Edit Mode plus repository, package,
clean-install, handoff, and upgrade checks, was independently audited, and
merged as `211d55c`. `main` is protected and repository metadata plus the
fork/PR-only agent policy are public. Manual soak run `30910305230` passed
472/472 Edit Mode, 512/512 robustness, and 5/5 resources while correctly
recording `qualifiesForStableExit: false`; stable evaluator run `30910883290`
kept promotion blocked at 0/2 scheduled runs and under 168 hours. Before public
recruitment, `codex/m16-fork-pr-qualification` closes
the discovered GitHub fork-secret boundary with a metadata-only trust check,
fork-safe Unity skip conditions, deterministic static tests, and documented
maintainer integration PRs. Genuine scheduled runs, starter issues, and the
168-hour stable decision remain pending. RC2 package bytes stay unchanged.
