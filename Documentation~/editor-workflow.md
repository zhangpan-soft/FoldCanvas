# Editor workflow

## MVP window

The FoldCanvas editor evolves toward a split workspace:

```text
┌──────────────────────────────┬──────────────────────────────┐
│ 2D Canvas                    │ 3D Preview                   │
│                              │                              │
│ panel regions                │ compiled surface             │
│ boundaries and seams         │ seam highlights              │
│ fold lines                   │ normals and thickness        │
│ artwork                      │ validation overlays           │
├──────────────────────────────┴──────────────────────────────┤
│ operation timeline | diagnostics | bake controls            │
└─────────────────────────────────────────────────────────────┘
```

The bootstrap window intentionally starts smaller: choose an asset, compile it, view diagnostics, and bake a mesh.

## Authoring actions

Planned actions:

- create panel from rectangle, disk, polygon, or mask
- name and reorder panels
- select and name boundaries
- pair seam boundaries
- add operation at current timeline position
- scrub operation history
- display UV artwork over panel domains
- display stretch, curvature, and thickness channels
- bake derived assets

## Preview rules

- preview state is disposable
- preview objects use `HideAndDontSave`
- preview compilation must not alter the source asset
- changing the source invalidates the preview hash
- the editor must display errors without creating partial saved assets

## Baking rules

- choose an explicit folder under `Assets/`
- sanitize file names
- update existing generated assets predictably
- save source hash and compiler version metadata later
- never write into package-cache folders
- never write outside the Unity project
