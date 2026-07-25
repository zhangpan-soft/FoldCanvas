using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal sealed class MeshBuildBuffer
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();
        public readonly Dictionary<string, PanelBuildRecord> Panels =
            new Dictionary<string, PanelBuildRecord>(System.StringComparer.Ordinal);
    }

    internal sealed class PanelBuildRecord
    {
        private readonly Dictionary<string, int[]> boundaries =
            new Dictionary<string, int[]>(System.StringComparer.Ordinal);

        public PanelBuildRecord(string panelId, int vertexStart, int vertexCount)
        {
            PanelId = panelId;
            VertexStart = vertexStart;
            VertexCount = vertexCount;
        }

        public string PanelId { get; }

        public int VertexStart { get; }

        public int VertexCount { get; }

        public IReadOnlyDictionary<string, int[]> Boundaries => boundaries;

        public void AddBoundary(string boundaryId, int[] vertexIndices)
        {
            boundaries.Add(boundaryId, vertexIndices);
        }
    }
}
