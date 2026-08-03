# Current task

Execute **M09: cyclic topology, handle cup, and torus**.

Authoritative task file:
[`Codex/M09_TOPOLOGY.md`](Codex/M09_TOPOLOGY.md)

M08 PR #7 was human-approved and merged into `main` as `dcc8574`. M09
development occurs on `codex/m09-topology`, created from that merged commit.

M09 proves that FoldCanvas can express deterministic loop topology from its
authoritative two-dimensional source:

- map one rectangle parameter panel to a toroidal surface through an explicit
  `ToroidalWrap` operation;
- close the torus U and V cycles only through declared Weld seams selected by
  Stitch;
- add optional normalized, non-wrapping boundary spans so a seam can select a
  stable sub-chain of an authored boundary;
- form a cup handle from an ordinary rectangular strip using existing rigid
  transforms and edge-aligned folds;
- weld the strip's two attachment boundaries to two cup-rim spans before one
  final Solidify;
- prove deterministic topology, UV behavior, radii, manifoldness, and closed
  volume through Edit Mode tests and visible Unity proof assets.

ADR 0007 defines the M09 topology vocabulary. Generated Meshes remain derived
artifacts; the 2D canvas, panels, boundary spans, seams, and ordered operations
remain the editable source.

M09 does not implement arbitrary sweep paths, circular holes in panel
interiors, Boolean/CSG operations, bevels, subdivision, smoothing, remeshing,
mesh cleanup, skeletal deformation, or later milestones.
