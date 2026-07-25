# FoldCanvas agent instructions

## Read order

Before editing, read:

1. `CURRENT_TASK.md`
2. `PLANS.md`
3. `Documentation~/architecture.md`
4. The milestone file referenced by `CURRENT_TASK.md`
5. Relevant ADRs under `Docs/ADR/`

Keep this file concise. Detailed requirements belong in the documents above.

## Non-negotiable rules

- The 2D canvas and FoldScript are source; generated `Mesh` assets are derived artifacts.
- Never replace the architecture with direct text-to-mesh, image-to-mesh, voxel, NeRF, Gaussian splat, or opaque model-generation APIs.
- Geometry compilation must be deterministic for identical inputs.
- Runtime code must not reference `UnityEditor`.
- Core package code must not depend on URP, HDRP, third-party libraries, or network services.
- Do not add a package dependency without an ADR and explicit task authorization.
- Preserve source canvas UVs through every operation.
- Unsupported or invalid geometry must return diagnostics, never silently degrade.
- Implement only the active milestone. Do not leap ahead into later operations.
- No broad refactors unrelated to the active acceptance criteria.
- New geometry behavior requires Edit Mode tests.
- Every package-version iteration updates `CHANGELOG.md` with the newest entry first.
- Public identifiers and code comments are English. User-facing documentation may be bilingual.

## Repository shape

The repository root is the UPM package. `Project~` is the local host project. Package code belongs in `Runtime`, `Editor`, and `Tests`. Do not place package implementation under `Project~/Assets`.

## Validation

Preferred validation order:

1. Parse all changed JSON files.
2. Inspect assembly-definition references.
3. Run Unity Edit Mode tests when a compatible Editor is available.
4. Run any milestone-specific scene or mesh verification.
5. Report what could not be run.

Do not claim Unity compilation or visual correctness unless it was actually verified.

## Completion report

End each task with:

- files changed
- behavior implemented
- tests or checks run
- diagnostics/error cases covered
- remaining manual Unity checks
- explicit statement that later milestones were not implemented
