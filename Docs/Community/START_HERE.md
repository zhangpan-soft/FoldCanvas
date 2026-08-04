# Start contributing to FoldCanvas

FoldCanvas is a deterministic Unity compiler that turns an authored 2D canvas
plus readable geometry rules into a rebuildable 3D surface. The source is not
a generated Mesh:

```text
3D result = 2D appearance + panels + boundaries + seams + ordered operations
```

That distinction is the project's core. AI may help author the canvas or
FoldScript, but the compiler owns topology, UV preservation, winding,
thickness, validation, and repeatability.

## Try it before changing it

1. Use Unity `6000.3.20f1`.
2. Install public pre-release
   [`v1.0.0-rc.2`](https://github.com/zhangpan-soft/FoldCanvas/releases/tag/v1.0.0-rc.2)
   through its `.tgz`, or clone the repository and open `Project~`.
3. Read [`Documentation~/architecture.md`](../../Documentation~/architecture.md)
   and [`CONTRIBUTING.md`](../../CONTRIBUTING.md). AI-agent contributors must
   also follow the [external agent policy](AGENT_COLLABORATION.md).
4. In Unity, open `Tools > FoldCanvas > Sample Gallery` and compile an existing
   cup, sphere, torus, or extension proof.
5. Run Edit Mode tests before and after your change. A real result includes
   test XML and Editor.log; Unity failing to start is not a pass.

## Choose a contribution lane

### Source example or geometry challenge

Describe the 2D panels and named boundaries first. Include seams, operation
order, UV expectations, winding, topology, invalid inputs, and numerical plus
visual success criteria. Use the `geometry-case` issue form.

### Deterministic test or diagnostic

Reduce a failure to the smallest source fixture. Assert coordinates, topology,
UVs, boundary order, diagnostics, or stable hashes instead of relying only on a
screenshot. Do not weaken an existing assertion to make a new case pass.

### Documentation

Improve a field definition, worked FoldScript example, mathematical diagram,
troubleshooting path, or bilingual explanation. Documentation must agree with
the schema and actual compiler behavior.

### Editor workflow

Improve source authoring, preview ownership, Undo/Redo, diagnostics focus, or
bake safety. Runtime code must remain free of `UnityEditor`; preview Meshes are
disposable derivatives.

### Core geometry

Start with an issue and an execution plan. Any new geometry behavior needs
documented coordinates, boundary order, handedness, winding, tolerance,
diagnostics, Edit Mode tests, and a visible proof. Architecture changes need an
ADR before implementation.

## What makes a good first task

A starter issue should fit one concept and state:

- why the result matters to a user;
- exact source representation or documentation surface;
- files likely to change;
- acceptance checks and expected diagnostics;
- explicit non-goals;
- whether package bytes or compatibility change.

Tasks labeled `good first issue` are intended to be independently useful and
reviewable. Tasks labeled `help wanted` may need deeper Unity, geometry, or CI
experience. Comment on the issue before starting so two people do not duplicate
the same work.

## Pull-request evidence

Every PR must preserve the 2D canvas plus FoldScript source authority and pass
hosted checks. Geometry changes also require deterministic Edit Mode tests.
Package or compatibility changes need an explicit version decision. Never
commit Unity licenses, credentials, tokens, generated `Library` state, or a
manually repaired Mesh as source.

The maintainer will review against the exact PR head. A later push invalidates
the earlier audit, so the final commit must rerun the required gates.
