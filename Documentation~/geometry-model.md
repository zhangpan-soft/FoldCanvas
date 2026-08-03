# Geometry model

## 1. What “2D-first” means

A FoldCanvas document describes a surface through one or more two-dimensional domains. The document may preserve all reconstruction information without preserving planar distances, angles, or area.

This distinction is essential:

- **Isometric flattening** preserves intrinsic distances and is impossible for many curved surfaces.
- **Information-preserving encoding** may use cuts, distortion, curvature, topology, and reconstruction instructions.

FoldCanvas targets the second concept.

## 2. Panel parameter domain

Each panel has local parameters `(u,v)` and a map into source canvas UV coordinates:

```text
CanvasUv(u,v) → (s,t)
```

Compilation creates a 3D embedding:

```text
Position(u,v) → (x,y,z)
```

The same samples retain `CanvasUv`, so appearance follows every fold and deformation automatically.

## 3. Discrete MVP representation

The MVP uses sampled triangle meshes internally:

```text
Panel domain
  ↓ tessellation
2D vertices + triangles + ordered boundaries + canvas UVs
  ↓ operations
3D vertices + unchanged topology where possible
```

Continuous operations are evaluated at sampled panel vertices. Adaptive refinement is a future feature.

## 4. Future field representation

A general surface atlas can be described through local fields such as:

- metric/stretch field
- shear field
- principal curvature magnitudes
- principal curvature direction
- thickness
- semantic/material class
- preferred tessellation density

These fields should be treated as source instructions, not mandatory MVP complexity.

## 5. Topology

Topology is encoded through panel boundaries and the seam graph.

Examples:

- Cylinder wall: rectangle `uMin` welded to `uMax`
- Cup: cylinder wall `vMin` welded to disk `perimeter`
- Sphere gores: neighboring gore side boundaries welded; tips merged into poles
- Torus: both parameter directions close cyclically
- Handle cup: a tube or strip attaches to two wall boundaries, creating a non-trivial loop

Spatial coincidence does not create topology automatically. Only explicit seam rules may weld source boundaries.

## 6. Spherical parameter surfaces

M05 represents a sphere as an atlas of explicit 2D rectangle domains. Each
panel carries immutable source position and canvas UV, while
`SphericalWrap` supplies the map from one normalized panel axis to longitude
and the other to latitude:

```text
P(lambda, phi) =
  Center
  + r cos(phi) cos(lambda) CurrentU
  + r sin(phi)             CurrentV
  + r cos(phi) sin(lambda) CurrentNormal
```

The map may stretch the 2D metric; this is expected for a spherical atlas and
is exposed as derived area-stretch metadata. It does not erase the 2D domain.

A geometric pole is one logical topology identity even when several render
vertices are needed to retain distinct UV or provenance samples. `Merge`
chooses one render sample per panel fan. `KeepFan` retains one render copy per
adjacent longitude cell but unions their `TopologyVertexId`. Neighboring panel
poles become the one final north/south identity only through explicit Weld
seams.

Boundary subdivision on a curved panel interpolates immutable source
coordinates and UV, then re-evaluates the surface map. Linear interpolation of
current 3D positions would create a chord and violate the radius contract.
There is no post-generation pole collapse, remesh, or automatic topology
repair.

## 7. Thickness

A surface is conceptually zero-thickness. `Solidify` creates a shell:

1. classify open and welded boundaries
2. compute or derive offset directions
3. create outer and inner surfaces
4. reverse inner winding
5. create side walls only on open boundaries
6. prevent duplicate walls inside welded seams

M04 render copies that share one logical topology identity also share
one solved offset position. Smooth groups may use a deterministic
incident-plane solution. A welded hard corner such as the cup wall-to-bottom join
requires an incident face-offset-plane solution so the inner wall and inner
bottom meet without a crack and retain the requested perpendicular thickness.
If that corner has no stable bounded solution, compilation fails rather than
guessing an averaged direction.

M04 does not attempt global self-intersection repair, variable thickness, or
bevels. Those require separate contracts.

M04.1 records each material hard edge as paired outer/inner corner segments
that point at the actual Solidify vertices. The cup's welded wall-bottom loop
therefore exposes an `OuterCorner` ring and an `InnerCorner` ring without
adding any rounding or cleanup geometry. The centralized hard-corner threshold
is an incident unit-normal dot product of `0.95` or less.

The compiler also derives a closed-volume report from logical topology:

```text
closed component =
  every logical edge used twice in opposite directions
  + one position per logical topology identity
  + non-zero absolute signed volume
```

This proves a closed, consistently oriented triangle shell. It is deliberately
separate from future robust global self-intersection analysis.

## 8. UV preservation invariant

For each generated vertex originating from panel sample `(u,v)`:

```text
generatedUv0 = CanvasUv(u,v)
```

Operations may duplicate vertices for seams, hard edges, or thickness, but duplicated vertices retain the same source UV unless an operation explicitly introduces a new side-wall UV policy.

M01 also retains the panel-local 2D source position, panel ownership, and a
deterministic provenance ID for every compiled vertex. Later duplication or
welding stages must preserve or deliberately combine those identifiers rather
than inferring origin from current 3D proximity.

## 9. Boundary parameterization

Each boundary is sampled and ordered. For stitching:

1. compute cumulative physical arc length
2. normalize both boundaries to `[0,1]`
3. select a common sample count
4. retain both boundaries' existing normalized breakpoints
5. add any requested minimum-density parameter grid
6. split boundary-adjacent surface triangles at missing parameters
7. respect or reverse orientation as declared
8. weld or bridge according to seam mode

Boundary count equality must never be assumed. Existing boundary breakpoints
must not be discarded merely to force an exact count, because their source UV
and provenance remain part of the authored surface.

For a recorded spherical meridian boundary, cumulative distance is evaluated
as exact spherical arc length. Any inserted current position is evaluated from
its source coordinate through the spherical map, so correspondence density
cannot pull the boundary inside the sphere.

## 10. Numerical tolerances

The compiler must centralize tolerances:

```text
position weld epsilon
zero-area triangle epsilon
normal comparison epsilon
boundary-length warning threshold
self-intersection epsilon
```

Do not scatter magic epsilon values across operations.

## 11. Derived normals

The user does not author normal maps in the core workflow. Geometric normals are derived after topology-affecting operations. An Unlit material can ignore them, but geometry validation and optional lighting still need consistent winding and normals.
