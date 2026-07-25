# FoldCanvas

> **暂定项目名，Pre-alpha。** 一个面向 Unity 的“二维优先、确定性编译”三维曲面引擎。

FoldCanvas 不把 Mesh 当作源文件，而把它视为编译产物：

```text
三维资产 = 二维画卷 + 面板 + 接缝 + 折叠程序 + 厚度
```

用户在二维画布中完成外观、区域和结构描述，编译器再确定性生成 Unity `Mesh`、材质绑定、诊断信息，后续继续生成 Collider、LOD 和 Prefab。

这不是给现有文生 3D 套一层壳。核心几何不依赖任何 AI 服务。AI 未来只负责生成或修改可读、可校验的 FoldScript，而不是直接吐出无法维护的三角形团。

## 当前已经放入仓库的内容

- 标准 Unity Package Manager 包结构
- 可序列化的二维源资产模型
- 矩形与圆盘面板离散化
- 原始画布 UV 全程保留
- 按顺序执行的刚体变换操作
- 编译诊断与验证基础
- Unity Editor Mesh 烘焙工具
- Edit Mode 测试
- 完整架构、FoldScript 规格、路线图与 Codex 分阶段提示词

首个公开证明目标是：一张二维图片同时包含杯底、杯壁、文字与 Logo，经过 FoldScript 编译后得到闭合、有厚度、图案正确的三维杯子。

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
5. 打开 `Window > FoldCanvas > FoldCanvas`。
6. 执行 `Tools > FoldCanvas > Create Bootstrap Sample`。

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
