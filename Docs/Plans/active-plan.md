# M23 active plan: 1.0.1 patch release

## Objective

Qualify and publish exact stable patch `v1.0.1` from an audited merge. This is
a release-evidence milestone: the authoritative geometry remains the 2D canvas plus FoldScript,
and no generated Mesh becomes source.

## Scope

- add a patch-release contract/schema for package `1.0.1`, stable baseline
  `v1.0.0`, FoldScript `0.1`, and Unity `6000.3.20f1`;
- make deterministic patch evidence point to that contract;
- support exact `v1.0.1` tag publication and manual recovery while keeping
  every historical tag path immutable;
- qualify the public archive in two clean consumers and a source-first
  `1.0.0` to `1.0.1` upgrade;
- retain real Unity XML and Editor logs as hosted artifacts;
- record exact-head audit before merge, then create an annotated tag and
  verify the resulting public release.

## Non-goals

- geometry, topology, operations, UV, compiler, or public API changes;
- package dependencies, network services in core, or a new Unity row;
- generated Mesh, OBJ, material, prefab, or report as upgrade authority;
- registry, external marketplace, or repository social-preview publication;
- simulated Windows evidence for issue #21.

## Gates

1. Contract and Schema parse and reject drift deterministically.
2. Release archives, manifests, checksums, and evidence are byte-reproducible.
3. `v1.0.1` is the sole new normal-release tag accepted by both publication
   and public qualification workflows.
4. Patch evidence binds exact contract bytes, stable baseline, API shape,
   corpus geometry, source-first upgrade fixture, and rollback.
5. Repository checks, full Edit Mode tests, clean installs, handoff, proof
   regeneration, source upgrade, and M13 long-run are green on exact PR head.
6. A maintainer audit comment names the exact head SHA before merge.
7. The annotated tag peels to the audited merge; publication has exactly four
   assets and is non-draft/non-prerelease.
8. Public qualification proves downloaded bytes, two clean consumers, and the
   source-first upgrade before reporting the patch as qualified.

## Failure and rollback

Any identity or evidence mismatch stops before publication. A failed tag run
may be retried only against the same immutable annotated tag. Never force-move
or rewrite a public tag or release. Rollback remains exact `v1.0.0`, followed
by recompilation from the unchanged 2D canvas and FoldScript.

## Progress

- 2026-08-13: PR #36 merged audited M22 head `8edd04b` as `1ff4a1b`.
- 2026-08-13: post-merge repository checks, full Unity workflow, and M13
  robustness long run all completed successfully on `main`.
- 2026-08-13: M23 selected as the explicit release milestone promised by M22;
  implementation is in progress on `agent/m23-patch-release-qualification`.
- 2026-08-13: the contract, Schema, deterministic evidence, public verifier,
  `v1.0.1` workflow paths, two-consumer/source-upgrade proof, and fail-closed
  exact-head authorization fixtures pass locally. Exact-head hosted audit and
  public tag qualification remain.
