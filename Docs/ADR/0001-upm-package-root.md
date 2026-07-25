# ADR 0001: Repository root is the UPM package

- Status: Accepted
- Date: 2026-07-25

## Context

FoldCanvas is intended to be installed into many Unity game projects and developed publicly on GitHub.

## Decision

The repository root is a Unity Package Manager package. A local host project lives under `Project~` and references the root with a file dependency.

## Consequences

- Git tags map directly to package versions.
- Runtime, Editor, Tests, Samples, and package documentation follow Unity package conventions.
- Package implementation must not be placed under the host project's `Assets` folder.
- The host project remains disposable and minimal.
