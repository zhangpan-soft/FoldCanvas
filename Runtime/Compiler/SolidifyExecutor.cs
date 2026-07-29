using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal static class SolidifyExecutor
    {
        private const float NormalDeduplicationDot = 0.9999f;
        private const float IndependentNormalTolerance = 0.000001f;
        private const float OffsetPlaneResidualTolerance = 0.001f;
        private const float MaximumMiterScale = 1000f;

        public static bool TryExecute(
            SolidifyOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (!FiniteMath.IsFinite(operation.Thickness) ||
                operation.Thickness <= 0f)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidSolidifyThickness,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Solidify thickness must be finite and greater than zero.",
                    operationId: operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "thickness",
                            FiniteMath.IsFinite(operation.Thickness)
                                ? operation.Thickness
                                : 0d,
                            "m")
                    }));
                return false;
            }

            if (operation.Direction != SolidifyDirection.Inward &&
                operation.Direction != SolidifyDirection.Outward &&
                operation.Direction != SolidifyDirection.Centered)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidSolidifyDirection,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Solidify direction is not a supported enum value.",
                    operationId: operation.Id));
                return false;
            }

            if (!TryResolveSelectedPanels(
                    operation,
                    buffer,
                    result,
                    out HashSet<int> selectedPanelIndices))
            {
                return false;
            }

            int sourceVertexCount = buffer.Vertices.Count;
            bool[] selectedVertices = new bool[sourceVertexCount];
            HashSet<int> selectedTopologyRoots = new HashSet<int>();
            for (int vertexIndex = 0;
                vertexIndex < sourceVertexCount;
                vertexIndex++)
            {
                MeshBuildVertex vertex = buffer.Vertices[vertexIndex];
                if (!selectedPanelIndices.Contains(vertex.PanelIndex))
                {
                    continue;
                }

                selectedVertices[vertexIndex] = true;
                selectedTopologyRoots.Add(
                    buffer.GetTopologyId(vertexIndex));
            }

            for (int vertexIndex = 0;
                vertexIndex < sourceVertexCount;
                vertexIndex++)
            {
                int topologyRoot = buffer.GetTopologyId(vertexIndex);
                if (selectedTopologyRoots.Contains(topologyRoot) &&
                    !selectedVertices[vertexIndex])
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .IncompleteSolidifyTopologySelection,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Solidify panel selection must include every render copy in each selected welded topology group.",
                        operationId: operation.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "topologyVertexId",
                                topologyRoot)
                        }));
                    return false;
                }
            }

            int sourceTriangleIndexCount = buffer.Triangles.Count;
            List<SourceTriangle> selectedTriangles =
                new List<SourceTriangle>();
            Dictionary<ulong, SourceEdgeUse> sourceEdges =
                new Dictionary<ulong, SourceEdgeUse>();
            Dictionary<int, List<Vector3>> incidentNormals =
                new Dictionary<int, List<Vector3>>();
            for (int triangleOffset = 0;
                triangleOffset < sourceTriangleIndexCount;
                triangleOffset += 3)
            {
                int a = buffer.Triangles[triangleOffset];
                int b = buffer.Triangles[triangleOffset + 1];
                int c = buffer.Triangles[triangleOffset + 2];
                int selectedCount =
                    (selectedVertices[a] ? 1 : 0) +
                    (selectedVertices[b] ? 1 : 0) +
                    (selectedVertices[c] ? 1 : 0);
                if (selectedCount == 0)
                {
                    continue;
                }

                if (selectedCount != 3)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .IncompleteSolidifyTopologySelection,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Solidify cannot select only part of a source triangle or Bridge strip.",
                        operationId: operation.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "triangleIndex",
                                triangleOffset / 3)
                        }));
                    return false;
                }

                int topologyA = buffer.GetTopologyId(a);
                int topologyB = buffer.GetTopologyId(b);
                int topologyC = buffer.GetTopologyId(c);
                if (topologyA == topologyB ||
                    topologyB == topologyC ||
                    topologyC == topologyA)
                {
                    AddNonManifoldDiagnostic(
                        operation,
                        result,
                        triangleOffset / 3);
                    return false;
                }

                Vector3 positionA = buffer.Vertices[a].Position;
                Vector3 positionB = buffer.Vertices[b].Position;
                Vector3 positionC = buffer.Vertices[c].Position;
                Vector3 cross = Vector3.Cross(
                    positionB - positionA,
                    positionC - positionA);
                if (!FiniteMath.IsFinite(cross) ||
                    cross.sqrMagnitude <=
                    FoldCanvasGeometryTolerances
                        .MinimumTriangleDoubleAreaSquared)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.ZeroAreaTriangle,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Solidify source triangle {triangleOffset / 3} has zero or near-zero area.",
                        operationId: operation.Id));
                    return false;
                }

                Vector3 normal =
                    cross / Mathf.Sqrt(cross.sqrMagnitude);
                AddIncidentNormal(
                    incidentNormals,
                    topologyA,
                    normal);
                AddIncidentNormal(
                    incidentNormals,
                    topologyB,
                    normal);
                AddIncidentNormal(
                    incidentNormals,
                    topologyC,
                    normal);

                if (!TryAddSourceEdge(
                        topologyA,
                        topologyB,
                        a,
                        b,
                        sourceEdges) ||
                    !TryAddSourceEdge(
                        topologyB,
                        topologyC,
                        b,
                        c,
                        sourceEdges) ||
                    !TryAddSourceEdge(
                        topologyC,
                        topologyA,
                        c,
                        a,
                        sourceEdges))
                {
                    AddNonManifoldDiagnostic(
                        operation,
                        result,
                        triangleOffset / 3);
                    return false;
                }

                selectedTriangles.Add(new SourceTriangle(
                    triangleOffset,
                    a,
                    b,
                    c));
            }

            if (selectedTriangles.Count == 0)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.SolidifyTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Solidify selected panels contain no source triangles.",
                    operationId: operation.Id));
                return false;
            }

            Dictionary<int, Vector3> unitMiters =
                new Dictionary<int, Vector3>();
            foreach (int topologyRoot in selectedTopologyRoots)
            {
                if (!incidentNormals.TryGetValue(
                        topologyRoot,
                        out List<Vector3> normals) ||
                    !TrySolveUnitMiter(normals, out Vector3 unitMiter))
                {
                    int normalCount = normals != null
                        ? normals.Count
                        : 0;
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsupportedSolidifyCorner,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Solidify could not solve a stable bounded offset-plane intersection for a selected topology vertex.",
                        operationId: operation.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "topologyVertexId",
                                topologyRoot),
                            new FoldCanvasDiagnosticValue(
                                "thickness",
                                operation.Thickness,
                                "m"),
                            new FoldCanvasDiagnosticValue(
                                "incidentNormalCount",
                                normalCount),
                            new FoldCanvasDiagnosticValue(
                                "minimumIncidentNormalDot",
                                MinimumNormalDot(normals))
                        }));
                    return false;
                }

                unitMiters.Add(topologyRoot, unitMiter);
            }

            GetLayerDistances(
                operation,
                out float outerDistance,
                out float innerDistance,
                out bool originalsAreOuter);
            Dictionary<int, Vector3> outerPositions =
                new Dictionary<int, Vector3>();
            Dictionary<int, Vector3> innerPositions =
                new Dictionary<int, Vector3>();
            foreach (int topologyRoot in selectedTopologyRoots)
            {
                Vector3 sourcePosition =
                    buffer.Vertices[topologyRoot].Position;
                Vector3 unitMiter = unitMiters[topologyRoot];
                outerPositions.Add(
                    topologyRoot,
                    sourcePosition + unitMiter * outerDistance);
                innerPositions.Add(
                    topologyRoot,
                    sourcePosition + unitMiter * innerDistance);
            }

            int[] outerMap = new int[sourceVertexCount];
            int[] innerMap = new int[sourceVertexCount];
            for (int i = 0; i < sourceVertexCount; i++)
            {
                outerMap[i] = -1;
                innerMap[i] = -1;
            }

            Dictionary<int, int> duplicateShellRepresentatives =
                new Dictionary<int, int>();
            for (int vertexIndex = 0;
                vertexIndex < sourceVertexCount;
                vertexIndex++)
            {
                if (!selectedVertices[vertexIndex])
                {
                    continue;
                }

                MeshBuildVertex source = buffer.Vertices[vertexIndex];
                int topologyRoot = buffer.GetTopologyId(vertexIndex);
                Vector3 originalLayerPosition = originalsAreOuter
                    ? outerPositions[topologyRoot]
                    : innerPositions[topologyRoot];
                MeshBuildVertex updated = source;
                updated.Position = originalLayerPosition;
                buffer.Vertices[vertexIndex] = updated;

                Vector3 duplicatePosition = originalsAreOuter
                    ? innerPositions[topologyRoot]
                    : outerPositions[topologyRoot];
                int duplicateIndex = buffer.AddVertex(
                    duplicatePosition,
                    source.SourcePosition,
                    source.SourceUv,
                    source.PanelIndex,
                    source.ProvenanceId);
                if (duplicateShellRepresentatives.TryGetValue(
                        topologyRoot,
                        out int duplicateRepresentative))
                {
                    buffer.UnionTopology(
                        duplicateRepresentative,
                        duplicateIndex);
                }
                else
                {
                    duplicateShellRepresentatives.Add(
                        topologyRoot,
                        duplicateIndex);
                }

                if (originalsAreOuter)
                {
                    outerMap[vertexIndex] = vertexIndex;
                    innerMap[vertexIndex] = duplicateIndex;
                }
                else
                {
                    outerMap[vertexIndex] = duplicateIndex;
                    innerMap[vertexIndex] = vertexIndex;
                }
            }

            for (int i = 0; i < selectedTriangles.Count; i++)
            {
                SourceTriangle triangle = selectedTriangles[i];
                buffer.Triangles[triangle.TriangleOffset] =
                    outerMap[triangle.A];
                buffer.Triangles[triangle.TriangleOffset + 1] =
                    outerMap[triangle.B];
                buffer.Triangles[triangle.TriangleOffset + 2] =
                    outerMap[triangle.C];

                buffer.Triangles.Add(innerMap[triangle.A]);
                buffer.Triangles.Add(innerMap[triangle.C]);
                buffer.Triangles.Add(innerMap[triangle.B]);
            }

            foreach (KeyValuePair<ulong, SourceEdgeUse> pair in sourceEdges)
            {
                SourceEdgeUse edge = pair.Value;
                if (edge.Count != 1)
                {
                    continue;
                }

                AppendRim(
                    edge.FromVertex,
                    edge.ToVertex,
                    outerMap,
                    innerMap,
                    buffer);
            }

            buffer.SnapWeldedTopologyPositions();
            return true;
        }

        private static float MinimumNormalDot(
            List<Vector3> normals)
        {
            if (normals == null || normals.Count < 2)
            {
                return 1f;
            }

            float minimum = 1f;
            for (int first = 0;
                first < normals.Count - 1;
                first++)
            {
                for (int second = first + 1;
                    second < normals.Count;
                    second++)
                {
                    minimum = Mathf.Min(
                        minimum,
                        Vector3.Dot(
                            normals[first],
                            normals[second]));
                }
            }

            return minimum;
        }

        private static bool TryResolveSelectedPanels(
            SolidifyOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            out HashSet<int> selectedPanelIndices)
        {
            selectedPanelIndices = new HashSet<int>();
            if (operation.PanelIds == null ||
                operation.PanelIds.Count == 0)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.SolidifyTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Solidify must select at least one panel.",
                    operationId: operation.Id));
                return false;
            }

            for (int i = 0; i < operation.PanelIds.Count; i++)
            {
                string panelId = operation.PanelIds[i];
                if (!buffer.TryGetPanel(
                    panelId,
                    out PanelBuildRecord panel))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.SolidifyTargetMissing,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Solidify references a panel that does not exist.",
                        panelId,
                        operation.Id));
                    return false;
                }

                selectedPanelIndices.Add(panel.PanelIndex);
            }

            return true;
        }

        private static void AddIncidentNormal(
            Dictionary<int, List<Vector3>> incidentNormals,
            int topologyRoot,
            Vector3 normal)
        {
            if (!incidentNormals.TryGetValue(
                    topologyRoot,
                    out List<Vector3> normals))
            {
                normals = new List<Vector3>();
                incidentNormals.Add(topologyRoot, normals);
            }

            for (int i = 0; i < normals.Count; i++)
            {
                if (Vector3.Dot(normals[i], normal) >=
                    NormalDeduplicationDot)
                {
                    return;
                }
            }

            normals.Add(normal);
        }

        private static bool TrySolveUnitMiter(
            List<Vector3> normals,
            out Vector3 unitMiter)
        {
            unitMiter = Vector3.zero;
            if (normals == null || normals.Count == 0)
            {
                return false;
            }

            if (normals.Count == 1)
            {
                unitMiter = normals[0];
                return true;
            }

            int bestFirst = 0;
            int bestSecond = 1;
            float bestPairIndependence = 0f;
            for (int first = 0; first < normals.Count - 1; first++)
            {
                for (int second = first + 1;
                    second < normals.Count;
                    second++)
                {
                    float independence =
                        Vector3.Cross(
                            normals[first],
                            normals[second]).sqrMagnitude;
                    if (independence > bestPairIndependence)
                    {
                        bestPairIndependence = independence;
                        bestFirst = first;
                        bestSecond = second;
                    }
                }
            }

            float bestTripleDeterminant = 0f;
            Vector3 tripleSolution = Vector3.zero;
            for (int first = 0; first < normals.Count - 2; first++)
            {
                for (int second = first + 1;
                    second < normals.Count - 1;
                    second++)
                {
                    for (int third = second + 1;
                        third < normals.Count;
                        third++)
                    {
                        Vector3 firstNormal = normals[first];
                        Vector3 secondNormal = normals[second];
                        Vector3 thirdNormal = normals[third];
                        float determinant = Vector3.Dot(
                            firstNormal,
                            Vector3.Cross(
                                secondNormal,
                                thirdNormal));
                        if (Mathf.Abs(determinant) <=
                            Mathf.Abs(bestTripleDeterminant))
                        {
                            continue;
                        }

                        bestTripleDeterminant = determinant;
                        tripleSolution =
                            (Vector3.Cross(secondNormal, thirdNormal) +
                             Vector3.Cross(thirdNormal, firstNormal) +
                             Vector3.Cross(firstNormal, secondNormal)) /
                            determinant;
                    }
                }
            }

            if (Mathf.Abs(bestTripleDeterminant) >
                IndependentNormalTolerance)
            {
                unitMiter = tripleSolution;
            }
            else
            {
                Vector3 firstNormal = normals[bestFirst];
                Vector3 secondNormal = normals[bestSecond];
                float dot = Vector3.Dot(firstNormal, secondNormal);
                float denominator = 1f + dot;
                if (bestPairIndependence <=
                        IndependentNormalTolerance ||
                    Mathf.Abs(denominator) <=
                        IndependentNormalTolerance)
                {
                    return false;
                }

                unitMiter =
                    (firstNormal + secondNormal) / denominator;
            }

            if (!FiniteMath.IsFinite(unitMiter) ||
                unitMiter.sqrMagnitude >
                    MaximumMiterScale * MaximumMiterScale)
            {
                return false;
            }

            for (int i = 0; i < normals.Count; i++)
            {
                if (Mathf.Abs(
                        Vector3.Dot(normals[i], unitMiter) - 1f) >
                    OffsetPlaneResidualTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryAddSourceEdge(
            int topologyFrom,
            int topologyTo,
            int rawFrom,
            int rawTo,
            Dictionary<ulong, SourceEdgeUse> edges)
        {
            int minimum = Math.Min(topologyFrom, topologyTo);
            int maximum = Math.Max(topologyFrom, topologyTo);
            ulong key =
                ((ulong)(uint)minimum << 32) |
                (uint)maximum;
            int direction = topologyFrom == minimum ? 1 : -1;
            if (!edges.TryGetValue(key, out SourceEdgeUse use))
            {
                edges.Add(
                    key,
                    new SourceEdgeUse(
                        1,
                        direction,
                        rawFrom,
                        rawTo));
                return true;
            }

            if (use.Count >= 2 ||
                use.FirstDirection == direction)
            {
                return false;
            }

            edges[key] = new SourceEdgeUse(
                use.Count + 1,
                use.FirstDirection,
                use.FromVertex,
                use.ToVertex);
            return true;
        }

        private static void AppendRim(
            int sourceFrom,
            int sourceTo,
            int[] outerMap,
            int[] innerMap,
            MeshBuildBuffer buffer)
        {
            int outerFrom = AddRimCopy(
                outerMap[sourceFrom],
                buffer);
            int outerTo = AddRimCopy(
                outerMap[sourceTo],
                buffer);
            int innerFrom = AddRimCopy(
                innerMap[sourceFrom],
                buffer);
            int innerTo = AddRimCopy(
                innerMap[sourceTo],
                buffer);

            buffer.Triangles.Add(outerTo);
            buffer.Triangles.Add(outerFrom);
            buffer.Triangles.Add(innerFrom);

            buffer.Triangles.Add(outerTo);
            buffer.Triangles.Add(innerFrom);
            buffer.Triangles.Add(innerTo);
        }

        private static int AddRimCopy(
            int sourceIndex,
            MeshBuildBuffer buffer)
        {
            MeshBuildVertex source = buffer.Vertices[sourceIndex];
            int copy = buffer.AddVertex(
                source.Position,
                source.SourcePosition,
                source.SourceUv,
                source.PanelIndex,
                source.ProvenanceId);
            buffer.UnionTopology(sourceIndex, copy);
            return copy;
        }

        private static void GetLayerDistances(
            SolidifyOperationDefinition operation,
            out float outerDistance,
            out float innerDistance,
            out bool originalsAreOuter)
        {
            switch (operation.Direction)
            {
                case SolidifyDirection.Inward:
                    outerDistance = 0f;
                    innerDistance = -operation.Thickness;
                    originalsAreOuter = true;
                    return;
                case SolidifyDirection.Outward:
                    outerDistance = operation.Thickness;
                    innerDistance = 0f;
                    originalsAreOuter = false;
                    return;
                default:
                    outerDistance = operation.Thickness * 0.5f;
                    innerDistance = -operation.Thickness * 0.5f;
                    originalsAreOuter = true;
                    return;
            }
        }

        private static void AddNonManifoldDiagnostic(
            SolidifyOperationDefinition operation,
            FoldCanvasCompileResult result,
            int triangleIndex)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.NonManifoldSolidifyInput,
                FoldCanvasDiagnosticSeverity.Error,
                "Solidify source topology must be consistently oriented and manifold before shell construction.",
                operationId: operation.Id,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "triangleIndex",
                        triangleIndex)
                }));
        }

        private readonly struct SourceTriangle
        {
            public SourceTriangle(
                int triangleOffset,
                int a,
                int b,
                int c)
            {
                TriangleOffset = triangleOffset;
                A = a;
                B = b;
                C = c;
            }

            public int TriangleOffset { get; }

            public int A { get; }

            public int B { get; }

            public int C { get; }
        }

        private readonly struct SourceEdgeUse
        {
            public SourceEdgeUse(
                int count,
                int firstDirection,
                int fromVertex,
                int toVertex)
            {
                Count = count;
                FirstDirection = firstDirection;
                FromVertex = fromVertex;
                ToVertex = toVertex;
            }

            public int Count { get; }

            public int FirstDirection { get; }

            public int FromVertex { get; }

            public int ToVertex { get; }
        }
    }
}
