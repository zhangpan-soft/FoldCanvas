# FoldCanvas 1.0.0 stable release

FoldCanvas `1.0.0` is the first stable Unity Package Manager release. It is the
qualified continuation of immutable `1.0.0-rc.2`: the package version and
release evidence advance, while geometry, topology, FoldScript `0.1`, source
UVs, dependencies, and the qualified Unity row remain unchanged.

## Qualified row

| Package | Unity Editor | FoldScript | Status |
|---|---|---|---|
| `1.0.0` | `6000.3.20f1` | `0.1` | stable, evidence-qualified |

Other Unity 6000.3 patches may work but are not part of the complete release
evidence. The package remains render-pipeline neutral and has no third-party or
network dependency.

## Install

Download the four assets from the exact `v1.0.0` GitHub release and verify the
`.tgz.sha256` file before installation. Add the archive to a project manifest:

```json
{
  "dependencies": {
    "com.foldcanvas.core": "file:../Packages/com.foldcanvas.core-1.0.0.tgz"
  }
}
```

The editable source remains the appearance canvas plus FoldScript. Recompile
that source after installation; do not migrate a generated Mesh as authority.

## Why stable is trustworthy

The exact RC2 lineage completed 172.5 soak hours, two genuine scheduled Unity
runs, 14/14 reviewed evidence gates, and zero release blockers. Stable
publication additionally requires:

- a deterministic `1.0.0` archive, manifest, checksum, and stable evidence;
- compiled Runtime API shape equal to RC2 after normalizing the version literal;
- unchanged cup, sphere, torus, planar, extension, and invalid-fold corpus;
- complete hosted Edit Mode, clean-install, handoff, upgrade, robustness, and
  resource evidence;
- exact-head audit and protected-main merge;
- verification of the actual public stable assets in two clean consumers;
- a source-first upgrade from immutable RC2.

The machine-readable contract is
[`m17-stable-release.json`](m17-stable-release.json). The exact readiness report
copied from hosted evidence is
[`m17-stable-readiness-report.json`](m17-stable-readiness-report.json).

## Upgrade and rollback

Preserve the 2D canvas and canonical FoldScript, install `1.0.0`, and recompile.
Generated Mesh, OBJ, Material, Prefab, receipt, report, and screenshot files are
derived and are not upgrade inputs.

Rollback uses immutable `v1.0.0-rc.2`, archive SHA-256
`72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707`.
Reinstall that archive and rebuild from the same authoritative source.

## Compatibility after 1.0

- patch releases preserve compatible Runtime API and source behavior;
- minor releases may add backward-compatible API or operations;
- removals, changed signatures, or incompatible semantics require a major
  version, an ADR, migration notes, and explicit evidence;
- unknown FoldScript versions remain stable errors rather than guessed input.

## 中文摘要

FoldCanvas `1.0.0` 是首个稳定版。它来自已经完成 172.5 小时浸泡、两次真实定时
Unity 验证、14/14 门禁且没有发布阻断项的 `1.0.0-rc.2`。本次只推进版本和发布证据，
不改变几何、拓扑、FoldScript `0.1`、UV、依赖或 Unity 验证版本。

安装时下载 `v1.0.0` 的 `.tgz` 并先核对 SHA-256。升级时只保留二维原画与
FoldScript，再重新编译；Mesh、OBJ、材质、Prefab 和报告都不是源。需要回退时使用
不可变的 `v1.0.0-rc.2` 公共归档。
