# ADR 0005: Editor baking before runtime generation

- Status: Accepted
- Date: 2026-07-25

## Decision

MVP compilation and asset creation are Editor workflows. Runtime generation can be added later after correctness and performance are characterized.

## Consequences

- Editor tooling owns `AssetDatabase` and prefab saving.
- Runtime assembly remains capable of in-memory compilation but is not optimized for per-frame or player-time authoring.
- Samples focus on Bake workflows.
