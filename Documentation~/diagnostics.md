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
- `FC2003 StitchSeamMissing`
- `FC2004 StitchBoundaryMissing`
- `FC2005 StitchSampleCountMismatch`
- `FC2006 StitchPositionMismatch`
- `FC2007 UnsupportedStitchSeamMode`
- `FC2008 DuplicateSeamId`
- `FC2009 EmptyStitchSeamList`
- `FC2010 StitchMustBeTerminalForSelectedPanels`
- `FC2011 ZeroLengthStitchBoundary`
- `FC2012 StitchBoundaryClosureMismatch`
- `FC2013 StitchBoundarySubdivisionFailed`
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
- `FC3023 UnsupportedMultiTurnRoll`
- `FC4001 InvalidSolidifyThickness`
- `FC4002 SolidifyTargetMissing`
- `FC4003 IncompleteSolidifyTopologySelection`
- `FC4004 UnsupportedSolidifyCorner`
- `FC4005 NonManifoldSolidifyInput`
- `FC4006 InvalidSolidifyDirection`
- `FC4007 SolidifyClosedVolumeValidationFailed`
- `FC5001 NonFiniteVertex`
- `FC5002 ZeroAreaTriangle`
- `FC5003 NonManifoldTopology`
- `FC5004 InvalidWeldEpsilon`

`FC2010` enforces the temporary terminal-Stitch contract: until topology-group
deformation propagation exists, a later RigidTransform, Fold, or Roll may not
target a panel selected by an earlier Stitch. Solidify may follow because it
consumes complete welded topology groups.

`FC2011`–`FC2013` stop collapsed, closure-incompatible, or non-subdividable
seams without creating unattached samples. `FC4001`–`FC4006` stop invalid
thickness, missing/partial targets, unbounded hard-corner offsets,
non-manifold input, and invalid direction values without returning a Mesh.
`FC4007` is the final selected-shell gate: it reports ordered open-edge,
non-manifold-edge, winding-conflict, collapsed-edge, topology-position,
component, and zero-volume counts when Solidify fails to create a closed
oriented volume.

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
- emit a 720-degree Circular Roll as overlapping zero-thickness layers
- weld boundaries with incompatible intent
- reverse a seam without recording it
- drop triangles
- replace an unsupported operation with a no-op

A compiler that produces the wrong object politely is worse than one that stops with a precise error.
