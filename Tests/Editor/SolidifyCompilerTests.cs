using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class SolidifyCompilerTests
    {
        private const float Tolerance = 1e-5f;
        private const float CupRadius = 0.25f;
        private const float CupHeight = 0.5f;
        private const int CupSegments = 8;

        [Test]
        public void Solidify_Inward_PreservesOuterAndOffsetsInner()
        {
            AssertPlanarSolidifyBounds(
                SolidifyDirection.Inward,
                -0.1f,
                0f);
        }

        [Test]
        public void Solidify_Outward_PreservesInnerAndOffsetsOuter()
        {
            AssertPlanarSolidifyBounds(
                SolidifyDirection.Outward,
                0f,
                0.1f);
        }

        [Test]
        public void Solidify_Centered_SplitsRequestedThickness()
        {
            AssertPlanarSolidifyBounds(
                SolidifyDirection.Centered,
                -0.05f,
                0.05f);
        }

        [Test]
        public void Solidify_InnerTrianglesReverseOuterWinding()
        {
            FoldCanvasAsset asset = CreatePlanarSolidifyAsset(
                SolidifyDirection.Inward,
                0.1f);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            Vector3 outerNormal = TriangleNormal(result, 0);
            Vector3 innerNormal = TriangleNormal(result, 2);
            Assert.That(outerNormal.z, Is.GreaterThan(0f));
            Assert.That(innerNormal.z, Is.LessThan(0f));
            Assert.That(triangles.Count / 3, Is.EqualTo(12));
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_SharedTopologyCopies_DoNotCrack()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertEveryTopologyGroupHasOnePosition(result);
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_WallBottomCorner_MeetsAtSharedMiter()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);
            int sourceVertexCount = CupSourceVertexCount();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            FoldCanvasCompiledBoundary wallBottom =
                result.CompiledData.GetPanel("wall")
                    .GetBoundary("vMin");
            FoldCanvasCompiledBoundary bottomPerimeter =
                result.CompiledData.GetPanel("bottom")
                    .GetBoundary("perimeter");
            for (int i = 0; i < bottomPerimeter.VertexIndices.Count; i++)
            {
                int innerWall =
                    sourceVertexCount + wallBottom.VertexIndices[i];
                int innerBottom =
                    sourceVertexCount + bottomPerimeter.VertexIndices[i];
                Assert.That(
                    result.CompiledData.Vertices[innerWall]
                        .TopologyVertexId,
                    Is.EqualTo(
                        result.CompiledData.Vertices[innerBottom]
                            .TopologyVertexId));
                Assert.That(
                    Vector3.Distance(
                        result.CompiledData.Vertices[innerWall].Position,
                        result.CompiledData.Vertices[innerBottom].Position),
                    Is.LessThanOrEqualTo(Tolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Solidify_PartialStitchedComponent_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateCupAsset(false);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-wall-only",
                    Thickness = 0.02f,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("wall");
            asset.Operations.Add(solidify);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertSingleError(
                result,
                FoldCanvasDiagnosticCodes
                    .IncompleteSolidifyTopologySelection,
                "solidify-wall-only");
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_InvalidThickness_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePlanarSolidifyAsset(
                SolidifyDirection.Inward,
                0f);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertSingleError(
                result,
                FoldCanvasDiagnosticCodes.InvalidSolidifyThickness,
                "solidify");
            Assert.That(result.Diagnostics[0].Values.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Values[0].Key,
                Is.EqualTo("thickness"));
            Destroy(result, asset);
        }

        [Test]
        public void StitchThenSolidify_UsesFinalTopology()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(CountOpenTopologyEdges(result), Is.EqualTo(0));
            Assert.That(CountNonManifoldTopologyEdges(result), Is.EqualTo(0));
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_Cup_GeneratesOnlyTopRim()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(64));
            Assert.That(CountOpenTopologyEdges(result), Is.EqualTo(0));
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_WeldedBottomSeam_HasNoInternalWall()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                CountRimTriangles(result, CupSourceVertexCount() * 2),
                Is.EqualTo(CupSegments * 2));
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_WeldedWallClosure_HasNoInternalWall()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                CountRimTriangles(result, CupSourceVertexCount() * 2),
                Is.EqualTo(CupSegments * 2));
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_RimWindingFacesOutward()
        {
            FoldCanvasAsset asset = CreatePlanarSolidifyAsset(
                SolidifyDirection.Inward,
                0.1f);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            int rimTriangleStart = 4;
            Vector3 firstRimNormal =
                TriangleNormal(result, rimTriangleStart);
            Assert.That(firstRimNormal.y, Is.LessThan(0f));
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_RimUvPolicy_IsDeterministic()
        {
            FoldCanvasAsset asset = CreatePlanarSolidifyAsset(
                SolidifyDirection.Inward,
                0.1f);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            Assert.That(
                second.CompiledData.Vertices.Count,
                Is.EqualTo(first.CompiledData.Vertices.Count));
            for (int i = 0; i < first.CompiledData.Vertices.Count; i++)
            {
                Assert.That(
                    second.CompiledData.Vertices[i].SourceUv,
                    Is.EqualTo(
                        first.CompiledData.Vertices[i].SourceUv));
                Assert.That(
                    second.CompiledData.Vertices[i].ProvenanceId,
                    Is.EqualTo(
                        first.CompiledData.Vertices[i].ProvenanceId));
            }

            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void Solidify_HighResolutionDisk_UsesFiniteUnitNormals()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "disk",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one * 0.1f,
                64,
                8));
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-disk",
                    Thickness = 0.004f,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("disk");
            asset.Operations.Add(solidify);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(CountOpenTopologyEdges(result), Is.EqualTo(0));
            for (int i = 0;
                i < result.CompiledData.Vertices.Count;
                i++)
            {
                Vector3 position =
                    result.CompiledData.Vertices[i].Position;
                Assert.That(float.IsNaN(position.x), Is.False);
                Assert.That(float.IsNaN(position.y), Is.False);
                Assert.That(float.IsNaN(position.z), Is.False);
            }

            Destroy(result, asset);
        }

        [Test]
        public void BottomPanel_AllVerticesAreCoplanar()
        {
            FoldCanvasAsset asset = CreateCupAsset(false);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            int bottomPanelIndex =
                result.CompiledData.GetPanel("bottom").PanelIndex;
            int inspected = 0;
            for (int i = 0;
                i < result.CompiledData.Vertices.Count;
                i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[i];
                if (vertex.PanelIndex != bottomPanelIndex)
                {
                    continue;
                }

                inspected++;
                Assert.That(
                    vertex.Position.y,
                    Is.EqualTo(-CupHeight * 0.5f)
                        .Within(Tolerance));
            }

            Assert.That(inspected, Is.GreaterThan(0));
            Destroy(result, asset);
        }

        [Test]
        public void WallBottomWeld_HasZeroBoundaryPositionGap()
        {
            FoldCanvasAsset asset = CreateCupAsset(false);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            FoldCanvasCompiledBoundary wall =
                result.CompiledData.GetPanel("wall")
                    .GetBoundary("vMin");
            FoldCanvasCompiledBoundary bottom =
                result.CompiledData.GetPanel("bottom")
                    .GetBoundary("perimeter");
            for (int i = 0; i < bottom.VertexIndices.Count; i++)
            {
                Assert.That(
                    Vector3.Distance(
                        result.CompiledData.Vertices[
                            wall.VertexIndices[i]].Position,
                        result.CompiledData.Vertices[
                            bottom.VertexIndices[i]].Position),
                    Is.LessThanOrEqualTo(Tolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void WallBottomWeld_HasNoOpenTopologyEdge()
        {
            FoldCanvasAsset asset = CreateCupAsset(false);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Dictionary<ulong, int> incidence =
                BuildTopologyEdgeIncidence(result);
            IReadOnlyList<int> wall =
                result.CompiledData.GetPanel("wall")
                    .GetBoundary("vMin").VertexIndices;
            for (int i = 0; i < CupSegments; i++)
            {
                ulong edge = EdgeKey(
                    TopologyId(result, wall[i]),
                    TopologyId(result, wall[i + 1]));
                Assert.That(incidence[edge], Is.EqualTo(2));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Solidify_WallBottomInnerCornerRemainsConnected()
        {
            FoldCanvasAsset asset = CreateCupAsset(true);
            int sourceVertexCount = CupSourceVertexCount();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> wall =
                result.CompiledData.GetPanel("wall")
                    .GetBoundary("vMin").VertexIndices;
            IReadOnlyList<int> bottom =
                result.CompiledData.GetPanel("bottom")
                    .GetBoundary("perimeter").VertexIndices;
            for (int i = 0; i < bottom.Count; i++)
            {
                FoldCanvasCompiledVertex innerWall =
                    result.CompiledData.Vertices[
                        sourceVertexCount + wall[i]];
                FoldCanvasCompiledVertex innerBottom =
                    result.CompiledData.Vertices[
                        sourceVertexCount + bottom[i]];
                Assert.That(
                    innerWall.TopologyVertexId,
                    Is.EqualTo(innerBottom.TopologyVertexId));
                Assert.That(
                    Vector3.Distance(
                        innerWall.Position,
                        innerBottom.Position),
                    Is.LessThanOrEqualTo(Tolerance));
            }

            Assert.That(CountOpenTopologyEdges(result), Is.EqualTo(0));
            Destroy(result, asset);
        }

        private static void AssertPlanarSolidifyBounds(
            SolidifyDirection direction,
            float expectedMinimumZ,
            float expectedMaximumZ)
        {
            FoldCanvasAsset asset = CreatePlanarSolidifyAsset(
                direction,
                0.1f);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            float minimumZ = float.PositiveInfinity;
            float maximumZ = float.NegativeInfinity;
            for (int i = 0;
                i < result.CompiledData.Vertices.Count;
                i++)
            {
                float z = result.CompiledData.Vertices[i].Position.z;
                minimumZ = Mathf.Min(minimumZ, z);
                maximumZ = Mathf.Max(maximumZ, z);
            }

            Assert.That(
                minimumZ,
                Is.EqualTo(expectedMinimumZ).Within(Tolerance));
            Assert.That(
                maximumZ,
                Is.EqualTo(expectedMaximumZ).Within(Tolerance));
            Assert.That(
                maximumZ - minimumZ,
                Is.EqualTo(0.1f).Within(Tolerance));
            Destroy(result, asset);
        }

        private static FoldCanvasAsset CreatePlanarSolidifyAsset(
            SolidifyDirection direction,
            float thickness)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify",
                    Thickness = thickness,
                    Direction = direction
                };
            solidify.PanelIds.Add("panel");
            asset.Operations.Add(solidify);
            return asset;
        }

        private static FoldCanvasAsset CreateCupAsset(bool solidify)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0f, 0.5f, 1f, 0.5f),
                new Vector2(
                    2f * Mathf.PI * CupRadius,
                    CupHeight),
                CupSegments,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.25f, 0f, 0.5f, 0.5f),
                Vector2.one * (2f * CupRadius),
                CupSegments,
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
                SampleCount = CupSegments
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
                Translation = new Vector3(
                    0f,
                    -CupHeight * 0.5f,
                    0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch-cup" };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            asset.Operations.Add(stitch);

            if (solidify)
            {
                SolidifyOperationDefinition operation =
                    new SolidifyOperationDefinition
                    {
                        Id = "solidify-cup",
                        Thickness = 0.02f,
                        Direction = SolidifyDirection.Inward
                    };
                operation.PanelIds.Add("wall");
                operation.PanelIds.Add("bottom");
                asset.Operations.Add(operation);
            }

            return asset;
        }

        private static int CupSourceVertexCount()
        {
            return
                (CupSegments + 1) * 2 +
                1 +
                CupSegments;
        }

        private static Vector3 TriangleNormal(
            FoldCanvasCompileResult result,
            int triangleIndex)
        {
            int offset = triangleIndex * 3;
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            Vector3 a = result.CompiledData.Vertices[
                triangles[offset]].Position;
            Vector3 b = result.CompiledData.Vertices[
                triangles[offset + 1]].Position;
            Vector3 c = result.CompiledData.Vertices[
                triangles[offset + 2]].Position;
            return Vector3.Cross(b - a, c - a).normalized;
        }

        private static void AssertEveryTopologyGroupHasOnePosition(
            FoldCanvasCompileResult result)
        {
            Dictionary<int, Vector3> positions =
                new Dictionary<int, Vector3>();
            for (int i = 0;
                i < result.CompiledData.Vertices.Count;
                i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[i];
                if (positions.TryGetValue(
                    vertex.TopologyVertexId,
                    out Vector3 position))
                {
                    Assert.That(
                        Vector3.Distance(position, vertex.Position),
                        Is.LessThanOrEqualTo(Tolerance));
                }
                else
                {
                    positions.Add(
                        vertex.TopologyVertexId,
                        vertex.Position);
                }
            }
        }

        private static int CountRimTriangles(
            FoldCanvasCompileResult result,
            int shellVertexCount)
        {
            int count = 0;
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                if (triangles[i] >= shellVertexCount ||
                    triangles[i + 1] >= shellVertexCount ||
                    triangles[i + 2] >= shellVertexCount)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountOpenTopologyEdges(
            FoldCanvasCompileResult result)
        {
            int open = 0;
            foreach (KeyValuePair<ulong, int> edge in
                BuildTopologyEdgeIncidence(result))
            {
                if (edge.Value == 1)
                {
                    open++;
                }
            }

            return open;
        }

        private static int CountNonManifoldTopologyEdges(
            FoldCanvasCompileResult result)
        {
            int nonManifold = 0;
            foreach (KeyValuePair<ulong, int> edge in
                BuildTopologyEdgeIncidence(result))
            {
                if (edge.Value > 2)
                {
                    nonManifold++;
                }
            }

            return nonManifold;
        }

        private static Dictionary<ulong, int>
            BuildTopologyEdgeIncidence(FoldCanvasCompileResult result)
        {
            Dictionary<ulong, int> incidence =
                new Dictionary<ulong, int>();
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = TopologyId(result, triangles[i]);
                int b = TopologyId(result, triangles[i + 1]);
                int c = TopologyId(result, triangles[i + 2]);
                AddEdge(incidence, a, b);
                AddEdge(incidence, b, c);
                AddEdge(incidence, c, a);
            }

            return incidence;
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
            return result.CompiledData.Vertices[vertexIndex]
                .TopologyVertexId;
        }

        private static void AssertSingleError(
            FoldCanvasCompileResult result,
            string code,
            string operationId)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(
                result.Diagnostics[0].OperationId,
                Is.EqualTo(operationId));
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
