using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public enum SphereValidationStage
    {
        SurfaceSnapshot = 0,
        PreSolidify = 1,
        PostSolidify = 2
    }

    public sealed class FoldCanvasSphereReport
    {
        private readonly ReadOnlyCollection<string> panelIds;

        private readonly ReadOnlyCollection<string>
            sphericalWrapOperationIds;

        internal FoldCanvasSphereReport(
            string componentId,
            IReadOnlyList<string> orderedPanelIds,
            IReadOnlyList<string> orderedSphericalWrapOperationIds,
            SphereValidationStage validationStage,
            string validationOperationId,
            int validationOperationIndex,
            int sphericalPanelCount,
            int surfaceRenderVertexCount,
            int surfaceTopologyVertexCount,
            int triangleCount,
            int uniqueEdgeCount,
            int openEdgeCount,
            int nonManifoldEdgeCount,
            int orientationConflictEdgeCount,
            int isolatedTopologyVertexCount,
            int connectedComponentCount,
            int eulerCharacteristic,
            int northPoleTopologyCount,
            int southPoleTopologyCount,
            int inwardTriangleCount,
            int inconsistentFrameCount,
            double maximumRadiusError,
            double radiusTolerance)
        {
            ComponentId = componentId ??
                throw new ArgumentNullException(nameof(componentId));
            panelIds = CopyStrings(
                orderedPanelIds,
                nameof(orderedPanelIds));
            sphericalWrapOperationIds = CopyStrings(
                orderedSphericalWrapOperationIds,
                nameof(orderedSphericalWrapOperationIds));
            ValidationStage = validationStage;
            ValidationOperationId = validationOperationId;
            ValidationOperationIndex = validationOperationIndex;
            SphericalPanelCount = sphericalPanelCount;
            SurfaceRenderVertexCount = surfaceRenderVertexCount;
            SurfaceTopologyVertexCount = surfaceTopologyVertexCount;
            TriangleCount = triangleCount;
            UniqueEdgeCount = uniqueEdgeCount;
            OpenEdgeCount = openEdgeCount;
            NonManifoldEdgeCount = nonManifoldEdgeCount;
            OrientationConflictEdgeCount =
                orientationConflictEdgeCount;
            IsolatedTopologyVertexCount =
                isolatedTopologyVertexCount;
            ConnectedComponentCount = connectedComponentCount;
            EulerCharacteristic = eulerCharacteristic;
            NorthPoleTopologyCount = northPoleTopologyCount;
            SouthPoleTopologyCount = southPoleTopologyCount;
            InwardTriangleCount = inwardTriangleCount;
            InconsistentFrameCount = inconsistentFrameCount;
            MaximumRadiusError = maximumRadiusError;
            RadiusTolerance = radiusTolerance;
        }

        public string ComponentId { get; }

        public IReadOnlyList<string> PanelIds => panelIds;

        public IReadOnlyList<string> SphericalWrapOperationIds =>
            sphericalWrapOperationIds;

        public SphereValidationStage ValidationStage { get; }

        public string ValidationOperationId { get; }

        public int ValidationOperationIndex { get; }

        public bool IsPreSolidify =>
            ValidationStage == SphereValidationStage.PreSolidify;

        public int SphericalPanelCount { get; }

        public int SurfaceRenderVertexCount { get; }

        public int SurfaceTopologyVertexCount { get; }

        public int TriangleCount { get; }

        public int UniqueEdgeCount { get; }

        public int OpenEdgeCount { get; }

        public int NonManifoldEdgeCount { get; }

        public int OrientationConflictEdgeCount { get; }

        public int IsolatedTopologyVertexCount { get; }

        public int ConnectedComponentCount { get; }

        public int EulerCharacteristic { get; }

        public int NorthPoleTopologyCount { get; }

        public int SouthPoleTopologyCount { get; }

        public int InwardTriangleCount { get; }

        public int InconsistentFrameCount { get; }

        public double MaximumRadiusError { get; }

        public double RadiusTolerance { get; }

        public bool IsClosedSphere =>
            SphericalPanelCount > 0 &&
            TriangleCount > 0 &&
            ConnectedComponentCount == 1 &&
            OpenEdgeCount == 0 &&
            NonManifoldEdgeCount == 0 &&
            OrientationConflictEdgeCount == 0 &&
            IsolatedTopologyVertexCount == 0 &&
            EulerCharacteristic == 2 &&
            NorthPoleTopologyCount == 1 &&
            SouthPoleTopologyCount == 1 &&
            InwardTriangleCount == 0 &&
            InconsistentFrameCount == 0 &&
            MaximumRadiusError <= RadiusTolerance;

        private static ReadOnlyCollection<string> CopyStrings(
            IReadOnlyList<string> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            string[] copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i] ??
                    throw new ArgumentException(
                        "Sphere report identifiers cannot contain null.",
                        parameterName);
            }

            return Array.AsReadOnly(copy);
        }
    }

    /// <summary>
    /// Validates component topology, manifold edge incidence, pole identity,
    /// radius agreement, frame consistency, and winding. This validator does
    /// not perform global triangle-triangle self-intersection detection.
    /// </summary>
    public static class FoldCanvasSphereValidator
    {
        public static FoldCanvasSphereReport Analyze(
            FoldCanvasCompiledData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            List<string> panelIds = new List<string>();
            List<string> operationIds = new List<string>();
            for (int i = 0; i < data.SphericalSurfaces.Count; i++)
            {
                panelIds.Add(data.SphericalSurfaces[i].PanelId);
                operationIds.Add(
                    data.SphericalSurfaces[i].OperationId);
            }

            return Analyze(
                data,
                "surface-snapshot",
                panelIds,
                operationIds,
                SphereValidationStage.SurfaceSnapshot,
                null,
                -1);
        }

        public static FoldCanvasSphereReport Analyze(
            FoldCanvasCompiledData data,
            string componentId,
            IReadOnlyList<string> orderedPanelIds,
            IReadOnlyList<string> orderedSphericalWrapOperationIds,
            SphereValidationStage validationStage,
            string validationOperationId,
            int validationOperationIndex)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrWhiteSpace(componentId))
            {
                throw new ArgumentException(
                    "Sphere component IDs must be non-empty.",
                    nameof(componentId));
            }

            if (orderedPanelIds == null)
            {
                throw new ArgumentNullException(
                    nameof(orderedPanelIds));
            }

            HashSet<string> selectedPanelIds =
                new HashSet<string>(
                    orderedPanelIds,
                    StringComparer.Ordinal);
            HashSet<int> surfaceRenderVertices = new HashSet<int>();
            HashSet<int> allSurfaceTopology = new HashSet<int>();
            HashSet<int> northPoles = new HashSet<int>();
            HashSet<int> southPoles = new HashSet<int>();
            Dictionary<int, FoldCanvasCompiledSphericalSurface>
                surfacesByPanel =
                    new Dictionary<int,
                        FoldCanvasCompiledSphericalSurface>();
            double maximumRadiusError = 0d;
            double radiusTolerance = 0d;
            int inconsistentFrameCount = 0;
            int selectedSurfaceCount = 0;
            FoldCanvasCompiledSphericalSurface referenceSurface = null;

            for (int i = 0; i < data.SphericalSurfaces.Count; i++)
            {
                FoldCanvasCompiledSphericalSurface surface =
                    data.SphericalSurfaces[i];
                if (!selectedPanelIds.Contains(surface.PanelId))
                {
                    continue;
                }

                selectedSurfaceCount++;
                surfacesByPanel.Add(surface.PanelIndex, surface);
                if (referenceSurface == null)
                {
                    referenceSurface = surface;
                }
                else
                {
                    double frameTolerance =
                        FoldCanvasGeometryTolerances
                            .SphericalRadiusAbsoluteTolerance +
                        FoldCanvasGeometryTolerances
                            .SphericalRadiusRelativeTolerance *
                        Math.Max(
                            referenceSurface.Radius,
                            surface.Radius);
                    if (Vector3.Distance(
                            referenceSurface.Center,
                            surface.Center) > frameTolerance ||
                        Math.Abs(
                            referenceSurface.Radius -
                            surface.Radius) > frameTolerance)
                    {
                        inconsistentFrameCount++;
                    }
                }

                if (surface.HasNorthPole)
                {
                    northPoles.Add(
                        surface.NorthPoleTopologyVertexId);
                }

                if (surface.HasSouthPole)
                {
                    southPoles.Add(
                        surface.SouthPoleTopologyVertexId);
                }

                double surfaceTolerance =
                    FoldCanvasGeometryTolerances
                        .SphericalRadiusAbsoluteTolerance +
                    FoldCanvasGeometryTolerances
                        .SphericalRadiusRelativeTolerance *
                    surface.Radius;
                radiusTolerance = Math.Max(
                    radiusTolerance,
                    surfaceTolerance);
                for (int vertexOffset = 0;
                    vertexOffset < surface.SurfaceVertexIndices.Count;
                    vertexOffset++)
                {
                    int vertexIndex =
                        surface.SurfaceVertexIndices[vertexOffset];
                    surfaceRenderVertices.Add(vertexIndex);
                    FoldCanvasCompiledVertex vertex =
                        data.Vertices[vertexIndex];
                    allSurfaceTopology.Add(
                        vertex.TopologyVertexId);
                    double radiusError = Math.Abs(
                        Vector3.Distance(
                            vertex.Position,
                            surface.Center) -
                        surface.Radius);
                    maximumRadiusError = Math.Max(
                        maximumRadiusError,
                        radiusError);
                }
            }

            Dictionary<ulong, EdgeUse> edges =
                new Dictionary<ulong, EdgeUse>();
            HashSet<int> referencedTopology = new HashSet<int>();
            Dictionary<int, HashSet<int>> adjacency =
                new Dictionary<int, HashSet<int>>();
            int triangleCount = 0;
            int inwardTriangleCount = 0;
            int orientationConflictFromCollapsedTriangles = 0;
            for (int offset = 0;
                offset < data.TriangleIndices.Count;
                offset += 3)
            {
                int aIndex = data.TriangleIndices[offset];
                int bIndex = data.TriangleIndices[offset + 1];
                int cIndex = data.TriangleIndices[offset + 2];
                if (!surfaceRenderVertices.Contains(aIndex) ||
                    !surfaceRenderVertices.Contains(bIndex) ||
                    !surfaceRenderVertices.Contains(cIndex))
                {
                    continue;
                }

                triangleCount++;
                FoldCanvasCompiledVertex a = data.Vertices[aIndex];
                FoldCanvasCompiledVertex b = data.Vertices[bIndex];
                FoldCanvasCompiledVertex c = data.Vertices[cIndex];
                int aTopology = a.TopologyVertexId;
                int bTopology = b.TopologyVertexId;
                int cTopology = c.TopologyVertexId;
                referencedTopology.Add(aTopology);
                referencedTopology.Add(bTopology);
                referencedTopology.Add(cTopology);
                AddAdjacency(adjacency, aTopology, bTopology);
                AddAdjacency(adjacency, bTopology, cTopology);
                AddAdjacency(adjacency, cTopology, aTopology);

                if (aTopology == bTopology ||
                    bTopology == cTopology ||
                    cTopology == aTopology)
                {
                    orientationConflictFromCollapsedTriangles++;
                    continue;
                }

                AddEdge(edges, aTopology, bTopology);
                AddEdge(edges, bTopology, cTopology);
                AddEdge(edges, cTopology, aTopology);

                if (surfacesByPanel.TryGetValue(
                        a.PanelIndex,
                        out FoldCanvasCompiledSphericalSurface surface))
                {
                    Vector3 cross = Vector3.Cross(
                        b.Position - a.Position,
                        c.Position - a.Position);
                    Vector3 centroid =
                        (a.Position + b.Position + c.Position) / 3f;
                    if (Vector3.Dot(
                            cross,
                            centroid - surface.Center) <= 0f)
                    {
                        inwardTriangleCount++;
                    }
                }
            }

            int openEdges = 0;
            int nonManifoldEdges = 0;
            int orientationConflicts =
                orientationConflictFromCollapsedTriangles;
            foreach (KeyValuePair<ulong, EdgeUse> pair in edges)
            {
                EdgeUse edge = pair.Value;
                if (edge.Count == 1)
                {
                    openEdges++;
                }
                else if (edge.Count > 2)
                {
                    nonManifoldEdges++;
                }
                else if (edge.DirectionTotal != 0)
                {
                    orientationConflicts++;
                }
            }

            int isolatedTopologyVertices =
                allSurfaceTopology.Count - referencedTopology.Count;
            int connectedComponents = CountConnectedComponents(
                referencedTopology,
                adjacency);
            int eulerCharacteristic =
                referencedTopology.Count -
                edges.Count +
                triangleCount;
            return new FoldCanvasSphereReport(
                componentId,
                orderedPanelIds,
                orderedSphericalWrapOperationIds,
                validationStage,
                validationOperationId,
                validationOperationIndex,
                selectedSurfaceCount,
                surfaceRenderVertices.Count,
                referencedTopology.Count,
                triangleCount,
                edges.Count,
                openEdges,
                nonManifoldEdges,
                orientationConflicts,
                isolatedTopologyVertices,
                connectedComponents,
                eulerCharacteristic,
                northPoles.Count,
                southPoles.Count,
                inwardTriangleCount,
                inconsistentFrameCount,
                maximumRadiusError,
                radiusTolerance);
        }

        private static void AddEdge(
            Dictionary<ulong, EdgeUse> edges,
            int from,
            int to)
        {
            int minimum = Math.Min(from, to);
            int maximum = Math.Max(from, to);
            ulong key =
                ((ulong)(uint)minimum << 32) |
                (uint)maximum;
            int direction = from == minimum ? 1 : -1;
            if (edges.TryGetValue(key, out EdgeUse existing))
            {
                edges[key] = new EdgeUse(
                    existing.Count + 1,
                    existing.DirectionTotal + direction);
            }
            else
            {
                edges.Add(key, new EdgeUse(1, direction));
            }
        }

        private static void AddAdjacency(
            Dictionary<int, HashSet<int>> adjacency,
            int first,
            int second)
        {
            if (!adjacency.TryGetValue(
                    first,
                    out HashSet<int> firstNeighbors))
            {
                firstNeighbors = new HashSet<int>();
                adjacency.Add(first, firstNeighbors);
            }

            if (!adjacency.TryGetValue(
                    second,
                    out HashSet<int> secondNeighbors))
            {
                secondNeighbors = new HashSet<int>();
                adjacency.Add(second, secondNeighbors);
            }

            firstNeighbors.Add(second);
            secondNeighbors.Add(first);
        }

        private static int CountConnectedComponents(
            HashSet<int> topologyVertices,
            Dictionary<int, HashSet<int>> adjacency)
        {
            List<int> ordered = new List<int>(topologyVertices);
            ordered.Sort();
            HashSet<int> visited = new HashSet<int>();
            int components = 0;
            Queue<int> queue = new Queue<int>();
            for (int i = 0; i < ordered.Count; i++)
            {
                int start = ordered[i];
                if (!visited.Add(start))
                {
                    continue;
                }

                components++;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    if (!adjacency.TryGetValue(
                            current,
                            out HashSet<int> neighbors))
                    {
                        continue;
                    }

                    List<int> orderedNeighbors =
                        new List<int>(neighbors);
                    orderedNeighbors.Sort();
                    for (int neighborIndex = 0;
                        neighborIndex < orderedNeighbors.Count;
                        neighborIndex++)
                    {
                        int neighbor =
                            orderedNeighbors[neighborIndex];
                        if (visited.Add(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return components;
        }

        private readonly struct EdgeUse
        {
            public EdgeUse(int count, int directionTotal)
            {
                Count = count;
                DirectionTotal = directionTotal;
            }

            public int Count { get; }

            public int DirectionTotal { get; }
        }
    }
}
