# Goal

Deliver M11 on `codex/m11-production-readiness`: prove that the deterministic
FoldCanvas release archive installs and works in a clean consumer Unity project,
freeze a reviewable public Runtime API baseline, and establish compatibility,
trust, corpus, and hosted release gates without adding new geometry behavior.

# User-visible proof

One command builds the UPM archive, creates a temporary Unity `6000.3.20f1`
consumer project, resolves only that archive, compiles consumer-owned code, and
emits a deterministic proof report plus NUnit XML and Editor log. The report
contains package/source/geometry/OBJ/diagnostic hashes and is reproducible on a
second clean run. A separate maintained corpus report covers representative
valid and invalid FoldCanvas sources.

# Scope

- clean Unity host generation from the freshly built `.tgz`
- consumer-owned public-API compile/export smoke test
- public Runtime API signature manifest and compatibility gate
- production acceptance corpus and deterministic evidence hashes
- native-extension trust model and compatibility/migration documentation
- independent hosted clean-install job and mandatory artifacts
- package version, CHANGELOG, roadmap, README, and release-gate documentation

# Non-goals

- new geometry operations, panels, wraps, or source topology semantics
- topology mutation through public extensions
- CSG, bevel, smoothing, subdivision, remesh, or cleanup
- glTF/FBX/USD/Blender integration
- runtime filesystem, network, AI provider, or credential behavior
- a `1.0.0` release or marketplace publication
- milestones after M11

# Files expected to change

- `CURRENT_TASK.md`, `Codex/M11_PRODUCTION_READINESS.md`
- `Docs/ADR/0009-production-readiness-evidence-gates.md`
- `Docs/Plans/active-plan.md`, governance and roadmap documents
- clean-host/project generation and evidence-validation scripts under `Scripts/`
- public API/corpus baseline files under `Documentation~/`
- consumer smoke fixtures and M11 Edit Mode tests
- GitHub Actions workflows for clean-archive installation
- package version, `CHANGELOG.md`, English/Chinese README, documentation index

# Geometry invariants

- Canvas, FoldScript, panels, seams, and ordered operations remain source.
- Clean-host reports, OBJ text, Meshes, package archives, hashes, and logs remain
  derived evidence.
- M11 does not change mapping equations, triangle/vertex order, UV, provenance,
  topology identity, winding, validation tolerances, or operation order.
- Repeated corpus and consumer compiles use identical source and settings and
  must emit identical topology, UV, diagnostic, and OBJ evidence.
- The clean host resolves `com.foldcanvas.core` from the built archive and may
  not fall back to the repository-root package.
- Consumer proof code references public package assemblies only.
- Public API signatures are normalized and sorted ordinally; process order,
  locale, filesystem order, and reflection enumeration order cannot affect the
  manifest.
- Native extensions are trusted in-process code. Capability limitation is an
  API contract, not an OS sandbox claim.

# Implementation steps

1. Record the owner-delegated maintenance model, complete M10 maintainer audit,
   merge PR #9, fast-forward `main`, and create the M11 branch/spec/ADR/plan.
2. Audit the release archive and package metadata for repository-relative
   assumptions; define the consumer report and API signature formats.
3. Add deterministic public Runtime API signature generation plus baseline and
   Edit Mode/static checks.
4. Add a clean-host generator that installs the newly built archive and creates
   a consumer-owned assembly/test without copying package implementation.
5. Add the consumer proof: authoritative 2D source, successful compile, strict
   validation where applicable, deterministic OBJ, and evidence report.
6. Add the production acceptance corpus with stable valid/invalid hashes and
   repeat-compile tests.
7. Add the independent hosted clean-install workflow, normalize evidence, and
   require XML, Editor log, resolution manifest, and consumer report artifacts.
8. Add compatibility, migration, trust-boundary, issue-triage, and release-gate
   documentation; update roadmap and README files.
9. Advance the preview package version and update `CHANGELOG.md` newest-first.
10. Run JSON/asmdef/runtime-isolation checks, deterministic archive validation,
    focused M11 tests, full Edit Mode tests, two clean installations, corpus
    verification, and any required foreground proof.
11. Commit/push the isolated branch, open a PR, verify every hosted job and
    artifact, record a maintainer audit, and merge only if no blocking finding
    remains.

# Test matrix

## Public API

- API manifest is identical across repeated generation
- API signatures are ordinal and locale independent
- removed/changed public signatures fail with actionable evidence
- internal and Editor-only types never enter the Runtime API manifest
- an intentional baseline update requires version/migration metadata

## Clean installation

- manifest resolves the freshly built `.tgz`
- Unity reports `LocalTarball` and resolves the package into the generated
  host's own `Library/PackageCache`, never the repository package root
- consumer assembly compiles with public references only
- consumer source compiles twice to identical geometry/OBJ evidence
- missing archive, resolution fallback, missing XML/log/report, or failed Unity
  startup fails the job

## Production corpus

- planar, cup, sphere, torus, and registered-wave cases retain expected counts
  and stable hashes
- valid closed cases retain required topology/volume/validation evidence
- expected-invalid case returns its stable diagnostic and no Mesh
- corpus order and diagnostic order are deterministic

## Regression

- all 394 existing M00-M10 tests remain enabled
- Runtime remains free of `UnityEditor`, network services, and new dependencies
- deterministic release archive remains byte-identical
- user-owned untracked `Project~` scenes and historical test evidence remain
  untouched and uncommitted

# Risks and rollback

- **False clean install:** generated host records both package source and
  resolved path, requires `LocalTarball` under its own `Library/PackageCache`,
  and rejects fallback to the repository package root.
- **Brittle API snapshots:** normalize generic/nested/array/ref signatures and
  sort ordinally; report signature differences rather than only a hash.
- **Machine-specific evidence:** topology and text hashes are gates; timings are
  informational and Unity version is recorded explicitly.
- **Corpus becomes generated source:** store canonical source identity and
  expected evidence, never accept a generated Mesh as the editable fixture.
- **Extension security overclaim:** document trusted-code boundaries; do not
  claim containment against arbitrary code already loaded in Unity.
- **CI cost:** keep clean-install evidence in a separate job and preserve the
  faster repository/package jobs.
- Rollback is reverting isolated M11 commits. M10 remains merged at `b67120f`.

# Progress log

- 2026-08-03: Owner delegated ongoing FoldCanvas roadmap, implementation,
  issue-triage, PR, merge, and release maintenance toward production readiness.
- 2026-08-03: Created an active long-running goal and a six-hour GitHub/roadmap
  heartbeat for issue, PR, CI, and milestone maintenance.
- 2026-08-03: Confirmed PR #9 had no review threads or issues, both hosted
  workflows were green, and the Unity artifact contained real XML/Editor.log
  evidence with 394/394 passing tests.
- 2026-08-03: Audited exact M10 head `34a59b0`: no blocking architecture,
  rollback, path, exporter, package, or CI finding. Recorded the audit in PR #9
  with the explicit native-extension trusted-code follow-up.
- 2026-08-03: Merged PR #9 and fast-forwarded `main` to merge commit `b67120f`.
- 2026-08-03: Created `codex/m11-production-readiness`, M11 specification, ADR
  0009, and this execution plan.
- 2026-08-03: Implemented the clean-archive host generator, consumer-owned API
  smoke fixture, resolution/evidence validators, and independent two-host
  evidence comparison.
- 2026-08-03: Added the 796-signature public Runtime API baseline, six-case
  production corpus, compatibility and production-readiness documentation,
  repository gates, and hosted clean-install workflow.
- 2026-08-03: Built final archive SHA-256
  `03236479e585eafcf7d57d59cfc442fc6b6f95ef782195a861fbdb94fead842e`;
  Unity `6000.3.20f1` passed all 401 package Edit Mode tests and both clean
  consumer hosts passed 1/1 with identical stable evidence.

# Decisions made

- Production readiness is not another geometry family. The next proof is clean
  consumption, compatibility, deterministic corpus evidence, and fault-visible
  hosted installation.
- The UPM archive, not the repository root, is the clean-host installation
  input.
- Public API stability is measured from the compiled Runtime assembly; source
  text heuristics are insufficient.
- A capability-limited extension API is not a security sandbox. Native
  executors are trusted contributor code and the documentation must say so.
- M11 prepares release-candidate gates but does not publish `1.0.0`.

# Final verification

Local implementation evidence is complete. Hosted PR evidence and maintainer
audit remain pending before merge.

- Unity `6000.3.20f1`: 401 passed, 0 failed, 0 skipped, 0 inconclusive
- two independent archive consumers: 1/1 passed in each host
- clean pair stable evidence SHA-256:
  `189b5629700f63029cad05708035d39b878029315f86335eee88ac1dc86e2bf9`
- public Runtime API: 796 signatures, digest
  `22f10070f633b216a7ca1eb0721cc6cab1ed792ce1db2a9304faa0dedead47d0`
- production corpus: six cases, five valid and one expected FC3011 invalid
- repository validation, clean-project static tests, release-package tests,
  workflow YAML parsing, and `git diff --check`: passed locally

Remaining hosted evidence:

- hosted workflow run IDs plus XML, Editor log, resolution, consumer, and corpus
  artifact presence
- foreground evidence only for claims that depend on rendering or interaction
