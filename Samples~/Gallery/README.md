# FoldCanvas sample gallery manifest

`gallery.json` is the canonical `foldcanvas-gallery` version `1` manifest used
by `Tools > FoldCanvas > Open Sample Gallery`.

Entries preserve authored order and may reference only normalized paths under
`Samples~/`. Proof actions must begin with `Tools/FoldCanvas/` and are executed
only after Runtime manifest validation. The JSON Schema is
`Schema/foldcanvas-gallery.schema.json`.

The manifest is ecosystem metadata, not geometry source. Each listed canvas,
FoldScript document, panel graph, seam graph, and operation list remains the
authoritative asset source; gallery views and generated Meshes remain derived.
