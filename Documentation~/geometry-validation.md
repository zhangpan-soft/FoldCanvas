# M07 geometry validation

M07 validates the final explicit geometry buffer after all enabled operations
and before a Unity `Mesh` is created. It reports evidence; it never deletes a
face, flips winding, closes a gap, moves a vertex, or substitutes another
shape. The 2D panels, seam graph, and ordered operations remain the only
authoritative source.

## Validation levels

`compile.validationLevel` controls cost, not whether earlier safety checks run.
Each level includes every check from the levels above it.

| Level | Contract |
|---|---|
| `basic` | Complete index triples, in-range triangle indices, finite positions, non-collapsed logical triangles, non-zero geometric area, duplicate topology triangles, non-manifold edge incidence, and local edge-winding conflicts. |
| `standard` | Basic plus logical-topology position agreement, bow-tie vertex fans, compiled-boundary length, executed Weld seam closure, closed-component global orientation, open-boundary warnings, and disconnected-component warnings. |
| `strict` | Standard plus deterministic broad-phase candidate generation and exact triangle-triangle intersection tests for non-adjacent topology. |

Open sheets and multi-part assets are valid FoldCanvas outputs. Standard and
Strict therefore report open boundaries and disconnected components as
warnings. A source that promises a closed solid must use Stitch/Solidify and
its operation-specific closed-volume gates in addition to the final report.

## Stable root-cause order

The validator stops after a fatal structural root cause so a duplicate face,
bad index, or collapsed triangle does not produce a flood of secondary edge
errors. Fatal precedence is:

1. incomplete or invalid triangle indices;
2. non-finite vertex positions;
3. collapsed or zero-area triangles;
4. duplicate topology triangles;
5. non-manifold edges or conflicting local winding;
6. Standard topology, boundary, seam, and closed-orientation failures;
7. Strict candidate-budget or confirmed-intersection failures.

Warnings are emitted only after the fatal Standard checks have passed.
Identical buffers and settings produce identical diagnostic, value, component,
edge, and triangle-pair order.

## Logical topology and components

Validation uses `TopologyVertexId`, not raw render indices. UV or provenance
splits may retain multiple render vertices while still representing one
logical point. Render copies with one topology identity must agree within
`weldEpsilon`; otherwise Standard returns `FC5016`.

Connected components are ordered by their minimum topology vertex ID. Edges
are ordered lexicographically by their two sorted topology IDs. A closed
component has no incidence-one edges. Its signed volume is evaluated in a
component-local frame for numerical stability; a negative non-zero volume is
an inward component and returns `FC5017`. Intentional zero-thickness sheets do
not fail this closed-component orientation rule.

Every executed Weld stores an immutable pair of final boundary sample indices
inside the compile transaction. Standard rechecks matching sample count and
maximum position gap after later allowed stages. Failed Stitch transactions
discard this evidence with the rest of their geometry state.

## Strict intersection contract

Strict validation performs a deterministic sweep along triangle AABB minimum
X. Surviving AABB pairs are sorted by `(triangleA, triangleB)` and evaluated
with separating-axis tests, including in-plane axes for coplanar triangles.
Pairs sharing any logical topology vertex are normal surface adjacency and are
not candidates.

The broad phase is capped at `250000` candidate pairs. Exceeding the cap
returns `FC5019`; FoldCanvas does not silently skip the remaining geometry.
A confirmed non-adjacent contact returns `FC5018`, records the first stable
triangle pair in the diagnostic context, and retains up to 1024 ordered pairs
in the report.

Strict validation detects overlap; it does not repair it. Bevel, subdivision,
smoothing, remeshing, cleanup, and collision-aware thickness adjustment are
outside M07.

## Report and diagnostic context

`FoldCanvasCompileResult.GeometryValidationReport` is available for every
compile that reaches final geometry validation, including a compile rejected
by that stage. Its read-only evidence includes:

- selected level and vertex/index/triangle counts;
- structural failure counts;
- logical edge, boundary, component, winding, and seam counts;
- ordered component and executed-Weld seam reports;
- Strict broad-phase/exact counts and ordered confirmed pairs;
- error/warning counts and `IsValid`.

`FoldCanvasDiagnostic` can additionally carry a boundary ID and a read-only
`FoldCanvasDiagnosticGeometryContext` containing render vertex, logical
vertex, component, triangle pair, or logical edge IDs. Ordered numeric values
and repair suggestions remain copied and read-only. These locations are
evidence for a human or a future AI source-repair loop, not permission to edit
the derived Mesh.

## Relationship to existing proof reports

M04.1's closed-volume report and M05's sphere report keep their specialized
contracts and validation timing. The M05 report by itself proves spherical
topology, radius, frame, poles, and winding. When the asset selects Strict,
the later M07 final report additionally proves that the final compiled
geometry has no confirmed non-adjacent triangle intersection under the M07
contract.

## Adversarial proof set

The Edit Mode suite includes deliberately malformed buffers for bad and
incomplete indices, NaN positions, zero area, duplicate faces, a non-manifold
edge, an inverted face, a bow-tie vertex, an open seam, a zero-length boundary,
disconnected components, a Weld gap, conflicting topology positions, an
inverted closed tetrahedron, a self-intersecting rolled sheet, and overlapping
thickness surfaces. Valid planar, production-cup, and explicit-gore sphere
assets are regression proofs against false positives.
