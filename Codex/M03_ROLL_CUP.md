# M03: Roll operator and decorated cup proof

## Visible proof

A single 2D canvas contains:

- a rectangular cup-wall region with `GPT 5.6` and a visible emblem
- a circular cup-bottom region with `CODEX`

After compilation:

- the wall is rolled into a cylindrical or tapered shell
- the artwork follows the wall continuously
- the disk is placed at the bottom plane
- the wall side seam is explicitly welded
- the disk perimeter is explicitly welded to the wall bottom
- the only remaining open topological boundary is the cup rim

The user authorized equal-sample `Weld` execution for the M03 cup after the
distance-only proof was found insufficient. General boundary resampling,
`Bridge`, `Solidify`, and thickness remain deferred.

## Roll semantics

Inputs:

- target panel
- direction U or V
- total roll angle
- start angle
- radius mode
- optional explicit radius

Required radius modes:

### PreserveArcLength

The source dimension along the roll direction becomes arc length. The compiler chooses the radius deterministically from source length and requested angle.

### Explicit

Use the provided radius and report stretch/compression ratio.

`FitTargetBoundary` remains unsupported until seam solving exists and must return a stable diagnostic.

## Mapping rules

- user-facing source contains no authored vertices or trigonometric code
- compiler may evaluate the deterministic roll mapping internally
- the non-roll source direction remains linear
- UV0 remains exactly source canvas UV
- positive source progression uses
  `theta = startAngleDegrees - t * angleDegrees`
- Roll reverses target triangle winding without changing connectivity, keeping
  a positive sweep radially outward and source U readable from the exterior
- full 360-degree first/last boundaries coincide spatially within tolerance;
  only an explicit Stitch operation welds their topology
- a full turn requires at least three source segments; two segments form two
  overlapping planar panels despite having nonzero triangle area
- Circular Roll is limited to one signed turn; larger sweeps return
  `FC3023 UnsupportedMultiTurnRoll`
- partial rolls remain open
- sign and start-angle conventions must be documented and tested
- the current target must be a congruent planar embedding; unit reflection may
  be accepted, while metric-changing scale, shear, collapse, and non-planarity
  fail with `FC3021 UnsupportedRollEmbedding`

## Cup sample

Add an importable or editor-generated sample with fixed physical dimensions. Include an Unlit-compatible preview material or clear instructions for one without adding a render-pipeline dependency.

The interactive cup proof uses an opaque two-sided Unlit preview shader because
M03 deliberately emits zero-thickness surfaces. This prevents the wall and
bottom from disappearing while the user orbits the object. Back faces mirror
their preview U lookup so readable artwork is not reversed when viewed through
the zero-thickness surface. This does not add inner-wall geometry or replace
the one-sided normal and handedness tests.

The proof owns an `EditorOnly` root containing the cup, source canvas, and one
untagged preview camera. It must reuse inactive owned objects, record Undo for
existing objects, and never read or modify `Camera.main`. The 64-sample wall
bottom boundary and disk perimeter must pass a numerical fit check before the
proof is shown.

## Minimum Stitch/Weld semantics

- Seam declarations remain inert until selected by a Stitch operation.
- M03 executes `Weld` only when effective boundary sample counts already
  match; it does not resample.
- A terminal render sample already welded to the first sample is counted once
  when a later closed-loop seam is matched.
- Paired positions must be within `weldEpsilon` and are snapped to one
  deterministic representative.
- UV/provenance/hard-normal attribute splits may retain separate render
  vertices, but all copies share one `TopologyVertexId`.
- Manifold validation operates on topology IDs. The cup must have no edge
  incidence above two and exactly its 64-edge top rim open.

## Tests

- zero angle fails with a useful diagnostic rather than divide-by-zero
- 180-degree roll produces a half-cylinder
- 360-degree preserve-arc-length roll produces expected radius
- seam endpoint maximum distance is below tolerance
- source height is preserved
- UV values are unchanged
- reversed roll angle reverses orientation predictably
- disk rigid placement matches intended cup bottom center and radius
- interactive preview material renders both sides without transparent gaps
- two-segment U/V full turns return `FC3022`
- multi-turn Circular Roll returns `FC3023`
- the owned preview is idempotent and leaves an existing MainCamera unchanged
- every wall-bottom sample coincides with the disk perimeter within tolerance
- equal-sample Stitch gives paired boundaries shared topology IDs
- the stitched cup has 1,281 topology vertices and only its top rim open
- source UVs remain distinct and unchanged across attribute seams
- the canonical exterior tangent carries increasing source U left-to-right

## Diagnostics

- zero or near-zero roll angle
- explicit radius not positive
- unsupported FitTargetBoundary
- unsupported source panel shape
- unsupported multi-turn Circular Roll
- excessive radial compression/stretch warning
- missing seam or boundary
- unsupported seam mode or required resampling
- seam gap above `weldEpsilon`
- non-manifold welded topology

## Non-goals

- no general resampling or Bridge
- no automatic proximity weld
- no inner wall
- no rim
- no real glass shader
- no handle
