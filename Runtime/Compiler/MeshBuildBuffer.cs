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

        public readonly List<MeshBuildVertex> Vertices =
            new List<MeshBuildVertex>();

        public readonly List<int> Triangles = new List<int>();

        public int PanelCount => orderedPanels.Count;

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
                    vertex.ProvenanceId);
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
    }

    internal sealed class PanelBuildRecord
    {
        private readonly List<BoundaryBuildRecord> orderedBoundaries =
            new List<BoundaryBuildRecord>();

        public PanelBuildRecord(
            PanelDefinition panel,
            int panelIndex,
            int vertexStart,
            int vertexCount)
        {
            PanelId = panel.Id;
            PanelIndex = panelIndex;
            Shape = panel.Shape;
            CanvasRect = panel.CanvasRect;
            PhysicalSize = panel.PhysicalSize;
            VertexStart = vertexStart;
            VertexCount = vertexCount;
        }

        public string PanelId { get; }

        public int PanelIndex { get; }

        public PanelShape Shape { get; }

        public Rect CanvasRect { get; }

        public Vector2 PhysicalSize { get; }

        public int VertexStart { get; }

        public int VertexCount { get; }

        public void AddBoundary(string boundaryId, int[] vertexIndices)
        {
            orderedBoundaries.Add(new BoundaryBuildRecord(
                boundaryId,
                vertexIndices));
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
