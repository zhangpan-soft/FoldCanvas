# Goal

Deliver M16 on `codex/m16-stable-soak-community`: keep public RC2 byte-exact,
run candidate-pinned soak evidence after `main` advances, aggregate the honest
stable gate, and make the repository actionable for a first-time contributor.

# User-visible proof

A Monday/Thursday workflow reads one reviewed active-candidate record from the
orchestration revision, checks out exact RC2 tag/commit, re-verifies its public
archive, runs full Unity plus 512 robustness cases and 5 resource envelopes,
and uploads a bound soak record. Separately, a contributor start page and
bounded public issues let an unfamiliar developer reproduce the project and
select work without reading the full internal milestone history.

# Scope

- close M15/PR #15 with exact public evidence
- non-packaged active RC2 control record and deterministic validator
- exact-tag scheduled Unity soak, never moving-`main` qualification
- stable-evidence aggregation over public, soak, issue, gate, and audit inputs
- contributor start page, issue form, repository metadata, and useful starter
  issues
- immutable GitHub Actions pins plus a deterministic approved-action validator
- complete hosted regression and autonomous exact-head audit

# Non-goals

- package-byte, geometry, topology, FoldScript, Runtime API, schema, compiler,
  dependency, or Unity-version changes
- republishing or mutating RC2
- final `1.0.0` before the stable report is ready
- manual runs masquerading as scheduled evidence
- fake engagement, unsolicited automated outreach, or marketplace publication
- adding or upgrading a third-party Action beyond the currently reviewed
  versions
- later-milestone geometry work

# Files expected to change

- `CURRENT_TASK.md`
- `Codex/M15_PUBLIC_DISTRIBUTION.md`
- `Codex/M16_STABLE_SOAK_AND_COMMUNITY.md`
- `Docs/Plans/active-plan.md`
- `Docs/Community/START_HERE.md`
- `.github/foldcanvas-active-candidate.json`
- `.github/ISSUE_TEMPLATE/contributor_task.yml`
- `.github/workflows/m16-candidate-soak.yml`
- `.github/workflows/repository-checks.yml`
- `Scripts/validate_active_candidate.py`
- `Scripts/test_active_candidate.py`
- stable soak collection/validation scripts added during work package D
- `Scripts/validate_action_pins.py`
- `Scripts/test_action_pins.py`
- `Scripts/validate_repository.py`
- existing `.github/workflows/*.yml` files that invoke remote Actions

# Geometry invariants

- Appearance canvas plus FoldCanvas/FoldScript source remain authoritative;
  all Meshes and soak/release reports remain derived.
- M16 changes no coordinate mapping, winding, boundary order, seam topology,
  tolerance, tessellation, validation, or operation semantics.
- The exact public RC2 archive remains
  `72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`.
- Scheduled evidence runs candidate commit
  `4db988ffac6dad4362d126001e5c9a67081ef2b7`; a later `main` commit cannot
  substitute.
- Identical candidate source/settings still produce identical ordered
  geometry, UVs, provenance, topology, diagnostics, OBJ, and reports.
- Runtime remains free of Editor, workflow, release, filesystem, and network
  behavior.

# Implementation steps

1. Record M15 audit, merge, RC2 publication/public qualification, and PR #15
   dispatch correction outside the immutable package.
2. Define M16 scope, candidate-pinned evidence contract, contributor task
   contract, risks, and non-goals before implementation.
3. Add the strict active-candidate JSON, validator, safe GitHub outputs, and
   deterministic positive/negative tests.
4. Add an orchestration workflow that validates current control, checks out
   exact tag/commit, re-verifies the public archive, runs real Unity/M13
   evidence, and emits a bound run record.
5. Build stable-evidence collection and validation that accepts only distinct
   successful scheduled records on RC2 and feeds the unchanged M15 evaluator.
6. Publish the contributor start page and bounded issue form; update repository
   metadata/topics and create useful first issues with exact acceptance.
7. Run JSON/YAML, repository, release-package, RC immutability,
   candidate-control, stable-exit, link, and diff checks locally.
8. Run complete hosted repository/Unity gates for the exact PR head, audit the
   diff and open issue/PR state, then merge autonomously when green.
9. After merge, manually dispatch the soak workflow as a non-qualifying proof;
   keep genuine scheduled evidence for stable qualification.
10. Evaluate stable readiness only after 168 hours, two qualifying schedules,
    zero release blockers, all gates, and an exact-candidate audit.
11. Resolve each currently used Action version through the official GitHub
    repository, pin every invocation to that full commit, and reject future
    floating or unapproved references in repository validation.

# Test matrix

## Candidate control

- valid RC2 record yields sorted, newline-safe GitHub outputs
- disabled, wrong version/tag, short commit/hash, invalid timestamp, Unity
  drift, bad seed/count, or stable-policy drift fails deterministically
- package/tag/contract identity stays aligned

## Soak workflow

- orchestration revision may be newer than candidate commit
- exact annotated tag resolves to the recorded full commit
- public archive digest must match before Unity
- full XML is Passed with zero failed/skipped/inconclusive tests
- Editor.log proves Unity `6000.3.20f1` completed with exit code zero
- robustness is 512/512 and resources are 5/5
- manual event records `qualifiesForStableExit: false`
- scheduled event records the exact candidate, unique run ID, completion time,
  and evidence digest
- pull-request candidate validation remains read-only; `checks: write` is
  scoped only to the non-PR Unity soak job that publishes its check result

## Stable aggregation

- duplicate, manual, pre-publication, wrong-commit, wrong-archive, failed,
  skipped, inconclusive, malformed, or missing-artifact runs do not qualify
- fewer than 168 hours or fewer than two qualifying schedules remains blocked
- open `release-blocker`, stale audit, or incomplete gate set remains blocked
- complete synthetic evidence remains ready without weakening M15 rules

## Community

- start-page links resolve
- issue forms contain GitHub Issue Form `name`, `description`, and `body`
- every starter issue has outcome, scope, acceptance, non-goals, and no hidden
  package/architecture expansion
- all remote workflow Actions use lowercase 40-character reviewed commit SHAs
  with exact version comments; alternate YAML-key encodings cannot hide a
  reference from validation
- local Actions stay under `.github/actions`; every manifest and transitive
  reference is validated, including unreferenced manifests, missing/duplicate
  targets, path escape, and cycles
- RC2 package archive rebuild remains byte-identical despite M16 repository
  control/community files

# Risks and rollback

- **Moving-main false evidence:** always check out recorded tag and compare
  full commit before Unity; never accept branch name as candidate identity.
- **Manual run counted as soak:** bind immutable GitHub event name and require
  exact `schedule` in the stable collector.
- **Control record silently changes candidate:** exact-head review plus version,
  contract, full commit, archive, and public-release checks fail closed.
- **Community issues become vague backlog:** publish only tasks with user value,
  bounded files/contracts, acceptance, and non-goals; close stale duplicates.
- **Fork CI either leaks secrets or cannot run Unity:** never send Unity or
  Actions secrets to fork code. Run only read-only checks on the fork, fail the
  base-owned trust gate, review the exact diff, and use a maintainer-owned
  integration PR for privileged Unity evidence.
  Privileged push execution is limited to post-merge protected `main`.
  Keep every external agent fork-only with no collaborator/write access;
  workflow branch guards are defense in depth, not isolation from a repository
  writer. A future writer requires a protected Environment or equivalent
  base-owned approval before Unity credentials remain available.
- **RC2 bytes drift:** M16 changes only release-excluded files and reruns the
  deterministic package hash check.
- **Mutable Action tag executes different code:** execute only reviewed full
  commit SHAs, retain the source version in comments, and make unapproved or
  floating references fail repository validation.
- **User scratch contamination:** stage explicit paths only; never include the
  untracked `Project~` files.
- Rollback is disabling the active-candidate record or reverting the M16
  orchestration merge. RC2 release assets and authoritative source are not
  modified.

# Progress log

- 2026-08-04: M15 exact head `4729ab93` passed 472/472 Edit Mode, two clean
  consumers, source-first upgrade, 512/512 robustness, and 5/5 resources; PR
  #14 merged as `4db988f`.
- 2026-08-04: Published public pre-release RC2 from `4db988f`; archive SHA-256
  is `72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`.
  Manual recovery run `30898280828` verified public assets, consumers, upgrade,
  and the expected blocked stable snapshot.
- 2026-08-04: PR #15 exact head `9a480bf3` passed repository, package,
  472/472 Unity, clean consumer, handoff, and upgrade gates. It was audited and
  merged as `0d06450`; future tagged publication now explicitly dispatches
  public qualification at the exact tag.
- 2026-08-04: Created `codex/m16-stable-soak-community`. Baseline public
  contributor count is zero. The repository has strong technical evidence but
  lacked an obvious starter path and candidate-pinned scheduled soak.
- 2026-08-04: Added the strict RC2 control record, deterministic validator and
  negative tests, candidate-pinned Monday/Thursday Unity soak workflow,
  contributor start page, and bounded contributor-task issue form.
- 2026-08-04: Added deterministic soak-record aggregation, a reviewed 14-gate
  RC2 ledger with tree-equivalence proof, and an automatic stable evaluator
  that rechecks GitHub run and individual required-job success, public
  qualification, open release blockers, exact audit, and scheduled evidence.
- 2026-08-04: Rebuilt the excluded M16 repository changes and confirmed the
  public RC2 archive remains byte-identical at
  `72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`.
  Repository, candidate, public-release, upgrade, clean-install, handoff,
  stable-exit, soak, gate-ledger, JSON/YAML, link, and diff checks pass locally.
- 2026-08-04: PR #16 exact head `82defcb8` passed 472/472 Edit Mode and all
  consumer gates, was audited, and merged as `211d55c`; `main` now has strict
  PR/check/conversation protection with admin enforcement.
- 2026-08-04: Pre-recruitment audit confirmed GitHub correctly withholds Unity
  secrets from fork PRs. Began the fail-closed fork intake gate and documented
  maintainer-owned integration PR path rather than exposing credentials or
  silently accepting skipped Unity evidence.
- 2026-08-04: Manual candidate soak `30910305230` passed 472/472 Edit Mode,
  512/512 robustness, and 5/5 resources, but correctly recorded itself as
  non-qualifying. Stable evaluation `30910883290` verified 14/14 historical
  gates and zero release blockers while remaining blocked at 0/2 schedules and
  under 168 hours.
- 2026-08-04: Defined external AI agents as fork/PR-only untrusted
  contributors. Moltbook is an optional recruitment surface, never a code or
  credential authority; its separate terms/public identity confirmation remain
  an owner gate before registration.
- 2026-08-04: PR #17 exact head `77b8adff` passed 472/472 Edit Mode plus the
  repository, deterministic package, clean-install, handoff, and source-upgrade
  gates. Independent audit found no P0/P1/P2 issue; it merged as `df5e13c`.
- 2026-08-04: Closed, unmerged canary PR #18 proved the zero-permission
  `Trusted contribution qualification` check attached to exact PR head
  `0d9f48ba`. Added it as the seventh strict required `main` check, then deleted
  the temporary canary branch. Post-merge repository and Unity push workflows
  also completed successfully.
- 2026-08-04: Confirmed repository access contains one owner and zero non-owner
  collaborators. Published bounded starter issues #19 (Roll convention
  diagram), #20 (schema/reference drift validator), and #21 (Windows RC2 clean
  install). External agents remain fork-only.
- 2026-08-05: The exact-head audit's remaining P3 supply-chain note was promoted
  into scoped M16 hardening. Official GitHub tag resolution verified signed
  commits for `actions/checkout@v4.2.2`,
  `actions/upload-artifact@v4.6.2`,
  `actions/download-artifact@v4.1.8`, and
  `game-ci/unity-test-runner@v4.3.1`; implementation will pin those exact
  commits and add deterministic anti-regression checks.
- 2026-08-05: Independent review rejected PR #23's initial line-oriented
  validator because alternate YAML mapping-key forms and a local composite
  wrapper could hide an unreviewed Action. The remediation enforces canonical
  Action-bearing YAML, scans every local manifest and transitive reference,
  and expands the deterministic policy suite to 35 positive and negative cases.
- 2026-08-06: Genuine scheduled candidate soak run `31076024500` checked out
  exact RC2 commit `4db988ff`, reverified archive SHA-256
  `72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`,
  passed 472/472 Edit Mode tests, 512/512 robustness cases, and 5/5 resource
  envelopes, and emitted a qualifying bound record with zero failed, skipped,
  or inconclusive tests. Stable evaluator run `31076434408` passed its workflow
  and all 14 reviewed gates with zero release blockers, but correctly reported
  `blocked` at 1/2 qualifying schedules and 44.325278/168 soak hours. No stable
  release was published.

# Decisions made

- M16 uses RC2 as the immutable candidate and changes no package file. Any
  packaged correction requires a new RC and a fresh soak identity.
- The default branch owns orchestration; the candidate tag owns executed
  package code and tests. This separates maintainable scheduling from immutable
  candidate evidence.
- Monday/Thursday scheduled runs balance independent observations with CI
  resource cost. The 168-hour clock remains independent and cannot be shortened
  by schedule frequency.
- Manual dispatch proves infrastructure but never stable qualification.
- Community growth is treated as product work: discoverability, a short
  successful first run, and bounded issues precede expectations of unsolicited
  external PRs.
- External AI agents receive no direct repository access. GitHub branch
  protection enforces PR/current-check/conversation gates without requiring a
  second approver that does not yet exist.
- A base-owned zero-permission metadata check blocks a fork PR from direct
  merge; successful external work is imported with attribution into an
  owner-authored integration PR for privileged Unity evidence. The primary
  credential boundary remains zero non-owner write access.
- Action tags remain useful review labels but are not execution identities.
  Workflows execute full reviewed commit SHAs and preserve the semantic version
  only as an inline comment; a new Action or version requires an explicit
  allowlist update and ordinary PR evidence.
- Action-bearing workflow and local-manifest YAML uses a deliberately narrow,
  canonical block style. Local Actions do not bypass dependency review: their
  checked-in manifests and transitive references are part of the same fail-closed
  validation graph.

# Final verification

Planning, active-candidate control, deterministic tests, candidate-pinned
workflow, stable artifact aggregation, reviewed gate ledger, automatic stable
evaluation, repository metadata, fork-safe contributor intake, starter issues,
exact-head hosted gates/audit, and the post-merge manual soak proof are
implemented. One genuine scheduled observation is now accepted; one additional
qualifying scheduled observation and the 168-hour minimum remain pending. RC2
package bytes, final `1.0.0`, external marketplace registration, geometry,
FoldScript, Runtime API, dependencies, and later milestones were not
implemented.
