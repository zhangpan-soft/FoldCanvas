# M15: public distribution and upgrade qualification

## Visible proof

1. Publish `com.foldcanvas.core` version `1.0.0-rc.2` from an exact reviewed
   tag without changing the already published `1.0.0-rc.1` release assets.
2. Download the four public GitHub release assets as an ordinary consumer,
   verify the checksum and every manifest entry, and install the downloaded
   `.tgz` into two independent Unity `6000.3.20f1` projects.
3. Rebuild the maintained source corpus and one transferable production cup
   from canonical FoldScript plus exact PNG bytes without a repository package
   fallback or reuse of a producer Mesh.
4. Rehearse an upgrade from the known-good `0.1.0-preview.21` source contract
   through the current RC: preserve source bytes and identity, discard derived
   artifacts, recompile, and compare deterministic geometry, diagnostics,
   topology, and validation evidence.
5. Produce one machine-readable stable-release exit report that stays blocked
   until public-asset consumers, upgrade evidence, exact-head CI, issue state,
   and the documented RC soak window are all satisfied.

## Goal

Close the gap between “the repository built an archive” and “a user installed
the exact public bytes.” M15 qualifies public distribution and source-first
upgrade behavior while preserving every M00-M14 compiler, topology,
determinism, API, FoldScript, and source-authority contract.

## Distribution and upgrade contract

- Published versions are immutable. Because `v1.0.0-rc.1` already exists, the
  first M15 change that enters the UPM package advances package/compiler,
  changelog, API evidence, corpus evidence, and release contracts together to
  `1.0.0-rc.2`.
- The public `.tgz`, checksum, per-file manifest, and candidate evidence must
  agree byte-for-byte. A workflow artifact or local rebuild cannot stand in for
  the public release download proof.
- Public consumers resolve only the downloaded archive under their own
  `Library/PackageCache`; they must not resolve the repository checkout.
- Upgrade input is authoritative 2D appearance plus FoldCanvas/FoldScript
  source. Mesh, OBJ, Prefab, Material, validation report, receipt, screenshot,
  and prior PackageCache contents are discarded or treated only as evidence.
- FoldScript remains `0.1`; M15 adds no migration guessing. Unsupported source
  versions still fail closed with stable diagnostics and no Mesh.
- Geometry and topology semantics remain frozen. An actionable defect found
  during qualification is fixed in a separate scoped RC iteration with its own
  exact-head evidence; it is not hidden by baseline regeneration.
- Final `1.0.0` is not created in M15. It remains blocked until at least seven
  calendar days after the latest RC publication, two distinct scheduled
  long-run passes on that RC lineage, no open release-blocking issue, and all
  machine-readable exit gates pass.

## Work packages

### A. M14 closure and immutable-version guard

- record the exact M14 audit, merge, tag, release, workflow, artifact, and
  digest evidence outside the immutable RC1 package;
- detect an attempt to build different package bytes under an already
  published package version;
- advance packaged M15 work to `1.0.0-rc.2` before modifying package content.

### B. Public release asset verifier

- download only the exact expected tag assets;
- reject redirects or final filenames that do not match the allowlist;
- verify archive checksum, release-asset digests, manifest order/count,
  per-entry size/SHA-256, evidence links, package version, and tag identity;
- emit a deterministic verification report without embedding credentials or
  temporary URLs.

### C. Public archive consumers

- generate two independent clean hosts from the verified public `.tgz`;
- run consumer-owned Runtime API compilation and deterministic geometry/OBJ
  evidence in real Unity `6000.3.20f1`;
- require distinct PackageCache paths, matching semantic evidence, real XML,
  and non-empty Editor logs;
- preserve the current clean-install test rather than replacing it.

### D. Source-first upgrade rehearsal

- preserve canonical source and exact PNG identity across package replacement;
- remove or ignore all prior derived output before recompilation;
- compare deterministic source, geometry, OBJ, diagnostics, validation,
  topology, and closed-volume evidence;
- prove an incompatible handoff/archive version remains an explicit rejection
  instead of silently importing old derived geometry.

### E. Stable exit and soak evidence

- define a machine-readable exit-gate document with RC tag/commit/digests,
  minimum soak duration, scheduled-run identities, issue snapshot, public
  consumer evidence, upgrade evidence, and audit decision;
- fail closed for missing, duplicate, stale, failed, skipped, inconclusive, or
  wrong-commit evidence;
- keep final stable publication disabled while the gate is incomplete.

### F. Acceptance and autonomous audit

- run repository, archive, full Edit Mode, clean archive, public archive,
  handoff, corpus, API, upgrade, and bounded robustness gates;
- inspect open issues, pull requests, exact-head workflows, uploaded artifacts,
  release assets, and diff security boundaries;
- merge and publish the RC autonomously only after a public audit against the
  exact head; any later head invalidates that audit.

## Tests

- RC1 public asset digests remain immutable and match the recorded release;
- all packaged M15 changes use `1.0.0-rc.2` rather than republishing RC1;
- wrong tag, missing asset, duplicate asset, bad checksum, bad manifest entry,
  path traversal, stale evidence, and package/tag mismatch fail before Unity;
- two public-archive consumers pass and reproduce identical evidence;
- a repository checkout or workflow artifact cannot satisfy the public-release
  source identity;
- upgrade preserves canonical FoldScript and PNG bytes and does not reuse Mesh;
- current compiler output remains deterministic after prior-package cleanup;
- incompatible FoldScript/handoff version still produces one stable rejection;
- stable exit remains blocked before seven days, with fewer than two distinct
  scheduled long runs, or with any open release blocker;
- all existing M00-M14 tests remain enabled.

## Non-goals

- new geometry, operations, topology repair, bevel, subdivision, smoothing,
  remesh, cleanup, CSG, or texture inference;
- FoldScript schema or semantic changes;
- treating generated Mesh, OBJ, Prefab, receipt, or report as migration source;
- claiming Unity versions not run in hosted evidence;
- signing, paid registry/service, legal relicensing, irreversible repository
  permissions, or external marketplace publication;
- publishing final `1.0.0` or implementing the later stable-release milestone.

## Governance

Routine planning, implementation, issue triage, GitHub pre-release
publication, exact-head review, merge, and roadmap continuation are delegated
to the autonomous maintainer. Credentials, paid services, irreversible
permissions, legal decisions, and external marketplace publication remain
explicit owner escalation points.

## Implementation status

Implementation is active on `codex/m15-public-distribution`, based on M14
merge `a8c81e61175dafbc48d1750de7ef6823589517a6`. The package is now
`1.0.0-rc.2`; public-asset, clean-consumer, source-upgrade, stable-exit,
workflow, schema, documentation, and regression contracts are implemented.
Local Unity `6000.3.20f1` passed 472/472 package tests and the real
`0.1.0-preview.21` to RC2 source-only upgrade passed 1/1 before and 1/1 after.
Hosted exact-head evidence, audit, merge, RC2 publication, public-download
qualification, and the blocked stable-exit snapshot remain required.
