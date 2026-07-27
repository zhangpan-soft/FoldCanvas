using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class FoldLineCompilerTests
    {
        private const float PositionTolerance = 1e-5f;

        [Test]
        public void ZeroDegreeFold_IsExactIdentity()
        {
            FoldCanvasAsset asset = CreatePanelAsset(2, 2);
            asset.Operations.Add(CreateFold(
                "identity",
                "panel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                FoldSide.Negative,
                0f));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = result.CompiledData.Vertices[i];
                Assert.That(
                    vertex.Position,
                    Is.EqualTo(new Vector3(
                        vertex.SourcePosition.x,
                        vertex.SourcePosition.y,
                        0f)));
            }

            Destroy(result, asset);
        }

        [TestCase(90f, 0.5f)]
        [TestCase(-90f, -0.5f)]
        public void SignedNinetyDegreeFold_UsesDirectedAxisHandedness(
            float angle,
            float expectedZ)
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 2);
            asset.Operations.Add(CreateFold(
                "signed",
                "panel",
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                FoldSide.Positive,
                angle));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Vector3 movedTopLeft = result.CompiledData.Vertices[4].Position;
            Assert.That(movedTopLeft.x, Is.EqualTo(-0.5f).Within(PositionTolerance));
            Assert.That(movedTopLeft.y, Is.EqualTo(0f).Within(PositionTolerance));
            Assert.That(movedTopLeft.z, Is.EqualTo(expectedZ).Within(PositionTolerance));

            Destroy(result, asset);
        }

        [Test]
        public void Fold_KeepsHingeFixedAndPreservesAxisDistance()
        {
            FoldCanvasAsset asset = CreatePanelAsset(2, 2);
            asset.Operations.Add(CreateFold(
                "preserve-distance",
                "panel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                FoldSide.Negative,
                37f));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            int[] hingeIndices = { 1, 4, 7 };
            for (int i = 0; i < hingeIndices.Length; i++)
            {
                int index = hingeIndices[i];
                Vector2 source = result.CompiledData.Vertices[index].SourcePosition;
                Assert.That(
                    result.CompiledData.Vertices[index].Position,
                    Is.EqualTo(new Vector3(source.x, source.y, 0f)));
            }

            Vector3 axisPoint = new Vector3(0f, -0.5f, 0f);
            Vector3 axisDirection = Vector3.up;
            int[] selectedIndices = { 2, 5, 8 };
            for (int i = 0; i < selectedIndices.Length; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[selectedIndices[i]];
                float before = DistanceToAxis(
                    new Vector3(
                        vertex.SourcePosition.x,
                        vertex.SourcePosition.y,
                        0f),
                    axisPoint,
                    axisDirection);
                float after = DistanceToAxis(
                    vertex.Position,
                    axisPoint,
                    axisDirection);
                Assert.That(after, Is.EqualTo(before).Within(PositionTolerance));
            }

            int[] unselectedIndices = { 0, 3, 6 };
            for (int i = 0; i < unselectedIndices.Length; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[unselectedIndices[i]];
                Assert.That(
                    vertex.Position,
                    Is.EqualTo(new Vector3(
                        vertex.SourcePosition.x,
                        vertex.SourcePosition.y,
                        0f)));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Fold_PreservesSourceUvProvenanceTopologyAndBoundaries()
        {
            FoldCanvasAsset asset = CreatePanelAsset(2, 2);
            FoldCanvasCompileResult planar = FoldCanvasCompiler.Compile(asset);
            asset.Operations.Add(CreateFold(
                "preserve-source",
                "panel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                FoldSide.Positive,
                -65f));
            FoldCanvasCompileResult folded = FoldCanvasCompiler.Compile(asset);

            Assert.That(planar.Success, Is.True, JoinDiagnostics(planar));
            Assert.That(folded.Success, Is.True, JoinDiagnostics(folded));
            Assert.That(
                folded.CompiledData.Vertices.Count,
                Is.EqualTo(planar.CompiledData.Vertices.Count));
            for (int i = 0; i < planar.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex before = planar.CompiledData.Vertices[i];
                FoldCanvasCompiledVertex after = folded.CompiledData.Vertices[i];
                Assert.That(after.SourcePosition, Is.EqualTo(before.SourcePosition));
                Assert.That(after.SourceUv, Is.EqualTo(before.SourceUv));
                Assert.That(after.PanelIndex, Is.EqualTo(before.PanelIndex));
                Assert.That(after.ProvenanceId, Is.EqualTo(before.ProvenanceId));
            }

            CollectionAssert.AreEqual(
                planar.CompiledData.TriangleIndices,
                folded.CompiledData.TriangleIndices);
            FoldCanvasCompiledPanel planarPanel = planar.CompiledData.Panels[0];
            FoldCanvasCompiledPanel foldedPanel = folded.CompiledData.Panels[0];
            Assert.That(
                foldedPanel.Boundaries.Count,
                Is.EqualTo(planarPanel.Boundaries.Count));
            for (int i = 0; i < planarPanel.Boundaries.Count; i++)
            {
                Assert.That(
                    foldedPanel.Boundaries[i].Id,
                    Is.EqualTo(planarPanel.Boundaries[i].Id));
                CollectionAssert.AreEqual(
                    planarPanel.Boundaries[i].VertexIndices,
                    foldedPanel.Boundaries[i].VertexIndices);
            }

            Object.DestroyImmediate(planar.Mesh);
            Object.DestroyImmediate(folded.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void OrderedFold_ResolvesLaterHingeFromCurrentEmbedding()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "raise-panel",
                "panel",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                FoldSide.Positive,
                90f));
            asset.Operations.Add(CreateFold(
                "turn-around-raised-edge",
                "panel",
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                FoldSide.Negative,
                90f));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Vector3 topRight = result.CompiledData.Vertices[3].Position;
            Assert.That(topRight.x, Is.EqualTo(-0.5f).Within(PositionTolerance));
            Assert.That(topRight.y, Is.EqualTo(0.5f).Within(PositionTolerance));
            Assert.That(topRight.z, Is.EqualTo(1f).Within(PositionTolerance));

            Destroy(result, asset);
        }

        [Test]
        public void FoldLineCrossingPriorCrease_ReturnsAmbiguousHingeDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(2, 2);
            asset.Operations.Add(CreateFold(
                "make-crease",
                "panel",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                FoldSide.Negative,
                90f));
            asset.Operations.Add(CreateFold(
                "cross-crease",
                "panel",
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.AmbiguousFoldHinge));
            Assert.That(result.Diagnostics[0].OperationId, Is.EqualTo("cross-crease"));

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void BoxSequence_ClosesBoundsWithSixOutwardFacesAndExactUvs()
        {
            FoldCanvasAsset asset = CreateBoxAsset();

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData.Panels.Count, Is.EqualTo(6));
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(24));
            Assert.That(result.CompiledData.TriangleIndices.Count / 3, Is.EqualTo(12));

            Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                Vector3 position = result.CompiledData.Vertices[i].Position;
                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }

            AssertVectorWithin(minimum, new Vector3(-0.5f, -0.5f, -1f));
            AssertVectorWithin(maximum, new Vector3(0.5f, 0.5f, 0f));

            Vector3[] expectedNormals =
            {
                Vector3.forward,
                Vector3.back,
                Vector3.down,
                Vector3.up,
                Vector3.left,
                Vector3.right
            };
            for (int panelIndex = 0; panelIndex < expectedNormals.Length; panelIndex++)
            {
                int triangleStart = panelIndex * 6;
                int ia = result.CompiledData.TriangleIndices[triangleStart];
                int ib = result.CompiledData.TriangleIndices[triangleStart + 1];
                int ic = result.CompiledData.TriangleIndices[triangleStart + 2];
                Vector3 a = result.CompiledData.Vertices[ia].Position;
                Vector3 b = result.CompiledData.Vertices[ib].Position;
                Vector3 c = result.CompiledData.Vertices[ic].Position;
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                Assert.That(
                    Vector3.Dot(normal, expectedNormals[panelIndex]),
                    Is.GreaterThan(0.9999f),
                    $"Panel {result.CompiledData.Panels[panelIndex].Id} has the wrong outward normal.");

                FoldCanvasCompiledPanel panel =
                    result.CompiledData.Panels[panelIndex];
                Rect rect = panel.CanvasRect;
                int start = panel.VertexStart;
                AssertVectorWithin(
                    result.CompiledData.Vertices[start].SourceUv,
                    new Vector2(rect.xMin, rect.yMin));
                AssertVectorWithin(
                    result.CompiledData.Vertices[start + 1].SourceUv,
                    new Vector2(rect.xMax, rect.yMin));
                AssertVectorWithin(
                    result.CompiledData.Vertices[start + 2].SourceUv,
                    new Vector2(rect.xMin, rect.yMax));
                AssertVectorWithin(
                    result.CompiledData.Vertices[start + 3].SourceUv,
                    new Vector2(rect.xMax, rect.yMax));
            }

            Destroy(result, asset);
        }

        [Test]
        public void BoxSequence_RepeatedCompileIsDeterministic()
        {
            FoldCanvasAsset asset = CreateBoxAsset();

            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            CollectionAssert.AreEqual(
                first.CompiledData.Vertices,
                second.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            CollectionAssert.AreEqual(first.Mesh.vertices, second.Mesh.vertices);
            CollectionAssert.AreEqual(first.Mesh.triangles, second.Mesh.triangles);
            CollectionAssert.AreEqual(first.Mesh.uv, second.Mesh.uv);

            Object.DestroyImmediate(first.Mesh);
            Object.DestroyImmediate(second.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void MissingFoldTarget_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "missing",
                "not-there",
                Vector2.zero,
                Vector2.right,
                FoldSide.Positive,
                90f));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.FoldTargetMissing,
                "missing");
        }

        [Test]
        public void NonFiniteFoldLine_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "non-finite-line",
                "panel",
                new Vector2(float.NaN, 0f),
                Vector2.right,
                FoldSide.Positive,
                90f));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.NonFiniteFoldLine,
                "non-finite-line");
        }

        [Test]
        public void OutOfRangeFoldLine_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "out-of-range",
                "panel",
                new Vector2(-0.1f, 0f),
                Vector2.right,
                FoldSide.Positive,
                90f));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.FoldLineOutOfRange,
                "out-of-range");
        }

        [Test]
        public void DegenerateFoldLine_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "degenerate",
                "panel",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                FoldSide.Positive,
                90f));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.DegenerateFoldLine,
                "degenerate");
        }

        [Test]
        public void NonFiniteFoldAngle_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "non-finite-angle",
                "panel",
                Vector2.zero,
                Vector2.right,
                FoldSide.Positive,
                float.PositiveInfinity));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.NonFiniteFoldAngle,
                "non-finite-angle");
        }

        [Test]
        public void NonzeroFalloff_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            FoldLineOperationDefinition fold = CreateFold(
                "falloff",
                "panel",
                Vector2.zero,
                Vector2.right,
                FoldSide.Positive,
                90f);
            fold.Falloff = 0.25f;
            asset.Operations.Add(fold);

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.UnsupportedFoldFalloff,
                "falloff");
        }

        [Test]
        public void InvalidFoldSide_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            FoldLineOperationDefinition fold = CreateFold(
                "invalid-side",
                "panel",
                Vector2.zero,
                Vector2.right,
                FoldSide.Positive,
                90f);
            fold.Side = (FoldSide)99;
            asset.Operations.Add(fold);

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.InvalidFoldSide,
                "invalid-side");
        }

        private static FoldCanvasAsset CreatePanelAsset(int uSegments, int vSegments)
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "FoldTest";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                uSegments,
                vSegments));
            return asset;
        }

        private static FoldCanvasAsset CreateBoxAsset()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M02BoxTest";
            const float third = 1f / 3f;
            asset.Panels.Add(CreateFace("top", new Rect(0f, 0.5f, third, 0.5f)));
            asset.Panels.Add(CreateFace("bottom", new Rect(third, 0.5f, third, 0.5f)));
            asset.Panels.Add(CreateFace("front", new Rect(third * 2f, 0.5f, third, 0.5f)));
            asset.Panels.Add(CreateFace("back", new Rect(0f, 0f, third, 0.5f)));
            asset.Panels.Add(CreateFace("left", new Rect(third, 0f, third, 0.5f)));
            asset.Panels.Add(CreateFace("right", new Rect(third * 2f, 0f, third, 0.5f)));

            AddLayout(asset, "front", new Vector3(0f, -1f, 0f));
            AddLayout(asset, "back", new Vector3(0f, 1f, 0f));
            AddLayout(asset, "left", new Vector3(-1f, 0f, 0f));
            AddLayout(asset, "right", new Vector3(1f, 0f, 0f));
            asset.Operations.Add(CreateFold(
                "fold-front", "front", new Vector2(0f, 1f), new Vector2(1f, 1f), FoldSide.Negative, 90f));
            asset.Operations.Add(CreateFold(
                "fold-back", "back", new Vector2(0f, 0f), new Vector2(1f, 0f), FoldSide.Positive, -90f));
            asset.Operations.Add(CreateFold(
                "fold-left", "left", new Vector2(1f, 0f), new Vector2(1f, 1f), FoldSide.Positive, -90f));
            asset.Operations.Add(CreateFold(
                "fold-right", "right", new Vector2(0f, 0f), new Vector2(0f, 1f), FoldSide.Negative, 90f));
            asset.Operations.Add(CreateFold(
                "fold-bottom-down", "bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), FoldSide.Positive, -90f));
            asset.Operations.Add(CreateFold(
                "fold-bottom-close", "bottom", new Vector2(0f, 1f), new Vector2(1f, 1f), FoldSide.Negative, -90f));
            return asset;
        }

        private static PanelDefinition CreateFace(string id, Rect canvasRect)
        {
            return PanelDefinition.CreateRectangle(
                id,
                canvasRect,
                Vector2.one,
                1,
                1);
        }

        private static void AddLayout(
            FoldCanvasAsset asset,
            string panelId,
            Vector3 translation)
        {
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "layout-" + panelId,
                PanelId = panelId,
                Translation = translation,
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });
        }

        private static FoldLineOperationDefinition CreateFold(
            string id,
            string panelId,
            Vector2 lineStart,
            Vector2 lineEnd,
            FoldSide side,
            float angle)
        {
            return new FoldLineOperationDefinition
            {
                Id = id,
                PanelId = panelId,
                LineStart = lineStart,
                LineEnd = lineEnd,
                Side = side,
                AngleDegrees = angle,
                Falloff = 0f
            };
        }

        private static void AssertSingleDiagnostic(
            FoldCanvasAsset asset,
            string expectedCode,
            string expectedOperationId)
        {
            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expectedCode));
            Assert.That(
                result.Diagnostics[0].OperationId,
                Is.EqualTo(expectedOperationId));

            Object.DestroyImmediate(asset);
        }

        private static float DistanceToAxis(
            Vector3 point,
            Vector3 axisPoint,
            Vector3 axisDirection)
        {
            return Vector3.Cross(
                axisDirection.normalized,
                point - axisPoint).magnitude;
        }

        private static void AssertVectorWithin(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(PositionTolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(PositionTolerance));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(PositionTolerance));
        }

        private static void AssertVectorWithin(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(PositionTolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(PositionTolerance));
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset)
        {
            Object.DestroyImmediate(result.Mesh);
            Object.DestroyImmediate(asset);
        }

        private static string JoinDiagnostics(FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }
    }
}
