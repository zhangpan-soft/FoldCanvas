# Goal

Complete M01 planar-panel provenance, immutable compiled metadata, ordered
boundaries, centralized validation, and deterministic Unity mesh conversion
without implementing any deformation or topology milestone.

# User-visible proof

- A decorated rectangle and ellipse compile flat in Unity with the expected
  source-canvas regions on their corners, center, and perimeter.
- The compile result exposes immutable vertex provenance and panel metadata.
- Rectangle and ellipse named boundaries can be inspected in their documented
  order.
- Invalid source and unsafe tessellation requests stop with stable diagnostics.

# Scope

- Replace the bootstrap-only parallel vertex/UV buffer with compiler-owned
  vertex records containing current 3D position, panel-local 2D source
  position, source-canvas UV, panel ownership, and provenance ID.
- Freeze compiler output into public read-only result metadata before creating
  the final Unity `Mesh`.
- Standardize ordered rectangle and disk/ellipse boundary metadata.
- Centralize panel source validation and geometry tolerances.
- Add configurable generated-vertex and generated-triangle safety limits.
- Add focused Edit Mode tests for every M01 acceptance criterion.
- Update M01 documentation, schema, version, and changelog after validation.

# Non-goals

- Fold, Roll, Stitch, Solidify, thickness, seam welding, or topology changes.
- Polygon, mask, spline, hole, or adaptive tessellation.
- Runtime authoring UI, a general 3D preview workspace, render-pipeline
  integration, network services, or AI-provider integration.
- Replacing the 2D source with any opaque mesh-generation API.

# Files expected to change

- `Docs/Plans/active-plan.md`
- `Runtime/Compiler/*` for compiled data, source validation, tolerances, and
  Unity mesh conversion
- `Runtime/Data/FoldCanvasCompileSettings.cs`
- `Runtime/Diagnostics/FoldCanvasDiagnostic.cs`
- `Tests/Editor/PlanarPanelCompilerTests.cs`
- `Tests/Editor/SourceValidationTests.cs`
- `Documentation~/compiler-pipeline.md`
- `Documentation~/diagnostics.md`
- `Schema/foldcanvas.schema.json`
- package/version task and changelog files only after acceptance passes

# Geometry invariants

- Panel samples start in panel-local XY coordinates in meters with `z = 0` and
  front normal `+Z`.
- Rectangle vertices are emitted row-major from bottom-left to top-right.
- Rectangle boundaries are ordered `uMin`, `uMax`, `vMin`, `vMax`; their sample
  directions are bottom-to-top, bottom-to-top, left-to-right, and
  left-to-right respectively.
- Disk/ellipse vertices emit one center followed by concentric rings; each ring
  starts at `+X` and advances counter-clockwise when viewed from `+Z`.
- The disk/ellipse `perimeter` is the outer ring in that same counter-clockwise
  order and is not closed by duplicating its first index.
- Triangle winding faces `+Z`.
- Rigid transforms modify only current 3D position. Source-local coordinates,
  UV, ownership, provenance, topology, and boundary order remain unchanged.
- Provenance IDs are assigned in deterministic global vertex order and are
  preserved by later vertex transforms.
- Dictionary lookup may resolve an ID, but dictionary enumeration never emits
  vertices, panels, boundaries, triangles, or diagnostics.
- Source rect and coordinate comparisons in tests use `1e-6`; generated
  triangle validity uses one centralized squared double-area threshold.

# Implementation steps

1. Map M01 criteria to the current compiler and document representation gaps.
2. Add immutable compiled vertex, panel, and boundary result types.
3. Refactor the internal build buffer and tessellators to retain source-local
   coordinates, ownership, provenance, and ordered boundaries.
4. Centralize panel validation, size/tessellation estimates, and configured
   cumulative safety limits before any geometry allocation.
5. Convert frozen compiled data into Unity `Mesh` as the final compiler step.
6. Add deterministic, UV, ellipse-radius, winding, provenance, immutability,
   boundary-index, and validation tests.
7. Run repository validation and the complete Unity Edit Mode suite.
8. Verify decorated rectangle/ellipse output and metadata in the real Unity
   Editor.
9. Update documentation, changelog, package version, and `CURRENT_TASK.md` only
   after all M01 acceptance criteria pass.

# Test matrix

| Acceptance area | Automated evidence |
| --- | --- |
| Immutable compiled representation | read-only vertex/panel/boundary views reject mutation |
| Per-vertex source provenance | exact source position, UV, panel index, and provenance assertions |
| Rectangle UV and boundaries | exact four corners plus ordered boundary index arrays |
| Disk/ellipse mapping | center UV plus expected physical radii and counter-clockwise perimeter |
| Winding | every planar triangle cross product has positive Z |
| Transform preservation | current position changes while source/provenance metadata remains byte-equivalent |
| Determinism | repeated compiles have identical vertex records, indices, panels, boundaries, and Unity arrays |
| Empty/duplicate IDs | distinct stable diagnostics in source order |
| Shape and dimensions | unknown enum, non-finite size, and non-positive size diagnostics |
| Canvas rect | non-finite and out-of-range rect diagnostics |
| Tessellation safety | minimum-count and configured cumulative-limit diagnostics without allocation |

# Risks and rollback

- New public compiled metadata becomes an API commitment. Types will expose
  value data and read-only collections only; existing `Mesh` and diagnostics
  APIs remain intact.
- Large integer tessellation products can overflow. Estimates use checked
  64-bit arithmetic and report excessive tessellation before allocation.
- Unity collection APIs may copy arrays differently across versions. Tests
  compare emitted ordered values, and the conversion remains a single explicit
  final step.
- Existing M00 editor baking depends on `result.Mesh`; that property and bake
  behavior remain unchanged.
- Any need for a dependency, render pipeline, or architecture change stops the
  milestone and requires a new ADR plus explicit authorization.

# Progress log

- 2026-07-25: Re-read `CURRENT_TASK.md`, `PLANS.md`,
  `Documentation~/architecture.md`, `Codex/M01_PLANAR_PANELS.md`, and ADRs
  0001-0006.
- 2026-07-25: Audited runtime compiler, source types, diagnostics, schemas,
  docs, and current tests against every M01 criterion.
- 2026-07-25: Confirmed M01 is active and M02+ remain out of scope.
- 2026-07-25: Added compiler-owned immutable vertex, panel, and ordered
  boundary data, then moved Unity `Mesh` construction to the final adapter.
- 2026-07-25: Added centralized source validation, checked geometry estimates,
  cumulative allocation limits, stable diagnostics, and M01 Edit Mode tests.
- 2026-07-27: Added the project-background and complete FoldScript JSON field
  reference, including explicit `roll` semantics and schema descriptions.
- 2026-07-27: Upgraded the clean sample contract from a circular disk to a
  visibly non-circular ellipse while preserving legacy generated asset GUIDs.
- 2026-07-27: Completed repository, Unity Test Runner, bake, and real-editor
  visual verification; M02+ geometry was not implemented.

# Decisions made

- Public M01 output will be `FoldCanvasCompiledData` containing immutable
  `FoldCanvasCompiledVertex` records and ordered `FoldCanvasCompiledPanel` /
  `FoldCanvasCompiledBoundary` metadata.
- A vertex stores current 3D position, panel-local 2D source position,
  source-canvas UV, zero-based source panel index, and deterministic provenance
  ID. The panel ID is resolved through the indexed compiled panel metadata.
- Boundary lookup uses an ordered read-only boundary list plus name lookup;
  no mutable arrays or compiler dictionaries are exposed.
- Compile settings gain cumulative `MaxGeneratedVertices` and
  `MaxGeneratedTriangles` limits with conservative defaults. Invalid or
  exceeded limits return diagnostics rather than clamping or allocating.
- Disk remains the serialized shape name; unequal physical X/Y dimensions are
  the M01 ellipse representation.

# Final verification

- `python3 Scripts/validate_repository.py`: passed.
- All changed JSON files parsed successfully with `python3 -m json.tool`.
- `git diff --check`: passed.
- Unity `6000.3.20f1` imported and compiled the package without C# errors.
- Unity Edit Mode Test Runner: 27 passed, 0 failed, 0 skipped, in 0.097 seconds
  on the final M01 code.
- The real editor upgraded the source sample, baked
  `M01PlanarProof_Generated`, and displayed 192 vertices / 320 triangles with
  the blue decorated rectangle and orange decorated ellipse visibly flat and
  correctly mapped.
- The proof scene was saved locally as
  `Assets/FoldCanvasGenerated/M01PlanarPreview.unity`; generated source, mesh,
  material, and scene outputs remain ignored derived artifacts.
- No Fold, Roll, Stitch, Solidify, seam welding, thickness, or later-milestone
  behavior was implemented.
