using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class PlanarPanelCompilerTests
    {
        [Test]
        public void Rectangle_ProducesDeterministicCountsAndCanvasUvs()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "RectangleTest";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0.25f, 0.1f, 0.5f, 0.6f),
                new Vector2(2f, 1f),
                2,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Mesh.vertexCount, Is.EqualTo(6));
            Assert.That(result.Mesh.triangles.Length, Is.EqualTo(12));

            Vector2[] uvs = result.Mesh.uv;
            Assert.That(uvs[0].x, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(uvs[0].y, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(uvs[2].x, Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(uvs[2].y, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(uvs[5].x, Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(uvs[5].y, Is.EqualTo(0.7f).Within(1e-6f));
            AssertFrontFacing(result.Mesh);

            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Disk_ProducesExpectedCounts()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "DiskTest";
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "disk",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                8,
                2));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Mesh.vertexCount, Is.EqualTo(17));
            Assert.That(result.Mesh.triangles.Length / 3, Is.EqualTo(24));
            AssertFrontFacing(result.Mesh);

            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void RigidTransform_MovesOnlyTargetPanel()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "TransformTest";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "left",
                new Rect(0f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "right",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "move-right",
                PanelId = "right",
                Translation = new Vector3(3f, 0f, 0f),
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Vector3[] vertices = result.Mesh.vertices;
            Vector2[] uvs = result.Mesh.uv;
            for (int i = 0; i < 4; i++)
            {
                float expectedX = i % 2 == 0 ? -0.5f : 0.5f;
                float expectedY = i < 2 ? -0.5f : 0.5f;
                Assert.That(vertices[i].x, Is.EqualTo(expectedX).Within(1e-6f));
                Assert.That(vertices[i].y, Is.EqualTo(expectedY).Within(1e-6f));
                Assert.That(vertices[i].z, Is.EqualTo(0f).Within(1e-6f));
            }

            for (int i = 4; i < 8; i++)
            {
                int localIndex = i - 4;
                float expectedX = localIndex % 2 == 0 ? 2.5f : 3.5f;
                float expectedY = localIndex < 2 ? -0.5f : 0.5f;
                Assert.That(vertices[i].x, Is.EqualTo(expectedX).Within(1e-6f));
                Assert.That(vertices[i].y, Is.EqualTo(expectedY).Within(1e-6f));
                Assert.That(vertices[i].z, Is.EqualTo(0f).Within(1e-6f));
            }

            Assert.That(uvs[0], Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(uvs[3], Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(uvs[4], Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(uvs[7], Is.EqualTo(new Vector2(1f, 1f)));

            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void IdenticalSource_ProducesIdenticalOrderedGeometry()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "DeterminismTest";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "rectangle",
                new Rect(0f, 0f, 0.5f, 1f),
                new Vector2(2f, 1f),
                3,
                2));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "disk",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                12,
                3));
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "move-disk",
                PanelId = "disk",
                Translation = new Vector3(2f, 0.25f, -0.5f),
                RotationEuler = new Vector3(20f, 30f, 40f),
                Scale = new Vector3(1.5f, 0.75f, 1f)
            });

            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            CollectionAssert.AreEqual(first.Mesh.vertices, second.Mesh.vertices);
            CollectionAssert.AreEqual(first.Mesh.triangles, second.Mesh.triangles);
            CollectionAssert.AreEqual(first.Mesh.uv, second.Mesh.uv);

            Object.DestroyImmediate(first.Mesh);
            Object.DestroyImmediate(second.Mesh);
            Object.DestroyImmediate(asset);
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
