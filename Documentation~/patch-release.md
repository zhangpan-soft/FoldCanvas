# FoldCanvas 1.0.1 stable patch

FoldCanvas `1.0.1` packages the proof-first README and deterministic proof
gallery without changing compiler geometry. Its release contract requires the
exact public archive to pass two independent clean consumers and a source-first
upgrade from immutable `1.0.0` before the patch is reported as qualified.

## Qualified row

| Package | Unity Editor | FoldScript | Compatibility |
|---|---|---|---|
| `1.0.1` | `6000.3.20f1` | `0.1` | compatible stable patch over `1.0.0` |

The normalized Runtime API still has 808 signatures, and all six maintained
production-corpus geometry identities remain unchanged. Source UVs,
dependencies, topology behavior, and the source-authority rule remain exactly
the same: the 2D canvas plus FoldScript are source, while Mesh and preview
artifacts are derived.

## Install and verify

Use only the four assets attached to exact release `v1.0.1`. Verify the
`.tgz.sha256` line before installing the archive. Recompile authoritative
source after installation instead of carrying a generated Mesh forward.

## Rollback

Rollback uses immutable public release `v1.0.0`, whose archive SHA-256 is
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.
Install that exact archive and rebuild from the same canvas and FoldScript.

The machine-readable gate is
[`m23-patch-release.json`](m23-patch-release.json). Registry and external
marketplace publication remain outside M23.

## 中文摘要

`1.0.1` 是兼容的稳定补丁，只加入经过审计的 README/证明画廊，不改变几何编译
行为。公开版本必须验证四个精确资产、两个独立干净安装，以及从不可变 `1.0.0`
开始的源优先升级。升级和回退都继续以二维画布与 FoldScript 为源，不把 Mesh 当作源。
