# Current task

Execute **M06: 2D canvas and 3D preview authoring workspace**.

Authoritative task file:
[`Codex/M06_EDITOR_WORKSPACE.md`](Codex/M06_EDITOR_WORKSPACE.md)

M06 development occurs on `codex/m06-editor-workspace`, stacked from the
verified M05 review head `46aa02f`. PR #4 remains open and unmerged; this
branch must not be merged to `main` before the M05 review gate is resolved.

The M06 proof is an Editor workflow in which a non-modeler starts from a blank
FoldCanvas asset, assigns an appearance canvas, creates rectangle and disk
panels, names them, pairs boundaries, adds explicit Place/Roll/Stitch/Solidify
operations, reads compiler diagnostics, previews the derived result, and bakes
the cup without editing code or vertex coordinates.

Use Unity UI Toolkit. Source changes must use serialized editing and support
Undo/Redo. Compilation is debounced and must never run on every repaint. The
preview hierarchy, Meshes, and materials are disposable derived artifacts,
owned by the workspace, excluded from builds, and cleaned up across recompiles
and domain reloads.

M06 must reuse the deterministic M00-M05 compiler and its diagnostics. It must
not change geometry semantics merely to make the UI easier, edit generated
Mesh data as source, add third-party UI/graph packages, add runtime authoring,
or implement M07 geometry validation, M08 AI/FoldScript round-tripping, or any
later milestone.
