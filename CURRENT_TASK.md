# Current task

Execute **M24: deterministic off-grid crease topology split**.

Authoritative task file:
[`Codex/M24_CREASE_TOPOLOGY_SPLIT.md`](Codex/M24_CREASE_TOPOLOGY_SPLIT.md)

M23 is complete. Exact package `v1.0.1` was published from audited merge
`867f3bd5501218aa95872db6e7e66cb213031cab`; the public qualification run
`31677125745` passed, and the release archive SHA-256 is
`4188d23b18b924f6642f9e4eabbc15500fb60fb3d3916f23857a5a19966e1de5`.
Immutable `v1.0.0` remains the documented patch rollback.

M24 activates the roadmap task previously guarded by
`FC3011 FoldCreaseRequiresTopologySplit`: compatible straight off-grid
rectangle creases are refined in normalized source space before panel emission,
then the existing current-frame rigid Fold executes without triangle stretch.

Implementation and local verification are complete on
`agent/m24-crease-topology-split`; exact-head PR audit, hosted required checks,
and protected-main merge remain.

The maintainer may research, plan, implement, audit, merge, and close this
repository-only milestone autonomously. Credentials, paid services,
irreversible permission changes, legal decisions, and external marketplace
publication remain owner escalation points.
