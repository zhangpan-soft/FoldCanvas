# Goal

Deliver M18 on a dedicated branch: modernize FoldCanvas GitHub Actions for the
Node 24 runner transition without changing the stable package, qualified Unity
row, trust boundary, or evidence coverage.

# User-visible proof

The same protected pull-request and main workflows complete with their current
artifacts and numerical evidence, migrated official Actions no longer emit
avoidable Node.js 20 deprecation warnings, and a deterministic package rebuild
still has the public `v1.0.0` archive SHA-256
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.

# Scope

- GitHub issue #25
- official upstream release, tag, commit, and runtime-metadata review
- immutable full-SHA Action allowlist and inline version-label updates
- adversarial Action-pin validator coverage
- workflow events, permissions, branch/fork guards, artifacts, and evidence
- explicit supported-path decision for `game-ci/unity-test-runner`
- complete local and hosted regression verification

# Non-goals

- package version or any package-included file
- Runtime, Editor, Tests, Schema, `Documentation~`, geometry, topology,
  FoldScript, source assets, tolerances, dependencies, or Unity Editor changes
- floating tags, unreviewed forks, warning suppression, or reduced CI gates
- public asset replacement, new release, registry, or marketplace publication

# Files expected to change

- `.github/workflows/*.yml`
- `Scripts/validate_action_pins.py`
- `Scripts/test_action_pins.py`
- `Scripts/validate_repository.py`
- `Docs/github-setup.md` when the reviewed policy needs clarification
- `CURRENT_TASK.md`, `Codex/M18_NODE24_CI.md`, and this plan

# Geometry invariants

- The 2D canvas plus FoldScript remain authoritative and generated Meshes
  remain derived.
- Geometry equations, operation order, coordinate systems, winding, boundary
  order, seam topology, source UVs, tolerances, diagnostics, and deterministic
  output do not change.
- Package version remains `1.0.0`, FoldScript remains `0.1`, and Unity remains
  `6000.3.20f1` throughout M18.
- No package-included path may change. The rebuilt archive must remain exactly
  byte-identical to public stable `v1.0.0`.

# Implementation steps

1. Re-query every approved Action's official repository and read primary
   release notes plus `action.yml`/`package.json` runtime metadata.
2. Record the selected version, tag, full commit, Node runtime, compatibility,
   and any migration notes. Do not infer a tag-to-commit mapping.
3. Decide the official GameCI path separately. If no supported Node 24 release
   exists, retain the reviewed pin and record a bounded exception.
4. Update the central immutable allowlist and every workflow reference in one
   atomic change; preserve events, permissions, guards, job names, artifact
   paths, inputs, outputs, and retention.
5. Expand deterministic negative tests for stale and mismatched version/SHA
   pairs and any approved compatibility exception.
6. Run Action-pin, repository, workflow-YAML, deterministic package, public
   release, and archive-byte checks locally.
7. Run full hosted Unity, clean-install, handoff, source-upgrade, and M13
   long-run gates.
8. Inspect hosted logs for runtime warnings, audit the exact head, merge only
   with required checks green, and update issue #25 with primary evidence.

# Test matrix

## Supply-chain identity

- every selected tag resolves to the recorded lowercase 40-character commit
- version comment, allowlist entry, and workflow SHA must agree
- tag, branch, short SHA, uppercase SHA, wrong version comment, unknown Action,
  YAML indirection, and unreviewed local/remote references fail deterministically

## Workflow preservation

- pull-request, push, schedule, release, and manual dispatch triggers remain
  unchanged unless an exact reviewed migration requires syntax adaptation
- permissions, fork guards, secrets boundary, required job names, artifacts,
  paths, retention, and fail-closed conditions remain equivalent
- package release recovery and public qualification paths still parse and pass

## Product regression

- deterministic main archive SHA-256 equals public stable
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`
- full 477-test Edit Mode suite passes with zero failed, skipped, or
  inconclusive results
- two clean installs, producer/receiver handoff, RC2-to-stable upgrade, and
  M13 robustness/resource evidence remain green

# Risks and rollback

- **Upstream tag drift:** resolve through official APIs, pin only the reviewed
  full commit, and retain the old exact pin for immediate rollback.
- **Runtime migration changes behavior:** review upstream release notes and
  metadata, preserve inputs/outputs, and require complete hosted evidence.
- **GameCI lacks Node 24 support:** keep its existing immutable pin and record
  the upstream blocker; do not fork, suppress, or misreport the warning.
- **Package-byte contamination:** stage only repository-infrastructure files
  and compare the rebuilt archive to the public stable hash before merge.
- **User scratch contamination:** never stage untracked `Project~` scenes,
  results, or generated `.meta` files.

# Progress log

- 2026-08-12: queried official GitHub release, tag, commit, signature, and
  `action.yml` metadata. Selected signed Node 24 releases for checkout
  `v7.0.1`, upload-artifact `v7.0.1`, and download-artifact `v8.0.1`.
- 2026-08-12: confirmed GameCI PR #304 is merged and `main` declares Node 24,
  but the latest official release/tag remains `v4.3.1` with Node 20. Retained
  its existing full-SHA pin as a bounded upstream exception.
- 2026-08-12: updated workflow references and the central allowlist atomically;
  added stale-version/SHA adversarial cases.
- 2026-08-12: the complete local repository matrix passed. Action pin tests
  report 44 cases; repository, release, stable-readiness, public-release,
  upgrade, soak, trust-boundary, clean-install, and handoff validators are
  green. The rebuilt stable archive is exactly
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.
  Hosted gates remain.
- 2026-08-12: M17 public qualification run `31512005281` completed successfully
  on exact tag commit `6ed32f1e`; two consumers and both upgrade phases passed
  in Unity `6000.3.20f1`, and stable proof artifact `9109614640` is qualified.
- 2026-08-12: selected GitHub issue #25 as M18 because hosted logs now show
  official Node.js 20 Action runtimes being forced onto Node 24. No Action pin
  has been changed yet.

# Decisions made

- Use the latest signed official Node 24 release for each first-party Action.
  The workflows use named artifacts rather than single `artifact-ids`, so
  download-artifact v5's ID-path change is irrelevant; v8's digest mismatch
  default strengthens the existing fail-closed contract. Upload v7 retains
  archive mode by default, so no workflow input changes are required.
- Checkout v7's safer `pull_request_target` / `workflow_run` behavior is
  compatible with this repository: the credential-bearing
  `pull_request_target` gate never checks out code, and the only workflow-run
  checkout is gated to same-repository schedule or manual candidate-soak runs.
- No tagged GameCI Node 24 release exists. Retain the signed v4.3.1 pin until
  upstream publishes one; do not treat an untagged `main` commit as a release.
- M18 is repository infrastructure only; its acceptance explicitly freezes all
  package-included bytes to the public stable archive.
- Official upstream repositories are the only authority for release, commit,
  and runtime identity.
- GameCI is evaluated independently because an official compatible release may
  lag the first-party Actions.
- A residual upstream warning is reported as an exception, never hidden by an
  environment flag or unsupported fork.

# Final verification

Upstream research, pin implementation, local checks, and archive identity are
complete. Hosted evidence, exact-head audit, merge, and issue closure remain
pending. No package-included file, geometry behavior, or later geometry
milestone has been implemented.
