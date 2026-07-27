# Changelog

All notable changes to FoldCanvas are documented in this file.

The format follows Keep a Changelog principles, and package versions follow semantic versioning while the API is in preview.

## [Unreleased]

No unreleased changes.

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
