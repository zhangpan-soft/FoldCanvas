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

M06 implements this split workspace in UI Toolkit. Open
`Tools > FoldCanvas > Open Authoring Workspace` to create/select a source,
author rectangle and disk panels, pair named boundaries, edit the ordered
operation timeline, navigate compiler diagnostics, inspect disposable debug
views, and explicitly bake a valid result. See the complete
[blank-source-to-cup walkthrough](authoring-workspace.md).

M08 adds `Import JSON` and `Export JSON` to the same toolbar. Import accepts a
FoldScript `0.1` file already under the project's `Assets/` or installed
`Packages/` tree, validates it before persistence, and writes only the explicit
`.asset` destination chosen by the user. Export writes canonical JSON under
`Assets/`. Diagnostics also exposes `Copy Repair Payload`; this copies source
and diagnostics only and never sends a network request. See the
[FoldScript Runtime and Editor workflow](foldscript-runtime.md).

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

M04.1 adds:

- `Tools > FoldCanvas > Create M04.1 Closed Volume Cup Sample`
- `Tools > FoldCanvas > Create M04.1 Closed Volume Cup Proof`
- `Tools > FoldCanvas > M04.1 View > Overview | Wireframe | Section`

The separate owned `EditorOnly` hierarchy keeps the production 2D source
canvas visible beside a texture-free one-sided solid, a wireframe built from
unique logical topology edges, and a fixed object-space vertical section. The
section-line Mesh comes from exact triangle/plane intersections. Generated
`OuterCorner` and `InnerCorner` line objects reference the emitted hard-corner
shell positions. Re-running the proof reuses inactive objects and never reads
or modifies `Camera.main`.

The wireframe, section, and corner line Meshes are disposable inspection
artifacts. They are not mesh cleanup, subdivision, smoothing, or editable
source.

M05 adds:

- `Tools > FoldCanvas > Create Sphere Sample`
- `Tools > FoldCanvas > Create Sphere Proof`
- `Tools > FoldCanvas > M05 Sphere View > Overview | Textured Sphere |
  Wireframe and Seams | UV Stretch | Radius Error`

The proof creates or reuses one inactive-aware `EditorOnly` root:

```text
FoldCanvas Sphere Root
├── Source Canvas Preview
├── Generated Sphere
├── Solid Validation
├── Wireframe Debug
├── Seam Debug
├── Pole Debug
├── UV Stretch Debug
├── Radius Error Debug
├── Validation Report
└── Preview Camera
```

The source preview displays the authoritative 2048 x 1024 eight-gore canvas.
Every sphere and debug Mesh is derived by compiling the matching
`FoldCanvasAsset`; no Unity sphere primitive or fixed sphere Mesh is created.
`Solid Validation` uses a texture-free one-sided material so outward winding
cannot be hidden by `Cull Off`. Wireframe edges use logical topology, while
seam and pole overlays are separately derived from source boundary and
spherical metadata. UV-stretch and radius-error colors are debug attributes,
not source edits.

The text report records the closed-sphere result and key counts. Re-running the
proof reuses the same root, children, baked assets, and one owned camera,
including when they are inactive. It never reads or modifies `Camera.main`.

## Authoring actions

- create rectangle and disk panels
- name and select panels
- select and name boundaries
- pair seam boundaries
- add operation at current timeline position
- reorder, enable, disable, and edit operations through explicit forms
- display UV artwork over panel domains
- display logical wireframe, panel colors, seams, normals, and thickness
- bake derived assets
- import canonical FoldScript into an explicit editable source asset
- export the selected source as canonical FoldScript
- copy a provider-neutral diagnostic repair payload

## Preview rules

- preview state is disposable
- preview objects use `HideAndDontSave`
- preview compilation must not alter the source asset
- changing the source invalidates the preview hash
- the editor must display errors without creating partial saved assets
- compilation is debounced and stale revisions never replace a newer preview
- the preview owns one hidden `EditorOnly` hierarchy and never uses
  `Camera.main`

## Baking rules

- choose an explicit folder under `Assets/`
- sanitize file names
- update existing generated assets predictably
- source-reference/hash and compiler-version bake metadata remain a later
  data-model task; the M06 Mesh is still explicitly derived from its separate
  source asset
- never write into package-cache folders
- never write outside the Unity project
