# Compatibility and migration policy

FoldCanvas is still a preview package, but compatibility changes are explicit
rather than accidental. The checked-in public Runtime API manifest, canonical
FoldScript version, package version, Unity evidence, and migration notes are
separate contracts.

## Qualified matrix

| Surface | Current contract | Qualification |
|---|---|---|
| UPM package | `0.1.0-preview.19` | deterministic `.tgz` archive |
| FoldScript | `0.1` | bounded reader and canonical writer |
| Unity package minimum | `6000.3` | declared by `package.json` |
| Unity production evidence | `6000.3.20f1` | repository tests and two clean archive installs |
| Runtime assembly | `FoldCanvas.Runtime` | public signatures in `public-runtime-api.json` |

Only `6000.3.20f1` is currently release-qualified. Other Unity 6000.3 patch
versions may work, but they are not claimed as production evidence until CI
runs them. The package has no URP/HDRP, third-party, or network dependency.

## Public Runtime API

`Documentation~/public-runtime-api.json` is generated from the compiled
`FoldCanvas.Runtime` assembly. It records public types and declared public
members in ordinal order, including constants, constructors, generic
constraints, parameter modifiers, and optional values. Internal types and the
Editor assembly are excluded.

- Additive preview changes require review, a changelog entry, and regeneration
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

## 中文摘要

FoldCanvas 仍处于预览期，但不会把破坏性变化藏在普通提交里。公共 Runtime API
由实际编译程序集生成基线；删除或修改签名必须有 ADR、迁移说明和明确版本决策。
FoldScript 目前只支持 `0.1`，未知版本会稳定拒绝，不能猜测解析。当前真正跑过完整
证据的 Unity 版本是 `6000.3.20f1`；`package.json` 中的 `6000.3` 是最低声明，
不等于每个补丁版本都已被生产验收。
