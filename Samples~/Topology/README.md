# M09 non-trivial topology

This sample guide creates two authoritative 2D FoldCanvas sources and their
disposable Unity proof objects. After importing the package, run:

`Tools > FoldCanvas > Create M09 Topology Proof`

The command creates:

- a rectangular parameter panel mapped by `ToroidalWrap`; its U and V cycles
  remain topologically open until two declared Weld seams are selected by
  Stitch;
- a cup whose handle is an ordinary three-cell rectangle strip, positioned by
  `RigidTransform`, bent by two edge-aligned `Fold` operations, and welded to
  two explicit spans on the cup's top boundary before one final `Solidify`;
- textured, one-sided solid-color, and logical-wireframe derived views under
  one owned `EditorOnly` preview root.

The command writes reproducible source assets and a generated proof canvas into
the host project's `Assets/FoldCanvasSamples/M09Topology` folder. Those host
files are generated examples, not package source and not hidden mesh templates.
The editable authority remains the appearance canvas, panels, boundary spans,
seams, and ordered operation list. Every Mesh can be deleted and recompiled.

No Unity torus primitive, imported handle Mesh, Boolean/CSG solver, proximity
weld, bevel, subdivision, remesher, or generated-Mesh edit is used.

## 中文说明

导入包后执行 `Tools > FoldCanvas > Create M09 Topology Proof`。该命令会从
二维矩形参数面板与显式 Weld 接缝生成环面，并把一个普通矩形条经过刚体定位、
两次网格对齐 Fold、两个杯口边界区间 Weld 和最终 Solidify 生成带把手的闭合
杯体。纹理、纯色单面材质和逻辑线框都只是派生证明；二维画布、面板、边界区间、
Seam 与有序操作才是可编辑源数据。
