# Codex bug-fix prompt

```text
Fix the supplied FoldCanvas bug without broad refactoring.

Before editing:

1. Read AGENTS.md and the relevant architecture/ADR documents.
2. Reproduce the bug with the smallest deterministic test.
3. Identify whether the fault is source validation, tessellation, operation mapping, seam topology, thickness, validation, Editor baking, or UI state.
4. Record the intended invariant.

Then:

- add a failing test
- implement the smallest root-cause fix
- run the affected test suite and broader Edit Mode tests
- inspect UV, vertex/index order, winding, diagnostics, and generated topology
- update documentation only when behavior or public contract changed

Do not hand-edit a generated mesh, suppress the diagnostic, reorder unrelated output, or weaken an assertion merely to make the test pass.

Final reply: reproduction, root cause, files changed, tests, before/after behavior, and remaining manual Unity checks.
```
