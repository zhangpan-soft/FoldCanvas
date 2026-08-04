using System;
using System.Linq;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M13ScaleTests
    {
        private const ulong RetrySeed = 0x5343414C45524554UL;

        [Test]
        public void Scale_NearVertexBudgetSucceedsDeterministically()
        {
            FoldCanvasAsset headroom =
                FoldCanvasRobustnessScaleFixtures
                    .CreateLargeSolidifyAsset(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedRenderVertexCount + 1,
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedTriangleCount + 1);
            FoldCanvasAsset exact =
                FoldCanvasRobustnessScaleFixtures
                    .CreateLargeSolidifyAsset(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedRenderVertexCount,
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedTriangleCount + 1);
            FoldCanvasCompileResult headroomResult = null;
            FoldCanvasCompileResult exactResult = null;
            try
            {
                headroomResult = CompilePreservingSource(headroom);
                exactResult = CompilePreservingSource(exact);

                AssertLargeSolidifySuccess(headroomResult);
                AssertLargeSolidifySuccess(exactResult);
                Assert.That(
                    GeometryHash(exactResult),
                    Is.EqualTo(GeometryHash(headroomResult)));
                Assert.That(
                    GeometryHash(exactResult),
                    Is.EqualTo(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifyGeometrySha256));
            }
            finally
            {
                Destroy(headroomResult, headroom);
                Destroy(exactResult, exact);
            }
        }

        [Test]
        public void Scale_OneVertexOverBudgetReturnsFC5005WithoutMesh()
        {
            FoldCanvasAsset firstAsset =
                CreateVertexOverBudgetAsset();
            FoldCanvasAsset secondAsset =
                CreateVertexOverBudgetAsset();
            FoldCanvasCompileResult first = null;
            FoldCanvasCompileResult second = null;
            try
            {
                first = CompilePreservingSource(firstAsset);
                second = CompilePreservingSource(secondAsset);

                AssertBudgetFailure(
                    first,
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded);
                AssertBudgetFailure(
                    second,
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded);
                AssertDiagnosticValue(
                    first.Diagnostics[0],
                    "currentUsed",
                    FoldCanvasRobustnessScaleFixtures
                        .LargeBaseVertexCount);
                AssertDiagnosticValue(
                    first.Diagnostics[0],
                    "requestedAdditional",
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedRenderVertexCount -
                    FoldCanvasRobustnessScaleFixtures
                        .LargeBaseVertexCount);
                AssertDiagnosticValue(
                    first.Diagnostics[0],
                    "maximumAllowed",
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedRenderVertexCount - 1);
                Assert.That(
                    DiagnosticHash(second),
                    Is.EqualTo(DiagnosticHash(first)));
            }
            finally
            {
                Destroy(first, firstAsset);
                Destroy(second, secondAsset);
            }
        }

        [Test]
        public void Scale_NearTriangleBudgetSucceedsDeterministically()
        {
            FoldCanvasAsset headroom =
                FoldCanvasRobustnessScaleFixtures
                    .CreateLargeSolidifyAsset(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedRenderVertexCount + 1,
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedTriangleCount + 1);
            FoldCanvasAsset exact =
                FoldCanvasRobustnessScaleFixtures
                    .CreateLargeSolidifyAsset(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedRenderVertexCount + 1,
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedTriangleCount);
            FoldCanvasCompileResult headroomResult = null;
            FoldCanvasCompileResult exactResult = null;
            try
            {
                headroomResult = CompilePreservingSource(headroom);
                exactResult = CompilePreservingSource(exact);

                AssertLargeSolidifySuccess(headroomResult);
                AssertLargeSolidifySuccess(exactResult);
                Assert.That(
                    GeometryHash(exactResult),
                    Is.EqualTo(GeometryHash(headroomResult)));
                Assert.That(
                    GeometryHash(exactResult),
                    Is.EqualTo(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifyGeometrySha256));
            }
            finally
            {
                Destroy(headroomResult, headroom);
                Destroy(exactResult, exact);
            }
        }

        [Test]
        public void Scale_OneTriangleOverBudgetReturnsFC5006WithoutMesh()
        {
            FoldCanvasAsset firstAsset =
                CreateTriangleOverBudgetAsset();
            FoldCanvasAsset secondAsset =
                CreateTriangleOverBudgetAsset();
            FoldCanvasCompileResult first = null;
            FoldCanvasCompileResult second = null;
            try
            {
                first = CompilePreservingSource(firstAsset);
                second = CompilePreservingSource(secondAsset);

                AssertBudgetFailure(
                    first,
                    FoldCanvasDiagnosticCodes
                        .GeneratedTriangleLimitExceeded);
                AssertBudgetFailure(
                    second,
                    FoldCanvasDiagnosticCodes
                        .GeneratedTriangleLimitExceeded);
                AssertDiagnosticValue(
                    first.Diagnostics[0],
                    "currentUsed",
                    FoldCanvasRobustnessScaleFixtures
                        .LargeBaseTriangleCount);
                AssertDiagnosticValue(
                    first.Diagnostics[0],
                    "requestedAdditional",
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedTriangleCount -
                    FoldCanvasRobustnessScaleFixtures
                        .LargeBaseTriangleCount);
                AssertDiagnosticValue(
                    first.Diagnostics[0],
                    "maximumAllowed",
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedTriangleCount - 1);
                Assert.That(
                    DiagnosticHash(second),
                    Is.EqualTo(DiagnosticHash(first)));
            }
            finally
            {
                Destroy(first, firstAsset);
                Destroy(second, secondAsset);
            }
        }

        [Test]
        [Timeout(30000)]
        public void Scale_StrictValidationBudgetReturnsStableDiagnostic()
        {
            FoldCanvasAsset firstAsset =
                FoldCanvasRobustnessScaleFixtures
                    .CreateStrictValidationBudgetAsset();
            FoldCanvasAsset secondAsset =
                FoldCanvasRobustnessScaleFixtures
                    .CreateStrictValidationBudgetAsset();
            FoldCanvasCompileResult first = null;
            FoldCanvasCompileResult second = null;
            try
            {
                first = CompilePreservingSource(firstAsset);
                second = CompilePreservingSource(secondAsset);

                AssertStrictBudgetFailure(first);
                AssertStrictBudgetFailure(second);
                Assert.That(
                    DiagnosticHash(second),
                    Is.EqualTo(DiagnosticHash(first)));
                Assert.That(
                    FoldCanvasRobustnessGenerator.CanonicalSource(
                        secondAsset),
                    Is.EqualTo(
                        FoldCanvasRobustnessGenerator.CanonicalSource(
                            firstAsset)));
            }
            finally
            {
                Destroy(first, firstAsset);
                Destroy(second, secondAsset);
            }
        }

        [Test]
        public void LargeFailureThenSmallRetryMatchesCleanCompile()
        {
            FoldCanvasRobustnessCaseResult clean =
                FoldCanvasRobustnessRunner.RunCase(
                    FoldCanvasRobustnessGenerator.PlanarValidSuite,
                    RetrySeed,
                    3);
            FoldCanvasAsset failingAsset =
                CreateVertexOverBudgetAsset();
            FoldCanvasCompileResult failure = null;
            try
            {
                failure = CompilePreservingSource(failingAsset);
                AssertBudgetFailure(
                    failure,
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded);

                FoldCanvasRobustnessCaseResult retry =
                    FoldCanvasRobustnessRunner.RunCase(
                        FoldCanvasRobustnessGenerator.PlanarValidSuite,
                        RetrySeed,
                        3);
                Assert.That(clean.passed, Is.True);
                Assert.That(retry.passed, Is.True);
                Assert.That(retry.sourceSha256,
                    Is.EqualTo(clean.sourceSha256));
                Assert.That(retry.geometrySha256,
                    Is.EqualTo(clean.geometrySha256));
                Assert.That(retry.diagnosticSha256,
                    Is.EqualTo(clean.diagnosticSha256));
                Assert.That(retry.renderVertexCount,
                    Is.EqualTo(clean.renderVertexCount));
                Assert.That(retry.triangleCount,
                    Is.EqualTo(clean.triangleCount));
            }
            finally
            {
                Destroy(failure, failingAsset);
            }
        }

        [Test]
        public void RepeatedCompile_DoesNotLeakGeometryStateOrChangeSource()
        {
            FoldCanvasAsset asset =
                FoldCanvasRobustnessScaleFixtures
                    .CreateLargeSolidifyAsset(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedRenderVertexCount,
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifiedTriangleCount);
            string sourceBefore =
                FoldCanvasRobustnessGenerator.CanonicalSource(asset);
            string expectedGeometryHash = null;
            try
            {
                for (int iteration = 0; iteration < 3; iteration++)
                {
                    FoldCanvasCompileResult result =
                        CompilePreservingSource(asset);
                    try
                    {
                        AssertLargeSolidifySuccess(result);
                        string geometryHash = GeometryHash(result);
                        if (expectedGeometryHash == null)
                        {
                            expectedGeometryHash = geometryHash;
                        }
                        else
                        {
                            Assert.That(
                                geometryHash,
                                Is.EqualTo(expectedGeometryHash));
                        }

                        Assert.That(
                            FoldCanvasRobustnessGenerator.CanonicalSource(
                                asset),
                            Is.EqualTo(sourceBefore));
                    }
                    finally
                    {
                        Destroy(result, null);
                    }
                }

                Assert.That(
                    expectedGeometryHash,
                    Is.EqualTo(
                        FoldCanvasRobustnessScaleFixtures
                            .LargeSolidifyGeometrySha256));
            }
            finally
            {
                Destroy(null, asset);
            }
        }

        private static FoldCanvasAsset CreateVertexOverBudgetAsset()
        {
            return FoldCanvasRobustnessScaleFixtures
                .CreateLargeSolidifyAsset(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedRenderVertexCount - 1,
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedTriangleCount + 1);
        }

        private static FoldCanvasAsset CreateTriangleOverBudgetAsset()
        {
            return FoldCanvasRobustnessScaleFixtures
                .CreateLargeSolidifyAsset(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedRenderVertexCount + 1,
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedTriangleCount - 1);
        }

        private static void AssertLargeSolidifySuccess(
            FoldCanvasCompileResult result)
        {
            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Mesh, Is.Not.Null);
            Assert.That(result.CompiledData, Is.Not.Null);
            Assert.That(
                result.CompiledData.Vertices.Count,
                Is.EqualTo(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedRenderVertexCount));
            Assert.That(
                result.CompiledData.TopologyVertexCount,
                Is.EqualTo(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedTopologyVertexCount));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(
                    FoldCanvasRobustnessScaleFixtures
                        .LargeSolidifiedTriangleCount));
            Assert.That(result.CompiledData.Vertices.All(item =>
                    Finite(item.Position) &&
                    Finite(item.SourcePosition) &&
                    Finite(item.SourceUv)),
                Is.True);
            Assert.That(result.Mesh.normals.Length,
                Is.EqualTo(result.CompiledData.Vertices.Count));
            Assert.That(result.Mesh.normals.All(Finite), Is.True);
        }

        private static void AssertBudgetFailure(
            FoldCanvasCompileResult result,
            string expectedCode)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity,
                Is.EqualTo(FoldCanvasDiagnosticSeverity.Error));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(expectedCode));
        }

        private static void AssertStrictBudgetFailure(
            FoldCanvasCompileResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.GeometryValidationReport, Is.Not.Null);
            Assert.That(
                result.GeometryValidationReport
                    .StrictCandidateBudgetExceeded,
                Is.True);
            Assert.That(
                result.GeometryValidationReport
                    .BroadPhaseCandidatePairCount,
                Is.EqualTo(
                    FoldCanvasLimits
                        .MaximumStrictValidationCandidatePairs + 1));
            Assert.That(result.Diagnostics.Any(item =>
                    item.Severity == FoldCanvasDiagnosticSeverity.Error &&
                    item.Code == FoldCanvasDiagnosticCodes
                        .StrictValidationBudgetExceeded),
                Is.True);
        }

        private static void AssertDiagnosticValue(
            FoldCanvasDiagnostic diagnostic,
            string key,
            double expected)
        {
            FoldCanvasDiagnosticValue value = diagnostic.Values.Single(
                item => string.Equals(
                    item.Key,
                    key,
                    StringComparison.Ordinal));
            Assert.That(value.Value, Is.EqualTo(expected));
        }

        private static string GeometryHash(
            FoldCanvasCompileResult result)
        {
            return FoldCanvasHandoffEvidenceBuilder.HashCompiledData(
                result.CompiledData);
        }

        private static string DiagnosticHash(
            FoldCanvasCompileResult result)
        {
            return FoldCanvasHandoffEvidenceBuilder.HashDiagnostics(result);
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
