# M04: Boundary resampling, stitching, and thickness

## Visible proof

The decorated cup becomes a closed manifold shell with configurable wall and base thickness. The generated mesh can be used as a Unity collider after convexity limitations are acknowledged.

## Boundary solver

Implement deterministic normalized-arc-length resampling:

1. extract ordered boundary samples
2. calculate cumulative current-space length
3. normalize to `[0,1]`
4. choose a stable common sample count
5. resample positions and source provenance
6. apply declared orientation/reversal

Never assume equal source vertex counts.

## Stitch modes

### Weld

Create shared topology when semantics permit. Avoid duplicate coincident seam vertices in final artifact.

### Bridge

If boundaries cannot share direct vertex identity, build a strip with controlled winding and provenance.

`Hinge` and `KeepOpen` may remain metadata-only if explicitly documented.

## Solidify

- generate outer and inner shells
- support inward, outward, and centered offsets
- reverse inner winding
- classify open boundaries after welding
- generate side walls only for true open boundaries, such as the cup rim
- do not create hidden internal walls across welded cup-bottom seams
- provide a deterministic UV policy for generated rim/side-wall faces

## Tests

- mismatched 32/64-sample boundaries stitch correctly
- seam reversal changes correspondence as expected
- welded seam has no crack above epsilon
- cup output has expected open rim only before rim side wall, then becomes a valid shell boundary arrangement
- manifold edge incidence is correct
- inner normals point inward
- thickness is within tolerance at sampled points
- no zero-area triangles at seam or rim

## Diagnostics

- missing boundary
- zero-length boundary
- seam length mismatch warning/error by threshold
- orientation conflict
- weld collapse
- invalid thickness
- solidify self-overlap warning
- non-manifold result

## Non-goals

- no robust global self-intersection repair
- no bevels
- no variable thickness field
- no handle
