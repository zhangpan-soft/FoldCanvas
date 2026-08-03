# Changelog

All notable changes to FoldCanvas are documented in this file.

The format follows Keep a Changelog principles, and package versions follow semantic versioning while the API is in preview.

## [Unreleased]

No unreleased changes.

## [0.1.0-preview.16] - 2026-08-03

### Added

- Executable FoldScript `0.1` DTOs, bounded JSON parsing, strict semantic
  validation, canonical serialization, and explicit meter/centimeter/millimeter
  conversion at the source/native boundary
- Runtime appearance-resolution contract plus Editor-safe import/export for
  explicit source paths under `Assets/` or `Packages/`
- Provider-neutral source proposal and repair interfaces, immutable compact
  repair payloads, and a repair coordinator that re-enters the ordinary
  importer and compiler gates
- M06 toolbar actions for FoldScript import/export and a Diagnostics action for
  copying the current canonical repair payload
- Stable `FC7001`-`FC7012` diagnostics for malformed, unsupported, unsafe,
  excessive, duplicate, unresolved, or invalid FoldScript and repair data
- Hostile-input, canonical round-trip, unit, path, Editor persistence,
  cup/sphere geometry, repair-loop, immutability, and provider-isolation tests

### Changed

- `FoldCanvasAsset` now retains portable source metadata and compile settings so
  imported JSON can be edited and exported without relying on Unity's internal
  serialization format
- Repository validation now checks executable FoldScript limits against the
  JSON Schema and checks the runtime package-version constant against
  `package.json`
- Package version advanced to `0.1.0-preview.16`

### Verified

- Unity `6000.3.20f1` passed all 326 Edit Mode tests, including 45 M08
  canonical, hostile-input, path, unit, Editor persistence, geometry, and
  repair-loop cases
- The live M06 workspace imported the bundled cup JSON, produced a successful
  Strict preview with 2972 vertices and 5120 triangles, exported canonical
  FoldScript, reimported it successfully, and copied a provider-neutral repair
  payload containing no Mesh field
- Repository validation, JSON parsing, Runtime/Editor assembly isolation, and
  `git diff --check` passed

### Not included

- No model provider, network transport, automatic repair, credential handling,
  binary Mesh import, M09 topology, Bevel, subdivision, smoothing, remeshing,
  or Mesh cleanup was added

## [0.1.0-preview.15] - 2026-08-03

### Added

- M07 cumulative Basic, Standard, and Strict final-geometry validation with a
  read-only `GeometryValidationReport`
- Stable diagnostics for invalid/incomplete triangle indices, duplicate
  topology faces, open boundaries, winding conflicts, disconnected geometry,
  executed-Weld gaps, bow-tie vertices, topology-position conflicts, inverted
  closed components, exact self-intersections, candidate-budget overflow, and
  degenerate compiled boundaries
- Read-only boundary, render/logical vertex, component, triangle-pair, and
  logical-edge diagnostic context for Editor navigation and future source
  repair tools
- Deterministic sweep-and-prune candidate generation and exact separating-axis
  triangle tests, including coplanar axes and a hard 250000-pair budget
- Adversarial fixtures and 28 M07 Edit Mode tests covering structural roots,
  topology and seam evidence, validation levels, determinism, non-mutation,
  self-intersecting roll/thickness cases, and valid cup/sphere regressions

### Changed

- The compiler now validates the explicit final build buffer before freezing
  compiled data or creating a Unity Mesh
- Executed Weld correspondence is retained transactionally for final seam-gap
  verification; failed Stitch transactions discard that evidence
- The M06 Diagnostics tab displays the selected validation level, component,
  open/non-manifold edge, and confirmed-intersection summary
- Package version advanced to `0.1.0-preview.15`

### Verified

- Unity `6000.3.20f1` passed all 281 Edit Mode tests, including 28 M07
  adversarial, level-gating, determinism, and false-positive regressions
- Repository validation and `git diff --check` passed

### Not included

- No automatic repair, Bevel, subdivision, smoothing, remeshing, Mesh cleanup,
  collision-aware thickness adjustment, M08 FoldScript import/AI loop, or M09
  non-trivial topology operation was added

## [0.1.0-preview.14] - 2026-07-31

### Added

- M06 UI Toolkit authoring workspace with a persistent split 2D source canvas
  and locally owned interactive 3D derived preview
- Rectangle/disk panel creation, stable renaming, numeric and handle-based
  canvas-region editing, named boundary highlighting, and seam endpoint pairing
- Ordered explicit operation forms, source-context diagnostic navigation,
  revisioned debounced compilation, and valid-only Bake controls
- Logical wireframe, panel-color, selected-seam, normal, and thickness preview
  overlays, all owned and disposed by the Editor window
- A bilingual blank-source-to-closed-cup walkthrough and 23 M06 Edit Mode tests
  covering Undo/Redo, deterministic diagnostics, stale-result rejection,
  preview ownership, full cup compilation, and protected baking

### Changed

- The legacy FoldCanvas window command now opens the M06 authoring workspace
- Package version advanced to `0.1.0-preview.14`

### Not included

- No M00-M05 geometry semantics were changed, and no M07 validator, M08
  import/export or AI loop, runtime authoring, node graph, bevel, subdivision,
  smoothing, remesh, or Mesh cleanup was added

## [0.1.0-preview.13] - 2026-07-31

### Fixed

- GameCI now writes its live Edit Mode XML and Editor log beneath the host
  project root, outside Unity-imported `Assets` and `Packages`
- CI no longer exposes a continuously changing Editor log through the
  repository-root `com.foldcanvas.core` package, preventing Unity's
  infinite-import-loop error across the Editor workflow fixture
- The post-Unity normalization step now resolves the real GameCI output,
  copies it to stable `test-results.xml` and `Editor.log` names, and rejects
  missing or empty evidence

### Changed

- Repository validation locks the non-imported GameCI staging path and the
  action output used by evidence normalization
- Package version advanced to `0.1.0-preview.13`

### Not included

- No M05 geometry, seam lifecycle, sphere validation, test assertion, or M06
  behavior was changed

## [0.1.0-preview.12] - 2026-07-31

### Added

- Deterministic source preflight for every enabled SphericalWrap dependency of
  a Stitch-selected seam endpoint
- Regression coverage for a component-forming Stitch before all wraps, a
  Stitch between member wraps, a cross-type Stitch before a wrap, deterministic
  ordering diagnostics, and the valid Wrap-to-Stitch-to-Solidify sequence
- `UNITY_SERIAL` forwarding for GameCI serial-license activation while
  preserving the existing Personal-license `UNITY_LICENSE` path

### Fixed

- A Stitch can no longer execute or trigger sphere validation before all
  enabled SphericalWrap operations targeting its selected panels
- `SphereValidationPlan` independently refuses to schedule a component when
  its last touching Stitch is not later than every member wrap
- Invalid Wrap/Stitch order now returns `FC2010` before tessellation and never
  emits `FC6014 SphereValidationFailed` or a premature sphere report

### Changed

- Package version advanced to `0.1.0-preview.12`

### Not included

- No Seam lifecycle, last-touching-Stitch, spherical mapping, topology repair,
  or M06 architecture was replaced

## [0.1.0-preview.11] - 2026-07-31

### Added

- Stable source validation for every Stitch-selected seam ID, endpoint panel
  ID, endpoint boundary ID, referenced panel, and referenced built-in boundary
- Regression coverage for default/null/empty/whitespace/missing seam
  references and sphere-to-ordinary Bridge/Weld lifecycle changes

### Fixed

- Default or malformed `BoundaryReference` values no longer reach a null-key
  `Dictionary.TryGetValue` call in `SphereValidationPlan.Build`
- Spherical component reports are now generated after the last Stitch whose
  selected seam touches the component, preventing a later cross-type Stitch
  from leaving an earlier closed report
- Solidify cannot consume sphere evidence captured before a later
  component-touching Stitch

### Changed

- Component-forming seams and component-touching Stitches are documented as
  separate deterministic planning passes
- Package version advanced to `0.1.0-preview.11`

### Not included

- No M05 mapping, pole, tessellation, geometry-budget, or topology-repair
  architecture was replaced, and no M06 behavior was added

## [0.1.0-preview.10] - 2026-07-30

### Added

- Component-scoped `SphereReports` with ordered panel/wrap identities,
  validation stage, operation ID, and operation index
- One cumulative `GeometryBudget` for panel tessellation, Stitch
  subdivision/Bridge geometry, and Solidify shell/rim geometry, backed by
  build-buffer hard limits and operation rollback
- Native ScriptableObject `sampleCount` validation using the same `8192`
  maximum as the JSON Schema
- Scale-aware pole classification using angular deviation, sphere radius, and
  the configured positional tolerance
- Three-pass golden-sphere deterministic hash coverage and independent
  open/closed multi-component regression cases
- A real Unity `6000.3.20f1` Edit Mode GitHub Actions job that uploads NUnit
  results and the Editor log

### Fixed

- Unrelated Solidify no longer suppresses zero-thickness sphere validation,
  and unrelated Stitch no longer triggers it
- An open spherical component cannot use a later Solidify shell to bypass its
  required pre-Solidify validation
- Stitch correspondence now batches sorted insertions with cached edge
  adjacency instead of rescanning every triangle for each sample
- Null, empty, whitespace, and missing spherical panel references return
  stable diagnostics instead of dictionary-key exceptions
- Budget or later seam failure rolls back a complete Stitch/Solidify
  transaction without retaining partial geometry or consumed budget

### Changed

- Package version advanced to `0.1.0-preview.10`
- Closed-sphere documentation now states that M05 proves topology,
  manifoldness, radius, frame, poles, and winding, but does not run global
  triangle-triangle self-intersection detection
- Repository checks now enforce Schema/native sample-limit consistency,
  Unity workflow presence, and package-version/CHANGELOG consistency

### Not included

- Global triangle-triangle self-intersection detection, Bevel, subdivision,
  smoothing, Remesh, Mesh Cleanup, automatic topology repair, and M06 remain
  intentionally unimplemented

## [0.1.0-preview.9] - 2026-07-30

### Added

- `SphericalWrapOperationDefinition` with explicit radius, latitude and
  longitude ranges, U/V direction, pole mode, and panel-grid subdivision
- Deterministic current-frame spherical mapping that preserves source
  positions, canvas UVs, panel ownership, provenance, ordered boundaries, and
  outward winding
- Pole-aware tessellation with `Merge` and `KeepFan` render policies backed by
  one logical north-pole and one logical south-pole identity
- Curved seam subdivision that re-evaluates inserted source samples on their
  spherical map instead of leaving chord points inside the requested radius
- Read-only spherical-surface metadata and `FoldCanvasSphereReport` validation
  for radius error, edge incidence, orientation, components, Euler
  characteristic, and pole topology
- Stable M05 diagnostics `FC6001` through `FC6015`
- An importable eight-gore FoldScript and deterministic 2048 x 1024 source
  canvas with visible `NORTH`, `FOLDCANVAS`, `SOUTH`, equator, and gore markers
- An idempotent package-owned `EditorOnly` proof with source canvas, textured
  sphere, texture-free one-sided solid, logical wireframe, seam and pole
  overlays, UV-stretch and radius-error views, validation report, and owned
  preview camera

### Changed

- Terminal-Stitch ordering also rejects a later per-panel `SphericalWrap`
  until shared-topology deformation propagation exists
- The package status, schema, architecture, compiler pipeline, field reference,
  geometry model, diagnostics, editor workflow, and roadmap now define the M05
  spherical reconstruction contract
- Package version advanced to `0.1.0-preview.9`, with the explicit-gore sphere
  exposed as an importable Package Manager sample

### Verified

- The golden asset derives one closed outward sphere from eight explicit 2D
  panels: 616 render vertices, 482 logical topology vertices, 960 triangles,
  1,440 logical edges, Euler characteristic 2, zero open or non-manifold
  edges, and one logical topology identity at each pole
- The measured maximum radial error is `0 m` for the golden asset; unequal seam
  sampling also remains on the requested spherical radius
- Unity `6000.3.20f1` passed all 174 Edit Mode tests and rendered the textured,
  one-sided solid, wireframe/seam, UV-stretch, and radius-error proof views

### Not included

- Unity Sphere/UV Sphere/Icosphere generation, imported or fixed sphere
  meshes, automatic topology repair, bevel, subdivision, remesh, mesh cleanup,
  and M06 remain intentionally unimplemented

## [0.1.0-preview.8] - 2026-07-30

### Added

- Immutable aggregate and per-component closed-volume reports with logical
  edge incidence, winding conflicts, topology-position agreement, connected
  components, and signed/absolute volume
- An operation-scoped Solidify closure gate with stable
  `FC4007 SolidifyClosedVolumeValidationFailed` structural values
- Deterministic paired outer/inner hard-corner segment metadata referencing the
  actual emitted shell vertices
- A separate M04.1 `Cup ClosedVolume` source example and idempotent
  `EditorOnly` proof hierarchy
- Texture-free one-sided solid, unique logical-topology wireframe, exact
  triangle/plane section lines, and generated `OuterCorner` / `InnerCorner`
  overlays

### Changed

- Successful compile results now expose `ClosedVolumeReport`; each successful
  Solidify also exposes its selected-shell report independently from unrelated
  panels
- M04 architecture, pipeline, geometry, diagnostics, editor workflow, sample,
  roadmap, and active plan now define the bounded closed-volume contract

### Verified

- The production cup reports one component, 7,680 unique logical edges, zero
  open/non-manifold/orientation-conflict edges, 64 paired wall-bottom corner
  segments, and non-zero material volume
- Unity `6000.3.20f1` passed all 152 Edit Mode tests, and rendered the
  texture-free solid, logical-wireframe, and vertical-section proof views

### Not included

- Bevel, subdivision, smoothing, and mesh-cleanup postprocessing remain
  intentionally unimplemented

## [0.1.0-preview.7] - 2026-07-29

### Added

- Deterministic normalized arc-length boundary correspondence that preserves
  authored breakpoints, adds `sampleCount` as a minimum-density grid, inserts
  missing samples into adjacent source triangles, and interpolates source
  positions and UV0
- Reusable unequal-count `Weld` and `Bridge` execution with logical topology
  identity preserved independently from render-vertex attribute splits
- `Solidify` support for inward, outward, and centered thickness, reversed
  inner-shell winding, shared offset-plane miters at welded hard corners, and
  side walls only on true open topology edges
- Stable M04 diagnostics `FC2010`–`FC2013` and `FC4001`–`FC4006`
- `M04ProductionCupCanvas.png`, an atlas-safe bilinear proof texture with
  square wall coverage and 12-pixel bottom-perimeter bleed
- A texture-free one-sided solid diagnostic material and package-owned M04
  exterior, exact-side, interior, and underside cameras

### Changed

- `sampleCount` now requests minimum seam correspondence density instead of
  requiring an already equal boundary count
- Stitch is terminal for later `RigidTransform`, `Fold`, or `Roll` operations
  on every panel it selected until topology-group deformation propagation is
  implemented; `Solidify` may consume the complete stitched component
- Topology validation now runs for all generated geometry, not only after a
  Weld
- Package status, JSON Schema, compiler pipeline, field reference, sample
  guide, and roadmap now describe the implemented M04 contracts

### Fixed

- High-resolution disk triangles use explicit magnitude normalization during
  Solidify, avoiding Unity's small-vector normalization cutoff
- Wall and bottom inner offsets are solved at one shared welded miter, so the
  thick cup cannot open at the inner corner
- The result proof no longer mistakes dark atlas pixels for a geometric crack;
  geometry is judged first with a solid material and then with bleed-safe art
- M04 preview cameras do not use or modify `Camera.main`, default to a normal
  exterior view, and isolate the retained M03 presentation preview

### Verified

- Unity `6000.3.20f1` compiled a thick cup with 2,972 render vertices, 2,562
  logical topology vertices, and 5,120 triangles
- The production cup reports `0 m` wall-to-bottom gap, `0.004 m` measured
  bottom-center thickness, zero open topology edges, and zero non-manifold
  edges
- Unity `6000.3.20f1` passed all 143 Edit Mode tests; JSON,
  assembly-reference, repository, and diff checks also passed

## [0.1.0-preview.6] - 2026-07-27

### Added

- An explicit M03 equal-sample `Weld` execution gate for ordered
  `StitchOperationDefinition` seam lists
- Deterministic logical topology identity through `TopologyVertexId`, while
  retaining render-vertex splits required by source UVs and provenance
- Stable Stitch diagnostics for missing or duplicate seams, missing
  boundaries, unsupported seam modes, sample-count mismatch, excessive
  boundary distance, empty Stitch lists, invalid weld tolerance, and
  non-manifold welded topology
- Cup acceptance checks proving the wall-side seam and bottom perimeter are
  welded and that only the 64-edge top rim remains open

### Changed

- The M03 cup source now executes `close-wall` and `attach-bottom` Weld seams
  after Roll and bottom placement
- Circular Roll uses the documented exterior-readable angular mapping and
  reverses target triangle winding deterministically for positive outward
  faces
- The package, schema, pipeline, field reference, sample guide, roadmap, and
  active plan now distinguish logical topology welding from UV/provenance
  render-vertex splits

### Fixed

- The cup base and wall are snapped to identical welded topology instead of
  remaining merely close in space
- Exterior wall artwork reads left-to-right; the two-sided proof shader also
  mirrors only back-face sampling so orbiting the zero-thickness preview does
  not show reversed text

### Verified

- Unity `6000.3.20f1` compiled the package and passed all 103 Edit Mode tests
- The welded cup compiles to 1,358 render vertices, 1,281 logical topology
  vertices, and 2,496 triangles, with exactly 64 open top-rim edges
- JSON parsing, repository validation, and `git diff --check` passed

## [0.1.0-preview.5] - 2026-07-27

### Added

- Deterministic rectangle `Roll` along U or V in the target panel's current
  rigid frame, with preserve-arc-length and explicit-radius modes
- Stable Roll diagnostics for invalid parameters, unsupported embeddings,
  unsupported boundary fitting, insufficient closed-turn tessellation, and
  unsupported multi-turn Circular Roll
- Ordered structured diagnostic values and repair-suggestion storage, including
  explicit-radius `sourceSpan`, `arcLength`, and `stretchRatio`
- A generated `GPT 5.6` / `CODEX` cup source canvas, cup asset, bake command,
  editor proof, package-owned opaque two-sided Unlit preview shader, and one
  inactive-aware `EditorOnly` preview hierarchy with its own untagged camera
- Edit Mode coverage for current-frame composition, signed handedness and
  normals, open/closed rolls, seam declaration behavior, structured
  diagnostics, tessellation, cup alignment, and preview culling

### Changed

- Declared seams remain inert source data until an explicit Stitch operation;
  Stitch and `FitTargetBoundary` each return one stable root-cause diagnostic
- Roll, Fold, Seam, diagnostic, pipeline, field-reference, schema, roadmap, and
  sample documentation now state the audited M03 contracts
- Full-turn Circular Roll now requires at least three source segments, and the
  Roll schema limits signed sweeps to one turn (`-360` through `+360`)
- Roll embedding compatibility is defined from final congruent planar geometry:
  unit reflection may pass, while metric-changing scale, shear, collapsed axes,
  and non-planarity fail
- Package metadata now describes planar, rigid-fold, and circular-roll support

### Fixed

- Off-grid Fold creases now stop with
  `FC3011 FoldCreaseRequiresTopologySplit` instead of stretching triangles
- The M03 interactive cup proof no longer loses half of its wall or bottom
  while orbiting; double-sided rendering is preview-only and does not add M04
  inner-wall, thickness, Stitch, or welding topology
- The M03 proof no longer reads or modifies `Camera.main`, does not duplicate
  owned objects on repeated runs, and numerically rejects a wall/bottom fit
  outside the seam-proof tolerance before showing the scene

### Verified

- Unity `6000.3.20f1` compiled the package and passed all 90 Edit Mode tests
- The real Unity Editor regenerated the 1,358-vertex, 2,496-triangle cup and
  displayed the complete wall plus both sides of the bottom across side,
  underside, and reverse viewing orientations
- JSON, assembly-reference, repository, and diff checks passed

## [0.1.0-preview.4] - 2026-07-27

### Added

- Deterministic rigid-crease `Fold` execution in serialized operation order
- Source-line embedding through each panel's deterministic triangulation into
  its current 3D surface
- Positive/negative source-side selection with Unity
  `Quaternion.AngleAxis` handedness
- Stable M02 diagnostics for missing targets, non-finite/out-of-range/
  degenerate lines, ambiguous current hinges, non-finite angles, nonzero
  falloff, and invalid sides
- A six-region generated appearance canvas, six-panel source asset, bake
  command, and real-editor box proof
- Numerical coverage for zero-degree identity, signed 90-degree rotation,
  hinge fixation, axis-distance preservation, ordered current-hinge
  resolution, source/UV/provenance preservation, determinism, box bounds,
  artwork mapping, and outward face normals

### Changed

- Internal panel build records retain deterministic triangle spans for
  source-to-current interpolation
- The compiler window now describes Fold as implemented while keeping Roll,
  Stitch, and Solidify explicit later milestones
- FoldScript field, pipeline, editor-workflow, diagnostics, schema, roadmap,
  and bilingual README documentation now describe the implemented M02
  semantics
- FoldScript schema line endpoints are constrained to normalized `[0,1]`
  coordinates

### Fixed

- M02's 384×256 generated proof canvas now disables Unity's default NPOT
  resizing so its six source regions import at exact pixel dimensions
- A fold line crossing a prior non-linear crease now stops with `FC3007`
  instead of selecting an arbitrary current axis
- The GUID-stable rebake test no longer assumes Unity preserves one managed
  object wrapper across `AssetDatabase.Refresh`; path, GUID, and updated mesh
  data remain the acceptance contract

### Verified

- Repository validation, JSON parsing, assembly-reference inspection, and diff
  checks passed
- Unity `6000.3.20f1` compiled the package and passed all 43 Edit Mode tests
- The real Unity Editor generated a 24-vertex, 12-triangle box and displayed
  all six distinct artwork regions from multiple viewing orientations
- The local proof scene was saved as
  `Assets/FoldCanvasGenerated/M02BoxPreview.unity`

## [0.1.0-preview.3] - 2026-07-27

### Added

- M01 immutable compiled geometry data with current positions, panel-local
  source positions, source-canvas UVs, panel ownership, provenance IDs, and
  ordered named boundaries
- Deterministic rectangle and disk/ellipse metadata, mesh-conversion, winding,
  source-preservation, and immutability coverage
- Pre-allocation cumulative vertex and triangle safety limits
- Stable diagnostics for empty panel IDs, unsupported shapes, non-finite or
  non-positive sizes, invalid compile limits, and excessive tessellation
- A decorated rectangle-and-ellipse planar proof sample
- Bilingual project background explaining the AI 2D-to-programmable-3D
  production problem
- A field-by-field FoldScript JSON reference, including exact `roll`
  direction, angle, radius, UV, seam, and implementation-status semantics
- Self-documenting JSON Schema descriptions and conditional `roll` radius
  requirements

### Changed

- Unity `Mesh` creation is now the final adapter step after compiler-owned data
  is frozen
- The bootstrap sample upgrades its legacy circular panel in place to an
  ellipse without replacing asset GUIDs
- README status and package documentation now begin with the project rationale
  and link to the complete asset-configuration contract

### Fixed

- Removed an undocumented `coordinateSpace` implication from the FoldScript
  `0.1` fold definition
- Clarified that `roll` is a continuous surface mapping, not an Euler rigid
  rotation, and that coincident edges remain unwelded

### Verified

- Repository validation and JSON parsing passed
- Unity `6000.3.20f1` compiled the package and passed all 27 Edit Mode tests
- The real Unity Editor rendered the saved `M01PlanarPreview` scene with 192
  vertices, 320 triangles, a decorated rectangle, and a visibly non-circular
  decorated ellipse

## [0.1.0-preview.2] - 2026-07-25

### Added

- M00 editor workflow tests for idempotent sample creation and GUID-preserving rebakes
- Determinism, winding, UV retention, target isolation, and stable diagnostic-order coverage
- Repository validation for JSON, asmdefs, local documentation links, ignored outputs, and sample references

### Fixed

- Removed the invalid `Samples~.meta` file that caused Unity package-import warnings
- Corrected the bundled future FoldScript example to reference its actual appearance canvas
- Aligned the documented `FC2002` diagnostic name with the bootstrap compiler
- Corrected GitHub Issue Forms to use the required `description` field
- Ignored host-project sample assets generated by the bootstrap menu

### Verified

- Unity `6000.3.20f1` compiled all package assemblies
- All 13 M00 Edit Mode test cases passed
- The real editor created the sample twice, compiled 192 vertices and 320 triangles in memory, and rebaked one stable mesh GUID

## [0.1.0-preview.1] - 2026-07-25

### Added

- Initial UPM package scaffold
- FoldCanvas source asset model
- Rectangle and disk tessellation
- Rigid transform operation
- Diagnostic model
- Editor bake window and bootstrap sample command
- Edit Mode tests
- FoldScript schema draft
- Architecture and Codex milestone documentation
