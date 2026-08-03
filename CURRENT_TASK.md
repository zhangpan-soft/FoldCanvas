# Current task

Execute **M12: production asset handoff**.

Authoritative task file:
[`Codex/M12_PRODUCTION_HANDOFF.md`](Codex/M12_PRODUCTION_HANDOFF.md)

M11 PR #10 was maintainer-audited and merged into `main` as `b757792` with
reviewed head `e1204ff`. Hosted run `30812427595` passed 401/401 package tests
and two independent 1/1 clean-archive consumer tests. M12 development occurs on
`codex/m12-production-handoff`, created from that merged commit.

M12 changes the acceptance question from “can another project install and use
the package?” to “can a production asset owner hand off the complete editable
2D source, reproducibility evidence, and rebuildable runtime outputs without
turning a generated Mesh into the source?”

The active proof therefore covers:

- one deterministic, versioned handoff archive containing canonical FoldScript,
  the exact PNG appearance source, derived OBJ, validation evidence, and rebuild
  instructions;
- bounded, traversal-safe, integrity-checked archive import with no partial
  project writes on rejection;
- regeneration of a FoldCanvas source asset, Mesh, one-sided textured Material,
  Prefab, and ownership receipt under an explicit new `Assets/` destination;
- producer/receiver evidence equality across two clean Unity projects;
- idempotent same-bundle import and refusal to overwrite an unowned or changed
  destination;
- documented exact-version and native-extension limitations for handoff v1.

M12 does not make Mesh canonical, add runtime file/network I/O, preserve Unity
GUIDs across projects, add a geometry family, expose topology mutation, migrate
between compiler versions, publish `1.0.0`, or implement M13/M14.
