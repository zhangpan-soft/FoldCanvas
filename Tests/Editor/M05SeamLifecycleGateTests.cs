using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M05SeamLifecycleGateTests
    {
        private const int GoreCount = 8;
        private const float Radius = 0.5f;

        [Test]
        public void SelectedSeam_DefaultBoundaryReference_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].A = default;

            Assert.DoesNotThrow(() => SphereValidationPlan.Build(asset));
            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.MissingPanelReference);
        }

        [Test]
        public void SelectedSeam_NullPanelReference_DoesNotThrow()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].A = new BoundaryReference(null, "uMax");

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.MissingPanelReference);
        }

        [Test]
        public void SelectedSeam_EmptyPanelReference_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].B = new BoundaryReference(string.Empty, "uMin");

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.MissingPanelReference);
        }

        [Test]
        public void SelectedSeam_WhitespacePanelReference_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].A = new BoundaryReference("   ", "uMax");

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.MissingPanelReference);
        }

        [Test]
        public void SelectedSeam_NullBoundaryId_DoesNotThrow()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].B = new BoundaryReference("flat", null);

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.StitchBoundaryMissing);
        }

        [Test]
        public void SelectedSeam_MissingPanel_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].A =
                new BoundaryReference("missing-panel", "uMax");

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.MissingPanelReference);
        }

        [Test]
        public void SelectedSeam_MissingBoundary_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].B =
                new BoundaryReference("flat", "missing-boundary");

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.StitchBoundaryMissing);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SelectedSeam_InvalidSeamId_ReturnsStableDiagnostic(
            string seamId)
        {
            FoldCanvasAsset asset = CreateSelectedSeamAsset();
            asset.Seams[0].Id = seamId;
            StitchOperationDefinition stitch =
                (StitchOperationDefinition)asset.Operations[
                    asset.Operations.Count - 1];
            stitch.SeamIds.Clear();
            stitch.SeamIds.Add(seamId);

            AssertStableDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.StitchSeamMissing);
        }

        [Test]
        public void SphereComponent_LaterCrossTypeBridge_IsRejectedOrRevalidated()
        {
            FoldCanvasAsset asset = CreateCrossTypeAsset(
                SeamMode.Bridge,
                16,
                false);
            int expectedIndex = asset.Operations.Count - 1;

            FoldCanvasCompileResult result = CompileWithoutThrow(asset);

            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            AssertFreshReport(
                result.SphereReport,
                "cross-type-stitch",
                expectedIndex);
            if (!result.Success)
            {
                Assert.That(result.Diagnostics, Is.Not.Empty);
            }

            Destroy(result, asset);
        }

        [Test]
        public void SphereComponent_LaterCrossTypeWeld_DoesNotLeaveStaleReport()
        {
            FoldCanvasAsset asset = CreateCrossTypeAsset(
                SeamMode.Weld,
                16,
                false);

            FoldCanvasCompileResult result = CompileWithoutThrow(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.SphereReports, Is.Empty);
            AssertContainsDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.StitchPositionMismatch);
            Destroy(result, asset);
        }

        [Test]
        public void SphereReport_ValidationOperationIndex_IsLastTouchingStitch()
        {
            FoldCanvasAsset asset = CreateCrossTypeAsset(
                SeamMode.Bridge,
                16,
                false);
            int expectedIndex = asset.Operations.Count - 1;

            FoldCanvasCompileResult result = CompileWithoutThrow(asset);

            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            AssertFreshReport(
                result.SphereReport,
                "cross-type-stitch",
                expectedIndex);
            Destroy(result, asset);
        }

        [Test]
        public void PostValidationBoundarySubdivision_CannotReturnClosedStaleReport()
        {
            FoldCanvasAsset asset = CreateCrossTypeAsset(
                SeamMode.Bridge,
                32,
                false);
            int expectedIndex = asset.Operations.Count - 1;

            FoldCanvasCompileResult result = CompileWithoutThrow(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            AssertFreshReport(
                result.SphereReport,
                "cross-type-stitch",
                expectedIndex);
            Assert.That(result.SphereReport.IsClosedSphere, Is.False);
            Assert.That(result.SphereReport.OpenEdgeCount, Is.GreaterThan(0));
            AssertContainsDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphereValidationFailed);
            Destroy(result, asset);
        }

        [Test]
        public void Solidify_AfterCrossTypeStitch_UsesFreshSphereEvidence()
        {
            FoldCanvasAsset asset = CreateCrossTypeAsset(
                SeamMode.Bridge,
                32,
                true);
            int expectedIndex = asset.Operations.Count - 2;

            FoldCanvasCompileResult result = CompileWithoutThrow(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            AssertFreshReport(
                result.SphereReport,
                "cross-type-stitch",
                expectedIndex);
            Assert.That(result.SphereReport.IsClosedSphere, Is.False);
            Assert.That(
                result.SolidifyClosedVolumeReports,
                Is.Empty);
            AssertContainsDiagnostic(
                result,
                FoldCanvasDiagnosticCodes.SphereValidationFailed);
            Destroy(result, asset);
        }

        private static FoldCanvasAsset CreateSelectedSeamAsset()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddRectangle(asset, "sphere", 4, 8);
            AddRectangle(asset, "flat", 1, 1);
            asset.Operations.Add(CreateWrap(
                "wrap-sphere",
                "sphere",
                new Vector2(-60f, 60f),
                new Vector2(-30f, 30f)));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "selected-seam",
                A = new BoundaryReference("sphere", "uMax"),
                B = new BoundaryReference("flat", "uMin"),
                Mode = SeamMode.Bridge
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "selected-stitch"
                };
            stitch.SeamIds.Add("selected-seam");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static FoldCanvasAsset CreateCrossTypeAsset(
            SeamMode mode,
            int flatVSegments,
            bool addSolidify)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            AddSphereComponent(asset, "sphere");
            AddRectangle(asset, "flat", 1, flatVSegments);
            asset.Seams.Add(new SeamDefinition
            {
                Id = "cross-type-seam",
                A = new BoundaryReference("sphere-gore-00", "uMax"),
                B = new BoundaryReference("flat", "uMin"),
                Mode = mode
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "cross-type-stitch"
                };
            stitch.SeamIds.Add("cross-type-seam");
            asset.Operations.Add(stitch);
            if (addSolidify)
            {
                SolidifyOperationDefinition solidify =
                    new SolidifyOperationDefinition
                    {
                        Id = "solidify-after-cross-type",
                        Thickness = 0.01f,
                        Direction = SolidifyDirection.Outward
                    };
                for (int i = 0; i < GoreCount; i++)
                {
                    solidify.PanelIds.Add($"sphere-gore-{i:00}");
                }

                asset.Operations.Add(solidify);
            }

            return asset;
        }

        private static void AddSphereComponent(
            FoldCanvasAsset asset,
            string prefix)
        {
            float goreWidth =
                Radius * Mathf.PI * 2f / GoreCount;
            float goreHeight = Radius * Mathf.PI;
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = $"{prefix}-stitch"
                };
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
                asset.Operations.Add(CreateWrap(
                    $"{prefix}-wrap-{i:00}",
                    panelId,
                    new Vector2(-90f, 90f),
                    new Vector2(
                        -180f + i * 45f,
                        -180f + (i + 1) * 45f)));

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

        private static SphericalWrapOperationDefinition CreateWrap(
            string operationId,
            string panelId,
            Vector2 latitudeRange,
            Vector2 longitudeRange)
        {
            return new SphericalWrapOperationDefinition
            {
                Id = operationId,
                PanelId = panelId,
                Radius = Radius,
                LatitudeRange = latitudeRange,
                LongitudeRange = longitudeRange,
                WrapDirection =
                    SphericalWrapDirection.LongitudeAlongU,
                PoleMode = SphericalPoleMode.Merge,
                SubdivisionMode =
                    SphericalSubdivisionMode.PanelGrid
            };
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

        private static FoldCanvasCompileResult CompileWithoutThrow(
            FoldCanvasAsset asset)
        {
            FoldCanvasCompileResult result = null;
            Assert.DoesNotThrow(
                () => result = FoldCanvasCompiler.Compile(asset));
            Assert.That(result, Is.Not.Null);
            return result;
        }

        private static void AssertStableDiagnostic(
            FoldCanvasAsset asset,
            string expectedCode)
        {
            FoldCanvasCompileResult first = CompileWithoutThrow(asset);
            FoldCanvasCompileResult second = CompileWithoutThrow(asset);
            Assert.That(first.Success, Is.False);
            Assert.That(first.Mesh, Is.Null);
            Assert.That(first.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                first.Diagnostics[0].Code,
                Is.EqualTo(expectedCode));
            Assert.That(
                second.Diagnostics.Count,
                Is.EqualTo(first.Diagnostics.Count));
            Assert.That(
                second.Diagnostics[0].ToString(),
                Is.EqualTo(first.Diagnostics[0].ToString()));
            Destroy(first, null);
            Destroy(second, asset);
        }

        private static void AssertFreshReport(
            FoldCanvasSphereReport report,
            string operationId,
            int operationIndex)
        {
            Assert.That(report, Is.Not.Null);
            Assert.That(
                report.ComponentId,
                Is.EqualTo("sphere-component-000"));
            Assert.That(
                report.ValidationStage,
                Is.EqualTo(SphereValidationStage.PreSolidify));
            Assert.That(
                report.ValidationOperationId,
                Is.EqualTo(operationId));
            Assert.That(
                report.ValidationOperationIndex,
                Is.EqualTo(operationIndex));
        }

        private static void AssertContainsDiagnostic(
            FoldCanvasCompileResult result,
            string code)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Code == code)
                {
                    return;
                }
            }

            Assert.Fail($"Expected diagnostic '{code}'.");
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset)
        {
            if (result?.Mesh != null)
            {
                Object.DestroyImmediate(result.Mesh);
            }

            if (asset != null)
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
