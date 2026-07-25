# Codex review prompt

```text
Audit the current FoldCanvas branch as a skeptical geometry/compiler reviewer.

Read AGENTS.md, CURRENT_TASK.md, PLANS.md, architecture docs, relevant ADRs, implementation, and tests.

Do not modify code on the first pass. Produce a severity-ranked review focused on:

- violation of 2D-source authority
- nondeterministic output
- lost or altered canvas UVs
- Editor references in Runtime
- output order controlled by dictionary iteration
- silent unsupported-operation behavior
- incorrect winding or boundary order
- numeric instability and magic tolerances
- topology errors
- tests that only check screenshots or counts while missing geometry semantics
- claimed verification that was not actually run
- scope creep into future milestones

For every finding, cite exact file and line, explain a reproducible failure case, and propose the smallest compliant repair.

After the review, wait for explicit authorization before editing.
```
