# M18: Node 24 CI runtime modernization

## Visible proof

1. Every selected third-party GitHub Action version is resolved from its
   official upstream repository to a reviewed full commit SHA.
2. Hosted pull-request and protected-main workflows retain their existing
   events, permissions, fork guards, artifact paths, evidence requirements,
   Unity `6000.3.20f1` row, and fail-closed behavior.
3. Migrated official Actions no longer emit the deprecated Node.js 20 runtime
   warning. GameCI is upgraded only when its official release metadata proves
   a supported Node 24 path; otherwise the exact pinned exception and upstream
   dependency are recorded rather than replaced by an unreviewed fork.
4. Repository, deterministic package, full Edit Mode, two clean installs,
   producer/receiver handoff, RC2-to-stable upgrade, and M13 long-run gates are
   green at the audited head.
5. The package tree remains byte-identical to public stable `v1.0.0`: rebuilding
   from maintained main produces archive SHA-256
   `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

## Goal

Remove avoidable CI runtime deprecation risk without changing FoldCanvas
package bytes, geometry behavior, or the qualified stable release.

## Scope

- resolve GitHub Action releases and runtime metadata from official upstream
  repositories;
- update immutable full-SHA allowlists, inline version labels, workflows, and
  adversarial Action-pin tests;
- preserve the existing privileged/fork trust boundary and every required
  artifact;
- audit the official GameCI compatibility path independently;
- prove the public stable archive remains unchanged;
- close or update GitHub issue #25 with exact evidence.

## Non-goals

- package version, package content, Runtime, Editor, Tests, Schema,
  `Documentation~`, geometry, topology, FoldScript, Unity version, dependency,
  license, marketplace, or public-release asset changes;
- floating Action tags, branch references, unreviewed forks, local Action
  substitutions, or reduced evidence;
- a Unity Editor upgrade or a new geometry milestone.

## Acceptance

- all remote Actions remain pinned to lowercase 40-character commits with
  exact reviewed version comments;
- official release/tag/commit/runtime evidence is recorded before a pin moves;
- negative tests reject old, floating, shortened, uppercase, mislabeled, and
  unapproved references deterministically;
- full hosted evidence passes without changing the public stable package
  archive;
- any residual GameCI warning is an explicit bounded upstream exception, not a
  silent success claim.

## Implementation status

M18 is complete. PR #32 merged exact audited head
`5ea179df718c3f6bed391c05b08186e43cb20990` as merge commit
`b45b84ed6c97baae1f8ea1fef7bb532b24c40904`, and issue #25 is closed. Official
release, tag, commit-signature, and `action.yml` metadata were re-queried on
2026-08-12 and recorded in `Docs/M18_NODE24_ACTION_REVIEW.md` before changing
any pin.

- `actions/checkout` is selected at signed `v7.0.1` commit
  `3d3c42e5aac5ba805825da76410c181273ba90b1` (`node24`).
- `actions/upload-artifact` is selected at signed `v7.0.1` commit
  `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a` (`node24`).
- `actions/download-artifact` is selected at signed `v8.0.1` commit
  `3e5f45b2cfb9172054b4087a40e8e0b5a5461e7c` (`node24`).
- `game-ci/unity-test-runner` remains at signed `v4.3.1` commit
  `0ff419b913a3630032cbe0de48a0099b5a9f0ed9` (`node20`). Its Node 24 change
  is merged upstream but still has no official release tag, so an unreleased
  commit, floating branch, fork, or warning suppression is not accepted.

Workflow pins and the central allowlist now contain those selections. The full
local repository matrix is green, including 44 Action-pin adversarial cases,
and the deterministic package SHA-256 remains
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.
Hosted implementation-head verification is green:

- PR Unity run `31533100145`: 477/477 Edit Mode tests, two independent clean
  consumers, producer/receiver handoff, and source upgrade all passed with all
  expected artifacts uploaded.
- push long-run `31533018780`: 477/477 Edit Mode tests plus 512 deterministic
  robustness cases passed with zero unexpected cases.
- manually dispatched public qualification `31533891950`: exact stable assets,
  two public consumers, RC2-to-stable source upgrade, and final publication
  proof passed; proof artifact `9118098988` is `qualified: true`.
- jobs without GameCI emitted no Node 20 Action warning. Jobs that invoke
  GameCI emitted one final warning whose Action list contains only the retained
  `game-ci/unity-test-runner` v4.3.1 SHA.

Protected-main runs `31535304144`, `31535304112`, and `31535304118` passed
repository validation, 477/477 Edit Mode tests, clean installs, handoff,
source-first upgrade, and 512/512 deterministic long-run cases after merge.
