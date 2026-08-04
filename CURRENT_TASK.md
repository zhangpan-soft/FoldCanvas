# Current task

Execute **M13: robustness and scale**.

Authoritative task file:
[`Codex/M13_ROBUSTNESS_SCALE.md`](Codex/M13_ROBUSTNESS_SCALE.md)

M12 PR #11 was maintainer-audited and merged into `main` as
`0d4a576736f7501d828cfc4a6824710ca4e5cb8a` with reviewed head
`5edcc23e1e98e785c5f76802f45f19962569aa49`. Hosted repository run
`30838162209` and Unity run `30838161788` passed; the complete package suite
was 431/431 and the independent handoff producer/receiver each passed 1/1.
M13 development occurs on `codex/m13-robustness-scale`, created from that
merged commit.

M13 changes the acceptance question from “can one reviewed production asset be
handed off reproducibly?” to “does the compiler keep its source-first,
deterministic, bounded behavior across many generated cases, near-limit assets,
repetition, cancellation between cases, and retry after failure?”

The active proof covers:

- a versioned deterministic case generator with fixed seeds and replayable case
  identities;
- bounded valid and invalid property cases that must never crash, silently
  approximate geometry, mutate source, or return a Mesh after failure;
- large deterministic fixtures near vertex, triangle, validation, source-input,
  and handoff limits;
- repeated compilation plus failure/cancellation-then-retry evidence with no
  leaked project asset, partial report, or stale geometry state;
- conservative time and allocation envelopes recorded separately from geometry
  hashes;
- a required pull-request smoke corpus and a larger scheduled/manual Unity
  regression run with XML, Editor log, canonical report, and replay data.

M13 does not add geometry families or operations, expose topology mutation,
change canonical source ownership, add runtime file/network I/O, implement
automatic mesh repair, freeze FoldScript/public API for 1.0, publish `1.0.0`,
or implement M14.
