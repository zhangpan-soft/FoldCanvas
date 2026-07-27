# Current task

Execute **M04: Boundary resampling, reusable stitching, and solidify**.

Authoritative task file:
[`Codex/M04_STITCH_SOLIDIFY.md`](Codex/M04_STITCH_SOLIDIFY.md)

M03 passed human audit and was merged through PR
[#1](https://github.com/zhangpan-soft/FoldCanvas/pull/1) into `main` at merge
commit `c7b1e61`. The accepted PR head `96d1688` is retained in that history.
Acceptance evidence is Unity `6000.3.20f1`, 103/103 Edit Mode tests, and a live
owned preview proving readable exterior artwork, explicit wall closure, an
explicit wall-to-bottom Weld, 1,281 logical topology vertices, exactly 64 open
top-rim edges, and zero measured seam gap.

M04 must prioritize visible and topological correctness of reusable seam
processing, thickness, inner walls, and rim generation. Until topology-group
deformation propagation exists, Stitch is terminal for every panel selected by
that Stitch: no later `RigidTransform`, `Fold`, or `Roll` may target those
panels. `Solidify` is the downstream whole-topology shell-construction stage,
not a per-panel deformation.

Do not reopen M03 for additional defensive parameter validation. Do not begin
M05 or later milestones until M04 acceptance criteria pass and this file is
deliberately advanced.
