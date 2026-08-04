# Goal

Deliver M14 on `codex/m14-release-candidate`: turn the accepted M00-M13
compiler into a precisely versioned, frozen, installable, supportable, and
rollback-safe `1.0.0-rc.1` candidate without changing geometry behavior or the
2D-source architecture.

# User-visible proof

Build the same candidate twice, inspect matching archive and file-manifest
digests, install it into independent Unity `6000.3.20f1` consumers, compile the
maintained source corpus, and retain full hosted XML/log/evidence artifacts. A
single release-candidate manifest must identify every frozen contract and gate.

# Scope

- `1.0.0-rc.1` package/compiler evidence version
- exact Unity `6000.3.20f1` package and hosted support row
- frozen compiled Runtime API and FoldScript `0.1` compatibility fixtures
- deterministic per-file release manifest and release-candidate evidence
- complete license, security, support, install, troubleshooting, and rollback
  documentation inside the distributable package
- pull-request dry-run release artifacts and tag-gated publication safeguards
- complete Unity, clean-install, corpus, handoff, and robustness regression
- autonomous exact-head maintainer audit and green-check merge

# Non-goals

- final `1.0.0` publication or external marketplace submission
- new geometry behavior, operation, topology mutation, or automatic repair
- FoldScript schema/semantic changes
- new Runtime filesystem/network behavior or package dependency
- support claims for an untested Unity version
- signing, paid registry/service, legal change, or irreversible permissions
- later-milestone implementation

# Invariants

- Appearance canvas plus canonical FoldScript remain source; all release
  bundles, reports, Meshes, OBJ, Prefabs, and screenshots remain derived.
- Runtime stays deterministic, dependency-free, and free of `UnityEditor`.
- Public Runtime API and FoldScript behavior cannot drift under a metadata-only
  release iteration.
- Every support statement points to real Unity XML/log evidence for the exact
  Editor version.
- Missing evidence fails closed. A workflow that did not start Unity is not a
  passing Unity gate.
- User `Project~` scratch is never staged or packaged.

# Implementation steps

1. Close M13 with exact-head autonomous audit and merge evidence; create the
   M14 branch, specification, roadmap entry, and active plan.
2. Inventory package/release workflows, public API/corpus baselines, canonical
   FoldScript fixtures, legal/security/support docs, and all hosted gates.
3. Define the machine-readable release-candidate contract and exact Unity
   support matrix.
4. Implement deterministic release file-manifest/evidence generation and
   fail-closed validation; include required distributable governance docs.
5. Freeze API and FoldScript compatibility evidence, then advance package,
   compiler, corpus, and changelog versions together to `1.0.0-rc.1`.
6. Add repository and Unity tests for version, matrix, archive, API,
   FoldScript, evidence, and rollback contracts.
7. Update workflows so every PR validates and uploads the complete candidate
   bundle while tag publication remains exact-version gated.
8. Run JSON, asmdef, repository, archive, clean-install, handoff, Python, and
   `git diff --check` validation locally.
9. Run the complete hosted Unity matrix, clean consumers, handoff, corpus, and
   bounded robustness evidence; record exact totals and artifact digests.
10. Open the M14 PR, audit its exact head and issue/check state, record the
    autonomous maintainer decision, and merge only when every required gate is
    green.

# Test matrix

## Version and freeze

- package, compiler, changelog, API, corpus, and RC evidence versions agree
- exact Unity minimum is `6000.3.20f1`
- Runtime API signatures and digest equal the frozen baseline
- canonical FoldScript fixtures remain stable and unknown versions fail closed

## Package and release evidence

- two archives and file manifests are byte-identical
- every archive entry has deterministic size/hash evidence
- legal/security/support/release docs are present
- no `.git`, `.github`, `Project~`, credentials, generated Mesh source, or
  repository-only plans enter the archive
- evidence rejects missing, empty, failed, skipped, or inconclusive gates
- exact tag must equal `v1.0.0-rc.1`

## Unity and consumers

- complete package suite passes on Unity `6000.3.20f1`
- two archive-only consumers resolve different PackageCache paths and produce
  identical evidence
- production corpus, producer/receiver handoff, and bounded robustness gates
  retain their accepted semantic results

## Governance and rollback

- issue forms request source, versions, diagnostics, expected invariants, and
  minimal reproduction
- security issues route privately
- rollback identifies the prior package/commit and preserves editable source
- exact-head audit plus green required checks precede autonomous merge

# Risks and rollback

- **False compatibility claim:** claim only the exact hosted row and make new
  rows reviewed data changes.
- **Metadata drift:** one release contract binds package, compiler, API,
  FoldScript, corpus, Unity, and archive evidence.
- **A workflow uploads partial evidence:** validators treat missing, skipped,
  failed, inconclusive, empty, or wrong-version data as failure.
- **RC mistaken for stable:** use `1.0.0-rc.1`, label it pre-release, and keep
  final `1.0.0` as a separate explicit decision.
- **User scratch contamination:** stage explicit paths only and enforce archive
  allowlists.
- Rollback is reverting isolated M14 commits and reinstalling the accepted M13
  `0.1.0-preview.21` archive from merge `d9434be`; authoritative 2D source is
  recompiled rather than replacing it with retained Meshes.

# Progress log

- 2026-08-04: Exact-head autonomous audit approved M13 head `461fd792`; hosted
  repository, Unity, and long-run gates were green. PR #12 merged into `main`
  as `d9434be` without touching the user's untracked `Project~` scratch.
- 2026-08-04: Created `codex/m14-release-candidate`, defined the RC contract,
  exact support claim, governance boundary, work packages, tests, and rollback
  before implementation. No final `1.0.0` or marketplace decision was made.
- 2026-08-04: Implemented `1.0.0-rc.1`, the exact `unityRelease: 20f1`
  manifest row, frozen 808-signature Runtime API digest, three canonical
  FoldScript compatibility fixtures, package-contained support/release docs,
  deterministic file-manifest/RC-evidence sidecars, and an RC-only least-
  privilege release workflow.
- 2026-08-04: An independent archive-installed Unity `6000.3.20f1` host passed
  6/6 focused M14 tests and 466/466 complete package tests with zero failures,
  skips, or inconclusive results. A deliberately invalid `-nographics` run
  exposed the Gallery view's real graphics requirement and was not counted;
  the accepted full run used a graphics-capable Editor.
- 2026-08-04: Repository, deterministic archive, RC contract, clean-install,
  handoff, Python compilation, and diff checks passed locally. Hosted PR-head
  Unity, clean-consumer, handoff, release-bundle, and robustness artifacts
  remain required before autonomous merge.
- 2026-08-04: Two independent archive-only consumers passed 1/1 each and
  matched geometry SHA-256
  `ef61e45fcabb15cb928a9ab0af0d8bb975f0ce3ae68a832b25305152204566f4`,
  OBJ SHA-256
  `19256092d37e2080f1914aaec4f056e26d3ddb48f9b104a8ca33772ab0879822`,
  and stable-evidence SHA-256
  `fd4917ec720a1d8388971bd3db23d45b92202297861fe41c4828a85cd2ebc43d`.
  A clean producer and receiver then passed 1/1 each and matched the transferred
  source, 2,972 render vertices, 2,562 topology vertices, 5,120 triangles, and
  one closed volume with comparison evidence SHA-256
  `94e7880f56977a1cb4f137c39aa824409e7c78e5627048fc8ca74664993d7a21`.

# Decisions made

- M14 emits `1.0.0-rc.1`, not stable `1.0.0`, because all release mechanisms
  must first prove themselves against an immutable candidate.
- Only Unity `6000.3.20f1` is qualified; the manifest uses `unityRelease` to
  avoid implying that every 6000.3 patch has passed.
- API and FoldScript freeze are compatibility gates, not opportunities to add
  new behavior.
- Autonomous merge requires a public exact-head audit plus green required
  checks; credentials, legal/paid/permission decisions, and marketplace
  publication still escalate.
- Release evidence is emitted as `built-unverified`; archive construction never
  claims that Unity, CI, or maintainer audit already passed.

# Final verification

Implementation and local evidence are complete. Pending draft PR, hosted
exact-head evidence, autonomous audit, and merge. No final `1.0.0`, external
marketplace publication, new geometry, or later milestone was implemented.
