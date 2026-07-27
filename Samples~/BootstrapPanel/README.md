# Bootstrap Panel sample

This sample accompanies the pre-alpha planar compiler.

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

Fold, Roll, Stitch, and Solidify intentionally remain unsupported until their roadmap milestones.
