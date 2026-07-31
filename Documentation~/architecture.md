# Architecture

## 1. North-star model

FoldCanvas separates **source representation** from **renderable artifact**.

```text
FoldCanvas source
├── Appearance canvas
├── Panel domains
├── Named boundaries
├── Seam graph
├── Ordered fold/deformation program
├── Thickness/layer data
└── Compile settings
        ↓ deterministic compiler
Derived Unity artifacts
├── Mesh
├── Material bindings
├── Collider suggestions
├── LOD candidates
├── Validation report
└── Prefab
```

A generated mesh is never the canonical editable source. Any workflow that requires manually fixing the generated mesh without updating the source is a compiler or representation failure to investigate.

## 2. Assembly boundaries

### `FoldCanvas.Runtime`

Contains only Unity-runtime-safe code:

- source data definitions
- geometry buffers
- deterministic compiler stages
- diagnostics
- low-level validation

It must not reference `UnityEditor` or a render pipeline.

### `FoldCanvas.Editor`

Contains:

- asset creation
- editor windows
- canvas authoring tools
- bake and save workflows
- preview object management
- import/export adapters

### `FoldCanvas.Tests.Editor`

Contains deterministic unit and integration tests. Tests should primarily assert topology, coordinates, UVs, boundaries, and diagnostics rather than screenshots.

## 3. Source layers

### Appearance canvas

One or more 2D images containing visible artwork. MVP uses one RGBA texture. The canvas coordinate system is normalized UV space `[0,1] × [0,1]` with origin at bottom-left after Unity texture import conventions are accounted for.

### Panel

A panel is a bounded 2D domain that becomes a surface patch. It includes:

- stable string identifier
- source-canvas region
- physical dimensions in meters
- tessellation policy
- named boundaries
- optional local fields in later versions

MVP shapes:

- rectangle
- disk/ellipse

Later shapes:

- polygon with holes
- spline-bounded patch
- image-mask patch
- multi-chart surface atlas

### Boundary

A boundary is an ordered polyline over a panel. Order is part of the contract. A boundary is parameterized by normalized arc length `t ∈ [0,1]`.

Standard rectangle boundaries:

```text
uMin: bottom-to-top
uMax: bottom-to-top
vMin: left-to-right
vMax: left-to-right
```

Standard disk boundary:

```text
perimeter: counter-clockwise when viewed from the panel front
```

### Seam

A seam links two named boundaries. It describes topology, not merely spatial proximity.

MVP seam modes:

- `Weld`: boundaries become one manifold edge
- `Bridge`: boundaries remain distinct and receive a deterministic connecting
  strip
- `Hinge`: boundaries remain distinct but share a fold relationship
- `KeepOpen`: relationship is documented without closure

### Operation

Operations transform a panel or a selected region. They are executed in stable list order for MVP. A later DAG representation may be added only through an ADR.

MVP operation family:

- rigid transform
- fold around a 2D line
- roll a panel along U or V
- spherical-wrap an explicit parameter panel
- stitch seam
- solidify/thicken

### Compile settings

Include tolerances, normal mode, output naming, validation level, and cumulative
generated-vertex/triangle safety limits. Compiler behavior must not depend on
editor selection, locale, frame time, random state, or object discovery.
One compile-scoped `GeometryBudget` covers panel tessellation and every later
Stitch or Solidify addition. Geometry-producing operations reserve before
mutation and use rollback transactions; the build buffer remains the hard
enforcement boundary.

## 4. Coordinate conventions

- Unity world coordinates are left-handed as used by Unity transforms.
- Source panel coordinates are 2D local coordinates in meters.
- Bootstrap panels start in the local XY plane with front normal `+Z`.
- UV origin is the lower-left corner of the source canvas region.
- Triangle winding is chosen so bootstrap panel fronts face `+Z`.
- M04 thickness solves incident oriented face-offset planes per logical
  topology vertex, preserving one shared position across UV/provenance render
  splits and hard welded corners.
- M04.1 freezes paired outer/inner hard-corner segments and a closed-volume
  report over logical topology, connected components, edge incidence, winding,
  and oriented volume.
- M05 `SphericalWrap` accepts only a finite, non-degenerate, congruent planar
  embedding. It resolves `CurrentOrigin`, unit `CurrentU`, unit `CurrentV`, and
  `CurrentNormal = normalize(cross(CurrentU, CurrentV))` from the complete
  current panel, preserving prior translation, rotation, or unit reflection.
- With longitude `lambda`, latitude `phi`, and radius `r`, M05 evaluates
  `CurrentOrigin + r*cos(phi)*cos(lambda)*CurrentU +
  r*sin(phi)*CurrentV + r*cos(phi)*sin(lambda)*CurrentNormal`. Direction
  selects whether source U or V supplies longitude; it does not replace the
  current frame with world axes.
- Spherical triangles are wound so their geometric normal has a positive dot
  product with the radial direction. Exact pole rows are emitted as explicit
  fan topology before deformation, never collapsed by cleanup afterward.
- Near-pole canonicalization must satisfy both angular tolerance and
  `radius * angularDeviationRadians <= weldEpsilon`; scale cannot turn a
  spatially large ring into one logical pole.

The M04.1 corner/volume records and M05 spherical-surface/sphere-report records
are read-only derived metadata. Editor wireframe, section, seam, pole, stretch,
and radius-error Meshes may visualize them, but they never become source
geometry or feed back into compilation.

M05 derives spherical components only from enabled SphericalWrap panels joined
by Stitch-selected seams whose two endpoints are spherical. Component
formation and component modification are separate: after components are
formed, any Stitch-selected seam with either endpoint in a component touches
that component. The compiler freezes its report after the last touching Stitch
and before a Solidify that touches that component. Solidify may build a later
shell but cannot replace this zero-thickness proof. The report proves the
documented topology, radius, frame, and winding invariants; it does not run a
global triangle-triangle self-intersection test.

Every operation document must state how it maps source coordinates to 3D and how it preserves boundary ordering.

## 5. Determinism contract

For identical:

- FoldCanvas source data
- appearance dimensions
- compile settings
- compiler version

FoldCanvas must emit identical:

- vertex count and order
- triangle count and index order
- UV values
- submesh layout
- diagnostic codes and ordering

Floating-point values are expected to be numerically stable within documented tolerance. Any future parallelism must preserve output ordering.

## 6. AI boundary

The geometry core has no model-provider dependency. AI adapters may:

- create artwork
- segment panels
- propose boundaries
- generate FoldScript
- revise source after compiler diagnostics

They may not silently replace a failed compile with an unrelated generated mesh.

## 7. Render boundary

MVP output only guarantees:

- geometry
- UV0
- derived normals
- one appearance texture binding

Unlit rendering is the reference proof. PBR maps, tangent generation, material classification, and specialized shaders are optional derived stages.

## 8. Extension points

Long-term extension points include:

- panel tessellators
- operations
- seam solvers
- validators
- field channels
- exporters
- AI provider adapters

An extension must consume and produce explicit geometry data. Hidden scene state is forbidden.
