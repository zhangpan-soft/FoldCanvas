# Diagnostics and validation

## Diagnostic shape

Every diagnostic contains:

- stable code
- severity
- message
- optional panel ID
- optional seam ID
- optional boundary ID
- optional operation ID/index
- optional read-only geometry context: render vertex, logical topology vertex,
  component, triangle pair, or logical topology edge
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
| FC6xxx | spherical mapping and closed-sphere validation |
| FC7xxx | FoldScript interchange, path safety, and repair responses |
| FC8xxx | boundary spans and toroidal mapping |
| FC90xx | explicit operation registration and transactional execution |
| FC91xx | sample-gallery manifest validation |
| FC92xx | deterministic text export validation |

## Initial codes

- `FC0001 NullAsset`
- `FC0002 DuplicatePanelId`
- `FC0003 DuplicateOperationId`
- `FC0004 EmptyPanelId`
- `FC0005 InvalidCompileLimits`
- `FC0006 InvalidValidationLevel`
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
- `FC2014 StitchSampleCountOutOfRange`
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
- `FC5005 GeneratedVertexLimitExceeded`
- `FC5006 GeneratedTriangleLimitExceeded`
- `FC5007 GeometryBudgetOverflow`
- `FC5008 InvalidTriangleIndex`
- `FC5009 IncompleteTriangleIndexBuffer`
- `FC5010 DuplicateTriangle`
- `FC5011 OpenTopologyBoundary`
- `FC5012 InconsistentWinding`
- `FC5013 DisconnectedGeometry`
- `FC5014 SeamClosureMismatch`
- `FC5015 BowTieTopologyVertex`
- `FC5016 TopologyPositionConflict`
- `FC5017 InvertedClosedComponent`
- `FC5018 SelfIntersection`
- `FC5019 StrictValidationBudgetExceeded`
- `FC5020 DegenerateCompiledBoundary`
- `FC6001 SphericalWrapTargetMissing`
- `FC6002 UnsupportedSphericalWrapPanelShape`
- `FC6003 NonFiniteSphericalWrapParameter`
- `FC6004 InvalidSphericalRadius`
- `FC6005 CollapsedSphericalRange`
- `FC6006 UnsupportedSphericalMultiTurn`
- `FC6007 InvalidSphericalWrapDirection`
- `FC6008 InvalidSphericalPoleMode`
- `FC6009 UnsupportedSphericalSubdivisionMode`
- `FC6010 UnsupportedSphericalEmbedding`
- `FC6011 InsufficientSphericalPoleTessellation`
- `FC6012 SphericalRadiusError`
- `FC6013 InvalidSphericalPoleTopology`
- `FC6014 SphereValidationFailed`
- `FC6015 DuplicateSphericalWrapTarget`
- `FC6016 SphereValidationRequiredBeforeSolidify`
- `FC7001 MalformedFoldScript`
- `FC7002 UnsupportedFoldScriptVersion`
- `FC7003 InvalidFoldScriptStructure`
- `FC7004 UnknownFoldScriptOperation`
- `FC7005 NonFiniteFoldScriptNumber`
- `FC7006 UnsafeAppearancePath`
- `FC7007 FoldScriptInputLimitExceeded`
- `FC7008 DuplicateFoldScriptIdentifier`
- `FC7009 FoldScriptAppearanceResolutionFailed`
- `FC7010 InvalidFoldScriptReference`
- `FC7011 InvalidRepairResponse`
- `FC7012 DuplicateFoldScriptProperty`
- `FC8001 InvalidBoundarySpan`
- `FC8002 ToroidalWrapTargetMissing`
- `FC8003 UnsupportedToroidalWrapPanelShape`
- `FC8004 NonFiniteToroidalWrapParameter`
- `FC8005 InvalidToroidalRadius`
- `FC8006 CollapsedToroidalRange`
- `FC8007 UnsupportedToroidalMultiTurn`
- `FC8008 UnsupportedToroidalEmbedding`
- `FC8009 InsufficientToroidalTessellation`
- `FC8010 ToroidalWindingFailed`
- `FC8011 InvalidToroidalWrapDirection`
- `FC9001 UnregisteredExtensionOperation`
- `FC9002 InvalidExtensionRegistration`
- `FC9003 DuplicateExtensionRegistration`
- `FC9004 ExtensionOperationTargetMissing`
- `FC9005 ExtensionOperationValidationFailed`
- `FC9006 ExtensionOperationExecutionFailed`
- `FC9007 ExtensionOperationInvalidMutation`
- `FC9008 ExtensionOperationException`
- `FC9101 MalformedGalleryManifest`
- `FC9102 UnsupportedGalleryVersion`
- `FC9103 InvalidGalleryEntry`
- `FC9104 DuplicateGalleryEntryId`
- `FC9201 ExportInputMissing`
- `FC9202 InvalidExportOptions`
- `FC9203 InvalidExportGeometry`

`FC2010` enforces the temporary terminal-Stitch contract: until topology-group
deformation propagation exists, a later RigidTransform, Fold, Roll,
SphericalWrap, ToroidalWrap, or registered position operation may not target a
panel selected by an earlier Stitch. Solidify
may follow because it consumes complete welded topology groups. Source
preflight also returns `FC2010` when a Stitch-selected endpoint's enabled
SphericalWrap is not strictly earlier than that Stitch. This form carries
`sphericalWrapOperationIndex` and `stitchOperationIndex` and runs before
tessellation or sphere reporting.

`FC2011`–`FC2013` stop collapsed, closure-incompatible, or non-subdividable
seams without creating unattached samples. `FC4001`–`FC4006` stop invalid
thickness, missing/partial targets, unbounded hard-corner offsets,
non-manifold input, and invalid direction values without returning a Mesh.
`FC4007` is the final selected-shell gate: it reports ordered open-edge,
non-manifold-edge, winding-conflict, collapsed-edge, topology-position,
component, and zero-volume counts when Solidify fails to create a closed
oriented volume.

`FC6001`–`FC6013` reject invalid spherical targets, fields, frames,
tessellation, radius, winding, or pole construction before a misleading
surface can escape. `FC6014` is the complete zero-thickness sphere gate. Its
ordered structural values report panels, render/topology vertices, triangles,
edges, open/non-manifold/orientation-conflict/isolated counts, components,
Euler characteristic, pole counts, inward triangles, frame inconsistencies,
maximum radius error, and tolerance. `FC6015` prevents two spherical mappings
from silently targeting the same panel. `FC6016` prevents a Solidify that
targets a spherical component from running before that component's final
touching Stitch and pre-Solidify sphere validation. `FC2001`, `FC2003`,
`FC2004`, and `FC2008` also protect component planning from invalid
Stitch-selected seam, panel, and boundary references.

`FC8001` rejects a non-finite, wrapping, reversed, collapsed, or out-of-range
boundary span before correspondence mutates geometry. Omission is the legacy
complete-boundary path. `FC8002`-`FC8011` reject missing or non-rectangular
ToroidalWrap targets, non-finite fields, radii that violate
`majorRadius > minorRadius > 0`, collapsed or multi-turn angular ranges,
non-congruent/non-planar current embeddings, insufficient full-turn source
segments, a surface whose emitted winding cannot be made consistently
tube-outward, or an invalid native direction enum. These diagnostics never
substitute a primitive torus or silently close coincident parameter boundaries.

`FC9001`-`FC9008` guard M10's native extension boundary. A custom operation
must be explicitly registered for the current compile under a valid unique
stable ID and exact definition type, pass preflight, and resolve one existing
panel. Failed, non-finite, or throwing execution is rolled back before the
diagnostic escapes and no Mesh is returned. `FC9101`-`FC9104` reject malformed,
unknown-version, unsafe, or duplicate gallery metadata before the Editor can
invoke a declared proof action. `FC9201`-`FC9203` reject absent compiled data,
invalid options, or structurally invalid immutable geometry; export never
repairs or mutates the input.

`FC2014` enforces the shared JSON/native `sampleCount` maximum of `8192`.
`FC5005` and `FC5006` report cumulative generated vertex or triangle budget
exhaustion with ordered `currentUsed`, `requestedAdditional`, and
`maximumAllowed` values. `FC5007` reports unsafe arithmetic or an operation
that attempts to exceed its exact reservation. Budget failures roll back the
failing Stitch or Solidify transaction and return no partial Mesh.

`FC5008`-`FC5020` are the M07 final-geometry diagnostics. Structural root
causes stop later checks, so an invalid index, duplicate face, or non-manifold
edge does not create a cascade. `FC5011` and `FC5013` are warnings because an
intentional sheet or multi-part asset is valid. `FC5014` rechecks only Weld
seams actually executed by Stitch. `FC5018` is an exact Strict-level
non-adjacent triangle intersection, not a broad-phase guess. `FC5019` stops
when the deterministic broad phase exceeds 250000 candidate pairs rather than
silently omitting exact tests. See
[M07 geometry validation](geometry-validation.md) for ordering and evidence.

## Validation levels

### Basic

- complete triangle index triples and in-range indices
- finite generated positions
- non-collapsed logical triangles and non-zero geometric area
- duplicate topology triangles
- non-manifold logical edges and local edge-winding conflicts

### Standard

Adds:

- logical-topology position agreement and bow-tie vertex fans
- compiled-boundary length
- executed Weld seam closure distance
- inward orientation of closed components
- open-boundary and disconnected-component warnings

### Strict

Adds expensive checks:

- deterministic sweep-and-prune broad phase
- exact non-adjacent triangle-triangle intersection tests
- a hard candidate-pair budget that fails instead of degrading silently

The sphere-specific report still proves component topology, manifold edge
incidence, Euler characteristic, pole identities, frame/radius agreement, and
winding at its documented operation stage. `IsClosedSphere` alone is not a
universal no-self-intersection claim. A later successful Strict M07 report
adds final-buffer triangle-intersection evidence.

## FoldScript and repair diagnostics

`FC7001`-`FC7012` form M08's untrusted-text boundary. The reader reports one
stable primary root cause and does not continue into native source conversion
or Mesh generation:

- syntax/truncation is `FC7001`; a duplicate JSON key is the more specific
  `FC7012`;
- any version other than exact `0.1` is `FC7002`, and an unknown operation type
  is `FC7004`;
- missing or unknown fields, invalid field shapes/enums/ranges, and unsupported
  document structure are `FC7003`;
- JSON tokens such as `NaN` or `Infinity`, and non-finite DTO values during
  write, are `FC7005`;
- absolute, URI-like, backslash-ambiguous, traversal, or out-of-root appearance
  references are `FC7006`; a safe path that cannot resolve is `FC7009`;
- character, depth, node, string, identifier, collection, or pixel-dimension
  limits use `FC7007` before unbounded native allocation;
- duplicate panel/seam/operation IDs use `FC7008`; missing panel, boundary,
  seam, or operation target references use `FC7010`;
- a null, empty, or otherwise absent replacement source in the repair response
  uses `FC7011`. Non-empty invalid replacement JSON returns its ordinary
  FoldScript diagnostic instead.

Diagnostic order, structured values, geometry context, and suggestions are
copied into immutable repair payload collections. A response cannot suppress a
diagnostic except by supplying complete replacement FoldScript that passes the
same importer and compiler.

## Failure philosophy

Do not silently:

- clamp a 720-degree operation to 360 degrees
- emit a 720-degree Circular Roll as overlapping zero-thickness layers
- leave a newly inserted spherical seam sample on a straight chord
- collapse a rectangular grid into a pole after deformation and call it cleanup
- substitute a Unity Sphere when an explicit gore seam graph is invalid
- substitute a Unity torus when an explicit toroidal parameter surface or
  either cycle-closing seam is invalid
- turn coincident full-turn toroidal boundary positions into an implicit weld
- wrap or clamp an invalid boundary span
- weld boundaries with incompatible intent
- reverse a seam without recording it
- drop triangles
- replace an unsupported operation with a no-op

A compiler that produces the wrong object politely is worse than one that stops with a precise error.
