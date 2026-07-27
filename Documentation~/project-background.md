# 为什么创建 FoldCanvas / Why FoldCanvas exists

## 立项背景：当前要解决的生产问题

这个项目源于一次实际的 AI 游戏制作尝试：二维生成模型已经能够稳定帮助创作
原画、纹理、贴花、精灵和图案，但 AI 直接生成的三维网格还很难成为可靠的
游戏生产资产。一个结果即使轮廓看起来正确，也可能同时存在以下问题：

- 修改一句提示词就替换整个网格，无法只做一次受控修改；
- 相似输入之间的拓扑和顶点顺序完全变化；
- UV、图案位置、比例、接缝和厚度不可控；
- 结果难以编辑、比较、缓存、测试和复现；
- 错误没有稳定的字段与诊断，程序和 AI 都难以自动修复；
- 依赖不透明的模型生成接口，无法形成长期稳定的资产源合同。

FoldCanvas 从一个工程化观察出发：三维表面可以由一个或多个二维参数域，加上
明确的重建规则来表示。让 AI 在它已经擅长的二维画面与结构化意图上工作，
再由确定性几何编译器负责游戏管线必须控制的部分。

```text
三维资产 =
    二维外观画布
  + 面板参数域
  + 命名边界与接缝
  + 有序构造操作
  + 厚度与编译设置
```

二维源文件和 FoldScript 是可以修改、审查、测试和版本控制的正式资产；
Unity `Mesh` 是随时可以删除并重新生成的派生产物。

## 为什么是引擎，而不是另一个模型生成器

FoldCanvas 不根据文字或图片猜一个最终三角网格。它定义一门小型几何语言和
具有可测合同的编译器：

- 相同输入产生相同的顶点、UV、三角形、边界与诊断顺序；
- 每个生成顶点保留二维源位置、画布 UV、所属面板与来源编号；
- 只有显式接缝和操作才能改变拓扑；
- 不支持或不安全的输入会停止并返回诊断，不会悄悄换一种结果；
- AI 可以根据稳定错误码修改源字段，形成可验证的修复循环。

所以这套格式不只服务 AI，也能服务程序化工具、构建服务器、资产生成器、
自动测试和人工制作管线。

## 为什么先在 Unity 中实现

Unity 提供了一个能立刻进入真实游戏项目验证的首个宿主：可序列化源资产、
Editor 烘焙与视觉检查、Edit Mode 确定性测试，以及标准的 `Mesh`、材质、
Prefab 和 Collider 输出。

Unity 是第一个宿主，不是数学边界。FoldScript 与编译器语义会尽量保持可移植，
让未来的非 Unity 实现也能遵循同一份合同。

## 诚实的数学边界

FoldCanvas 不声称任意曲面都能在保持长度、角度和面积的同时无失真展开。
一般曲面可能需要切口、多个面板、度量形变、曲率说明和显式拓扑。这里追求的
是信息可保留的二维表示，不是“所有物体都能等距摊成一张纸”。

早期里程碑会先验证矩形、圆盘/椭圆、盒子、卷曲侧壁、杯子与球面分片，再处理
更复杂的拓扑。

---

## The production problem

Generative image systems are already useful for concept art, textures, decals,
sprites, and other two-dimensional work. Generative 3D systems are much less
reliable for game production: the visible silhouette may look plausible while
topology, UVs, scale, seams, thickness, editability, and repeatability remain
uncontrolled.

That gap creates a practical problem for an AI-assisted game pipeline:

- a prompt revision can replace the whole mesh instead of making one controlled
  change;
- topology and vertex order can change between otherwise similar generations;
- artwork may not remain attached to the intended surface;
- failures are difficult to diagnose or repair programmatically;
- generated assets are hard to diff, cache, test, or reproduce;
- opaque model-generation APIs cannot provide a stable source contract.

FoldCanvas starts from the observation that a surface can be represented by one
or more two-dimensional parameter domains plus explicit reconstruction
instructions. The AI can work where it is strongest—creating the 2D appearance
and proposing structured intent—while a deterministic geometry compiler handles
the parts a game pipeline must control.

```text
3D Asset =
    2D Appearance Canvas
  + Panel Domains
  + Named Boundaries and Seams
  + Ordered Construction Operations
  + Thickness and Compile Settings
```

The FoldCanvas source is editable and reviewable. The Unity `Mesh` is a derived
artifact that can be deleted and rebuilt.

## Why an engine instead of another model generator

FoldCanvas is not intended to guess a finished triangle mesh from text or an
image. It defines a small geometry language and a compiler with measurable
contracts:

- identical inputs produce identical vertex, UV, triangle, boundary, and
  diagnostic ordering;
- each generated vertex retains its 2D source position, canvas UV, panel
  ownership, and provenance;
- topology is created only by explicit seams and operations;
- unsupported or unsafe input stops with diagnostics instead of silently
  producing a different object;
- an AI repair loop can change source fields in response to stable error codes.

This makes the format useful not only for AI, but also for procedural tools,
build servers, asset generators, tests, and human-authored pipelines.

## Why Unity is the first host

Unity provides a practical first environment for:

- serializable source assets;
- Editor baking and visual inspection;
- deterministic Edit Mode tests;
- standard `Mesh`, material, prefab, and collider outputs;
- immediate use inside an actual game project.

Unity is the first host, not the mathematical boundary. The source format and
compiler concepts are designed so future non-Unity implementations can follow
the same semantics.

## Honest mathematical scope

FoldCanvas does not claim that every curved surface can be flattened without
distortion. General surfaces may require cuts, multiple panels, metric
distortion, curvature instructions, and explicit topology. The project targets
an information-preserving 2D representation, not universal isometric
flattening.

The early milestones intentionally begin with rectangles, disks/ellipses,
boxes, rolled walls, cups, and sphere gores before attempting more complicated
topology.
