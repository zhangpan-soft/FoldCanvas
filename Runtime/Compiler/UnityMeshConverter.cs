using UnityEngine;
using UnityEngine.Rendering;

namespace FoldCanvas
{
    internal static class UnityMeshConverter
    {
        public static Mesh Create(
            FoldCanvasCompiledData data,
            string meshName,
            bool recalculateNormals)
        {
            Mesh mesh = new Mesh
            {
                name = meshName
            };

            if (data.Vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            Vector3[] positions = new Vector3[data.Vertices.Count];
            Vector2[] uvs = new Vector2[data.Vertices.Count];
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                positions[i] = vertex.Position;
                uvs[i] = vertex.SourceUv;
            }

            int[] triangles = new int[data.TriangleIndices.Count];
            for (int i = 0; i < data.TriangleIndices.Count; i++)
            {
                triangles[i] = data.TriangleIndices[i];
            }

            mesh.vertices = positions;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0, true);

            if (recalculateNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
