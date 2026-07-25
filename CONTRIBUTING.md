# Contributing to FoldCanvas

FoldCanvas welcomes code, tests, mathematical notes, UX proposals, sample canvases, and adversarial geometry cases.

## Before opening a pull request

1. Read `README.md`, `AGENTS.md`, and `Documentation~/architecture.md`.
2. Search existing issues and ADRs.
3. Keep the change within one geometry concept whenever possible.
4. Add or update deterministic tests.
5. Include a source asset or numerical fixture that demonstrates the behavior.
6. Document failure modes, not only successful output.
7. Add a concise `CHANGELOG.md` entry for each package-version iteration, newest first.

## Pull request requirements

A geometry pull request should explain:

- the 2D source domain
- boundary ordering
- mapping into 3D
- UV preservation behavior
- expected winding and normal direction
- tolerance choices
- topology changes
- invalid-input diagnostics
- deterministic test coverage

Screenshots are useful, but they never replace mesh assertions.

## Coding conventions

- C# public APIs use PascalCase.
- Private serialized fields use `camelCase` without prefixes.
- Runtime assemblies never reference `UnityEditor`.
- Avoid LINQ and hidden allocations in compiler hot paths unless measurements justify them.
- Avoid global singletons.
- Avoid mutable static geometry state.
- Prefer explicit diagnostics to exceptions for user-authored invalid geometry.
- Exceptions remain appropriate for programmer errors and impossible internal states.

## Architecture changes

Create an ADR under `Docs/ADR/` before changing source authority, determinism guarantees, coordinate conventions, package dependencies, or the AI/provider boundary.

## New operation checklist

Every new FoldScript operation needs:

- schema definition
- C# source definition
- compiler implementation
- validation rules
- deterministic tests
- at least one sample
- documentation and a diagram
- diagnostic codes for invalid parameters

## License

By contributing, you agree that your contribution is licensed under Apache License 2.0.
