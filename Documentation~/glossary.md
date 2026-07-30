# Glossary

**Appearance canvas**  
The 2D image containing visible artwork used by one or more panels.

**Boundary**  
An ordered edge curve on a panel, addressable by stable ID.

**Compile artifact**  
A generated mesh, material, collider, validation report, or prefab. It is disposable and reproducible from source.

**FoldCanvas asset**  
The authoritative source document inside Unity.

**FoldScript**  
The portable JSON representation of FoldCanvas source.

**Operation**  
A deterministic transformation or topology command applied to source panels.

**Panel**  
A bounded 2D domain that is tessellated and embedded into 3D.

**Seam**  
An explicit topological relationship between two ordered boundaries.

**Solidify**  
The operation that turns a zero-thickness surface into a shell with thickness.

**SphericalWrap**
An M05 deformation that maps an explicit 2D rectangle parameter panel into a
spherical latitude/longitude patch in the panel's current local frame. It is
not a sphere generator and does not create seam topology by itself.

**Source UV**  
The coordinate on the original appearance canvas retained by generated vertices.

**Surface atlas**  
A collection of 2D charts/panels that together describe a more complex surface.

**Topology vertex**
A logical surface identity used for edge incidence and manifold validation.
Multiple render vertices may share one topology vertex while retaining
different source UV or provenance values.
