using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class StitchCompilerTests
    {
        private const float PositionTolerance = 1e-5f;

        [Test]
        public void Stitch_EqualSampleWeld_AssignsSharedTopologyIdentity()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 1f);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledBoundary right =
                result.CompiledData.GetPanel("left").GetBoundary("uMax");
            FoldCanvasCompiledBoundary left =
                result.CompiledData.GetPanel("right").GetBoundary("uMin");
            Assert.That(right.VertexIndices.Count, Is.EqualTo(2));
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(8));
            Assert.That(result.CompiledData.TopologyVertexCount, Is.EqualTo(6));
            for (int i = 0; i < right.VertexIndices.Count; i++)
            {
                int rightIndex = right.VertexIndices[i];
                int leftIndex = left.VertexIndices[i];
                Assert.That(rightIndex, Is.Not.EqualTo(leftIndex));
                Assert.That(
                    result.CompiledData.Vertices[rightIndex].TopologyVertexId,
                    Is.EqualTo(
                        result.CompiledData.Vertices[leftIndex]
                            .TopologyVertexId));
                Assert.That(
                    Vector3.Distance(
                        result.CompiledData.Vertices[rightIndex].Position,
                        result.CompiledData.Vertices[leftIndex].Position),
                    Is.LessThanOrEqualTo(PositionTolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Stitch_PreservesDistinctSourceUvsAcrossAttributeSeam()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 1f);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledBoundary right =
                result.CompiledData.GetPanel("left").GetBoundary("uMax");
            FoldCanvasCompiledBoundary left =
                result.CompiledData.GetPanel("right").GetBoundary("uMin");
            for (int i = 0; i < right.VertexIndices.Count; i++)
            {
                FoldCanvasCompiledVertex a =
                    result.CompiledData.Vertices[right.VertexIndices[i]];
                FoldCanvasCompiledVertex b =
                    result.CompiledData.Vertices[left.VertexIndices[i]];
                Assert.That(a.TopologyVertexId, Is.EqualTo(b.TopologyVertexId));
                Assert.That(a.SourceUv.x, Is.EqualTo(0.4f));
                Assert.That(b.SourceUv.x, Is.EqualTo(0.6f));
                Assert.That(a.SourceUv, Is.Not.EqualTo(b.SourceUv));
                Assert.That(a.ProvenanceId, Is.Not.EqualTo(b.ProvenanceId));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Stitch_PositionMismatch_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 2f);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.StitchPositionMismatch,
                "join",
                "stitch");
            Assert.That(result.Diagnostics[0].Values.Count, Is.EqualTo(3));
            Assert.That(
                result.Diagnostics[0].Values[0].Key,
                Is.EqualTo("maximumGap"));
            Assert.That(
                result.Diagnostics[0].Values[1].Key,
                Is.EqualTo("weldEpsilon"));
            Assert.That(
                result.Diagnostics[0].Values[2].Key,
                Is.EqualTo("sampleCount"));
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_SampleCountMismatch_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 1f);
            asset.Panels[1] = PanelDefinition.CreateRectangle(
                "right",
                new Rect(0.6f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                2);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.StitchSampleCountMismatch,
                "join",
                "stitch");
            Assert.That(result.Diagnostics[0].Values.Count, Is.EqualTo(3));
            Assert.That(result.Diagnostics[0].Values[0].Value, Is.EqualTo(2d));
            Assert.That(result.Diagnostics[0].Values[1].Value, Is.EqualTo(3d));
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_ClosedLoopEndpoint_IsCountedOnceForNextSeam()
        {
            FoldCanvasAsset asset = CreateSmallStitchedCupAsset();

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(27));
            Assert.That(result.CompiledData.TopologyVertexCount, Is.EqualTo(17));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(24));

            FoldCanvasCompiledPanel wall =
                result.CompiledData.GetPanel("wall");
            FoldCanvasCompiledPanel bottom =
                result.CompiledData.GetPanel("bottom");
            FoldCanvasCompiledBoundary uMin = wall.GetBoundary("uMin");
            FoldCanvasCompiledBoundary uMax = wall.GetBoundary("uMax");
            for (int i = 0; i < uMin.VertexIndices.Count; i++)
            {
                Assert.That(
                    TopologyId(result, uMin.VertexIndices[i]),
                    Is.EqualTo(TopologyId(result, uMax.VertexIndices[i])));
            }

            FoldCanvasCompiledBoundary wallBottom =
                wall.GetBoundary("vMin");
            FoldCanvasCompiledBoundary diskPerimeter =
                bottom.GetBoundary("perimeter");
            Assert.That(
                TopologyId(result, wallBottom.VertexIndices[0]),
                Is.EqualTo(
                    TopologyId(
                        result,
                        wallBottom.VertexIndices[
                            wallBottom.VertexIndices.Count - 1])));
            for (int i = 0; i < diskPerimeter.VertexIndices.Count; i++)
            {
                Assert.That(
                    TopologyId(result, wallBottom.VertexIndices[i]),
                    Is.EqualTo(
                        TopologyId(result, diskPerimeter.VertexIndices[i])));
            }

            AssertOnlyTopRimIsOpen(result, wall);
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_UnsupportedMode_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 1f);
            asset.Seams[0].Mode = SeamMode.Hinge;

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedStitchSeamMode,
                "join",
                "stitch");
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_MissingSeam_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 1f);
            asset.Seams.Clear();

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.StitchSeamMissing,
                "join",
                "stitch");
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_DuplicateSeamId_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentRectangleAsset(1, 1f);
            asset.Seams.Add(new SeamDefinition
            {
                Id = "join",
                A = new BoundaryReference("left", "uMax"),
                B = new BoundaryReference("right", "uMin"),
                Mode = SeamMode.Weld,
                SampleCount = 2
            });

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.DuplicateSeamId,
                "join",
                "stitch");
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_EmptySeamList_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));
            asset.Operations.Add(new StitchOperationDefinition
            {
                Id = "empty-stitch"
            });

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.EmptyStitchSeamList,
                null,
                "empty-stitch");
            Destroy(result, asset);
        }

        [Test]
        public void Stitch_WeldThatCollapsesTriangles_ReturnsNonManifoldTopology()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.CompileSettings.WeldEpsilon = 1f;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 1f),
                1,
                1));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "collapse",
                A = new BoundaryReference("panel", "uMin"),
                B = new BoundaryReference("panel", "uMax"),
                Mode = SeamMode.Weld,
                SampleCount = 2
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("collapse");
            asset.Operations.Add(stitch);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.NonManifoldTopology));
            Destroy(result, asset);
        }

        [Test]
        public void RepeatedStitchCompile_ProducesIdenticalTopology()
        {
            FoldCanvasAsset asset = CreateSmallStitchedCupAsset();

            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            Assert.That(
                second.CompiledData.TopologyVertexCount,
                Is.EqualTo(first.CompiledData.TopologyVertexCount));
            for (int i = 0; i < first.CompiledData.Vertices.Count; i++)
            {
                Assert.That(
                    second.CompiledData.Vertices[i],
                    Is.EqualTo(first.CompiledData.Vertices[i]));
            }

            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        private static FoldCanvasAsset CreateAdjacentRectangleAsset(
            int vSegments,
            float rightTranslation)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "left",
                new Rect(0f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                vSegments));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "right",
                new Rect(0.6f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                vSegments));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "join",
                A = new BoundaryReference("left", "uMax"),
                B = new BoundaryReference("right", "uMin"),
                Mode = SeamMode.Weld,
                ReverseB = false,
                SampleCount = vSegments + 1
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-right",
                PanelId = "right",
                Translation = new Vector3(rightTranslation, 0f, 0f),
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("join");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static FoldCanvasAsset CreateSmallStitchedCupAsset()
        {
            const float radius = 0.25f;
            const float height = 0.5f;
            const int segments = 8;
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0f, 0.5f, 1f, 0.5f),
                new Vector2(2f * Mathf.PI * radius, height),
                segments,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.25f, 0f, 0.5f, 0.5f),
                Vector2.one * (2f * radius),
                segments,
                1));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-wall",
                A = new BoundaryReference("wall", "uMin"),
                B = new BoundaryReference("wall", "uMax"),
                Mode = SeamMode.Weld,
                SampleCount = 2
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-bottom",
                A = new BoundaryReference("wall", "vMin"),
                B = new BoundaryReference("bottom", "perimeter"),
                Mode = SeamMode.Weld,
                SampleCount = segments
            });
            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll-wall",
                PanelId = "wall",
                Direction = RollDirection.U,
                AngleDegrees = 360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                StartAngleDegrees = 180f
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(0f, -height * 0.5f, 0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch-cup" };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static void AssertOnlyTopRimIsOpen(
            FoldCanvasCompileResult result,
            FoldCanvasCompiledPanel wall)
        {
            Dictionary<ulong, int> edgeIncidence =
                new Dictionary<ulong, int>();
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = TopologyId(result, triangles[i]);
                int b = TopologyId(result, triangles[i + 1]);
                int c = TopologyId(result, triangles[i + 2]);
                AddEdge(edgeIncidence, a, b);
                AddEdge(edgeIncidence, b, c);
                AddEdge(edgeIncidence, c, a);
            }

            HashSet<ulong> expectedTopRim = new HashSet<ulong>();
            IReadOnlyList<int> top = wall.GetBoundary("vMax").VertexIndices;
            for (int i = 0; i < top.Count - 1; i++)
            {
                expectedTopRim.Add(EdgeKey(
                    TopologyId(result, top[i]),
                    TopologyId(result, top[i + 1])));
            }

            int openEdges = 0;
            foreach (KeyValuePair<ulong, int> pair in edgeIncidence)
            {
                Assert.That(pair.Value, Is.LessThanOrEqualTo(2));
                if (pair.Value == 1)
                {
                    openEdges++;
                    Assert.That(expectedTopRim.Contains(pair.Key), Is.True);
                }
            }

            Assert.That(openEdges, Is.EqualTo(expectedTopRim.Count));
            Assert.That(openEdges, Is.EqualTo(8));
        }

        private static void AddEdge(
            Dictionary<ulong, int> incidence,
            int a,
            int b)
        {
            ulong key = EdgeKey(a, b);
            incidence.TryGetValue(key, out int count);
            incidence[key] = count + 1;
        }

        private static ulong EdgeKey(int a, int b)
        {
            int minimum = Mathf.Min(a, b);
            int maximum = Mathf.Max(a, b);
            return ((ulong)(uint)minimum << 32) | (uint)maximum;
        }

        private static int TopologyId(
            FoldCanvasCompileResult result,
            int vertexIndex)
        {
            return result.CompiledData.Vertices[vertexIndex].TopologyVertexId;
        }

        private static void AssertOnlyError(
            FoldCanvasCompileResult result,
            string code,
            string seamId,
            string operationId)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].SeamId, Is.EqualTo(seamId));
            Assert.That(
                result.Diagnostics[0].OperationId,
                Is.EqualTo(operationId));
        }

        private static string JoinDiagnostics(FoldCanvasCompileResult result)
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
