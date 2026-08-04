# M14 release-candidate guide

FoldCanvas `1.0.0-rc.1` freezes the accepted M00-M13 compiler behavior for
release qualification. It does not add a geometry operation and it is not the
final `1.0.0` decision.

## Exact support row

| Package | Unity Editor | FoldScript | Status |
|---|---|---|---|
| `1.0.0-rc.1` | `6000.3.20f1` | `0.1` | required M14 qualification row |

`package.json` declares `unity: 6000.3` and `unityRelease: 20f1`, so the
package minimum matches the patch actually used for acceptance. No other Unity
patch is claimed until it produces the complete hosted evidence set.

## Install the candidate

Use the immutable `.tgz` created by the Package release workflow. In a target
project's `Packages/manifest.json`, add a relative or absolute `file:` reference
to that archive:

```json
{
  "dependencies": {
    "com.foldcanvas.core": "file:../Packages/com.foldcanvas.core-1.0.0-rc.1.tgz"
  }
}
```

Keep the archive checksum and compare it before installation. A repository
checkout through `file:` is convenient for development but is not equivalent
to the qualified archive.

## First source-driven proof

1. Import the Bootstrap Panel sample or open the FoldCanvas authoring workspace.
2. Keep the appearance PNG and FoldScript as the editable source.
3. Compile with the desired Basic, Standard, or Strict validation level.
4. Treat diagnostics and validation reports as acceptance evidence.
5. Bake only a successful result; delete and rebuild derived Meshes whenever
   the source or package changes.

## Candidate evidence

Every bundle contains or accompanies:

- deterministic UPM archive and SHA-256 file;
- sorted per-file size/SHA-256 manifest;
- release-candidate evidence tying version, Unity row, FoldScript, API,
  production corpus, archive, and rollback contracts together;
- Apache-2.0 license, notice, security, support, contributing, conduct,
  changelog, schema, samples, and package documentation.

The pull-request workflow builds these files without publishing a release.
An RC tag must exactly equal the package version and is published as a GitHub
pre-release only after the exact-head audit and required green checks.

## Upgrade and rollback

Before upgrading, preserve the 2D canvas, canonical FoldScript, prior `.tgz`,
and content hashes. Install the candidate in a clean branch or project, compile
the production sources, compare diagnostics/corpus evidence, and only then
adopt it.

The M14 rollback target is `0.1.0-preview.21` at merge `d9434be`. Reinstall its
archive and recompile authoritative source. Generated Mesh, OBJ, report,
Material, and Prefab files are not rollback inputs.

## Troubleshooting

- **Package is hidden or incompatible:** confirm the Editor is exactly
  `6000.3.20f1` and inspect `Packages/packages-lock.json`.
- **A clean project works but an existing project fails:** compare package
  resolution, asmdef references, custom operation registries, and source paths.
- **Geometry differs:** compare package/compiler/FoldScript versions, ordered
  operations, validation level, source and appearance hashes, and diagnostics.
- **Only a screenshot is available:** recover or minimize the source before
  treating it as a compiler defect.
- **Security concern:** do not publish exploit details; use private
  vulnerability reporting described in `SECURITY.md`.

See [compatibility and migration](compatibility.md),
[production readiness](production-readiness.md), and the repository-root
`SUPPORT.md` for the complete policy.
