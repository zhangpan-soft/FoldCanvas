# ADR 0007: Explicit cyclic topology from boundary spans and seams

- Status: Accepted
- Date: 2026-08-03

## Context

M00-M08 can build and validate genus-zero shells, but they cannot express a
torus or attach one strip to two selected regions of another boundary. Adding
an opaque torus primitive or importing a finished handle Mesh would violate the
FoldCanvas rule that the 2D source domain and construction program are
authoritative.

## Decision

M09 adds two bounded source concepts:

1. A boundary reference may optionally select one finite, normalized,
   non-wrapping arc-length span in the boundary's authored direction. Omission
   selects the complete boundary exactly as before. Stitch remains the only
   operation that changes topology identity.
2. `ToroidalWrap` maps an existing congruent planar rectangle into a toroidal
   parameter surface. It changes positions but does not implicitly close a
   cycle. Authored U/V boundary seams selected by Stitch perform each closure.

The handle proof uses an ordinary rectangle strip, existing RigidTransform and
Fold operations, two cup-rim boundary spans, explicit Weld seams, and the
existing Solidify operation. The torus proof uses one rectangle,
ToroidalWrap, and two explicit Weld seams.

## Consequences

- Torus and handle topology remains reproducible from FoldCanvas source.
- UV render duplicates may remain at cyclic seams while topology IDs are
  welded; UV continuity behavior is explicit rather than hidden by cleanup.
- Span endpoints can reuse deterministic Stitch boundary subdivision and its
  geometry budget/transaction guarantees.
- M09 supports rim/boundary attachment but does not punch holes into an
  interior face. Interior tube sockets need a later explicit loop/hole-domain
  ADR.
- Arbitrary sweep paths, CSG, bevels, remeshing, and imported primitive Meshes
  remain outside M09.
