# FoldScript 0.1 runtime and Editor workflow

M08 makes FoldScript `0.1` an executable, portable source contract. A JSON
document is parsed into explicit DTOs, validated, converted to a
`FoldCanvasAsset`, and compiled through the same deterministic geometry path as
a source created in the Unity Inspector. JSON and the 2D appearance remain
source; a generated `Mesh` remains disposable derived data.

## Public Runtime API

```csharp
FoldScriptReadResult read = FoldScriptSerializer.Read(json);
FoldScriptWriteResult write = FoldScriptSerializer.Write(read.Document);

FoldScriptImportResult import = FoldScriptImporter.Import(
    json,
    new FoldScriptImportOptions
    {
        SourceProjectPath = "Assets/FoldScripts/cup.foldcanvas.json",
        AppearanceResolver = resolver,
        RequireAppearance = true
    });

FoldCanvasCompileResult compile =
    FoldCanvasCompiler.Compile(import.Asset);
```

`FoldScriptSerializer.Canonicalize` validates and rewrites JSON in canonical
form. `FoldScriptImporter` performs read, semantic validation, appearance
resolution, unit conversion, native-source construction, and canonical output.
Runtime accepts an `IFoldScriptAppearanceResolver`; it never reads files, calls
`AssetDatabase`, opens a network connection, or selects an AI provider.

All result diagnostics are copied into read-only ordered collections. Callers
must check `Success` before using `Document`, `Asset`, or canonical output.

## Canonical form

Root properties are emitted in this fixed order:

```text
schemaVersion, assetId, displayName, units, canvas,
panels, seams, operations, compile, extensions
```

Known nested properties also have fixed writer order. Authored panel, seam, and
operation arrays are never sorted because their order is part of source
meaning. Keys inside `extensions` are sorted with ordinal comparison; extension
values are preserved but never interpreted by the geometry compiler.

Numbers use invariant round-trip formatting, negative zero is normalized, JSON
escaping is deterministic, line endings are LF, and output has exactly one
trailing newline. Identical DTO content therefore produces byte-identical JSON
independent of current locale.

## Units

FoldScript may declare `meter`, `centimeter`, or `millimeter`. Physical panel
sizes, translations, Roll/SphericalWrap radii, Solidify thickness, and physical
compile tolerances are converted to meters before native compilation. Angles,
normalized canvas rectangles, normalized Fold lines, counts, IDs, booleans,
and validation levels are not scaled. Export converts native meter values back
to the document's retained unit.

## Untrusted-input boundary

The in-package reader is deliberately bounded before native source lists are
allocated. It rejects:

- malformed JSON, duplicate object properties, non-finite numbers, excessive
  character count, nesting depth, node count, and string length;
- unknown schema versions, unknown operation types, missing/unknown fields,
  invalid identifiers/enums/numeric ranges, excessive collection counts, and
  duplicate or missing references;
- absolute paths, URI-like paths, backslashes, empty/dot/traversal segments,
  and paths outside `Assets/` or `Packages/`.

The executable limits live in `FoldCanvasLimits`; repository validation checks
the matching JSON Schema maxima. Invalid input returns one stable primary
`FC7001`-`FC7012` diagnostic and does not silently default required data or
produce a `Mesh`.

## Editor import and export

Open `Tools > FoldCanvas > Open Authoring Workspace` and use:

1. **Import JSON** — select a `.json` file already inside this Unity project's
   `Assets/` or installed `Packages/` tree, then choose an explicit `.asset`
   destination under `Assets/`.
2. Edit and preview the imported `FoldCanvasAsset` in the existing M06
   workspace.
3. **Export JSON** — choose an explicit path under `Assets/`; the Editor writes
   canonical FoldScript and imports it into the project.
4. In Diagnostics, use **Copy Repair Payload** to copy the current canonical
   source plus ordered compile diagnostics.

Import validates a detached source first. A failed import does not mutate the
destination. Re-importing to an existing FoldCanvas source records Undo and
reuses that object; an occupied non-FoldCanvas destination is rejected.

## Provider-neutral repair loop

`IFoldCanvasSourceProposer` and `IFoldCanvasSourceRepairer` describe synchronous
data boundaries only. They do not include provider SDKs, credentials, transport,
or automatic execution.

```text
canonical FoldScript + ordered compiler diagnostics
                         ↓
               external repair adapter
                         ↓
              replacement FoldScript JSON
                         ↓
        bounded import → deterministic compile
```

`FoldCanvasRepairRequest` contains schema/compiler versions, asset ID,
canonical source JSON, diagnostic code/severity/message, source and geometry
context, ordered numeric values, and suggestions. It deliberately contains no
Mesh, vertex buffer, triangle buffer, texture pixels, credentials, or provider
metadata. `FoldCanvasRepairCoordinator.Apply` accepts replacement JSON only;
the response has no privileged geometry-mutation path.

M08 does not send a repair request, choose a model, or automatically accept a
change. A human or external package remains responsible for transport and
review.
