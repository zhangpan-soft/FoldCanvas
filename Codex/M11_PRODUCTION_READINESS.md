# M11: Production-readiness foundation

## Visible proof

1. Build the deterministic UPM archive and install that archive, not the
   repository checkout, into a generated clean Unity `6000.3.20f1` host.
2. Compile a consumer-owned assembly in that host against the public
   `FoldCanvas.Runtime` API, create authoritative 2D source, generate derived
   geometry and OBJ text, and emit stable counts/hashes.
3. Repeat the clean installation and compile without changing the evidence.
4. Validate a checked-in public Runtime API surface baseline and explain every
   intentional compatibility change.
5. Run the maintained production corpus through Basic, Standard, and Strict
   validation as applicable, with deterministic diagnostic and geometry hashes.
6. Produce hosted XML, Editor log, package manifest, and consumer proof evidence
   that can be audited without access to a developer's local Unity scene.

## Goal

Establish the first release-candidate evidence layer above M00-M10. Prove that
FoldCanvas is consumable as a package and that future maintenance can detect
API, format, determinism, installation, and trust-boundary regressions before a
release reaches users.

## Production-readiness contract

- The release archive is the installation input. A clean-host test must not
  resolve Runtime or Editor assemblies through the repository-root `file:`
  package used by `Project~`.
- Consumer proof code lives outside the package and compiles through public API
  only. Internal access, copied package source, scene state, and `Camera.main`
  are forbidden evidence shortcuts.
- One checked-in, ordinal public Runtime API manifest records public types and
  members. Additions are reviewable; removals or signature changes require an
  ADR, migration notes, and an appropriate version decision.
- FoldScript `0.1`, package version, Unity minimum version, validation level,
  package archive hash, source hash, geometry hash, and diagnostic hash are
  explicit evidence fields.
- Native operation executors are trusted in-process contributor code. The M10
  API limits capabilities it exposes, but it is not an OS sandbox and cannot
  prevent deliberately hostile managed code from using other process APIs.
- Identical clean-host inputs must produce identical topology, UV, provenance,
  diagnostics, and exporter text. Timing remains informational only.
- M11 evidence is derived. Canvas, FoldScript, panels, seams, and ordered
  operations remain authoritative.

## Work packages

### A. Clean archive installation

- Generate a minimal host project under a temporary/artifact directory.
- Reference the freshly built `.tgz` archive through the host manifest.
- Add one consumer-owned Edit Mode test assembly under that host's `Assets/`.
- Run Unity `6000.3.20f1` and upload NUnit XML, Editor log, package-resolution
  evidence, and the deterministic consumer report.
- Reject fallback to the repository package or a missing archive.

### B. Public API and compatibility

- Generate a stable public Runtime API signature manifest from the compiled
  assembly using deterministic ordering and normalized type names.
- Compare it in Edit Mode tests and repository validation.
- Add package/FoldScript/Unity compatibility and migration documentation.
- Record the native-extension trust model and supported reporting path.

### C. Production acceptance corpus

- Maintain a small, explicit corpus covering planar artwork, the production
  cup, sphere gores, cyclic torus topology, and a registered position operation.
- Store source identity plus expected vertex/triangle/topology/diagnostic hashes,
  not generated Mesh assets as source.
- Recompile each case at least twice and compare evidence.
- Include at least one expected-invalid fixture so stable failure is part of
  production acceptance.

### D. Release gate

- Run repository validation, deterministic package validation, full package
  Edit Mode tests, clean-install consumer tests, and corpus verification as
  independent hosted jobs.
- Make missing XML/log/report artifacts fail the job.
- Document which checks block a preview/RC release.

## Tests

- clean host resolves `com.foldcanvas.core` from the freshly built archive
- consumer assembly has no reference to package internals or repository paths
- consumer proof compiles and exports stable geometry twice
- clean host uploads real XML, Editor log, package-resolution, and proof report
- public API manifest is ordinal and repeatable
- public API manifest detects a removed or changed signature
- production corpus cases retain stable topology/UV/diagnostic hashes
- expected-invalid corpus case returns its stable diagnostic and no Mesh
- native extension trust boundary is documented and repository-validated
- all existing M00-M10 tests remain enabled

## Non-goals

- a new panel, fold, wrap, Stitch, or Solidify operation
- topology mutation through public extensions
- arbitrary text/image-to-mesh generation
- CSG, bevel, subdivision, smoothing, remesh, or cleanup
- glTF, FBX, USD, Blender, or runtime filesystem/network integration
- publishing `1.0.0` or an external marketplace package
- supporting Unity versions that have not been run in hosted evidence
