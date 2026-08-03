using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M05HardeningTests
    {
        private const int GoreCount = 8;
        private const float Radius = 0.5f;

        [Test]
        public void OpenSphere_FollowedBySolidify_IsRejected()
        {
            FoldCanvasAsset asset = CreateSphereAsset(
                "open",
                Vector3.zero,
                false);
            asset.Operations.Add(CreateSolidify(
                "solidify-open",
                GetPanelIds("open")));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphereValidationFailed);
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            Assert.That(
                result.SphereReports[0].ValidationStage,
                Is.EqualTo(SphereValidationStage.PreSolidify));
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void UnrelatedSolidify_DoesNotSuppressSphereValidation()
        {
            FoldCanvasAsset asset = CreateSphereAsset(
                "open",
                Vector3.zero,
                false);
            AddRectangle(asset, "unrelated-solid", 1, 1);
            asset.Operations.Add(CreateSolidify(
                "solidify-unrelated",
                new[] { "unrelated-solid" }));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphereValidationFailed);
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            Assert.That(
                result.SphereReports[0].PanelIds.Count,
                Is.EqualTo(GoreCount));
            Destroy(result, asset);
        }

        [Test]
        public void UnrelatedStitch_DoesNotTriggerSphereValidation()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddRectangle(asset, "patch", 4, 8);
            AddRectangle(asset, "flat-a", 1, 1);
            AddRectangle(asset, "flat-b", 1, 1);
            asset.Operations.Add(CreateWrap(
                "wrap-patch",
                "patch",
                Radius,
                new Vector2(-60f, 60f),
                new Vector2(-30f, 30f),
                SphericalPoleMode.Merge));
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "position-flat-b",
                PanelId = "flat-b",
                Translation = Vector3.right
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "flat-seam",
                A = new BoundaryReference("flat-a", "uMax"),
                B = new BoundaryReference("flat-b", "uMin"),
                Mode = SeamMode.Weld
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "flat-stitch" };
            stitch.SeamIds.Add("flat-seam");
            asset.Operations.Add(stitch);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            Assert.That(
                result.SphereReport.ValidationStage,
                Is.EqualTo(SphereValidationStage.SurfaceSnapshot));
            Destroy(result, asset);
        }

        [Test]
        public void TwoIndependentSphereComponents_AreValidatedIndependently()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddSphereComponent(
                asset,
                "first",
                new Vector3(-1.5f, 0f, 0f),
                true);
            AddSphereComponent(
                asset,
                "second",
                new Vector3(1.5f, 0f, 0f),
                true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReports.Count, Is.EqualTo(2));
            Assert.That(
                result.SphereReports[0].ComponentId,
                Is.EqualTo("sphere-component-000"));
            Assert.That(
                result.SphereReports[1].ComponentId,
                Is.EqualTo("sphere-component-001"));
            Assert.That(result.SphereReports[0].IsClosedSphere, Is.True);
            Assert.That(result.SphereReports[1].IsClosedSphere, Is.True);
            Assert.That(
                result.SphereReports[0].PanelIds[0],
                Does.StartWith("first-"));
            Assert.That(
                result.SphereReports[1].PanelIds[0],
                Does.StartWith("second-"));
            Destroy(result, asset);
        }

        [Test]
        public void ClosedSphere_WithUnrelatedSolidify_StillPasses()
        {
            FoldCanvasAsset asset = CreateSphereAsset(
                "closed",
                Vector3.zero,
                true);
            AddRectangle(asset, "unrelated-solid", 1, 1);
            asset.Operations.Add(CreateSolidify(
                "solidify-unrelated",
                new[] { "unrelated-solid" }));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            Assert.That(result.SphereReport.IsClosedSphere, Is.True);
            Assert.That(result.SphereReport.IsPreSolidify, Is.True);
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.EqualTo(1));
            Destroy(result, asset);
        }

        [Test]
        public void SphereReport_PreservesPreSolidifyEvidence()
        {
            FoldCanvasAsset asset = CreateSphereAsset(
                "closed",
                Vector3.zero,
                true);
            asset.Operations.Add(CreateSolidify(
                "solidify-sphere",
                GetPanelIds("closed")));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            FoldCanvasSphereReport report = result.SphereReport;
            Assert.That(report.IsPreSolidify, Is.True);
            Assert.That(
                report.ValidationOperationId,
                Is.EqualTo("closed-stitch"));
            Assert.That(report.PanelIds.Count, Is.EqualTo(GoreCount));
            Assert.That(
                report.SphericalWrapOperationIds.Count,
                Is.EqualTo(GoreCount));
            Assert.That(report.MaximumRadiusError, Is.EqualTo(0d));
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.EqualTo(1));
            Destroy(result, asset);
        }

        [Test]
        public void SolidifyBeforeRequiredSphereStitches_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSphereAsset(
                "ordered",
                Vector3.zero,
                true);
            FoldOperationDefinition stitch =
                asset.Operations[asset.Operations.Count - 1];
            asset.Operations.RemoveAt(asset.Operations.Count - 1);
            asset.Operations.Add(CreateSolidify(
                "early-solidify",
                GetPanelIds("ordered")));
            asset.Operations.Add(stitch);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .SphereValidationRequiredBeforeSolidify);
            Assert.That(
                result.Diagnostics[0].OperationId,
                Is.EqualTo("early-solidify"));
            Assert.That(result.SphereReports.Count, Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void OneClosedSphereAndOneOpenSphere_FailsOnlyOpenComponent()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddSphereComponent(
                asset,
                "closed",
                new Vector3(-1.5f, 0f, 0f),
                true);
            AddSphereComponent(
                asset,
                "open",
                new Vector3(1.5f, 0f, 0f),
                false);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphereValidationFailed);
            Assert.That(result.SphereReports.Count, Is.EqualTo(2));
            Assert.That(result.SphereReports[0].IsClosedSphere, Is.True);
            Assert.That(result.SphereReports[1].IsClosedSphere, Is.False);
            Assert.That(
                result.Diagnostics[0].PanelId,
                Does.StartWith("open-"));
            Destroy(result, asset);
        }

        [Test]
        public void GoldenSphere_ThreeCompilesHaveStableCountsReportsAndHash()
        {
            FoldCanvasAsset asset =
                SphereCompilerTests.CreateGoldenSphereAsset();
            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult third =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            Assert.That(third.Success, Is.True, JoinDiagnostics(third));
            AssertGoldenCounts(first);
            AssertGoldenCounts(second);
            AssertGoldenCounts(third);
            ulong firstHash = ComputeDeterministicHash(first);
            Assert.That(
                ComputeDeterministicHash(second),
                Is.EqualTo(firstHash));
            Assert.That(
                ComputeDeterministicHash(third),
                Is.EqualTo(firstHash));
            Destroy(first, null);
            Destroy(second, null);
            Destroy(third, asset);
        }

        [Test]
        public void Stitch_SampleCountBeyondBudget_ReturnsDiagnosticWithoutMutation()
        {
            FoldCanvasAsset asset = CreateUnequalStitchAsset(16);
            MeshBuildBuffer buffer = BuildPanelBuffer(
                asset,
                14,
                100);
            int vertexCount = buffer.Vertices.Count;
            int triangleCount = buffer.Triangles.Count;
            int boundaryCount = buffer.GetPanelAt(0)
                .TryGetBoundary(
                    "uMax",
                    out BoundaryBuildRecord boundary)
                    ? boundary.VertexIndices.Count
                    : -1;
            FoldCanvasCompileResult result =
                new FoldCanvasCompileResult();

            bool success = StitchExecutor.TryExecute(
                (StitchOperationDefinition)asset.Operations[
                    asset.Operations.Count - 1],
                asset,
                buffer,
                result,
                out _);

            Assert.That(success, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded));
            Assert.That(buffer.Vertices.Count, Is.EqualTo(vertexCount));
            Assert.That(buffer.Triangles.Count, Is.EqualTo(triangleCount));
            Assert.That(
                boundary.VertexIndices.Count,
                Is.EqualTo(boundaryCount));
            Assert.That(buffer.PanelCount, Is.EqualTo(2));
            Destroy(null, asset);
        }

        [Test]
        [Timeout(15000)]
        public void Stitch_MaximumSampleCount_UsesBatchedBoundarySubdivision()
        {
            FoldCanvasAsset asset = CreateUnequalStitchAsset(
                FoldCanvasLimits.MaximumStitchSampleCount);
            asset.Seams[0].B =
                new BoundaryReference("b", "uMin");
            asset.Operations.Insert(
                0,
                new RigidTransformOperationDefinition
                {
                    Id = "place-b",
                    PanelId = "b",
                    Translation = Vector3.right,
                    Scale = Vector3.one
                });

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(
                result.CompiledData.GetPanel("a")
                    .GetBoundary("uMax").VertexIndices.Count,
                Is.GreaterThanOrEqualTo(
                    FoldCanvasLimits.MaximumStitchSampleCount));
            Assert.That(
                result.CompiledData.GetPanel("b")
                    .GetBoundary("uMin").VertexIndices.Count,
                Is.EqualTo(
                    result.CompiledData.GetPanel("a")
                        .GetBoundary("uMax").VertexIndices.Count));
            Destroy(result, asset);
        }

        [Test]
        public void MultiSeamStitch_BudgetFailureRollsBackEntireOperation()
        {
            FoldCanvasAsset asset =
                CreateTwoUnequalStitchPairs();
            StitchOperationDefinition combined =
                new StitchOperationDefinition
                {
                    Id = "combined-stitch"
                };
            combined.SeamIds.Add("seam-0");
            combined.SeamIds.Add("seam-1");
            asset.Operations.Clear();
            asset.Operations.Add(combined);
            MeshBuildBuffer buffer = BuildPanelBuffer(
                asset,
                31,
                100);
            int beforeVertices = buffer.Vertices.Count;
            int beforeTriangles = buffer.Triangles.Count;
            int beforeBoundaryCount = GetBoundaryCount(
                buffer,
                0,
                "uMax");
            int beforeTopology =
                buffer.GetTopologyId(beforeVertices - 1);
            FoldCanvasCompileResult result =
                new FoldCanvasCompileResult();

            bool success = StitchExecutor.TryExecute(
                combined,
                asset,
                buffer,
                result,
                out _);

            Assert.That(success, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded));
            Assert.That(buffer.Vertices.Count, Is.EqualTo(beforeVertices));
            Assert.That(buffer.Triangles.Count, Is.EqualTo(beforeTriangles));
            Assert.That(buffer.PanelCount, Is.EqualTo(4));
            Assert.That(
                GetBoundaryCount(buffer, 0, "uMax"),
                Is.EqualTo(beforeBoundaryCount));
            Assert.That(
                buffer.GetTopologyId(beforeVertices - 1),
                Is.EqualTo(beforeTopology));
            Assert.That(buffer.HasTopologyWelds, Is.False);
            Assert.That(
                buffer.UsedGeneratedVertices,
                Is.EqualTo(beforeVertices));
            Assert.That(
                buffer.UsedGeneratedTriangles,
                Is.EqualTo(beforeTriangles / 3));
            Destroy(null, asset);
        }

        [Test]
        public void MultipleStitches_RespectCumulativeGeneratedLimits()
        {
            FoldCanvasAsset asset =
                CreateTwoUnequalStitchPairs();
            asset.CompileSettings.MaxGeneratedVertices = 31;
            asset.CompileSettings.MaxGeneratedTriangles = 100;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .GeneratedVertexLimitExceeded);
            AssertDiagnosticValue(
                result.Diagnostics[0],
                "currentUsed",
                31d);
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_RespectsGeneratedVertexLimit()
        {
            FoldCanvasAsset asset = CreateSolidifyRectangleAsset();
            asset.CompileSettings.MaxGeneratedVertices = 23;
            asset.CompileSettings.MaxGeneratedTriangles = 100;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .GeneratedVertexLimitExceeded);
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_RespectsGeneratedTriangleLimit()
        {
            FoldCanvasAsset asset = CreateSolidifyRectangleAsset();
            asset.CompileSettings.MaxGeneratedVertices = 100;
            asset.CompileSettings.MaxGeneratedTriangles = 11;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .GeneratedTriangleLimitExceeded);
            Destroy(result, asset);
        }

        [Test]
        public void PanelAndStitch_ShareTheSameCumulativeBudget()
        {
            FoldCanvasAsset asset = CreateUnequalStitchAsset(0);
            asset.CompileSettings.MaxGeneratedVertices = 14;
            asset.CompileSettings.MaxGeneratedTriangles = 100;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .GeneratedVertexLimitExceeded);
            AssertDiagnosticValue(
                result.Diagnostics[0],
                "currentUsed",
                14d);
            Destroy(result, asset);
        }

        [Test]
        public void MeshBuildBuffer_HardLimitCannotBeBypassed()
        {
            MeshBuildBuffer buffer = new MeshBuildBuffer(1, 1);

            Assert.That(
                buffer.TryAddVertex(
                    Vector3.zero,
                    Vector2.zero,
                    Vector2.zero,
                    0,
                    out int vertexIndex,
                    out FoldCanvasDiagnostic firstVertexDiagnostic),
                Is.True);
            Assert.That(vertexIndex, Is.Zero);
            Assert.That(firstVertexDiagnostic, Is.Null);
            Assert.That(
                buffer.TryAddVertex(
                    Vector3.one,
                    Vector2.one,
                    Vector2.one,
                    0,
                    out _,
                    out FoldCanvasDiagnostic vertexDiagnostic),
                Is.False);
            Assert.That(
                vertexDiagnostic.Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded));
            Assert.That(
                buffer.TryAddTriangle(
                    0,
                    0,
                    0,
                    out FoldCanvasDiagnostic firstTriangleDiagnostic),
                Is.True);
            Assert.That(firstTriangleDiagnostic, Is.Null);
            Assert.That(
                buffer.TryAddTriangle(
                    0,
                    0,
                    0,
                    out FoldCanvasDiagnostic triangleDiagnostic),
                Is.False);
            Assert.That(
                triangleDiagnostic.Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeneratedTriangleLimitExceeded));
            Assert.That(buffer.Vertices.Count, Is.EqualTo(1));
            Assert.That(buffer.Triangles.Count, Is.EqualTo(3));
        }

        [Test]
        public void NativeAsset_SampleCountAboveMaximum_IsRejected()
        {
            FoldCanvasAsset asset = CreateUnequalStitchAsset(
                FoldCanvasLimits.MaximumStitchSampleCount + 1);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .StitchSampleCountOutOfRange);
            Destroy(result, asset);
        }

        [Test]
        public void NativeAsset_NegativeSampleCount_IsRejected()
        {
            FoldCanvasAsset asset = CreateUnequalStitchAsset(-1);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes
                    .StitchSampleCountMismatch);
            Assert.That(
                result.Diagnostics[0].OperationId,
                Is.EqualTo("stitch"));
            Destroy(result, asset);
        }

        [Test]
        public void BudgetArithmeticOverflow_ReturnsStableDiagnostic()
        {
            GeometryBudget budget = new GeometryBudget(10, 10);

            bool success = budget.TryReserve(
                long.MaxValue,
                long.MaxValue,
                "overflow",
                "Test",
                out _,
                out FoldCanvasDiagnostic diagnostic);

            Assert.That(success, Is.False);
            Assert.That(
                diagnostic.Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeometryBudgetOverflow));
            Assert.That(budget.UsedVertices, Is.Zero);
            Assert.That(budget.UsedTriangles, Is.Zero);
        }

        [Test]
        public void BudgetFailure_DoesNotLeavePartialMeshMutation()
        {
            FoldCanvasAsset asset = CreateUnequalStitchAsset(0);
            MeshBuildBuffer buffer = BuildPanelBuffer(
                asset,
                14,
                10);
            int beforeVertices = buffer.Vertices.Count;
            int beforeTriangles = buffer.Triangles.Count;
            int beforePanelCount = buffer.PanelCount;
            int beforeTopology =
                buffer.GetTopologyId(beforeVertices - 1);
            int beforeBoundaryCount = GetBoundaryCount(
                buffer,
                0,
                "uMax");
            FoldCanvasCompileResult result =
                new FoldCanvasCompileResult();

            bool reserved = buffer.TryBeginGeometryOperation(
                1,
                1,
                "over-budget",
                "Test",
                result,
                out _);

            Assert.That(reserved, Is.False);
            Assert.That(buffer.Vertices.Count, Is.EqualTo(beforeVertices));
            Assert.That(buffer.Triangles.Count, Is.EqualTo(beforeTriangles));
            Assert.That(buffer.PanelCount, Is.EqualTo(beforePanelCount));
            Assert.That(
                buffer.GetTopologyId(beforeVertices - 1),
                Is.EqualTo(beforeTopology));
            Assert.That(
                GetBoundaryCount(buffer, 0, "uMax"),
                Is.EqualTo(beforeBoundaryCount));
            Assert.That(
                buffer.UsedGeneratedVertices,
                Is.EqualTo(beforeVertices));
            Assert.That(
                buffer.UsedGeneratedTriangles,
                Is.EqualTo(beforeTriangles / 3));
            Destroy(null, asset);
        }

        private static void AssertSphericalWrapInvalidBlankPanelId(
            string panelId)
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                panelId,
                Radius,
                new Vector2(-45f, 45f),
                new Vector2(-30f, 30f),
                SphericalPoleMode.Merge);

            FoldCanvasCompileResult result = null;
            Assert.DoesNotThrow(
                () => result = FoldCanvasCompiler.Compile(asset));

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphericalWrapTargetMissing);
            Destroy(result, asset);
        }

        [Test]
        public void SphericalWrap_NullPanelId_ReturnsStableDiagnostic()
        {
            AssertSphericalWrapInvalidBlankPanelId(
                null);
        }

        [Test]
        public void SphericalWrap_EmptyPanelId_ReturnsStableDiagnostic()
        {
            AssertSphericalWrapInvalidBlankPanelId(
                string.Empty);
        }

        [Test]
        public void SphericalWrap_WhitespacePanelId_ReturnsStableDiagnostic()
        {
            AssertSphericalWrapInvalidBlankPanelId(
                "   ");
        }

        [Test]
        public void SphericalWrap_MissingPanelId_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                "missing",
                Radius,
                new Vector2(-45f, 45f),
                new Vector2(-30f, 30f),
                SphericalPoleMode.Merge);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphericalWrapTargetMissing);
            Destroy(result, asset);
        }

        [Test]
        public void MeshBuildBuffer_NullPanelLookup_DoesNotThrow()
        {
            MeshBuildBuffer buffer = new MeshBuildBuffer();

            bool found = true;
            Assert.DoesNotThrow(
                () => found = buffer.TryGetPanel(null, out _));

            Assert.That(found, Is.False);
        }

        [Test]
        public void InvalidNativeAsset_DoesNotThrowUnhandledException()
        {
            FoldCanvasAsset asset = CreateSinglePatchAsset(
                null,
                Radius,
                new Vector2(-45f, 45f),
                new Vector2(-30f, 30f),
                SphericalPoleMode.Merge);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-invalid"
                };
            solidify.PanelIds.Add(null);
            asset.Operations.Add(solidify);

            FoldCanvasCompileResult result = null;
            Assert.DoesNotThrow(
                () => result = FoldCanvasCompiler.Compile(asset));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.GreaterThan(0));
            Destroy(result, asset);
        }

        [Test]
        public void PoleTolerance_ScalesWithRadius()
        {
            const float latitude = 89.99995f;

            Assert.That(
                SphericalWrapExecutor.IsPole(
                    latitude,
                    1f,
                    1e-5f),
                Is.True);
            Assert.That(
                SphericalWrapExecutor.IsPole(
                    latitude,
                    1e9f,
                    1e-5f),
                Is.False);
        }

        [Test]
        public void LargeRadius_NearPoleRing_IsNotCollapsedIncorrectly()
        {
            FoldCanvasAsset asset = CreateLargeNearPoleAsset(
                SphericalPoleMode.Merge);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledSphericalSurface surface =
                result.CompiledData.SphericalSurfaces[0];
            FoldCanvasCompiledBoundary north =
                result.CompiledData.GetPanel("patch")
                    .GetBoundary("vMax");
            Assert.That(surface.HasNorthPole, Is.False);
            Assert.That(north.VertexIndices.Count, Is.EqualTo(5));
            Destroy(result, asset);
        }

        [Test]
        public void ExactPositivePole_IsRecognized()
        {
            Assert.That(
                SphericalWrapExecutor.IsPole(
                    90f,
                    1e9f,
                    1e-5f),
                Is.True);
        }

        [Test]
        public void ExactNegativePole_IsRecognized()
        {
            Assert.That(
                SphericalWrapExecutor.IsPole(
                    -90f,
                    1e9f,
                    1e-5f),
                Is.True);
        }

        [Test]
        public void SmallRadius_PoleTolerance_RemainsStable()
        {
            Assert.That(
                SphericalWrapExecutor.IsPole(
                    89.9999f,
                    1e-5f,
                    1e-5f),
                Is.True);
        }

        [Test]
        public void PoleMerge_DoesNotMergeSpatiallyDistantRing()
        {
            FoldCanvasAsset asset = CreateLargeNearPoleAsset(
                SphericalPoleMode.Merge);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledBoundary north =
                result.CompiledData.GetPanel("patch")
                    .GetBoundary("vMax");
            HashSet<int> topology = new HashSet<int>();
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < north.VertexIndices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[
                        north.VertexIndices[i]];
                topology.Add(vertex.TopologyVertexId);
                positions.Add(vertex.Position);
            }

            Assert.That(topology.Count, Is.EqualTo(5));
            Assert.That(
                Vector3.Distance(
                    positions[0],
                    positions[positions.Count - 1]),
                Is.GreaterThan(100f));
            Destroy(result, asset);
        }

        [Test]
        public void PoleKeepFan_DoesNotMisclassifyLargeRadiusRing()
        {
            FoldCanvasAsset asset = CreateLargeNearPoleAsset(
                SphericalPoleMode.KeepFan);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledSphericalSurface surface =
                result.CompiledData.SphericalSurfaces[0];
            FoldCanvasCompiledBoundary north =
                result.CompiledData.GetPanel("patch")
                    .GetBoundary("vMax");
            HashSet<int> topology = new HashSet<int>();
            for (int i = 0; i < north.VertexIndices.Count; i++)
            {
                topology.Add(
                    result.CompiledData.Vertices[
                        north.VertexIndices[i]]
                        .TopologyVertexId);
            }

            Assert.That(surface.HasNorthPole, Is.False);
            Assert.That(topology.Count, Is.EqualTo(5));
            Destroy(result, asset);
        }

        private static FoldCanvasAsset CreateSphereAsset(
            string prefix,
            Vector3 center,
            bool close)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddSphereComponent(asset, prefix, center, close);
            return asset;
        }

        private static void AddSphereComponent(
            FoldCanvasAsset asset,
            string prefix,
            Vector3 center,
            bool close)
        {
            float goreWidth =
                Radius * Mathf.PI * 2f / GoreCount;
            float goreHeight = Radius * Mathf.PI;
            for (int i = 0; i < GoreCount; i++)
            {
                string panelId = $"{prefix}-gore-{i:00}";
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    panelId,
                    new Rect(
                        i / (float)GoreCount,
                        0f,
                        1f / GoreCount,
                        1f),
                    new Vector2(goreWidth, goreHeight),
                    4,
                    16));
                if (center != Vector3.zero)
                {
                    asset.Operations.Add(
                        new RigidTransformOperationDefinition
                        {
                            Id = $"{prefix}-position-{i:00}",
                            PanelId = panelId,
                            Translation = center
                        });
                }

                asset.Operations.Add(CreateWrap(
                    $"{prefix}-wrap-{i:00}",
                    panelId,
                    Radius,
                    new Vector2(-90f, 90f),
                    new Vector2(
                        -180f + i * 45f,
                        -180f + (i + 1) * 45f),
                    SphericalPoleMode.Merge));
            }

            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = $"{prefix}-stitch"
                };
            int seamCount = close ? GoreCount : GoreCount - 1;
            for (int i = 0; i < seamCount; i++)
            {
                string seamId = $"{prefix}-seam-{i:00}";
                asset.Seams.Add(new SeamDefinition
                {
                    Id = seamId,
                    A = new BoundaryReference(
                        $"{prefix}-gore-{i:00}",
                        "uMax"),
                    B = new BoundaryReference(
                        $"{prefix}-gore-{(i + 1) % GoreCount:00}",
                        "uMin"),
                    Mode = SeamMode.Weld
                });
                stitch.SeamIds.Add(seamId);
            }

            asset.Operations.Add(stitch);
        }

        private static FoldCanvasAsset CreateSinglePatchAsset(
            string targetPanelId,
            float radius,
            Vector2 latitudeRange,
            Vector2 longitudeRange,
            SphericalPoleMode poleMode)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddRectangle(asset, "patch", 4, 8);
            asset.Operations.Add(CreateWrap(
                "wrap",
                targetPanelId,
                radius,
                latitudeRange,
                longitudeRange,
                poleMode));
            return asset;
        }

        private static FoldCanvasAsset CreateLargeNearPoleAsset(
            SphericalPoleMode poleMode)
        {
            return CreateSinglePatchAsset(
                "patch",
                1e9f,
                new Vector2(-45f, 89.99995f),
                new Vector2(-30f, 30f),
                poleMode);
        }

        private static SphericalWrapOperationDefinition CreateWrap(
            string operationId,
            string panelId,
            float radius,
            Vector2 latitudeRange,
            Vector2 longitudeRange,
            SphericalPoleMode poleMode)
        {
            return new SphericalWrapOperationDefinition
            {
                Id = operationId,
                PanelId = panelId,
                Radius = radius,
                LatitudeRange = latitudeRange,
                LongitudeRange = longitudeRange,
                WrapDirection =
                    SphericalWrapDirection.LongitudeAlongU,
                PoleMode = poleMode,
                SubdivisionMode =
                    SphericalSubdivisionMode.PanelGrid
            };
        }

        private static FoldCanvasAsset CreateUnequalStitchAsset(
            int sampleCount)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddRectangle(asset, "a", 1, 1);
            AddRectangle(asset, "b", 1, 4);
            asset.Seams.Add(new SeamDefinition
            {
                Id = "seam",
                A = new BoundaryReference("a", "uMax"),
                B = new BoundaryReference("b", "uMax"),
                Mode = SeamMode.Weld,
                SampleCount = sampleCount
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("seam");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static FoldCanvasAsset CreateTwoUnequalStitchPairs()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddRectangle(asset, "a0", 1, 1);
            AddRectangle(asset, "b0", 1, 4);
            AddRectangle(asset, "a1", 1, 1);
            AddRectangle(asset, "b1", 1, 4);
            for (int pair = 0; pair < 2; pair++)
            {
                string seamId = $"seam-{pair}";
                asset.Seams.Add(new SeamDefinition
                {
                    Id = seamId,
                    A = new BoundaryReference(
                        $"a{pair}",
                        "uMax"),
                    B = new BoundaryReference(
                        $"b{pair}",
                        "uMax"),
                    Mode = SeamMode.Weld
                });
                StitchOperationDefinition stitch =
                    new StitchOperationDefinition
                    {
                        Id = $"stitch-{pair}"
                    };
                stitch.SeamIds.Add(seamId);
                asset.Operations.Add(stitch);
            }

            return asset;
        }

        private static FoldCanvasAsset CreateSolidifyRectangleAsset()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddRectangle(asset, "panel", 1, 1);
            asset.Operations.Add(CreateSolidify(
                "solidify",
                new[] { "panel" }));
            return asset;
        }

        private static SolidifyOperationDefinition CreateSolidify(
            string operationId,
            IReadOnlyList<string> panelIds)
        {
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = operationId,
                    Thickness = 0.01f,
                    Direction = SolidifyDirection.Outward
                };
            for (int i = 0; i < panelIds.Count; i++)
            {
                solidify.PanelIds.Add(panelIds[i]);
            }

            return solidify;
        }

        private static string[] GetPanelIds(string prefix)
        {
            string[] ids = new string[GoreCount];
            for (int i = 0; i < GoreCount; i++)
            {
                ids[i] = $"{prefix}-gore-{i:00}";
            }

            return ids;
        }

        private static void AddRectangle(
            FoldCanvasAsset asset,
            string panelId,
            int uSegments,
            int vSegments)
        {
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                panelId,
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                uSegments,
                vSegments));
        }

        private static MeshBuildBuffer BuildPanelBuffer(
            FoldCanvasAsset asset,
            int maxVertices,
            int maxTriangles)
        {
            MeshBuildBuffer buffer = new MeshBuildBuffer(
                maxVertices,
                maxTriangles);
            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                FoldCanvasSourceValidator.TryEstimateGeometry(
                    panel,
                    out long vertices,
                    out long triangles);
                FoldCanvasCompileResult reservationResult =
                    new FoldCanvasCompileResult();
                Assert.That(
                    buffer.TryBeginGeometryOperation(
                        vertices,
                        triangles,
                        panel.Id,
                        "PanelTessellation",
                        reservationResult,
                        out GeometryOperationScope scope),
                    Is.True,
                    JoinDiagnostics(reservationResult));
                using (scope)
                {
                    PanelTessellator.Append(panel, buffer, null);
                }
            }

            return buffer;
        }

        private static int GetBoundaryCount(
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

        private static void AssertGoldenCounts(
            FoldCanvasCompileResult result)
        {
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(616));
            Assert.That(
                result.CompiledData.TopologyVertexCount,
                Is.EqualTo(482));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(960));
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            Assert.That(
                result.SphereReport.SphericalPanelCount,
                Is.EqualTo(GoreCount));
            Assert.That(
                result.SphereReport.SurfaceRenderVertexCount,
                Is.EqualTo(616));
            Assert.That(
                result.SphereReport.SurfaceTopologyVertexCount,
                Is.EqualTo(482));
            Assert.That(
                result.SphereReport.TriangleCount,
                Is.EqualTo(960));
            Assert.That(result.SphereReport.UniqueEdgeCount, Is.EqualTo(1440));
            Assert.That(
                result.SphereReport.ConnectedComponentCount,
                Is.EqualTo(1));
            Assert.That(
                result.SphereReport.EulerCharacteristic,
                Is.EqualTo(2));
            Assert.That(result.SphereReport.OpenEdgeCount, Is.Zero);
            Assert.That(result.SphereReport.NonManifoldEdgeCount, Is.Zero);
            Assert.That(
                result.SphereReport.OrientationConflictEdgeCount,
                Is.Zero);
            Assert.That(result.SphereReport.InwardTriangleCount, Is.Zero);
            Assert.That(
                result.SphereReport.MaximumRadiusError,
                Is.EqualTo(0d));
            Assert.That(
                result.SphereReport.NorthPoleTopologyCount,
                Is.EqualTo(1));
            Assert.That(
                result.SphereReport.SouthPoleTopologyCount,
                Is.EqualTo(1));
            Assert.That(result.SphereReport.IsPreSolidify, Is.True);
            Assert.That(
                result.SphereReport.ComponentId,
                Is.EqualTo("sphere-component-000"));
        }

        private static ulong ComputeDeterministicHash(
            FoldCanvasCompileResult result)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0;
                i < result.CompiledData.Vertices.Count;
                i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[i];
                AddHash(ref hash, FloatBits(vertex.Position.x), prime);
                AddHash(ref hash, FloatBits(vertex.Position.y), prime);
                AddHash(ref hash, FloatBits(vertex.Position.z), prime);
                AddHash(ref hash, FloatBits(vertex.SourcePosition.x), prime);
                AddHash(ref hash, FloatBits(vertex.SourcePosition.y), prime);
                AddHash(ref hash, FloatBits(vertex.SourceUv.x), prime);
                AddHash(ref hash, FloatBits(vertex.SourceUv.y), prime);
                AddHash(ref hash, vertex.PanelIndex, prime);
                AddHash(ref hash, vertex.ProvenanceId, prime);
                AddHash(ref hash, vertex.TopologyVertexId, prime);
            }

            for (int i = 0;
                i < result.CompiledData.TriangleIndices.Count;
                i++)
            {
                AddHash(
                    ref hash,
                    result.CompiledData.TriangleIndices[i],
                    prime);
            }

            for (int reportIndex = 0;
                reportIndex < result.SphereReports.Count;
                reportIndex++)
            {
                FoldCanvasSphereReport report =
                    result.SphereReports[reportIndex];
                AddHash(ref hash, report.ComponentId, prime);
                AddHash(
                    ref hash,
                    (int)report.ValidationStage,
                    prime);
                AddHash(
                    ref hash,
                    report.EulerCharacteristic,
                    prime);
                AddHash(
                    ref hash,
                    BitConverter.DoubleToInt64Bits(
                        report.MaximumRadiusError),
                    prime);
            }

            return hash;
        }

        private static int FloatBits(float value)
        {
            return BitConverter.ToInt32(
                BitConverter.GetBytes(value),
                0);
        }

        private static void AddHash(
            ref ulong hash,
            long value,
            ulong prime)
        {
            unchecked
            {
                hash ^= (ulong)value;
                hash *= prime;
            }
        }

        private static void AddHash(
            ref ulong hash,
            string value,
            ulong prime)
        {
            for (int i = 0; i < value.Length; i++)
            {
                AddHash(ref hash, value[i], prime);
            }
        }

        private static void AssertDiagnostic(
            FoldCanvasCompileResult result,
            string code)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.GreaterThan(0));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        }

        private static void AssertDiagnosticValue(
            FoldCanvasDiagnostic diagnostic,
            string key,
            double expected)
        {
            for (int i = 0; i < diagnostic.Values.Count; i++)
            {
                if (diagnostic.Values[i].Key == key)
                {
                    Assert.That(
                        diagnostic.Values[i].Value,
                        Is.EqualTo(expected));
                    return;
                }
            }

            Assert.Fail($"Missing diagnostic value '{key}'.");
        }

        private static string JoinDiagnostics(
            FoldCanvasCompileResult result)
        {
            List<string> diagnostics = new List<string>();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                diagnostics.Add(result.Diagnostics[i].ToString());
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
