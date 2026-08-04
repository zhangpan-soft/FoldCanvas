# Goal

Deliver M15 on `codex/m15-public-distribution`: qualify the exact public
GitHub release bytes and a source-first upgrade path, then emit a fail-closed
stable-release exit gate without changing geometry or FoldScript semantics.

# User-visible proof

An ordinary consumer downloads the public RC bundle, verifies its checksum and
every archived file, installs the `.tgz` into two independent Unity
`6000.3.20f1` projects, and reproduces deterministic Runtime API, geometry,
OBJ, corpus, and handoff evidence. A separate upgrade rehearsal preserves the
canonical 2D/FoldScript source while rebuilding every derived artifact under
the new package. A machine-readable exit report explains exactly why stable
`1.0.0` is still blocked or ready.

# Scope

- close M14 with exact audit, merge, tag, release, and public asset evidence
- preserve published `1.0.0-rc.1` immutability
- advance the first packaged M15 iteration to `1.0.0-rc.2`
- exact public-release asset verification and deterministic report
- two real clean Unity consumers resolving only the downloaded public archive
- source-first upgrade rehearsal from the rollback baseline to the current RC
- stable-release soak/issue/run exit-gate contract
- full hosted regression and autonomous exact-head audit

# Non-goals

- final `1.0.0` publication or external marketplace submission
- new geometry behavior, operation, topology mutation, or automatic repair
- FoldScript schema/semantic changes or migration guessing
- use of Mesh, OBJ, Prefab, Material, receipt, report, or screenshot as upgrade
  source
- new Runtime filesystem/network behavior or package dependency
- support claims for an untested Unity version
- signing, paid service, legal change, or irreversible permissions
- later-milestone implementation

# Files expected to change

- `CURRENT_TASK.md`
- `Codex/M15_PUBLIC_DISTRIBUTION.md`
- `Docs/Plans/active-plan.md`
- `Documentation~/roadmap.md`
- `Documentation~/production-readiness.md`
- `Documentation~/compatibility.md`
- `Documentation~/m15-public-distribution.json`
- `Schema/foldcanvas-public-release-evidence.schema.json`
- `Scripts/` public-release, upgrade, and exit-gate validators
- `.github/workflows/` public release qualification
- `Tests/Editor/` M15 contract and upgrade tests
- package/compiler/API/corpus/changelog version evidence for `1.0.0-rc.2`

# Geometry invariants

- Appearance canvas plus FoldCanvas/FoldScript source remain authoritative;
  all Meshes and release/upgrade reports remain derived.
- M15 changes no coordinate equation, winding rule, boundary order, seam
  topology, tolerance, tessellation, or validation semantics.
- Identical source/settings/compiler still produce the same ordered geometry,
  UV, provenance, topology, diagnostics, and reports.
- Runtime remains free of `UnityEditor`, filesystem, network, release, and
  migration behavior.
- Upgrade recompiles source after discarding derived output; it never imports
  old Mesh vertices or indices.
- Unsupported version transitions fail explicitly and generate no Mesh.

# Implementation steps

1. Record M14 exact-head audit, merge, tag, public release, and hosted evidence;
   create the M15 milestone and active plan outside the immutable RC1 package.
2. Inventory release assets, package builder, clean-host generator, handoff
   compatibility, API/corpus baselines, upgrade docs, and workflow permissions.
3. Advance every packaged M15 change together to `1.0.0-rc.2`; add an
   immutability regression for the published RC1 digest.
4. Define public-release verification and stable-exit machine-readable
   contracts plus JSON Schemas before implementing their runners.
5. Implement a fail-closed public asset verifier with exact allowlist,
   tag/version, checksum, manifest, archive-entry, evidence, and source-origin
   checks.
6. Generate two independent clean hosts from only the verified public archive;
   validate package resolution, Runtime API usage, geometry/OBJ evidence, XML,
   Editor logs, and pair equality.
7. Implement the source-first upgrade rehearsal: preserve canonical source and
   PNG hashes, remove prior derived outputs, replace the package, recompile,
   and compare complete semantic evidence.
8. Implement the stable-exit validator for soak time, distinct scheduled
   long-runs, issue state, exact-head audit, public consumers, and upgrade
   evidence. Keep stable publication disabled while incomplete.
9. Run JSON, asmdef, repository, archive, clean-install, handoff, Python, and
   `git diff --check` validation locally.
10. Run complete hosted Unity, public-consumer, upgrade, handoff, corpus, API,
    and 512-case/5-resource evidence; record exact totals and artifact digests.
11. Open the M15 PR, audit the exact head and issue/check state, merge only when
    every required gate is green, publish RC2, and independently verify the
    public assets and post-release consumer workflow.

# Test matrix

## Version immutability

- recorded public RC1 archive/manifest/evidence digests remain unchanged
- packaged M15 bytes require `1.0.0-rc.2`
- package, compiler, changelog, API, corpus, and M15 contracts agree
- tag must exactly equal the package version

## Public asset verification

- exact four-asset allowlist succeeds
- missing, duplicate, renamed, extra, zero-byte, or mismatched assets fail
- checksum, GitHub digest, manifest order/count, member size/hash, and evidence
  linkage all agree
- traversal, link, forbidden path, stale version, and wrong tag fail closed
- report contains stable identities but no token or temporary signed URL

## Unity consumers and upgrade

- complete package suite passes on Unity `6000.3.20f1`
- two public-archive consumers use distinct PackageCache paths and reproduce
  identical evidence
- source and PNG hashes are identical before and after upgrade
- no prior Mesh/OBJ/Prefab/Material/receipt becomes compiler input
- geometry, diagnostics, topology, and validation evidence match the frozen
  semantics
- incompatible FoldScript/handoff versions remain stable rejections

## Stable exit and governance

- fewer than seven elapsed days fails
- fewer than two distinct scheduled long-run identities fails
- duplicate, wrong-commit, failed, skipped, or inconclusive runs fail
- any open release-blocking issue fails
- exact-head audit is invalidated by a later commit
- credentials and external marketplace actions remain out of scope

# Risks and rollback

- **Same version, different bytes:** bump to RC2 before any packaged change and
  lock RC1 public digests in tests.
- **CI artifact mistaken for public distribution:** require GitHub release URL,
  tag, asset identity, and downloaded digest in separate evidence.
- **Network flake mistaken for invalid package:** separate bounded transport
  retries from cryptographic/content validation and preserve the exact error.
- **Upgrade silently consumes derived geometry:** generate evidence from source
  hashes and a clean compile after owned derived-output removal.
- **Premature stable label:** make soak, scheduled runs, issue state, and audit
  machine-readable fail-closed inputs; M15 cannot publish `1.0.0`.
- **User scratch contamination:** stage explicit paths only; never include the
  user's untracked `Project~` files.
- Rollback is reinstalling immutable `v1.0.0-rc.1` or the M14 rollback
  `0.1.0-preview.21`, then recompiling authoritative source. Generated artifacts
  are never rollback input.

# Progress log

- 2026-08-04: M14 PR #13 exact head `7311771a2907b8ef58851185698ab4d62d91e6b4`
  passed repository, package, 466/466 Edit Mode, two-clean-install,
  producer/receiver handoff, 512/512 robustness, and 5/5 resource gates. The
  autonomous audit was recorded publicly and the PR merged as `a8c81e6`.
- 2026-08-04: Published `v1.0.0-rc.1` as a GitHub pre-release through run
  `30888055335`. The public archive digest is
  `ff3a065eec3a638701ff51d4f069684df4f075226305253ae04fd6ed2b250fdd`;
  release-asset digests match the reviewed candidate bundle.
- 2026-08-04: Created `codex/m15-public-distribution` and defined the public
  asset, upgrade, immutable-version, stable-exit, test, and rollback contracts
  before packaged implementation. No RC2 or stable version change existed at
  plan creation.
- 2026-08-04: Implemented the exact public-asset verifier, two public consumer
  workflow, marked source-first upgrade generator/advancer, real Unity evidence
  validators/comparator, and deterministic stable-exit evaluator. Static
  repository/release/consumer/handoff/negative validation passes.
- 2026-08-04: Local Unity `6000.3.20f1` package archive passed 472/472 Edit Mode
  tests with a graphics device. The first `-nographics` attempt correctly
  failed the gallery-window visual contract and was not treated as success.
- 2026-08-04: A clean upgrade host passed 1/1 on exact rollback
  `0.1.0-preview.21`, removed only its owned derived state, then passed 1/1 on
  RC2 with identical source, appearance, geometry, OBJ, diagnostic, topology,
  validation, and single-closed-volume evidence.
- 2026-08-04: Exact head `4729ab93ed37f0bc59546eb3e1b2464f9b310959`
  passed all hosted gates and the recorded maintainer audit, PR #14 merged as
  `4db988f`, and package run `30898124157` published `v1.0.0-rc.2`. Because a
  release created with `GITHUB_TOKEN` does not emit a second workflow run, the
  first public qualification was explicitly dispatched as run `30898280828`.

# Decisions made

- M15 uses `1.0.0-rc.2` for packaged changes because a published version is an
  immutable byte identity; RC1 will not be rebuilt or overwritten.
- Public-download proof is a distinct evidence rung above workflow artifacts.
- Upgrade operates on 2D appearance plus FoldScript source and recompiles from
  scratch; exact-version handoff rejection remains deliberate.
- Final stable release is a later milestone after a minimum seven-day RC soak,
  two distinct scheduled long-run passes, zero open release blocker, and a
  complete machine-readable exit gate.
- M15 is a distribution/upgrade qualification milestone, not a geometry
  feature milestone.
- Package publication will explicitly dispatch
  `public-release-qualification.yml` with `GITHUB_TOKEN` and `actions: write`.
  GitHub documents `workflow_dispatch` as an allowed workflow-to-workflow
  exception, whereas relying on the `release` event from the same token leaves
  public qualification silently unstarted.

# Final verification

Packaged implementation, RC2 version advance, static validation, local 472/472
Unity regression, and local source-first upgrade evidence are complete. Hosted
exact-head CI/audit, merge, RC2 publication, public-asset download, two public
consumers, repeated public-package upgrade, and the blocked stable-exit snapshot
remain pending. No final `1.0.0`, external marketplace publication, new
geometry, or later milestone was implemented.
