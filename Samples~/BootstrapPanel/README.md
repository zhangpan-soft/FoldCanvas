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

For the M03 decorated cup proof, keep the packaged
`gpt-cup-canvas.png` available and use:

```text
Tools > FoldCanvas > Create M03 Cup Proof
```

The editor copies the source canvas into the host project, creates a
`FoldCanvasAsset`, rolls the `GPT 5.6` wall through `360` degrees, rigidly
places the `CODEX` disk at the bottom, bakes the derived mesh, and creates a
preview object beside the unchanged 2D source canvas. Read the positive Roll
from outside the cup: source U runs left-to-right around the wall and generated
normals point radially outward. The wall seam and bottom remain spatially
aligned but topologically separate.

The proof material is an opaque two-sided Unlit visualization so the
zero-thickness M03 wall and disk stay visible while orbiting the selected
object. This does not duplicate triangles, add an inner wall, or weaken the
compiler's outward-winding tests; physical thickness and closed cup topology
remain M04 work.

Stitch and Solidify intentionally remain unsupported until M04.
