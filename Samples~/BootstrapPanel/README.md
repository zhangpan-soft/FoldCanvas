# Bootstrap Panel sample

This sample accompanies the pre-alpha planar, Fold, and Roll compiler.

After importing the sample, use:

```text
Tools > FoldCanvas > Create Bootstrap Sample
```

The command creates a checker-pattern appearance canvas and a `FoldCanvasAsset` under `Assets/FoldCanvasSamples`. The asset contains:

- one rectangle panel using the left half of the canvas
- one decorated ellipse (`disk` shape with unequal physical dimensions) using
  the right half of the canvas
- one rigid transform that moves the ellipse beside the rectangle

Bake it from the Inspector or `Window > FoldCanvas > FoldCanvas`.

M08 also makes the bundled
`gpt-cup.future-example.foldcanvas.json` executable. The historical filename is
retained so existing sample links remain stable. After importing this package
sample into the host project's `Assets` tree, open the authoring workspace,
choose `Import JSON`, select that file, and save the editable source to an
explicit `.asset` path. `Export JSON` writes the selected source back in
canonical FoldScript `0.1` form; Diagnostics can copy a provider-neutral repair
payload without making a network request.

For the M03 decorated cup proof, keep the packaged
`gpt-cup-canvas.png` available and use:

```text
Tools > FoldCanvas > Create M03 Cup Proof
```

The editor copies the source canvas into the host project, creates a
`FoldCanvasAsset`, rolls the `GPT 5.6` wall through `360` degrees, rigidly
places the `CODEX` disk at the bottom, bakes the derived mesh, and creates a
package-owned `EditorOnly` preview root containing the cup, unchanged 2D source
canvas, and one untagged preview camera. Re-running the command reuses those
objects, including when inactive, and never modifies an existing MainCamera.
Read the positive Roll from outside the cup: source U runs left-to-right around
the wall and generated normals point radially outward. The command validates
all 64 wall-bottom/perimeter samples before showing the proof. It then proves
that the wall-side seam and bottom perimeter share logical topology and that
only the 64-edge top rim remains open.

The proof material is an opaque two-sided Unlit visualization so the
zero-thickness M03 wall and disk stay visible while orbiting the selected
object. This does not duplicate triangles, add an inner wall, or weaken the
compiler's outward-winding tests; physical thickness and the closed thick-shell
construction are demonstrated separately by the M04 proof.

For the M04 result proof, keep `M04ProductionCupCanvas.png` available and use:

```text
Tools > FoldCanvas > Create M04 Production Cup Proof
```

The command compiles the same exact wall and bottom placement into an inward
solid shell. It creates two copies of the one generated mesh: a texture-free,
one-sided solid diagnostic and a one-sided production-texture proof. The
production canvas fills every wall edge with safe color, keeps the welded wall
bottom and disk perimeter the same color, and supplies 12 pixels of disk-edge
bleed for bilinear filtering.

The owned `EditorOnly` root contains normal exterior, exact-side, interior, and
underside cameras. Exterior is enabled by default; switch views from
`Tools > FoldCanvas > M04 View`. The retained decorated M03 canvas is a
presentation example, not the M04 geometry-seam oracle.

For the M04.1 closed-volume inspection proof, use:

```text
Tools > FoldCanvas > Create M04.1 Closed Volume Cup Proof
```

The command creates a separate `Cup ClosedVolume` FoldCanvas source using the
same production 2D canvas, exact bottom placement, explicit Welds, and inward
Solidify rules. It rejects the proof unless the compiled result reports one
connected non-zero closed volume.

Its owned `EditorOnly` hierarchy shows:

- the authoritative 2D source canvas
- a texture-free one-sided solid result
- a wireframe made from unique logical topology edges
- a fixed vertical section and triangle/plane intersection lines
- automatically generated `OuterCorner` and `InnerCorner` overlays for the
  welded wall-bottom hard corner

Switch between `Overview`, `Wireframe`, and `Section` from
`Tools > FoldCanvas > M04.1 View`. The proof does not add bevels, subdivision,
smoothing, or mesh-cleanup postprocessing.

## M12 production handoff fixture

`m12-production-cup.foldcanvas.json` and `M04ProductionCupCanvas.png` are the
authoritative production-handoff fixture. The FoldScript uses the same exact
wall/bottom placement, explicit Weld seams, and Strict inward Solidify contract
as the production cup proof; the canvas uses square wall corners, matching
weld-edge colors, and disk perimeter bleed.

Import the sample into `Assets/`, import the JSON as an editable source, select
that source, and use:

```text
Tools > FoldCanvas > Handoff > Export Selected Source...
```

The resulting `.foldcanvas.zip` contains canonical source and exact PNG bytes,
plus derived OBJ and compile evidence. A receiver imports it through
`Tools > FoldCanvas > Handoff > Import Archive...`. Delete only
`GeneratedMesh.asset`, `Appearance.mat`, and `Runtime.prefab`, then choose
`Rebuild Selected Import` to prove that those runtime outputs come from the
received 2D source rather than the archived OBJ or an edited Mesh.
