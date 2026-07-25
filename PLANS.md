# Execution plans

FoldCanvas geometry tasks can span many files and several validation passes. For any task expected to take more than a small isolated edit, create or update an execution plan in `Docs/Plans/active-plan.md` before implementation.

An execution plan must remain useful to a developer who did not see the conversation that created it.

## Required sections

```text
# Goal
# User-visible proof
# Scope
# Non-goals
# Files expected to change
# Geometry invariants
# Implementation steps
# Test matrix
# Risks and rollback
# Progress log
# Decisions made
# Final verification
```

## Plan rules

- Tie every implementation step to an acceptance criterion from the active milestone.
- Record coordinate-system, winding, boundary-order, and tolerance decisions explicitly.
- Update the progress log as work proceeds.
- When an unexpected architectural choice appears, stop and record it under `Decisions made` before coding further.
- A plan is not permission to implement future milestones.
- Delete no public API without documenting migration impact.
- The final plan must state what was verified in Unity and what was only statically inspected.
