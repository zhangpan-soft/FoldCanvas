# M05: Sphere gores and information-preserving 2D encoding

## Visible proof

A symmetric set of 2D gore regions carrying visible latitude/longitude artwork compiles into a closed sphere. The artwork joins predictably across seams and radial error is numerically bounded.

## Mathematical statement

Do not claim the sphere is flattened isometrically. The 2D source is information-preserving through explicit reconstruction mapping and seams. Document distortion.

## Source representation

Support one of these approaches, selected in the execution plan:

1. multiple explicit gore panels plus a `SphericalWrap` operation, or
2. one gore template with deterministic rotational replication

Do not hardcode a Unity Sphere primitive or copy its mesh.

## Required behavior

- configurable sphere radius
- configurable gore count
- predictable north/south pole treatment
- explicit neighboring seam graph
- source UV preservation
- outward winding
- no duplicate non-manifold pole fans
- optional distortion visualization data

## Tests

- all surface vertices lie at target radius within tolerance
- closed manifold topology
- Euler characteristic matches a sphere after final welding
- no open boundaries
- pole vertices do not generate zero-area triangles
- neighboring seam gaps below epsilon
- deterministic output for fixed gore count and tessellation
- visible artwork orientation test through UV samples

## Non-goals

- no arbitrary genus
- no texture seam painting algorithm
- no geodesic optimality claim
- no adaptive pole remeshing beyond what is required for valid output
