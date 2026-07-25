# M03: Roll operator and decorated cup proof

## Visible proof

A single 2D canvas contains:

- a rectangular cup-wall region with `GPT 5.6` and a visible emblem
- a circular cup-bottom region with `CODEX`

After compilation:

- the wall is rolled into a cylindrical or tapered shell
- the artwork follows the wall continuously
- the disk is placed at the bottom plane
- the two source surfaces visually align

Topological welding and thickness are intentionally deferred to M04.

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
- full 360-degree first/last boundaries coincide spatially within tolerance, while remaining topologically separate until M04
- partial rolls remain open
- sign and start-angle conventions must be documented and tested

## Cup sample

Add an importable or editor-generated sample with fixed physical dimensions. Include an Unlit-compatible preview material or clear instructions for one without adding a render-pipeline dependency.

## Tests

- zero angle fails with a useful diagnostic rather than divide-by-zero
- 180-degree roll produces a half-cylinder
- 360-degree preserve-arc-length roll produces expected radius
- seam endpoint maximum distance is below tolerance
- source height is preserved
- UV values are unchanged
- reversed roll angle reverses orientation predictably
- disk rigid placement matches intended cup bottom center and radius

## Diagnostics

- zero or near-zero roll angle
- explicit radius not positive
- unsupported FitTargetBoundary
- unsupported source panel shape
- excessive radial compression/stretch warning

## Non-goals

- no weld
- no inner wall
- no rim
- no real glass shader
- no handle
