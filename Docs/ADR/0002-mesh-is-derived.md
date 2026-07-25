# ADR 0002: Mesh is a derived artifact

- Status: Accepted
- Date: 2026-07-25

## Context

Traditional modeling workflows often treat mesh topology as the primary editable asset. FoldCanvas aims to make the 2D canvas and construction program authoritative.

## Decision

Generated Unity meshes are compiler outputs. The authoritative source is the FoldCanvas document: appearance canvas, panels, boundaries, seams, operations, thickness, and settings.

## Consequences

- Generated mesh edits are not round-tripped in the MVP.
- A bug that requires hand-fixing generated vertices must be addressed in source or compiler behavior.
- Bake output should include source/version metadata later.
- Visual comparison alone is insufficient; source reproducibility is required.
