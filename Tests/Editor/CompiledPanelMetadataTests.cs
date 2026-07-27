using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FoldCanvas.Tests
{
    public sealed class CompiledPanelMetadataTests
    {
        [Test]
        public void Rectangle_PreservesSourceCoordinatesOwnershipAndBoundaries()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "RectangleMetadata";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "rectangle",
                new Rect(0.25f, 0.1f, 0.5f, 0.6f),
                new Vector2(2f, 1f),
                2,
                2));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData, Is.Not.Null);
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(9));
            Assert.That(result.CompiledData.TriangleIndices.Count, Is.EqualTo(24));

            FoldCanvasCompiledPanel panel =
                result.CompiledData.GetPanel("rectangle");
            Assert.That(panel.PanelIndex, Is.EqualTo(0));
            Assert.That(panel.Shape, Is.EqualTo(PanelShape.Rectangle));
            Assert.That(panel.CanvasRect, Is.EqualTo(new Rect(0.25f, 0.1f, 0.5f, 0.6f)));
            Assert.That(panel.PhysicalSize, Is.EqualTo(new Vector2(2f, 1f)));
            Assert.That(panel.VertexStart, Is.EqualTo(0));
            Assert.That(panel.VertexCount, Is.EqualTo(9));

            CollectionAssert.AreEqual(
                new[] { "uMin", "uMax", "vMin", "vMax" },
                BoundaryIds(panel));
            CollectionAssert.AreEqual(
                new[] { 0, 3, 6 },
                panel.GetBoundary("uMin").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 2, 5, 8 },
                panel.GetBoundary("uMax").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2 },
                panel.GetBoundary("vMin").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 6, 7, 8 },
                panel.GetBoundary("vMax").VertexIndices);

            AssertVertex(
                result.CompiledData.Vertices[0],
                new Vector2(-1f, -0.5f),
                new Vector2(0.25f, 0.1f),
                0,
                0);
            AssertVertex(
                result.CompiledData.Vertices[2],
                new Vector2(1f, -0.5f),
                new Vector2(0.75f, 0.1f),
                0,
                2);
            AssertVertex(
                result.CompiledData.Vertices[6],
                new Vector2(-1f, 0.5f),
                new Vector2(0.25f, 0.7f),
                0,
                6);
            AssertVertex(
                result.CompiledData.Vertices[8],
                new Vector2(1f, 0.5f),
                new Vector2(0.75f, 0.7f),
                0,
                8);
            AssertCompiledDataMatchesMesh(result);
            AssertFrontFacing(result.Mesh);

            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Ellipse_MapsCenterRadiiAndCounterClockwisePerimeter()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "EllipseMetadata";
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "ellipse",
                new Rect(0.2f, 0.1f, 0.6f, 0.8f),
                new Vector2(4f, 2f),
                4,
                2));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledData data = result.CompiledData;
            FoldCanvasCompiledPanel panel = data.GetPanel("ellipse");
            Assert.That(data.Vertices.Count, Is.EqualTo(9));
            Assert.That(data.TriangleIndices.Count, Is.EqualTo(36));
            Assert.That(panel.Boundaries.Count, Is.EqualTo(1));
            Assert.That(panel.Boundaries[0].Id, Is.EqualTo("perimeter"));
            CollectionAssert.AreEqual(
                new[] { 5, 6, 7, 8 },
                panel.GetBoundary("perimeter").VertexIndices);

            AssertVertex(
                data.Vertices[0],
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                0,
                0);
            AssertSourceAndUv(
                data.Vertices[5],
                new Vector2(2f, 0f),
                new Vector2(0.8f, 0.5f));
            AssertSourceAndUv(
                data.Vertices[6],
                new Vector2(0f, 1f),
                new Vector2(0.5f, 0.9f));
            AssertSourceAndUv(
                data.Vertices[7],
                new Vector2(-2f, 0f),
                new Vector2(0.2f, 0.5f));
            AssertSourceAndUv(
                data.Vertices[8],
                new Vector2(0f, -1f),
                new Vector2(0.5f, 0.1f));

            IReadOnlyList<int> perimeter =
                panel.GetBoundary("perimeter").VertexIndices;
            float signedDoubleArea = 0f;
            for (int i = 0; i < perimeter.Count; i++)
            {
                Vector2 current = data.Vertices[perimeter[i]].SourcePosition;
                Vector2 next =
                    data.Vertices[perimeter[(i + 1) % perimeter.Count]].SourcePosition;
                signedDoubleArea += current.x * next.y - current.y * next.x;
            }

            Assert.That(signedDoubleArea, Is.GreaterThan(0f));
            AssertCompiledDataMatchesMesh(result);
            AssertFrontFacing(result.Mesh);

            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void RigidTransform_ChangesOnlyCurrentPositions()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "ProvenanceTransform";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0.1f, 0.2f, 0.7f, 0.6f),
                new Vector2(2f, 1f),
                2,
                1));

            FoldCanvasCompileResult baseline = FoldCanvasCompiler.Compile(asset);
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "move",
                PanelId = "panel",
                Translation = new Vector3(4f, 5f, 6f),
                RotationEuler = new Vector3(15f, 25f, 35f),
                Scale = new Vector3(2f, 0.5f, 1f)
            });
            FoldCanvasCompileResult transformed = FoldCanvasCompiler.Compile(asset);

            Assert.That(baseline.Success, Is.True, JoinDiagnostics(baseline));
            Assert.That(transformed.Success, Is.True, JoinDiagnostics(transformed));
            Assert.That(
                transformed.CompiledData.Vertices.Count,
                Is.EqualTo(baseline.CompiledData.Vertices.Count));

            bool anyPositionChanged = false;
            for (int i = 0; i < baseline.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex before = baseline.CompiledData.Vertices[i];
                FoldCanvasCompiledVertex after = transformed.CompiledData.Vertices[i];
                anyPositionChanged |= before.Position != after.Position;
                Assert.That(after.SourcePosition, Is.EqualTo(before.SourcePosition));
                Assert.That(after.SourceUv, Is.EqualTo(before.SourceUv));
                Assert.That(after.PanelIndex, Is.EqualTo(before.PanelIndex));
                Assert.That(after.ProvenanceId, Is.EqualTo(before.ProvenanceId));
            }

            Assert.That(anyPositionChanged, Is.True);
            CollectionAssert.AreEqual(
                baseline.CompiledData.TriangleIndices,
                transformed.CompiledData.TriangleIndices);
            AssertPanelMetadataEqual(
                baseline.CompiledData.Panels[0],
                transformed.CompiledData.Panels[0]);

            Object.DestroyImmediate(baseline.Mesh);
            Object.DestroyImmediate(transformed.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void CompiledMetadata_DoesNotExposeMutableCollections()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledData data = result.CompiledData;
            FoldCanvasCompiledPanel panel = data.GetPanel("panel");
            FoldCanvasCompiledBoundary boundary = panel.GetBoundary("uMin");

            Assert.Throws<NotSupportedException>(() =>
                ((IList<FoldCanvasCompiledVertex>)data.Vertices)[0] =
                    data.Vertices[0]);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)data.TriangleIndices)[0] =
                    data.TriangleIndices[0]);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FoldCanvasCompiledPanel>)data.Panels).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FoldCanvasCompiledBoundary>)panel.Boundaries).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)boundary.VertexIndices)[0] =
                    boundary.VertexIndices[0]);

            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void RepeatedCompile_PreservesAllCompiledOrdering()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "CompiledDeterminism";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "first",
                new Rect(0f, 0f, 0.5f, 1f),
                new Vector2(2f, 1f),
                3,
                2));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "second",
                new Rect(0.5f, 0f, 0.5f, 1f),
                new Vector2(1f, 1.5f),
                12,
                3));

            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            CollectionAssert.AreEqual(
                first.CompiledData.Vertices,
                second.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            CollectionAssert.AreEqual(
                new[] { "first", "second" },
                PanelIds(first.CompiledData));

            Assert.That(
                second.CompiledData.Panels.Count,
                Is.EqualTo(first.CompiledData.Panels.Count));
            for (int i = 0; i < first.CompiledData.Panels.Count; i++)
            {
                AssertPanelMetadataEqual(
                    first.CompiledData.Panels[i],
                    second.CompiledData.Panels[i]);
            }

            CollectionAssert.AreEqual(first.Mesh.vertices, second.Mesh.vertices);
            CollectionAssert.AreEqual(first.Mesh.uv, second.Mesh.uv);
            CollectionAssert.AreEqual(first.Mesh.triangles, second.Mesh.triangles);

            Object.DestroyImmediate(first.Mesh);
            Object.DestroyImmediate(second.Mesh);
            Object.DestroyImmediate(asset);
        }

        private static void AssertVertex(
            FoldCanvasCompiledVertex vertex,
            Vector2 expectedSourcePosition,
            Vector2 expectedUv,
            int expectedPanelIndex,
            int expectedProvenanceId)
        {
            AssertSourceAndUv(vertex, expectedSourcePosition, expectedUv);
            Assert.That(vertex.PanelIndex, Is.EqualTo(expectedPanelIndex));
            Assert.That(vertex.ProvenanceId, Is.EqualTo(expectedProvenanceId));
        }

        private static void AssertSourceAndUv(
            FoldCanvasCompiledVertex vertex,
            Vector2 expectedSourcePosition,
            Vector2 expectedUv)
        {
            Assert.That(
                vertex.SourcePosition.x,
                Is.EqualTo(expectedSourcePosition.x).Within(1e-6f));
            Assert.That(
                vertex.SourcePosition.y,
                Is.EqualTo(expectedSourcePosition.y).Within(1e-6f));
            Assert.That(
                vertex.Position.x,
                Is.EqualTo(expectedSourcePosition.x).Within(1e-6f));
            Assert.That(
                vertex.Position.y,
                Is.EqualTo(expectedSourcePosition.y).Within(1e-6f));
            Assert.That(vertex.Position.z, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(vertex.SourceUv.x, Is.EqualTo(expectedUv.x).Within(1e-6f));
            Assert.That(vertex.SourceUv.y, Is.EqualTo(expectedUv.y).Within(1e-6f));
        }

        private static void AssertCompiledDataMatchesMesh(
            FoldCanvasCompileResult result)
        {
            Vector3[] meshVertices = result.Mesh.vertices;
            Vector2[] meshUvs = result.Mesh.uv;
            int[] meshTriangles = result.Mesh.triangles;

            Assert.That(
                result.CompiledData.Vertices.Count,
                Is.EqualTo(meshVertices.Length));
            for (int i = 0; i < meshVertices.Length; i++)
            {
                Assert.That(
                    result.CompiledData.Vertices[i].Position,
                    Is.EqualTo(meshVertices[i]));
                Assert.That(
                    result.CompiledData.Vertices[i].SourceUv,
                    Is.EqualTo(meshUvs[i]));
            }

            CollectionAssert.AreEqual(
                result.CompiledData.TriangleIndices,
                meshTriangles);
        }

        private static void AssertPanelMetadataEqual(
            FoldCanvasCompiledPanel first,
            FoldCanvasCompiledPanel second)
        {
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(second.PanelIndex, Is.EqualTo(first.PanelIndex));
            Assert.That(second.Shape, Is.EqualTo(first.Shape));
            Assert.That(second.CanvasRect, Is.EqualTo(first.CanvasRect));
            Assert.That(second.PhysicalSize, Is.EqualTo(first.PhysicalSize));
            Assert.That(second.VertexStart, Is.EqualTo(first.VertexStart));
            Assert.That(second.VertexCount, Is.EqualTo(first.VertexCount));
            Assert.That(second.Boundaries.Count, Is.EqualTo(first.Boundaries.Count));
            for (int i = 0; i < first.Boundaries.Count; i++)
            {
                Assert.That(
                    second.Boundaries[i].Id,
                    Is.EqualTo(first.Boundaries[i].Id));
                CollectionAssert.AreEqual(
                    first.Boundaries[i].VertexIndices,
                    second.Boundaries[i].VertexIndices);
            }
        }

        private static string[] BoundaryIds(FoldCanvasCompiledPanel panel)
        {
            string[] ids = new string[panel.Boundaries.Count];
            for (int i = 0; i < panel.Boundaries.Count; i++)
            {
                ids[i] = panel.Boundaries[i].Id;
            }

            return ids;
        }

        private static string[] PanelIds(FoldCanvasCompiledData data)
        {
            string[] ids = new string[data.Panels.Count];
            for (int i = 0; i < data.Panels.Count; i++)
            {
                ids[i] = data.Panels[i].Id;
            }

            return ids;
        }

        private static void AssertFrontFacing(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                Assert.That(
                    Vector3.Cross(b - a, c - a).z,
                    Is.GreaterThan(0f),
                    $"Triangle {i / 3} must face +Z.");
            }
        }

        private static string JoinDiagnostics(FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }
    }
}
