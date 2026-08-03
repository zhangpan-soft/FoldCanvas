## Proof

Describe the source canvas/panels and the concrete compiled object.

## Invariants

- [ ] deterministic vertex/index order
- [ ] source UV preservation
- [ ] documented boundary order and winding
- [ ] stable diagnostics for invalid input
- [ ] Runtime does not reference UnityEditor

## Tests

List Edit Mode tests and manual Unity checks.

## Scope

State the active milestone and confirm that later milestones were not implemented.

## Architecture

- [ ] no new dependency, or an ADR is included
- [ ] generated Mesh remains a derived artifact
- [ ] AI/provider code is outside the core
- [ ] custom operations use an explicit per-compile registry with no global discovery
- [ ] exports and gallery views remain derived artifacts
