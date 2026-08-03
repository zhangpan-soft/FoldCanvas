# M10 contributor operation template

This sample shows the complete bounded extension path without editing the
FoldCanvas compiler:

1. `WaveOperationDefinition` is a native `SerializeReference` source operation
   and reports `FoldOperationType.Custom`.
2. `WaveOperationExecutor` declares one stable reverse-domain type ID, validates
   one target panel, reads immutable source/UV/provenance vertex data, and writes
   only finite positions.
3. `WaveOperationExample` creates a registry explicitly and passes it to one
   `FoldCanvasCompiler.Compile` call.

The registry is not global. Omitting it returns `FC9001` and no Mesh. Failed or
throwing execution rolls back positions. The public M10 context intentionally
cannot change triangles, topology IDs, boundaries, UVs, provenance, or geometry
budgets.

Custom native operations require their defining assembly. FoldScript `0.1`
continues to reject unknown operation types; it does not carry opaque extension
payloads. A future portable operation-codec design requires a versioned schema
and separate ADR.

Import this sample through Unity Package Manager, add a
`WaveOperationDefinition` to a `FoldCanvasAsset.Operations` managed-reference
list from your own authoring UI, and call `WaveOperationExample.TryCompile`.
The returned Mesh remains derived; keep the canvas, panel, and operation source.
