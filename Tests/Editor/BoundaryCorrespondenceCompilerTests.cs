using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class BoundaryCorrespondenceCompilerTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Stitch_ThirtyTwoToSixtyFour_ResamplesDeterministically()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                31,
                63,
                SeamMode.Weld,
                1f,
                0);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            Assert.That(
                first.CompiledData.GetPanel("a")
                    .GetBoundary("uMax").VertexIndices.Count,
                Is.EqualTo(94));
            Assert.That(
                first.CompiledData.GetPanel("b")
                    .GetBoundary("uMin").VertexIndices.Count,
                Is.EqualTo(94));
            AssertCompiledDataEqual(first, second);
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void Stitch_SampleCount_AddsMinimumDensityWithoutDroppingBreakpoints()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                2,
                3,
                SeamMode.Weld,
                1f,
                5);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> boundary =
                result.CompiledData.GetPanel("a")
                    .GetBoundary("uMax").VertexIndices;
            Assert.That(boundary.Count, Is.EqualTo(7));
            Assert.That(
                ContainsBoundarySourceY(result, boundary, -1f / 6f),
                Is.True);
            Assert.That(
                ContainsBoundarySourceY(result, boundary, 0f),
                Is.True);
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_ResampledVertex_IsPartOfAdjacentSurface()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                1,
                2,
                SeamMode.Weld,
                1f,
                0);
            const int authoredVertexCount = 10;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            bool insertedVertexIsReferenced = false;
            for (int i = 0;
                i < result.CompiledData.TriangleIndices.Count;
                i++)
            {
                if (result.CompiledData.TriangleIndices[i] >=
                    authoredVertexCount)
                {
                    insertedVertexIsReferenced = true;
                    break;
                }
            }

            Assert.That(insertedVertexIsReferenced, Is.True);
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_Resampling_PreservesInterpolatedSourceUv()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                1,
                2,
                SeamMode.Weld,
                1f,
                0);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> aBoundary =
                result.CompiledData.GetPanel("a")
                    .GetBoundary("uMax").VertexIndices;
            FoldCanvasCompiledVertex midpoint =
                result.CompiledData.Vertices[aBoundary[1]];
            Assert.That(midpoint.SourceUv.x, Is.EqualTo(0.45f).Within(Tolerance));
            Assert.That(midpoint.SourceUv.y, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(midpoint.SourcePosition.y, Is.EqualTo(0f).Within(Tolerance));
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_Resampling_PreservesTriangleWinding()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                2,
                3,
                SeamMode.Weld,
                1f,
                0);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertAllTrianglesFacePositiveZ(result);
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_RepeatedCompile_ProducesIdenticalTopology()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                3,
                5,
                SeamMode.Weld,
                1f,
                9);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            AssertCompiledDataEqual(first, second);
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void Stitch_ZeroLengthBoundary_ReturnsSingleStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                1,
                1,
                SeamMode.Weld,
                1f,
                0);
            asset.Operations.Insert(1, new RigidTransformOperationDefinition
            {
                Id = "collapse-a",
                PanelId = "a",
                Scale = new Vector3(1f, 0f, 1f)
            });

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.ZeroLengthStitchBoundary));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("a"));
            Destroy(result, asset);
        }

        [Test]
        public void Weld_ThirtyTwoToSixtyFour_ProducesSharedTopology()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                31,
                63,
                SeamMode.Weld,
                1f,
                0);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> a = result.CompiledData.GetPanel("a")
                .GetBoundary("uMax").VertexIndices;
            IReadOnlyList<int> b = result.CompiledData.GetPanel("b")
                .GetBoundary("uMin").VertexIndices;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(
                    result.CompiledData.Vertices[a[i]].TopologyVertexId,
                    Is.EqualTo(
                        result.CompiledData.Vertices[b[i]]
                            .TopologyVertexId));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Weld_ReverseB_ChangesCorrespondencePredictably()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                2,
                2,
                SeamMode.Weld,
                1f,
                0);
            asset.Seams[0].ReverseB = true;
            asset.Seams[0].B =
                new BoundaryReference("b", "uMax");
            asset.Operations[0] = new RigidTransformOperationDefinition
            {
                Id = "place-b",
                PanelId = "b",
                Translation = Vector3.right,
                RotationEuler = new Vector3(0f, 0f, 180f),
                Scale = Vector3.one
            };

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> a = result.CompiledData.GetPanel("a")
                .GetBoundary("uMax").VertexIndices;
            IReadOnlyList<int> b = result.CompiledData.GetPanel("b")
                .GetBoundary("uMax").VertexIndices;
            Assert.That(
                result.CompiledData.Vertices[a[0]].TopologyVertexId,
                Is.EqualTo(
                    result.CompiledData.Vertices[b[b.Count - 1]]
                        .TopologyVertexId));
            Destroy(result, asset);
        }

        [Test]
        public void Weld_LeavesNoCrackAboveEpsilon()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                3,
                5,
                SeamMode.Weld,
                1f,
                0);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> a = result.CompiledData.GetPanel("a")
                .GetBoundary("uMax").VertexIndices;
            IReadOnlyList<int> b = result.CompiledData.GetPanel("b")
                .GetBoundary("uMin").VertexIndices;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(
                    Vector3.Distance(
                        result.CompiledData.Vertices[a[i]].Position,
                        result.CompiledData.Vertices[b[i]].Position),
                    Is.LessThanOrEqualTo(
                        FoldCanvasCompileSettings.DefaultWeldEpsilon));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Weld_PreservesDistinctUvRenderCopies()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                2,
                3,
                SeamMode.Weld,
                1f,
                0);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> a = result.CompiledData.GetPanel("a")
                .GetBoundary("uMax").VertexIndices;
            IReadOnlyList<int> b = result.CompiledData.GetPanel("b")
                .GetBoundary("uMin").VertexIndices;
            Assert.That(
                result.CompiledData.Vertices[a[0]].SourceUv,
                Is.Not.EqualTo(
                    result.CompiledData.Vertices[b[0]].SourceUv));
            Assert.That(a[0], Is.Not.EqualTo(b[0]));
            Destroy(result, asset);
        }

        [Test]
        public void Bridge_ThirtyTwoToSixtyFour_ProducesDeterministicStrip()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                31,
                63,
                SeamMode.Bridge,
                2f,
                0);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            Assert.That(
                first.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(466));
            AssertCompiledDataEqual(first, second);
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void Bridge_HasConsistentWindingAndNoZeroAreaTriangles()
        {
            FoldCanvasAsset asset = CreateUnequalAsset(
                2,
                3,
                SeamMode.Bridge,
                2f,
                7);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertAllTrianglesFacePositiveZ(result);
            Destroy(result, asset);
        }

        private static FoldCanvasAsset CreateUnequalAsset(
            int segmentsA,
            int segmentsB,
            SeamMode mode,
            float translationB,
            int sampleCount)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "a",
                new Rect(0.05f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                segmentsA));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "b",
                new Rect(0.55f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                segmentsB));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "join",
                A = new BoundaryReference("a", "uMax"),
                B = new BoundaryReference("b", "uMin"),
                Mode = mode,
                SampleCount = sampleCount
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-b",
                PanelId = "b",
                Translation = Vector3.right * translationB,
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("join");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static bool ContainsBoundarySourceY(
            FoldCanvasCompileResult result,
            IReadOnlyList<int> boundary,
            float expected)
        {
            for (int i = 0; i < boundary.Count; i++)
            {
                if (Mathf.Abs(
                    result.CompiledData.Vertices[
                        boundary[i]].SourcePosition.y - expected) <=
                    Tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertAllTrianglesFacePositiveZ(
            FoldCanvasCompileResult result)
        {
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a =
                    result.CompiledData.Vertices[
                        triangles[i]].Position;
                Vector3 b =
                    result.CompiledData.Vertices[
                        triangles[i + 1]].Position;
                Vector3 c =
                    result.CompiledData.Vertices[
                        triangles[i + 2]].Position;
                Assert.That(
                    Vector3.Cross(b - a, c - a).z,
                    Is.GreaterThan(0f),
                    $"triangle {i / 3}");
            }
        }

        private static void AssertCompiledDataEqual(
            FoldCanvasCompileResult first,
            FoldCanvasCompileResult second)
        {
            Assert.That(
                second.CompiledData.Vertices.Count,
                Is.EqualTo(first.CompiledData.Vertices.Count));
            for (int i = 0;
                i < first.CompiledData.Vertices.Count;
                i++)
            {
                Assert.That(
                    second.CompiledData.Vertices[i],
                    Is.EqualTo(first.CompiledData.Vertices[i]));
            }

            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
        }

        private static string Diagnostics(
            FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset,
            bool destroyAsset = true)
        {
            if (result != null && result.Mesh != null)
            {
                Object.DestroyImmediate(result.Mesh);
            }

            if (destroyAsset && asset != null)
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
