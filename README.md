# FoldCanvas

> **Working title. Pre-alpha.** A 2D-first, deterministic surface compiler for Unity.

## Why this project exists

AI image generation is already strong enough to produce useful concept art,
textures, decals, and other 2D source material. AI-generated 3D meshes are far
less reliable for a game pipeline: topology, UVs, scale, seams, thickness,
editability, and repeatability can change unpredictably between generations.

FoldCanvas separates those responsibilities. AI or a human authors a 2D
appearance canvas and a structured construction description; a deterministic
compiler reconstructs the 3D surface with stable geometry, UVs, provenance, and
diagnostics. This turns a generated asset into something that can be reviewed,
edited, tested, diffed, and rebuilt instead of an opaque triangle result.

Read the full [project background](Documentation~/project-background.md) and
the [FoldScript JSON field reference](Documentation~/foldscript-field-reference.md).

FoldCanvas treats a three-dimensional game asset as a compiled result rather than the primary source file.

```text
3D Asset = 2D Canvas + Panels + Seams + Fold Program + Thickness
```

The author edits a two-dimensional canvas and a readable construction program. FoldCanvas compiles that source into a Unity `Mesh`, material bindings, validation diagnostics, and eventually colliders, LODs, and prefabs.

This repository is not another text-to-mesh wrapper. The geometry core is deterministic and provider-independent. AI is intended to generate or edit the structured source representation, never to bypass it with an opaque triangle soup.

## Status

The repository currently contains the **M05 spherical-surface compiler, M06
Editor authoring workspace, M07 geometry validator, M08 FoldScript
interchange/repair boundary, M09 cyclic-topology proof, and M10 bounded
extension/release ecosystem**:

- Unity Package Manager package layout
- Serializable source asset model
- Validated rectangle and disk/ellipse panel tessellation
- Stable source-local coordinates and canvas UV preservation
- Immutable compiled data with panel ownership, provenance, and ordered boundaries
- Pre-allocation vertex/triangle safety limits
- Ordered rigid-transform operations
- Ordered rigid-crease Fold operations with documented signed handedness
- Deterministic source-line embedding into the panel's current 3D surface
- Stable diagnostics for invalid lines, missing targets, nonzero falloff, and
  non-linear current hinges
- Circular Roll in the target's current congruent planar frame, with
  documented signed handedness and outward winding
- Unequal-count seam resampling with real adjacent-triangle subdivision
- Reusable Weld and Bridge seams with deterministic logical topology IDs;
  source UV/provenance splits remain valid render-vertex splits
- Inward, outward, and centered Solidify, including shared welded-corner
  miters, reversed inner winding, and side walls only on true open edges
- Immutable closed-volume reports with logical edge incidence, connected
  components, winding conflicts, and signed/absolute volume
- Automatically derived outer/inner hard-corner segments over emitted
  Solidify vertices
- Explicit rectangular spherical-gore panels mapped through a documented
  current local frame, with radius, latitude, longitude, direction, pole, and
  subdivision fields
- Deterministic spherical pole topology, curved-surface seam subdivision,
  outward winding, and closed-sphere validation with Euler characteristic and
  bounded radial error
- Component-scoped pre-Solidify sphere reports, so unrelated Stitch or
  Solidify operations cannot trigger, suppress, or replace a sphere proof
- One cumulative geometry budget across panel tessellation, Stitch
  subdivision/bridges, and Solidify shell/rim generation
- An editor-generated six-region canvas that compiles into a textured box
- An editor-generated thick cup whose wall seam and bottom perimeter are
  welded, whose inner corner remains connected, and whose top rim is closed
- An editor-generated eight-gore 2D canvas that reconstructs one closed sphere
  while preserving readable source artwork and independent UV charts
- Deterministic diagnostics and validation
- Mesh baking tools for the Unity Editor
- A UI Toolkit workspace with a split 2D source canvas and owned 3D preview
- Rectangle/disk creation and canvas handles, named boundary/seam pairing,
  explicit ordered operation forms, structured diagnostic focus, Undo/Redo,
  revisioned debounced compilation, debug overlays, and valid-only Bake
- Cumulative Basic, Standard, and Strict final-geometry validation with stable
  root-cause precedence and a read-only report
- Localized boundary, render/logical vertex, component, triangle-pair, and
  logical-edge diagnostic context
- Deterministic Strict sweep-and-prune plus exact non-adjacent triangle
  intersection checks; broad-phase candidates alone are never reported as
  confirmed collisions
- Adversarial validator fixtures for malformed topology, open seams,
  self-intersecting rolls, and thickness overlap, plus valid cup/sphere
  false-positive regressions
- Bounded executable FoldScript `0.1` import/export with explicit DTOs, strict
  semantic/reference validation, safe appearance paths, and unit conversion
- Deterministic canonical JSON that preserves authored panel, seam, and
  operation order while producing locale-independent byte-stable output
- Provider-neutral immutable source proposal/repair contracts and compact
  diagnostic payloads; replacement source must re-enter the normal importer
  and compiler, with no model SDK or network dependency in the package
- M06 Import JSON, Export JSON, and Copy Repair Payload actions with explicit
  Editor asset ownership, Undo-aware replacement, and failed-import isolation
- Optional normalized boundary-reference spans with deterministic source-
  triangle subdivision for off-grid endpoints
- `ToroidalWrap` over an authored rectangle, with signed major/minor ranges,
  current-frame preservation, outward winding, and explicit Weld-only cycle
  closure
- An editor-generated torus with two authored closure seams, Euler
  characteristic zero, and distinct source UV copies at both topology seams
- An editor-generated handled cup whose ordinary rectangular strip is placed,
  folded twice, welded to two top-rim spans, and Solidified into one closed
  connected volume
- An explicit per-compile contributor operation registry with position-only,
  single-panel mutation, deterministic preflight, and complete rollback
- A versioned sample gallery and compiling custom-operation template
- Deterministic dependency-free OBJ export, maintained performance evidence,
  and byte-reproducible allowlisted UPM release archives
- Edit-mode tests
- Architecture, FoldScript, roadmap, and Codex task prompts

The first public proof target was a cup whose base and wall artwork live on one
2D canvas and compile into a closed, thickened 3D object. M05 extends the same
source-first contract to a closed sphere assembled from eight explicit 2D
gore panels.

M04 keeps M03 Circular Roll's one-turn and congruent-planar-frame contracts.
Its seam solver retains both boundaries' normalized breakpoints, performs real
surface subdivision for missing samples, and then Welds or Bridges the paired
chains. Solidify consumes the final stitched topology, creates outer and inner
shells, and rims only true open boundaries. Until shared-topology deformation
propagation exists, Stitch is terminal for later position deformation on every
panel it selected.

M05 does not call a sphere generator. Each rectangle is tessellated from its
authored 2D domain, mapped by `SphericalWrap` in its current congruent planar
frame, and joined only by the declared seam graph and terminal `Stitch`. Exact
pole samples are represented explicitly, inserted seam samples are
re-evaluated on the spherical map, and the final logical topology must report
one component, no open/non-manifold edges, outward winding, Euler
characteristic 2, one north pole, one south pole, and bounded radius error.
Only spherical-to-spherical seams form a component, but any later Stitch seam
with either endpoint in that component delays its report. The report is frozen
after the last touching Stitch and before a Solidify touching the component.
Every selected spherical endpoint must be wrapped before its Stitch; invalid
order fails before tessellation and cannot emit a premature sphere report.
The sphere-specific report does not itself perform global triangle-triangle
self-intersection detection. When `validationLevel` is `strict`, the later M07
final-buffer report adds deterministic exact non-adjacent triangle evidence.

M06 makes the same source model directly authorable without vertex editing.
`Tools > FoldCanvas > Open Authoring Workspace` opens the 2D canvas, 3D
preview, panel/operation/seam forms, diagnostics, and Bake controls. Preview
objects are disposable Editor-only derivatives and never become source. See
the [blank-source-to-cup walkthrough](Documentation~/authoring-workspace.md).

M07 validates the final explicit geometry buffer without changing it. Basic
protects structural safety, Standard adds logical topology, boundaries,
executed Welds, components, and orientation, and Strict adds exact
triangle-intersection checks behind a deterministic broad phase. Intentional
open sheets and multi-part assets remain valid with warnings. See
[M07 geometry validation](Documentation~/geometry-validation.md).

M08 makes FoldScript `0.1` a real portable source boundary. Runtime accepts
bounded untrusted JSON, validates all documented fields and references,
converts declared physical units to native meters, and emits deterministic
canonical JSON. The Editor can import/export explicit project assets, while an
external AI integration may consume immutable repair payloads and return only
complete replacement FoldScript. The package itself performs no provider call,
authentication, or automatic repair. See the
[M08 Runtime and Editor workflow](Documentation~/foldscript-runtime.md).

M09 proves non-trivial loop topology without hiding a primitive or editing the
generated Mesh. `ToroidalWrap` maps one explicit rectangular source panel to a
toroidal surface, while two declared Weld seams separately close its major and
minor cycles. Boundary references may select normalized non-wrapping spans, so
the handle proof reuses an ordinary rectangle strip, existing rigid transforms
and edge-aligned folds, two cup-rim attachment spans, terminal Stitch, and one
final Solidify. The torus and handled cup both retain source UV/provenance and
must pass deterministic logical-topology tests. See the
[M09 topology sample guide](Samples~/Topology/README.md).

M10 opens one deliberately narrow contributor boundary without exposing the
compiler's topology buffer. A native custom operation is registered explicitly
for one compile and may replace only finite positions on one existing panel;
UVs, provenance, triangles, boundaries, topology, and geometry budget remain
unchanged. Gallery views, OBJ files, performance reports, and release archives
remain derived. FoldScript `0.1` still rejects unknown operations. See the
[M10 extensibility contract](Documentation~/extensibility.md) and
[operation template](Samples~/OperationExtension/README.md).

## Design principles

1. **The 2D source is authoritative.** Generated meshes are disposable build artifacts.
2. **Geometry is deterministic.** The same source, settings, and compiler version must yield the same topology and vertex order.
3. **Appearance stays attached.** Every generated vertex retains UV coordinates from the source canvas.
4. **AI emits intent, not triangles.** AI adapters produce schema-constrained FoldScript and canvas layers.
5. **Errors are first-class output.** Invalid seams, inversions, self-intersections, and unsupported operations must produce actionable diagnostics.
6. **Simple materials first.** MVP rendering uses one appearance texture and an Unlit-compatible material. PBR is optional and derived later.
7. **Unity is the first host, not the mathematical boundary.** The core representation should remain portable enough for future non-Unity compilers.

## Mathematical scope

FoldCanvas does **not** claim that every curved surface can be flattened isometrically. An information-preserving 2D representation may include seams, metric distortion, curvature instructions, topology, and thickness. See [`Documentation~/geometry-model.md`](Documentation~/geometry-model.md).

## Requirements

- Unity **6.3 LTS**, baseline project version `6000.3.20f1`
- No render-pipeline dependency for the core package
- Unity Test Framework for package tests

## Open the development project

1. Clone the repository.
2. Open `Project~` from Unity Hub with Unity 6.3 LTS.
3. Unity resolves the package through a local `file:../../` dependency.
4. Open `Window > General > Test Runner` and run Edit Mode tests.
5. Open `Tools > FoldCanvas > Open Authoring Workspace`.
6. Use `Tools > FoldCanvas > Create Bootstrap Sample` to create the planar
   M01 source, or `Tools > FoldCanvas > Create M02 Box Proof` to create, bake,
   and display the six-face fold proof. Use
   `Tools > FoldCanvas > Create M03 Cup Proof` for the retained zero-thickness
   presentation example, or
   `Tools > FoldCanvas > Create M04 Production Cup Proof` for the solid-color
   and bleed-safe thick-cup proof with four owned validation cameras. Use
   `Tools > FoldCanvas > Create M04.1 Closed Volume Cup Proof` for the
   texture-free solid, logical-wireframe, section, and inner/outer-corner
   validation hierarchy. Use
   `Tools > FoldCanvas > Create Sphere Proof` for the M05 2D-gore source,
   textured and one-sided solid spheres, logical wireframe, seam/pole overlays,
   UV-stretch view, radius-error view, and validation report. Use
   `Tools > FoldCanvas > Create M09 Topology Proof` for the explicit two-cycle
   torus and folded-strip handled cup with textured, one-sided solid, logical-
   wireframe, source-canvas, and topology-report views. Use
   `Tools > FoldCanvas > Open Sample Gallery` for the versioned manifest and
   `Tools > FoldCanvas > Create M10 Ecosystem Proof` for the registered wave,
   solid one-sided view, registry report, and deterministic OBJ proof.

## Continuous integration

GitHub Actions runs two independent checks:

- `repository-validation` parses repository JSON, checks assembly and runtime
  boundaries, confirms the Schema/native `sampleCount` maximum, and validates
  documentation links and version metadata.
- `unity-editmode-tests` opens the tracked `Project~` host in Unity
  `6000.3.20f1`, compiles Runtime, Editor, and Tests assemblies, runs all Edit
  Mode tests, and uploads the NUnit XML plus `Editor.log`, even on failure.
  Live GameCI output is staged below the host-project root so a changing log
  is never imported through the repository-root local UPM package; evidence is
copied to `artifacts/unity-editmode` only after Unity exits.

The separate `Package release` workflow builds the allowlisted UPM archive
twice, verifies byte identity and version agreement, uploads the `.tgz` plus
SHA-256 evidence, and publishes only an exact matching pushed version tag.

The Unity job uses GameCI and requires repository Actions secrets
`UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` for a Unity Personal
license. License data is never stored in the repository.

## Install in another Unity project

During local development, add this to the target project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.foldcanvas.core": "file:/absolute/path/to/FoldCanvas"
  }
}
```

After the repository is public, it can also be installed by Git URL and tag.

## Repository map

```text
FoldCanvas/
├── AGENTS.md                     Codex repository instructions
├── PLANS.md                      Long-task execution-plan protocol
├── CURRENT_TASK.md               The one active milestone
├── Runtime/                      Runtime-safe data and compiler code
├── Editor/                       Unity Editor tooling and baking
├── Tests/Editor/                 Edit-mode package tests
├── Samples~/                     Importable package samples
├── Documentation~/              Package documentation
├── Docs/                         Project governance, ADRs, and research notes
├── Codex/                        Master and milestone implementation prompts
├── Schema/                       FoldScript JSON Schema
└── Project~/                     Local Unity host project
```

## Start with Codex

Codex reads `AGENTS.md` automatically. Paste the task in [`Codex/MASTER_PROMPT.md`](Codex/MASTER_PROMPT.md), or simply ask it to execute `CURRENT_TASK.md` according to `PLANS.md`.

Do not ask Codex to implement the entire roadmap in one turn. Complete one milestone, run tests, inspect the generated artifact, and then advance `CURRENT_TASK.md`.

## Roadmap snapshot

| Milestone | Proof |
|---|---|
| M00 | Repository opens and bootstrap tests pass |
| M01 | Robust planar panels and preserved UVs |
| M02 | Fold six decorated rectangle panels into a box — complete |
| M03 | Roll a decorated wall into a cylindrical cup — complete |
| M04 | General seam resampling/bridging, shell thickness, and M04.1 closed-volume proof — implemented |
| M05 | Compile explicit 2D sphere gores into a validated closed sphere — implemented |
| M06 | Split 2D canvas / 3D preview editor — implemented and merged |
| M07 | Manifold, inversion, seam, and exact intersection validators — implemented and merged |
| M08 | FoldScript import/export and AI feedback loop — implemented and merged |
| M09 | Handle cup, torus, boundary spans, and non-trivial topology — implemented and merged |
| M10 | Explicit extensions, gallery, OBJ, performance, and reproducible package release — implemented on review branch |

The detailed acceptance criteria live in [`Documentation~/roadmap.md`](Documentation~/roadmap.md).

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md) and the architecture decisions in [`Docs/ADR`](Docs/ADR). Geometry changes require deterministic tests and at least one human-readable failure diagnostic.

## Publishing on GitHub

Repository creation, first-push commands, protection recommendations, and naming cautions are documented in [`Docs/github-setup.md`](Docs/github-setup.md). `FoldCanvas` is a working name and should be checked before public launch.

## License

Apache License 2.0. See [`LICENSE.md`](LICENSE.md).
