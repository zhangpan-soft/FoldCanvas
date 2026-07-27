# FoldScript specification draft

FoldScript is the portable, human-readable interchange format for FoldCanvas source documents. Unity `ScriptableObject` assets are an editor representation; FoldScript is the long-term serialized contract.

Schema file: [`../Schema/foldcanvas.schema.json`](../Schema/foldcanvas.schema.json)

Field-by-field semantics, units, formulas, defaults, and implementation status:
[`foldscript-field-reference.md`](foldscript-field-reference.md).

Project motivation and the production problem this format addresses:
[`project-background.md`](project-background.md).

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
3D axis, compilation fails instead of guessing.

### Roll

```json
{
  "id": "roll-wall",
  "type": "roll",
  "panel": "wall",
  "direction": "u",
  "angleDegrees": 360.0,
  "radiusMode": "preserveArcLength",
  "startAngleDegrees": 0.0
}
```

`roll` means continuously wrapping one panel dimension onto a circular arc; it
does **not** mean applying one rigid rotation to the panel. The compiler may use
trigonometric evaluation internally, but no user-authored vertices or UV unwrap
are required. Exact direction, radius, angle, and seam semantics are defined in
the [field reference](foldscript-field-reference.md#63-roll--planned-for-m03).

### Solidify

```json
{
  "id": "thicken-cup",
  "type": "solidify",
  "targets": ["wall", "bottom"],
  "thickness": 0.002,
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

A zero sample count means the compiler selects a stable common count.

Cup-bottom seam:

```json
{
  "id": "wall-bottom-seam",
  "a": { "panel": "wall", "boundary": "vMin" },
  "b": { "panel": "bottom", "boundary": "perimeter" },
  "mode": "weld",
  "reverseB": true,
  "sampleCount": 64
}
```

## 6. Complete cup sketch

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
      "canvasRect": [0.05, 0.10, 0.90, 0.50],
      "physicalSize": [0.31415927, 0.12],
      "tessellation": { "uSegments": 64, "vSegments": 12 }
    },
    {
      "id": "bottom",
      "shape": "disk",
      "canvasRect": [0.10, 0.68, 0.22, 0.22],
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
      "reverseB": true,
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
      "startAngleDegrees": 0.0
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
      "thickness": 0.002,
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

- Unknown major schema versions are errors.
- Unknown operation types are errors.
- Unknown optional fields produce warnings only when forward compatibility is safe.
- Importers must preserve unrecognized metadata under a dedicated extension object once schema extensions are introduced.
