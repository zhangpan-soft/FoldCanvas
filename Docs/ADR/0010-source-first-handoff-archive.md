# ADR 0010: Production handoff archives remain source-first

- Status: Accepted
- Date: 2026-08-03

## Context

A production asset must move between Unity projects with its artwork, editable
construction program, validation evidence, and runtime-ready output. A raw Mesh,
Prefab, UnityPackage, or OBJ alone loses source ownership and can hide compiler,
package, texture-import, and compatibility assumptions. Importing an arbitrary
archive also creates path traversal, decompression, partial-write, and overwrite
risks.

## Decision

M12 defines a versioned deterministic handoff ZIP whose authoritative entries
are canonical FoldScript and the exact PNG appearance bytes. Manifest, OBJ,
compile report, and rebuild instructions are derived evidence around those two
source entries.

The archive has a fixed allowlisted layout, fixed timestamps, no compression,
ordinal entry order, bounded sizes, and SHA-256 for every payload. Import fully
validates and recompiles detached in-memory source before it creates any Unity
asset. Persistence targets one explicit previously nonexistent `Assets/`
folder; on failure FoldCanvas removes only that newly owned folder. The same
archive may be recognized idempotently by its receipt, but changed or unowned
destinations are never overwritten implicitly.

The receiving project regenerates Mesh, one-sided textured Material, and Prefab
with its installed FoldCanvas compiler. Archived OBJ is compared as evidence
and is never accepted as geometry input. Handoff v1 requires exact package,
compiler, and FoldScript versions and supports only an exact portable PNG.
Native custom operations remain unsupported because FoldScript `0.1` does not
encode their contributor definitions.

## Consequences

- Asset owners retain reviewable 2D source and deterministic rebuild steps.
- Receivers can ship ordinary Unity assets without depending on the producer
  project or editing generated topology.
- Archive tampering, traversal, incompatible versions, and evidence drift fail
  before project writes.
- Project-local Unity GUIDs are intentionally regenerated; logical asset ID and
  content hashes provide portable identity.
- Exact-version v1 is conservative. Upgrade/migration and signed distribution
  require later versioned decisions rather than silent compatibility guesses.
