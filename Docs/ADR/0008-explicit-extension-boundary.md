# ADR 0008: Explicit per-compile extension boundary

- Status: Accepted
- Date: 2026-08-03

## Context

M00-M09 prove deterministic source-to-mesh compilation, but adding every future
operation directly to the core compiler would make contribution slow and would
encourage forks. A mutable global registry or reflection scan would make output
depend on process state and assembly load order. Exposing the internal mesh
buffer would also let an extension bypass geometry budgets, topology records,
UV provenance, rollback, and validation.

## Decision

M10 adds an explicit registry value passed to one compiler invocation. The
compiler snapshots registrations in stable operation-type-ID order during
preflight. No registration is discovered implicitly and the default overload
uses no extensions.

The first public mutation boundary is deliberately narrow: a registered custom
operation targets exactly one existing panel and may replace only its finite
vertex positions. Source positions, UVs, provenance, triangle indices,
boundaries, topology identities, and geometry-budget usage remain inaccessible
and unchanged. Execution is transactional, and extensions still obey the
terminal-Stitch rule.

Gallery manifests, performance reports, OBJ output, and release archives are
versioned or reproducible derived ecosystem artifacts. None becomes canonical
geometry source. FoldScript `0.1` does not gain an opaque custom-operation
payload; a future portable codec registry needs its own ADR and schema version.

## Consequences

- Contributors can implement and test bounded deformations without editing the
  core compiler or relying on hidden global state.
- Existing callers and all M00-M09 assets retain the exact default path.
- Topology-changing extensions remain internal until a future public API can
  enforce reservation, transaction, seam, and validation invariants.
- Native custom operation assets depend on the contributing assembly; their
  portability is explicit rather than falsely implied by FoldScript `0.1`.
- Deterministic OBJ/release output can be reviewed and cached without changing
  source ownership.
