using System;
using System.Linq;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M13ScaleFamilyTests
    {
        [Test]
        [Timeout(120000)]
        public void Scale_LargePlanarFamilyProducesLockedEvidence()
        {
            AssertDeterministicFixture(
                FoldCanvasRobustnessScaleFixtures
                    .CreateLargePlanarAsset,
                FoldCanvasRobustnessScaleFixtures
                    .LargePlanarGeometrySha256,
                FoldCanvasRobustnessScaleFixtures
                    .LargePlanarRenderVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargePlanarTopologyVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargePlanarTriangleCount,
                result =>
                {
                    int expectedVertices =
                        (FoldCanvasRobustnessScaleFixtures
                            .LargePlanarUSegments + 1) *
                        (FoldCanvasRobustnessScaleFixtures
                            .LargePlanarVSegments + 1);
                    int expectedTriangles = 2 *
                        FoldCanvasRobustnessScaleFixtures
                            .LargePlanarUSegments *
                        FoldCanvasRobustnessScaleFixtures
                            .LargePlanarVSegments;
                    Assert.That(result.CompiledData.Vertices.Count,
                        Is.EqualTo(expectedVertices));
                    Assert.That(result.CompiledData.TopologyVertexCount,
                        Is.EqualTo(expectedVertices));
                    Assert.That(
                        result.CompiledData.TriangleIndices.Count / 3,
                        Is.EqualTo(expectedTriangles));
                    Assert.That(
                        result.GeometryValidationReport.OpenEdgeCount,
                        Is.EqualTo(2 *
                            (FoldCanvasRobustnessScaleFixtures
                                .LargePlanarUSegments +
                             FoldCanvasRobustnessScaleFixtures
                                .LargePlanarVSegments)));
                });
        }

        [Test]
        [Timeout(120000)]
        public void Scale_LargeCupFamilyProducesClosedLockedEvidence()
        {
            AssertDeterministicFixture(
                FoldCanvasRobustnessScaleFixtures.CreateLargeCupAsset,
                FoldCanvasRobustnessScaleFixtures
                    .LargeCupGeometrySha256,
                FoldCanvasRobustnessScaleFixtures
                    .LargeCupRenderVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeCupTopologyVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeCupTriangleCount,
                result =>
                {
                    Assert.That(result.ClosedVolumeReport, Is.Not.Null);
                    Assert.That(
                        result.ClosedVolumeReport.IsSingleClosedVolume,
                        Is.True);
                    Assert.That(
                        result.GeometryValidationReport.StrictChecksExecuted,
                        Is.True);
                    Assert.That(
                        result.GeometryValidationReport.OpenEdgeCount,
                        Is.Zero);
                });
        }

        [Test]
        [Timeout(120000)]
        public void Scale_DenseCupThicknessCrossingRowsReturnsStableIntersectionDiagnostic()
        {
            FoldCanvasAsset firstAsset =
                FoldCanvasRobustnessScaleFixtures.CreateLargeCupAsset(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeCupCrossingThickness);
            FoldCanvasAsset secondAsset =
                FoldCanvasRobustnessScaleFixtures.CreateLargeCupAsset(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeCupCrossingThickness);
            FoldCanvasCompileResult first = null;
            FoldCanvasCompileResult second = null;
            try
            {
                first = CompilePreservingSource(firstAsset);
                second = CompilePreservingSource(secondAsset);
                AssertStrictIntersectionFailure(first);
                AssertStrictIntersectionFailure(second);
                Assert.That(
                    FoldCanvasHandoffEvidenceBuilder.HashDiagnostics(second),
                    Is.EqualTo(
                        FoldCanvasHandoffEvidenceBuilder.HashDiagnostics(
                            first)));
            }
            finally
            {
                Destroy(first, firstAsset);
                Destroy(second, secondAsset);
            }
        }

        [Test]
        [Timeout(120000)]
        public void Scale_LargeSphereFamilyProducesClosedLockedEvidence()
        {
            AssertDeterministicFixture(
                FoldCanvasRobustnessScaleFixtures.CreateLargeSphereAsset,
                FoldCanvasRobustnessScaleFixtures
                    .LargeSphereGeometrySha256,
                FoldCanvasRobustnessScaleFixtures
                    .LargeSphereRenderVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeSphereTopologyVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeSphereTriangleCount,
                result =>
                {
                    Assert.That(result.SphereReports.Count, Is.EqualTo(1));
                    Assert.That(result.SphereReport.IsClosedSphere, Is.True);
                    Assert.That(
                        result.SphereReport.SphericalPanelCount,
                        Is.EqualTo(
                            FoldCanvasRobustnessScaleFixtures
                                .LargeSphereGoreCount));
                    Assert.That(
                        result.GeometryValidationReport.OpenEdgeCount,
                        Is.Zero);
                });
        }

        [Test]
        [Timeout(120000)]
        public void Scale_LargeTorusFamilyProducesClosedLockedEvidence()
        {
            AssertDeterministicFixture(
                FoldCanvasRobustnessScaleFixtures.CreateLargeTorusAsset,
                FoldCanvasRobustnessScaleFixtures
                    .LargeTorusGeometrySha256,
                FoldCanvasRobustnessScaleFixtures
                    .LargeTorusRenderVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeTorusTopologyVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeTorusTriangleCount,
                result =>
                {
                    Assert.That(
                        result.GeometryValidationReport.StrictChecksExecuted,
                        Is.True);
                    Assert.That(
                        result.GeometryValidationReport.ComponentCount,
                        Is.EqualTo(1));
                    Assert.That(
                        result.GeometryValidationReport.OpenEdgeCount,
                        Is.Zero);
                    Assert.That(
                        result.GeometryValidationReport
                            .NonManifoldEdgeCount,
                        Is.Zero);
                });
        }

        [Test]
        [Timeout(120000)]
        public void Scale_LargeStitchResamplingProducesLockedEvidence()
        {
            AssertDeterministicFixture(
                FoldCanvasRobustnessScaleFixtures.CreateLargeStitchAsset,
                FoldCanvasRobustnessScaleFixtures
                    .LargeStitchGeometrySha256,
                FoldCanvasRobustnessScaleFixtures
                    .LargeStitchRenderVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeStitchTopologyVertexCount,
                FoldCanvasRobustnessScaleFixtures
                    .LargeStitchTriangleCount,
                result =>
                {
                    FoldCanvasCompiledBoundary boundaryA =
                        result.CompiledData.GetPanel("left")
                            .GetBoundary("uMax");
                    FoldCanvasCompiledBoundary boundaryB =
                        result.CompiledData.GetPanel("right")
                            .GetBoundary("uMin");
                    Assert.That(
                        boundaryA.VertexIndices.Count,
                        Is.EqualTo(
                            FoldCanvasRobustnessScaleFixtures
                                .LargeStitchSampleCount));
                    Assert.That(
                        boundaryB.VertexIndices.Count,
                        Is.EqualTo(boundaryA.VertexIndices.Count));
                    for (int sampleIndex = 0;
                        sampleIndex < boundaryA.VertexIndices.Count;
                        sampleIndex++)
                    {
                        FoldCanvasCompiledVertex a =
                            result.CompiledData.Vertices[
                                boundaryA.VertexIndices[sampleIndex]];
                        FoldCanvasCompiledVertex b =
                            result.CompiledData.Vertices[
                                boundaryB.VertexIndices[sampleIndex]];
                        Assert.That(
                            a.TopologyVertexId,
                            Is.EqualTo(b.TopologyVertexId));
                        Assert.That(
                            Vector3.Distance(a.Position, b.Position),
                            Is.LessThanOrEqualTo(
                                result.GeometryValidationReport.Seams[0]
                                    .Tolerance));
                    }

                    Assert.That(
                        result.GeometryValidationReport.Seams.Count,
                        Is.EqualTo(1));
                    Assert.That(
                        result.GeometryValidationReport.Seams[0]
                            .SampleCount,
                        Is.EqualTo(
                            FoldCanvasRobustnessScaleFixtures
                                .LargeStitchSampleCount));
                    Assert.That(
                        result.GeometryValidationReport.Seams[0]
                            .IsClosedWithinTolerance,
                        Is.True);
                });
        }

        private static void AssertDeterministicFixture(
            Func<FoldCanvasAsset> createAsset,
            string expectedGeometryHash,
            int expectedRenderVertexCount,
            int expectedTopologyVertexCount,
            int expectedTriangleCount,
            Action<FoldCanvasCompileResult> assertFamily)
        {
            FoldCanvasAsset firstAsset = createAsset();
            FoldCanvasAsset secondAsset = createAsset();
            FoldCanvasCompileResult first = null;
            FoldCanvasCompileResult second = null;
            try
            {
                first = CompilePreservingSource(firstAsset);
                second = CompilePreservingSource(secondAsset);
                AssertSuccess(first);
                AssertSuccess(second);
                assertFamily(first);
                assertFamily(second);
                Assert.That(first.CompiledData.Vertices.Count,
                    Is.EqualTo(expectedRenderVertexCount));
                Assert.That(first.CompiledData.TopologyVertexCount,
                    Is.EqualTo(expectedTopologyVertexCount));
                Assert.That(
                    first.CompiledData.TriangleIndices.Count / 3,
                    Is.EqualTo(expectedTriangleCount));
                Assert.That(second.CompiledData.Vertices.Count,
                    Is.EqualTo(expectedRenderVertexCount));
                Assert.That(second.CompiledData.TopologyVertexCount,
                    Is.EqualTo(expectedTopologyVertexCount));
                Assert.That(
                    second.CompiledData.TriangleIndices.Count / 3,
                    Is.EqualTo(expectedTriangleCount));

                string firstHash =
                    FoldCanvasHandoffEvidenceBuilder.HashCompiledData(
                        first.CompiledData);
                string secondHash =
                    FoldCanvasHandoffEvidenceBuilder.HashCompiledData(
                        second.CompiledData);
                string evidence = firstAsset.name +
                    " render=" + first.CompiledData.Vertices.Count +
                    " topology=" +
                    first.CompiledData.TopologyVertexCount +
                    " triangles=" +
                    first.CompiledData.TriangleIndices.Count / 3 +
                    " sha256=" + firstHash;
                TestContext.Progress.WriteLine(evidence);
                Debug.Log("FoldCanvas M13 scale evidence: " + evidence);
                Assert.That(secondHash, Is.EqualTo(firstHash));
                Assert.That(firstHash,
                    Is.EqualTo(expectedGeometryHash));
            }
            finally
            {
                Destroy(first, firstAsset);
                Destroy(second, secondAsset);
            }
        }

        private static FoldCanvasCompileResult CompilePreservingSource(
            FoldCanvasAsset asset)
        {
            string before =
                FoldCanvasRobustnessGenerator.CanonicalSource(asset);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);
            Assert.That(
                FoldCanvasRobustnessGenerator.CanonicalSource(asset),
                Is.EqualTo(before));
            return result;
        }

        private static void AssertSuccess(FoldCanvasCompileResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Mesh, Is.Not.Null);
            Assert.That(result.CompiledData, Is.Not.Null);
            Assert.That(result.GeometryValidationReport, Is.Not.Null);
            Assert.That(result.GeometryValidationReport.IsValid, Is.True);
            Assert.That(result.CompiledData.Vertices.All(item =>
                    Finite(item.Position) &&
                    Finite(item.SourcePosition) &&
                    Finite(item.SourceUv)),
                Is.True);
            Assert.That(result.Mesh.normals.Length,
                Is.EqualTo(result.CompiledData.Vertices.Count));
            Assert.That(result.Mesh.normals.All(Finite), Is.True);
        }

        private static void AssertStrictIntersectionFailure(
            FoldCanvasCompileResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.GeometryValidationReport, Is.Not.Null);
            Assert.That(
                result.GeometryValidationReport.StrictChecksExecuted,
                Is.True);
            Assert.That(
                result.GeometryValidationReport.ExactIntersectionPairCount,
                Is.GreaterThan(0));
            Assert.That(result.Diagnostics.Any(item =>
                    item.Severity == FoldCanvasDiagnosticSeverity.Error &&
                    item.Code == FoldCanvasDiagnosticCodes.SelfIntersection),
                Is.True);
        }

        private static bool Finite(Vector2 value)
        {
            return Finite(value.x) && Finite(value.y);
        }

        private static bool Finite(Vector3 value)
        {
            return Finite(value.x) && Finite(value.y) && Finite(value.z);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string JoinDiagnostics(
            FoldCanvasCompileResult result)
        {
            return result == null
                ? "null result"
                : string.Join(" | ", result.Diagnostics.Select(
                    item => item.ToString()));
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
