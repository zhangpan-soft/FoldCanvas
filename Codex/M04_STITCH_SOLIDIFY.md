# M04: Boundary resampling, stitching, and thickness

## Milestone state

M04 is active after M03 human approval, PR #1 merge, and the human-approved
planning PR #2 merge at `1644090`. Geometry implementation occurs on
`feat/m04-stitch-solidify`. The executable plan is
[`Docs/Plans/active-plan.md`](../Docs/Plans/active-plan.md).

## Visible proof

The decorated cup becomes a visibly correct closed shell with configurable wall
and base thickness. It has an exterior surface, a correctly wound inner wall,
and a generated top rim. The wall closure and wall-to-bottom attachment use the
same reusable seam-processing path exercised by mismatched sample counts.

The proof must be inspected in Unity from exterior, interior, rim, and underside
views with a one-sided material. A collider may be generated after Unity
convexity limitations are acknowledged, but collider convenience cannot hide
open, overlapping, inverted, or non-manifold topology.

The seam proof is performed in two passes:

1. a texture-free, solid-color, one-sided material proves geometry
2. `M04ProductionCupCanvas.png` proves bilinear-filtered appearance without
   atlas-background contamination

The production wall fills its rectangular UV region with square corners and no
outline along welded `vMin`. The bottom color reaches its perimeter and bleeds
8 to 16 pixels beyond the sampled circle. Wall `vMin` and bottom `perimeter`
use matching colors. The decorated M03 canvas remains a presentation example
only.

The default camera is a normal exterior view. Exact-side, interior, and
underside views remain separate validation views.

M04.1 adds a separate `Cup ClosedVolume` validation example. The same
production 2D source canvas and deterministic Roll, Stitch, and Solidify rules
must produce:

- a one-sided, texture-free solid proof
- a unique logical-topology wireframe
- a vertical section proof
- automatically derived `OuterCorner` and `InnerCorner` overlays at the welded
  wall/bottom hard corner

The wireframe, section lines, and corner overlays are Editor-only derived
evidence. They do not replace or modify the FoldCanvas source or compiled cup.

The accepted cup placement is locked: bottom rotation `(90, 0, 0)`, bottom and
wall `vMin` at `Y = -height / 2`, and explicit wall-to-bottom Weld. Do not
move, enlarge, overlap, or epsilon-offset the disk to conceal a line.

## Operation-ordering constraint

Until topology-group deformation propagation is implemented, Stitch is the
terminal position-deforming operation for every panel selected by that Stitch.

- Resolve the panels named by every seam selected by a Stitch operation.
- Any later `RigidTransform`, `Fold`, or `Roll` targeting one of those panels
  fails deterministically with `FC2010 StitchMustBeTerminalForSelectedPanels`
  and produces no Mesh.
- A later deformation of an unrelated panel remains legal.
- `Solidify` is allowed after Stitch because it consumes the final stitched
  topology as one shell-construction stage. It must never move only one render
  copy or one panel-side of a shared topology group.
- Relaxing this constraint requires a future topology-group deformation
  propagation task and, if the data contract changes, an ADR.

This is a bounded M04 ordering rule, not a reason to reopen M03 validation.

## Boundary solver

Implement deterministic normalized-arc-length resampling:

1. extract ordered boundary samples
2. remove only a redundant terminal closed-loop sample that already shares the
   first sample's logical topology identity
3. calculate cumulative current-space length
4. normalize to `[0,1]`
5. choose a stable common sample count
6. retain the sorted union of both boundaries' existing normalized breakpoints
7. when `sampleCount > 0`, add a uniform minimum-density parameter grid;
   existing breakpoints remain mandatory and may make the paired count larger
8. subdivide boundary edges and their incident surface triangles when a
   required parameter is absent
9. interpolate current position, source position, source UV, ownership, and
   deterministic generated provenance
10. apply declared orientation/reversal

Never assume equal source vertex counts. Never append unattached seam samples:
every inserted boundary vertex must participate in the adjacent source surface
topology. Repeated compilation must produce identical samples, triangle order,
topology identities, and diagnostics.

In M04, `sampleCount` is a requested minimum correspondence density, not
permission to discard authored boundary breakpoints. This meaning must be
synchronized across the JSON Schema, FoldScript specification, and field
reference when the solver is implemented.

## Stitch modes

### Weld

Create shared logical topology when semantics permit. Preserve separate render
vertices where UV, provenance, or hard-normal charts differ, but union their
`TopologyVertexId` and snap them to a deterministic representative. Do not
infer a Weld from spatial coincidence.

### Bridge

If boundaries cannot share direct vertex identity, build a deterministic strip
with controlled winding, explicit generated provenance, and a documented UV
policy. Bridge must use the same ordered correspondence solver as Weld.

`Hinge` and `KeepOpen` may remain metadata-only if explicitly documented.

## Solidify

- operate on the final stitched topology, not isolated source-panel ranges
- generate outer and inner shells
- support inward, outward, and centered offsets
- reverse inner winding
- classify open boundaries after welding
- generate side walls only for true open boundaries, such as the cup rim
- do not create hidden internal walls across welded cup-bottom seams
- retain source UV0 exactly on outer and inner shell copies
- give generated rim/side-wall vertices a deterministic UV0 policy based on
  their source boundary endpoints
- solve welded hard corners, including the cup wall-to-bottom join, from their
  incident face-offset planes so wall and bottom inner copies meet at one
  topology position with the requested face thickness
- require Solidify targets to include complete welded topology components
- preserve stable topology and generated provenance
- reject invalid or non-manifold shell construction instead of returning a
  visually plausible approximation

## M04.1 closed-volume validation

A successful compile exposes an immutable report over logical topology. A
component is a closed volume only when:

- it contains triangles
- every logical edge has exactly two oppositely directed incident triangles
- every logical topology identity resolves to one current position
- its absolute signed volume is non-zero

The report also records connected-component, open-edge, non-manifold-edge,
orientation-conflict, collapsed-edge, and zero-volume counts. The Cup
ClosedVolume example must report exactly one closed component.

Solidify applies the same check to only the shell produced by its selected
panels before it can return success. Unrelated panels are excluded from that
operation-scoped validation.

This proof does not claim robust global self-intersection detection.

Solidify also records paired outer/inner hard-corner segments referencing the
actual emitted shell vertices. The cup's wall `vMin` / bottom `perimeter`
connection therefore exposes continuous `OuterCorner` and `InnerCorner` rings
without adding bevel geometry.

## Tests

- mismatched 32/64-sample boundaries stitch correctly
- seam reversal changes correspondence as expected
- welded seam has no crack above epsilon
- cup output has expected open rim only before rim side wall, then becomes a valid shell boundary arrangement
- manifold edge incidence is correct
- inner normals point inward
- thickness is within tolerance at sampled points
- no zero-area triangles at seam or rim
- post-Stitch deformation of a selected panel returns the terminal-operation
  diagnostic
- post-Stitch deformation of an unrelated panel remains legal
- Stitch followed by Solidify consumes the final welded topology

Required named ordering tests:

- `PostStitchRigidTransform_OnSelectedPanel_ReturnsTerminalOperationDiagnostic`
- `PostStitchFold_OnSelectedPanel_ReturnsTerminalOperationDiagnostic`
- `PostStitchRoll_OnSelectedPanel_ReturnsTerminalOperationDiagnostic`
- `PostStitchDeformation_OnUnselectedPanel_RemainsAllowed`
- `StitchThenSolidify_UsesFinalTopology`

Required result tests:

- `BottomPanel_AllVerticesAreCoplanar`
- `WallBottomWeld_HasZeroBoundaryPositionGap`
- `WallBottomWeld_HasNoOpenTopologyEdge`
- `Solidify_WallBottomInnerCornerRemainsConnected`

## Diagnostics

- `FC2010 StitchMustBeTerminalForSelectedPanels`
- missing boundary
- zero-length boundary
- seam length mismatch warning/error by threshold
- orientation conflict
- weld collapse
- invalid thickness
- solidify self-overlap warning
- non-manifold result

## Non-goals

- no additional M03 defensive parameter-validation pass
- no topology-group deformation propagation
- no robust global self-intersection repair
- no bevels
- no subdivision
- no smoothing
- no mesh-cleanup postprocessing
- no bottom overlap or transform adjustment used as a seam workaround
- no variable thickness field
- no handle
- no SpiralRoll or LayeredRoll
- no M05 sphere-gores work
