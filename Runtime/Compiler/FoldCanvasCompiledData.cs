using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public readonly struct FoldCanvasCompiledVertex :
        IEquatable<FoldCanvasCompiledVertex>
    {
        internal FoldCanvasCompiledVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex,
            int provenanceId,
            int topologyVertexId)
        {
            Position = position;
            SourcePosition = sourcePosition;
            SourceUv = sourceUv;
            PanelIndex = panelIndex;
            ProvenanceId = provenanceId;
            TopologyVertexId = topologyVertexId;
        }

        public Vector3 Position { get; }

        public Vector2 SourcePosition { get; }

        public Vector2 SourceUv { get; }

        public int PanelIndex { get; }

        public int ProvenanceId { get; }

        public int TopologyVertexId { get; }

        public bool Equals(FoldCanvasCompiledVertex other)
        {
            return Position.Equals(other.Position) &&
                SourcePosition.Equals(other.SourcePosition) &&
                SourceUv.Equals(other.SourceUv) &&
                PanelIndex == other.PanelIndex &&
                ProvenanceId == other.ProvenanceId &&
                TopologyVertexId == other.TopologyVertexId;
        }

        public override bool Equals(object obj)
        {
            return obj is FoldCanvasCompiledVertex other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Position.GetHashCode();
                hash = (hash * 397) ^ SourcePosition.GetHashCode();
                hash = (hash * 397) ^ SourceUv.GetHashCode();
                hash = (hash * 397) ^ PanelIndex;
                hash = (hash * 397) ^ ProvenanceId;
                hash = (hash * 397) ^ TopologyVertexId;
                return hash;
            }
        }
    }

    public sealed class FoldCanvasCompiledBoundary
    {
        private readonly ReadOnlyCollection<int> vertexIndices;

        internal FoldCanvasCompiledBoundary(string id, IReadOnlyList<int> indices)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            int[] copiedIndices = new int[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                copiedIndices[i] = indices[i];
            }

            vertexIndices = Array.AsReadOnly(copiedIndices);
        }

        public string Id { get; }

        public IReadOnlyList<int> VertexIndices => vertexIndices;
    }

    public sealed class FoldCanvasCompiledPanel
    {
        private readonly ReadOnlyCollection<FoldCanvasCompiledBoundary> boundaries;

        internal FoldCanvasCompiledPanel(
            string id,
            int panelIndex,
            PanelShape shape,
            Rect canvasRect,
            Vector2 physicalSize,
            int vertexStart,
            int vertexCount,
            IReadOnlyList<FoldCanvasCompiledBoundary> orderedBoundaries)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            PanelIndex = panelIndex;
            Shape = shape;
            CanvasRect = canvasRect;
            PhysicalSize = physicalSize;
            VertexStart = vertexStart;
            VertexCount = vertexCount;

            if (orderedBoundaries == null)
            {
                throw new ArgumentNullException(nameof(orderedBoundaries));
            }

            FoldCanvasCompiledBoundary[] copiedBoundaries =
                new FoldCanvasCompiledBoundary[orderedBoundaries.Count];
            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                copiedBoundaries[i] = orderedBoundaries[i];
            }

            boundaries = Array.AsReadOnly(copiedBoundaries);
        }

        public string Id { get; }

        public int PanelIndex { get; }

        public PanelShape Shape { get; }

        public Rect CanvasRect { get; }

        public Vector2 PhysicalSize { get; }

        public int VertexStart { get; }

        public int VertexCount { get; }

        public IReadOnlyList<FoldCanvasCompiledBoundary> Boundaries => boundaries;

        public bool TryGetBoundary(
            string boundaryId,
            out FoldCanvasCompiledBoundary boundary)
        {
            for (int i = 0; i < boundaries.Count; i++)
            {
                if (string.Equals(
                    boundaries[i].Id,
                    boundaryId,
                    StringComparison.Ordinal))
                {
                    boundary = boundaries[i];
                    return true;
                }
            }

            boundary = null;
            return false;
        }

        public FoldCanvasCompiledBoundary GetBoundary(string boundaryId)
        {
            if (TryGetBoundary(boundaryId, out FoldCanvasCompiledBoundary boundary))
            {
                return boundary;
            }

            throw new KeyNotFoundException(
                $"Panel '{Id}' has no boundary named '{boundaryId}'.");
        }
    }

    public sealed class FoldCanvasCompiledData
    {
        private readonly ReadOnlyCollection<FoldCanvasCompiledVertex> vertices;
        private readonly ReadOnlyCollection<int> triangleIndices;
        private readonly ReadOnlyCollection<FoldCanvasCompiledPanel> panels;

        private readonly int topologyVertexCount;

        internal FoldCanvasCompiledData(
            IReadOnlyList<FoldCanvasCompiledVertex> sourceVertices,
            IReadOnlyList<int> sourceTriangleIndices,
            IReadOnlyList<FoldCanvasCompiledPanel> sourcePanels)
        {
            if (sourceVertices == null)
            {
                throw new ArgumentNullException(nameof(sourceVertices));
            }

            if (sourceTriangleIndices == null)
            {
                throw new ArgumentNullException(nameof(sourceTriangleIndices));
            }

            if (sourcePanels == null)
            {
                throw new ArgumentNullException(nameof(sourcePanels));
            }

            FoldCanvasCompiledVertex[] copiedVertices =
                new FoldCanvasCompiledVertex[sourceVertices.Count];
            for (int i = 0; i < sourceVertices.Count; i++)
            {
                copiedVertices[i] = sourceVertices[i];
            }

            int[] copiedTriangleIndices = new int[sourceTriangleIndices.Count];
            for (int i = 0; i < sourceTriangleIndices.Count; i++)
            {
                copiedTriangleIndices[i] = sourceTriangleIndices[i];
            }

            FoldCanvasCompiledPanel[] copiedPanels =
                new FoldCanvasCompiledPanel[sourcePanels.Count];
            for (int i = 0; i < sourcePanels.Count; i++)
            {
                copiedPanels[i] = sourcePanels[i];
            }

            vertices = Array.AsReadOnly(copiedVertices);
            triangleIndices = Array.AsReadOnly(copiedTriangleIndices);
            panels = Array.AsReadOnly(copiedPanels);

            bool[] seenTopologyVertices = new bool[copiedVertices.Length];
            int uniqueTopologyVertices = 0;
            for (int i = 0; i < copiedVertices.Length; i++)
            {
                int topologyVertexId = copiedVertices[i].TopologyVertexId;
                if (topologyVertexId < 0 ||
                    topologyVertexId >= copiedVertices.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceVertices),
                        "Topology vertex IDs must reference a source vertex.");
                }

                if (!seenTopologyVertices[topologyVertexId])
                {
                    seenTopologyVertices[topologyVertexId] = true;
                    uniqueTopologyVertices++;
                }
            }

            topologyVertexCount = uniqueTopologyVertices;
        }

        public IReadOnlyList<FoldCanvasCompiledVertex> Vertices => vertices;

        public IReadOnlyList<int> TriangleIndices => triangleIndices;

        public IReadOnlyList<FoldCanvasCompiledPanel> Panels => panels;

        public int TopologyVertexCount => topologyVertexCount;

        public bool TryGetPanel(string panelId, out FoldCanvasCompiledPanel panel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (string.Equals(panels[i].Id, panelId, StringComparison.Ordinal))
                {
                    panel = panels[i];
                    return true;
                }
            }

            panel = null;
            return false;
        }

        public FoldCanvasCompiledPanel GetPanel(string panelId)
        {
            if (TryGetPanel(panelId, out FoldCanvasCompiledPanel panel))
            {
                return panel;
            }

            throw new KeyNotFoundException(
                $"Compiled data has no panel named '{panelId}'.");
        }
    }
}
