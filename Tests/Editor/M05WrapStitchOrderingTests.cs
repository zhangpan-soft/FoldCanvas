using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M05WrapStitchOrderingTests
    {
        private const int GoreCount = 8;

        [Test]
        public void ComponentFormingStitch_BeforeWrap_ReturnsOrderingDiagnostic()
        {
            FoldCanvasAsset asset = CreateSphereWithStitchAt(0);
            SphereValidationPlan plan =
                SphereValidationPlan.Build(asset);
            FoldCanvasCompileResult defenseResult =
                new FoldCanvasCompileResult();

            Assert.That(
                plan.TryValidateAfterOperation(
                    0,
                    new MeshBuildBuffer(),
                    defenseResult),
                Is.True);
            Assert.That(defenseResult.SphereReports, Is.Empty);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOrderingFailure(result);
            Destroy(result, asset);
        }

        [Test]
        public void StitchBetweenComponentWraps_ReturnsOrderingDiagnostic()
        {
            FoldCanvasAsset asset = CreateSphereWithStitchAt(4);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOrderingFailure(result);
            Destroy(result, asset);
        }

        [Test]
        public void CrossTypeStitch_BeforeWrap_DoesNotEmitSphereReport()
        {
            FoldCanvasAsset asset =
                SphereCompilerTests.CreateGoldenSphereAsset();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "flat",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                16));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "cross-type-seam",
                A = new BoundaryReference("gore-00", "uMin"),
                B = new BoundaryReference("flat", "uMax"),
                Mode = SeamMode.Bridge
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "cross-type-stitch"
                };
            stitch.SeamIds.Add("cross-type-seam");
            asset.Operations.Insert(0, stitch);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOrderingFailure(result);
            Assert.That(result.SphereReports, Is.Empty);
            Destroy(result, asset);
        }

        [Test]
        public void InvalidWrapStitchOrder_DoesNotReturnSphereValidationFailed()
        {
            FoldCanvasAsset asset = CreateSphereWithStitchAt(0);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            AssertOrderingFailure(result);
            Assert.That(
                ContainsDiagnostic(
                    result,
                    FoldCanvasDiagnosticCodes.SphereValidationFailed),
                Is.False);
            Destroy(result, asset);
        }

        [Test]
        public void InvalidWrapStitchOrder_IsDeterministic()
        {
            FoldCanvasAsset asset = CreateSphereWithStitchAt(4);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            AssertOrderingFailure(first);
            AssertOrderingFailure(second);
            Assert.That(
                second.Diagnostics.Count,
                Is.EqualTo(first.Diagnostics.Count));
            for (int i = 0; i < first.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic firstDiagnostic =
                    first.Diagnostics[i];
                FoldCanvasDiagnostic secondDiagnostic =
                    second.Diagnostics[i];
                Assert.That(
                    secondDiagnostic.ToString(),
                    Is.EqualTo(firstDiagnostic.ToString()));
                Assert.That(
                    secondDiagnostic.Values.Count,
                    Is.EqualTo(firstDiagnostic.Values.Count));
                for (int valueIndex = 0;
                    valueIndex < firstDiagnostic.Values.Count;
                    valueIndex++)
                {
                    Assert.That(
                        secondDiagnostic.Values[valueIndex].Key,
                        Is.EqualTo(
                            firstDiagnostic.Values[valueIndex].Key));
                    Assert.That(
                        secondDiagnostic.Values[valueIndex].Value,
                        Is.EqualTo(
                            firstDiagnostic.Values[valueIndex].Value));
                }
            }

            Destroy(first, null);
            Destroy(second, asset);
        }

        [Test]
        public void ValidWrapThenStitch_StillProducesFreshPreSolidifyReport()
        {
            FoldCanvasAsset asset =
                SphereCompilerTests.CreateGoldenSphereAsset();
            int stitchIndex = asset.Operations.Count - 1;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertFreshSphereReport(result, stitchIndex);
            Destroy(result, asset);
        }

        [Test]
        public void ValidWrapThenStitchThenSolidify_StillPasses()
        {
            FoldCanvasAsset asset =
                SphereCompilerTests.CreateGoldenSphereAsset();
            int stitchIndex = asset.Operations.Count - 1;
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-sphere",
                    Thickness = 0.01f,
                    Direction = SolidifyDirection.Outward
                };
            for (int i = 0; i < GoreCount; i++)
            {
                solidify.PanelIds.Add($"gore-{i:00}");
            }

            asset.Operations.Add(solidify);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertFreshSphereReport(result, stitchIndex);
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.EqualTo(1));
            Assert.That(
                result.SolidifyClosedVolumeReports[0].IsClosedVolume,
                Is.True);
            Destroy(result, asset);
        }

        private static FoldCanvasAsset CreateSphereWithStitchAt(
            int stitchIndex)
        {
            FoldCanvasAsset asset =
                SphereCompilerTests.CreateGoldenSphereAsset();
            int originalIndex = asset.Operations.Count - 1;
            StitchOperationDefinition stitch =
                (StitchOperationDefinition)asset.Operations[
                    originalIndex];
            asset.Operations.RemoveAt(originalIndex);
            asset.Operations.Insert(stitchIndex, stitch);
            return asset;
        }

        private static void AssertOrderingFailure(
            FoldCanvasCompileResult result)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.SphereReports, Is.Empty);
            Assert.That(
                ContainsDiagnostic(
                    result,
                    FoldCanvasDiagnosticCodes
                        .StitchMustBeTerminalForSelectedPanels),
                Is.True,
                JoinDiagnostics(result));
        }

        private static void AssertFreshSphereReport(
            FoldCanvasCompileResult result,
            int stitchIndex)
        {
            Assert.That(result.SphereReports.Count, Is.EqualTo(1));
            Assert.That(
                result.SphereReport.ValidationStage,
                Is.EqualTo(SphereValidationStage.PreSolidify));
            Assert.That(
                result.SphereReport.ValidationOperationId,
                Is.EqualTo("stitch-sphere"));
            Assert.That(
                result.SphereReport.ValidationOperationIndex,
                Is.EqualTo(stitchIndex));
            Assert.That(result.SphereReport.IsClosedSphere, Is.True);
        }

        private static bool ContainsDiagnostic(
            FoldCanvasCompileResult result,
            string code)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        private static string JoinDiagnostics(
            FoldCanvasCompileResult result)
        {
            string joined = string.Empty;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (i > 0)
                {
                    joined += "\n";
                }

                joined += result.Diagnostics[i].ToString();
            }

            return joined;
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
