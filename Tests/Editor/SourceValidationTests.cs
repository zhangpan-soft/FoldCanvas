using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class SourceValidationTests
    {
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
            Object.DestroyImmediate(asset);
        }

        [TestCase(FoldOperationType.Fold)]
        [TestCase(FoldOperationType.Roll)]
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
