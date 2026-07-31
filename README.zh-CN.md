# FoldCanvas

> **暂定项目名，Pre-alpha。** 一个面向 Unity 的“二维优先、确定性编译”三维曲面引擎。

## 为什么要做这个引擎

这个项目来自“只用 AI 创建 3D 游戏”时遇到的一个直接瓶颈：AI 已经很擅长
二维原画、纹理、贴花和图案设计，但直接生成的三维模型仍经常出现拓扑混乱、
UV 不可控、比例漂移、厚度错误、接缝不闭合、难以修改，以及相同提示无法
稳定复现等问题。外观看起来像一个物体，并不等于它已经是可进入游戏生产
管线的资产。

FoldCanvas 的思路是把 AI 擅长和不擅长的部分拆开：

- AI 或人类负责二维画面，以及可读、可校验的结构化构造意图；
- 确定性几何编译器负责顶点、拓扑、UV、接缝、厚度和诊断；
- 同一份二维源与配置可以反复生成相同的三维结果，而不是每次重新“猜”模型。

因此项目要解决的不是“再做一个文生 3D 接口”，而是给 AI 游戏开发提供一种
可编程、可审查、可测试、可版本控制的 3D 资产源格式。

完整背景见[《为什么创建 FoldCanvas》](Documentation~/project-background.md)，
资产配置每个字段（包括 `roll`）的精确定义见
[《FoldScript JSON 字段参考》](Documentation~/foldscript-field-reference.md)。

FoldCanvas 不把 Mesh 当作源文件，而把它视为编译产物：

```text
三维资产 = 二维画卷 + 面板 + 接缝 + 折叠程序 + 厚度
```

用户在二维画布中完成外观、区域和结构描述，编译器再确定性生成 Unity `Mesh`、材质绑定、诊断信息，后续继续生成 Collider、LOD 和 Prefab。

这不是给现有文生 3D 套一层壳。核心几何不依赖任何 AI 服务。AI 未来只负责生成或修改可读、可校验的 FoldScript，而不是直接吐出无法维护的三角形团。

## 当前已经实现的内容

- 标准 Unity Package Manager 包结构
- 可序列化的二维源资产模型
- 经过输入校验的矩形与圆盘/椭圆面板离散化
- 原始二维坐标与画布 UV 全程保留
- 不可变的编译结果、面板归属、来源编号和有序边界
- 顶点/三角形生成上限，危险细分会在分配内存前报错
- 按顺序执行的刚体变换操作
- 按顺序执行的刚性折线 `Fold`，正负角度方向已有明确合同
- 折线会从二维源确定性映射到面板当前的三维表面
- 缺失目标、非法折线、非零 falloff、弯曲/含糊铰链都有稳定诊断码
- 在目标当前全等平面框架中执行 Circular Roll，正负手性与外法线已有明确合同
- Seam 声明只有被显式 Stitch 选择后才执行；Weld 后逻辑拓扑共享
  `TopologyVertexId`，UV/来源不同的渲染顶点仍可保留
- 不等采样边界按当前空间弧长确定性重采样，缺失采样点会真实拆分相邻三角形
- 可复用的 Weld 与 Bridge
- `inward`、`outward`、`centered` 三种 Solidify 厚度方向，焊接硬角共享
  miter，内壳反向绕序，并且只在真实开放边生成侧壁
- 一张含六个不同区域的二维画布可以编译成带图案盒体
- 杯壁侧缝和杯底圆周真实拓扑焊接；加厚后内角连续，杯口 rim 完整闭合
- 明确声明的矩形二维球瓣可通过 `SphericalWrap` 的半径、经纬范围、方向、
  极点与细分字段映射到球面
- 球面接缝插点会重新执行球面公式，极点拥有明确逻辑拓扑，最终用欧拉特征、
  边关联、绕序和半径误差验证真正闭合
- 只有球面到球面的接缝用于形成组件；任何一端命中该组件的后续 Stitch 都会推迟
  报告，组件只在最后一个触碰 Stitch 之后、相关 Solidify 之前独立生成报告
- 所选球面端点必须先执行 SphericalWrap、再执行 Stitch；非法顺序会在面板离散前
  返回 `FC2010`，不会生成提前或过期的球面报告
- 面板离散、Stitch 插点/Bridge 与 Solidify 内外壳/rim 共用同一份累计几何预算
- 一张包含 `NORTH`、`FOLDCANVAS`、`SOUTH`、赤道与球瓣编号的二维画布，
  可以在不调用 Unity 球体原语的情况下重建为闭合球体
- 稳定的编译诊断与确定性验证
- Unity Editor Mesh 烘焙工具
- UI Toolkit 二维源画布 / 三维派生预览分栏工作区
- 矩形与圆盘创建、画布区域拖拽、命名边界与接缝配对、显式操作表单、
  结构化诊断定位、Undo/Redo、防抖编译、调试叠加和仅限有效结果的 Bake
- Edit Mode 测试
- 完整架构、FoldScript 规格、路线图与 Codex 分阶段提示词

首个公开证明目标是：一张二维图片同时包含杯底、杯壁、文字与 Logo，经过 FoldScript 编译后得到闭合、有厚度、图案正确的三维杯子。

M04 保留 M03 Circular Roll 的一圈限制与全等平面框架合同，并已实现通用
边界重采样、Weld、Bridge、内外壳、硬角 miter 与开放边 rim。重采样不会
生成悬空点，而会拆分相邻源三角形并插值二维坐标和 UV。Stitch 之后，在共享
拓扑变形传播实现前，不能再单独移动其已选择面板；Solidify 可以在 Stitch
之后消费完整焊接拓扑。

M05 不是新增一个“球体生成器”。八个矩形球瓣先从二维源域确定性离散，再在
各自当前全等平面框架中执行 `SphericalWrap`，最后只依据显式 Seam Graph 和
终端 `Stitch` 焊接。接缝新增采样点会重新落在球面公式上，而不是停留在球内
弦线上；最终必须满足单一连通分量、零开放边、零非流形边、向外绕序、欧拉
示性数 2，以及南北极各一个逻辑拓扑点。
这份零厚度报告会在组件最后一个触碰 Stitch 后、任何命中该组件的 Solidify
之前固定下来。球面到普通面板的 Bridge/Weld 也属于触碰操作，不能保留更早的
旧报告。M05 当前不执行全局 triangle-triangle 自相交检测，因此闭球报告不
等于对任意几何“绝无自交”的完整证明。

M06 让同一套源数据不再必须通过代码创建。执行
`Tools > FoldCanvas > Open Authoring Workspace`，即可在左侧编辑二维面板和
接缝，在右侧查看由编译器生成的三维结果，并在下方编辑操作、查看诊断和显式
Bake。预览对象只是 Editor 内可销毁的派生产物，不会取代源资产。完整步骤见
[《M06 工作区：从空白源到闭合杯子》](Documentation~/authoring-workspace.md)。

## 七条项目宪法

1. **二维源文件拥有最高权威。** Mesh 可以删除并重新生成。
2. **编译必须确定。** 同样的源、设置和版本必须产生相同顶点顺序与拓扑。
3. **外观天然附着。** 顶点从出生起就保留画卷 UV。
4. **AI 输出意图，不输出三角形。**
5. **错误也是产品。** 接缝、翻面、自交和不支持操作必须给出可行动诊断。
6. **先用一张图和 Unlit。** PBR 以后作为可选派生层加入。
7. **Unity 是第一个宿主，不是理论边界。** 核心表示应保留跨引擎可能性。

## 数学边界

项目不声称所有曲面都能保持长度、角度和面积不变地铺平。这里说的“无损”是信息可逆，二维源可以同时保存切缝、伸缩、曲率、拓扑与厚度。详见 [`Documentation~/geometry-model.md`](Documentation~/geometry-model.md)。

## 环境

- Unity **6.3 LTS**
- 项目基线版本：`6000.3.20f1`
- 核心包不绑定 URP/HDRP
- 测试使用 Unity Test Framework

## 开始开发

1. 克隆仓库。
2. 用 Unity Hub 打开 `Project~`。
3. Unity 会通过 `file:../../` 引用仓库根目录的本地包。
4. 在 Test Runner 中运行 Edit Mode 测试。
5. 打开 `Tools > FoldCanvas > Open Authoring Workspace`。
6. 执行 `Tools > FoldCanvas > Create Bootstrap Sample` 查看 M01 平面样例；
   执行 `Tools > FoldCanvas > Create M02 Box Proof` 可直接创建、烘焙并显示
   六面折叠盒体；执行 `Tools > FoldCanvas > Create M03 Cup Proof` 可保留
   零厚度演示样例；执行
   `Tools > FoldCanvas > Create M04 Production Cup Proof` 可创建纯色与
   防渗色纹理两种厚杯证明，以及外观、精确侧面、内部、底部四个独立相机。
   执行 `Tools > FoldCanvas > Create M04.1 Closed Volume Cup Proof` 可查看
   无贴图闭合体、逻辑线框、截面与内外硬角；执行
   `Tools > FoldCanvas > Create Sphere Proof` 可查看 M05 二维球瓣、纹理球、
   单面纯材质球、接缝/极点、UV 拉伸、半径误差与闭合报告。所有证明相机均由
   FoldCanvas 自己拥有，不会修改项目已有 MainCamera。

## 持续集成

GitHub Actions 包含两个独立检查：

- `repository-validation` 解析 JSON，检查程序集与 Runtime 边界、Schema 和
  C# 原生 `sampleCount` 上限一致性、文档链接及版本元数据；
- `unity-editmode-tests` 用 Unity `6000.3.20f1` 打开仓库自带的 `Project~`
  宿主，真实编译 Runtime、Editor、Tests 程序集并运行全部 Edit Mode 测试，
  成功或失败都会上传 NUnit XML 与 `Editor.log`。GameCI 运行中的文件先写入
  宿主项目根目录下的非导入区域，避免持续变化的日志通过仓库根 UPM 包触发
  Unity 重复导入；Unity 退出后才复制到 `artifacts/unity-editmode`。

Unity CI 使用 GameCI。Unity Personal 授权需要在仓库 Actions Secrets 中配置
`UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD`，许可证信息不会写入仓库。

## 交给 Codex

仓库根目录已经放入 `AGENTS.md`。Codex 会自动读取它。第一次执行时，直接复制 [`Codex/MASTER_PROMPT.md`](Codex/MASTER_PROMPT.md)，或者要求 Codex：

```text
按照 AGENTS.md 和 PLANS.md 执行 CURRENT_TASK.md，只完成当前里程碑。
```

不要让 Codex 一次吞掉完整路线图。每完成一个里程碑，就在 Unity 中编译、跑测试、检查生成结果，再推进下一阶段。

## 发布到 GitHub

仓库创建、首个提交、分支保护与项目改名注意事项见 [`Docs/github-setup.md`](Docs/github-setup.md)。`FoldCanvas` 当前是工作名，公开发布前应完成名称与商标检索。

## 许可证

Apache License 2.0，详见 [`LICENSE.md`](LICENSE.md)。
