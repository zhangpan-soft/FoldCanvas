# Current task

Execute **M10: extensibility and ecosystem**.

Authoritative task file:
[`Codex/M10_EXTENSIBILITY.md`](Codex/M10_EXTENSIBILITY.md)

M09 PR #8 was human-approved and merged into `main` as `7be4117`. M10
development occurs on `codex/m10-extensibility`, created from that merged
commit.

M10 turns the proven M00-M09 compiler into a bounded contributor platform:

- pass an explicit operation registry to one compile without global discovery;
- allow registered third-party position-only operations to deform exactly one
  existing panel while preserving UV, provenance, triangles, and topology;
- publish a versioned sample-gallery manifest and a compiling contributor
  operation template;
- export immutable compiled data to deterministic text OBJ without changing
  the FoldCanvas source or generated Mesh;
- measure repeatable Editor compilation baselines as derived evidence;
- build a deterministic UPM release archive and automate tagged GitHub
  releases.

ADR 0008 defines the extension trust boundary. FoldCanvas source remains
authoritative, generated Meshes and exports remain derived, and the default
compiler behavior stays byte/topology compatible when no registry is supplied.

M10 does not expose topology mutation to extensions, auto-discover assemblies,
add custom FoldScript operation codecs, add runtime file/network I/O, implement
glTF/FBX, or add new geometry families.
