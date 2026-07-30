using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public sealed class FoldCanvasClosedVolumeComponentReport
    {
        internal FoldCanvasClosedVolumeComponentReport(
            int minimumTopologyVertexId,
            int topologyVertexCount,
            int triangleCount,
            double signedVolume,
            double minimumAcceptedAbsoluteVolume)
        {
            MinimumTopologyVertexId = minimumTopologyVertexId;
            TopologyVertexCount = topologyVertexCount;
            TriangleCount = triangleCount;
            SignedVolume = signedVolume;
            MinimumAcceptedAbsoluteVolume =
                minimumAcceptedAbsoluteVolume;
        }

        public int MinimumTopologyVertexId { get; }

        public int TopologyVertexCount { get; }

        public int TriangleCount { get; }

        public double SignedVolume { get; }

        public double AbsoluteVolume => Math.Abs(SignedVolume);

        public double MinimumAcceptedAbsoluteVolume { get; }

        public bool HasNonZeroVolume =>
            AbsoluteVolume > MinimumAcceptedAbsoluteVolume;
    }

    public sealed class FoldCanvasClosedVolumeReport
    {
        private readonly ReadOnlyCollection<
            FoldCanvasClosedVolumeComponentReport> components;

        internal FoldCanvasClosedVolumeReport(
            string operationId,
            int triangleCount,
            int topologyVertexCount,
            int uniqueEdgeCount,
            int twoIncidentEdgeCount,
            int openEdgeCount,
            int nonManifoldEdgeCount,
            int orientationConflictEdgeCount,
            int collapsedTopologyEdgeCount,
            int topologyPositionConflictCount,
            int partiallySelectedTriangleCount,
            IReadOnlyList<FoldCanvasClosedVolumeComponentReport>
                sourceComponents)
        {
            OperationId = operationId;
            TriangleCount = triangleCount;
            TopologyVertexCount = topologyVertexCount;
            UniqueEdgeCount = uniqueEdgeCount;
            TwoIncidentEdgeCount = twoIncidentEdgeCount;
            OpenEdgeCount = openEdgeCount;
            NonManifoldEdgeCount = nonManifoldEdgeCount;
            OrientationConflictEdgeCount =
                orientationConflictEdgeCount;
            CollapsedTopologyEdgeCount =
                collapsedTopologyEdgeCount;
            TopologyPositionConflictCount =
                topologyPositionConflictCount;
            PartiallySelectedTriangleCount =
                partiallySelectedTriangleCount;

            FoldCanvasClosedVolumeComponentReport[] copied =
                new FoldCanvasClosedVolumeComponentReport[
                    sourceComponents.Count];
            int zeroVolumeComponents = 0;
            double signedVolume = 0d;
            double totalAbsoluteVolume = 0d;
            for (int i = 0; i < sourceComponents.Count; i++)
            {
                FoldCanvasClosedVolumeComponentReport component =
                    sourceComponents[i];
                copied[i] = component;
                signedVolume += component.SignedVolume;
                totalAbsoluteVolume += component.AbsoluteVolume;
                if (!component.HasNonZeroVolume)
                {
                    zeroVolumeComponents++;
                }
            }

            components = Array.AsReadOnly(copied);
            ZeroVolumeComponentCount = zeroVolumeComponents;
            SignedVolume = signedVolume;
            TotalAbsoluteVolume = totalAbsoluteVolume;
        }

        public string OperationId { get; }

        public int TriangleCount { get; }

        public int TopologyVertexCount { get; }

        public int UniqueEdgeCount { get; }

        public int TwoIncidentEdgeCount { get; }

        public int OpenEdgeCount { get; }

        public int NonManifoldEdgeCount { get; }

        public int OrientationConflictEdgeCount { get; }

        public int CollapsedTopologyEdgeCount { get; }

        public int TopologyPositionConflictCount { get; }

        public int PartiallySelectedTriangleCount { get; }

        public int ComponentCount => components.Count;

        public int ZeroVolumeComponentCount { get; }

        public double SignedVolume { get; }

        public double TotalAbsoluteVolume { get; }

        public IReadOnlyList<FoldCanvasClosedVolumeComponentReport>
            Components => components;

        public bool IsClosedVolume =>
            TriangleCount > 0 &&
            UniqueEdgeCount > 0 &&
            TwoIncidentEdgeCount == UniqueEdgeCount &&
            OpenEdgeCount == 0 &&
            NonManifoldEdgeCount == 0 &&
            OrientationConflictEdgeCount == 0 &&
            CollapsedTopologyEdgeCount == 0 &&
            TopologyPositionConflictCount == 0 &&
            PartiallySelectedTriangleCount == 0 &&
            ComponentCount > 0 &&
            ZeroVolumeComponentCount == 0;

        public bool IsSingleClosedVolume =>
            IsClosedVolume && ComponentCount == 1;
    }

    public static class FoldCanvasClosedVolumeValidator
    {
        public static FoldCanvasClosedVolumeReport Analyze(
            FoldCanvasCompiledData data)
        {
            return Analyze(data, null, null);
        }

        internal static FoldCanvasClosedVolumeReport Analyze(
            FoldCanvasCompiledData data,
            ISet<int> includedTopologyVertexIds,
            string operationId)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Dictionary<int, Vector3> topologyPositions =
                new Dictionary<int, Vector3>();
            HashSet<int> positionConflicts = new HashSet<int>();
            float positionToleranceSquared =
                FoldCanvasGeometryTolerances
                    .ClosedVolumeTopologyPositionTolerance *
                FoldCanvasGeometryTolerances
                    .ClosedVolumeTopologyPositionTolerance;
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                int topologyId = vertex.TopologyVertexId;
                if (includedTopologyVertexIds != null &&
                    !includedTopologyVertexIds.Contains(topologyId))
                {
                    continue;
                }

                if (topologyPositions.TryGetValue(
                        topologyId,
                        out Vector3 position))
                {
                    if ((position - vertex.Position).sqrMagnitude >
                        positionToleranceSquared)
                    {
                        positionConflicts.Add(topologyId);
                    }
                }
                else
                {
                    topologyPositions.Add(
                        topologyId,
                        vertex.Position);
                }
            }

            List<TriangleRecord> triangles =
                new List<TriangleRecord>();
            int partiallySelectedTriangleCount = 0;
            int collapsedTopologyEdgeCount = 0;
            Dictionary<ulong, EdgeUse> edges =
                new Dictionary<ulong, EdgeUse>();
            IReadOnlyList<int> indices = data.TriangleIndices;
            for (int offset = 0; offset < indices.Count; offset += 3)
            {
                int a = data.Vertices[indices[offset]]
                    .TopologyVertexId;
                int b = data.Vertices[indices[offset + 1]]
                    .TopologyVertexId;
                int c = data.Vertices[indices[offset + 2]]
                    .TopologyVertexId;
                if (includedTopologyVertexIds != null)
                {
                    int included =
                        (includedTopologyVertexIds.Contains(a) ? 1 : 0) +
                        (includedTopologyVertexIds.Contains(b) ? 1 : 0) +
                        (includedTopologyVertexIds.Contains(c) ? 1 : 0);
                    if (included == 0)
                    {
                        continue;
                    }

                    if (included != 3)
                    {
                        partiallySelectedTriangleCount++;
                        continue;
                    }
                }

                triangles.Add(new TriangleRecord(a, b, c));
                collapsedTopologyEdgeCount +=
                    AddEdge(edges, a, b);
                collapsedTopologyEdgeCount +=
                    AddEdge(edges, b, c);
                collapsedTopologyEdgeCount +=
                    AddEdge(edges, c, a);
            }

            int openEdgeCount = 0;
            int twoIncidentEdgeCount = 0;
            int nonManifoldEdgeCount = 0;
            int orientationConflictEdgeCount = 0;
            foreach (KeyValuePair<ulong, EdgeUse> pair in edges)
            {
                EdgeUse use = pair.Value;
                if (use.Count == 1)
                {
                    openEdgeCount++;
                }
                else if (use.Count == 2)
                {
                    twoIncidentEdgeCount++;
                    if (use.HasOrientationConflict)
                    {
                        orientationConflictEdgeCount++;
                    }
                }
                else
                {
                    nonManifoldEdgeCount++;
                }
            }

            List<FoldCanvasClosedVolumeComponentReport> components =
                BuildComponents(triangles, topologyPositions);
            return new FoldCanvasClosedVolumeReport(
                operationId,
                triangles.Count,
                topologyPositions.Count,
                edges.Count,
                twoIncidentEdgeCount,
                openEdgeCount,
                nonManifoldEdgeCount,
                orientationConflictEdgeCount,
                collapsedTopologyEdgeCount,
                positionConflicts.Count,
                partiallySelectedTriangleCount,
                components);
        }

        private static int AddEdge(
            Dictionary<ulong, EdgeUse> edges,
            int from,
            int to)
        {
            if (from == to)
            {
                return 1;
            }

            int minimum = Math.Min(from, to);
            int maximum = Math.Max(from, to);
            ulong key =
                ((ulong)(uint)minimum << 32) |
                (uint)maximum;
            int direction = from == minimum ? 1 : -1;
            if (!edges.TryGetValue(key, out EdgeUse use))
            {
                edges.Add(key, new EdgeUse(1, direction, false));
                return 0;
            }

            bool conflict =
                use.HasOrientationConflict ||
                use.FirstDirection == direction;
            edges[key] = new EdgeUse(
                use.Count + 1,
                use.FirstDirection,
                conflict);
            return 0;
        }

        private static List<FoldCanvasClosedVolumeComponentReport>
            BuildComponents(
                List<TriangleRecord> triangles,
                Dictionary<int, Vector3> topologyPositions)
        {
            Dictionary<int, int> parents =
                new Dictionary<int, int>();
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleRecord triangle = triangles[i];
                EnsureParent(parents, triangle.A);
                EnsureParent(parents, triangle.B);
                EnsureParent(parents, triangle.C);
                Union(parents, triangle.A, triangle.B);
                Union(parents, triangle.B, triangle.C);
            }

            Dictionary<int, ComponentAccumulator> accumulators =
                new Dictionary<int, ComponentAccumulator>();
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleRecord triangle = triangles[i];
                int root = Find(parents, triangle.A);
                if (!accumulators.TryGetValue(
                        root,
                        out ComponentAccumulator accumulator))
                {
                    accumulator = new ComponentAccumulator(root);
                    accumulators.Add(root, accumulator);
                }

                accumulator.Triangles.Add(triangle);
                accumulator.TopologyVertexIds.Add(triangle.A);
                accumulator.TopologyVertexIds.Add(triangle.B);
                accumulator.TopologyVertexIds.Add(triangle.C);
            }

            List<int> roots = new List<int>(accumulators.Keys);
            roots.Sort();
            List<FoldCanvasClosedVolumeComponentReport> reports =
                new List<FoldCanvasClosedVolumeComponentReport>(
                    roots.Count);
            for (int i = 0; i < roots.Count; i++)
            {
                ComponentAccumulator accumulator =
                    accumulators[roots[i]];
                Vector3 reference =
                    topologyPositions[
                        accumulator.MinimumTopologyVertexId];
                double signedVolume = 0d;
                Bounds bounds = new Bounds(reference, Vector3.zero);
                foreach (int topologyId in
                    accumulator.TopologyVertexIds)
                {
                    bounds.Encapsulate(topologyPositions[topologyId]);
                }

                for (int triangleIndex = 0;
                    triangleIndex < accumulator.Triangles.Count;
                    triangleIndex++)
                {
                    TriangleRecord triangle =
                        accumulator.Triangles[triangleIndex];
                    Vector3 a =
                        topologyPositions[triangle.A] - reference;
                    Vector3 b =
                        topologyPositions[triangle.B] - reference;
                    Vector3 c =
                        topologyPositions[triangle.C] - reference;
                    signedVolume += SignedTetrahedronVolume(a, b, c);
                }

                double scale = Math.Max(
                    bounds.size.x,
                    Math.Max(bounds.size.y, bounds.size.z));
                double volumeTolerance = Math.Max(
                    FoldCanvasGeometryTolerances
                        .MinimumClosedVolume,
                    scale * scale * scale *
                    FoldCanvasGeometryTolerances
                        .RelativeClosedVolumeTolerance);
                reports.Add(
                    new FoldCanvasClosedVolumeComponentReport(
                        accumulator.MinimumTopologyVertexId,
                        accumulator.TopologyVertexIds.Count,
                        accumulator.Triangles.Count,
                        signedVolume,
                        volumeTolerance));
            }

            return reports;
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

        private static void EnsureParent(
            Dictionary<int, int> parents,
            int topologyId)
        {
            if (!parents.ContainsKey(topologyId))
            {
                parents.Add(topologyId, topologyId);
            }
        }

        private static int Find(
            Dictionary<int, int> parents,
            int topologyId)
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

        private static void Union(
            Dictionary<int, int> parents,
            int first,
            int second)
        {
            int firstRoot = Find(parents, first);
            int secondRoot = Find(parents, second);
            if (firstRoot == secondRoot)
            {
                return;
            }

            int representative = Math.Min(firstRoot, secondRoot);
            int merged = Math.Max(firstRoot, secondRoot);
            parents[merged] = representative;
        }

        private readonly struct TriangleRecord
        {
            public TriangleRecord(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }

            public int A { get; }

            public int B { get; }

            public int C { get; }
        }

        private readonly struct EdgeUse
        {
            public EdgeUse(
                int count,
                int firstDirection,
                bool hasOrientationConflict)
            {
                Count = count;
                FirstDirection = firstDirection;
                HasOrientationConflict = hasOrientationConflict;
            }

            public int Count { get; }

            public int FirstDirection { get; }

            public bool HasOrientationConflict { get; }
        }

        private sealed class ComponentAccumulator
        {
            public ComponentAccumulator(int minimumTopologyVertexId)
            {
                MinimumTopologyVertexId = minimumTopologyVertexId;
            }

            public int MinimumTopologyVertexId { get; }

            public List<TriangleRecord> Triangles { get; } =
                new List<TriangleRecord>();

            public HashSet<int> TopologyVertexIds { get; } =
                new HashSet<int>();
        }
    }
}
