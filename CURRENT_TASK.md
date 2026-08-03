# Current task

Execute **M11: production-readiness foundation**.

Authoritative task file:
[`Codex/M11_PRODUCTION_READINESS.md`](Codex/M11_PRODUCTION_READINESS.md)

M10 PR #9 received a recorded maintainer audit and was merged into `main` as
`b67120f`. M11 development occurs on `codex/m11-production-readiness`, created
from that merged commit.

M11 changes the acceptance question from “does the package work inside its own
repository?” to “can an ordinary Unity project install, compile, use, upgrade,
and diagnose the published package without repository-local assumptions?”

The active proof therefore covers:

- deterministic package installation into a generated clean Unity host;
- a consumer-owned assembly that compiles a FoldCanvas source through only the
  public package API and verifies stable geometry/export evidence;
- a checked-in public Runtime API surface baseline and explicit compatibility
  policy;
- a versioned production-acceptance corpus with stable source/output hashes;
- an extension trust model that distinguishes capability limits from a process
  security sandbox;
- hosted clean-install evidence in addition to repository tests.

M11 does not add a geometry family, expose topology mutation, add CSG/bevel/
remesh, introduce runtime file or network access, publish version 1.0, or add a
new package dependency.
