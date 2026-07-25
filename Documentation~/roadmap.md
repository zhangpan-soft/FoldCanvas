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

**Proof:** one cross-shaped 2D box net folds into a six-sided box while artwork remains on the correct faces.

- fold line selection
- positive/negative side classification
- rigid rotation about an arbitrary 2D line embedded in 3D
- operation order
- shared-edge behavior
- 90-degree acceptance tests

## M03: Roll and cup

**Proof:** a rectangular wall region bearing text and a logo rolls into a cylindrical wall; a disk becomes the base.

- roll along U/V
- preserve arc length mode
- explicit radius mode
- full and partial roll
- seam coincidence metric
- cup sample canvas

This milestone may display separate coincident surfaces. Topological welding belongs to M04.

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
