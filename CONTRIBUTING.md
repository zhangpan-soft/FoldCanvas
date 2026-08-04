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

Every merge records an audit against the exact pull-request head. A later push
invalidates the earlier decision. Required repository, Unity, archive,
clean-install, and milestone-specific checks must be green with real evidence;
a workflow that did not start Unity is not a passing Unity check.

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

For a native M10 position-only operation, start from
`Samples~/OperationExtension` and read
`Documentation~/extensibility.md`. Register the exact definition type
explicitly for one compile. Do not add assembly scanning, a global registry,
an opaque FoldScript `0.1` payload, or access to triangles/topology/boundaries
through the extension API. A request that needs those capabilities is a core
architecture proposal and requires an ADR plus a future milestone.

Sample-gallery entries must use the versioned manifest and normalized
`Samples~/` paths. Optional exporters consume immutable compiled data and keep
file I/O in the Editor layer.

## License

By contributing, you agree that your contribution is licensed under Apache License 2.0.
