# Goal

Deliver M22 on `agent/m22-readme-proof-patch`: integrate M21's audited proof
gallery into the package READMEs as a compatible `1.0.1` patch and generate a
deterministic release-excluded repository social-preview candidate.

# User-visible proof

The README hero shows `2D canvas + FoldScript -> deterministic 3D geometry`
through two compact source/result/topology rows. A `1280 x 640` companion image
uses the same validated pixels. All claims link to a manifest that binds Unity,
source, tool, geometry, topology, and SHA-256 evidence.

# Scope

- GitHub issue #26, README/social-preview gate only
- English and Chinese README hero integration
- deterministic standard-library social-card composition and validation
- package/compiler version `1.0.1`, newest-first changelog, API/corpus header
  regeneration, and patch compatibility evidence
- M21 closeout, M22 task/roadmap documentation, repository and hosted gates

# Non-goals

- compiler, geometry, topology, UV, FoldScript, sample, shader, dependency, or
  Unity-version behavior
- edits to M21's six audited Unity proof PNGs or geometry claims
- a tag, GitHub release, repository setting change, registry, or marketplace
  publication
- issue #21 Windows evidence or later geometry milestones

# Files expected to change

- `README.md`, `README.zh-CN.md`, `CHANGELOG.md`, `package.json`
- `Runtime/Data/FoldCanvasVersion.cs`
- `Documentation~/public-runtime-api.json`
- `Documentation~/m11-production-corpus.json`
- `Documentation~/compatibility.md`, `Documentation~/roadmap.md`
- `SUPPORT.md`
- `Docs/Community/ProofGallery/README.md`
- `Docs/Community/ProofGallery/social-preview.png`
- `Documentation~/ProofGallery/*.png` byte-identical package copies used by
  the package-root READMEs
- `Scripts/generate_social_preview.py`
- `Scripts/validate_readme_proof.py`, `Scripts/test_readme_proof.py`
- `Scripts/validate_repository.py`, repository workflow wiring
- `CURRENT_TASK.md`, M21/M22 milestone files, and this plan

# Geometry invariants

- source canvases and FoldScript remain the only authoritative geometry input
- the six M21 PNGs remain byte-identical and continue to bind to the same cup
  and sphere geometry hashes and topology values
- README image targets are package-contained byte copies of those exact PNGs;
  provenance and the release-excluded social candidate use public repository
  links so installed-package READMEs contain no broken `Docs/` references
- README and social-card generation do not invoke, mutate, approximate, hide,
  or replace geometry
- public Runtime API shape remains the normalized 808-signature stable shape;
  only reviewed version literals may change from `1.0.0` to `1.0.1`
- all six production-corpus case records other than the package-version header
  remain byte-semantically unchanged
- FoldScript remains `0.1`; Unity remains `6000.3.20f1`; dependencies remain
  empty
- `v1.0.0`, RC2, and their public archive assets are immutable

# Implementation steps

1. Close M21 records and freeze M22 as a patch-version documentation gate.
2. Add concise English/Chinese hero copy, two three-column proof rows, useful
   alt text, and links to the maintained sample/provenance documentation.
3. Compose a fixed `1280 x 640` social card from decoded validated proof PNGs
   with only fixed colors/labels; encode deterministic RGBA PNG bytes.
4. Add a dependency-free validator for README placement/links/alt text,
   version agreement, M21 input hashes, output dimensions/hash, and compositor
   repeatability, plus adversarial fixtures.
5. Advance package/compiler/changelog/API/corpus version evidence to `1.0.1`
   while proving normalized API shape and corpus cases unchanged.
6. Wire repository validation and update the M21 proof host to consume the
   current archive version without weakening its geometry evidence.
7. Run complete static/archive/Unity/clean-host/proof gates and visual checks.
8. Push a PR, require green exact-head hosted checks, record an exact-head
   maintainer audit, and merge only if all protected-main requirements pass.

# Test matrix

## README and social proof

- both READMEs contain the hero statement before the long status inventory
- exactly six local proof-image references resolve and have meaningful alt text
- every claim links to M21 manifest/guide/sample documentation
- social output is `1280 x 640`, RGBA, deterministic across two builds, and
  changes/fails when a required source hash or layout contract is altered
- remote URLs, traversal, missing images, wrong dimensions, duplicate panels,
  stale hashes, unexpected PNG chunks, and absent provenance fail

## Patch compatibility

- package, compiler, changelog, public API, and corpus headers equal `1.0.1`
- normalized API signatures remain the stable 808-signature identity
- corpus case IDs, hashes, counts, diagnostics, topology, and validation remain
  unchanged
- the deterministic archive is rebuilt twice and has a new non-v1.0.0 hash

## Product regression

- repository validators and full Edit Mode suite pass
- repeated clean archive installs, producer/receiver handoff, source-first
  upgrade, and proof regeneration pass
- Published `v1.0.0` and RC2 artifacts are never edited or republished

# Risks and rollback

- **Package identity ambiguity:** advance version before merging README bytes,
  and validate a new deterministic hash without overwriting any public asset.
- **Screenshot-as-claim:** keep M21 geometry reports and manifest as the claim
  authority; README pixels alone never prove topology.
- **Social-card fabrication:** permit only fixed crop/scale composition from the
  six tracked M21 images plus fixed labels/background; validate source hashes.
- **API false break:** normalize only version literals and compare complete
  signature order/digest.
- **Scratch contamination:** never stage untracked `.meta`, Project~ scenes,
  TestResults, Library, or other user scratch.

Rollback is one revert of the M22 merge. It restores package version `1.0.0`
and removes only the README/social integration and its evidence. No geometry or
source migration is required.

# Progress log

- 2026-08-13: verified M21 merge `67fb659`, green main repository/Unity runs,
  zero open PRs, no external CLAIMs, protected main, and #21 still requiring a
  real external Windows host.
- 2026-08-13: confirmed both root READMEs are package allowlist members; chose
  compatible patch `1.0.1` instead of mutating or pretending to preserve the
  published `1.0.0` archive.
- 2026-08-13: inspected all six M21 PNGs and retained their exact bytes.

# Decisions made

- M22 is versioned `1.0.1`; `v1.0.0` remains the current published rollback
  until a future explicit release milestone qualifies/publishes another tag.
- A version constant embedded in signatures is normalized for API-shape proof;
  no public type/member shape change is accepted.
- The social-preview file is a tracked candidate only. Applying it to GitHub's
  repository setting is a distinct public presentation action outside M22.
- Social composition uses a checked-in dependency-free script and exact M21
  source hashes; no AI image generation, browser screenshot, or manual retouch.

# Final verification

Local implementation is complete. The complete repository matrix passes,
including 44 Action-pin fixtures, 72 Schema fields, 14 Roll review fixtures,
9 M21 gallery fixtures, 7 M22 README/social fixtures, deterministic archive,
stable-baseline compatibility, public-release verifier, source-first upgrade,
clean-install, handoff, trust, JSON, and diff checks. The final `1.0.1`
archive is byte-deterministic with SHA-256
`5e27d390b02c46a5bb8d7f255cfa73e6f654771bd0f750a67da6c161a07b97c3`.

Unity `6000.3.20f1` installed that exact archive into a disposable clean host
and passed 477/477 full package Edit Mode tests with zero failures, skips, or
inconclusive results. `test-results.xml` and `Editor.log` exist under
`/tmp/foldcanvas-m22-final-unity.tvLgRL/results/`. All six README package
images are byte-identical to M21, and the social candidate was visually
inspected. Hosted exact-head checks, audit, and merge remain. M22 does not
implement a new geometry operation or any later milestone behavior.
