# M20: Roll handedness review diagram

## Visible proof

1. One source-controlled SVG shows the source rectangle and the Roll-U and
   Roll-V circular mappings in the resolved current frame.
2. Positive and negative sweeps identify their minimum boundary, first-quarter
   direction, maximum boundary, cylinder axis, and radial orientation.
3. One bilingual guide maps every claim to the implemented equation and named
   Edit Mode tests.
4. The review distinguishes source-UV reading order from triangle winding and
   explains why a two-sided material cannot repair incorrect geometry.
5. A dependency-free validator rejects missing convention metadata, external
   assets, executable SVG content, and unmapped claims in deterministic order.

## Goal

Let a first-time FoldScript author predict where source U/V, signed Roll
angles, `CurrentNormal`, texture reading direction, and front-facing triangles
land on a cylinder without trial-and-error material changes.

## Scope

- GitHub issue #19;
- the implemented M03 Circular Roll equation and current-frame contract;
- one repository-native SVG and short English/Chinese review guide under
  `Docs/Community/GeometryReviews/`;
- deterministic standard-library validation and adversarial fixtures;
- repository-check integration and stable-package byte preservation.

## Non-goals

- compiler, Runtime, Editor, Unity tests, schema, FoldScript, operation,
  tolerance, winding, material, UV, topology, geometry, dependency, package
  version, or public-release change;
- a generated Mesh, raster render, external font, network-loaded asset,
  screenshot, marketing image, or attempt to make negative Roll outward;
- issues #21 or #26.

## Acceptance

- every sweep follows
  `theta = startAngleDegrees - t * angleDegrees`;
- Roll-U maps selected U around an axis parallel to `CurrentV`; Roll-V maps
  selected V around an axis parallel to `CurrentU`;
- at start angle zero, the selected minimum boundary begins on the negative
  selected-axis radial direction;
- positive quarter sweep advances to `-CurrentNormal`; negative quarter sweep
  advances to `+CurrentNormal`;
- positive full Roll has radial-outward winding and negative full Roll has the
  documented radial-inward orientation;
- source UV and boundary order remain authored; the canonical positive Roll-U
  exterior proof reads increasing source U left-to-right;
- a two-sided material is explicitly documented as visibility behavior, not a
  winding repair;
- SVG and guide contain no external content and pass deterministic validation;
- repository validation, package archive identity, workflow YAML, Python
  compilation, link checks, and `git diff --check` pass;
- hosted required checks are green at the exact audited head before merge.

## Architecture boundary

M20 is release-excluded review documentation and maintenance tooling. The 2D
canvas plus FoldScript remain source, Roll remains the existing deterministic
position mapping, and generated Meshes remain derived artifacts.

## Implementation status

Implementation is complete on `agent/m20-roll-handedness-diagram`. No external
agent CLAIM or open pull request existed when the maintainer selected issue #19
on 2026-08-12.

The repository-native SVG shows four signed sweeps and embeds the exact current
frame, selected axis, start/minimum, first-quarter, and radial-orientation
contract. The bilingual guide maps seven claims to the maintained formula,
executor, and named Edit Mode tests. Nine deterministic positive/adversarial
fixtures reject malformed XML, external raster/reference content, executable
SVG, external fonts, missing sweep/formula/test evidence, unreviewed claims,
unstable multi-errors, and input mutation.

The complete local repository matrix is green, visual rendering was inspected
at the SVG's native `1600 x 1180` viewBox, and the rebuilt stable archive
remains exactly
`16fc41fcdbe40861b19c9e928569ef84fadca347cd85b329ea835c6a59ab66e7`.
Hosted exact-head checks, audit, merge, and issue closure remain.
