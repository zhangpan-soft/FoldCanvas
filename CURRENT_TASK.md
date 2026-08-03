# Current task

Execute **M08: FoldScript import/export and AI repair contract**.

Authoritative task file:
[`Codex/M08_FOLDSCRIPT_AI.md`](Codex/M08_FOLDSCRIPT_AI.md)

M07 PR #6 was human-approved and merged into `main` as `9ca0d68`. M08
development occurs on `codex/m08-foldscript-ai`, created from that merged
commit.

M08 makes FoldScript `0.1` executable as the portable source contract:

- parse untrusted JSON into explicit FoldScript DTOs;
- emit canonical JSON with fixed property order, source array order, invariant
  numeric formatting, and stable escaping;
- convert explicitly between DTOs and `FoldCanvasAsset` without serializing
  Unity object internals;
- retain document identity, units, appearance reference, and canvas metadata;
- reject malformed, oversized, unknown-version, unknown-operation,
  non-finite, duplicate-ID, and unsafe-path input with stable diagnostics;
- expose provider-neutral proposal/repair contracts and a compact diagnostic
  payload whose corrected FoldScript must pass the same importer and compiler;
- provide Editor import/export actions whose appearance resolution is confined
  to approved Unity project paths.

The Runtime assembly remains offline and provider-independent. It must not
reference `UnityEditor`, access arbitrary files, add a JSON/AI/network package,
or accept binary mesh payloads. Generated Meshes remain derived artifacts.

M08 does not implement a model provider, network transport, automatic repair,
M09 handle/torus topology, Bevel, subdivision, smoothing, remesh, or Mesh
cleanup.
