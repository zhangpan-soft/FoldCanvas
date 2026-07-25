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
