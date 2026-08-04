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

- [ ] exact PR head recorded
- [ ] full Unity XML and Editor.log uploaded
- [ ] deterministic release-candidate bundle validated
- [ ] no failed, skipped, or inconclusive required test

## Scope

State the active milestone and confirm that later milestones were not implemented.

## Architecture

- [ ] no new dependency, or an ADR is included
- [ ] generated Mesh remains a derived artifact
- [ ] AI/provider code is outside the core
- [ ] custom operations use an explicit per-compile registry with no global discovery
- [ ] exports and gallery views remain derived artifacts

## Release and rollback

- [ ] package/API/FoldScript/Unity compatibility impact is explicit
- [ ] rollback target preserves the 2D canvas plus FoldScript source
- [ ] no credentials, paid service, irreversible permission, legal change, or external marketplace publication is hidden in this PR

## External fork note

Fork pull requests intentionally cannot access FoldCanvas Unity or repository
secrets and cannot merge directly. The maintainer reviews the exact fork head,
preserves attribution, imports an approved patch into a maintainer-owned
integration PR, and runs the complete privileged Unity evidence there. Do not
add credentials or weaken a check to bypass `Trusted contribution qualification`.
