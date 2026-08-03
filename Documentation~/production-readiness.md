# Production-readiness evidence

M11 adds consumer evidence above repository-local correctness. It does not
claim that every future asset is valid; it makes release regressions visible
before ordinary Unity projects receive a package.

## Evidence ladder

A preview or release-candidate build passes these independent rungs:

1. repository JSON, assembly, architecture, documentation, and contract checks;
2. deterministic byte-identical UPM archive construction;
3. all package Edit Mode tests in the tracked Unity host;
4. two independent clean Unity projects resolving only the built `.tgz`;
5. consumer-owned code compiling through `FoldCanvas.Runtime` public API;
6. public API baseline and production-corpus comparison;
7. foreground rendering or interaction checks for any visual claim.

Passing a lower rung does not replace a higher one. In particular, a Python
check does not prove C# compilation, a Unity exit code without XML does not
prove tests ran, and a generated Mesh does not replace its 2D source.

## Clean archive proof

`Scripts/create_clean_install_project.py` creates a minimal host whose manifest
references one freshly built `.tgz`. The host owns its test assembly and
references only `FoldCanvas.Runtime`. It checks that Unity reports
`LocalTarball`, that the resolved package is under the host's
`Library/PackageCache`, and that no repository-root package fallback occurred.

The consumer creates an ordinary rectangle source, compiles it twice, exports
OBJ twice, and records package, source, geometry, OBJ, and diagnostic SHA-256
values. CI repeats the entire installation in a second host and
`compare_clean_install_evidence.py` requires identical stable evidence while
also requiring distinct PackageCache paths.

Each clean run must upload:

- real NUnit `test-results.xml`;
- the Unity `Editor.log` proving `6000.3.20f1` started;
- `consumer-report.json`;
- the generated `manifest.json` and `packages-lock.json`;
- a validation result. The pair also uploads one comparison result.

Missing, empty, skipped, failed, or inconclusive evidence fails the gate.

## Production corpus

`Documentation~/m11-production-corpus.json` stores source and result evidence,
not generated Mesh assets. Every case compiles twice before it is compared to
the reviewed baseline.

| Case | Validation | Production question |
|---|---|---|
| `cyclic-torus` | Strict | do two explicit cycles stay closed and non-self-intersecting? |
| `invalid-off-grid-fold` | Basic | does unsupported topology split fail as `FC3011` with no Mesh? |
| `planar-artwork` | Basic | do source coordinates, UVs, and deterministic tessellation remain stable? |
| `production-cup` | Strict | do welded wall/base, inner shell, rim, and closed volume remain connected? |
| `registered-wave` | Standard | does an explicit position-only native extension retain bounded topology? |
| `sphere-gores` | Standard | do explicit 2D gores retain a closed Euler-2 sphere? |

For successful cases the gate records render and logical vertex counts,
triangles, components, open/non-manifold edges, closed-volume state, source,
full compiled-data, OBJ, and diagnostic hashes. The invalid case records its
stable root diagnostic and empty geometry evidence. The derived CI report is
written under the host's `M11Evidence` directory and uploaded with package-test
artifacts.

## Public API gate

`Documentation~/public-runtime-api.json` is regenerated from the compiled
Runtime assembly, never from a source-text approximation. Repository checks
validate its order and digest; Edit Mode tests regenerate it in memory and
show added and removed signatures. See [compatibility and migration](compatibility.md).

## Native extension trust boundary

M10's registry restricts the mesh capability supplied by FoldCanvas: an
executor can replace finite positions on one selected panel and cannot mutate
topology, UV, provenance, triangles, boundaries, or the geometry budget through
that context. Failed execution rolls those positions back.

This is not an operating-system or managed-code security sandbox. The executor
is trusted in-process contributor code loaded into Unity and could independently
call APIs outside FoldCanvas. Do not load an untrusted extension assembly. Use
source review, ordinary dependency controls, and a separate process if hostile
code containment is required.

## Release blockers and issue triage

The following block a preview/RC release:

- a reproducible security, data-loss, source-corruption, compiler-correctness,
  determinism, topology, or installation defect;
- a missing or failed repository, full Unity, clean-install, API, corpus, or
  archive gate;
- an unexplained public API or corpus baseline change;
- a release archive whose version, changelog, runtime constant, or tag differs;
- a visual production claim that has not been checked in the foreground.

Public issues are triaged in this order: security, data loss/source corruption,
compiler correctness, determinism, topology, installation/upgrade, then
authoring usability and documentation. A minimized source asset, Unity version,
package version, diagnostics, and reproduction steps are requested before a
geometry fix. Credentials and private source assets must not be posted publicly.

External publication, marketplace submission, paid services, credentials,
legal decisions, and irreversible permission changes remain owner escalation
points even under autonomous maintenance.

## Local commands

Fast checks that do not claim Unity execution:

```text
python3 Scripts/validate_repository.py
python3 Scripts/test_release_package.py
python3 Scripts/test_clean_install_project.py
```

Use Unity `6000.3.20f1` without an immediate `-quit` when invoking
`-runTests`; the Test Framework exits after writing XML. Hosted CI remains the
release evidence of record because it runs the complete package suite and both
independent clean hosts.

## 中文摘要

M11 把“仓库里能跑”提升为“真正发布的 `.tgz` 能在两个全新 Unity 工程里安装、
编译、调用并产生完全一致的证据”。CI 必须上传真实 XML、Editor.log、包解析文件、
消费者报告和双安装对比；任何缺失都算失败。生产语料覆盖平面、厚杯、球瓣、环面、
原生扩展和一个预期失败案例。原生扩展是受信任的 Unity 进程内代码，受限 API 不是
安全沙箱。二维 Canvas、FoldScript 与几何规则仍是源，所有 Mesh 和报告仍是派生物。
