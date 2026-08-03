# M10 extensibility and ecosystem

M10 makes FoldCanvas extensible without exposing the internal topology buffer
or replacing the deterministic compiler. The canvas, panels, seams, and ordered
operations remain source. Meshes, OBJ files, performance reports, gallery views,
and release archives remain derived artifacts.

## Explicit operation registration

Create a registry for one compile and register exact operation definition
types. There is no global registry, assembly scan, reflection discovery, scene
lookup, or registration-order execution:

```csharp
FoldCanvasOperationRegistry registry = new FoldCanvasOperationRegistry();
if (!registry.TryRegister(
        new MyWaveOperationExecutor(),
        out FoldCanvasDiagnostic registrationDiagnostic))
{
    Debug.LogError(registrationDiagnostic);
    return;
}

FoldCanvasCompileResult result =
    FoldCanvasCompiler.Compile(source, registry);
```

An executor supplies:

- a stable lowercase reverse-domain `OperationTypeId`;
- a concrete custom `FoldOperationDefinition` CLR type;
- `PositionOnly` mutation kind;
- deterministic preflight that resolves exactly one existing target panel;
- deterministic execution through `FoldCanvasOperationExecutionContext`.

The context exposes each current position plus read-only source position, source
UV, panel index, provenance ID, panel shape/size, and tolerance. The only write
method is `TrySetPosition`, which accepts finite positions for vertices in the
resolved panel. It cannot add/remove vertices or triangles, union topology,
change boundaries, consume geometry budget, or edit UV/provenance.

The compiler snapshots registry descriptors in ordinal type-ID order before
tessellation. Operations still execute in authored source order. Failed,
non-finite, or throwing execution restores all panel positions and returns no
Mesh. A custom position operation after a Stitch that selected its panel
returns `FC2010`, matching built-in deformations.

Omitting the registry for a custom native operation returns `FC9001`. This is
intentional: FoldScript `0.1` has no opaque extension operation payload and
continues to reject unknown operation types. Native custom assets require the
assembly that defines them.

See the complete compiling template under
[`Samples~/OperationExtension`](../Samples~/OperationExtension/README.md).

## Sample gallery

`Samples~/Gallery/gallery.json` uses format `foldcanvas-gallery`, version `1`.
Every entry has a stable ID, title, description, safe `Samples~/` source path,
minimum package version, ordered unique tags, and optional FoldScript,
appearance, and `Tools/FoldCanvas/` proof-menu paths. Runtime parsing is bounded
and strict; duplicate IDs, unsafe paths, malformed JSON, and unknown versions
return `FC9101`-`FC9104` in deterministic order.

Open `Tools > FoldCanvas > Open Sample Gallery` to view and run declared proof
commands. The gallery invokes only paths explicitly present in the validated
manifest.

## Deterministic OBJ export

`FoldCanvasObjExporter.Export` consumes immutable
`FoldCanvasCompiledData` and returns text without file I/O. It emits render
vertices, source UVs, and faces in compiled order, using one-based matching
position/UV indices, invariant round-trip floats, normalized zero, and `\n`
line endings. UV seam copies remain distinct. The exporter never rewrites the
source, compiled data, or Unity Mesh.

Editor integrations may use `FoldCanvasObjEditorExporter` for a normalized
`.obj` path under `Assets/`. OBJ is the first dependency-free optional exporter;
material graphs, glTF, FBX, USD, and scene export are not M10 features.

## Performance evidence

`Documentation~/m10-performance-baselines.json` tracks three maintained cases:

- `planar-grid-64x32`;
- `full-roll-64x8`;
- `registered-wave-48x24`.

Run `Tools > FoldCanvas > Run M10 Performance Baselines`. The Editor warms each
case, measures it repeatedly, checks expected vertex/triangle counts and stable
geometry SHA-256, then writes a derived report under
`Library/FoldCanvas/M10PerformanceReport.json`. Timing never participates in
geometry output, and tests use generous ceilings rather than a brittle exact
machine duration.

## Release automation

`python3 Scripts/build_release_package.py` builds a sorted, metadata-normalized
`com.foldcanvas.core-<version>.tgz` under a `package/` root and writes a SHA-256
sidecar. Only the approved UPM surface is included; `Project~`, `.git`, GitHub
configuration, local logs, generated meshes, credentials, and test results are
excluded. `Scripts/test_release_package.py` builds twice and requires identical
bytes.

The `Package release` GitHub Actions workflow uploads package/digest artifacts
on manual runs. It creates a GitHub release only for a pushed tag exactly equal
to `v` plus the package/runtime/changelog version. GitHub supplies the token;
no credential is stored in the repository.

## 中文摘要

M10 的扩展不是“把内部 Mesh 缓冲区交给插件随便改”。调用方必须为一次编译
显式创建 Registry；扩展只能选择一个现有面板并修改有限的顶点位置，不能修改
三角形、拓扑、边界、UV、来源编号或几何预算。失败、非法数值或异常都会回滚，
不会留下半成品 Mesh。

样例 Gallery、OBJ、性能报告和发布压缩包都是可重建的派生证据。FoldScript
`0.1` 仍拒绝未知操作；要让自定义操作跨项目以 JSON 移植，未来必须新增明确的
Codec 与 Schema 版本，不能在 M10 偷塞不透明字段。
