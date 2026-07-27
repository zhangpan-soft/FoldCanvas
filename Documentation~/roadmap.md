# Roadmap

The roadmap is proof-driven. A milestone is complete only when its visible object and numerical invariants both pass.

## M00: Bootstrap and repository health

**Proof:** the host project opens, package assemblies compile, JSON parses, and existing Edit Mode tests pass.

- validate package layout and metadata
- repair compile issues without architecture changes
- verify editor/runtime separation
- document exact Unity checks

## M01: Planar source domains

**Proof:** decorated rectangle and disk panels compile flat with exact UV correspondence.

- robust rectangle grid
- robust disk/ellipse tessellation
- standard ordered boundaries
- deterministic vertex/index order
- canvas-rect validation
- panel provenance

## M02: Fold and box

**Status:** complete in `0.1.0-preview.4`.

**Proof:** six rectangle regions from one generated 2D appearance canvas
compile through ordered rigid-crease operations into a six-sided box while
artwork remains on the correct faces.

- fold line selection
- positive/negative side classification
- rigid rotation about an arbitrary 2D line embedded in 3D
- operation order
- shared-edge behavior
- 90-degree acceptance tests

Acceptance evidence: Unity `6000.3.20f1`, 43/43 Edit Mode tests, and the saved
local `M02BoxPreview` scene showing all six distinct artwork regions over
multiple viewing orientations. The six panels remain unwelded by design.

## M03: Roll and cup

**Status:** implemented in `0.1.0-preview.5` on the audit branch; acceptance
remains pending until the PR is reviewed and merged.

**Proof:** a rectangular wall region bearing text and a logo rolls into a cylindrical wall; a disk becomes the base.

- roll along U/V
- preserve arc length mode
- explicit radius mode
- full and partial roll
- seam coincidence metric
- cup sample canvas
- current-frame composition after rigid translation/rotation
- documented positive/negative handedness and radial normal convention
- structured explicit-radius stretch diagnostics
- full-turn minimum of three source segments
- one-turn Circular Roll limit with stable multi-turn rejection
- congruent current-frame composition, including unit reflection
- package-owned, idempotent EditorOnly proof camera hierarchy

This milestone may display separate coincident surfaces. Topological welding belongs to M04.

### Future Roll tasks: SpiralRoll and LayeredRoll

**Status:** explicitly deferred; not part of M03 or M04.

**Proof:** multi-turn source strips use an explicit pitch or layer-spacing
contract, account for thickness and collision between turns, and avoid mapping
several source intervals onto the same cylindrical surface. Until such an
operation is active, Circular Roll angles outside `[-360, 360]` return
`FC3023 UnsupportedMultiTurnRoll`.

### Future Fold task: deterministic crease topology split

**Status:** explicitly deferred; not part of M03 or M04.

**Proof:** an off-grid crease inserts deterministic source vertices and edges,
splits every crossed triangle without changing UV correspondence, and then
executes the exact rigid fold with stable provenance and triangle ordering.

Until that task is active, any crease that is not already a continuous existing
edge chain returns `FC3011 FoldCreaseRequiresTopologySplit` and produces no
Mesh.

## M04: Stitch and solidify

**Proof:** the cup becomes a closed manifold shell with configurable wall and base thickness.

- boundary extraction
- normalized arc-length parameterization
- deterministic resampling
- weld/bridge modes
- open-boundary classification
- inner shell and rim generation
- manifold tests

## M05: Sphere gores

**Proof:** symmetric 2D gores reconstruct a closed sphere with bounded radial error and preserved artwork.

- gore panel generator/import
- spherical wrap operation or curvature field prototype
- pole handling
- neighbor seam graph
- distortion visualization
- radial error test

## M06: Authoring workspace

**Proof:** a non-modeler can create and edit the cup without touching code or vertex coordinates.

- UI Toolkit split window
- 2D canvas region editor
- 3D preview
- operation list/timeline
- boundary pairing UI
- diagnostics navigation
- undo/redo

## M07: Geometry validator

**Proof:** intentionally broken samples produce stable, localized diagnostics.

- non-manifold edges
- zero-area triangles
- inverted components
- open boundaries
- seam mismatch
- broad-phase self-intersection

## M08: FoldScript and AI loop

**Proof:** JSON round-trips, invalid AI output is rejected safely, and a diagnostic can drive a corrected second compile.

- importer/exporter
- schema validation
- canonical serialization
- repair payload
- provider-neutral adapter interface

## M09: Non-trivial topology

**Proof:** a cup handle and a torus compile through explicit loop topology.

- tube/strip strategy
- multiple closed boundaries
- handle attachments
- cyclic parameter domains
- topology expectation tests

## M10: Extensibility and ecosystem

- operation registration model
- sample/gallery format
- contributor operation template
- performance baselines
- package release automation
- optional exporters

## Deliberate non-goals before M08

- photorealistic PBR inference
- arbitrary text/image to universal 3D
- character rigging
- skeletal animation
- runtime cloud inference
- direct editing of generated mesh
- Blender replacement
- unrestricted volumetric solids
