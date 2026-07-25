# M07: Geometry validator

## Goal

Turn geometry failure into precise compiler feedback suitable for humans and future AI repair.

## Required validators

- finite coordinates
- valid triangle indices
- zero-area triangles
- duplicate triangles
- edge incidence and non-manifold edges
- open boundaries/components
- inconsistent winding
- seam closure distance
- disconnected components
- broad-phase self-intersection candidate detection
- exact triangle intersection for reported candidates when practical

## Requirements

- deterministic diagnostic order
- validation levels Basic, Standard, Strict
- diagnostics identify panel, seam, operation, component, triangle, or edge where possible
- expensive strict checks can be disabled
- no silent repair in validator; repair is a separate explicit operation

## Adversarial fixtures

Include intentionally broken sources/meshes for:

- bow-tie vertex
- duplicate face
- inverted face
- open seam
- zero-length boundary
- self-intersecting roll
- thickness overlap

## Acceptance

Each fixture must produce its intended stable code without unrelated error floods obscuring the root cause.
