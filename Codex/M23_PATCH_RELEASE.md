# M23: 1.0.1 patch release

## Outcome

Publish the proof-first README work as exact GitHub release `v1.0.1` only
after the patch bytes, compatible API/corpus row, source-first upgrade, clean
consumers, and exact audited Git head have all passed fail-closed gates.

## In scope

- one machine-readable `1.0.1` patch-release contract and JSON Schema;
- deterministic archive, manifest, checksum, and patch evidence bound to that
  contract;
- tag publication and recovery support for exact `v1.0.1`;
- public-asset, two-clean-consumer, and `v1.0.0` to `v1.0.1` source-upgrade
  qualification;
- exact-head maintainer audit before merge and annotated tag creation;
- package and repository documentation that names `v1.0.0` as the immutable
  rollback.

## Acceptance

1. `v1.0.1` is the only newly supported release tag.
2. The tag must equal `v` plus `package.json.version` and peel to the exact
   audited merge commit.
3. The package has exactly four public assets with deterministic names and
   hashes.
4. Patch evidence identifies `Documentation~/m23-patch-release.json`, the
   immutable `v1.0.0` baseline, and a non-prerelease GitHub publication.
5. Runtime API shape, production-corpus geometry, FoldScript `0.1`, Unity
   `6000.3.20f1`, dependencies, source UVs, and generated topology are
   unchanged from the M22-qualified patch.
6. Two clean public consumers compile the exact downloaded archive, and a
   source-first `1.0.0` to `1.0.1` upgrade rebuilds the maintained production
   cup without accepting derived geometry as input.
7. Wrong tags, wrong contracts, wrong archive bytes, wrong baselines, draft or
   prerelease metadata, missing/extra assets, and stale evidence all fail.
8. Public qualification uploads the real Unity XML, Editor logs, comparisons,
   and one final patch-publication proof artifact.

## Non-goals

- new geometry, topology, FoldScript fields, or compiler behavior;
- a Unity-version expansion;
- registry or external marketplace publication;
- changing any existing `v1.0.0`, RC2, or earlier release asset;
- applying the repository social-preview setting;
- satisfying Windows issue #21 without evidence from a real Windows host.

## Rollback

Delete an unpushed local tag or leave a failed public attempt unpublished.
After publication, never move or rewrite `v1.0.1`; roll users back to immutable
`v1.0.0` and fix forward under a later semantic version.
