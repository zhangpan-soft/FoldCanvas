# M05 explicit-gore sphere

This sample proves that FoldCanvas reconstructs a closed spherical surface from
an ordinary 2D canvas plus explicit geometry rules. The source contains eight
rectangular gore panels. Each panel is mapped with `sphericalWrap`, and the
declared longitude boundaries are joined by one terminal `stitch` operation.

The source of truth is:

- `sphere-canvas.png`: the 2048 x 1024 appearance canvas;
- `sphere-golden.foldcanvas.json`: eight panel domains, eight spherical
  mappings, and the complete seam graph.

The generated Unity mesh is a derived artifact. The implementation does not use
Unity's sphere primitive, a hidden UV sphere, an imported sphere mesh, or a
post-process remesher.

`NORTH`, `FOLDCANVAS`, `SOUTH`, the equator band, and gore numbers are placed in
2D so that texture orientation, pole behavior, seam continuity, and UV
provenance can be inspected on the reconstructed result.

## 中文说明

这个样例用八片明确声明的二维球瓣，通过 `sphericalWrap` 映射与终端
`stitch` 焊接，生成一个闭合球面。二维图片和 FoldScript 是源资产；
Unity Mesh 只是可重复生成的派生产物。实现没有调用 Unity 球体原语，
也没有藏入固定球体网格。
