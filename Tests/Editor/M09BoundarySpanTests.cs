using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M09BoundarySpanTests
    {
        private const float Tolerance = 0.00001f;

        [Test]
        public void BoundarySpan_OmittedPreservesFullBoundaryBehavior()
        {
            FoldCanvasAsset legacy = CreateAdjacentAsset(
                new BoundaryReference("a", "uMax"),
                new BoundaryReference("b", "uMin"),
                4);
            FoldCanvasAsset explicitFull = CreateAdjacentAsset(
                new BoundaryReference(
                    "a",
                    "uMax",
                    new Vector2(0f, 1f)),
                new BoundaryReference(
                    "b",
                    "uMin",
                    new Vector2(0f, 1f)),
                4);

            FoldCanvasCompileResult legacyResult =
                FoldCanvasCompiler.Compile(legacy);
            FoldCanvasCompileResult explicitResult =
                FoldCanvasCompiler.Compile(explicitFull);

            Assert.That(
                legacyResult.Success,
                Is.True,
                Diagnostics(legacyResult));
            Assert.That(
                explicitResult.Success,
                Is.True,
                Diagnostics(explicitResult));
            CollectionAssert.AreEqual(
                legacyResult.CompiledData.Vertices,
                explicitResult.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                legacyResult.CompiledData.TriangleIndices,
                explicitResult.CompiledData.TriangleIndices);
            Destroy(legacyResult, legacy);
            Destroy(explicitResult, explicitFull);
        }

        [Test]
        public void BoundarySpan_SelectsDeterministicOpenSubchain()
        {
            FoldCanvasAsset asset = CreateAdjacentAsset(
                new BoundaryReference(
                    "a",
                    "uMax",
                    new Vector2(0.25f, 0.75f)),
                new BoundaryReference(
                    "b",
                    "uMin",
                    new Vector2(0.25f, 0.75f)),
                4);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertSharedAtY(result, -0.25f, true);
            AssertSharedAtY(result, 0f, true);
            AssertSharedAtY(result, 0.25f, true);
            AssertSharedAtY(result, -0.5f, false);
            AssertSharedAtY(result, 0.5f, false);
            Destroy(result, asset);
        }

        [Test]
        public void BoundarySpan_OffGridEndpointsSubdivideDeterministically()
        {
            FoldCanvasAsset asset = CreateAdjacentAsset(
                new BoundaryReference(
                    "a",
                    "uMax",
                    new Vector2(0.2f, 0.8f)),
                new BoundaryReference(
                    "b",
                    "uMin",
                    new Vector2(0.2f, 0.8f)),
                2);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            AssertSharedAtY(first, -0.3f, true);
            AssertSharedAtY(first, 0.3f, true);
            Assert.That(
                first.CompiledData.Vertices.Count,
                Is.GreaterThan(12));
            CollectionAssert.AreEqual(
                first.CompiledData.Vertices,
                second.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void BoundarySpan_ReverseBAppliesAfterSelection()
        {
            FoldCanvasAsset asset = CreateAdjacentAsset(
                new BoundaryReference(
                    "a",
                    "uMax",
                    new Vector2(0.2f, 0.6f)),
                new BoundaryReference(
                    "b",
                    "uMax",
                    new Vector2(0.4f, 0.8f)),
                5,
                rotateB: true,
                reverseB: true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertSharedAtY(result, -0.3f, true, "uMax", "uMax");
            AssertSharedAtY(result, 0.1f, true, "uMax", "uMax");
            AssertSharedAtY(result, -0.5f, false, "uMax", "uMax");
            AssertSharedAtY(result, 0.5f, false, "uMax", "uMax");
            Destroy(result, asset);
        }

        [Test]
        public void BoundarySpan_InvalidRangeReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentAsset(
                new BoundaryReference(
                    "a",
                    "uMax",
                    new Vector2(0.8f, 0.2f)),
                new BoundaryReference("b", "uMin"),
                2);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Mesh, Is.Null);
            Assert.That(first.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                first.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidBoundarySpan));
            Assert.That(
                second.Diagnostics[0].Code,
                Is.EqualTo(first.Diagnostics[0].Code));
            Assert.That(
                second.Diagnostics[0].Values[0].Value,
                Is.EqualTo(first.Diagnostics[0].Values[0].Value));
            Assert.That(
                second.Diagnostics[0].Values[1].Value,
                Is.EqualTo(first.Diagnostics[0].Values[1].Value));
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void BoundarySpan_FailedStitchRollsBackInsertedGeometry()
        {
            FoldCanvasAsset asset = CreateAdjacentAsset(
                new BoundaryReference(
                    "a",
                    "uMax",
                    new Vector2(0.2f, 0.8f)),
                new BoundaryReference(
                    "b",
                    "uMin",
                    new Vector2(0.2f, 0.8f)),
                2);
            RigidTransformOperationDefinition place =
                (RigidTransformOperationDefinition)asset.Operations[0];
            place.Translation = new Vector3(1.1f, 0f, 0f);
            StitchOperationDefinition stitch =
                (StitchOperationDefinition)asset.Operations[1];
            MeshBuildBuffer buffer = BuildBuffer(asset);
            FoldCanvasCompileResult transformResult =
                new FoldCanvasCompileResult();
            Assert.That(
                RigidTransformExecutor.TryExecute(
                    place,
                    buffer,
                    transformResult),
                Is.True,
                Diagnostics(transformResult));
            int vertexCount = buffer.Vertices.Count;
            int triangleCount = buffer.Triangles.Count;
            int boundaryA = BoundaryCount(buffer, 0, "uMax");
            int boundaryB = BoundaryCount(buffer, 1, "uMin");
            FoldCanvasCompileResult stitchResult =
                new FoldCanvasCompileResult();

            bool success = StitchExecutor.TryExecute(
                stitch,
                asset,
                buffer,
                stitchResult,
                out _);

            Assert.That(success, Is.False);
            Assert.That(
                stitchResult.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.StitchPositionMismatch));
            Assert.That(buffer.Vertices.Count, Is.EqualTo(vertexCount));
            Assert.That(buffer.Triangles.Count, Is.EqualTo(triangleCount));
            Assert.That(
                BoundaryCount(buffer, 0, "uMax"),
                Is.EqualTo(boundaryA));
            Assert.That(
                BoundaryCount(buffer, 1, "uMin"),
                Is.EqualTo(boundaryB));
            Assert.That(buffer.HasTopologyWelds, Is.False);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        private static FoldCanvasAsset CreateAdjacentAsset(
            BoundaryReference a,
            BoundaryReference b,
            int vSegments,
            bool rotateB = false,
            bool reverseB = false)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M09BoundarySpan";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "a",
                new Rect(0f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                vSegments));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "b",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                vSegments));
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-b",
                PanelId = "b",
                Translation = Vector3.right,
                RotationEuler = rotateB
                    ? new Vector3(0f, 0f, 180f)
                    : Vector3.zero,
                Scale = Vector3.one
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "span",
                A = a,
                B = b,
                Mode = SeamMode.Weld,
                ReverseB = reverseB
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-span"
                };
            stitch.SeamIds.Add("span");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static void AssertSharedAtY(
            FoldCanvasCompileResult result,
            float sourceY,
            bool expectedShared,
            string boundaryA = "uMax",
            string boundaryB = "uMin")
        {
            int a = FindBoundaryVertex(
                result,
                "a",
                boundaryA,
                sourceY);
            float bSourceY = boundaryB == "uMax"
                ? -sourceY
                : sourceY;
            int b = FindBoundaryVertex(
                result,
                "b",
                boundaryB,
                bSourceY);
            Assert.That(
                Topology(result, a) == Topology(result, b),
                Is.EqualTo(expectedShared),
                $"Unexpected topology sharing at source y={sourceY}.");
        }

        private static int FindBoundaryVertex(
            FoldCanvasCompileResult result,
            string panelId,
            string boundaryId,
            float sourceY)
        {
            IReadOnlyList<int> indices = result.CompiledData
                .GetPanel(panelId)
                .GetBoundary(boundaryId)
                .VertexIndices;
            int closest = -1;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < indices.Count; i++)
            {
                float distance = Mathf.Abs(
                    result.CompiledData.Vertices[indices[i]]
                        .SourcePosition.y - sourceY);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = indices[i];
                }
            }

            Assert.That(
                closestDistance,
                Is.LessThan(Tolerance),
                $"No boundary sample at source y={sourceY}.");
            return closest;
        }

        private static int Topology(
            FoldCanvasCompileResult result,
            int vertexIndex)
        {
            return result.CompiledData.Vertices[vertexIndex]
                .TopologyVertexId;
        }

        private static MeshBuildBuffer BuildBuffer(FoldCanvasAsset asset)
        {
            MeshBuildBuffer buffer = new MeshBuildBuffer(1000, 1000);
            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                FoldCanvasSourceValidator.TryEstimateGeometry(
                    panel,
                    out long vertices,
                    out long triangles);
                FoldCanvasCompileResult reservation =
                    new FoldCanvasCompileResult();
                Assert.That(
                    buffer.TryBeginGeometryOperation(
                        vertices,
                        triangles,
                        panel.Id,
                        "PanelTessellation",
                        reservation,
                        out GeometryOperationScope scope),
                    Is.True,
                    Diagnostics(reservation));
                using (scope)
                {
                    PanelTessellator.Append(panel, buffer, null);
                }
            }

            return buffer;
        }

        private static int BoundaryCount(
            MeshBuildBuffer buffer,
            int panelIndex,
            string boundaryId)
        {
            Assert.That(
                buffer.GetPanelAt(panelIndex).TryGetBoundary(
                    boundaryId,
                    out BoundaryBuildRecord boundary),
                Is.True);
            return boundary.VertexIndices.Count;
        }

        private static string Diagnostics(FoldCanvasCompileResult result)
        {
            string message = string.Empty;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                message += result.Diagnostics[i].Code + ": " +
                    result.Diagnostics[i].Message + "\n";
            }

            return message;
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset,
            bool destroyAsset = true)
        {
            if (result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }

            if (destroyAsset)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
