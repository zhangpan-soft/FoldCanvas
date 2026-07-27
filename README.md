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

The repository currently contains the **M02 fold compiler**:

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
- An editor-generated six-region canvas that compiles into a textured box
- Deterministic diagnostics and validation
- Mesh baking tools for the Unity Editor
- Edit-mode tests
- Architecture, FoldScript, roadmap, and Codex task prompts

The first public proof target is a cup whose base and wall artwork live on one 2D canvas and compile into a closed, thickened 3D object.

On the M03 audit branch, Circular Roll is intentionally narrower than that
future closed-shell target: it supports at most one signed turn, requires at
least three source segments for a complete turn, and accepts only congruent
planar current embeddings. The cup wall and base are numerically aligned but
remain separate zero-thickness surfaces until M04.

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
5. Open `Window > FoldCanvas > FoldCanvas`.
6. Use `Tools > FoldCanvas > Create Bootstrap Sample` to create the planar
   M01 source, or `Tools > FoldCanvas > Create M02 Box Proof` to create, bake,
   and display the six-face fold proof. On the M03 audit branch, use
   `Tools > FoldCanvas > Create M03 Cup Proof` for the owned cup/source/camera
   proof.

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
| M03 | Roll a decorated wall into a cylindrical cup |
| M04 | Resample seams, weld boundaries, and add thickness |
| M05 | Compile sphere gores into a closed sphere |
| M06 | Split 2D canvas / 3D preview editor |
| M07 | Manifold, inversion, seam, and intersection validators |
| M08 | FoldScript import/export and AI feedback loop |
| M09 | Handle cup, torus, and non-trivial topology |

The detailed acceptance criteria live in [`Documentation~/roadmap.md`](Documentation~/roadmap.md).

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md) and the architecture decisions in [`Docs/ADR`](Docs/ADR). Geometry changes require deterministic tests and at least one human-readable failure diagnostic.

## Publishing on GitHub

Repository creation, first-push commands, protection recommendations, and naming cautions are documented in [`Docs/github-setup.md`](Docs/github-setup.md). `FoldCanvas` is a working name and should be checked before public launch.

## License

Apache License 2.0. See [`LICENSE.md`](LICENSE.md).
