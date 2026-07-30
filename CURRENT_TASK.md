# Current task

Execute **M05: Sphere gores and spherical surface reconstruction**.

Authoritative task file:
[`Codex/M05_SPHERE_GORES.md`](Codex/M05_SPHERE_GORES.md)

M04 and M04.1 passed human audit and were merged through PR
[#3](https://github.com/zhangpan-soft/FoldCanvas/pull/3) into `main` at merge
commit `ef36808`. The accepted implementation heads `ecb0791` and `b78772f`
remain in that history. Acceptance evidence is Unity `6000.3.20f1`, 152/152
Edit Mode tests, a one-component closed-volume report, zero open or
non-manifold logical edges, and texture-free solid, wireframe, and section
proofs.

M05 implementation occurs on `codex/m05-spherical-wrap`. It must prove that
explicit 2D spherical panels, ordered source boundaries, `SphericalWrap`,
the existing seam graph, Weld, and the deterministic compiler can reconstruct
one closed sphere while preserving source UV, canvas coordinates, provenance,
and outward winding.

The generated sphere Mesh remains a derived artifact. Do not use a Unity
Sphere primitive, UV Sphere, Icosphere, imported sphere Mesh, fixed precomputed
sphere vertices, or an automatic mesh-repair/cleanup stage as the generation
path.

M05 must not implement Bevel, Subdivision surface smoothing, Remesh, Mesh
Cleanup, arbitrary topology repair, or M06. Keep `CURRENT_TASK.md` on M05
until the full diff, complete Edit Mode suite, topology/radius report, and
actual Unity sphere proof pass human audit.
