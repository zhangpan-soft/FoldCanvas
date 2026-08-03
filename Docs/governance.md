# Early governance

FoldCanvas begins as a maintainer-led open-source project.

## Decision hierarchy

1. Published source invariants and accepted ADRs
2. Reproducible tests and compiler diagnostics
3. Maintainer decisions for unresolved design trade-offs
4. Experimental branches and proposals

## Maintainer responsibilities

- protect the 2D-source-first architecture
- keep the deterministic core provider-independent
- avoid premature claims of universal coverage
- publish clear milestones and acceptance criteria
- review contributor work respectfully and technically
- preserve backward compatibility once stable formats are released

## Autonomous maintenance cadence

The repository owner has delegated day-to-day roadmap, implementation, issue
triage, pull-request, merge, and preview-release maintenance to the active
FoldCanvas maintainer workflow. That delegation does not weaken evidence gates:

- every non-trivial change still uses an isolated branch and reviewable PR;
- required repository and Unity checks must be green before merge;
- a maintainer self-audit must identify the exact head and disclose that it is
  not an independent human review;
- security reports, credentials, paid services, irreversible permission
  changes, legal decisions, and external marketplace publication are escalated
  to the owner;
- public issues are reproduced and prioritized by security, data loss,
  compiler correctness, determinism, topology, installation, then usability.

The project is checked periodically for new issues, pull requests, failed CI,
and the next active production-readiness milestone. Generated Meshes never
become source merely to shorten maintenance work.

## Contributor paths

Contributors can help through:

- compiler operations
- numerical geometry
- editor UX
- tests and fuzz cases
- sample assets
- documentation and diagrams
- FoldScript tooling
- AI adapters outside the core
- academic comparisons and benchmarks

## Format stability

Before FoldScript `1.0`, breaking schema changes are permitted but require migration notes. After `1.0`, breaking changes require a new major version and a documented converter where practical.
