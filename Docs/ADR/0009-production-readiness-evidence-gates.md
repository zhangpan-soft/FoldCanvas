# ADR 0009: Production readiness uses consumer evidence gates

- Status: Accepted
- Date: 2026-08-03

## Context

M00-M10 provide extensive compiler, topology, Editor, and package-release
evidence, but most Unity execution still happens through the repository's local
`Project~` host. That arrangement can hide missing archive files, assembly
visibility mistakes, repository-relative paths, sample import assumptions, and
public API breaks. Passing repository tests alone is therefore insufficient for
a production package.

M10 also introduces native operation executors. Their public context is
capability-limited, but the executor itself is ordinary managed code in the
Unity process. Treating this as a security sandbox would promise a guarantee the
runtime cannot enforce.

## Decision

FoldCanvas production readiness is an evidence ladder:

1. repository/static validation;
2. package Edit Mode tests in the tracked host;
3. byte-reproducible release archive;
4. clean Unity host resolving only that archive;
5. consumer-owned code compiling through public API;
6. deterministic production-corpus evidence;
7. foreground proof for claims that depend on rendering or interaction.

M11 adds a checked-in public Runtime API signature baseline. Before `1.0`, a
breaking change still requires an ADR, migration notes, and an explicit package
version decision. After `1.0`, semantic-version compatibility governs the same
baseline.

Native extension executors are trusted contributor code. FoldCanvas limits the
mesh capabilities supplied by its API and rolls back invalid compilation, but
does not claim to sandbox arbitrary code already loaded into the Unity process.

Clean-host and corpus reports are derived evidence. They never replace the
canvas, FoldScript, or source asset with a generated Mesh.

## Consequences

- A repository-only pass can no longer qualify a release candidate.
- Missing package files, internal-API dependencies, and archive-resolution
  fallbacks fail before release.
- Public API changes become explicit and reviewable instead of accidental.
- Contributors receive an honest trust model for native extensions.
- CI becomes longer, so clean-install jobs remain separate from fast static and
  package-test jobs and retain their own artifacts.
