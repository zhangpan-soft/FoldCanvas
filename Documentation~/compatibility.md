# Compatibility and migration policy

FoldCanvas `1.0.1` is the compatible stable patch release candidate; public
`v1.0.0` remains the rollback until M23's separate release and public-consumer
gates publish and qualify the patch. Compatibility changes are explicit rather
than accidental. The checked-in public Runtime API manifest, canonical
FoldScript version, package version, Unity evidence, and migration notes are
separate contracts.

## Qualified matrix

| Surface | Current contract | Qualification |
|---|---|---|
| UPM package | `1.0.1` | deterministic `.tgz`, checksum, file manifest, and patch qualification evidence |
| FoldScript | `0.1` | bounded reader and canonical writer |
| Unity package minimum | `6000.3.20f1` | `unity: 6000.3` plus `unityRelease: 20f1` |
| Unity production evidence | `6000.3.20f1` | repository tests and two clean archive installs |
| Runtime assembly | `FoldCanvas.Runtime` | public signatures in `public-runtime-api.json` |

Only `6000.3.20f1` is currently release-qualified. Other Unity 6000.3 patch
versions may work, but they are not claimed as production evidence until CI
runs them. The package has no URP/HDRP, third-party, or network dependency.

Patch `1.0.1` carries forward stable `1.0.0`'s replayable Editor-only robustness, scale,
cancellation/retry, resource-envelope, hosted long-run, public-asset, and
source-upgrade evidence. It does not add or remove a Runtime geometry operation
or change FoldScript `0.1`.

## Public Runtime API

`Documentation~/public-runtime-api.json` is generated from the compiled
`FoldCanvas.Runtime` assembly. It records public types and declared public
members in ordinal order, including constants, constructors, generic
constraints, parameter modifiers, and optional values. Internal types and the
Editor assembly are excluded.

- Additive RC changes require review, a compatible version decision, a
  changelog entry, and regeneration
  through `Tools > FoldCanvas > Maintenance > Regenerate Public Runtime API
  Baseline`.
- A removed type, removed member, or changed signature requires an ADR,
  migration notes, and an explicit package-version decision.
- After `1.0.0`, semantic-version compatibility governs this same baseline;
  breaking Runtime API changes require a new major version.
- Editing the JSON by hand to make CI green is not an accepted update. The
  compiled assembly is the evidence source.

## FoldScript migration

FoldScript `0.1` remains the only executable interchange version. Readers
reject an unknown schema version instead of guessing. Canonical export retains
panel, seam, and operation order while normalizing object fields and numbers.

Before a future schema version is accepted, the release must provide:

1. an ADR describing changed semantics;
2. updated JSON Schema and field reference;
3. deterministic old/new fixtures and diagnostics;
4. migration notes and, where practical, an explicit converter;
5. evidence that old input is either preserved or rejected with a stable
   diagnostic.

Native M10 extension operations are not part of FoldScript `0.1`. A Unity
asset containing one requires its defining assembly and an explicit registry
for each compile. A future portable extension codec needs a separately
versioned schema; opaque payload fallback is not allowed.

## Production handoff v1

The M12 `.foldcanvas.zip` contract is versioned independently as
`com.foldcanvas.handoff` version `1`. A receiver accepts it only when package,
compiler, and FoldScript `0.1` versions exactly match the installed package. It
does not attempt forward or backward migration.

Portable v1 source is exactly one canonical FoldScript document and one PNG.
Native custom operations, multiple canvases, non-PNG appearance formats, Unity
GUID preservation, signing, and migration are unsupported. The receiver
regenerates project-local GUIDs and derived outputs; the logical asset ID plus
source, appearance, and evidence hashes provide portable identity. Any future
handoff version requires an ADR, schema, fixtures, deterministic converter or
stable rejection path, and producer/receiver evidence. See
[Production handoff](production-handoff.md).

## Upgrade procedure

For every package upgrade:

1. preserve the editable canvas, FoldScript, panels, seams, and operations;
2. install the new `.tgz` or immutable version tag in a clean branch;
3. recompile rather than carrying generated Meshes forward as source;
4. run the project tests at its chosen validation levels;
5. review new diagnostics and compare production-corpus hashes;
6. update custom native extensions against the reviewed API diff;
7. retain the previous archive and source commit as the rollback point.

Generated Mesh, OBJ, reports, hashes, and logs are derived artifacts. Their
changes are reviewed evidence, not migrations of the authoritative source.

## Release-candidate and semantic versions

`1.0.1` preserves the normalized stable public API and exact production-corpus
geometry. `1.0.0`, RC1, and RC2 are immutable prior releases; none is rebuilt
or relabeled. A stable patch keeps the public API and compatible behavior, a
minor version may add backward-compatible API, and a major version is required
for a removal, changed signature, or incompatible behavior.

The historical RC1 freeze lives in
[`m14-release-candidate.json`](m14-release-candidate.json); the current public
RC2 distribution and historical exit gates live in
[`m15-public-distribution.json`](m15-public-distribution.json). The current
stable contract and frozen M16 readiness evidence live in
[`m17-stable-release.json`](m17-stable-release.json). Installation and rollback
are documented in the [`stable release guide`](stable-release.md). The M23
patch contract and procedure live in
[`m23-patch-release.json`](m23-patch-release.json) and
[`patch-release.md`](patch-release.md).

## 中文摘要

FoldCanvas `1.0.1` 是兼容稳定补丁发布候选；在 M23 的发布和公开消费者门禁完成前，
`v1.0.0` 仍作为公开回退。项目不会把
破坏性变化藏在普通提交里。公共 Runtime API
由实际编译程序集生成基线；删除或修改签名必须有 ADR、迁移说明和明确版本决策。
FoldScript 目前只支持 `0.1`，未知版本会稳定拒绝，不能猜测解析。当前真正跑过完整
证据的 Unity 版本是 `6000.3.20f1`；`package.json` 同时声明 `6000.3` 和
`20f1`，没有跑过完整证据的其他补丁版本不在支持承诺内。

M12 交付包 v1 同样采用严格版本合同：接收方的 package、compiler 和 FoldScript
版本必须完全一致。跨项目只保留逻辑资产 ID 和内容哈希，Unity GUID 由接收项目重新
生成；Mesh、OBJ、材质、Prefab 和回执都是可重建派生产物。
