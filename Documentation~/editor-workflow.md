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

The current window intentionally starts smaller: choose an asset, compile it,
view diagnostics, and bake a mesh.

M02 also provides two proof commands:

- `Tools > FoldCanvas > Create M02 Box Sample` creates or updates the
  GUID-stable six-region appearance canvas and six-panel source asset.
- `Tools > FoldCanvas > Create M02 Box Proof` compiles and bakes that source,
  creates an unlit preview material, and selects a scene object using the
  generated mesh. It never creates a Unity cube primitive.

The M02 proof mesh is a closed-looking but deliberately unwelded six-panel
shell.

M04 adds:

- `Tools > FoldCanvas > Create M04 Production Cup Sample`
- `Tools > FoldCanvas > Create M04 Production Cup Proof`
- `Tools > FoldCanvas > M04 View > Exterior | Exact Side | Interior |
  Underside`

The proof builds one generated thick-cup Mesh and presents it twice: first with
a texture-free one-sided diagnostic material, then with the bilinear
`M04ProductionCupCanvas.png`. The owned `EditorOnly` root never reads or
modifies `Camera.main`; exterior is the default camera and the other three
views remain independently selectable. The retained M03 decorated canvas is
not used as evidence of geometric closure.

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
