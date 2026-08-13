# Goal

Implement M24 deterministic topology refinement for compatible straight
off-grid Fold creases without weakening the 2D canvas plus FoldScript source
architecture or any downstream panel-range contract.

# User-visible proof

One generated decorated coarse rectangle (`uSegments = 1`, `vSegments = 1`)
uses a
vertical crease from `(0.3, 0)` to `(0.3, 1)`. The proof shows the flat source
grid and inserted edge, a 90-degree folded result with artwork continuous to
the hinge, and a one-sided wireframe view proving that no triangle spans the
crease.

# Scope

- enabled rigid Fold operations on rectangle panels;
- straight perimeter-to-perimeter source creases that partition the rectangle;
- pre-tessellation refinement planning in authored Fold order;
- deterministic intersection reuse, polygon clipping, and triangulation;
- source UV, provenance, topology, winding, panel ownership, and named-boundary
  preservation;
- stable rejection outside the supported crease contract;
- geometry-budget, repeatability, regression, and visual proof gates.

# Non-goals

- curved, branched, disk, interior-ending, or collinear-overlap refinement;
- falloff, bevel, subdivision, smooth, remesh, or cleanup;
- post-Stitch deformation or topology-group propagation;
- SpiralRoll, LayeredRoll, dependency additions, or a FoldScript schema change;
- publishing a package version or external marketplace artifact in M24.

# Files expected to change

- `Runtime/Compiler/FoldCanvasCompiler.cs`
- `Runtime/Compiler/PanelTessellator.cs`
- `Runtime/Compiler/FoldLineExecutor.cs`
- one new internal crease-refinement planner under `Runtime/Compiler/`
- `Tests/Editor/FoldLineCompilerTests.cs` and focused integration tests
- M24 Editor proof/sample files if required
- field reference, specification, compiler pipeline, diagnostics, roadmap,
  package changelog, task file, and ADR 0011

# Geometry invariants

- Coordinates: Fold lines stay in panel-normalized `[0,1]^2`; inserted source
  positions map through `x=(u-0.5)*width`, `y=(v-0.5)*height`.
- Winding: every output triangle preserves the original source triangle's
  signed winding; positive area is required before emission.
- Crease direction and angle: `lineStart -> lineEnd` remains the directed hinge
  and positive angles remain Unity `Quaternion.AngleAxis` semantics.
- Boundary order: inserted vertices are spliced between the same consecutive
  boundary vertices; `uMin/uMax` remain low-to-high V and `vMin/vMax` remain
  low-to-high U.
- UV: new UV is evaluated from immutable normalized source coordinates and the
  panel `CanvasRect`; no atlas resampling or generated texture occurs.
- Topology/provenance: one canonical inserted vertex is reused for every
  triangle sharing the crossed edge; its initial provenance ID is its stable
  emitted render index.
- Ranges: every panel is emitted once and retains contiguous vertex and
  triangle ranges after all planned refinement.
- Tolerances: source side/intersection classification uses centralized
  `NormalizedFoldLineTolerance`; no hidden epsilon offset moves geometry.
- Failure: unsupported or over-budget refinement yields diagnostics and no
  Mesh, never an approximate Fold.

# Implementation steps

1. Close M23 documentation with its exact release and public qualification
   evidence; activate M24 and ADR 0011.
2. Build an immutable per-panel crease refinement plan before buffer creation,
   validating compatible Fold lines and estimating exact final geometry.
3. Tessellate affected rectangles locally, split source triangles in authored
   crease order, splice boundary crossings, and append the refined panel once.
4. Keep Fold execution responsible only for current-frame hinge validation and
   selected-side rigid rotation; retain `FC3011` for unsupported refinement.
5. Add focused Edit Mode tests for geometry, UVs, boundaries, winding,
   determinism, budgets, prior transforms, multiple creases, and unchanged
   on-grid behavior.
6. Update public documentation, corpus expectations, changelog, and proof.
7. Run JSON/static checks, full Unity Edit Mode tests, and the M24 proof; record
   exact artifacts.
8. Commit and push the branch, open a PR, record an exact-head maintainer audit,
   require protected checks, and merge only when all gates are green.

# Test matrix

| Case | Expected evidence |
|---|---|
| `1x1`, vertical x=0.3, +90 | success; explicit crease edge; no spanning triangle |
| same crease, -90 / opposite side | documented handedness and fixed hinge |
| non-square `CanvasRect` | exact interpolated source UV |
| boundary crossings | ordered `vMin`/`vMax` insertions; other boundaries unchanged |
| shared diagonal crossing | one canonical inserted edge vertex reused by both cells |
| prior rigid transform | hinge resolves in the current frame |
| two compatible creases | authored-order deterministic topology and positions |
| on-grid and boundary crease | zero topology churn; legacy tests unchanged |
| crossing an earlier non-planar crease | stable `FC3007`; no Mesh |
| disk/interior-ending/ambiguous overlap | one stable `FC3011`; no Mesh |
| vertex/triangle budget short by one | stable budget diagnostic; no partial Mesh |
| repeated compile | equal compiled vertices, indices, topology, boundaries, diagnostics |
| full repository suite | all prior Edit Mode and repository checks pass |

# Risks and rollback

- Risk: appending split vertices during operation execution breaks contiguous
  panel ranges. Mitigation: plan and emit refined local panels once.
- Risk: separate triangle intersection calculations create cracks. Mitigation:
  canonical undirected-edge keys and one local vertex per crossing.
- Risk: clipping changes winding or emits zero-area slivers. Mitigation: stable
  side classification, duplicate removal, signed-area checks, and focused tests.
- Risk: later topology-sensitive operations assume the original grid. Mitigation:
  preserve the original corner indices, contiguous ranges, and validate Roll,
  Stitch, Solidify, sphere, torus, extension, and export regressions.
- Rollback: abandon the M24 branch; public `v1.0.1` and immutable `v1.0.0`
  remain untouched.

# Progress log

- 2026-08-13: M23 public release and qualification were live-verified; no open
  PR or external contributor claim required priority handling.
- 2026-08-13: M24 selected from the explicit roadmap because silent off-grid
  crease approximation is a core compiler correctness limitation.
- 2026-08-13: identified the panel-range hazard and chose pre-tessellation
  refinement planning before implementation.
- 2026-08-13: branch `agent/m24-crease-topology-split`, milestone contract,
  execution plan, and ADR 0011 created.
- 2026-08-13: implemented immutable authored-order source refinement, canonical
  edge intersections, stable triangle clipping, named-boundary splicing, and
  exact refined geometry-budget planning before global panel emission.
- 2026-08-13: separated the new M24 production corpus from immutable M11/M17
  release evidence; all historical release validators remain green.
- 2026-08-13: added a generated decorated canvas plus EditorOnly flat/folded
  one-sided proof and derived source wireframe. Its idempotence/resource test
  passes and both textured surfaces use the same source canvas.
- 2026-08-13: verified that refined named-boundary breakpoints survive Stitch
  correspondence resampling and then Solidify into a closed volume.
- 2026-08-13: repository workflow parity passed, Fold-focused tests passed
  30/30, proof test passed 1/1, and the complete Unity `6000.3.20f1` Edit Mode
  suite passed 491/491 with zero failures, skips, or inconclusive results.

# Decisions made

- Refinement is a deterministic compilation planning stage, not a mutable mesh
  post-process and not source data.
- The first supported domain is a straight rectangle partition whose endpoints
  lie on the perimeter. Other domains retain `FC3011`.
- Existing edge-chain creases are detected and left byte-identical.
- Multiple compatible off-grid creases refine in Fold operation order so later
  current-frame hinge validation sees the exact prior deformation.
- M24 expands compiler behavior under `[Unreleased]`; semantic-version
  publication is a separate later release milestone.

# Final verification

Local implementation verification is complete:

- repository-check workflow parity and `git diff --check`: passed;
- Unity Editor: `6000.3.20f1 (c9ba695d4f07)`;
- focused Fold tests: 30/30 passed;
- M24 Editor proof test: 1/1 passed;
- full Edit Mode: 491/491 passed, 0 failed, 0 skipped, 0 inconclusive;
- local XML: `/tmp/foldcanvas-m24/all-tests-final2.xml`;
- local Editor log: `/tmp/foldcanvas-m24/Editor-all-final2.log`.

PR head/audit SHA, hosted run URLs, merge SHA, and post-merge protected-main
checks remain pending. Curved/interior creases, falloff, topology-group
propagation, SpiralRoll, LayeredRoll, bevel, subdivision, smoothing, and cleanup
were not implemented.
