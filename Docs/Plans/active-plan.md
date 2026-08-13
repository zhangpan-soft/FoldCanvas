# Goal

Qualify and publish the already merged M24 compiler behavior as immutable
FoldCanvas `1.1.0`, while adding a fail-closed repository gate that prevents a
published semantic version from ever naming different UPM bytes.

# User-visible proof

An exact public `v1.1.0` archive installs in two clean Unity `6000.3.20f1`
consumers. The unchanged production cup rebuilds after a source-only upgrade
from public `v1.0.1`, and the M24 off-grid Fold corpus case succeeds only in the
new minor line. Public release metadata, four assets, hashes, Unity XML/logs,
and the final qualification report are retained as reviewable evidence.

# Scope

- package/compiler/API/corpus version advance to `1.1.0`;
- immutable public-release identity ledger and offline tag rebuild validation;
- M25 minor-release contract, schema, deterministic release evidence, tests,
  exact-head authorization, tag publishing, and public qualification;
- two clean public consumers and `1.0.1` to `1.1.0` source-first upgrade;
- M24 closure evidence, compatibility documentation, README status, roadmap,
  changelog, and release guide.

# Non-goals

- new geometry, topology, FoldScript fields, dependencies, or Unity versions;
- curved/disk/branched/interior-ending/overlap crease refinement;
- topology-group deformation propagation or later per-panel deformation after
  Stitch;
- bevel, subdivision, smoothing, cleanup, SpiralRoll, or LayeredRoll;
- registry, Asset Store, or external marketplace publication;
- rewriting `v1.0.1` or any prior tag or release asset.

# Files expected to change

- `package.json`, `Runtime/Data/FoldCanvasVersion.cs`
- `Documentation~/public-runtime-api.json`, current production corpus
- new M25 contract, release ledger, schemas, release guide, task, ADR, roadmap,
  README status, compatibility policy, and changelog
- release builder/verifier/authorization and immutable-ledger validators/tests
- package, public-qualification, Unity-upgrade, long-run, and repository workflows
- version-sensitive Edit Mode release/API/corpus tests

# Geometry invariants

- M25 adds no geometry implementation; the exact M24 compiler tree is the
  candidate behavior.
- The 2D canvas plus FoldScript `0.1` remain source and generated Mesh remains
  derived.
- M24 off-grid Fold topology, source UVs, named boundary order, triangle
  winding, operation ordering, diagnostics, and deterministic hashes remain
  unchanged.
- The five pre-M24 production cases retain exact geometry and topology
  identities; only the formerly rejected off-grid Fold case changes contract.
- Source-first upgrade consumes only the maintained JSON and PNG, never Mesh,
  OBJ, Prefab, report, receipt, material, or screenshot.

# Implementation steps

1. Close M24 with exact audit/merge/hosted evidence and adopt ADR 0012.
2. Add a public-release identity ledger; rebuild every recorded tag offline and
   fail if current bytes reuse an immutable version with a different digest.
3. Advance package/compiler/API/current-corpus identity to `1.1.0` and add the
   M25 minor-release contract/schema/guide.
4. Generalize deterministic release evidence and public verification for the
   minor contract without weakening historical M15/M17/M23 validation.
5. Extend exact-head authorization, tag publication, public consumers, and
   source-first upgrade to `v1.1.0`.
6. Add adversarial repository tests and focused Unity tests for version/API,
   unchanged legacy corpus cases, and the accepted off-grid Fold case.
7. Run JSON, static, archive, workflow-parity, focused Unity, and complete Edit
   Mode validation; record exact XML/log paths and counts.
8. Commit/push, open PR, record exact-head maintainer audit, require green
   hosted checks, merge, annotate/tag, publish, qualify, and record evidence.

# Test matrix

| Case | Expected evidence |
|---|---|
| rebuild public `v1.0.1` | exact `4188d23b...` archive |
| current tree says `1.0.1` | rejected because current archive differs |
| ledger tag/commit/digest drift | deterministic validation failure |
| current `1.1.0` archive twice | byte-identical archive/manifest/evidence |
| Runtime API | 808 signatures; normalized digest unchanged |
| five legacy corpus cases | exact prior geometry/topology hashes |
| `off-grid-fold` | success, 7 vertices, 6 triangles, no error |
| wrong tag/audit/head/workflow | publication authorization rejected |
| clean public consumers A/B | matching package and geometry evidence |
| `1.0.1 -> 1.1.0` source upgrade | same cup source and generated result |
| full suite | all prior tests enabled and green |

# Risks and rollback

- Risk: historical validators assume current `1.0.1`. Mitigation: separate
  historical identity checks from current-line checks; never rewrite contracts.
- Risk: a current builder cannot reproduce an older archive. Mitigation: the
  ledger rebuilds each tag using scripts from that tag.
- Risk: a generalized release path accidentally authorizes arbitrary versions.
  Mitigation: exact contract/tag allowlists and one-version authorization tests.
- Risk: source-upgrade logic assumes patch-only lineage. Mitigation: require an
  exact contracted target and semantically newer baseline, independent of
  major/minor equality.
- Rollback: abandon the M25 branch before publication. After publication, use
  immutable `v1.0.1` and rebuild from authoritative 2D source.

# Progress log

- 2026-08-13: M24 exact head `c9845d7` was audited, merged through PR #41 as
  `7ffa350`, and passed protected-main repository, Unity, and long-run checks;
  Unity passed 491/491 tests.
- 2026-08-13: no open PR, release blocker, or external contributor claim
  preempted roadmap work; Windows issue #21 still requires a real Windows host.
- 2026-08-13: current main rebuilt as archive SHA-256 `1b00d79c...` while still
  identifying as public `1.0.1`; exact `v1.0.1` rebuilt as `4188d23b...`.
- 2026-08-13: selected `1.1.0` because M24 adds backward-compatible compiler
  behavior, and adopted an immutable-release ledger rather than a one-off fix.
- 2026-08-13: implemented the M25 contract, schemas, historical-tag rebuild
  gate, generalized deterministic release/public qualification paths, exact-head
  authorization, and version/API/corpus regression tests without changing
  geometry code or FoldScript.
- 2026-08-13: repository-workflow parity passed all 31 validation entrypoints;
  all four public tags rebuilt to their recorded archive digests, and the
  deterministic `1.1.0` archive repeated byte-for-byte; its final digest is
  recomputed from the frozen PR head rather than treated as authored source.
- 2026-08-13: the complete Unity `6000.3.20f1` Edit Mode suite passed 495/495
  with zero failures, skips, or inconclusive results in an isolated host.

# Decisions made

- A released semantic version identifies immutable bytes, not merely API shape.
- Main must advance version before the first packaged post-release change.
- M24 is a backward-compatible operation-domain expansion, so SemVer minor
  `1.1.0` is required; it is not a patch and not a breaking major release.
- Public `v1.0.1` is the direct rollback and source-upgrade baseline.
- GitHub release publication is in scope; registries and marketplaces are not.

# Final verification

Local implementation verification is complete:

- workflow YAML parsing, action-pin validation, repository validation, JSON,
  schema/field coverage, proof, release, upgrade, clean-install, handoff, and
  trust-boundary parity: 31/31 entrypoints passed;
- immutable public identities: 4/4 tags rebuilt to their recorded archive
  SHA-256 values;
- deterministic current archive: identical repeated output; the exact SHA-256
  is emitted from the frozen PR head and verified again at publication;
- Unity Editor: `6000.3.20f1 (c9ba695d4f07)`;
- full Edit Mode: 495/495 passed, 0 failed, 0 skipped, 0 inconclusive;
- local XML: `/tmp/foldcanvas-m25-unity-all.xml`;
- local Editor log: `/tmp/foldcanvas-m25-unity-all.log`;
- `git diff --check`: passed.

Hosted PR checks, exact-head audit, protected-main merge, tag/release identity,
four public asset digests, two public consumers, source-upgrade comparison, and
post-publication qualification remain pending. Later geometry milestones were
not implemented.
