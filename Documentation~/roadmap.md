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

**Status:** accepted and merged in `0.1.0-preview.6`.

**Proof:** a rectangular wall region bearing text and a logo rolls into a cylindrical wall; a disk becomes the base.

- roll along U/V
- preserve arc length mode
- explicit radius mode
- full and partial roll
- explicit equal-sample wall closure and bottom Weld
- logical topology identity across UV/provenance attribute seams
- seam coincidence metric and exact representative snapping
- cup sample canvas
- current-frame composition after rigid translation/rotation
- documented positive/negative handedness and radial normal convention
- structured explicit-radius stretch diagnostics
- full-turn minimum of three source segments
- one-turn Circular Roll limit with stable multi-turn rejection
- congruent current-frame composition, including unit reflection
- package-owned, idempotent EditorOnly proof camera hierarchy

Acceptance evidence: PR #1 was human-approved and merged into `main` at
`c7b1e61`, retaining reviewed head `96d1688`. Unity `6000.3.20f1` passed
103/103 Edit Mode tests. The live proof showed readable exterior artwork,
explicit wall and bottom Welds, 1,281 logical topology vertices, exactly 64
open top-rim edges, and zero measured wall/bottom seam gap.

M03 does not perform general boundary resampling, Bridge, Solidify, thickness,
or inner-shell generation. Its narrow Weld gate requires existing equal sample
counts and leaves only the cup's top rim open.

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

## M04: General Stitch and solidify

**Status:** implemented, human-audited, and merged through PR #3.

**Proof:** the cup becomes a closed manifold shell with configurable wall and base thickness.

- general boundary extraction
- normalized arc-length parameterization
- deterministic resampling
- resampled weld and bridge modes
- Stitch is terminal for every selected panel until topology-group deformation
  propagation exists; later per-panel RigidTransform/Fold/Roll/SphericalWrap
  fails
- open-boundary classification
- inner shell and rim generation
- manifold tests
- solid-color and bleed-safe production-canvas proof from exterior, exact-side,
  interior, and underside views

### M04.1: Closed Volume Validation

**Status:** implemented, human-audited, and merged through PR #3.

**Proof:** the same production 2D cup canvas and geometry program compile into
one non-zero closed volume, with inspectable logical wireframe, vertical
section, and generated inner/outer wall-bottom corner rings.

- immutable aggregate and per-component closed-volume reports
- exactly two oppositely directed uses per logical edge
- one position per logical topology identity
- non-zero absolute signed volume per component
- operation-scoped Solidify closure gate
- generated `OuterCorner` and `InnerCorner` hard-edge segments
- Editor-only texture-free solid, logical-wireframe, and section proof

M04.1 does not implement bevel, subdivision, smoothing, mesh cleanup, or
robust global self-intersection repair. M07 remains the broader broken-geometry
diagnostic milestone.

## M05: Sphere gores

**Status:** implemented, human-audited, and merged.

**Proof:** eight explicit rectangular 2D gores reconstruct one closed outward
sphere with preserved artwork, bounded radial error, one logical north pole,
one logical south pole, and Euler characteristic 2.

- `SphericalWrap` in each panel's resolved current congruent planar frame
- explicit radius, latitude/longitude ranges, U/V parameter direction, pole
  mode, and panel-grid subdivision fields
- pole-aware source tessellation with `Merge` and `KeepFan` render policies
- neighbor seam graph using the reusable M04 normalized arc-length solver and
  Weld
- curved seam insertion that re-evaluates source samples on the sphere
- read-only radius, winding, component, edge-incidence, Euler, and pole report
- deterministic 2048 x 1024 source artwork plus textured, one-sided solid,
  wireframe/seam/pole, UV-stretch, and radius-error Editor views

M05 does not use a Unity Sphere/UV Sphere/Icosphere, imported or fixed sphere
Mesh, automatic topology repair, bevel, subdivision, remesh, mesh cleanup, or
M06 authoring UI.

## M06: Authoring workspace

**Status:** merged into `main` through PR #5.

**Proof:** a non-modeler can create and edit the cup without touching code or vertex coordinates.

- UI Toolkit split window
- cursor-centered zoom, bounded pan, rectangle/disk canvas handles, and named
  boundary highlighting
- locally owned orbiting 3D preview with frame, logical wireframe, panel color,
  seam, normal, and thickness overlays
- ordered explicit operation forms for RigidTransform, Fold, Roll, Stitch,
  Solidify, and SphericalWrap
- boundary pairing UI with deterministic A/B highlighting
- deterministic structured diagnostics with source-context navigation
- Undo/Redo, revisioned debounce, stale-result rejection, disposable preview
  resources, and valid-only explicit Bake
- documented blank-source-to-closed-cup walkthrough

M06 does not edit generated Mesh topology, add runtime authoring, introduce a
node graph, or implement M07 self-intersection/broken-geometry validation.

## M07: Geometry validator

**Status:** implemented, human-audited, and merged in PR #6.

**Proof:** intentionally broken samples produce stable, localized diagnostics.

- Basic structural gates for bad/incomplete indices, non-finite positions,
  collapsed/zero-area/duplicate faces, non-manifold edges, and winding
  conflicts
- Standard logical-position, bow-tie, compiled-boundary, executed-Weld,
  component-orientation, open-boundary, and disconnected-component evidence
- Strict deterministic sweep-and-prune plus exact non-adjacent triangle
  intersection, with a hard candidate-pair budget
- read-only component, seam, and intersection report plus localized boundary,
  vertex, topology, component, triangle-pair, and edge context
- adversarial bow-tie, duplicate, inverted, open-seam, zero-boundary,
  self-intersecting roll, and thickness-overlap fixtures

M07 reports but does not repair geometry. It does not add Bevel, subdivision,
smoothing, remeshing, Mesh cleanup, or M08 import/AI behavior.

## M08: FoldScript and AI loop

**Status:** human-approved and merged through PR #7 as `dcc8574`.

**Proof:** JSON round-trips canonically, invalid AI output is rejected safely,
and a diagnostic drives a corrected second compile through the ordinary gates.

- bounded Runtime importer/exporter over explicit public DTOs
- strict schema/semantic/reference/limit validation and safe appearance paths
- fixed-order invariant canonical serialization with preserved source arrays
- explicit unit-aware DTO/native conversion and Editor source persistence
- immutable compact repair payload and corrected-response coordinator
- provider-neutral proposer/repairer interfaces with no SDK or network

M08 does not choose or call a model, store credentials, automatically accept a
repair, import binary geometry, or change M00-M07 geometry semantics.

## M09: Non-trivial topology

**Status:** human-approved and merged through PR #8 as `7be4117`.

**Proof:** one command creates two editable 2D sources and derived Unity views:
a rectangular parameter panel mapped into a torus whose two cycles close only
through explicit Weld seams, and a cup whose ordinary rectangular strip handle
is positioned, folded twice, attached to two top-rim spans, and Solidified with
the cup into one closed volume.

- optional normalized non-wrapping boundary-reference spans, selected before
  `reverseB`, with deterministic off-grid source-triangle subdivision
- explicit `ToroidalWrap` in the panel's current congruent planar frame
- signed major/minor angle ranges and U/V axis assignment
- `majorRadius > minorRadius > 0`, one-turn limits, and full-turn sampling gates
- outward winding without two-sided-material dependence
- two explicit seam closures and retained UV attribute seams for the torus
- reusable cup-rim span attachments using existing RigidTransform, Fold,
  Stitch, and Solidify operations
- torus Euler characteristic `0`, manifoldness, radius, winding, UV, and
  deterministic-output tests
- handle attachment gap/incidence plus final connected closed-volume tests
- owned Editor-only textured, solid-color, wireframe, source-canvas, camera,
  and topology-report proof objects

M09 does not add an arbitrary sweep path, interior panel holes, Boolean/CSG,
implicit proximity welding, multiple toroidal turns, bevel, subdivision,
smoothing, remeshing, or Mesh cleanup. The appearance canvas, panels, boundary
spans, seam graph, and ordered operations remain source; Meshes remain derived.

## M10: Extensibility and ecosystem

**Status:** maintainer-audited and merged through PR #9 as `b67120f`.

**Proof:** one Editor command compiles an ordinary 2D rectangle through a
contributor-defined wave operation in a separate assembly, using an explicit
per-compile registry, then displays textured and one-sided solid results and
exports the same immutable compiled data to deterministic OBJ.

- explicit per-compile operation registry with stable descriptor order and no
  global/reflection discovery
- single-panel position-only execution context preserving source UV,
  provenance, triangle order, boundaries, topology, and geometry budget
- pre-tessellation target validation plus complete position rollback for
  failed, non-finite, or throwing extensions
- versioned bounded sample-gallery manifest, JSON Schema, and Editor browser
- compiling contributor wave-operation template in a Runtime-only sample
  assembly
- deterministic dependency-free OBJ text export with an Editor asset-path
  adapter
- maintained planar, Roll, and registered-wave performance evidence whose
  timing never affects geometry
- byte-reproducible allowlisted UPM archive, SHA-256 evidence, and tag-gated
  GitHub release workflow
- owned EditorOnly ecosystem proof that never reads or modifies `Camera.main`

M10 does not expose topology/triangle/boundary/budget mutation, invent a custom
FoldScript `0.1` codec, add global registration, introduce a new geometry
family, add CSG/bevel/remesh, export glTF/FBX/USD, or perform runtime file or
network I/O. Meshes, OBJ, gallery views, reports, and release archives remain
derived artifacts.

## M11: Production-readiness foundation

**Status:** active on `codex/m11-production-readiness`.

**Proof:** a freshly built deterministic UPM archive installs into a generated
clean Unity host; consumer-owned code compiles through public API only and emits
repeatable geometry, OBJ, diagnostic, package-resolution, XML, and Editor-log
evidence.

- clean-host archive installation with no repository-package fallback
- consumer-owned public API compile/export fixture
- checked-in normalized public Runtime API signature baseline
- package/FoldScript/Unity compatibility and migration policy
- explicit trusted-code model for native operation executors
- representative valid/invalid production acceptance corpus
- independent hosted clean-install job with mandatory evidence artifacts

M11 does not change geometry equations or topology, publish `1.0.0`, add a
package dependency, expose topology mutation, or add runtime file/network I/O.

## M12: Production asset handoff

**Status:** planned; not active.

**Proof direction:** one authored source bundle can be handed to another Unity
project with appearance assets, canonical FoldScript, validation evidence, and
derived runtime-ready outputs while retaining exact source ownership and
rebuild instructions.

## M13: Robustness and scale

**Status:** planned; not active.

**Proof direction:** bounded fuzz/property cases, large deterministic assets,
interruption/retry behavior, memory/time budgets, and long-running regression
evidence fail safely without partial source or Mesh state.

## M14: 1.0 release candidate

**Status:** planned; not active.

**Proof direction:** supported Unity/version matrix, frozen public API and
FoldScript compatibility policy, clean-install corpus, documentation, license,
security, issue-response, release, and rollback gates are all satisfied before
an explicit `1.0.0` decision.

## Deliberate product non-goals

- photorealistic PBR inference
- arbitrary text/image to universal 3D
- character rigging
- skeletal animation
- runtime cloud inference
- direct editing of generated mesh
- Blender replacement
- unrestricted volumetric solids
