# Production handoff

M12 transfers a complete FoldCanvas production asset between Unity projects
without turning a generated Mesh into editable source. The receiver owns the
same canonical 2D canvas and FoldScript, recompiles them locally, and can ship
the generated Prefab as an ordinary runtime asset.

```text
authoritative source                 receiver-owned derived outputs
source.foldcanvas.json ─┐
                       ├─ verify + compile ─> Mesh + Material + Prefab
appearance.png ─────────┘                    + evidence + receipt
```

The OBJ, Mesh, Material, Prefab, compile report, and receipt are evidence or
outputs. Editing them does not change the FoldCanvas source, and Rebuild never
reads their topology as compiler input.

## Editor workflow

### Export

1. Select a valid `FoldCanvasAsset` whose appearance is a PNG under `Assets/`
   or an installed package.
2. Use `Tools > FoldCanvas > Handoff > Export Selected Source...`, or open the
   Authoring workspace Bake tab and choose `Export Source-First Handoff...`.
3. Save the result with the `.foldcanvas.zip` suffix.
4. Record the SHA-256 shown by FoldCanvas beside the archive.

Export compiles without mutating the source, texture importer, selection, or
scene. A failed export does not replace an existing valid destination file.

### Import

1. Use `Tools > FoldCanvas > Handoff > Import Archive...`.
2. Select the archive and an existing parent folder under `Assets/`.
3. FoldCanvas derives one new child folder name from the archive name.
4. Review the created editable source and the generated runtime Prefab.

The destination child must not already contain unrelated or changed data. An
exact archive imported again into its intact receipt-owned folder succeeds
without rewriting any asset. Use a different destination for a changed archive.

A successful import creates exactly these receiver-owned files:

```text
source.foldcanvas.json       canonical authoritative construction source
appearance.png               exact authoritative 2D artwork
FoldCanvasSource.asset       editable Unity representation of the source
GeneratedMesh.asset          derived render Mesh
Appearance.mat               derived one-sided textured Material
Runtime.prefab               derived runtime-ready Prefab
handoff-receipt.json         ownership and reproducibility receipt
```

### Rebuild

Select the imported folder or one of its assets, then use
`Tools > FoldCanvas > Handoff > Rebuild Selected Import`. Rebuild verifies that
the source JSON, PNG, editable asset, and receipt still agree, recompiles the
source, and recreates only the receipt-owned Mesh, Material, and Prefab. Source
drift returns a diagnostic instead of silently accepting a different result.

## Archive v1 layout

Version 1 is one ZIP with exactly six stored entries in this ordinal order:

| Entry | Role | Authority |
|---|---|---|
| `manifest.json` | format, compatibility, texture, and payload contract | metadata |
| `source.foldcanvas.json` | canonical FoldScript `0.1` | authoritative |
| `appearance.png` | exact PNG canvas bytes | authoritative |
| `derived/model.obj` | deterministic human/DCC review surface | derived evidence |
| `evidence/compile-report.json` | reproducibility and validation hashes/counts | derived evidence |
| `README.md` | receiver ownership and rebuild instructions | documentation |

Entries are UTF-8/LF where textual, uncompressed, timestamped at
`1980-01-01T00:00:00`, and carry no directory, link, comment, extra-field, or
encryption metadata. The archive itself is not executable content.

## Manifest field reference

| Field | Meaning |
|---|---|
| `format` | Exact identifier `com.foldcanvas.handoff`. |
| `version` | Archive contract version; v1 is the string `1`. |
| `packageVersion` | Exact FoldCanvas UPM package version used by the producer. |
| `compilerVersion` | Exact deterministic compiler version used for evidence. |
| `foldScriptVersion` | Exact source-language version; v1 requires `0.1`. |
| `asset.id` | Stable logical source identity, independent of Unity GUIDs. |
| `asset.displayName` | Human-readable asset name. |
| `canvas.width`, `canvas.height` | Required decoded PNG dimensions in pixels. |
| `texture.filterMode` | `Point`, `Bilinear`, or `Trilinear`. |
| `texture.wrapModeU`, `texture.wrapModeV` | `Repeat`, `Clamp`, `Mirror`, or `MirrorOnce`. |
| `texture.mipmapEnabled` | Whether the receiver enables texture mipmaps. |
| `texture.sRgbTexture` | Whether PNG color bytes are interpreted as sRGB. |
| `texture.alphaIsTransparency` | Unity alpha-transparency importer setting. |
| `texture.anisoLevel` | Receiver anisotropic filtering level, from 0 through 16. |
| `texture.pixelsPerUnit` | Positive Unity sprite pixels-per-unit importer value. |
| `payloads[].path` | Exact allowlisted payload path and ordinal position. |
| `payloads[].role` | Exact role: `source`, `appearance`, `derived-obj`, `compile-evidence`, or `instructions`. |
| `payloads[].byteLength` | Exact payload byte count before any read is trusted. |
| `payloads[].sha256` | Lowercase SHA-256 of the exact payload bytes. |

`manifest.json` describes five payloads because the manifest does not hash
itself. The exporter returns the complete archive SHA-256 separately.

## Compile evidence field reference

`evidence/compile-report.json` uses format
`com.foldcanvas.handoff.compile-report`, version `1`.

| Field group | Meaning |
|---|---|
| `assetId`, package/compiler/FoldScript versions | Identity and exact compatibility context. |
| `sourceSha256`, `appearanceSha256` | Hashes of the two authoritative inputs. |
| `geometrySha256` | Complete ordered compiled positions, UVs, provenance, topology, boundaries, corners, and spherical metadata. |
| `objSha256` | Deterministic derived OBJ text hash. |
| `diagnosticSha256` | Ordered diagnostic codes, messages, values, context, and suggestions. |
| `validationSha256` | Complete final geometry validation report hash. |
| `closedVolumeSha256` | Final and operation-stage closed-volume evidence hash. |
| `sphereReportsSha256` | Ordered spherical-component report evidence hash. |
| `validationLevel` | Source-requested `Basic`, `Standard`, or `Strict` level. |
| render/topology/triangle counts | Generated surface size, with triangle count measured in faces. |
| diagnostic/error/warning counts | Deterministic compiler and validation totals. |
| open/non-manifold/component counts | Final logical-topology summary. |
| `validationIsValid` | Whether the final validator accepted the generated buffer. |
| `isClosedVolume` | Whether final closed-volume evidence reports closure. |
| `isSingleClosedVolume` | Whether the result is exactly one closed component. |
| `sphereReportCount` | Number of retained component-scoped sphere reports. |

The receiver recomputes every field. Counts alone never substitute for full
geometry and report hashes.

## Receipt field reference

`handoff-receipt.json` uses format `com.foldcanvas.handoff.receipt`, version
`1`. It records `archiveSha256`, logical `assetId`, exact package/compiler
versions, source/appearance/geometry/OBJ hashes, and the project-relative paths
of the source JSON, PNG, editable source asset, Mesh, Material, Prefab, and the
receipt itself. These paths define the files the importer owns. The receipt is
not geometry source and is never permission to overwrite an unrelated folder.

## Compatibility and limits

Handoff v1 is deliberately exact-version:

- package version must equal the receiver package;
- compiler version must equal the receiver compiler;
- FoldScript must be canonical version `0.1`;
- one PNG must decode at the manifest dimensions and the decoded canvas must
  not exceed 67,108,864 pixels;
- native custom operations are unsupported because FoldScript `0.1` cannot
  encode their implementation or contributor assembly.

The maximum archive is 256 MiB. Per-entry limits are 1 MiB manifest, 16 MiB
FoldScript, 64 MiB PNG, 128 MiB OBJ, 4 MiB compile evidence, and 1 MiB README,
with a 214 MiB total expanded limit. Unknown versions, migration guesses,
multiple canvases, other image formats, signing, encryption, and runtime
filesystem/network import are not part of v1.

## Security and failure behavior

Import treats every archive as untrusted. Before allocating payload buffers it
requires the exact entry count/order/names, stored method, fixed timestamp,
safe relative paths, zero extra/comment metadata, no links or reparse points,
and bounded declared sizes. It then verifies canonical JSON, hashes, PNG bytes,
detached compilation, derived OBJ, and all evidence before any `Assets/` write.

One root cause produces one stable `FC9301`-`FC9312` diagnostic. Persistence
occurs only in one new explicit folder; if asset creation fails, that folder is
removed and no existing folder is touched. See [Diagnostics](diagnostics.md).

## 中文速览

M12 交付的不是一个不可编辑 Mesh，而是“二维 PNG 原画 + 可读 FoldScript
构造规则”。接收方先在内存中校验压缩包、重新编译并比对完整证据，全部一致后才在
指定的 `Assets/` 子目录创建可编辑源资产和派生的 Mesh、材质、Prefab。删除派生
产物后可以从二维源重新生成；OBJ、Prefab 和回执永远不能反过来成为几何源。
