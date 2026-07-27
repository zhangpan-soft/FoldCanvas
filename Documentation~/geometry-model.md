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

## 6. Thickness

A surface is conceptually zero-thickness. `Solidify` creates a shell:

1. classify open and welded boundaries
2. compute or derive offset directions
3. create outer and inner surfaces
4. reverse inner winding
5. create side walls only on open boundaries
6. prevent duplicate walls inside welded seams

The first implementation may use vertex-normal offsets. Later versions may need collision-aware offsets and corner treatment.

## 7. UV preservation invariant

For each generated vertex originating from panel sample `(u,v)`:

```text
generatedUv0 = CanvasUv(u,v)
```

Operations may duplicate vertices for seams, hard edges, or thickness, but duplicated vertices retain the same source UV unless an operation explicitly introduces a new side-wall UV policy.

M01 also retains the panel-local 2D source position, panel ownership, and a
deterministic provenance ID for every compiled vertex. Later duplication or
welding stages must preserve or deliberately combine those identifiers rather
than inferring origin from current 3D proximity.

## 8. Boundary parameterization

Each boundary is sampled and ordered. For stitching:

1. compute cumulative physical arc length
2. normalize both boundaries to `[0,1]`
3. select a common sample count
4. resample both curves at matching parameters
5. respect or reverse orientation as declared
6. weld or bridge according to seam mode

Boundary count equality must never be assumed.

## 9. Numerical tolerances

The compiler must centralize tolerances:

```text
position weld epsilon
zero-area triangle epsilon
normal comparison epsilon
boundary-length warning threshold
self-intersection epsilon
```

Do not scatter magic epsilon values across operations.

## 10. Derived normals

The user does not author normal maps in the core workflow. Geometric normals are derived after topology-affecting operations. An Unlit material can ignore them, but geometry validation and optional lighting still need consistent winding and normals.
