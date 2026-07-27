# Diagnostics and validation

## Diagnostic shape

Every diagnostic contains:

- stable code
- severity
- message
- optional panel ID
- optional seam ID
- optional operation ID/index
- ordered, copied, read-only structured numeric values
- ordered, copied, read-only repair suggestions

Each structured numeric value has a stable key, finite `double` value, and
optional unit. Dictionaries are not used for this contract, so repeated
compiles preserve value and suggestion order. Diagnostics are emitted in
compiler stage and source order.

## Severity

- `Info`: useful compile detail
- `Warning`: output can be produced but may be undesirable
- `Error`: artifact is invalid or requested semantics were not fulfilled

## Code families

| Range | Meaning |
|---|---|
| FC0xxx | document and schema |
| FC1xxx | panel and tessellation |
| FC2xxx | boundary and seam |
| FC3xxx | operations |
| FC4xxx | thickness and topology |
| FC5xxx | mesh validation |
| FC6xxx | editor baking/import/export |

## Initial codes

- `FC0001 NullAsset`
- `FC0002 DuplicatePanelId`
- `FC0003 DuplicateOperationId`
- `FC0004 EmptyPanelId`
- `FC0005 InvalidCompileLimits`
- `FC1001 InvalidPanelDimensions`
- `FC1002 InvalidTessellation`
- `FC1003 CanvasRectOutOfRange`
- `FC1004 UnsupportedPanelShape`
- `FC1005 NonFinitePanelSize`
- `FC1006 NonPositivePanelSize`
- `FC1007 ExcessiveTessellation`
- `FC2001 MissingPanelReference`
- `FC2002 UnsupportedSeam` (reserved; seam declarations alone do not emit it)
- `FC2104 SeamLengthMismatch`
- `FC3001 UnsupportedOperation`
- `FC3002 NonFiniteOperationParameter`
- `FC3003 FoldTargetMissing`
- `FC3004 NonFiniteFoldLine`
- `FC3005 FoldLineOutOfRange`
- `FC3006 DegenerateFoldLine`
- `FC3007 AmbiguousFoldHinge`
- `FC3008 NonFiniteFoldAngle`
- `FC3009 UnsupportedFoldFalloff`
- `FC3010 InvalidFoldSide`
- `FC3011 FoldCreaseRequiresTopologySplit`
- `FC3012 RollTargetMissing`
- `FC3013 NonFiniteRollParameter`
- `FC3014 NearZeroRollAngle`
- `FC3015 InvalidExplicitRollRadius`
- `FC3016 UnsupportedFitTargetBoundary`
- `FC3017 UnsupportedRollPanelShape`
- `FC3018 RollStretchReport`
- `FC3019 InvalidRollDirection`
- `FC3020 InvalidRollRadiusMode`
- `FC3021 UnsupportedRollEmbedding`
- `FC3022 InsufficientRollTessellation`
- `FC4001 InvalidThickness`
- `FC5001 NonFiniteVertex`
- `FC5002 ZeroAreaTriangle`
- `FC5003 NonManifoldEdge`
- `FC5004 OpenBoundary`
- `FC5005 InvertedTriangle`
- `FC5006 SelfIntersection`

## Validation levels

### Basic

- source references
- finite numbers
- index bounds
- non-zero triangles

### Standard

Adds:

- open boundaries
- manifold edge counts
- seam closure distance
- duplicate vertices

### Strict

Adds expensive checks:

- self-intersection
- shell thickness collision
- orientation consistency across components
- topology expectations

## Failure philosophy

Do not silently:

- clamp a 720-degree operation to 360 degrees
- weld boundaries with incompatible intent
- reverse a seam without recording it
- drop triangles
- replace an unsupported operation with a no-op

A compiler that produces the wrong object politely is worse than one that stops with a precise error.
