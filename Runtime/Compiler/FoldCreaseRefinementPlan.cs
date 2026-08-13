using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal sealed class FoldCreaseRefinementPlan
    {
        private readonly Dictionary<string, FoldCreaseRefinedPanel>
            panelsById;

        private FoldCreaseRefinementPlan(
            Dictionary<string, FoldCreaseRefinedPanel> panelsById)
        {
            this.panelsById = panelsById;
        }

        public static FoldCreaseRefinementPlan Build(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            Dictionary<string, FoldCreaseRefinedPanel> refinedPanels =
                new Dictionary<string, FoldCreaseRefinedPanel>(
                    StringComparer.Ordinal);
            HashSet<string> wrapManagedPanels =
                CollectWrapManagedPanels(asset);
            int maximumVertices = asset.CompileSettings != null
                ? asset.CompileSettings.MaxGeneratedVertices
                : FoldCanvasCompileSettings.DefaultMaxGeneratedVertices;
            int maximumTriangles = asset.CompileSettings != null
                ? asset.CompileSettings.MaxGeneratedTriangles
                : FoldCanvasCompileSettings.DefaultMaxGeneratedTriangles;
            int remainingVertices = maximumVertices;
            int remainingTriangles = maximumTriangles;
            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition sourcePanel = asset.Panels[i];
                if (sourcePanel != null &&
                    FoldCanvasSourceValidator.TryEstimateGeometry(
                        sourcePanel,
                        out long sourceVertices,
                        out long sourceTriangles))
                {
                    remainingVertices -= (int)sourceVertices;
                    remainingTriangles -= (int)sourceTriangles;
                }
            }

            for (int panelIndex = 0;
                panelIndex < asset.Panels.Count;
                panelIndex++)
            {
                PanelDefinition panel = asset.Panels[panelIndex];
                if (panel == null ||
                    panel.Shape != PanelShape.Rectangle ||
                    string.IsNullOrWhiteSpace(panel.Id))
                {
                    continue;
                }

                MutableRectangleTopology topology = null;
                bool changed = false;
                for (int operationIndex = 0;
                    operationIndex < asset.Operations.Count;
                    operationIndex++)
                {
                    if (!(asset.Operations[operationIndex] is
                            FoldLineOperationDefinition fold) ||
                        !fold.Enabled ||
                        !string.Equals(
                            fold.PanelId,
                            panel.Id,
                            StringComparison.Ordinal) ||
                        !IsValidPlanningCandidate(fold))
                    {
                        continue;
                    }

                    if (topology == null)
                    {
                        topology = new MutableRectangleTopology(
                            panel,
                            maximumVertices,
                            maximumTriangles,
                            remainingVertices,
                            remainingTriangles);
                    }
                    Vector2 lineStart = ClampNormalized(fold.LineStart);
                    Vector2 lineEnd = ClampNormalized(fold.LineEnd);
                    if (topology.IsExistingContinuousEdgeChain(
                            lineStart,
                            lineEnd))
                    {
                        continue;
                    }

                    if (wrapManagedPanels.Contains(panel.Id) ||
                        !IsSupportedRectanglePartition(
                            lineStart,
                            lineEnd))
                    {
                        AddUnsupportedDiagnostic(
                            fold,
                            result,
                            wrapManagedPanels.Contains(panel.Id)
                                ? "M24 cannot refine an off-grid Fold on a panel whose initial tessellation is managed by SphericalWrap or ToroidalWrap."
                                : "M24 off-grid Fold refinement requires one straight rectangle crease with both endpoints on the perimeter and its open segment inside the panel.");
                        return new FoldCreaseRefinementPlan(
                            refinedPanels);
                    }

                    if (!topology.TrySplit(
                            lineStart,
                            lineEnd,
                            out string failureCode,
                            out string failure))
                    {
                        if (string.Equals(
                                failureCode,
                                FoldCanvasDiagnosticCodes
                                    .GeneratedVertexLimitExceeded,
                                StringComparison.Ordinal) ||
                            string.Equals(
                                failureCode,
                                FoldCanvasDiagnosticCodes
                                    .GeneratedTriangleLimitExceeded,
                                StringComparison.Ordinal))
                        {
                            result.Add(new FoldCanvasDiagnostic(
                                failureCode,
                                FoldCanvasDiagnosticSeverity.Error,
                                failure,
                                panel.Id,
                                fold.Id));
                        }
                        else
                        {
                            AddUnsupportedDiagnostic(
                                fold,
                                result,
                                failure);
                        }

                        return new FoldCreaseRefinementPlan(
                            refinedPanels);
                    }

                    changed = true;
                    remainingVertices -= topology.LastAddedVertexCount;
                    remainingTriangles -= topology.LastAddedTriangleCount;
                }

                if (changed)
                {
                    refinedPanels.Add(panel.Id, topology.Freeze());
                }
            }

            if (!TryValidateAggregateBudget(
                    asset,
                    refinedPanels,
                    maximumVertices,
                    maximumTriangles,
                    result))
            {
                return new FoldCreaseRefinementPlan(
                    new Dictionary<string, FoldCreaseRefinedPanel>(
                        StringComparer.Ordinal));
            }

            return new FoldCreaseRefinementPlan(refinedPanels);
        }

        public bool TryGetPanel(
            string panelId,
            out FoldCreaseRefinedPanel panel)
        {
            if (string.IsNullOrWhiteSpace(panelId))
            {
                panel = null;
                return false;
            }

            return panelsById.TryGetValue(panelId, out panel);
        }

        private static bool IsValidPlanningCandidate(
            FoldLineOperationDefinition fold)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            return FiniteMath.IsFinite(fold.LineStart) &&
                FiniteMath.IsFinite(fold.LineEnd) &&
                IsInsideNormalizedPanel(fold.LineStart, tolerance) &&
                IsInsideNormalizedPanel(fold.LineEnd, tolerance) &&
                (fold.LineEnd - fold.LineStart).magnitude > tolerance &&
                FiniteMath.IsFinite(fold.AngleDegrees) &&
                fold.AngleDegrees != 0f &&
                FiniteMath.IsFinite(fold.Falloff) &&
                fold.Falloff == 0f &&
                (fold.Side == FoldSide.Positive ||
                    fold.Side == FoldSide.Negative);
        }

        private static bool IsSupportedRectanglePartition(
            Vector2 lineStart,
            Vector2 lineEnd)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            if (!IsOnRectanglePerimeter(lineStart, tolerance) ||
                !IsOnRectanglePerimeter(lineEnd, tolerance))
            {
                return false;
            }

            Vector2 midpoint = (lineStart + lineEnd) * 0.5f;
            return midpoint.x > tolerance &&
                midpoint.x < 1f - tolerance &&
                midpoint.y > tolerance &&
                midpoint.y < 1f - tolerance;
        }

        private static HashSet<string> CollectWrapManagedPanels(
            FoldCanvasAsset asset)
        {
            HashSet<string> panelIds =
                new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                FoldOperationDefinition operation = asset.Operations[i];
                if (operation == null || !operation.Enabled)
                {
                    continue;
                }

                string panelId = null;
                if (operation is
                    SphericalWrapOperationDefinition sphericalWrap)
                {
                    panelId = sphericalWrap.PanelId;
                }
                else if (operation is
                    ToroidalWrapOperationDefinition toroidalWrap)
                {
                    panelId = toroidalWrap.PanelId;
                }

                if (!string.IsNullOrWhiteSpace(panelId))
                {
                    panelIds.Add(panelId);
                }
            }

            return panelIds;
        }

        private static bool TryValidateAggregateBudget(
            FoldCanvasAsset asset,
            IReadOnlyDictionary<string, FoldCreaseRefinedPanel>
                refinedPanels,
            int maximumVertices,
            int maximumTriangles,
            FoldCanvasCompileResult result)
        {
            long vertexCount = 0L;
            long triangleCount = 0L;
            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                if (panel == null)
                {
                    continue;
                }

                if (refinedPanels.TryGetValue(
                        panel.Id,
                        out FoldCreaseRefinedPanel refined))
                {
                    vertexCount += refined.VertexCount;
                    triangleCount += refined.TriangleCount;
                }
                else if (FoldCanvasSourceValidator.TryEstimateGeometry(
                    panel,
                    out long panelVertices,
                    out long panelTriangles))
                {
                    vertexCount += panelVertices;
                    triangleCount += panelTriangles;
                }

                if (vertexCount > maximumVertices)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .GeneratedVertexLimitExceeded,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Planned Fold crease refinement exceeds the generated vertex safety limit.",
                        panel.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "plannedVertices",
                                vertexCount),
                            new FoldCanvasDiagnosticValue(
                                "maximumVertices",
                                maximumVertices)
                        }));
                    return false;
                }

                if (triangleCount > maximumTriangles)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .GeneratedTriangleLimitExceeded,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Planned Fold crease refinement exceeds the generated triangle safety limit.",
                        panel.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "plannedTriangles",
                                triangleCount),
                            new FoldCanvasDiagnosticValue(
                                "maximumTriangles",
                                maximumTriangles)
                        }));
                    return false;
                }
            }

            return true;
        }

        private static void AddUnsupportedDiagnostic(
            FoldLineOperationDefinition fold,
            FoldCanvasCompileResult result,
            string message)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes
                    .FoldCreaseRequiresTopologySplit,
                FoldCanvasDiagnosticSeverity.Error,
                message,
                fold.PanelId,
                fold.Id));
        }

        private static bool IsInsideNormalizedPanel(
            Vector2 point,
            float tolerance)
        {
            return point.x >= -tolerance &&
                point.x <= 1f + tolerance &&
                point.y >= -tolerance &&
                point.y <= 1f + tolerance;
        }

        private static bool IsOnRectanglePerimeter(
            Vector2 point,
            float tolerance)
        {
            return Mathf.Abs(point.x) <= tolerance ||
                Mathf.Abs(point.x - 1f) <= tolerance ||
                Mathf.Abs(point.y) <= tolerance ||
                Mathf.Abs(point.y - 1f) <= tolerance;
        }

        private static Vector2 ClampNormalized(Vector2 point)
        {
            return new Vector2(
                Mathf.Clamp01(point.x),
                Mathf.Clamp01(point.y));
        }
    }

    internal sealed class FoldCreaseRefinedPanel
    {
        public FoldCreaseRefinedPanel(
            IReadOnlyList<Vector2> normalizedVertices,
            IReadOnlyList<int> triangleIndices,
            IReadOnlyList<FoldCreaseRefinedBoundary> boundaries)
        {
            NormalizedVertices = Copy(normalizedVertices);
            TriangleIndices = Copy(triangleIndices);
            FoldCreaseRefinedBoundary[] boundaryCopy =
                new FoldCreaseRefinedBoundary[boundaries.Count];
            for (int i = 0; i < boundaries.Count; i++)
            {
                boundaryCopy[i] = boundaries[i];
            }

            Boundaries = Array.AsReadOnly(boundaryCopy);
        }

        public IReadOnlyList<Vector2> NormalizedVertices { get; }

        public IReadOnlyList<int> TriangleIndices { get; }

        public IReadOnlyList<FoldCreaseRefinedBoundary> Boundaries { get; }

        public int VertexCount => NormalizedVertices.Count;

        public int TriangleCount => TriangleIndices.Count / 3;

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            T[] copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }

    internal sealed class FoldCreaseRefinedBoundary
    {
        public FoldCreaseRefinedBoundary(
            string id,
            IReadOnlyList<int> vertexIndices,
            bool isClosed)
        {
            Id = id;
            int[] copy = new int[vertexIndices.Count];
            for (int i = 0; i < vertexIndices.Count; i++)
            {
                copy[i] = vertexIndices[i];
            }

            VertexIndices = Array.AsReadOnly(copy);
            IsClosed = isClosed;
        }

        public string Id { get; }

        public IReadOnlyList<int> VertexIndices { get; }

        public bool IsClosed { get; }
    }

    internal sealed class MutableRectangleTopology
    {
        private readonly List<Vector2> vertices = new List<Vector2>();

        private List<int> triangleIndices = new List<int>();

        private readonly List<MutableBoundary> boundaries =
            new List<MutableBoundary>();

        private readonly int maximumVertices;

        private readonly int maximumTriangles;

        private int remainingAdditionalVertices;

        private int remainingAdditionalTriangles;

        public MutableRectangleTopology(
            PanelDefinition panel,
            int maximumVertices,
            int maximumTriangles,
            int remainingAdditionalVertices,
            int remainingAdditionalTriangles)
        {
            this.maximumVertices = maximumVertices;
            this.maximumTriangles = maximumTriangles;
            this.remainingAdditionalVertices =
                remainingAdditionalVertices;
            this.remainingAdditionalTriangles =
                remainingAdditionalTriangles;
            int rowLength = panel.USegments + 1;
            for (int v = 0; v <= panel.VSegments; v++)
            {
                float normalizedV = v / (float)panel.VSegments;
                for (int u = 0; u <= panel.USegments; u++)
                {
                    vertices.Add(new Vector2(
                        u / (float)panel.USegments,
                        normalizedV));
                }
            }

            for (int v = 0; v < panel.VSegments; v++)
            {
                for (int u = 0; u < panel.USegments; u++)
                {
                    int i0 = v * rowLength + u;
                    int i1 = i0 + 1;
                    int i3 = i0 + rowLength;
                    int i2 = i3 + 1;
                    AddTriangleUnchecked(i0, i1, i2);
                    AddTriangleUnchecked(i0, i2, i3);
                }
            }

            List<int> uMin = new List<int>(panel.VSegments + 1);
            List<int> uMax = new List<int>(panel.VSegments + 1);
            for (int v = 0; v <= panel.VSegments; v++)
            {
                uMin.Add(v * rowLength);
                uMax.Add(v * rowLength + panel.USegments);
            }

            List<int> vMin = new List<int>(panel.USegments + 1);
            List<int> vMax = new List<int>(panel.USegments + 1);
            for (int u = 0; u <= panel.USegments; u++)
            {
                vMin.Add(u);
                vMax.Add(panel.VSegments * rowLength + u);
            }

            boundaries.Add(new MutableBoundary("uMin", uMin, false));
            boundaries.Add(new MutableBoundary("uMax", uMax, false));
            boundaries.Add(new MutableBoundary("vMin", vMin, false));
            boundaries.Add(new MutableBoundary("vMax", vMax, false));
        }

        public int LastAddedVertexCount { get; private set; }

        public int LastAddedTriangleCount { get; private set; }

        public bool TrySplit(
            Vector2 lineStart,
            Vector2 lineEnd,
            out string failureCode,
            out string failure)
        {
            Vector2 lineDelta = lineEnd - lineStart;
            int verticesBefore = vertices.Count;
            int trianglesBefore = triangleIndices.Count / 3;
            Dictionary<EdgeKey, int> intersections =
                new Dictionary<EdgeKey, int>();
            List<int> refinedTriangles =
                new List<int>(triangleIndices.Count * 2);

            for (int offset = 0;
                offset < triangleIndices.Count;
                offset += 3)
            {
                int a = triangleIndices[offset];
                int b = triangleIndices[offset + 1];
                int c = triangleIndices[offset + 2];
                float distanceA = SignedDistance(
                    lineStart,
                    lineDelta,
                    vertices[a]);
                float distanceB = SignedDistance(
                    lineStart,
                    lineDelta,
                    vertices[b]);
                float distanceC = SignedDistance(
                    lineStart,
                    lineDelta,
                    vertices[c]);
                bool hasPositive = IsStrictlyPositive(distanceA) ||
                    IsStrictlyPositive(distanceB) ||
                    IsStrictlyPositive(distanceC);
                bool hasNegative = IsStrictlyNegative(distanceA) ||
                    IsStrictlyNegative(distanceB) ||
                    IsStrictlyNegative(distanceC);
                if (!hasPositive || !hasNegative)
                {
                    if (!TryAddTriangle(
                            refinedTriangles,
                            a,
                            b,
                            c,
                            SignedArea(a, b, c),
                            out failureCode,
                            out failure))
                    {
                        return false;
                    }

                    continue;
                }

                List<int> triangle = new List<int>(3) { a, b, c };
                if (!TryClip(
                        triangle,
                        true,
                        lineStart,
                        lineDelta,
                        intersections,
                        out List<int> positive,
                        out failureCode,
                        out failure) ||
                    !TryClip(
                        triangle,
                        false,
                        lineStart,
                        lineDelta,
                        intersections,
                        out List<int> negative,
                        out failureCode,
                        out failure))
                {
                    return false;
                }

                float originalArea = SignedArea(a, b, c);
                if (!TryTriangulate(
                        positive,
                        originalArea,
                        refinedTriangles,
                        out failureCode,
                        out failure) ||
                    !TryTriangulate(
                        negative,
                        originalArea,
                        refinedTriangles,
                        out failureCode,
                        out failure))
                {
                    return false;
                }
            }

            triangleIndices = refinedTriangles;
            int addedVertices = vertices.Count - verticesBefore;
            int addedTriangles =
                triangleIndices.Count / 3 - trianglesBefore;
            if (addedVertices > remainingAdditionalVertices)
            {
                failureCode = FoldCanvasDiagnosticCodes
                    .GeneratedVertexLimitExceeded;
                failure =
                    "Fold crease refinement exceeds the generated vertex safety limit.";
                return false;
            }

            if (addedTriangles > remainingAdditionalTriangles)
            {
                failureCode = FoldCanvasDiagnosticCodes
                    .GeneratedTriangleLimitExceeded;
                failure =
                    "Fold crease refinement exceeds the generated triangle safety limit.";
                return false;
            }

            remainingAdditionalVertices -= addedVertices;
            remainingAdditionalTriangles -= addedTriangles;
            LastAddedVertexCount = addedVertices;
            LastAddedTriangleCount = addedTriangles;
            SpliceBoundaryIntersections(intersections);
            if (!IsExistingContinuousEdgeChain(lineStart, lineEnd))
            {
                failureCode = FoldCanvasDiagnosticCodes
                    .FoldCreaseRequiresTopologySplit;
                failure =
                    "The deterministic refinement did not produce one continuous crease edge chain.";
                return false;
            }

            failureCode = null;
            failure = null;
            return true;
        }

        public bool IsExistingContinuousEdgeChain(
            Vector2 lineStart,
            Vector2 lineEnd)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            bool hasStart = false;
            bool hasEnd = false;
            for (int i = 0; i < vertices.Count; i++)
            {
                hasStart |= (vertices[i] - lineStart).sqrMagnitude <=
                    tolerance * tolerance;
                hasEnd |= (vertices[i] - lineEnd).sqrMagnitude <=
                    tolerance * tolerance;
            }

            if (!hasStart || !hasEnd)
            {
                return false;
            }

            List<CreaseInterval> intervals =
                new List<CreaseInterval>();
            for (int offset = 0;
                offset < triangleIndices.Count;
                offset += 3)
            {
                AddEdgeInterval(
                    lineStart,
                    lineEnd,
                    triangleIndices[offset],
                    triangleIndices[offset + 1],
                    intervals);
                AddEdgeInterval(
                    lineStart,
                    lineEnd,
                    triangleIndices[offset + 1],
                    triangleIndices[offset + 2],
                    intervals);
                AddEdgeInterval(
                    lineStart,
                    lineEnd,
                    triangleIndices[offset + 2],
                    triangleIndices[offset],
                    intervals);
            }

            intervals.Sort(CompareIntervals);
            float coveredUntil = 0f;
            for (int i = 0; i < intervals.Count; i++)
            {
                CreaseInterval interval = intervals[i];
                if (interval.Start > coveredUntil + tolerance)
                {
                    return false;
                }

                coveredUntil = Mathf.Max(coveredUntil, interval.End);
                if (coveredUntil >= 1f - tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        public FoldCreaseRefinedPanel Freeze()
        {
            FoldCreaseRefinedBoundary[] frozenBoundaries =
                new FoldCreaseRefinedBoundary[boundaries.Count];
            for (int i = 0; i < boundaries.Count; i++)
            {
                frozenBoundaries[i] = new FoldCreaseRefinedBoundary(
                    boundaries[i].Id,
                    boundaries[i].VertexIndices,
                    boundaries[i].IsClosed);
            }

            return new FoldCreaseRefinedPanel(
                vertices,
                triangleIndices,
                frozenBoundaries);
        }

        private bool TryClip(
            IReadOnlyList<int> polygon,
            bool keepPositive,
            Vector2 lineStart,
            Vector2 lineDelta,
            Dictionary<EdgeKey, int> intersections,
            out List<int> clipped,
            out string failureCode,
            out string failure)
        {
            clipped = new List<int>(4);
            int previous = polygon[polygon.Count - 1];
            float previousDistance = SignedDistance(
                lineStart,
                lineDelta,
                vertices[previous]);
            bool previousInside = IsInside(
                previousDistance,
                keepPositive);
            for (int i = 0; i < polygon.Count; i++)
            {
                int current = polygon[i];
                float currentDistance = SignedDistance(
                    lineStart,
                    lineDelta,
                    vertices[current]);
                bool currentInside = IsInside(
                    currentDistance,
                    keepPositive);
                if (currentInside != previousInside)
                {
                    if (!TryGetIntersection(
                            previous,
                            current,
                            lineStart,
                            lineDelta,
                            intersections,
                            out int intersection,
                            out failureCode,
                            out failure))
                    {
                        return false;
                    }

                    AddDistinct(clipped, intersection);
                }

                if (currentInside)
                {
                    AddDistinct(clipped, current);
                }

                previous = current;
                previousInside = currentInside;
            }

            RemoveClosingDuplicate(clipped);
            failureCode = null;
            failure = null;
            return true;
        }

        private bool TryGetIntersection(
            int first,
            int second,
            Vector2 lineStart,
            Vector2 lineDelta,
            Dictionary<EdgeKey, int> intersections,
            out int intersection,
            out string failureCode,
            out string failure)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            float firstDistance = SignedDistance(
                lineStart,
                lineDelta,
                vertices[first]);
            if (Mathf.Abs(firstDistance) <= tolerance)
            {
                intersection = first;
                failureCode = null;
                failure = null;
                return true;
            }

            float secondDistance = SignedDistance(
                lineStart,
                lineDelta,
                vertices[second]);
            if (Mathf.Abs(secondDistance) <= tolerance)
            {
                intersection = second;
                failureCode = null;
                failure = null;
                return true;
            }

            EdgeKey key = new EdgeKey(first, second);
            if (intersections.TryGetValue(key, out intersection))
            {
                failureCode = null;
                failure = null;
                return true;
            }

            if (vertices.Count >= maximumVertices)
            {
                intersection = -1;
                failureCode = FoldCanvasDiagnosticCodes
                    .GeneratedVertexLimitExceeded;
                failure =
                    "Fold crease refinement exceeds the generated vertex safety limit.";
                return false;
            }

            int canonicalFirst = key.First;
            int canonicalSecond = key.Second;
            float canonicalFirstDistance = SignedDistance(
                lineStart,
                lineDelta,
                vertices[canonicalFirst]);
            float canonicalSecondDistance = SignedDistance(
                lineStart,
                lineDelta,
                vertices[canonicalSecond]);
            float denominator =
                canonicalFirstDistance - canonicalSecondDistance;
            if (Mathf.Abs(denominator) <= tolerance)
            {
                intersection = -1;
                failureCode = FoldCanvasDiagnosticCodes
                    .FoldCreaseRequiresTopologySplit;
                failure =
                    "The crease intersection is numerically ambiguous on a source edge.";
                return false;
            }

            float parameter = Mathf.Clamp01(
                canonicalFirstDistance / denominator);
            Vector2 sourcePoint = Vector2.LerpUnclamped(
                vertices[canonicalFirst],
                vertices[canonicalSecond],
                parameter);
            sourcePoint.x = Mathf.Clamp01(sourcePoint.x);
            sourcePoint.y = Mathf.Clamp01(sourcePoint.y);
            intersection = vertices.Count;
            vertices.Add(sourcePoint);
            intersections.Add(key, intersection);
            failureCode = null;
            failure = null;
            return true;
        }

        private bool TryTriangulate(
            List<int> polygon,
            float originalArea,
            List<int> destination,
            out string failureCode,
            out string failure)
        {
            RemoveClosingDuplicate(polygon);
            if (polygon.Count < 3)
            {
                failureCode = FoldCanvasDiagnosticCodes
                    .FoldCreaseRequiresTopologySplit;
                failure =
                    "The crease produced a collapsed source polygon.";
                return false;
            }

            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                if (!TryAddTriangle(
                        destination,
                        polygon[0],
                        polygon[i],
                        polygon[i + 1],
                        originalArea,
                        out failureCode,
                        out failure))
                {
                    return false;
                }
            }

            failureCode = null;
            failure = null;
            return true;
        }

        private bool TryAddTriangle(
            List<int> destination,
            int a,
            int b,
            int c,
            float referenceArea,
            out string failureCode,
            out string failure)
        {
            if (destination.Count / 3 >= maximumTriangles)
            {
                failureCode = FoldCanvasDiagnosticCodes
                    .GeneratedTriangleLimitExceeded;
                failure =
                    "Fold crease refinement exceeds the generated triangle safety limit.";
                return false;
            }

            float area = SignedArea(a, b, c);
            float minimumArea =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance *
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            if (a == b || b == c || c == a ||
                Mathf.Abs(area) <= minimumArea ||
                Mathf.Abs(referenceArea) <= minimumArea)
            {
                failureCode = FoldCanvasDiagnosticCodes
                    .FoldCreaseRequiresTopologySplit;
                failure =
                    "The crease would produce a zero-area source triangle at the configured tolerance.";
                return false;
            }

            if (Mathf.Sign(area) != Mathf.Sign(referenceArea))
            {
                int swap = b;
                b = c;
                c = swap;
            }

            destination.Add(a);
            destination.Add(b);
            destination.Add(c);
            failureCode = null;
            failure = null;
            return true;
        }

        private void SpliceBoundaryIntersections(
            IReadOnlyDictionary<EdgeKey, int> intersections)
        {
            for (int boundaryIndex = 0;
                boundaryIndex < boundaries.Count;
                boundaryIndex++)
            {
                MutableBoundary boundary = boundaries[boundaryIndex];
                if (boundary.VertexIndices.Count < 2)
                {
                    continue;
                }

                List<int> refined = new List<int>(
                    boundary.VertexIndices.Count + 1);
                int segmentCount = boundary.IsClosed
                    ? boundary.VertexIndices.Count
                    : boundary.VertexIndices.Count - 1;
                refined.Add(boundary.VertexIndices[0]);
                for (int segment = 0;
                    segment < segmentCount;
                    segment++)
                {
                    int from = boundary.VertexIndices[segment];
                    int to = boundary.VertexIndices[
                        (segment + 1) % boundary.VertexIndices.Count];
                    if (intersections.TryGetValue(
                            new EdgeKey(from, to),
                            out int intersection) &&
                        intersection != from &&
                        intersection != to)
                    {
                        refined.Add(intersection);
                    }

                    if (!boundary.IsClosed || segment + 1 < segmentCount)
                    {
                        refined.Add(to);
                    }
                }

                boundary.VertexIndices = refined;
            }
        }

        private void AddEdgeInterval(
            Vector2 lineStart,
            Vector2 lineEnd,
            int edgeStart,
            int edgeEnd,
            List<CreaseInterval> intervals)
        {
            Vector2 lineDelta = lineEnd - lineStart;
            float lineLength = lineDelta.magnitude;
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            Vector2 sourceStart = vertices[edgeStart];
            Vector2 sourceEnd = vertices[edgeEnd];
            if (Mathf.Abs(Cross2(
                    sourceStart - lineStart,
                    lineDelta)) > tolerance * lineLength ||
                Mathf.Abs(Cross2(
                    sourceEnd - lineStart,
                    lineDelta)) > tolerance * lineLength)
            {
                return;
            }

            float lineLengthSquared = lineDelta.sqrMagnitude;
            float startParameter = Vector2.Dot(
                sourceStart - lineStart,
                lineDelta) / lineLengthSquared;
            float endParameter = Vector2.Dot(
                sourceEnd - lineStart,
                lineDelta) / lineLengthSquared;
            float intervalStart = Mathf.Max(
                0f,
                Mathf.Min(startParameter, endParameter));
            float intervalEnd = Mathf.Min(
                1f,
                Mathf.Max(startParameter, endParameter));
            if (intervalEnd - intervalStart > tolerance)
            {
                intervals.Add(new CreaseInterval(
                    intervalStart,
                    intervalEnd));
            }
        }

        private float SignedArea(int a, int b, int c)
        {
            return Cross2(
                vertices[b] - vertices[a],
                vertices[c] - vertices[a]);
        }

        private static float SignedDistance(
            Vector2 lineStart,
            Vector2 lineDelta,
            Vector2 point)
        {
            return Cross2(lineDelta, point - lineStart) /
                lineDelta.magnitude;
        }

        private static bool IsInside(
            float signedDistance,
            bool keepPositive)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            return keepPositive
                ? signedDistance >= -tolerance
                : signedDistance <= tolerance;
        }

        private static bool IsStrictlyPositive(float value)
        {
            return value >
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
        }

        private static bool IsStrictlyNegative(float value)
        {
            return value <
                -FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
        }

        private static void AddDistinct(List<int> values, int value)
        {
            if (values.Count == 0 || values[values.Count - 1] != value)
            {
                values.Add(value);
            }
        }

        private static void RemoveClosingDuplicate(List<int> values)
        {
            if (values.Count > 1 && values[0] == values[values.Count - 1])
            {
                values.RemoveAt(values.Count - 1);
            }
        }

        private void AddTriangleUnchecked(int a, int b, int c)
        {
            triangleIndices.Add(a);
            triangleIndices.Add(b);
            triangleIndices.Add(c);
        }

        private static int CompareIntervals(
            CreaseInterval left,
            CreaseInterval right)
        {
            int startComparison = left.Start.CompareTo(right.Start);
            return startComparison != 0
                ? startComparison
                : left.End.CompareTo(right.End);
        }

        private static float Cross2(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private sealed class MutableBoundary
        {
            public MutableBoundary(
                string id,
                List<int> vertexIndices,
                bool isClosed)
            {
                Id = id;
                VertexIndices = vertexIndices;
                IsClosed = isClosed;
            }

            public string Id { get; }

            public List<int> VertexIndices { get; set; }

            public bool IsClosed { get; }
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public EdgeKey(int first, int second)
            {
                First = Math.Min(first, second);
                Second = Math.Max(first, second);
            }

            public int First { get; }

            public int Second { get; }

            public bool Equals(EdgeKey other)
            {
                return First == other.First && Second == other.Second;
            }

            public override bool Equals(object obj)
            {
                return obj is EdgeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (First * 397) ^ Second;
                }
            }
        }

        private readonly struct CreaseInterval
        {
            public CreaseInterval(float start, float end)
            {
                Start = start;
                End = end;
            }

            public float Start { get; }

            public float End { get; }
        }
    }
}
