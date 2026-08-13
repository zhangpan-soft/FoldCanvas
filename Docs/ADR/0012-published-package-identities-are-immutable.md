# ADR 0012: Published package identities are immutable bytes

- Status: Accepted
- Date: 2026-08-13

## Context

M24 intentionally merged a backward-compatible compiler capability after
`v1.0.1` had been published. The repository root is itself the UPM package, so
the M24 source changes altered deterministic package bytes while `package.json`
and `FoldCanvasVersion` still said `1.0.1`. A consumer installing the public tag
and a consumer installing main would therefore receive different behavior and
bytes under the same semantic version.

## Decision

Every public tag is an immutable package identity: tag, peeled commit, release
id, publication time, archive/checksum/manifest/evidence digests, and asset
count are recorded in one ordinal machine-readable ledger. Repository
validation rebuilds each recorded tag with that tag's own release builder and
requires the archive SHA-256 to match the ledger.

If the current package version already appears in that ledger, the current
deterministic package archive must also match the recorded digest. Any packaged
source, documentation, sample, test, or metadata change after publication must
therefore advance the version before it can merge. Backward-compatible new API
or compiler operations use a minor version; compatible fixes and non-behavioral
package changes use a patch; incompatible behavior or API requires a major
version and migration decision.

M24 adds compatible Fold behavior, so M25 advances both package and compiler
identity to `1.1.0`. FoldScript remains `0.1`; generated Meshes remain derived.

## Consequences

- main can no longer silently impersonate immutable release bytes.
- historical releases are rebuilt from their own tagged builders rather than
  from current release code.
- release metadata is auditable without making a network request in normal
  repository validation.
- advancing a version is required before, not after, the first packaged change
  following publication.
- no public tag or asset is moved, rewritten, or relabeled.
