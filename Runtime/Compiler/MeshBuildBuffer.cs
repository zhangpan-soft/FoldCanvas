using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal struct MeshBuildVertex
    {
        public MeshBuildVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex,
            int provenanceId)
        {
            Position = position;
            SourcePosition = sourcePosition;
            SourceUv = sourceUv;
            PanelIndex = panelIndex;
            ProvenanceId = provenanceId;
        }

        public Vector3 Position;

        public readonly Vector2 SourcePosition;

        public readonly Vector2 SourceUv;

        public readonly int PanelIndex;

        public readonly int ProvenanceId;
    }

    internal sealed class MeshBuildBuffer
    {
        private readonly List<PanelBuildRecord> orderedPanels =
            new List<PanelBuildRecord>();

        private readonly Dictionary<string, PanelBuildRecord> panelsById =
            new Dictionary<string, PanelBuildRecord>(StringComparer.Ordinal);

        private readonly List<int> topologyParents = new List<int>();

        public readonly List<MeshBuildVertex> Vertices =
            new List<MeshBuildVertex>();

        public readonly List<int> Triangles = new List<int>();

        public int PanelCount => orderedPanels.Count;

        public bool HasTopologyWelds { get; private set; }

        public int AddVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex)
        {
            int vertexIndex = Vertices.Count;
            Vertices.Add(new MeshBuildVertex(
                position,
                sourcePosition,
                sourceUv,
                panelIndex,
                vertexIndex));
            topologyParents.Add(vertexIndex);
            return vertexIndex;
        }

        public void AddPanel(PanelBuildRecord panel)
        {
            orderedPanels.Add(panel);
            panelsById.Add(panel.PanelId, panel);
        }

        public bool TryGetPanel(string panelId, out PanelBuildRecord panel)
        {
            return panelsById.TryGetValue(panelId, out panel);
        }

        public int GetTopologyId(int vertexIndex)
        {
            return FindTopologyRoot(vertexIndex);
        }

        public void UnionTopology(int firstVertexIndex, int secondVertexIndex)
        {
            int firstRoot = FindTopologyRoot(firstVertexIndex);
            int secondRoot = FindTopologyRoot(secondVertexIndex);
            if (firstRoot == secondRoot)
            {
                return;
            }

            int representative = Math.Min(firstRoot, secondRoot);
            int merged = Math.Max(firstRoot, secondRoot);
            topologyParents[merged] = representative;
            HasTopologyWelds = true;
        }

        public void SnapWeldedTopologyPositions()
        {
            if (!HasTopologyWelds)
            {
                return;
            }

            for (int i = 0; i < Vertices.Count; i++)
            {
                int representative = FindTopologyRoot(i);
                if (representative == i)
                {
                    continue;
                }

                MeshBuildVertex vertex = Vertices[i];
                vertex.Position = Vertices[representative].Position;
                Vertices[i] = vertex;
            }
        }

        public FoldCanvasCompiledData Freeze()
        {
            FoldCanvasCompiledVertex[] compiledVertices =
                new FoldCanvasCompiledVertex[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                MeshBuildVertex vertex = Vertices[i];
                compiledVertices[i] = new FoldCanvasCompiledVertex(
                    vertex.Position,
                    vertex.SourcePosition,
                    vertex.SourceUv,
                    vertex.PanelIndex,
                    vertex.ProvenanceId,
                    FindTopologyRoot(i));
            }

            FoldCanvasCompiledPanel[] compiledPanels =
                new FoldCanvasCompiledPanel[orderedPanels.Count];
            for (int i = 0; i < orderedPanels.Count; i++)
            {
                compiledPanels[i] = orderedPanels[i].Freeze();
            }

            return new FoldCanvasCompiledData(
                compiledVertices,
                Triangles,
                compiledPanels);
        }

        private int FindTopologyRoot(int vertexIndex)
        {
            int root = vertexIndex;
            while (topologyParents[root] != root)
            {
                root = topologyParents[root];
            }

            int current = vertexIndex;
            while (topologyParents[current] != current)
            {
                int next = topologyParents[current];
                topologyParents[current] = root;
                current = next;
            }

            return root;
        }
    }

    internal sealed class PanelBuildRecord
    {
        private readonly List<BoundaryBuildRecord> orderedBoundaries =
            new List<BoundaryBuildRecord>();

        public PanelBuildRecord(
            PanelDefinition panel,
            int panelIndex,
            int vertexStart,
            int vertexCount,
            int triangleIndexStart,
            int triangleIndexCount)
        {
            PanelId = panel.Id;
            PanelIndex = panelIndex;
            Shape = panel.Shape;
            CanvasRect = panel.CanvasRect;
            PhysicalSize = panel.PhysicalSize;
            USegments = panel.USegments;
            VSegments = panel.VSegments;
            VertexStart = vertexStart;
            VertexCount = vertexCount;
            TriangleIndexStart = triangleIndexStart;
            TriangleIndexCount = triangleIndexCount;
        }

        public string PanelId { get; }

        public int PanelIndex { get; }

        public PanelShape Shape { get; }

        public Rect CanvasRect { get; }

        public Vector2 PhysicalSize { get; }

        public int USegments { get; }

        public int VSegments { get; }

        public int VertexStart { get; }

        public int VertexCount { get; }

        public int TriangleIndexStart { get; }

        public int TriangleIndexCount { get; }

        public void AddBoundary(string boundaryId, int[] vertexIndices)
        {
            orderedBoundaries.Add(new BoundaryBuildRecord(
                boundaryId,
                vertexIndices));
        }

        public bool TryGetBoundary(
            string boundaryId,
            out BoundaryBuildRecord boundary)
        {
            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                BoundaryBuildRecord candidate = orderedBoundaries[i];
                if (string.Equals(
                    candidate.Id,
                    boundaryId,
                    StringComparison.Ordinal))
                {
                    boundary = candidate;
                    return true;
                }
            }

            boundary = null;
            return false;
        }

        public FoldCanvasCompiledPanel Freeze()
        {
            FoldCanvasCompiledBoundary[] compiledBoundaries =
                new FoldCanvasCompiledBoundary[orderedBoundaries.Count];
            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                BoundaryBuildRecord boundary = orderedBoundaries[i];
                compiledBoundaries[i] = new FoldCanvasCompiledBoundary(
                    boundary.Id,
                    boundary.VertexIndices);
            }

            return new FoldCanvasCompiledPanel(
                PanelId,
                PanelIndex,
                Shape,
                CanvasRect,
                PhysicalSize,
                VertexStart,
                VertexCount,
                compiledBoundaries);
        }
    }

    internal sealed class BoundaryBuildRecord
    {
        public BoundaryBuildRecord(string id, int[] vertexIndices)
        {
            Id = id;
            VertexIndices = vertexIndices;
        }

        public string Id { get; }

        public int[] VertexIndices { get; }
    }
}
