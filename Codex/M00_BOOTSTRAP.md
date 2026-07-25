# M00: Bootstrap and repository health

## Goal

Turn the provided scaffold into a verified first commit without expanding feature scope.

## Required work

1. Inspect the UPM package layout, `package.json`, asmdefs, `Project~`, code, tests, schema, and documentation links.
2. Parse every JSON file.
3. Check all C# files for obvious namespace, accessibility, serialization, and Editor/Runtime boundary problems.
4. Open or batch-run Unity 6.3 LTS when available.
5. Fix compile errors, failing bootstrap tests, broken menu paths, invalid local package references, and asset-baking defects.
6. Ensure `Tools > FoldCanvas > Create Bootstrap Sample` is idempotent.
7. Ensure `Window > FoldCanvas > FoldCanvas` can compile a source in memory and bake a mesh under `Assets/FoldCanvasGenerated`.
8. Ensure unsupported Fold, Roll, Stitch, and Solidify operations fail with diagnostics rather than being ignored.
9. Do not implement Fold, Roll, Stitch, Solidify, FoldScript importing, or a 3D preview in this milestone.

## Acceptance criteria

- Unity recognizes the root as package `com.foldcanvas.core`.
- `Project~` resolves the package locally.
- Runtime assembly contains no `UnityEditor` reference.
- Editor and test assemblies compile.
- All bootstrap Edit Mode tests pass.
- Rectangle and disk source panels compile to valid non-empty meshes.
- Rigid transform affects only its target panel.
- Duplicate panel IDs, invalid canvas rects, and unsupported operations produce stable codes.
- Sample creation can be executed twice without duplicate assets or objects.
- Baking updates an existing mesh asset without changing its GUID.
- No Library, Temp, Logs, generated mesh, or IDE cache is added to source control.

## Required final report

Include the exact Unity version used, test count, failures fixed, files changed, and any checks the environment could not execute.
