# M06 authoring workspace

FoldCanvas M06 provides an Editor-only UI Toolkit workspace for editing the
authoritative 2D source and inspecting its disposable 3D result. Open it with:

`Tools > FoldCanvas > Open Authoring Workspace`

The left viewport displays the appearance canvas, panel rectangles/disks,
named boundaries, and selected seam endpoints. The right viewport displays a
locally owned derived preview. The bottom tabs edit panels, ordered operations,
seams, compiler diagnostics, and explicit bake output.

## Source and preview contract

- The `FoldCanvasAsset`, appearance canvas, panels, seams, and ordered
  operations are source. Preview and baked Meshes are compiler outputs.
- Every source edit is recorded with Unity Undo/Redo and marks the source
  asset dirty.
- Compilation is debounced after editing. `Compile Now` bypasses the wait.
- A monotonically increasing source revision discards a result if the source
  changes while that compile is running.
- Preview objects, Meshes, materials, and debug lines are `HideAndDontSave`,
  owned by the window, excluded from builds, and destroyed on replacement or
  window close. The workspace never reads or changes `Camera.main`.
- Baking is explicit. An invalid compile cannot create or overwrite a baked
  Mesh. The current data model does not embed a source-reference sidecar in
  the Mesh; the source asset remains separate and authoritative.

## Controls

In the 2D viewport, use the mouse wheel to zoom around the cursor. Pan with
the middle mouse button or `Alt` plus left drag. `Frame` resets the view.
Select a panel to expose its four canvas-rect handles. Rectangle boundaries
are `uMin`, `uMax`, `vMin`, and `vMax`; a disk has `perimeter`. A selected seam
shows endpoint A in cyan and endpoint B in pink.

In the 3D viewport, left-drag orbits, the wheel zooms, and `Frame` fits the
current result. `Solid` forces the one-sided texture-free diagnostic material
so backface/winding problems cannot be hidden by a two-sided presentation
shader. The toolbar can also show logical wireframe, per-panel colors, the
selected seam, normals, and Solidify thickness segments. These are inspection
overlays, not editable topology.

## Manual proof: blank source to closed cup

This walkthrough uses the same geometry contract as the M04 production cup.
It changes no vertex or triangle directly.

1. Open the workspace and choose `New Source`. Save the asset under `Assets`.
   In `Panels`, assign `M04ProductionCupCanvas.png` if it is available. The
   solid preview also works without a texture.
2. Add a Rectangle, rename it `wall`, and enter:

   - Canvas Rect: `x=0.06, y=0.46, width=0.88, height=0.44`
   - Physical Size: `x=0.31415927, y=0.12`
   - U Segments: `64`
   - V Segments: `12`

3. Add a Disk, rename it `bottom`, and enter:

   - Canvas Rect: `x=0.32, y=0.02, width=0.36, height=0.36`
   - Physical Size: `x=0.10, y=0.10`
   - Radial Segments: `64`
   - Radial Rings: `8`

4. In `Seams`, create `wall-close`: endpoint A is `wall.uMin`, endpoint B is
   `wall.uMax`, mode is `Weld`, and Sample Count is `13`.
5. Create `wall-bottom`: endpoint A is `wall.vMin`, endpoint B is
   `bottom.perimeter`, mode is `Weld`, and Sample Count is `64`.
6. In `Operations`, keep this exact order:

   1. `Roll` targeting `wall`: Direction `U`, Angle `360`, Radius Mode
      `PreserveArcLength`, Start Angle `180`.
   2. `RigidTransform` targeting `bottom`: Translation `(0,-0.06,0)`, Rotation
      Euler `(90,0,0)`, Scale `(1,1,1)`.
   3. `Stitch` selecting both `wall-close` and `wall-bottom`.
   4. `Solidify` selecting both `wall` and `bottom`: Thickness `0.004`,
      Direction `Inward`.

   Stitch is terminal for every panel selected by its seams until shared
   topology deformation propagation exists. Do not place a later per-panel
   Roll, Fold, RigidTransform, or SphericalWrap after this Stitch.
7. Click `Compile Now`. The current proof is valid only when Diagnostics has
   no errors and the closed-volume report has one component, zero open edges,
   zero non-manifold edges, and non-zero volume.
8. Inspect the exterior and interior with the one-sided solid view. Enable
   Wire, Panels, Seam, Normals, and Thickness separately. No background may be
   visible through the wall/bottom joint.
9. If using the production canvas, confirm the wall and bottom appearance meet
   without dark atlas pixels. The old decorated M03 canvas is not geometric
   seam evidence.
10. In `Bake`, select a folder under `Assets` and click
    `Bake Current Valid Result`. Change the source to an invalid physical size
    and confirm that Bake is disabled/refused and the existing baked asset is
    unchanged; then Undo the edit.

## 中文速览

M06 工作区把权威二维源放在左侧，把可随时销毁重建的三维结果放在右侧。面板、
接缝和操作列表支持 Undo/Redo；编辑后会防抖编译，`Compile Now` 可立即编译。
诊断中的 `Focus Source` 会定位到对应面板、操作或接缝。只有编译成功时才能显式
Bake，失败结果不会覆盖上一次有效 Mesh。上面的杯子步骤完全通过二维区域和几何
规则完成，不直接编辑顶点，保持“二维源资产 + 几何规则 = 三维结果”的项目原则。
