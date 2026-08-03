using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public sealed class FoldCanvasGeometryComponentReport
    {
        internal FoldCanvasGeometryComponentReport(
            int componentIndex,
            int minimumTopologyVertexId,
            int topologyVertexCount,
            int triangleCount,
            int uniqueEdgeCount,
            int openEdgeCount,
            double signedVolume,
            double minimumAcceptedAbsoluteVolume)
        {
            ComponentIndex = componentIndex;
            MinimumTopologyVertexId = minimumTopologyVertexId;
            TopologyVertexCount = topologyVertexCount;
            TriangleCount = triangleCount;
            UniqueEdgeCount = uniqueEdgeCount;
            OpenEdgeCount = openEdgeCount;
            SignedVolume = signedVolume;
            MinimumAcceptedAbsoluteVolume =
                minimumAcceptedAbsoluteVolume;
        }

        public int ComponentIndex { get; }

        public int MinimumTopologyVertexId { get; }

        public int TopologyVertexCount { get; }

        public int TriangleCount { get; }

        public int UniqueEdgeCount { get; }

        public int OpenEdgeCount { get; }

        public double SignedVolume { get; }

        public double AbsoluteVolume => Math.Abs(SignedVolume);

        public double MinimumAcceptedAbsoluteVolume { get; }

        public bool IsClosed =>
            TriangleCount > 0 &&
            UniqueEdgeCount > 0 &&
            OpenEdgeCount == 0;

        public bool HasNonZeroVolume =>
            AbsoluteVolume > MinimumAcceptedAbsoluteVolume;

        public bool IsInverted =>
            IsClosed &&
            HasNonZeroVolume &&
            SignedVolume < 0d;
    }

    public sealed class FoldCanvasGeometrySeamReport
    {
        internal FoldCanvasGeometrySeamReport(
            string operationId,
            string seamId,
            int sampleCount,
            double maximumGap,
            double tolerance,
            bool hasMatchingSampleCount)
        {
            OperationId = operationId;
            SeamId = seamId;
            SampleCount = sampleCount;
            MaximumGap = maximumGap;
            Tolerance = tolerance;
            HasMatchingSampleCount = hasMatchingSampleCount;
        }

        public string OperationId { get; }

        public string SeamId { get; }

        public int SampleCount { get; }

        public double MaximumGap { get; }

        public double Tolerance { get; }

        public bool HasMatchingSampleCount { get; }

        public bool IsClosedWithinTolerance =>
            HasMatchingSampleCount &&
            MaximumGap <= Tolerance;
    }

    public sealed class FoldCanvasTriangleIntersectionRecord
    {
        internal FoldCanvasTriangleIntersectionRecord(
            int triangleA,
            int triangleB,
            string panelIdA,
            string panelIdB)
        {
            TriangleA = triangleA;
            TriangleB = triangleB;
            PanelIdA = panelIdA;
            PanelIdB = panelIdB;
        }

        public int TriangleA { get; }

        public int TriangleB { get; }

        public string PanelIdA { get; }

        public string PanelIdB { get; }
    }

    public sealed class FoldCanvasGeometryValidationReport
    {
        private readonly List<FoldCanvasGeometryComponentReport>
            components =
                new List<FoldCanvasGeometryComponentReport>();

        private readonly ReadOnlyCollection<
            FoldCanvasGeometryComponentReport> readOnlyComponents;

        private readonly List<FoldCanvasGeometrySeamReport> seams =
            new List<FoldCanvasGeometrySeamReport>();

        private readonly ReadOnlyCollection<FoldCanvasGeometrySeamReport>
            readOnlySeams;

        private readonly List<FoldCanvasTriangleIntersectionRecord>
            intersections =
                new List<FoldCanvasTriangleIntersectionRecord>();

        private readonly ReadOnlyCollection<
            FoldCanvasTriangleIntersectionRecord> readOnlyIntersections;

        internal FoldCanvasGeometryValidationReport(
            FoldCanvasValidationLevel validationLevel,
            int vertexCount,
            int triangleIndexCount)
        {
            ValidationLevel = validationLevel;
            VertexCount = vertexCount;
            TriangleIndexCount = triangleIndexCount;
            TriangleCount = triangleIndexCount / 3;
            IsValid = true;
            readOnlyComponents = components.AsReadOnly();
            readOnlySeams = seams.AsReadOnly();
            readOnlyIntersections = intersections.AsReadOnly();
        }

        public FoldCanvasValidationLevel ValidationLevel { get; }

        public int VertexCount { get; }

        public int TriangleIndexCount { get; }

        public int TriangleCount { get; }

        public int NonFiniteVertexCount { get; internal set; }

        public int InvalidTriangleIndexCount { get; internal set; }

        public bool HasIncompleteTriangleIndexBuffer { get; internal set; }

        public int CollapsedTopologyTriangleCount { get; internal set; }

        public int ZeroAreaTriangleCount { get; internal set; }

        public int DuplicateTriangleCount { get; internal set; }

        public int UniqueTopologyEdgeCount { get; internal set; }

        public int OpenEdgeCount { get; internal set; }

        public int OpenBoundaryCount { get; internal set; }

        public int NonManifoldEdgeCount { get; internal set; }

        public int OrientationConflictEdgeCount { get; internal set; }

        public int TopologyPositionConflictCount { get; internal set; }

        public int BowTieTopologyVertexCount { get; internal set; }

        public int DegenerateBoundaryCount { get; internal set; }

        public int InvertedClosedComponentCount { get; internal set; }

        public int SeamMismatchCount { get; internal set; }

        public int BroadPhaseCandidatePairCount { get; internal set; }

        public int ExactIntersectionPairCount { get; internal set; }

        public bool StrictChecksExecuted { get; internal set; }

        public bool StrictCandidateBudgetExceeded { get; internal set; }

        public int ErrorCount { get; internal set; }

        public int WarningCount { get; internal set; }

        public bool IsValid { get; internal set; }

        public int ComponentCount => components.Count;

        public int DisconnectedComponentCount =>
            Math.Max(0, ComponentCount - 1);

        public IReadOnlyList<FoldCanvasGeometryComponentReport>
            Components => readOnlyComponents;

        public IReadOnlyList<FoldCanvasGeometrySeamReport> Seams =>
            readOnlySeams;

        public IReadOnlyList<FoldCanvasTriangleIntersectionRecord>
            Intersections => readOnlyIntersections;

        internal void AddComponent(
            FoldCanvasGeometryComponentReport component)
        {
            components.Add(component);
        }

        internal void AddSeam(FoldCanvasGeometrySeamReport seam)
        {
            seams.Add(seam);
        }

        internal void AddIntersection(
            FoldCanvasTriangleIntersectionRecord intersection)
        {
            intersections.Add(intersection);
        }
    }

    internal static class FoldCanvasGeometryValidator
    {
        private const int MaximumRecordedIntersections = 1024;

        public static FoldCanvasGeometryValidationReport Validate(
            MeshBuildBuffer buffer,
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            FoldCanvasValidationLevel level =
                asset?.CompileSettings != null
                    ? asset.CompileSettings.ValidationLevel
                    : FoldCanvasValidationLevel.Basic;
            float tolerance =
                asset?.CompileSettings != null
                    ? asset.CompileSettings.WeldEpsilon
                    : buffer.PositionTolerance;
            if (!FiniteMath.IsFinite(tolerance) || tolerance <= 0f)
            {
                tolerance = FoldCanvasCompileSettings.DefaultWeldEpsilon;
            }

            FoldCanvasGeometryValidationReport report =
                new FoldCanvasGeometryValidationReport(
                    level,
                    buffer.Vertices.Count,
                    buffer.Triangles.Count);

            if (buffer.Triangles.Count % 3 != 0)
            {
                report.HasIncompleteTriangleIndexBuffer = true;
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .IncompleteTriangleIndexBuffer,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Generated triangle indices do not form complete triples.",
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "triangleIndexCount",
                                buffer.Triangles.Count),
                            new FoldCanvasDiagnosticValue(
                                "remainder",
                                buffer.Triangles.Count % 3)
                        }));
                return report;
            }

            int firstInvalidOffset = -1;
            for (int i = 0; i < buffer.Triangles.Count; i++)
            {
                int vertexIndex = buffer.Triangles[i];
                if (vertexIndex >= 0 &&
                    vertexIndex < buffer.Vertices.Count)
                {
                    continue;
                }

                report.InvalidTriangleIndexCount++;
                if (firstInvalidOffset < 0)
                {
                    firstInvalidOffset = i;
                }
            }

            if (report.InvalidTriangleIndexCount > 0)
            {
                int invalidVertexIndex =
                    buffer.Triangles[firstInvalidOffset];
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidTriangleIndex,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Generated triangle indices must reference an existing vertex.",
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "invalidIndexCount",
                                report.InvalidTriangleIndexCount),
                            new FoldCanvasDiagnosticValue(
                                "invalidVertexIndex",
                                invalidVertexIndex),
                            new FoldCanvasDiagnosticValue(
                                "vertexCount",
                                buffer.Vertices.Count)
                        },
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                vertexIndex: invalidVertexIndex >= 0
                                    ? invalidVertexIndex
                                    : -1,
                                triangleIndex: firstInvalidOffset / 3)));
                return report;
            }

            int firstNonFiniteVertex = -1;
            for (int i = 0; i < buffer.Vertices.Count; i++)
            {
                if (FiniteMath.IsFinite(buffer.Vertices[i].Position))
                {
                    continue;
                }

                report.NonFiniteVertexCount++;
                if (firstNonFiniteVertex < 0)
                {
                    firstNonFiniteVertex = i;
                }
            }

            if (report.NonFiniteVertexCount > 0)
            {
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonFiniteVertex,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Generated geometry contains NaN or infinity coordinates.",
                        PanelIdForVertex(buffer, firstNonFiniteVertex),
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "nonFiniteVertexCount",
                                report.NonFiniteVertexCount)
                        },
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                vertexIndex: firstNonFiniteVertex,
                                topologyVertexId:
                                    buffer.GetTopologyId(
                                        firstNonFiniteVertex))));
                return report;
            }

            Dictionary<int, Vector3> topologyPositions =
                BuildTopologyPositions(
                    buffer,
                    tolerance,
                    out List<int> topologyPositionConflicts);
            report.TopologyPositionConflictCount =
                topologyPositionConflicts.Count;

            List<TriangleRecord> triangles =
                BuildTriangles(
                    buffer,
                    report,
                    out int firstCollapsedTriangle,
                    out int firstZeroAreaTriangle,
                    out TrianglePair firstDuplicatePair);

            if (report.CollapsedTopologyTriangleCount > 0)
            {
                TriangleRecord triangle = triangles[firstCollapsedTriangle];
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonManifoldTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Welded topology collapses a generated triangle.",
                        triangle.PanelId,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "collapsedTriangleCount",
                                report.CollapsedTopologyTriangleCount)
                        },
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                triangleIndex: triangle.TriangleIndex)));
                return report;
            }

            if (report.ZeroAreaTriangleCount > 0)
            {
                TriangleRecord triangle = triangles[firstZeroAreaTriangle];
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.ZeroAreaTriangle,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Generated geometry contains a zero or near-zero area triangle.",
                        triangle.PanelId,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "zeroAreaTriangleCount",
                                report.ZeroAreaTriangleCount)
                        },
                        repairSuggestions: new[]
                        {
                            "Adjust the source panel or deformation so every emitted triangle has finite area."
                        },
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                triangleIndex: triangle.TriangleIndex)));
                return report;
            }

            if (report.DuplicateTriangleCount > 0)
            {
                TriangleRecord triangle =
                    triangles[firstDuplicatePair.Second];
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.DuplicateTriangle,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Generated geometry contains duplicate topology triangles.",
                        triangle.PanelId,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "duplicateTriangleCount",
                                report.DuplicateTriangleCount)
                        },
                        repairSuggestions: new[]
                        {
                            "Remove the source or operation path that emits the face twice; the validator will not delete it automatically."
                        },
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                triangleIndex:
                                    firstDuplicatePair.First,
                                relatedTriangleIndex:
                                    firstDuplicatePair.Second)));
                return report;
            }

            Dictionary<EdgeKey, EdgeUse> edges = BuildEdges(triangles);
            List<EdgeKey> orderedEdgeKeys =
                new List<EdgeKey>(edges.Keys);
            orderedEdgeKeys.Sort();
            EdgeKey firstOpenEdge = default;
            EdgeKey firstNonManifoldEdge = default;
            EdgeKey firstOrientationConflict = default;
            bool hasOpenEdge = false;
            bool hasNonManifoldEdge = false;
            bool hasOrientationConflict = false;
            for (int i = 0; i < orderedEdgeKeys.Count; i++)
            {
                EdgeKey key = orderedEdgeKeys[i];
                EdgeUse use = edges[key];
                if (use.Incidents.Count == 1)
                {
                    report.OpenEdgeCount++;
                    if (!hasOpenEdge)
                    {
                        firstOpenEdge = key;
                        hasOpenEdge = true;
                    }
                }
                else if (use.Incidents.Count > 2)
                {
                    report.NonManifoldEdgeCount++;
                    if (!hasNonManifoldEdge)
                    {
                        firstNonManifoldEdge = key;
                        hasNonManifoldEdge = true;
                    }
                }
                else if (use.Incidents.Count == 2 &&
                    use.Incidents[0].Direction ==
                    use.Incidents[1].Direction)
                {
                    report.OrientationConflictEdgeCount++;
                    if (!hasOrientationConflict)
                    {
                        firstOrientationConflict = key;
                        hasOrientationConflict = true;
                    }
                }
            }

            report.UniqueTopologyEdgeCount = edges.Count;
            BuildComponents(
                triangles,
                edges,
                orderedEdgeKeys,
                topologyPositions,
                report,
                out Dictionary<int, int> componentByTopologyId);
            report.OpenBoundaryCount = CountOpenBoundaries(
                edges,
                orderedEdgeKeys);

            if (report.NonManifoldEdgeCount > 0)
            {
                EdgeUse use = edges[firstNonManifoldEdge];
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonManifoldTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        "A topology edge has more than two incident triangles.",
                        PanelIdForTriangle(
                            triangles,
                            use.Incidents[0].TriangleIndex),
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "nonManifoldEdgeCount",
                                report.NonManifoldEdgeCount),
                            new FoldCanvasDiagnosticValue(
                                "incidentTriangleCount",
                                use.Incidents.Count)
                        },
                        geometryContext: EdgeContext(
                            firstNonManifoldEdge,
                            componentByTopologyId,
                            use.Incidents[0].TriangleIndex)));
                return report;
            }

            if (report.OrientationConflictEdgeCount > 0)
            {
                EdgeUse use = edges[firstOrientationConflict];
                AddError(
                    report,
                    result,
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InconsistentWinding,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Two incident triangles traverse the same topology edge in the same direction.",
                        PanelIdForTriangle(
                            triangles,
                            use.Incidents[0].TriangleIndex),
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "orientationConflictEdgeCount",
                                report.OrientationConflictEdgeCount)
                        },
                        repairSuggestions: new[]
                        {
                            "Correct the source operation winding; the validator will not flip faces automatically."
                        },
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                componentIndex: ComponentIndexFor(
                                    componentByTopologyId,
                                    firstOrientationConflict.A),
                                triangleIndex:
                                    use.Incidents[0].TriangleIndex,
                                relatedTriangleIndex:
                                    use.Incidents[1].TriangleIndex,
                                edgeTopologyVertexA:
                                    firstOrientationConflict.A,
                                edgeTopologyVertexB:
                                    firstOrientationConflict.B)));
                return report;
            }

            if (level >= FoldCanvasValidationLevel.Standard)
            {
                if (report.TopologyPositionConflictCount > 0)
                {
                    int topologyId = topologyPositionConflicts[0];
                    AddError(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .TopologyPositionConflict,
                            FoldCanvasDiagnosticSeverity.Error,
                            "Render vertices sharing one topology identity do not share one position.",
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "topologyPositionConflictCount",
                                    report.TopologyPositionConflictCount),
                                new FoldCanvasDiagnosticValue(
                                    "positionTolerance",
                                    tolerance,
                                    "m")
                            },
                            geometryContext:
                                new FoldCanvasDiagnosticGeometryContext(
                                    topologyVertexId: topologyId,
                                    componentIndex: ComponentIndexFor(
                                        componentByTopologyId,
                                        topologyId))));
                    return report;
                }

                report.BowTieTopologyVertexCount = CountBowTieVertices(
                    triangles,
                    topologyPositions,
                    out int firstBowTieTopologyId,
                    out int firstBowTieFanCount);
                if (report.BowTieTopologyVertexCount > 0)
                {
                    AddError(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .BowTieTopologyVertex,
                            FoldCanvasDiagnosticSeverity.Error,
                            "A topology vertex contains multiple disconnected incident-face fans.",
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "bowTieTopologyVertexCount",
                                    report.BowTieTopologyVertexCount),
                                new FoldCanvasDiagnosticValue(
                                    "incidentFanCount",
                                    firstBowTieFanCount)
                            },
                            geometryContext:
                                new FoldCanvasDiagnosticGeometryContext(
                                    topologyVertexId:
                                        firstBowTieTopologyId,
                                    componentIndex: ComponentIndexFor(
                                        componentByTopologyId,
                                        firstBowTieTopologyId))));
                    return report;
                }

                ValidateBoundaries(
                    buffer,
                    tolerance,
                    report,
                    out PanelBuildRecord firstDegeneratePanel,
                    out BoundaryBuildRecord firstDegenerateBoundary,
                    out double firstDegenerateLength);
                if (report.DegenerateBoundaryCount > 0)
                {
                    AddError(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .DegenerateCompiledBoundary,
                            FoldCanvasDiagnosticSeverity.Error,
                            "A compiled boundary has zero or near-zero spatial length.",
                            firstDegeneratePanel.PanelId,
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "degenerateBoundaryCount",
                                    report.DegenerateBoundaryCount),
                                new FoldCanvasDiagnosticValue(
                                    "boundaryLength",
                                    firstDegenerateLength,
                                    "m"),
                                new FoldCanvasDiagnosticValue(
                                    "positionTolerance",
                                    tolerance,
                                    "m")
                            },
                            boundaryId: firstDegenerateBoundary.Id));
                    return report;
                }

                ValidateWeldSeams(
                    buffer,
                    tolerance,
                    report,
                    out FoldCanvasGeometrySeamReport firstMismatchedSeam,
                    out int firstMismatchedVertexIndex);
                if (report.SeamMismatchCount > 0)
                {
                    int topologyId = firstMismatchedVertexIndex >= 0
                        ? buffer.GetTopologyId(
                            firstMismatchedVertexIndex)
                        : -1;
                    AddError(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes.SeamClosureMismatch,
                            FoldCanvasDiagnosticSeverity.Error,
                            "An executed Weld seam is not closed within the configured tolerance.",
                            firstMismatchedVertexIndex >= 0
                                ? PanelIdForVertex(
                                    buffer,
                                    firstMismatchedVertexIndex)
                                : null,
                            firstMismatchedSeam.OperationId,
                            firstMismatchedSeam.SeamId,
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "seamMismatchCount",
                                    report.SeamMismatchCount),
                                new FoldCanvasDiagnosticValue(
                                    "maximumGap",
                                    firstMismatchedSeam.MaximumGap,
                                    "m"),
                                new FoldCanvasDiagnosticValue(
                                    "positionTolerance",
                                    tolerance,
                                    "m"),
                                new FoldCanvasDiagnosticValue(
                                    "sampleCount",
                                    firstMismatchedSeam.SampleCount)
                            },
                            geometryContext:
                                new FoldCanvasDiagnosticGeometryContext(
                                    vertexIndex:
                                        firstMismatchedVertexIndex,
                                    topologyVertexId: topologyId,
                                    componentIndex: ComponentIndexFor(
                                        componentByTopologyId,
                                        topologyId))));
                    return report;
                }

                FoldCanvasGeometryComponentReport firstInverted = null;
                for (int i = 0; i < report.Components.Count; i++)
                {
                    if (!report.Components[i].IsInverted)
                    {
                        continue;
                    }

                    report.InvertedClosedComponentCount++;
                    if (firstInverted == null)
                    {
                        firstInverted = report.Components[i];
                    }
                }

                if (report.InvertedClosedComponentCount > 0)
                {
                    AddError(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .InvertedClosedComponent,
                            FoldCanvasDiagnosticSeverity.Error,
                            "A closed component has inward global orientation.",
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "invertedClosedComponentCount",
                                    report.InvertedClosedComponentCount),
                                new FoldCanvasDiagnosticValue(
                                    "signedVolume",
                                    firstInverted.SignedVolume,
                                    "m3"),
                                new FoldCanvasDiagnosticValue(
                                    "minimumAcceptedAbsoluteVolume",
                                    firstInverted
                                        .MinimumAcceptedAbsoluteVolume,
                                    "m3")
                            },
                            repairSuggestions: new[]
                            {
                                "Correct the source winding; the validator will not reverse the component automatically."
                            },
                            geometryContext:
                                new FoldCanvasDiagnosticGeometryContext(
                                    topologyVertexId: firstInverted
                                        .MinimumTopologyVertexId,
                                    componentIndex:
                                        firstInverted.ComponentIndex)));
                    return report;
                }

                if (report.OpenEdgeCount > 0)
                {
                    EdgeUse use = edges[firstOpenEdge];
                    AddWarning(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .OpenTopologyBoundary,
                            FoldCanvasDiagnosticSeverity.Warning,
                            "Generated geometry contains open topology boundaries. Intentional sheets remain valid, but closed-volume assets should Stitch or Solidify them explicitly.",
                            PanelIdForTriangle(
                                triangles,
                                use.Incidents[0].TriangleIndex),
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "openEdgeCount",
                                    report.OpenEdgeCount),
                                new FoldCanvasDiagnosticValue(
                                    "openBoundaryCount",
                                    report.OpenBoundaryCount)
                            },
                            geometryContext: EdgeContext(
                                firstOpenEdge,
                                componentByTopologyId,
                                use.Incidents[0].TriangleIndex)));
                }

                if (report.ComponentCount > 1)
                {
                    FoldCanvasGeometryComponentReport second =
                        report.Components[1];
                    AddWarning(
                        report,
                        result,
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .DisconnectedGeometry,
                            FoldCanvasDiagnosticSeverity.Warning,
                            "Generated geometry contains multiple disconnected topology components.",
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "componentCount",
                                    report.ComponentCount),
                                new FoldCanvasDiagnosticValue(
                                    "disconnectedComponentCount",
                                    report.DisconnectedComponentCount)
                            },
                            geometryContext:
                                new FoldCanvasDiagnosticGeometryContext(
                                    topologyVertexId:
                                        second.MinimumTopologyVertexId,
                                    componentIndex:
                                        second.ComponentIndex)));
                }
            }

            if (level >= FoldCanvasValidationLevel.Strict)
            {
                ValidateStrictIntersections(
                    triangles,
                    tolerance,
                    report,
                    result);
            }

            return report;
        }

        private static List<TriangleRecord> BuildTriangles(
            MeshBuildBuffer buffer,
            FoldCanvasGeometryValidationReport report,
            out int firstCollapsedTriangle,
            out int firstZeroAreaTriangle,
            out TrianglePair firstDuplicatePair)
        {
            int triangleCount = buffer.Triangles.Count / 3;
            List<TriangleRecord> triangles =
                new List<TriangleRecord>(triangleCount);
            Dictionary<TriangleKey, int> firstTriangleByKey =
                new Dictionary<TriangleKey, int>();
            firstCollapsedTriangle = -1;
            firstZeroAreaTriangle = -1;
            firstDuplicatePair = default;
            bool hasDuplicatePair = false;
            for (int triangleIndex = 0;
                triangleIndex < triangleCount;
                triangleIndex++)
            {
                int offset = triangleIndex * 3;
                int aIndex = buffer.Triangles[offset];
                int bIndex = buffer.Triangles[offset + 1];
                int cIndex = buffer.Triangles[offset + 2];
                int topologyA = buffer.GetTopologyId(aIndex);
                int topologyB = buffer.GetTopologyId(bIndex);
                int topologyC = buffer.GetTopologyId(cIndex);
                Vector3 a = buffer.Vertices[aIndex].Position;
                Vector3 b = buffer.Vertices[bIndex].Position;
                Vector3 c = buffer.Vertices[cIndex].Position;
                TriangleRecord triangle = new TriangleRecord(
                    triangleIndex,
                    aIndex,
                    bIndex,
                    cIndex,
                    topologyA,
                    topologyB,
                    topologyC,
                    a,
                    b,
                    c,
                    PanelIdForTriangleIndices(
                        buffer,
                        aIndex,
                        bIndex,
                        cIndex));
                triangles.Add(triangle);

                if (topologyA == topologyB ||
                    topologyB == topologyC ||
                    topologyC == topologyA)
                {
                    report.CollapsedTopologyTriangleCount++;
                    if (firstCollapsedTriangle < 0)
                    {
                        firstCollapsedTriangle = triangleIndex;
                    }

                    continue;
                }

                if (Vector3.Cross(b - a, c - a).sqrMagnitude <=
                    FoldCanvasGeometryTolerances
                        .MinimumTriangleDoubleAreaSquared)
                {
                    report.ZeroAreaTriangleCount++;
                    if (firstZeroAreaTriangle < 0)
                    {
                        firstZeroAreaTriangle = triangleIndex;
                    }

                    continue;
                }

                TriangleKey key = new TriangleKey(
                    topologyA,
                    topologyB,
                    topologyC);
                if (firstTriangleByKey.TryGetValue(
                        key,
                        out int firstTriangle))
                {
                    report.DuplicateTriangleCount++;
                    if (!hasDuplicatePair)
                    {
                        firstDuplicatePair = new TrianglePair(
                            firstTriangle,
                            triangleIndex);
                        hasDuplicatePair = true;
                    }
                }
                else
                {
                    firstTriangleByKey.Add(key, triangleIndex);
                }
            }

            return triangles;
        }

        private static Dictionary<int, Vector3> BuildTopologyPositions(
            MeshBuildBuffer buffer,
            float tolerance,
            out List<int> conflicts)
        {
            Dictionary<int, Vector3> positions =
                new Dictionary<int, Vector3>();
            HashSet<int> conflictSet = new HashSet<int>();
            float toleranceSquared = tolerance * tolerance;
            for (int i = 0; i < buffer.Vertices.Count; i++)
            {
                int topologyId = buffer.GetTopologyId(i);
                Vector3 position = buffer.Vertices[i].Position;
                if (positions.TryGetValue(
                        topologyId,
                        out Vector3 existing))
                {
                    if ((existing - position).sqrMagnitude >
                        toleranceSquared)
                    {
                        conflictSet.Add(topologyId);
                    }
                }
                else
                {
                    positions.Add(topologyId, position);
                }
            }

            conflicts = new List<int>(conflictSet);
            conflicts.Sort();
            return positions;
        }

        private static Dictionary<EdgeKey, EdgeUse> BuildEdges(
            IReadOnlyList<TriangleRecord> triangles)
        {
            Dictionary<EdgeKey, EdgeUse> edges =
                new Dictionary<EdgeKey, EdgeUse>();
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleRecord triangle = triangles[i];
                AddEdge(
                    edges,
                    triangle.TopologyA,
                    triangle.TopologyB,
                    triangle.TriangleIndex);
                AddEdge(
                    edges,
                    triangle.TopologyB,
                    triangle.TopologyC,
                    triangle.TriangleIndex);
                AddEdge(
                    edges,
                    triangle.TopologyC,
                    triangle.TopologyA,
                    triangle.TriangleIndex);
            }

            return edges;
        }

        private static void AddEdge(
            Dictionary<EdgeKey, EdgeUse> edges,
            int from,
            int to,
            int triangleIndex)
        {
            EdgeKey key = new EdgeKey(from, to);
            int direction = from == key.A ? 1 : -1;
            if (!edges.TryGetValue(key, out EdgeUse use))
            {
                use = new EdgeUse();
                edges.Add(key, use);
            }

            use.Incidents.Add(
                new EdgeIncident(triangleIndex, direction));
        }

        private static void BuildComponents(
            IReadOnlyList<TriangleRecord> triangles,
            IReadOnlyDictionary<EdgeKey, EdgeUse> edges,
            IReadOnlyList<EdgeKey> orderedEdgeKeys,
            IReadOnlyDictionary<int, Vector3> topologyPositions,
            FoldCanvasGeometryValidationReport report,
            out Dictionary<int, int> componentByTopologyId)
        {
            TopologyUnionFind unionFind = new TopologyUnionFind();
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleRecord triangle = triangles[i];
                unionFind.Ensure(triangle.TopologyA);
                unionFind.Ensure(triangle.TopologyB);
                unionFind.Ensure(triangle.TopologyC);
                unionFind.Union(
                    triangle.TopologyA,
                    triangle.TopologyB);
                unionFind.Union(
                    triangle.TopologyB,
                    triangle.TopologyC);
            }

            Dictionary<int, ComponentAccumulator> accumulators =
                new Dictionary<int, ComponentAccumulator>();
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleRecord triangle = triangles[i];
                int root = unionFind.Find(triangle.TopologyA);
                if (!accumulators.TryGetValue(
                        root,
                        out ComponentAccumulator accumulator))
                {
                    accumulator = new ComponentAccumulator();
                    accumulators.Add(root, accumulator);
                }

                accumulator.AddTriangle(triangle);
            }

            for (int i = 0; i < orderedEdgeKeys.Count; i++)
            {
                EdgeKey key = orderedEdgeKeys[i];
                int root = unionFind.Find(key.A);
                if (accumulators.TryGetValue(
                        root,
                        out ComponentAccumulator accumulator))
                {
                    accumulator.UniqueEdgeCount++;
                    if (edges[key].Incidents.Count == 1)
                    {
                        accumulator.OpenEdgeCount++;
                    }
                }
            }

            List<ComponentAccumulator> ordered =
                new List<ComponentAccumulator>(accumulators.Values);
            ordered.Sort((left, right) =>
                left.MinimumTopologyVertexId.CompareTo(
                    right.MinimumTopologyVertexId));
            componentByTopologyId = new Dictionary<int, int>();
            for (int componentIndex = 0;
                componentIndex < ordered.Count;
                componentIndex++)
            {
                ComponentAccumulator accumulator = ordered[componentIndex];
                int referenceId = accumulator.MinimumTopologyVertexId;
                Vector3 reference = topologyPositions[referenceId];
                Bounds bounds = new Bounds(reference, Vector3.zero);
                foreach (int topologyId in accumulator.TopologyVertexIds)
                {
                    bounds.Encapsulate(topologyPositions[topologyId]);
                    componentByTopologyId[topologyId] = componentIndex;
                }

                double signedVolume = 0d;
                for (int triangleIndex = 0;
                    triangleIndex < accumulator.Triangles.Count;
                    triangleIndex++)
                {
                    TriangleRecord triangle =
                        accumulator.Triangles[triangleIndex];
                    Vector3 a =
                        topologyPositions[triangle.TopologyA] - reference;
                    Vector3 b =
                        topologyPositions[triangle.TopologyB] - reference;
                    Vector3 c =
                        topologyPositions[triangle.TopologyC] - reference;
                    signedVolume += SignedTetrahedronVolume(a, b, c);
                }

                double scale = Math.Max(
                    bounds.size.x,
                    Math.Max(bounds.size.y, bounds.size.z));
                double volumeTolerance = Math.Max(
                    FoldCanvasGeometryTolerances.MinimumClosedVolume,
                    scale * scale * scale *
                    FoldCanvasGeometryTolerances
                        .RelativeClosedVolumeTolerance);
                report.AddComponent(
                    new FoldCanvasGeometryComponentReport(
                        componentIndex,
                        accumulator.MinimumTopologyVertexId,
                        accumulator.TopologyVertexIds.Count,
                        accumulator.Triangles.Count,
                        accumulator.UniqueEdgeCount,
                        accumulator.OpenEdgeCount,
                        signedVolume,
                        volumeTolerance));
            }
        }

        private static int CountOpenBoundaries(
            IReadOnlyDictionary<EdgeKey, EdgeUse> edges,
            IReadOnlyList<EdgeKey> orderedEdgeKeys)
        {
            TopologyUnionFind openEdges = new TopologyUnionFind();
            for (int i = 0; i < orderedEdgeKeys.Count; i++)
            {
                EdgeKey key = orderedEdgeKeys[i];
                if (edges[key].Incidents.Count != 1)
                {
                    continue;
                }

                openEdges.Ensure(key.A);
                openEdges.Ensure(key.B);
                openEdges.Union(key.A, key.B);
            }

            return openEdges.RootCount;
        }

        private static int CountBowTieVertices(
            IReadOnlyList<TriangleRecord> triangles,
            IReadOnlyDictionary<int, Vector3> topologyPositions,
            out int firstBowTieTopologyId,
            out int firstBowTieFanCount)
        {
            Dictionary<int, List<TriangleRecord>> incident =
                new Dictionary<int, List<TriangleRecord>>();
            for (int i = 0; i < triangles.Count; i++)
            {
                AddIncidentTriangle(
                    incident,
                    triangles[i].TopologyA,
                    triangles[i]);
                AddIncidentTriangle(
                    incident,
                    triangles[i].TopologyB,
                    triangles[i]);
                AddIncidentTriangle(
                    incident,
                    triangles[i].TopologyC,
                    triangles[i]);
            }

            List<int> topologyIds =
                new List<int>(topologyPositions.Keys);
            topologyIds.Sort();
            int count = 0;
            firstBowTieTopologyId = -1;
            firstBowTieFanCount = 0;
            for (int i = 0; i < topologyIds.Count; i++)
            {
                int topologyId = topologyIds[i];
                if (!incident.TryGetValue(
                        topologyId,
                        out List<TriangleRecord> incidentTriangles) ||
                    incidentTriangles.Count < 2)
                {
                    continue;
                }

                bool[] visited = new bool[incidentTriangles.Count];
                int fanCount = 0;
                for (int seed = 0;
                    seed < incidentTriangles.Count;
                    seed++)
                {
                    if (visited[seed])
                    {
                        continue;
                    }

                    fanCount++;
                    Queue<int> queue = new Queue<int>();
                    queue.Enqueue(seed);
                    visited[seed] = true;
                    while (queue.Count > 0)
                    {
                        int current = queue.Dequeue();
                        for (int candidate = 0;
                            candidate < incidentTriangles.Count;
                            candidate++)
                        {
                            if (visited[candidate] ||
                                !ShareOtherTopologyVertex(
                                    incidentTriangles[current],
                                    incidentTriangles[candidate],
                                    topologyId))
                            {
                                continue;
                            }

                            visited[candidate] = true;
                            queue.Enqueue(candidate);
                        }
                    }
                }

                if (fanCount <= 1)
                {
                    continue;
                }

                count++;
                if (firstBowTieTopologyId < 0)
                {
                    firstBowTieTopologyId = topologyId;
                    firstBowTieFanCount = fanCount;
                }
            }

            return count;
        }

        private static void AddIncidentTriangle(
            Dictionary<int, List<TriangleRecord>> incident,
            int topologyId,
            TriangleRecord triangle)
        {
            if (!incident.TryGetValue(
                    topologyId,
                    out List<TriangleRecord> triangles))
            {
                triangles = new List<TriangleRecord>();
                incident.Add(topologyId, triangles);
            }

            triangles.Add(triangle);
        }

        private static bool ShareOtherTopologyVertex(
            TriangleRecord first,
            TriangleRecord second,
            int centerTopologyId)
        {
            first.GetOtherTopologyVertices(
                centerTopologyId,
                out int firstA,
                out int firstB);
            second.GetOtherTopologyVertices(
                centerTopologyId,
                out int secondA,
                out int secondB);
            return firstA == secondA ||
                firstA == secondB ||
                firstB == secondA ||
                firstB == secondB;
        }

        private static void ValidateBoundaries(
            MeshBuildBuffer buffer,
            float tolerance,
            FoldCanvasGeometryValidationReport report,
            out PanelBuildRecord firstPanel,
            out BoundaryBuildRecord firstBoundary,
            out double firstLength)
        {
            firstPanel = null;
            firstBoundary = null;
            firstLength = 0d;
            for (int panelIndex = 0;
                panelIndex < buffer.PanelCount;
                panelIndex++)
            {
                PanelBuildRecord panel = buffer.GetPanelAt(panelIndex);
                for (int boundaryIndex = 0;
                    boundaryIndex < panel.Boundaries.Count;
                    boundaryIndex++)
                {
                    BoundaryBuildRecord boundary =
                        panel.Boundaries[boundaryIndex];
                    double length = BoundaryLength(buffer, boundary);
                    if (boundary.VertexIndices.Count >= 2 &&
                        length > tolerance)
                    {
                        continue;
                    }

                    if (IsAllowedCollapsedSphericalPoleBoundary(
                        buffer,
                        panel,
                        boundary))
                    {
                        continue;
                    }

                    report.DegenerateBoundaryCount++;
                    if (firstBoundary == null)
                    {
                        firstPanel = panel;
                        firstBoundary = boundary;
                        firstLength = length;
                    }
                }
            }
        }

        private static double BoundaryLength(
            MeshBuildBuffer buffer,
            BoundaryBuildRecord boundary)
        {
            double length = 0d;
            for (int i = 1; i < boundary.VertexIndices.Count; i++)
            {
                Vector3 previous =
                    buffer.Vertices[boundary.VertexIndices[i - 1]].Position;
                Vector3 current =
                    buffer.Vertices[boundary.VertexIndices[i]].Position;
                length += Vector3.Distance(previous, current);
            }

            if (boundary.IsClosed &&
                boundary.VertexIndices.Count > 2)
            {
                Vector3 last = buffer.Vertices[
                    boundary.VertexIndices[
                        boundary.VertexIndices.Count - 1]].Position;
                Vector3 first = buffer.Vertices[
                    boundary.VertexIndices[0]].Position;
                length += Vector3.Distance(last, first);
            }

            return length;
        }

        private static bool IsAllowedCollapsedSphericalPoleBoundary(
            MeshBuildBuffer buffer,
            PanelBuildRecord panel,
            BoundaryBuildRecord boundary)
        {
            if (boundary.VertexIndices.Count == 0 ||
                !buffer.TryGetSphericalWrap(
                    panel.PanelIndex,
                    out SphericalWrapBuildRecord sphericalWrap))
            {
                return false;
            }

            int topologyId = buffer.GetTopologyId(
                boundary.VertexIndices[0]);
            for (int i = 1; i < boundary.VertexIndices.Count; i++)
            {
                if (buffer.GetTopologyId(boundary.VertexIndices[i]) !=
                    topologyId)
                {
                    return false;
                }
            }

            return sphericalWrap.NorthPoleVertexIndex >= 0 &&
                    buffer.GetTopologyId(
                        sphericalWrap.NorthPoleVertexIndex) == topologyId ||
                sphericalWrap.SouthPoleVertexIndex >= 0 &&
                    buffer.GetTopologyId(
                        sphericalWrap.SouthPoleVertexIndex) == topologyId;
        }

        private static void ValidateWeldSeams(
            MeshBuildBuffer buffer,
            float tolerance,
            FoldCanvasGeometryValidationReport report,
            out FoldCanvasGeometrySeamReport firstMismatch,
            out int firstMismatchedVertexIndex)
        {
            firstMismatch = null;
            firstMismatchedVertexIndex = -1;
            for (int recordIndex = 0;
                recordIndex < buffer.WeldValidationRecords.Count;
                recordIndex++)
            {
                WeldValidationRecord record =
                    buffer.WeldValidationRecords[recordIndex];
                int sampleCount = Math.Min(
                    record.BoundaryA.Count,
                    record.BoundaryB.Count);
                bool matchingCount =
                    record.BoundaryA.Count == record.BoundaryB.Count;
                double maximumGap = 0d;
                int maximumGapVertex = -1;
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    int aIndex = record.BoundaryA[sample];
                    int bIndex = record.BoundaryB[sample];
                    if (aIndex < 0 ||
                        aIndex >= buffer.Vertices.Count ||
                        bIndex < 0 ||
                        bIndex >= buffer.Vertices.Count)
                    {
                        matchingCount = false;
                        continue;
                    }

                    double gap = Vector3.Distance(
                        buffer.Vertices[aIndex].Position,
                        buffer.Vertices[bIndex].Position);
                    if (gap > maximumGap)
                    {
                        maximumGap = gap;
                        maximumGapVertex = aIndex;
                    }
                }

                FoldCanvasGeometrySeamReport seamReport =
                    new FoldCanvasGeometrySeamReport(
                        record.OperationId,
                        record.SeamId,
                        sampleCount,
                        maximumGap,
                        tolerance,
                        matchingCount);
                report.AddSeam(seamReport);
                if (seamReport.IsClosedWithinTolerance)
                {
                    continue;
                }

                report.SeamMismatchCount++;
                if (firstMismatch == null)
                {
                    firstMismatch = seamReport;
                    firstMismatchedVertexIndex = maximumGapVertex;
                }
            }
        }

        private static void ValidateStrictIntersections(
            IReadOnlyList<TriangleRecord> triangles,
            float tolerance,
            FoldCanvasGeometryValidationReport report,
            FoldCanvasCompileResult result)
        {
            report.StrictChecksExecuted = true;
            List<TriangleRecord> ordered =
                new List<TriangleRecord>(triangles);
            ordered.Sort((left, right) =>
            {
                int compare = left.Minimum.x.CompareTo(right.Minimum.x);
                return compare != 0
                    ? compare
                    : left.TriangleIndex.CompareTo(right.TriangleIndex);
            });
            List<TrianglePair> candidates = new List<TrianglePair>();
            for (int i = 0; i < ordered.Count; i++)
            {
                TriangleRecord first = ordered[i];
                for (int j = i + 1; j < ordered.Count; j++)
                {
                    TriangleRecord second = ordered[j];
                    if (second.Minimum.x > first.Maximum.x + tolerance)
                    {
                        break;
                    }

                    if (first.SharesTopologyVertex(second) ||
                        !AabbOverlaps(first, second, tolerance))
                    {
                        continue;
                    }

                    report.BroadPhaseCandidatePairCount++;
                    if (report.BroadPhaseCandidatePairCount >
                        FoldCanvasLimits
                            .MaximumStrictValidationCandidatePairs)
                    {
                        report.StrictCandidateBudgetExceeded = true;
                        AddError(
                            report,
                            result,
                            new FoldCanvasDiagnostic(
                                FoldCanvasDiagnosticCodes
                                    .StrictValidationBudgetExceeded,
                                FoldCanvasDiagnosticSeverity.Error,
                                "Strict self-intersection validation exceeded its deterministic candidate-pair budget.",
                                values: new[]
                                {
                                    new FoldCanvasDiagnosticValue(
                                        "candidatePairCount",
                                        report
                                            .BroadPhaseCandidatePairCount),
                                    new FoldCanvasDiagnosticValue(
                                        "maximumCandidatePairs",
                                        FoldCanvasLimits
                                            .MaximumStrictValidationCandidatePairs)
                                },
                                geometryContext:
                                    new FoldCanvasDiagnosticGeometryContext(
                                        triangleIndex:
                                            first.TriangleIndex,
                                        relatedTriangleIndex:
                                            second.TriangleIndex)));
                        return;
                    }

                    candidates.Add(new TrianglePair(
                        Math.Min(
                            first.TriangleIndex,
                            second.TriangleIndex),
                        Math.Max(
                            first.TriangleIndex,
                            second.TriangleIndex)));
                }
            }

            candidates.Sort();
            FoldCanvasTriangleIntersectionRecord firstIntersection = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                TrianglePair pair = candidates[i];
                TriangleRecord first = triangles[pair.First];
                TriangleRecord second = triangles[pair.Second];
                if (!TrianglesIntersect(first, second, tolerance))
                {
                    continue;
                }

                report.ExactIntersectionPairCount++;
                FoldCanvasTriangleIntersectionRecord intersection =
                    new FoldCanvasTriangleIntersectionRecord(
                        pair.First,
                        pair.Second,
                        first.PanelId,
                        second.PanelId);
                if (report.Intersections.Count <
                    MaximumRecordedIntersections)
                {
                    report.AddIntersection(intersection);
                }

                if (firstIntersection == null)
                {
                    firstIntersection = intersection;
                }
            }

            if (firstIntersection == null)
            {
                return;
            }

            string panelDescription = string.IsNullOrEmpty(
                firstIntersection.PanelIdB)
                ? string.Empty
                : $" The related triangle belongs to panel '{firstIntersection.PanelIdB}'.";
            AddError(
                report,
                result,
                new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.SelfIntersection,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Strict validation confirmed a non-adjacent triangle intersection." +
                    panelDescription,
                    firstIntersection.PanelIdA,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "broadPhaseCandidatePairCount",
                            report.BroadPhaseCandidatePairCount),
                        new FoldCanvasDiagnosticValue(
                            "confirmedIntersectionPairCount",
                            report.ExactIntersectionPairCount)
                    },
                    repairSuggestions: new[]
                    {
                        "Revise the source deformation, placement, or thickness; the validator will not move intersecting faces."
                    },
                    geometryContext:
                        new FoldCanvasDiagnosticGeometryContext(
                            triangleIndex:
                                firstIntersection.TriangleA,
                            relatedTriangleIndex:
                                firstIntersection.TriangleB)));
        }

        private static bool AabbOverlaps(
            TriangleRecord first,
            TriangleRecord second,
            float tolerance)
        {
            return first.Minimum.x <= second.Maximum.x + tolerance &&
                first.Maximum.x + tolerance >= second.Minimum.x &&
                first.Minimum.y <= second.Maximum.y + tolerance &&
                first.Maximum.y + tolerance >= second.Minimum.y &&
                first.Minimum.z <= second.Maximum.z + tolerance &&
                first.Maximum.z + tolerance >= second.Minimum.z;
        }

        private static bool TrianglesIntersect(
            TriangleRecord first,
            TriangleRecord second,
            float tolerance)
        {
            Vector3 firstEdge0 = first.B - first.A;
            Vector3 firstEdge1 = first.C - first.B;
            Vector3 firstEdge2 = first.A - first.C;
            Vector3 secondEdge0 = second.B - second.A;
            Vector3 secondEdge1 = second.C - second.B;
            Vector3 secondEdge2 = second.A - second.C;
            Vector3 firstNormal = Vector3.Cross(
                firstEdge0,
                first.C - first.A);
            Vector3 secondNormal = Vector3.Cross(
                secondEdge0,
                second.C - second.A);

            if (IsSeparated(firstNormal, first, second, tolerance) ||
                IsSeparated(secondNormal, first, second, tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge0, secondEdge0),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge0, secondEdge1),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge0, secondEdge2),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge1, secondEdge0),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge1, secondEdge1),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge1, secondEdge2),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge2, secondEdge0),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge2, secondEdge1),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstEdge2, secondEdge2),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstNormal, firstEdge0),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstNormal, firstEdge1),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(firstNormal, firstEdge2),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(secondNormal, secondEdge0),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(secondNormal, secondEdge1),
                    first,
                    second,
                    tolerance) ||
                IsSeparated(
                    Vector3.Cross(secondNormal, secondEdge2),
                    first,
                    second,
                    tolerance))
            {
                return false;
            }

            return true;
        }

        private static bool IsSeparated(
            Vector3 axis,
            TriangleRecord first,
            TriangleRecord second,
            float tolerance)
        {
            float axisSquared = axis.sqrMagnitude;
            if (axisSquared <=
                FoldCanvasGeometryTolerances
                    .MinimumValidationAxisSquared)
            {
                return false;
            }

            axis *= 1f / Mathf.Sqrt(axisSquared);
            Project(first, axis, out float firstMinimum, out float firstMaximum);
            Project(
                second,
                axis,
                out float secondMinimum,
                out float secondMaximum);
            return firstMaximum < secondMinimum - tolerance ||
                secondMaximum < firstMinimum - tolerance;
        }

        private static void Project(
            TriangleRecord triangle,
            Vector3 axis,
            out float minimum,
            out float maximum)
        {
            float a = Vector3.Dot(triangle.A, axis);
            float b = Vector3.Dot(triangle.B, axis);
            float c = Vector3.Dot(triangle.C, axis);
            minimum = Mathf.Min(a, Mathf.Min(b, c));
            maximum = Mathf.Max(a, Mathf.Max(b, c));
        }

        private static FoldCanvasDiagnosticGeometryContext EdgeContext(
            EdgeKey edge,
            IReadOnlyDictionary<int, int> componentByTopologyId,
            int triangleIndex)
        {
            return new FoldCanvasDiagnosticGeometryContext(
                componentIndex: ComponentIndexFor(
                    componentByTopologyId,
                    edge.A),
                triangleIndex: triangleIndex,
                edgeTopologyVertexA: edge.A,
                edgeTopologyVertexB: edge.B);
        }

        private static int ComponentIndexFor(
            IReadOnlyDictionary<int, int> componentByTopologyId,
            int topologyId)
        {
            return topologyId >= 0 &&
                componentByTopologyId.TryGetValue(
                    topologyId,
                    out int componentIndex)
                ? componentIndex
                : -1;
        }

        private static string PanelIdForVertex(
            MeshBuildBuffer buffer,
            int vertexIndex)
        {
            if (vertexIndex < 0 ||
                vertexIndex >= buffer.Vertices.Count)
            {
                return null;
            }

            int panelIndex = buffer.Vertices[vertexIndex].PanelIndex;
            return panelIndex >= 0 && panelIndex < buffer.PanelCount
                ? buffer.GetPanelAt(panelIndex).PanelId
                : null;
        }

        private static string PanelIdForTriangleIndices(
            MeshBuildBuffer buffer,
            int a,
            int b,
            int c)
        {
            int panelIndex = buffer.Vertices[a].PanelIndex;
            if (buffer.Vertices[b].PanelIndex != panelIndex ||
                buffer.Vertices[c].PanelIndex != panelIndex ||
                panelIndex < 0 ||
                panelIndex >= buffer.PanelCount)
            {
                return null;
            }

            return buffer.GetPanelAt(panelIndex).PanelId;
        }

        private static string PanelIdForTriangle(
            IReadOnlyList<TriangleRecord> triangles,
            int triangleIndex)
        {
            return triangleIndex >= 0 && triangleIndex < triangles.Count
                ? triangles[triangleIndex].PanelId
                : null;
        }

        private static double SignedTetrahedronVolume(
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            double crossX =
                (double)b.y * c.z -
                (double)b.z * c.y;
            double crossY =
                (double)b.z * c.x -
                (double)b.x * c.z;
            double crossZ =
                (double)b.x * c.y -
                (double)b.y * c.x;
            return (
                (double)a.x * crossX +
                (double)a.y * crossY +
                (double)a.z * crossZ) / 6d;
        }

        private static void AddError(
            FoldCanvasGeometryValidationReport report,
            FoldCanvasCompileResult result,
            FoldCanvasDiagnostic diagnostic)
        {
            result.Add(diagnostic);
            report.ErrorCount++;
            report.IsValid = false;
        }

        private static void AddWarning(
            FoldCanvasGeometryValidationReport report,
            FoldCanvasCompileResult result,
            FoldCanvasDiagnostic diagnostic)
        {
            result.Add(diagnostic);
            report.WarningCount++;
        }

        private sealed class TriangleRecord
        {
            public TriangleRecord(
                int triangleIndex,
                int renderA,
                int renderB,
                int renderC,
                int topologyA,
                int topologyB,
                int topologyC,
                Vector3 a,
                Vector3 b,
                Vector3 c,
                string panelId)
            {
                TriangleIndex = triangleIndex;
                RenderA = renderA;
                RenderB = renderB;
                RenderC = renderC;
                TopologyA = topologyA;
                TopologyB = topologyB;
                TopologyC = topologyC;
                A = a;
                B = b;
                C = c;
                PanelId = panelId;
                Minimum = Vector3.Min(a, Vector3.Min(b, c));
                Maximum = Vector3.Max(a, Vector3.Max(b, c));
            }

            public int TriangleIndex { get; }

            public int RenderA { get; }

            public int RenderB { get; }

            public int RenderC { get; }

            public int TopologyA { get; }

            public int TopologyB { get; }

            public int TopologyC { get; }

            public Vector3 A { get; }

            public Vector3 B { get; }

            public Vector3 C { get; }

            public Vector3 Minimum { get; }

            public Vector3 Maximum { get; }

            public string PanelId { get; }

            public bool SharesTopologyVertex(TriangleRecord other)
            {
                return TopologyA == other.TopologyA ||
                    TopologyA == other.TopologyB ||
                    TopologyA == other.TopologyC ||
                    TopologyB == other.TopologyA ||
                    TopologyB == other.TopologyB ||
                    TopologyB == other.TopologyC ||
                    TopologyC == other.TopologyA ||
                    TopologyC == other.TopologyB ||
                    TopologyC == other.TopologyC;
            }

            public void GetOtherTopologyVertices(
                int center,
                out int first,
                out int second)
            {
                if (TopologyA == center)
                {
                    first = TopologyB;
                    second = TopologyC;
                    return;
                }

                if (TopologyB == center)
                {
                    first = TopologyA;
                    second = TopologyC;
                    return;
                }

                first = TopologyA;
                second = TopologyB;
            }
        }

        private readonly struct TriangleKey : IEquatable<TriangleKey>
        {
            public TriangleKey(int first, int second, int third)
            {
                int minimum = Math.Min(first, Math.Min(second, third));
                int maximum = Math.Max(first, Math.Max(second, third));
                A = minimum;
                C = maximum;
                B = first + second + third - minimum - maximum;
            }

            public int A { get; }

            public int B { get; }

            public int C { get; }

            public bool Equals(TriangleKey other)
            {
                return A == other.A && B == other.B && C == other.C;
            }

            public override bool Equals(object obj)
            {
                return obj is TriangleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = A;
                    hash = (hash * 397) ^ B;
                    hash = (hash * 397) ^ C;
                    return hash;
                }
            }
        }

        private readonly struct TrianglePair : IComparable<TrianglePair>
        {
            public TrianglePair(int first, int second)
            {
                First = first;
                Second = second;
            }

            public int First { get; }

            public int Second { get; }

            public int CompareTo(TrianglePair other)
            {
                int compare = First.CompareTo(other.First);
                return compare != 0
                    ? compare
                    : Second.CompareTo(other.Second);
            }
        }

        private readonly struct EdgeKey :
            IEquatable<EdgeKey>,
            IComparable<EdgeKey>
        {
            public EdgeKey(int first, int second)
            {
                A = Math.Min(first, second);
                B = Math.Max(first, second);
            }

            public int A { get; }

            public int B { get; }

            public int CompareTo(EdgeKey other)
            {
                int compare = A.CompareTo(other.A);
                return compare != 0
                    ? compare
                    : B.CompareTo(other.B);
            }

            public bool Equals(EdgeKey other)
            {
                return A == other.A && B == other.B;
            }

            public override bool Equals(object obj)
            {
                return obj is EdgeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (A * 397) ^ B;
                }
            }
        }

        private readonly struct EdgeIncident
        {
            public EdgeIncident(int triangleIndex, int direction)
            {
                TriangleIndex = triangleIndex;
                Direction = direction;
            }

            public int TriangleIndex { get; }

            public int Direction { get; }
        }

        private sealed class EdgeUse
        {
            public List<EdgeIncident> Incidents { get; } =
                new List<EdgeIncident>(2);
        }

        private sealed class ComponentAccumulator
        {
            public int MinimumTopologyVertexId { get; private set; } =
                int.MaxValue;

            public HashSet<int> TopologyVertexIds { get; } =
                new HashSet<int>();

            public List<TriangleRecord> Triangles { get; } =
                new List<TriangleRecord>();

            public int UniqueEdgeCount { get; set; }

            public int OpenEdgeCount { get; set; }

            public void AddTriangle(TriangleRecord triangle)
            {
                Triangles.Add(triangle);
                AddTopologyVertex(triangle.TopologyA);
                AddTopologyVertex(triangle.TopologyB);
                AddTopologyVertex(triangle.TopologyC);
            }

            private void AddTopologyVertex(int topologyId)
            {
                TopologyVertexIds.Add(topologyId);
                MinimumTopologyVertexId = Math.Min(
                    MinimumTopologyVertexId,
                    topologyId);
            }
        }

        private sealed class TopologyUnionFind
        {
            private readonly Dictionary<int, int> parents =
                new Dictionary<int, int>();

            public int RootCount
            {
                get
                {
                    HashSet<int> roots = new HashSet<int>();
                    List<int> ids = new List<int>(parents.Keys);
                    ids.Sort();
                    for (int i = 0; i < ids.Count; i++)
                    {
                        roots.Add(Find(ids[i]));
                    }

                    return roots.Count;
                }
            }

            public void Ensure(int topologyId)
            {
                if (!parents.ContainsKey(topologyId))
                {
                    parents.Add(topologyId, topologyId);
                }
            }

            public int Find(int topologyId)
            {
                int root = topologyId;
                while (parents[root] != root)
                {
                    root = parents[root];
                }

                int current = topologyId;
                while (parents[current] != current)
                {
                    int next = parents[current];
                    parents[current] = root;
                    current = next;
                }

                return root;
            }

            public void Union(int first, int second)
            {
                Ensure(first);
                Ensure(second);
                int firstRoot = Find(first);
                int secondRoot = Find(second);
                if (firstRoot == secondRoot)
                {
                    return;
                }

                int representative = Math.Min(firstRoot, secondRoot);
                int merged = Math.Max(firstRoot, secondRoot);
                parents[merged] = representative;
            }
        }
    }
}
