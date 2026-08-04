# M14: 1.0 release candidate

## Visible proof

1. Build `com.foldcanvas.core` version `1.0.0-rc.1` twice and obtain the same
   archive bytes, file manifest, and SHA-256 digest.
2. Install that exact archive into two independent Unity `6000.3.20f1`
   projects and reproduce public-API, production-corpus, source-handoff, and
   compiler evidence without resolving the repository checkout.
3. Regenerate the compiled Runtime API manifest and prove it matches a frozen
   RC baseline; prove canonical FoldScript `0.1` fixtures round-trip without
   semantic change.
4. Run the complete package Edit Mode suite, clean-install pair, producer /
   receiver handoff, and bounded robustness gates in hosted Unity.
5. Produce one machine-readable release-candidate evidence document tying the
   candidate version, Unity version, API digest, corpus identity, archive
   digest, checks, audit decision, and rollback target together.

## Goal

Turn the accepted M00-M13 package into an auditable release candidate without
changing its geometry or source-authority contracts. A candidate is ready only
when users can identify its exact support surface, install it from a
deterministic archive, report a reproducible defect, and return to the previous
known-good package without treating generated Meshes as source.

## Release-candidate contract

- The M14 package version is `1.0.0-rc.1`. This is pre-release evidence and is
  not the final `1.0.0` publication decision.
- `FoldCanvas.Runtime` public signatures are frozen at the reviewed RC
  baseline. An addition requires a compatible version decision; a removal or
  changed signature requires an ADR, migration notes, and a new major-version
  decision after 1.0.
- FoldScript `0.1` remains the only executable interchange format. Unknown
  versions remain stable rejections. M14 adds compatibility fixtures and gates,
  not new fields or operations.
- The only qualified Editor row is Unity `6000.3.20f1`. `package.json` declares
  `unity: 6000.3` plus `unityRelease: 20f1`; other patches are unclaimed until
  they pass the same hosted matrix.
- Canvas, panels, seams, ordered operations, and canonical FoldScript remain
  authoritative. Archives, manifests, reports, screenshots, OBJ, Prefabs, and
  Meshes are derived evidence.
- A release candidate is blocked by any failed or missing repository, archive,
  Unity, clean-install, API, corpus, handoff, robustness, license, security, or
  rollback gate.

## Work packages

### A. Version and compatibility freeze

- advance package/compiler evidence to `1.0.0-rc.1` in one reviewed iteration;
- add the exact Unity patch declaration and one-row supported matrix;
- freeze the compiled Runtime API digest and signature count;
- lock canonical FoldScript `0.1` fixtures and compatibility behavior;
- document semantic-version rules for future RC, patch, minor, and major work.

### B. Release evidence and archive contents

- emit a deterministic sorted file manifest with per-file SHA-256 and size;
- emit archive checksum plus a release-candidate evidence summary;
- prove the archive contains license, notice, security, support, changelog,
  documentation, schema, samples, and no repository or scratch content;
- keep timestamps, ownership, permissions, and entry order normalized.

### C. Supported Unity matrix

- express the exact supported row in a machine-readable matrix;
- run the full package suite and two independent clean archive installs for
  that row;
- reject missing XML, Editor log, package resolution, or version evidence;
- add a new row only after identical hosted qualification exists.

### D. User, maintainer, and rollback documentation

- provide install, first compile, validation, upgrade, rollback, and
  troubleshooting paths using 2D source plus FoldScript;
- publish support scope and required reproduction fields;
- document severity order: security, data loss/source corruption, compiler
  correctness, determinism, topology, installation, then usability/docs;
- record autonomous exact-head audit and green-check merge policy.

### E. Release workflow

- make pull requests build and validate the RC bundle without publishing it;
- keep tag-triggered GitHub release publication separate and verify tag equals
  package version exactly;
- upload archive, checksum, file manifest, candidate evidence, and logs as
  mandatory artifacts;
- do not create a final `1.0.0` tag or external marketplace submission in M14.

### F. Acceptance and self-audit

- run repository validation, deterministic archive tests, full Edit Mode
  tests, clean-install pair, handoff producer/receiver, production corpus, and
  M13 bounded robustness evidence;
- audit the exact PR head, uploaded artifacts, test totals, and unresolved
  issues;
- merge autonomously only when every required check is green and the audit is
  recorded against that exact head.

## Tests

- manifest Unity minimum resolves to exactly `6000.3.20f1`;
- RC version matches package, compiler, changelog, API, corpus, and evidence;
- Runtime API baseline detects addition, removal, and changed signature;
- canonical FoldScript `0.1` fixtures retain byte-stable canonical output;
- unknown FoldScript version remains a stable rejection with no Mesh;
- release file manifest and archive are byte-identical across two builds;
- archive contains required legal/support/security material and no forbidden
  repository, secret, generated Mesh, or `Project~` content;
- release evidence rejects a missing/failed/skipped/inconclusive gate;
- clean consumers resolve only the archive and reproduce stable evidence;
- rollback points to an existing immutable prior version/commit and restores
  source-driven compilation rather than generated Mesh state;
- all existing M00-M13 tests remain enabled.

## Non-goals

- new geometry, operations, topology repair, bevel, subdivision, smoothing,
  remesh, cleanup, CSG, or texture inference;
- changing FoldScript `0.1` or adding opaque extension payloads;
- adding Runtime filesystem/network behavior or package dependencies;
- claiming Unity versions not run in hosted evidence;
- cryptographic signing, a paid registry, marketplace packaging, or legal
  relicensing;
- publishing the final `1.0.0` release.

## Governance

The owner has delegated routine project planning, implementation, issue triage,
exact-head review, pull-request merge, and roadmap continuation to the
maintainer agent. That delegation does not include credentials, paid services,
irreversible repository permissions, legal decisions, or external marketplace
publication. Those remain explicit escalation points.

## Implementation status

Active on `codex/m14-release-candidate`, based on M13 merge `d9434be`. No M14
implementation or package-version change existed at branch creation.
