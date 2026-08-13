# M25: 1.1.0 minor release qualification

## Outcome

Publish the M24 off-grid Fold capability as exact GitHub release `v1.1.0`
without reusing the immutable `v1.0.1` package identity, and make future
published-version byte drift fail in repository validation.

## In scope

- advance package and compiler identity from `1.0.1` to `1.1.0`;
- one machine-readable minor-release contract and JSON Schema;
- an immutable release ledger that can rebuild every recorded public tag and
  compare its deterministic archive against the published SHA-256;
- deterministic archive, manifest, checksum, and minor-release evidence;
- exact-head maintainer audit and tag authorization for `v1.1.0`;
- two clean public consumers and a source-first `1.0.1` to `1.1.0` upgrade;
- production-corpus proof that the M24 off-grid Fold succeeds while the five
  unchanged cases retain their exact geometry identities;
- exact GitHub publication and post-publication qualification.

## Acceptance

1. The package, compiler, Runtime API manifest, and current production corpus
   identify `1.1.0`; FoldScript remains `0.1` and Unity remains
   `6000.3.20f1`.
2. `v1.0.1` rebuilds to archive SHA-256
   `4188d23b18b924f6642f9e4eabbc15500fb60fb3d3916f23857a5a19966e1de5`.
3. The current tree cannot identify itself as any version recorded in the
   immutable release ledger unless its deterministic archive is byte-identical
   to that release.
4. Normalized Runtime API shape remains the stable 808-signature baseline.
5. The current six-case production corpus equals M24 evidence: five legacy
   cases are unchanged and `off-grid-fold` is a successful seven-vertex,
   six-triangle result with no error diagnostic.
6. Release publication requires exactly one owner audit for the exact PR head,
   an annotated `v1.1.0` tag at the protected-main merge, green required
   exact-merge workflows, and zero open `release-blocker` issues.
7. The exact four public assets pass canonical archive verification, install
   into two clean Unity consumers, and rebuild unchanged production-cup source
   after upgrading from immutable `v1.0.1`.
8. Repository checks, all Unity Edit Mode tests, deterministic release, source
   upgrade, public consumers, and post-publication proof are green.

## Non-goals

- new geometry behavior beyond the already merged M24 crease refinement;
- changing FoldScript `0.1`, package dependencies, or the supported Unity row;
- expanding crease refinement to curves, disks, branches, interior endings,
  or collinear overlap;
- topology-group deformation propagation or any post-Stitch deformation;
- registry, Asset Store, or other external marketplace publication;
- changing any existing Git tag, release, or public asset.

## Rollback

Before publication, delete only an unpushed local `v1.1.0` tag or leave a
failed public attempt unpublished. After publication, never move or rewrite
`v1.1.0`; reinstall immutable `v1.0.1` and rebuild from the same 2D canvas plus
FoldScript source.
