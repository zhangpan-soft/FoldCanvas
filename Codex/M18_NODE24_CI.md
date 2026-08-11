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

Planning began after the qualified `v1.0.0` public release. No M18 Action pin
has been selected or changed yet.
