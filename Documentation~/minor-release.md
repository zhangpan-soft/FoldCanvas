# FoldCanvas 1.1.0 minor release

FoldCanvas `1.1.0` is the first post-stable compiler feature release. It adds
the M24 deterministic topology split for compatible straight off-grid
rectangle Fold creases while preserving FoldScript `0.1`, normalized public
Runtime API shape, dependencies, source UV authority, and the exact Unity
`6000.3.20f1` support row.

## Why a minor version

Public `v1.0.1` is immutable. M24 changes deterministic package bytes and turns
one previously rejected valid Fold domain into a successful compile. That is a
backward-compatible compiler capability, so semantic versioning requires
`1.1.0`; reusing `1.0.1` would make one version name two behaviors.

## Qualified behavior

- straight perimeter-to-perimeter off-grid creases on rectangle panels are
  refined in normalized source space before tessellation;
- source UVs, named boundaries, winding, provenance, operation order, and
  contiguous panel ranges remain deterministic;
- disk, curved, branched, interior-ending, and overlap cases still fail with
  stable `FC3011` and no Mesh;
- the other five maintained production-corpus cases retain their exact
  geometry and topology evidence.

## Install and verify

Use only the four assets attached to exact GitHub release `v1.1.0`. Verify the
`.sha256` file and file manifest before installing the `.tgz`. The public
qualification workflow installs that exact archive in two independent clean
Unity consumers and retains their XML, Editor logs, package resolution, and
geometry comparison evidence.

## Upgrade and rollback

Keep the 2D canvas and FoldScript as authoritative inputs. Upgrade the package,
then recompile; do not migrate Mesh, OBJ, Prefab, Material, receipt, report, or
screenshot files as source. The qualification workflow rehearses this path
from immutable `v1.0.1` with the maintained production cup.

Rollback uses exact public `v1.0.1`, archive SHA-256
`4188d23b18b924f6642f9e4eabbc15500fb60fb3d3916f23857a5a19966e1de5`.
Reinstall it and rebuild from the same 2D source. Assets requiring M24 off-grid
crease refinement will correctly return the older stable diagnostic there.

The machine-readable contract is
[`m25-minor-release.json`](m25-minor-release.json). This release does not
publish to a registry or external marketplace and does not add later geometry
milestones.

## 中文摘要

`1.1.0` 将 M24 的离网格直线折痕拓扑切分作为向后兼容的新编译能力发布。
公开 `v1.0.1` 的字节保持不变；二维画布和 FoldScript `0.1` 仍是源，Mesh 仍
是派生结果。升级时只保留二维源并重新编译；需要回滚时安装上述固定哈希的
`v1.0.1`。
