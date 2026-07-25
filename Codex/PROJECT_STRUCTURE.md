# FoldCanvas 项目结构与职责

```text
FoldCanvas/
├── AGENTS.md
│   Codex 每次工作前自动读取的常驻规则，保持短小。
│
├── PLANS.md
│   多文件任务的执行计划格式。Codex 需维护 Docs/Plans/active-plan.md。
│
├── CURRENT_TASK.md
│   唯一当前里程碑开关。不要一次让 Codex 做完整路线图。
│
├── package.json
│   UPM 包清单。仓库根目录本身就是 com.foldcanvas.core。
│
├── Runtime/
│   ├── Data/
│   │   ├── FoldCanvasAsset.cs
│   │   ├── PanelDefinition.cs
│   │   ├── SeamDefinition.cs
│   │   ├── FoldOperationDefinition.cs
│   │   └── FoldCanvasCompileSettings.cs
│   │
│   ├── Compiler/
│   │   ├── FoldCanvasCompiler.cs
│   │   ├── FoldCanvasCompileResult.cs
│   │   ├── MeshBuildBuffer.cs
│   │   ├── PanelTessellator.cs
│   │   ├── RigidTransformExecutor.cs
│   │   └── FiniteMath.cs
│   │
│   ├── Diagnostics/
│   │   └── FoldCanvasDiagnostic.cs
│   │
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   │
│   └── FoldCanvas.Runtime.asmdef
│
├── Editor/
│   ├── FoldCanvasBaker.cs
│   ├── FoldCanvasSampleCreator.cs
│   ├── FoldCanvasWindow.cs
│   ├── FoldCanvasAssetEditor.cs
│   └── FoldCanvas.Editor.asmdef
│
├── Tests/Editor/
│   ├── PlanarPanelCompilerTests.cs
│   ├── SourceValidationTests.cs
│   └── FoldCanvas.Tests.Editor.asmdef
│
├── Samples~/BootstrapPanel/
│   ├── README.md
│   ├── gpt-cup-canvas.png
│   └── gpt-cup.future-example.foldcanvas.json
│
├── Documentation~/
│   ├── architecture.md
│   ├── geometry-model.md
│   ├── foldscript-spec.md
│   ├── compiler-pipeline.md
│   ├── diagnostics.md
│   ├── editor-workflow.md
│   ├── ai-integration.md
│   ├── roadmap.md
│   └── glossary.md
│
├── Schema/
│   ├── foldcanvas.schema.json
│   └── gpt-cup.example.foldcanvas.json
│
├── Docs/
│   ├── ADR/
│   ├── Plans/
│   ├── research-boundary.md
│   └── governance.md
│
├── Codex/
│   ├── MASTER_PROMPT.md
│   ├── PROJECT_STRUCTURE.md
│   ├── M00_BOOTSTRAP.md
│   ├── M01_PLANAR_PANELS.md
│   ├── M02_FOLD_BOX.md
│   ├── M03_ROLL_CUP.md
│   ├── M04_STITCH_SOLIDIFY.md
│   ├── M05_SPHERE_GORES.md
│   ├── M06_EDITOR_WORKSPACE.md
│   ├── M07_VALIDATOR.md
│   ├── M08_FOLDSCRIPT_AI.md
│   ├── M09_TOPOLOGY.md
│   ├── REVIEW_PROMPT.md
│   └── BUGFIX_PROMPT.md
│
├── Scripts/
│   └── validate_repository.py
│
├── Project~/
│   ├── Assets/
│   ├── Packages/manifest.json
│   └── ProjectSettings/ProjectVersion.txt
│
└── .github/
    ├── ISSUE_TEMPLATE/
    ├── workflows/repository-checks.yml
    └── pull_request_template.md
```

## 编译器长期分层

随着里程碑推进，`Runtime/Compiler` 应逐渐拆成：

```text
Compiler/
├── SourceValidation/
├── Tessellation/
├── Operations/
│   ├── Fold/
│   ├── Roll/
│   ├── SphericalWrap/
│   └── Solidify/
├── Boundaries/
├── Seams/
├── Topology/
├── DerivedAttributes/
├── Validation/
└── ArtifactConversion/
```

不要在 M00 就提前建立一堆空目录和空接口。目录随真实能力生长，每个抽象必须有至少两个实际调用点或明确的当前需求。

## 源数据与编译数据

必须保持两组对象分离：

```text
Unity Authoring Source
FoldCanvasAsset / FoldScript DTO

Compiler-owned Intermediate Representation
positions2D
positions3D
canvasUv
panel ownership
provenance
triangles
ordered boundaries
seam topology
```

不能让 `UnityEngine.Mesh` 成为操作之间传递的中间表示，否则边界、来源和二维信息会迅速丢失。

## 包拆分时机

M08 以前只维护：

```text
com.foldcanvas.core
```

AI 接入后再考虑：

```text
com.foldcanvas.ai.abstractions
com.foldcanvas.ai.openai
com.foldcanvas.ai.local
```

不要在没有真实 provider 实现前提前拆出空包。
