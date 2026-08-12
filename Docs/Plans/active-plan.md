# Goal

Deliver M21 on `agent/m21-proof-gallery-evidence`: regenerate honest cup and
sphere proof images from tracked 2D source through the ordinary compiler,
without changing stable package bytes.

# User-visible proof

A reviewable gallery pairs each maintained 2D canvas with a clean textured 3D
result and a topology view. A canonical manifest binds pixels to compiler
evidence, source hashes, source revision, Unity version, and the exact
regeneration command.

# Scope

- GitHub issue #26, proof-generation gate only
- release-excluded clean-host Unity renderer and Edit Mode test adapter
- cup source, textured exterior, and closed-volume wire/section evidence
- eight-gore sphere source, textured result, and wire/seam evidence
- canonical manifest, dependency-free validator, adversarial fixtures
- repository and hosted exact-head integration

# Non-goals

- package-included README changes or a package version bump
- repository social-preview publication
- Runtime, compiler, geometry, topology, Schema, samples, existing Tests,
  materials, dependencies, public API, or release-asset changes
- ImageGen, Blender, stock geometry, manual pixel painting, editor chrome, or
  untracked Project~ input
- issue #21 or later geometry work

# Files expected to change

- `Scripts/Templates~/M21ProofGallery/Assets/FoldCanvasProofGalleryGenerator.cs`
- `Scripts/Templates~/M21ProofGallery/Assets/FoldCanvasProofGalleryTests.cs`
- `Scripts/create_proof_gallery_project.py`
- `Scripts/generate_proof_gallery.py`
- `Docs/Community/ProofGallery/*.png`
- `Docs/Community/ProofGallery/manifest.json`
- `Docs/Community/ProofGallery/README.md`
- `Scripts/validate_proof_gallery.py`
- `Scripts/test_proof_gallery.py`
- `Scripts/validate_repository.py`
- `.github/workflows/repository-checks.yml`
- `.github/workflows/unity-tests.yml`
- `Docs/Community/START_HERE.md`
- `CURRENT_TASK.md`, `Codex/M20_ROLL_HANDEDNESS_DIAGRAM.md`,
  `Codex/M21_PROOF_GALLERY_EVIDENCE.md`, and this plan

# Geometry invariants

- source canvases and FoldScript remain the only authoritative asset input;
  every Mesh, wireframe, report, and PNG is derived
- cup proof uses the maintained production canvas, rolled/welded wall and
  bottom, and inward Solidify without overlap or epsilon concealment
- cup closed-volume report must have one component, zero open,
  non-manifold, and orientation-conflict edges, and positive absolute volume
- sphere proof uses exactly eight explicit gore panels and their seam graph;
  no Unity primitive sphere may enter the proof
- sphere report must have Euler characteristic two, zero open,
  non-manifold, orientation-conflict, and inward-triangle counts, with one
  north and one south topology pole
- camera transforms, render size, clear color, light-independent shaders,
  object transforms, ordering, and PNG encoder are fixed
- geometry identity is computed from canonical compiled buffers, not from
  transient Unity instance IDs or screenshots
- package version remains `1.0.0`, FoldScript remains `0.1`, Unity remains
  `6000.3.20f1`, and archive SHA-256 remains
  `16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`

# Implementation steps

1. Close M20 records and freeze M21's release-excluded boundary.
2. Add one batch-safe, release-excluded clean-host adapter that builds proof
   objects through the public Runtime compiler, renders fixed cameras to PNG,
   records compile reports, and exits nonzero on any mismatch.
3. Render cup source/textured/topology and sphere source/textured/topology
   images at fixed dimensions with no UI or external asset.
4. Emit a canonical manifest with source revision, Unity/package/FoldScript
   versions, exact command, source/output hashes, geometry hashes, and checked
   topology values.
5. Add a standard-library validator for schema, ordering, path confinement,
   PNG structure/dimensions, hashes, claim invariants, and prohibited metadata.
6. Add real-repository and adversarial fixtures; wire static validation and a
   Unity batch-regeneration comparison into hosted CI.
7. Inspect every rendered PNG, run the complete local validation matrix, and
   confirm stable archive identity.
8. Push a PR, wait for exact-head hosted checks, record exact-head audit, and
   merge only when all required gates are green.

# Test matrix

## Generator and geometry

- clean batch invocation succeeds twice and produces byte-identical PNGs and
  manifest content except for no environment-dependent fields
- cup source/textured/topology views match the maintained source and closed
  volume invariants
- sphere source/textured/topology views match eight-gore and sphere-report
  invariants
- output directories cannot escape the repository proof root
- failed compile/report/render writes no partial accepted manifest

## Evidence safety

- missing file, wrong hash, invalid PNG signature/dimension, remote reference,
  absolute/local path, unexpected field/order, duplicate output, untracked
  source, wrong Unity/package/FoldScript version, geometry-hash mismatch, and
  weakened topology claim fail deterministically
- multi-error diagnostics are stable and validation does not mutate inputs

## Product regression

- repository validation and 477 existing Edit Mode tests stay green
- clean-install, producer/receiver handoff, and source-first upgrade stay green
- stable archive remains byte-identical to public v1.0.0
- no package-included file changes

# Risks and rollback

- **Render nondeterminism:** use unlit deterministic materials, explicit camera
  and render-target settings, fixed color space, no post-processing, and byte
  comparison across two clean renders.
- **Proof drift:** bind every image to source and geometry hashes, and regenerate
  in CI rather than accepting manually edited pixels.
- **Leaking local state:** start from a new scene, write only to an explicit
  proof root, exclude UI, and reject usernames/absolute paths in metadata.
- **Package contamination:** statically reject changes to package allowlisted
  paths and rebuild the exact stable archive.
- **Scratch contamination:** never stage untracked Project~ scenes, TestResults,
  or generated `.meta` files.

Rollback is one revert of the M21 integration commit. It removes only the
release-excluded renderer, gallery evidence, validators, workflow wiring, and
task records. No source migration is required.

# Progress log

- 2026-08-13: verified M20 merge `eb310e0`, green main repository and Unity
  runs, zero open PRs, no external CLAIMs, and #21 still blocked on real
  Windows evidence.
- 2026-08-13: confirmed `README.md` is an explicit deterministic UPM archive
  member. Split #26 so proof generation precedes a separately versioned README
  integration.
- 2026-08-13: selected tracked M04.1 cup and M05 eight-gore sphere creators as
  the only allowed proof geometry paths; no untracked Project~ input is used.

# Decisions made

- M21 creates its host adapter and evidence outside package allowlisted paths.
  The public v1.0.0 and RC2 archives are immutable; a future README edit must
  be a patch release.
- Use existing one-sided unlit/textured, topology-wireframe, seam, and section
  proof materials. No lighting or two-sided material may conceal geometry.
- Record compiler topology reports and canonical geometry hashes alongside
  images; screenshots alone are insufficient evidence.
- Use a fresh empty scene and an explicit owned RenderTexture for batch output;
  never discover or modify a user camera.

# Final verification

Local implementation is complete. Unity `6000.3.20f1` regenerated all seven
evidence files twice from disposable clean hosts; both output sets were
byte-identical. All six PNGs were visually inspected. Proof validation (six
PNGs, two sources, two geometry reports), nine adversarial proof fixtures,
repository/release/installation/upgrade/handoff checks, 44 Action-pin cases,
JSON/Python checks, `git diff --check`, and the trusted-contribution gate pass.
The stable archive remains exactly
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.
Hosted exact-head checks, audit, and merge remain. M21 has not changed compiler,
geometry, package contents, or later milestone behavior.
