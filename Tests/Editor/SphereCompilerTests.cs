using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class SphereCompilerTests
    {
        private const int GoldenGoreCount = 8;
        private const float GoldenRadius = 0.5f;
        private const float PositionTolerance = 1e-5f;

        [Test]
        public void Sphere_HasClosedTopology()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReport, Is.Not.Null);
            Assert.That(result.SphereReport.IsClosedSphere, Is.True);
            Assert.That(
                result.SphereReport.ConnectedComponentCount,
                Is.EqualTo(1));
            Assert.That(result.SphereReport.OpenEdgeCount, Is.Zero);
            Assert.That(
                result.ClosedVolumeReport.IsSingleClosedVolume,
                Is.True);
            Destroy(result, asset);
        }

        [Test]
        public void Sphere_HasNoNonManifoldEdges()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReport.NonManifoldEdgeCount, Is.Zero);
            Assert.That(
                result.SphereReport.OrientationConflictEdgeCount,
                Is.Zero);
            Assert.That(
                result.SphereReport.IsolatedTopologyVertexCount,
                Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void Sphere_HasEulerCharacteristicTwo()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(
                result.SphereReport.EulerCharacteristic,
                Is.EqualTo(2));
            Assert.That(
                result.SphereReport.SurfaceTopologyVertexCount -
                result.SphereReport.UniqueEdgeCount +
                result.SphereReport.TriangleCount,
                Is.EqualTo(2));
            Destroy(result, asset);
        }

        [Test]
        public void Sphere_RadiusErrorWithinTolerance()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(
                result.SphereReport.MaximumRadiusError,
                Is.LessThanOrEqualTo(
                    result.SphereReport.RadiusTolerance));
            foreach (FoldCanvasCompiledSphericalSurface surface in
                result.CompiledData.SphericalSurfaces)
            {
                foreach (int vertexIndex in
                    surface.SurfaceVertexIndices)
                {
                    float radius = Vector3.Distance(
                        result.CompiledData.Vertices[vertexIndex].Position,
                        surface.Center);
                    Assert.That(
                        radius,
                        Is.EqualTo(surface.Radius)
                            .Within(PositionTolerance));
                }
            }

            Destroy(result, asset);
        }

        [Test]
        public void Sphere_PolesAreMerged()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(
                result.SphereReport.NorthPoleTopologyCount,
                Is.EqualTo(1));
            Assert.That(
                result.SphereReport.SouthPoleTopologyCount,
                Is.EqualTo(1));
            int north =
                result.CompiledData.SphericalSurfaces[0]
                    .NorthPoleTopologyVertexId;
            int south =
                result.CompiledData.SphericalSurfaces[0]
                    .SouthPoleTopologyVertexId;
            Assert.That(north, Is.Not.EqualTo(south));
            for (int i = 1;
                i < result.CompiledData.SphericalSurfaces.Count;
                i++)
            {
                Assert.That(
                    result.CompiledData.SphericalSurfaces[i]
                        .NorthPoleTopologyVertexId,
                    Is.EqualTo(north));
                Assert.That(
                    result.CompiledData.SphericalSurfaces[i]
                        .SouthPoleTopologyVertexId,
                    Is.EqualTo(south));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Sphere_SourceUvPreserved()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledBoundary firstSide =
                result.CompiledData.GetPanel("gore-07")
                    .GetBoundary("uMax");
            FoldCanvasCompiledBoundary secondSide =
                result.CompiledData.GetPanel("gore-00")
                    .GetBoundary("uMin");
            int midpoint = firstSide.VertexIndices.Count / 2;
            FoldCanvasCompiledVertex first =
                result.CompiledData.Vertices[
                    firstSide.VertexIndices[midpoint]];
            FoldCanvasCompiledVertex second =
                result.CompiledData.Vertices[
                    secondSide.VertexIndices[midpoint]];
            Assert.That(
                first.TopologyVertexId,
                Is.EqualTo(second.TopologyVertexId));
            Assert.That(first.SourceUv, Is.Not.EqualTo(second.SourceUv));
            Assert.That(first.SourcePosition, Is.Not.EqualTo(Vector2.zero));
            Assert.That(
                first.SourceUv.x,
                Is.InRange(0f, 1f));
            Assert.That(
                first.SourceUv.y,
                Is.InRange(0f, 1f));
            Assert.That(first.ProvenanceId, Is.Not.EqualTo(second.ProvenanceId));
            Destroy(result, asset);
        }

        [Test]
        public void Sphere_RegenerationIsDeterministic()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            Assert.That(
                second.CompiledData.Vertices.Count,
                Is.EqualTo(first.CompiledData.Vertices.Count));
            for (int i = 0; i < first.CompiledData.Vertices.Count; i++)
            {
                Assert.That(
                    second.CompiledData.Vertices[i],
                    Is.EqualTo(first.CompiledData.Vertices[i]));
            }

            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            Assert.That(
                second.Diagnostics.Count,
                Is.EqualTo(first.Diagnostics.Count));
            for (int i = 0; i < first.Diagnostics.Count; i++)
            {
                Assert.That(
                    second.Diagnostics[i].Code,
                    Is.EqualTo(first.Diagnostics[i].Code));
                Assert.That(
                    second.Diagnostics[i].Values.Count,
                    Is.EqualTo(first.Diagnostics[i].Values.Count));
            }

            Assert.That(
                second.SphereReport.MaximumRadiusError,
                Is.EqualTo(first.SphereReport.MaximumRadiusError));
            Assert.That(
                second.SphereReport.EulerCharacteristic,
                Is.EqualTo(first.SphereReport.EulerCharacteristic));
            Destroy(first, null);
            Destroy(second, asset);
        }

        [Test]
        public void Sphere_UnequalSeamSamplesRemainOnRadius()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset(
                index => index == 0 ? 8 : 16);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReport.IsClosedSphere, Is.True);
            Assert.That(
                result.CompiledData.GetPanel("gore-00")
                    .GetBoundary("uMax").VertexIndices.Count,
                Is.EqualTo(17));
            Assert.That(
                result.SphereReport.MaximumRadiusError,
                Is.LessThanOrEqualTo(
                    result.SphereReport.RadiusTolerance));
            Destroy(result, asset);
        }

        [Test]
        public void Sphere_HasOutwardWinding()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReport.InwardTriangleCount, Is.Zero);
            AssertAllTrianglesFaceOutward(
                result.CompiledData,
                Vector3.zero);
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_AfterRigidTransform_PreservesCurrentFrame()
        {
            Vector3 translation = new Vector3(1.25f, -0.4f, 2f);
            Vector3 rotationEuler = new Vector3(17f, 38f, -12f);
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                new Vector2(-60f, 60f),
                new Vector2(-25f, 25f));
            asset.Operations.Insert(0, new RigidTransformOperationDefinition
            {
                Id = "position",
                PanelId = "patch",
                Translation = translation,
                RotationEuler = rotationEuler,
                Scale = Vector3.one
            });

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledSphericalSurface surface =
                result.CompiledData.SphericalSurfaces[0];
            Quaternion rotation = Quaternion.Euler(rotationEuler);
            AssertVector(surface.Center, translation);
            AssertVector(surface.CurrentU, rotation * Vector3.right);
            AssertVector(surface.CurrentV, rotation * Vector3.up);
            AssertVector(surface.CurrentNormal, rotation * Vector3.forward);
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_AfterUnitReflection_HasOutwardWinding()
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                new Vector2(-60f, 60f),
                new Vector2(-25f, 25f));
            asset.Operations.Insert(0, new RigidTransformOperationDefinition
            {
                Id = "reflect",
                PanelId = "patch",
                Scale = new Vector3(-1f, 1f, 1f)
            });

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReport.InwardTriangleCount, Is.Zero);
            FoldCanvasCompiledSphericalSurface surface =
                result.CompiledData.SphericalSurfaces[0];
            AssertVector(surface.CurrentU, Vector3.left);
            AssertVector(surface.CurrentV, Vector3.up);
            AssertVector(surface.CurrentNormal, Vector3.back);
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_InvalidEmbedding_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                new Vector2(-60f, 60f),
                new Vector2(-25f, 25f));
            asset.Operations.Insert(0, new RigidTransformOperationDefinition
            {
                Id = "scale",
                PanelId = "patch",
                Scale = new Vector3(2f, 1f, 1f)
            });

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedSphericalEmbedding,
                "patch",
                "wrap");
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_InvalidRadius_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                new Vector2(-60f, 60f),
                new Vector2(-25f, 25f));
            ((SphericalWrapOperationDefinition)asset.Operations[0]).Radius =
                0f;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.InvalidSphericalRadius,
                "patch",
                "wrap");
            Assert.That(result.Diagnostics[0].Values.Count, Is.EqualTo(2));
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_MultiTurnLongitude_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                new Vector2(-60f, 60f),
                new Vector2(0f, 361f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedSphericalMultiTurn,
                "patch",
                "wrap");
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_InsufficientPoleTessellation_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<
                FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "patch",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 1f),
                4,
                1));
            asset.Operations.Add(CreateWrap(
                "wrap",
                "patch",
                new Vector2(-90f, 90f),
                new Vector2(-30f, 30f),
                SphericalPoleMode.Merge));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes
                    .InsufficientSphericalPoleTessellation,
                "patch",
                "wrap");
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_KeepFan_PreservesRenderUvSplitsAndOneTopologyPole()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<
                FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "patch",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 1f),
                4,
                4));
            asset.Operations.Add(CreateWrap(
                "wrap",
                "patch",
                new Vector2(-90f, 90f),
                new Vector2(-30f, 30f),
                SphericalPoleMode.KeepFan));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledBoundary south =
                result.CompiledData.GetPanel("patch")
                    .GetBoundary("vMin");
            Assert.That(south.VertexIndices.Count, Is.EqualTo(4));
            HashSet<int> topologyIds = new HashSet<int>();
            HashSet<float> sourceUvX = new HashSet<float>();
            HashSet<int> referenced = new HashSet<int>(
                result.CompiledData.TriangleIndices);
            foreach (int vertexIndex in south.VertexIndices)
            {
                topologyIds.Add(
                    result.CompiledData.Vertices[vertexIndex]
                        .TopologyVertexId);
                sourceUvX.Add(
                    result.CompiledData.Vertices[vertexIndex]
                        .SourceUv.x);
                Assert.That(referenced.Contains(vertexIndex), Is.True);
            }

            Assert.That(topologyIds.Count, Is.EqualTo(1));
            Assert.That(sourceUvX.Count, Is.EqualTo(4));
            Destroy(result, asset);
        }

        [Test]
        public void Sphere_CanContinueSolidify()
        {
            FoldCanvasAsset asset = CreateGoldenSphereAsset();
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify",
                    Thickness = 0.01f,
                    Direction = SolidifyDirection.Outward
                };
            for (int i = 0; i < GoldenGoreCount; i++)
            {
                solidify.PanelIds.Add($"gore-{i:00}");
            }

            asset.Operations.Add(solidify);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.EqualTo(1));
            Assert.That(
                result.SolidifyClosedVolumeReports[0].IsClosedVolume,
                Is.True);
            Destroy(result, asset);
        }

        internal static FoldCanvasAsset CreateGoldenSphereAsset(
            Func<int, int> latitudeSegments = null)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M05SphereGolden";
            float goreWidth =
                GoldenRadius * Mathf.PI * 2f / GoldenGoreCount;
            float goreHeight = GoldenRadius * Mathf.PI;
            for (int i = 0; i < GoldenGoreCount; i++)
            {
                int vSegments = latitudeSegments?.Invoke(i) ?? 16;
                string panelId = $"gore-{i:00}";
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    panelId,
                    new Rect(
                        i / (float)GoldenGoreCount,
                        0f,
                        1f / GoldenGoreCount,
                        1f),
                    new Vector2(goreWidth, goreHeight),
                    4,
                    vSegments));
                asset.Operations.Add(CreateWrap(
                    $"wrap-{i:00}",
                    panelId,
                    new Vector2(-90f, 90f),
                    new Vector2(
                        -180f + i * 45f,
                        -180f + (i + 1) * 45f),
                    SphericalPoleMode.Merge));
            }

            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-sphere"
                };
            for (int i = 0; i < GoldenGoreCount; i++)
            {
                string seamId = $"seam-{i:00}";
                string panelA = $"gore-{i:00}";
                string panelB =
                    $"gore-{(i + 1) % GoldenGoreCount:00}";
                asset.Seams.Add(new SeamDefinition
                {
                    Id = seamId,
                    A = new BoundaryReference(panelA, "uMax"),
                    B = new BoundaryReference(panelB, "uMin"),
                    Mode = SeamMode.Weld,
                    ReverseB = false,
                    SampleCount = 0
                });
                stitch.SeamIds.Add(seamId);
            }

            asset.Operations.Add(stitch);
            return asset;
        }

        private static FoldCanvasAsset CreateSinglePatchAsset(
            Vector2 latitudeRange,
            Vector2 longitudeRange)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "patch",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 1f),
                4,
                8));
            asset.Operations.Add(CreateWrap(
                "wrap",
                "patch",
                latitudeRange,
                longitudeRange,
                SphericalPoleMode.Merge));
            return asset;
        }

        private static SphericalWrapOperationDefinition CreateWrap(
            string operationId,
            string panelId,
            Vector2 latitudeRange,
            Vector2 longitudeRange,
            SphericalPoleMode poleMode)
        {
            return new SphericalWrapOperationDefinition
            {
                Id = operationId,
                PanelId = panelId,
                Radius = GoldenRadius,
                LatitudeRange = latitudeRange,
                LongitudeRange = longitudeRange,
                WrapDirection =
                    SphericalWrapDirection.LongitudeAlongU,
                PoleMode = poleMode,
                SubdivisionMode =
                    SphericalSubdivisionMode.PanelGrid
            };
        }

        private static void AssertAllTrianglesFaceOutward(
            FoldCanvasCompiledData data,
            Vector3 center)
        {
            for (int offset = 0;
                offset < data.TriangleIndices.Count;
                offset += 3)
            {
                Vector3 a = data.Vertices[
                    data.TriangleIndices[offset]].Position;
                Vector3 b = data.Vertices[
                    data.TriangleIndices[offset + 1]].Position;
                Vector3 c = data.Vertices[
                    data.TriangleIndices[offset + 2]].Position;
                Vector3 cross = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                Assert.That(
                    Vector3.Dot(cross, centroid - center),
                    Is.GreaterThan(0f));
            }
        }

        private static void AssertOnlyError(
            FoldCanvasCompileResult result,
            string code,
            string panelId,
            string operationId)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(
                result.Diagnostics[0].Severity,
                Is.EqualTo(FoldCanvasDiagnosticSeverity.Error));
            Assert.That(
                result.Diagnostics[0].PanelId,
                Is.EqualTo(panelId));
            Assert.That(
                result.Diagnostics[0].OperationId,
                Is.EqualTo(operationId));
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(
                Vector3.Distance(actual, expected),
                Is.LessThanOrEqualTo(PositionTolerance),
                $"Expected {expected}, got {actual}.");
        }

        private static string JoinDiagnostics(
            FoldCanvasCompileResult result)
        {
            List<string> diagnostics = new List<string>();
            foreach (FoldCanvasDiagnostic diagnostic in result.Diagnostics)
            {
                string text = diagnostic.ToString();
                for (int i = 0; i < diagnostic.Values.Count; i++)
                {
                    FoldCanvasDiagnosticValue value =
                        diagnostic.Values[i];
                    text +=
                        $" {value.Key}={value.Value:R}{value.Unit}";
                }

                diagnostics.Add(text);
            }

            return string.Join("\n", diagnostics);
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset)
        {
            if (result?.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }

            if (asset != null)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
