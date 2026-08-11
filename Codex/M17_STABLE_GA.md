# M17: stable `1.0.0` general availability

## Visible proof

1. The immutable RC2 candidate has one machine-readable stable-exit report
   whose status is `ready`, with 172.5 soak hours, two qualifying scheduled
   runs, 14/14 reviewed gates, and zero open release blockers.
2. The package advances to semantic version `1.0.0` without changing geometry,
   FoldScript `0.1`, source UVs, topology, dependencies, or the qualified Unity
   row.
3. A `v1.0.0` tag on the exact audited merge builds one deterministic archive,
   publishes a non-draft non-prerelease GitHub release, and dispatches public
   qualification only after the release exists.
4. Public qualification downloads the exact stable assets, verifies their
   digests and stable evidence, installs the archive into two clean Unity
   `6000.3.20f1` consumers, and recompiles unchanged source after upgrading
   from immutable RC2.

## Goal

Promote the qualified RC2 lineage to a real stable UPM package while preserving
the source-first architecture and making the promotion evidence reproducible.
M17 is a release and compatibility milestone, not a geometry milestone.

## Stable promotion contract

- `Documentation~/m17-stable-release.json` binds the stable package/tag,
  immutable RC2 public identity, exact M16 readiness workflow/artifact/report,
  version-neutral public API shape, production-corpus identities, rollback,
  and pre/post-publication gates.
- The M16 readiness evidence is accepted only at workflow run `31501082596`,
  artifact `9105046023`, report SHA-256
  `c581d89bb45a6269d183ff73d881d450f20c49e0dbd565e679ac9a922f779ad4`,
  and artifact digest
  `945b75d662c0eb39395eae03b1ec32cefd8e2d6d8b2a83f759c1aa7c3dfc37b0`.
- The stable tag must exactly equal `v` plus `package.json.version` and point to
  the audited merge. Release creation remains tag-driven and fail-closed.
- RC1 and RC2 releases and assets are immutable historical inputs. They are
  never rewritten, relabeled, or reused under a different filename.
- Stable package bytes are new because the semantic version, compatibility
  documentation, baseline manifests, and stable evidence change. Runtime
  geometry behavior and authored source do not.

## Compatibility contract

- Public Runtime API type/member shape must equal RC2 after replacing only the
  literal package version in version-bearing signatures. The normalized
  signature digest is frozen in the stable contract.
- The six production-corpus cases retain identical ordered source, geometry,
  OBJ, diagnostic, count, and topology evidence. Only their package-version
  header advances.
- FoldScript remains `0.1`; no converter or relaxed reader is introduced.
- Unity `6000.3.20f1` remains the sole fully qualified Editor patch.
- `1.0.0-rc.2` is the first stable rollback and the required source-first
  upgrade baseline.

## Work packages

### A. Freeze M16 readiness

- record the exact ready report, input hash, workflow/artifact identity, and
  RC2 lineage;
- validate the downloaded report independently and reject any other run,
  artifact, candidate, target, gate count, or non-ready status.

### B. Stable package identity

- advance package and compiler version constants to `1.0.0`;
- regenerate compiled Runtime API and production-corpus baselines in Unity;
- add a stable release contract, schema, guide, tests, and changelog entry;
- preserve version-neutral API shape and every geometry/corpus identity.

### C. Deterministic stable bundle

- emit stable evidence rather than candidate evidence when the current package
  is `1.0.0`;
- bind archive, manifest, stable contract, readiness report, rollback, gates,
  API, corpus, and source-upgrade policy;
- retain deterministic ordering, normalized archive metadata, and exact tag
  matching.

### D. Stable publication and public qualification

- allow only the reviewed `v1.0.0` stable tag in addition to historical RC
  tags;
- download and validate the exact M16 ready artifact before publishing stable;
- publish stable without the GitHub prerelease flag;
- make the public verifier derive expected prerelease/stable state from the
  selected contract;
- run two public clean consumers plus RC2-to-stable source-first upgrade and
  upload complete XML/log/evidence artifacts.

### E. Acceptance and autonomous audit

- run JSON/YAML, repository, package, readiness, public-verifier, upgrade,
  action-pin, link, and diff checks locally;
- run full hosted Unity, clean-install, handoff, upgrade, and long-run gates;
- audit the exact PR head and merge only with required checks green;
- create and push `v1.0.0` only from the audited merge, then independently
  inspect the public release and qualification artifacts.

## Tests

- stable readiness accepts only the exact ready report and rejects blocked,
  wrong-run, wrong-candidate, wrong-target, stale-gate, and changed-byte input;
- deterministic bundle output is byte-identical and contains stable evidence;
- stable evidence reports `stableRelease: true`, final GitHub publication, and
  RC2 rollback while forbidding marketplace claims;
- public verifier accepts stable non-prerelease metadata and continues to
  accept immutable RC2 prerelease fixtures;
- stable package/API/corpus tests prove the version-only compatibility delta;
- all existing M00-M16 behavior tests remain enabled.

## Non-goals

- geometry, topology, FoldScript, Runtime API shape, dependency, Unity-version,
  render-pipeline, marketplace, signing, registry, or paid-service changes;
- mutating RC2, treating generated Meshes as source, or bypassing protected
  `main` and hosted CI;
- Bevel, Subdivision, Smooth, cleanup post-processing, or a new operation.

## Governance

Routine implementation, exact-head audit, merge, GitHub stable release, public
qualification, issue triage, and roadmap continuation are delegated to the
autonomous maintainer. Credentials, paid services, irreversible permission
changes, legal decisions, and external marketplace publication remain owner
escalation points.

## Implementation status

M16 evaluator run `31501082596` completed successfully at
`2026-08-11T14:21:46Z`. Its downloaded artifact digest matched GitHub metadata,
and `report.json` is `ready` at 172.5 hours, 2/2 qualifying scheduled runs,
14/14 gates, and zero open release blockers. Stable package implementation,
static validation, Unity baseline regeneration, and 477/477 local Edit Mode
tests are complete. Hosted verification, exact-head audit, merge, tag, public
release, and public qualification remain pending.
