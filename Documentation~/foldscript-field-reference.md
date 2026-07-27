# FoldScript JSON 字段参考 / Field reference

本页是资产配置 JSON 的语义合同，不只说明字段类型，也说明坐标、单位、顺序、
公式和失败方式。最容易混淆的 `roll` 在这里不是欧拉角里的“横滚旋转”，而是
把一个面板的 U 或 V 方向**连续卷到圆弧上**；例如把带图案的矩形侧壁卷成
圆柱杯壁。它不会自动把空间重合的两条边焊接起来。完整定义见
[`roll` 一节](#63-roll--planned-for-m03)。

This document defines the intended meaning of every FoldScript `0.1` field so a
human, procedural tool, or AI system can author the same asset consistently.
The machine-readable constraints live in
[`../Schema/foldcanvas.schema.json`](../Schema/foldcanvas.schema.json).

> **Implementation status:** FoldScript `0.1` is a versioned draft contract.
> M01 implements planar `rectangle` and `disk`/ellipse compilation through the
> Unity `FoldCanvasAsset` representation. `rigidTransform` is implemented.
> `fold`, `roll`, `stitch`, `solidify`, seam compilation, and JSON import/export
> are specified for later milestones and currently return diagnostics or are
> unavailable. A schema-valid JSON file is not a claim that every requested
> operation is implemented.

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
| `seams[].mode` | enum | yes | `"weld"`, `"hinge"`, or `"keepOpen"`. |
| `seams[].reverseB` | boolean | yes | Reverse B's sample order before matching it to A. |
| `seams[].sampleCount` | integer ≥ 0 | yes | `0` requests deterministic automatic resampling; a positive value requests that common sample count. |

Seam modes:

- `weld`: resample as needed and create one topological connection.
- `hinge`: retain distinct boundary vertices while recording a shared fold
  relationship.
- `keepOpen`: retain an explicit relationship without closing the boundary.

Declaring a seam does not execute it. A later `stitch` operation selects which
seams to resolve and when.

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

### 6.2 `fold` — planned for M02

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

### 6.3 `roll` — planned for M03

`roll` continuously maps one panel dimension onto a circular arc. It is not a
rigid rotation of the whole panel.

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `panel` | ID | yes | Target panel. |
| `direction` | enum | yes | `"u"` wraps panel X around an axis parallel to +Y; `"v"` wraps panel Y around an axis parallel to +X. |
| `angleDegrees` | number | yes | Signed total angular sweep from the minimum edge to the maximum edge. `360` makes the two edge positions coincide but does not weld them. |
| `radiusMode` | enum | yes | How the roll radius is selected. |
| `radius` | positive number | conditional | Required when `radiusMode` is `"explicit"`. In document units. |
| `targetSeam` | ID | conditional | Required when `radiusMode` is `"fitTargetBoundary"`; identifies the seam constraint the future solver must fit. |
| `startAngleDegrees` | number | yes | Angular placement of the minimum edge before applying the signed sweep. |

Let `t` be the normalized coordinate along the chosen roll direction and
`theta = radians(startAngleDegrees + t * angleDegrees)`.

For `direction: "u"`:

```text
currentPosition = [R * cos(theta), sourceY, R * sin(theta)]
```

For `direction: "v"`:

```text
currentPosition = [sourceX, R * cos(theta), R * sin(theta)]
```

Radius modes:

- `preserveArcLength`: `R = sourceSpan / abs(radians(angleDegrees))`. This
  preserves physical length along the rolled direction.
- `explicit`: use `radius`; the requested angle is authoritative, so the arc
  may stretch or compress relative to source length.
- `fitTargetBoundary`: reserved for deriving a radius from `targetSeam` so the
  rolled boundary can match its target. M03 deliberately reports this mode as
  unsupported until seam solving exists; it must never silently fall back to a
  different radius mode.

A zero angular sweep is invalid for `preserveArcLength`. Roll preserves UV and
does not merge coincident minimum/maximum edges; seam resolution belongs to
`stitch`.

### 6.4 `stitch` — planned for M04

| Field | Type | Meaning |
|---|---:|---|
| `seams` | ID array | One or more seam IDs resolved in listed order. |

The seam's mode, orientation, and sample count control the topology operation.
Boundary-count equality must not be assumed; deterministic resampling is part
of stitch processing.

### 6.5 `solidify` — planned for M04

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

## 7. Compile settings

| Field | Type | Required | Meaning |
|---|---:|:---:|---|
| `weldEpsilon` | positive number | yes | Physical distance tolerance, after unit conversion, for later welding and coincidence checks. It does not implicitly weld geometry. |
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
