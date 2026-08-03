# FoldScript 0.1 specification

FoldScript is the portable, human-readable interchange format for FoldCanvas source documents. Unity `ScriptableObject` assets are an editor representation; FoldScript is the long-term serialized contract.

Schema file: [`../Schema/foldcanvas.schema.json`](../Schema/foldcanvas.schema.json)

Field-by-field semantics, units, formulas, defaults, and implementation status:
[`foldscript-field-reference.md`](foldscript-field-reference.md).

Project motivation and the production problem this format addresses:
[`project-background.md`](project-background.md).

**Implementation status:** M09 implements this `0.1` document as executable
Runtime import/export over boundary spans and toroidal mapping as well as the
earlier operation family. The bounded reader accepts only the fields and
operation types documented here, canonical output is deterministic, and
corrected AI output must re-enter the same importer and compiler. See
[FoldScript 0.1 runtime and Editor workflow](foldscript-runtime.md).

## 1. Goals

- readable enough for humans to review
- constrained enough for AI systems to generate reliably
- versioned and deterministic
- independent of Unity scene state
- explicit about units, topology, and operation order
- able to round-trip without losing identifiers

## 2. Document skeleton

```json
{
  "schemaVersion": "0.1",
  "assetId": "gpt-cup",
  "displayName": "GPT Cup",
  "units": "meter",
  "canvas": {
    "appearance": "gpt-cup.png",
    "width": 2048,
    "height": 2048
  },
  "panels": [],
  "seams": [],
  "operations": [],
  "compile": {}
}
```

## 3. Panel example

```json
{
  "id": "wall",
  "shape": "rectangle",
  "canvasRect": [0.05, 0.15, 0.90, 0.45],
  "physicalSize": [0.31415927, 0.12],
  "tessellation": {
    "uSegments": 64,
    "vSegments": 12
  }
}
```

`canvasRect` is `[x, y, width, height]` in normalized canvas coordinates.

Disk example:

```json
{
  "id": "bottom",
  "shape": "disk",
  "canvasRect": [0.10, 0.68, 0.22, 0.22],
  "physicalSize": [0.10, 0.10],
  "tessellation": {
    "radialSegments": 64,
    "radialRings": 8
  }
}
```

## 4. Operations

Operations are executed in array order in schema version `0.1`.

### Rigid transform

```json
{
  "id": "place-bottom",
  "type": "rigidTransform",
  "panel": "bottom",
  "translation": [0.0, -0.06, 0.0],
  "rotationEuler": [90.0, 0.0, 0.0],
  "scale": [1.0, 1.0, 1.0]
}
```

### Fold

```json
{
  "id": "raise-side-a",
  "type": "fold",
  "panel": "box-net",
  "line": [[0.25, 0.40], [0.75, 0.40]],
  "side": "positive",
  "angleDegrees": 90.0,
  "falloff": 0.0
}
```

The line is expressed in panel-normalized coordinates. Schema version `0.1`
does not define an alternative `coordinateSpace`. M02 implements this operation
as a rigid crease when `falloff` is exactly zero. Positive angles match
`Quaternion.AngleAxis(angleDegrees, currentAxisFromAtoB)`. If an earlier
operation bends the authored line so it no longer maps to one straight current
3D axis, compilation fails instead of guessing. The complete authored crease
must also be an existing continuous mesh-edge chain; otherwise M03 returns
`FC3011 FoldCreaseRequiresTopologySplit` instead of stretching crossed
triangles.

### Roll

```json
{
  "id": "roll-wall",
  "type": "roll",
  "panel": "wall",
  "direction": "u",
  "angleDegrees": 360.0,
  "radiusMode": "preserveArcLength",
  "startAngleDegrees": 180.0
}
```

`roll` means continuously wrapping one panel dimension onto a circular arc; it
does **not** mean applying one rigid rotation to the panel. The compiler may use
trigonometric evaluation internally, but no user-authored vertices or UV unwrap
are required. Exact direction, radius, angle, and seam semantics are defined in
the [field reference](foldscript-field-reference.md#63-roll--implemented-in-m03).
M03 Circular Roll accepts only `-360 <= angleDegrees <= 360`; larger
multi-turn, spiral, or layered requests require a future operation. A complete
turn also requires at least three source segments in the selected direction.

### Spherical wrap

```json
{
  "id": "wrap-gore-00",
  "type": "sphericalWrap",
  "panel": "gore-00",
  "radius": 0.5,
  "latitudeRange": [-90.0, 90.0],
  "longitudeRange": [-180.0, -135.0],
  "wrapDirection": "longitudeAlongU",
  "poleMode": "merge",
  "subdivisionMode": "panelGrid"
}
```

M05 applies this mapping to an already-authored rectangle panel in its current
congruent planar frame. It does not allocate a hidden sphere template. Multiple
gore panels become one sphere only when their declared longitude boundaries
are selected by a later `stitch` operation. Exact pole rows are constructed as
fan topology before deformation, and any seam samples inserted later are
evaluated from their immutable 2D source coordinates through the same
spherical mapping.

The importable complete example is
[`Samples~/Sphere/sphere-golden.foldcanvas.json`](../Samples~/Sphere/sphere-golden.foldcanvas.json).
Its eight explicit panels cover adjacent 45-degree longitude ranges, share one
radius and current frame, and Weld eight ordered side seams into one validated
closed sphere.

### Toroidal wrap

```json
{
  "id": "wrap-torus",
  "type": "toroidalWrap",
  "panel": "torus-chart",
  "majorRadius": 0.60,
  "minorRadius": 0.18,
  "majorAngleRange": [0.0, 360.0],
  "minorAngleRange": [0.0, 360.0],
  "wrapDirection": "majorAlongU"
}
```

M09 maps the authored rectangle in its current congruent planar frame. It
requires `majorRadius > minorRadius > 0`, at most one signed turn on each axis,
and at least three source segments for each full-turn direction. The operation
only changes positions and outward winding. Closing the major and minor cycles
requires two declared seams selected by a later `stitch`; coincident edge
positions do not weld automatically.

### Solidify

```json
{
  "id": "thicken-cup",
  "type": "solidify",
  "targets": ["wall", "bottom"],
  "thickness": 0.004,
  "direction": "inward"
}
```

## 5. Seam example

```json
{
  "id": "wall-side-seam",
  "a": { "panel": "wall", "boundary": "uMin" },
  "b": { "panel": "wall", "boundary": "uMax" },
  "mode": "weld",
  "reverseB": false,
  "sampleCount": 0
}
```

A zero sample count asks M04 to use the sorted union of both boundaries'
existing normalized arc-length breakpoints. A positive value additionally adds
a uniform minimum-density grid. Missing samples are inserted into the actual
source surfaces; authored breakpoints are never discarded.

M09 may select a normalized non-wrapping sub-chain on either endpoint:

```json
{
  "id": "attach-handle-a",
  "a": { "panel": "handle", "boundary": "vMin" },
  "b": {
    "panel": "wall",
    "boundary": "vMax",
    "span": [0.0, 0.0416666667]
  },
  "mode": "weld",
  "reverseB": true,
  "sampleCount": 0
}
```

The span is measured by normalized current-space arc length in authored
boundary order and must satisfy `0 <= startT < endT <= 1`. It is selected before
`reverseB`; omission preserves the complete-boundary behavior. Off-grid
endpoints split the real boundary-adjacent source triangle transactionally.

Cup-bottom seam:

```json
{
  "id": "wall-bottom-seam",
  "a": { "panel": "wall", "boundary": "vMin" },
  "b": { "panel": "bottom", "boundary": "perimeter" },
  "mode": "weld",
  "reverseB": false,
  "sampleCount": 64
}
```

## 6. Complete cup sketch

This sketch is executable M04 intent: `stitch-all` resolves the wall closure
and bottom attachment, then `solidify` consumes the complete welded component
to produce outer and inner shells plus one top rim. The retained M03 live
sample omits the final operation and remains a zero-thickness presentation
example.

```json
{
  "schemaVersion": "0.1",
  "assetId": "gpt-cup",
  "displayName": "GPT Cup",
  "units": "meter",
  "canvas": {
    "appearance": "gpt-cup.png",
    "width": 2048,
    "height": 2048
  },
  "panels": [
    {
      "id": "wall",
      "shape": "rectangle",
      "canvasRect": [0.06, 0.46, 0.88, 0.44],
      "physicalSize": [0.31415927, 0.12],
      "tessellation": { "uSegments": 64, "vSegments": 12 }
    },
    {
      "id": "bottom",
      "shape": "disk",
      "canvasRect": [0.32, 0.02, 0.36, 0.36],
      "physicalSize": [0.10, 0.10],
      "tessellation": { "radialSegments": 64, "radialRings": 8 }
    }
  ],
  "seams": [
    {
      "id": "close-wall",
      "a": { "panel": "wall", "boundary": "uMin" },
      "b": { "panel": "wall", "boundary": "uMax" },
      "mode": "weld",
      "reverseB": false,
      "sampleCount": 13
    },
    {
      "id": "attach-bottom",
      "a": { "panel": "wall", "boundary": "vMin" },
      "b": { "panel": "bottom", "boundary": "perimeter" },
      "mode": "weld",
      "reverseB": false,
      "sampleCount": 64
    }
  ],
  "operations": [
    {
      "id": "roll-wall",
      "type": "roll",
      "panel": "wall",
      "direction": "u",
      "angleDegrees": 360.0,
      "radiusMode": "preserveArcLength",
      "startAngleDegrees": 180.0
    },
    {
      "id": "place-bottom",
      "type": "rigidTransform",
      "panel": "bottom",
      "translation": [0.0, -0.06, 0.0],
      "rotationEuler": [90.0, 0.0, 0.0],
      "scale": [1.0, 1.0, 1.0]
    },
    {
      "id": "stitch-all",
      "type": "stitch",
      "seams": ["close-wall", "attach-bottom"]
    },
    {
      "id": "solidify",
      "type": "solidify",
      "targets": ["wall", "bottom"],
      "thickness": 0.004,
      "direction": "inward"
    }
  ],
  "compile": {
    "weldEpsilon": 0.00001,
    "recalculateNormals": true,
    "validationLevel": "strict",
    "maxGeneratedVertices": 1000000,
    "maxGeneratedTriangles": 2000000
  }
}
```

## 7. Versioning

- Any schema version other than exact `"0.1"` is an error.
- Unknown operation types are errors.
- Unknown fields outside the top-level `extensions` object are errors; required
  fields are never guessed or silently defaulted.
- Namespaced data inside `extensions` is preserved canonically but never changes
  geometry or compiler behavior.

## 8. Canonical interchange and safety

The canonical writer emits root properties in this order:

```text
schemaVersion, assetId, displayName, units, canvas,
panels, seams, operations, compile, extensions
```

Known nested properties use fixed order while semantic source arrays retain
their authored order. Numbers use invariant round-trip formatting, output uses
LF line endings and one trailing newline, and extension-object keys are sorted
ordinally. The parser rejects malformed or duplicate properties, non-finite
numbers, excessive size/depth/node/string/collection limits, invalid IDs,
missing references, and unsafe appearance paths before native geometry is
compiled.

Runtime performs no file or network I/O. Appearance resolution is supplied by
`IFoldScriptAppearanceResolver`; the Editor adapter accepts only normalized
project paths under `Assets/` or `Packages/`. Full API and repair-loop details
are in [the M08 runtime guide](foldscript-runtime.md).
