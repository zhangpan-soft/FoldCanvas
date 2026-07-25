# M06: 2D canvas and 3D preview workspace

## User proof

A person who does not edit vertices can create the cup sample inside Unity by selecting canvas regions, naming boundaries, adding Roll/Place/Stitch/Solidify operations, viewing errors, and baking the result.

## UI technology

Use Unity UI Toolkit. Keep UXML/USS modular if the window becomes substantial. Do not add third-party graph or editor packages.

## Layout

- left: 2D appearance canvas
- right: interactive 3D preview
- bottom or side: panel list, operation list, seams, diagnostics, bake controls

## Required 2D tools

- zoom and pan
- rectangle panel creation
- disk panel creation
- panel selection and naming
- canvas rect handles
- boundary highlighting
- seam pairing
- operation creation through explicit forms

## Required 3D tools

- orbit only inside preview, not a source operation
- frame result
- wireframe toggle
- panel coloring/debug overlay
- seam highlight
- normals toggle
- thickness display
- diagnostic focus

## State and undo

- use SerializedObject where appropriate
- all source edits support Undo/Redo
- preview is disposable and never saved automatically
- compile is debounced, cancellable where possible, and never runs every repaint
- no leaked meshes or materials across recompiles/domain reloads

## Acceptance

A documented manual test walks from blank asset to baked cup without code edits.
