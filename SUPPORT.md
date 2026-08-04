# FoldCanvas support policy

## Supported release

The supported release-candidate line is `1.0.0-rc.1` on Unity
`6000.3.20f1`. Other Unity versions may work, but they are not release-qualified
until they pass the same hosted package, clean-install, handoff, corpus, and
robustness gates.

This is a pre-release package. Support is best effort and no response-time or
availability SLA is offered.

## Before reporting a defect

Keep the original editable 2D canvas and FoldScript. Do not reduce a report to
the generated Mesh, because the Mesh is derived and cannot prove the source or
operation sequence that produced it.

Include:

- exact Unity version, package version, and commit or archive SHA-256;
- the smallest canonical FoldScript and appearance dimensions that reproduce
  the issue, with private artwork replaced when necessary;
- compile validation level and complete ordered diagnostics;
- expected topology, winding, seam, UV, or visual invariant;
- actual counts/report plus repeatable reproduction steps;
- whether a clean archive-only project reproduces the problem.

Never post credentials, Unity licenses, private source assets, account data, or
security exploit details in a public issue.

## Priority order

Maintenance triage uses this order:

1. security and credential exposure;
2. data loss or authoritative-source corruption;
3. compiler correctness or a successful result for invalid geometry;
4. nondeterminism or stale cross-compile state;
5. topology, winding, UV, seam, and closed-volume defects;
6. installation, upgrade, archive, or supported-Unity failure;
7. authoring usability, documentation, and feature requests.

A reproducible release blocker is fixed before roadmap expansion. Geometry
changes require deterministic Edit Mode tests and must not silently approximate
unsupported input.

## Security reports

Use the repository's private vulnerability-reporting page for suspected code
execution, path traversal, unsafe deserialization, malicious import, or secret
exposure. See [SECURITY.md](SECURITY.md).

## Rollback

Retain the previous package archive and authoritative source before upgrading.
For the M14 candidate, the known-good rollback is `0.1.0-preview.21` from merge
commit `d9434be`. Reinstall that archive or immutable commit, then recompile the
2D canvas and FoldScript. Do not restore a generated Mesh as the editable
source.
