using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class SourceValidationTests
    {
        [Test]
        public void EmptyAndDuplicatePanelIds_HaveDistinctStableCodes()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                string.Empty,
                new Rect(0f, 0f, 0.25f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "same",
                new Rect(0.25f, 0f, 0.25f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "same",
                new Rect(0.5f, 0f, 0.25f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.EmptyPanelId));
            Assert.That(
                result.Diagnostics[1].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.DuplicatePanelId));
            Assert.That(result.Diagnostics[1].PanelId, Is.EqualTo("same"));
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void DuplicatePanelId_FailsWithStableCode()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "same",
                new Rect(0f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "same",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(ContainsCode(result, FoldCanvasDiagnosticCodes.DuplicatePanelId), Is.True);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void UnsupportedPanelShape_ReturnsDiagnosticInsteadOfThrowing()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            PanelDefinition panel = PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1);
            panel.Shape = (PanelShape)999;
            asset.Panels.Add(panel);

            FoldCanvasCompileResult result = null;
            Assert.DoesNotThrow(() => result = FoldCanvasCompiler.Compile(asset));

            Assert.That(result.Success, Is.False);
            Assert.That(
                ContainsCode(result, FoldCanvasDiagnosticCodes.UnsupportedPanelShape),
                Is.True);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void NonFinitePanelSizes_ReturnStableDiagnostics()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "nan",
                new Rect(0f, 0f, 0.5f, 1f),
                new Vector2(float.NaN, 1f),
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "infinity",
                new Rect(0.5f, 0f, 0.5f, 1f),
                new Vector2(1f, float.PositiveInfinity),
                8,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.NonFinitePanelSize));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("nan"));
            Assert.That(
                result.Diagnostics[1].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.NonFinitePanelSize));
            Assert.That(result.Diagnostics[1].PanelId, Is.EqualTo("infinity"));
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void NonPositivePanelSizes_ReturnStableDiagnostics()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "zero",
                new Rect(0f, 0f, 0.5f, 1f),
                new Vector2(0f, 1f),
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "negative",
                new Rect(0.5f, 0f, 0.5f, 1f),
                new Vector2(1f, -1f),
                8,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.NonPositivePanelSize));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("zero"));
            Assert.That(
                result.Diagnostics[1].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.NonPositivePanelSize));
            Assert.That(result.Diagnostics[1].PanelId, Is.EqualTo("negative"));
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void NonFiniteCanvasRect_IsRejected()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, float.NaN, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(
                ContainsCode(result, FoldCanvasDiagnosticCodes.CanvasRectOutOfRange),
                Is.True);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void BelowMinimumTessellation_ReturnsStableDiagnostics()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "rectangle",
                new Rect(0f, 0f, 0.5f, 1f),
                Vector2.one,
                0,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "disk",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                2,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidTessellation));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("rectangle"));
            Assert.That(
                result.Diagnostics[1].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidTessellation));
            Assert.That(result.Diagnostics[1].PanelId, Is.EqualTo("disk"));
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void CumulativeTessellationLimit_FailsBeforeGeometryAllocation()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.CompileSettings.MaxGeneratedVertices = 7;
            asset.CompileSettings.MaxGeneratedTriangles = 4;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "first",
                new Rect(0f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "second",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.ExcessiveTessellation));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("second"));
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void InvalidCompileLimits_ReturnDiagnosticWithoutClamping()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.CompileSettings.MaxGeneratedVertices = 0;
            asset.CompileSettings.MaxGeneratedTriangles = -1;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidCompileLimits));
            Assert.That(
                ContainsCode(result, FoldCanvasDiagnosticCodes.ExcessiveTessellation),
                Is.True);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void ExtremeTessellation_ReturnsDiagnosticWithoutOverflow()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "extreme",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                int.MaxValue,
                int.MaxValue));

            FoldCanvasCompileResult result = null;
            Assert.DoesNotThrow(() => result = FoldCanvasCompiler.Compile(asset));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.ExcessiveTessellation));
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Object.DestroyImmediate(asset);
        }

        [TestCase(FoldOperationType.Stitch)]
        [TestCase(FoldOperationType.Solidify)]
        public void UnsupportedOperation_IsNeverSilentlyIgnored(FoldOperationType operationType)
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                4,
                4));
            asset.Operations.Add(CreateUnsupportedOperation(operationType));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(ContainsCode(result, FoldCanvasDiagnosticCodes.UnsupportedOperation), Is.True);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].OperationId, Is.EqualTo(operationType.ToString()));
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void DeclaredSeam_WithoutStitch_DoesNotFail()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                2,
                2));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "declared-only",
                A = new BoundaryReference("panel", "uMin"),
                B = new BoundaryReference("panel", "uMax"),
                Mode = SeamMode.KeepOpen
            });

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, DiagnosticSignature(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void UnsupportedStitch_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                2,
                2));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "declared",
                A = new BoundaryReference("panel", "uMin"),
                B = new BoundaryReference("panel", "uMax"),
                Mode = SeamMode.Weld
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("declared");
            asset.Operations.Add(stitch);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.UnsupportedOperation));
            Assert.That(result.Diagnostics[0].OperationId, Is.EqualTo("stitch"));
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void InvalidCanvasRect_FailsBeforeMeshCreation()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0.8f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(ContainsCode(result, FoldCanvasDiagnosticCodes.CanvasRectOutOfRange), Is.True);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void RepeatedCompile_ProducesStableDiagnosticOrderAndContext()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "invalid",
                new Rect(0.75f, 0f, 0.5f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "invalid",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(asset);

            Assert.That(DiagnosticSignature(first), Is.EqualTo(DiagnosticSignature(second)));
            Assert.That(
                DiagnosticSignature(first),
                Is.EqualTo("FC1003|invalid||\nFC0002|invalid||\n"));
            Object.DestroyImmediate(asset);
        }

        private static FoldOperationDefinition CreateUnsupportedOperation(
            FoldOperationType operationType)
        {
            switch (operationType)
            {
                case FoldOperationType.Fold:
                    return new FoldLineOperationDefinition
                    {
                        Id = operationType.ToString(),
                        PanelId = "panel"
                    };
                case FoldOperationType.Roll:
                    return new RollOperationDefinition
                    {
                        Id = operationType.ToString(),
                        PanelId = "panel"
                    };
                case FoldOperationType.Stitch:
                    return new StitchOperationDefinition
                    {
                        Id = operationType.ToString()
                    };
                case FoldOperationType.Solidify:
                    return new SolidifyOperationDefinition
                    {
                        Id = operationType.ToString()
                    };
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(operationType),
                        operationType,
                        null);
            }
        }

        private static string DiagnosticSignature(FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                builder.Append(diagnostic.Code);
                builder.Append('|');
                builder.Append(diagnostic.PanelId);
                builder.Append('|');
                builder.Append(diagnostic.OperationId);
                builder.Append('|');
                builder.Append(diagnostic.SeamId);
                builder.Append('\n');
            }

            return builder.ToString();
        }

        private static bool ContainsCode(FoldCanvasCompileResult result, string code)
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
    }
}
