# FoldScript JSON 字段参考 / Field reference

本页是资产配置 JSON 的语义合同，不只说明字段类型，也说明坐标、单位、顺序、
公式和失败方式。最容易混淆的 `roll` 在这里不是欧拉角里的“横滚旋转”，而是
把一个面板的 U 或 V 方向**连续卷到圆弧上**；例如把带图案的矩形侧壁卷成
圆柱杯壁。它不会自动把空间重合的两条边焊接起来。完整定义见
[`roll` 一节](#63-roll--implemented-in-m03)。

This document defines the intended meaning of every FoldScript `0.1` field so a
human, procedural tool, or AI system can author the same asset consistently.
The machine-readable constraints live in
[`../Schema/foldcanvas.schema.json`](../Schema/foldcanvas.schema.json).

> **Implementation status:** FoldScript `0.1` is a versioned draft contract.
> M05 implements planar `rectangle` and `disk`/ellipse compilation,
> `rigidTransform`, edge-aligned rigid-crease `fold`, rectangle `roll`,
> explicit rectangle `sphericalWrap`, deterministic `weld`/`bridge` Stitch,
> and `solidify` through the Unity
> `FoldCanvasAsset` representation. Seam declarations remain inert until
> selected by Stitch. JSON import/export, `hinge`, `keepOpen`, and later
> operations remain unavailable. A schema-valid JSON file is not a claim that
> every future operation is implemented.

## 1. Global conventions

| Convention | Definition |
|---|---|
| Document order | `panels`, `seams`, and `operations` are processed in array order. Object-property order has no geometric meaning. |
| IDs | IDs are case-sensitive stable identifiers matching `^[A-Za-z][A-Za-z0-9_.-]{0,63}$`. They are references, not display labels. |
| Physical units | `meter`, `centimeter`, or `millimeter`. Importers normalize physical values to meters before compilation. |
| Canvas coordinates | Normalized `[0,1] × [0,1]`, origin at bottom-left. |
| Panel-local coordinates | A panel begins in local XY. `+X` is increasing U, `+Y` is increasing V, and the front normal is `+Z`. |
| Angles | Degrees. Signed values preserve direction; they are not silently clamped. |
| Transform order | Operations execute exactly in JSON array order. |
| UV rule | Geometry operations change current 3D positions but preserve source canvas UV unless that operation explicitly defines a new UV policy. |
| Topology rule | Spatial coincidence alone never welds geometry. Only an explicit seam/stitch operation may change topology. |

Array vector ordering is always explicit:

```text
vec2 = [x, y]
vec3 = [x, y, z]
rect = [x, y, width, height]
```

## 2. Top-level document

| Field | Type | Required | Meaning and constraints |
|---|---:|:---:|---|
| `schemaVersion` | string | yes | Exact source contract version. Version `0.1` accepts only `"0.1"`. |
| `assetId` | ID | yes | Stable machine identifier used by tools, caches, and diagnostics. |
| `displayName` | string | yes | Human-readable name, 1–128 characters. It has no geometric effect. |
| `units` | enum | yes | `"meter"`, `"centimeter"`, or `"millimeter"` for all physical values. |
| `canvas` | object | yes | Appearance image metadata described below. |
| `panels` | array | yes | Ordered 2D source domains. At least one panel. |
| `seams` | array | yes | Ordered named relationships between panel boundaries. May be empty. |
| `operations` | array | yes | Ordered geometry program. May be empty for flat panels. |
| `compile` | object | yes | Validation, normal, tolerance, and safety settings. |
| `extensions` | object | no | Namespaced forward-compatible metadata. Core geometry must not depend on unknown extension fields. |

## 3. Appearance canvas

| Field | Type | Required | Meaning and constraints |
|---|---:|:---:|---|
| `canvas.appearance` | string | yes | Relative asset path or import-resolvable image reference. It is not an arbitrary network request. |
| `canvas.width` | integer | yes | Source image width in pixels, 1–32768. Used for validation and authoring tools, not physical scale. |
| `canvas.height` | integer | yes | Source image height in pixels, 1–32768. |

`canvasRect` values on panels select regions of this image. Pixels remain
attached because every compiled vertex stores its source-canvas UV.

## 4. Panels

### 4.1 Common panel fields

| Field | Type | Required | Meaning and constraints |
|---|---:|:---:|---|
| `panels[].id` | ID | yes | Unique panel identifier referenced by operations and seams. |
| `panels[].shape` | enum | yes | `"rectangle"` or `"disk"` in version `0.1`. |
| `panels[].canvasRect` | rect | yes | `[x,y,width,height]` in normalized canvas space. Width and height must be positive, and the complete rectangle must remain inside `[0,1]²`. |
| `panels[].physicalSize` | positive vec2 | yes | Physical `[width,height]` in document units. For a disk, unequal values define an ellipse. |
| `panels[].tessellation` | object | yes | Shape-specific deterministic sampling counts. |

### 4.2 Rectangle

`shape: "rectangle"` emits a row-major grid from bottom-left to top-right.

| Field | Type | Meaning |
|---|---:|---|
| `uSegments` | integer ≥ 1 | Number of horizontal cells. Emits `uSegments + 1` samples per row. |
| `vSegments` | integer ≥ 1 | Number of vertical cells. Emits `vSegments + 1` rows. |

For normalized grid coordinates `fu = u/uSegments` and
`fv = v/vSegments`:

```text
sourceX = (fu - 0.5) * physicalWidth
sourceY = (fv - 0.5) * physicalHeight
uvX = canvasX + fu * canvasWidth
uvY = canvasY + fv * canvasHeight
```

Generated named boundaries:

| Boundary | Ordered samples |
|---|---|
| `uMin` | bottom to top on the minimum-U edge |
| `uMax` | bottom to top on the maximum-U edge |
| `vMin` | left to right on the minimum-V edge |
| `vMax` | left to right on the maximum-V edge |

### 4.3 Disk or ellipse

`shape: "disk"` emits one center vertex followed by concentric rings. A
non-square `physicalSize` produces an ellipse.

| Field | Type | Meaning |
|---|---:|---|
| `radialSegments` | integer ≥ 3 | Samples around every ring. Angle starts at `+X` and advances counter-clockwise when viewed from `+Z`. |
| `radialRings` | integer ≥ 1 | Number of concentric rings outside the center. |

For ring fraction `r`, angle `theta`, and physical radii `rx`, `ry`:

```text
sourceX = cos(theta) * r * rx
sourceY = sin(theta) * r * ry
uvX = canvasCenterX + cos(theta) * r * canvasWidth / 2
uvY = canvasCenterY + sin(theta) * r * canvasHeight / 2
```

The center maps exactly to the center of `canvasRect`. The generated
`perimeter` boundary is the outer ring, ordered counter-clockwise, without
duplicating its first index at the end.

## 5. Boundary references and seams

A boundary reference is:

```json
{ "panel": "wall", "boundary": "uMin" }
```

| Field | Type | Meaning |
|---|---:|---|
| `panel` | ID | Existing panel ID. |
| `boundary` | ID | Named boundary generated by that panel shape. |

A seam declares topology intent between two ordered boundaries:

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `seams[].id` | ID | yes | Unique seam identifier. |
| `seams[].a` | boundary ref | yes | First ordered boundary. |
| `seams[].b` | boundary ref | yes | Second ordered boundary. |
| `seams[].mode` | enum | yes | `"weld"`, `"bridge"`, `"hinge"`, or `"keepOpen"`. |
| `seams[].reverseB` | boolean | yes | Reverse B's sample order before matching it to A. |
| `seams[].sampleCount` | integer ≥ 0 | yes | Minimum correspondence density. `0` uses the union of existing normalized breakpoints; a positive value also adds a uniform parameter grid. Existing samples are never discarded, so the final count may be larger. |

Seam modes:

- `weld`: resample both source surfaces to one deterministic correspondence,
  require paired positions within `weldEpsilon`, and create one logical
  topological connection.
- `bridge`: use the same correspondence to create a deterministic connecting
  strip without unioning the two boundary topology sets.
- `hinge`: retain distinct boundary vertices while recording a shared fold
  relationship.
- `keepOpen`: retain an explicit relationship without closing the boundary.

Declaring a seam does not execute it. A later `stitch` operation selects which
seams to resolve and when. M04 executes `weld` and `bridge`; selecting `hinge`
or `keepOpen` remains unsupported, while an unselected declaration is valid
and inert.

## 6. Operations

All operations share:

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `id` | ID | yes | Unique operation identifier used by diagnostics. |
| `type` | enum | yes | Selects the operation schema. |
| `enabled` | boolean | no | Defaults to `true`. A disabled operation remains serialized but has no effect. |

### 6.1 `rigidTransform`

Moves one panel without changing its topology, UV, source position, ownership,
or provenance.

| Field | Type | Meaning |
|---|---:|---|
| `panel` | ID | Target panel. |
| `translation` | vec3 | Translation in document units, converted to meters. |
| `rotationEuler` | vec3 | Unity-compatible Euler angles in degrees. |
| `scale` | vec3 | Component-wise local scale applied before rotation and translation. |

Mapping:

```text
nextPosition = Euler(rotationEuler) * (currentPosition ⊙ scale)
             + translation
```

Because operations run in array order, repeated rigid transforms compose. The
stored `sourcePosition` and `sourceUv` remain unchanged.

### 6.2 `fold` — implemented in M02

Rotates samples on one selected side of a line embedded in the target panel.
The line itself is the hinge axis.

| Field | Type | Meaning |
|---|---:|---|
| `panel` | ID | Target panel. |
| `line` | `[vec2,vec2]` | Start and end in panel-normalized coordinates `[0,1]²`. The two points must differ. |
| `side` | enum | `"positive"` selects points left of directed line A→B; `"negative"` selects points right of it. |
| `angleDegrees` | number | Signed rotation around the embedded A→B hinge axis. |
| `falloff` | number 0–1 | `0` is a rigid crease. Values above zero reserve a normalized influence width for a smooth bend. |

Side classification for point `P`:

```text
sideValue = cross2(B - A, P - A)
positive: sideValue > 0
negative: sideValue < 0
```

Positive angle semantics must match Unity
`Quaternion.AngleAxis(angleDegrees, axisFromAtoB)`. Samples on the hinge remain
stationary. Degenerate lines or ambiguous samples produce diagnostics.

M02 executes a rigid crease as follows:

1. Validate the target, finite line, normalized range, nonzero line length,
   side, finite angle, and `falloff`.
2. Verify both line endpoints are existing source vertices and the complete
   line is covered without gaps by a continuous chain of existing triangle
   edges.
3. Map the complete source line through the target panel's deterministic source
   triangles into the panel's **current** 3D embedding.
4. Check every line/triangle-edge crossing and each interval midpoint. The
   mapped samples must form one non-collapsed, order-preserving straight axis.
5. Classify every vertex using its immutable normalized source position.
6. Rotate only the selected side's current position about the directed current
   axis. Source position, UV, ownership, provenance, indices, and boundaries
   remain unchanged.

The first source-order triangle is used when a mapped point lies on a shared
triangle edge. This is deterministic and produces the same position because
the edge vertices are shared. A line that crosses an earlier crease and is no
longer straight in current 3D returns `FC3007 AmbiguousFoldHinge`; the compiler
does not choose one segment or flatten the hinge.

M02 requires `falloff` to be exactly `0`. Values above zero are reserved for a
future smooth-bend operation and return `FC3009 UnsupportedFoldFalloff`.
Line endpoints must be inside `[0,1]²` and the complete line must lie inside the
panel's source triangulation. This latter condition matters for non-rectangular
domains such as disks.

M03 adds a safety contract without adding topology: if the authored crease is
not already an edge chain, compilation returns
`FC3011 FoldCreaseRequiresTopologySplit`, produces no Mesh, and does not rotate
an approximate set of existing vertices. Deterministic crease splitting is a
separate future roadmap task.

### 6.3 `roll` — implemented in M03

`roll` continuously maps one panel dimension onto a circular arc. It is not a
rigid rotation of the whole panel.

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `panel` | ID | yes | Target panel. |
| `direction` | enum | yes | `"u"` wraps panel X around an axis parallel to +Y; `"v"` wraps panel Y around an axis parallel to +X. |
| `angleDegrees` | number | yes | Signed Circular Roll sweep from the minimum edge to the maximum edge, in `[-360, 360]`. `+/-360` makes the two edge positions coincide; Roll itself does not weld them, but a later explicit Stitch may. Larger multi-turn sweeps are unsupported in M03. |
| `radiusMode` | enum | yes | How the roll radius is selected. |
| `radius` | positive number | conditional | Required when `radiusMode` is `"explicit"`. In document units. |
| `targetSeam` | ID | conditional | Required when `radiusMode` is `"fitTargetBoundary"`; identifies the seam constraint the future solver must fit. |
| `startAngleDegrees` | number | yes | Angular placement of the minimum edge before applying the signed sweep. |

Let `t` be the normalized coordinate along the chosen roll direction and
`theta = radians(startAngleDegrees - t * angleDegrees)`.

Before mapping, the compiler resolves the target's current frame:

- `CurrentOrigin`: current position of the source rectangle center;
- `CurrentU`: unit current direction of increasing source U;
- `CurrentV`: unit current direction of increasing source V;
- `CurrentNormal = normalize(cross(CurrentU, CurrentV))`.

Every target vertex must still equal
`CurrentOrigin + sourceX*CurrentU + sourceY*CurrentV` within centralized
tolerances. M03 accepts any congruent planar embedding, including translation,
rotation, and an orientation-reversing planar isometry. In-plane
metric-changing scale, shear, a collapsed axis, non-planarity, and a prior
non-planar Fold return `FC3021 UnsupportedRollEmbedding`; compatibility is
determined from the final geometry rather than remembered operation history,
and the compiler does not reconstruct a frame from one convenient triangle.

For `direction: "u"`:

```text
currentPosition =
    CurrentOrigin
    + sourceY * CurrentV
    - R * cos(theta) * CurrentU
    + R * sin(theta) * CurrentNormal
```

For `direction: "v"`:

```text
currentPosition =
    CurrentOrigin
    + sourceX * CurrentU
    - R * cos(theta) * CurrentV
    + R * sin(theta) * CurrentNormal
```

At `startAngleDegrees = 0`, the minimum boundary begins on the negative
selected-axis radial direction. Positive angles advance toward
`-CurrentNormal`. Roll reverses each target triangle's winding without changing
connectivity. A positive sweep therefore produces radially outward normals;
a negative sweep uses the opposite circulation and produces the documented
radially inward orientation. UV and boundary order remain source-authored in
both cases.

Radius modes:

- `preserveArcLength`: `R = sourceSpan / abs(radians(angleDegrees))`. This
  preserves physical length along the rolled direction.
- `explicit`: use `radius`; the requested angle is authoritative, so the arc
  may stretch or compress relative to source length. A successful compile
  emits `FC3018 RollStretchReport` with ordered structured values
  `sourceSpan`, `arcLength`, and `stretchRatio`, where
  `arcLength = radius * abs(radians(angleDegrees))` and
  `stretchRatio = arcLength / sourceSpan`. The report is `Info` inside
  `[0.5,2]` and `Warning` outside it; the geometry is never silently clamped.
- `fitTargetBoundary`: reserved for deriving a radius from `targetSeam` so the
  rolled boundary can match its target. M03 deliberately reports this mode as
  unsupported until seam solving exists; it must never silently fall back to a
  different radius mode.

A zero angular sweep is invalid for `preserveArcLength`. Roll preserves UV and
does not merge coincident minimum/maximum edges; seam resolution belongs to
`stitch`. A closed full-turn Roll requires at least three source segments in
the selected direction; otherwise it returns
`FC3022 InsufficientRollTessellation`. Two segments sample only 0, 180, and 360
degrees, so their two nonzero-area panels overlap in one plane. Coincident
full-turn endpoints retain different render vertex indices until explicitly
stitched.

The selected angular coordinate is
`theta = startAngleDegrees - t * angleDegrees`. Roll reverses triangle winding
without changing connectivity, so positive full turns keep radial outward
front faces while increasing source U reads left-to-right at the canonical
exterior view.

M03 Circular Roll accepts at most one signed turn. A magnitude above 360
degrees returns `FC3023 UnsupportedMultiTurnRoll` instead of producing
overlapping cylindrical layers. Future `SpiralRoll` or `LayeredRoll`
operations require separate pitch, layer-spacing, thickness, and collision
contracts.

中文摘要：M03 的圆形 `roll` 只接受 `-360` 到 `+360` 度；完整一圈至少需要
三个源分段，因为两个分段只会采样 0/180/360 度并生成两张重叠平面。Roll
读取执行前的最终几何，只接受与源矩形全等的平面嵌入；平移、旋转和单位镜像
可以通过，改变平面内度量的缩放、剪切、轴塌缩和非平面结果会返回稳定诊断。

### 6.4 `sphericalWrap` — implemented in M05

`sphericalWrap` maps one explicit rectangular 2D parameter panel onto a
spherical patch. It is a deformation rule; it does not create a sphere,
invent seam topology, or call a Unity primitive.

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `panel` | ID | yes | Rectangle parameter panel to map. |
| `radius` | positive number | yes | Sphere radius in document units, measured from the resolved `CurrentOrigin`. |
| `latitudeRange` | `[number,number]` | yes | Signed latitude endpoints in degrees. Each endpoint is inside `[-90,90]`; the span must be nonzero. |
| `longitudeRange` | `[number,number]` | yes | Signed longitude endpoints in degrees. Each endpoint is inside `[-360,360]`, the span must be nonzero, and its magnitude may not exceed 360 degrees. |
| `wrapDirection` | enum | yes | `"longitudeAlongU"` maps source U to longitude and V to latitude; `"longitudeAlongV"` swaps those parameter roles. |
| `poleMode` | enum | yes | `"merge"` emits one render pole per panel fan. `"keepFan"` retains one render pole copy per adjacent longitude cell while assigning all copies one logical topology identity. |
| `subdivisionMode` | enum | yes | M05 accepts only `"panelGrid"` and uses the panel's authored segment counts. |

Before mapping, the complete current panel must still be a congruent,
non-degenerate planar embedding. The compiler resolves:

```text
CurrentOrigin = current position corresponding to source (0,0)
CurrentU      = unit current direction of increasing source X/U
CurrentV      = unit current direction of increasing source Y/V
CurrentNormal = normalize(cross(CurrentU, CurrentV))
```

Translation, rotation, and a unit reflection are preserved. In-plane
metric-changing scale, shear, collapse, or a prior non-planar deformation
returns `FC6010 UnsupportedSphericalEmbedding`.

Let `longitudeT` and `latitudeT` be selected by `wrapDirection` and linearly
interpolate the authored angular ranges. With radians `lambda` and `phi`:

```text
currentPosition =
    CurrentOrigin
    + radius * cos(phi) * cos(lambda) * CurrentU
    + radius * sin(phi)               * CurrentV
    + radius * cos(phi) * sin(lambda) * CurrentNormal
```

The compiler chooses triangle index orientation from the actual mapped frame
and then verifies every triangle faces radially outward. It does not rely on a
two-sided material.

An exact `-90` or `+90` endpoint changes tessellation before deformation.
`merge` uses one source/UV sample at the midpoint of that panel's pole edge.
`keepFan` retains a render sample per adjacent longitude cell so UV/provenance
splits survive, but unions those copies to one `TopologyVertexId`. A panel that
spans both poles needs at least two authored latitude segments so at least one
non-pole row exists. Pole topology is never repaired by collapsing a generated
mesh afterward.

Neighboring gores remain separate until an explicit `stitch` selects their
side seams. If unequal correspondence inserts a new point, the source
coordinate and UV are interpolated, but current 3D position is recomputed
through this spherical formula. It therefore remains on `radius` rather than a
straight chord. A complete stitched zero-thickness sphere must pass the M05
sphere report: one component, no open/non-manifold/orientation-conflict or
isolated topology, Euler characteristic 2, one north pole, one south pole,
outward winding, consistent frame, and bounded radius error.

中文摘要：`sphericalWrap` 只把明确声明的矩形二维球瓣映射到当前局部球面，
不会凭空生成球体。二维源坐标和 UV 保留；极点在离散阶段明确处理；球瓣之间
只有经过 Seam Graph 与 `stitch` 才会焊接。新增接缝采样点重新执行球面公式，
不会落在球内弦线上。

### 6.5 `stitch`

| Field | Type | Meaning |
|---|---:|---|
| `seams` | ID array | One or more seam IDs resolved in listed order. |

The seam's mode, orientation, and sample count control the topology operation.
Seam declarations remain inert until selected here.

M04 parameterizes both ordered boundaries by normalized current-space arc
length. It retains the union of authored breakpoints and, when
`sampleCount > 0`, adds a uniform minimum-density grid. Missing samples are
inserted by subdividing the corresponding boundary edge and adjacent source
triangle; current position, source position, UV0, panel ownership, and
provenance are interpolated. No free-floating sample is permitted.

Weld requires paired positions within `compile.weldEpsilon` and assigns one
deterministic `TopologyVertexId`. Separate render vertices remain legal when
source UVs, provenance, or hard normals differ; this is an attribute seam, not
an open topological edge. Bridge emits a strip from the same paired samples
without unioning them. `hinge` and `keepOpen` execution remain unsupported.

Until topology-group deformation propagation exists, a later
`rigidTransform`, `fold`, `roll`, or `sphericalWrap` cannot target any panel
selected by an earlier Stitch. It returns `FC2010` and no Mesh. Solidify is
allowed after Stitch because it consumes complete logical topology groups.

### 6.6 `solidify` — implemented in M04

Creates thickness from one or more zero-thickness panels after requested seam
operations.

| Field | Type | Meaning |
|---|---:|---|
| `targets` | ID array | Panel IDs to thicken. |
| `thickness` | positive number | Total shell thickness in document units. |
| `direction` | enum | `"inward"`, `"outward"`, or `"centered"`. |

Direction semantics:

- `inward`: keep the current surface as the outer surface and offset the new
  shell along negative generated normals;
- `outward`: keep the current surface as the inner surface and offset along
  positive generated normals;
- `centered`: offset by half the thickness in both normal directions.

Open boundaries receive rim/side-wall geometry. Welded internal seams must not
receive duplicate walls.

The selected panels must include every render copy in each selected welded
topology group; M04 never tears a partial stitched component or silently
expands the target. Outer winding follows the source surface, inner triangles
reverse it, and all render copies sharing one topology ID receive the same
offset-plane solution. A hard wall-to-bottom corner therefore has one shared
inner miter rather than two independently offset surfaces.

After Stitch, logical edge incidence classifies rims: incidence one receives
one side-wall strip, incidence two is already internal, and incidence above two
fails. The cup top receives a rim; its wall closure and welded bottom loop do
not receive hidden internal walls.

## 7. Compile settings

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `weldEpsilon` | positive number | yes | Physical distance tolerance, after unit conversion, for explicit Weld and coincidence checks. It does not implicitly weld geometry. |
| `recalculateNormals` | boolean | yes | Derive Unity mesh normals after geometry compilation. |
| `validationLevel` | enum | yes | `"basic"`, `"standard"`, or `"strict"`; higher levels add more expensive validation when implemented. |
| `maxGeneratedVertices` | integer ≥ 1 | no | Cumulative pre-allocation safety limit. C# default: `1,000,000`. |
| `maxGeneratedTriangles` | integer ≥ 1 | no | Cumulative pre-allocation safety limit. C# default: `2,000,000`. |

M01 rejects an unsafe tessellation request before allocating partial geometry.
Limits are errors, not automatic simplification targets.

## 8. Determinism and failure behavior

For identical source, settings, and compiler version, tools must preserve:

- panel, vertex, triangle, boundary, operation, and diagnostic ordering;
- current positions, source-local positions, UV0, ownership, and provenance;
- explicit IDs and operation list order.

Importers and compilers must not silently clamp, reorder, infer a seam from
proximity, drop an unsupported operation, or substitute an unrelated generated
mesh. See [Diagnostics and validation](diagnostics.md).
