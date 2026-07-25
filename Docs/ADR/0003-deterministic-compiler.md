# ADR 0003: Deterministic compiler

- Status: Accepted
- Date: 2026-07-25

## Decision

Identical source, settings, and compiler version must generate stable vertex order, index order, UVs, and diagnostic order.

## Rationale

Determinism enables:

- reliable tests
- meaningful source control diffs for metadata
- cache keys
- AI repair loops
- reproducible bug reports
- future remote build services

## Consequences

- No dependence on scene discovery, frame time, locale, or random state.
- Parallel processing must preserve deterministic merge order.
- Dictionaries cannot define output order unless explicitly sorted.
