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

        [Test]
        public void ZeroDegreeOffGridFold_IsIdentityWithoutTopologyChurn()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "off-grid-identity",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                0f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData.Vertices, Has.Count.EqualTo(4));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(2));
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[i];
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

        [Test]
        public void Fold_OffGridCrease_SplitsTrianglesAndFoldsWithoutStretch()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "off-grid",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(7));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(6));

            int bottomCrease = FindVertexBySourcePosition(
                result,
                new Vector2(-0.2f, -0.5f));
            int diagonalCrease = FindVertexBySourcePosition(
                result,
                new Vector2(-0.2f, -0.2f));
            int topCrease = FindVertexBySourcePosition(
                result,
                new Vector2(-0.2f, 0.5f));
            Assert.That(bottomCrease, Is.GreaterThanOrEqualTo(0));
            Assert.That(diagonalCrease, Is.GreaterThanOrEqualTo(0));
            Assert.That(topCrease, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                Vector3.Distance(
                    result.CompiledData.Vertices[bottomCrease].Position,
                    new Vector3(-0.2f, -0.5f, 0f)),
                Is.LessThanOrEqualTo(PositionTolerance));
            Assert.That(
                Vector3.Distance(
                    result.CompiledData.Vertices[diagonalCrease].Position,
                    new Vector3(-0.2f, -0.2f, 0f)),
                Is.LessThanOrEqualTo(PositionTolerance));
            Assert.That(
                Vector3.Distance(
                    result.CompiledData.Vertices[topCrease].Position,
                    new Vector3(-0.2f, 0.5f, 0f)),
                Is.LessThanOrEqualTo(PositionTolerance));

            for (int offset = 0;
                offset < result.CompiledData.TriangleIndices.Count;
                offset += 3)
            {
                bool hasLeft = false;
                bool hasRight = false;
                for (int corner = 0; corner < 3; corner++)
                {
                    FoldCanvasCompiledVertex vertex =
                        result.CompiledData.Vertices[
                            result.CompiledData.TriangleIndices[
                                offset + corner]];
                    hasLeft |= vertex.SourcePosition.x <
                        -0.2f - PositionTolerance;
                    hasRight |= vertex.SourcePosition.x >
                        -0.2f + PositionTolerance;
                }

                Assert.That(
                    hasLeft && hasRight,
                    Is.False,
                    $"Triangle {offset / 3} spans the crease.");
            }

            FoldCanvasCompiledPanel panel =
                result.CompiledData.Panels[0];
            CollectionAssert.AreEqual(
                new[] { 0, bottomCrease, 1 },
                panel.GetBoundary("vMin").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 2, topCrease, 3 },
                panel.GetBoundary("vMax").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 0, 2 },
                panel.GetBoundary("uMin").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 1, 3 },
                panel.GetBoundary("uMax").VertexIndices);

            Destroy(result, asset);
        }

        [Test]
        public void Fold_ReversedOffGridCrease_PreservesNamedBoundaryOrder()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "reversed",
                "panel",
                new Vector2(0.3f, 1f),
                new Vector2(0.3f, 0f),
                FoldSide.Negative,
                -90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            int bottomCrease = FindVertexBySourcePosition(
                result,
                new Vector2(-0.2f, -0.5f));
            int topCrease = FindVertexBySourcePosition(
                result,
                new Vector2(-0.2f, 0.5f));
            FoldCanvasCompiledPanel panel = result.CompiledData.Panels[0];
            CollectionAssert.AreEqual(
                new[] { 0, bottomCrease, 1 },
                panel.GetBoundary("vMin").VertexIndices);
            CollectionAssert.AreEqual(
                new[] { 2, topCrease, 3 },
                panel.GetBoundary("vMax").VertexIndices);

            Destroy(result, asset);
        }

        [Test]
        public void Fold_OffGridCrease_PreservesInterpolatedSourceUv()
        {
            FoldCanvasAsset asset = ScriptableObject
                .CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0.1f, 0.2f, 0.6f, 0.4f),
                new Vector2(2f, 4f),
                1,
                1));
            asset.Operations.Add(CreateFold(
                "uv",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[i];
                if (Mathf.Abs(vertex.SourcePosition.x + 0.4f) >
                    PositionTolerance)
                {
                    continue;
                }

                Assert.That(
                    vertex.SourceUv.x,
                    Is.EqualTo(0.28f).Within(PositionTolerance));
                float normalizedV =
                    vertex.SourcePosition.y / 4f + 0.5f;
                Assert.That(
                    vertex.SourceUv.y,
                    Is.EqualTo(0.2f + normalizedV * 0.4f)
                        .Within(PositionTolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void Fold_OffGridCrease_AfterRigidTransformUsesCurrentFrame()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place",
                PanelId = "panel",
                Translation = new Vector3(2f, 3f, 4f),
                RotationEuler = new Vector3(15f, 30f, 45f),
                Scale = Vector3.one
            });
            asset.Operations.Add(CreateFold(
                "off-grid-current-frame",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Quaternion placement = Quaternion.Euler(15f, 30f, 45f);
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    result.CompiledData.Vertices[i];
                if (Mathf.Abs(vertex.SourcePosition.x + 0.2f) >
                    PositionTolerance)
                {
                    continue;
                }

                Vector3 expected = placement * new Vector3(
                    vertex.SourcePosition.x,
                    vertex.SourcePosition.y,
                    0f) + new Vector3(2f, 3f, 4f);
                AssertVectorWithin(vertex.Position, expected);
            }

            Destroy(result, asset);
        }

        [Test]
        public void Fold_OffGridCrease_RepeatedCompileIsDeterministic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "deterministic",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Negative,
                -67f));

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            CollectionAssert.AreEqual(
                first.CompiledData.Vertices,
                second.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            for (int i = 0; i < first.CompiledData.Panels[0].Boundaries.Count; i++)
            {
                CollectionAssert.AreEqual(
                    first.CompiledData.Panels[0].Boundaries[i]
                        .VertexIndices,
                    second.CompiledData.Panels[0].Boundaries[i]
                        .VertexIndices);
            }

            Object.DestroyImmediate(first.Mesh);
            Object.DestroyImmediate(second.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Fold_AngledOffGridCrease_HasNoSpanningOrZeroAreaTriangle()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            Vector2 lineStart = new Vector2(0f, 0.2f);
            Vector2 lineEnd = new Vector2(1f, 0.8f);
            asset.Operations.Add(CreateFold(
                "angled",
                "panel",
                lineStart,
                lineEnd,
                FoldSide.Positive,
                53f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData.Vertices, Has.Count.EqualTo(7));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(6));
            AssertSourceTrianglesRespectCrease(result, lineStart, lineEnd);
            Destroy(result, asset);
        }

        [Test]
        public void Fold_MultipleParallelOffGridCreases_RefineInAuthoredOrder()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "first",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                25f));
            asset.Operations.Add(CreateFold(
                "second",
                "panel",
                new Vector2(0.7f, 0f),
                new Vector2(0.7f, 1f),
                FoldSide.Positive,
                -40f));

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            Assert.That(first.CompiledData.Vertices, Has.Count.EqualTo(11));
            Assert.That(
                first.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(12));
            AssertSourceTrianglesRespectCrease(
                first,
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f));
            AssertSourceTrianglesRespectCrease(
                first,
                new Vector2(0.7f, 0f),
                new Vector2(0.7f, 1f));
            CollectionAssert.AreEqual(
                first.CompiledData.Vertices,
                second.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);

            Object.DestroyImmediate(first.Mesh);
            Object.DestroyImmediate(second.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Fold_InteriorEndingCrease_ReturnsRequiresTopologySplitDiagnostic()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Operations.Add(CreateFold(
                "interior-ending",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 0.7f),
                FoldSide.Positive,
                90f));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.FoldCreaseRequiresTopologySplit,
                "interior-ending");
        }

        [Test]
        public void Fold_OffGridDiskCrease_ReturnsRequiresTopologySplitDiagnostic()
        {
            FoldCanvasAsset asset = ScriptableObject
                .CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "disk",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                16,
                2));
            asset.Operations.Add(CreateFold(
                "disk-crease",
                "disk",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            AssertSingleDiagnostic(
                asset,
                FoldCanvasDiagnosticCodes.FoldCreaseRequiresTopologySplit,
                "disk-crease");
        }

        [Test]
        public void Fold_OffGridCrease_UsesRefinedGeometryBudget()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.CompileSettings.MaxGeneratedVertices = 6;
            asset.CompileSettings.MaxGeneratedTriangles = 6;
            asset.Operations.Add(CreateFold(
                "budget",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("panel"));

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Fold_OffGridCrease_UsesRefinedTriangleBudget()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.CompileSettings.MaxGeneratedVertices = 7;
            asset.CompileSettings.MaxGeneratedTriangles = 5;
            asset.Operations.Add(CreateFold(
                "triangle-budget",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .GeneratedTriangleLimitExceeded));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("panel"));

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Fold_OffGridCrease_PreservesLaterPanelRanges()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "later",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));
            asset.Operations.Add(CreateFold(
                "range",
                "panel",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                90f));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData.Panels[0].VertexStart, Is.Zero);
            Assert.That(result.CompiledData.Panels[0].VertexCount,
                Is.EqualTo(7));
            Assert.That(result.CompiledData.Panels[1].VertexStart,
                Is.EqualTo(7));
            Assert.That(result.CompiledData.Panels[1].VertexCount,
                Is.EqualTo(4));
            for (int i = 7; i < 11; i++)
            {
                Assert.That(
                    result.CompiledData.Vertices[i].PanelIndex,
                    Is.EqualTo(1));
            }

            Destroy(result, asset);
        }

        [Test]
        public void OffGridFoldBoundaries_StitchThenSolidifyIntoClosedVolume()
        {
            FoldCanvasAsset asset = CreatePanelAsset(1, 1);
            asset.Panels[0].Id = "lower";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "upper",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "refined-join",
                A = new BoundaryReference("lower", "vMax"),
                B = new BoundaryReference("upper", "vMin"),
                Mode = SeamMode.Weld,
                ReverseB = false,
                SampleCount = 3
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-upper",
                PanelId = "upper",
                Translation = Vector3.up,
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });
            asset.Operations.Add(CreateFold(
                "fold-lower",
                "lower",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                45f));
            asset.Operations.Add(CreateFold(
                "fold-upper",
                "upper",
                new Vector2(0.3f, 0f),
                new Vector2(0.3f, 1f),
                FoldSide.Positive,
                45f));
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch-refined" };
            stitch.SeamIds.Add("refined-join");
            asset.Operations.Add(stitch);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-refined",
                    Thickness = 0.02f,
                    Direction = SolidifyDirection.Centered
                };
            solidify.PanelIds.Add("lower");
            solidify.PanelIds.Add("upper");
            asset.Operations.Add(solidify);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledBoundary lower = result.CompiledData
                .GetPanel("lower").GetBoundary("vMax");
            FoldCanvasCompiledBoundary upper = result.CompiledData
                .GetPanel("upper").GetBoundary("vMin");
            // The authored crease contributes the 0.3 breakpoint while the
            // Stitch sample count contributes 0.5. Neither may be discarded.
            Assert.That(lower.VertexIndices, Has.Count.EqualTo(4));
            Assert.That(upper.VertexIndices, Has.Count.EqualTo(4));
            bool foundRefinedBreakpoint = false;
            for (int i = 0; i < lower.VertexIndices.Count; i++)
            {
                FoldCanvasCompiledVertex a = result.CompiledData.Vertices[
                    lower.VertexIndices[i]];
                FoldCanvasCompiledVertex b = result.CompiledData.Vertices[
                    upper.VertexIndices[i]];
                foundRefinedBreakpoint |= Mathf.Abs(
                    a.SourcePosition.x + 0.2f) <= PositionTolerance;
                Assert.That(a.TopologyVertexId, Is.EqualTo(b.TopologyVertexId));
                Assert.That(
                    Vector3.Distance(a.Position, b.Position),
                    Is.LessThanOrEqualTo(PositionTolerance));
            }
            Assert.That(foundRefinedBreakpoint, Is.True);

            Assert.That(
                result.SolidifyClosedVolumeReports,
                Has.Count.EqualTo(1));
            Assert.That(
                result.SolidifyClosedVolumeReports[0].IsClosedVolume,
                Is.True);
            Assert.That(
                result.SolidifyClosedVolumeReports[0].OpenEdgeCount,
                Is.Zero);

            Destroy(result, asset);
        }

        private static int FindVertexBySourcePosition(
            FoldCanvasCompileResult result,
            Vector2 sourcePosition)
        {
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                if (Vector2.Distance(
                        result.CompiledData.Vertices[i].SourcePosition,
                        sourcePosition) <= PositionTolerance)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void AssertSourceTrianglesRespectCrease(
            FoldCanvasCompileResult result,
            Vector2 lineStart,
            Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            for (int offset = 0;
                offset < result.CompiledData.TriangleIndices.Count;
                offset += 3)
            {
                Vector2 a = ToNormalizedSource(
                    result.CompiledData.Vertices[
                        result.CompiledData.TriangleIndices[offset]]);
                Vector2 b = ToNormalizedSource(
                    result.CompiledData.Vertices[
                        result.CompiledData.TriangleIndices[offset + 1]]);
                Vector2 c = ToNormalizedSource(
                    result.CompiledData.Vertices[
                        result.CompiledData.TriangleIndices[offset + 2]]);
                float area = Cross2(b - a, c - a);
                Assert.That(
                    Mathf.Abs(area),
                    Is.GreaterThan(1e-7f),
                    $"Triangle {offset / 3} has zero source area.");
                float da = Cross2(line, a - lineStart);
                float db = Cross2(line, b - lineStart);
                float dc = Cross2(line, c - lineStart);
                bool positive = da > PositionTolerance ||
                    db > PositionTolerance ||
                    dc > PositionTolerance;
                bool negative = da < -PositionTolerance ||
                    db < -PositionTolerance ||
                    dc < -PositionTolerance;
                Assert.That(
                    positive && negative,
                    Is.False,
                    $"Triangle {offset / 3} spans the crease.");
            }
        }

        private static Vector2 ToNormalizedSource(
            FoldCanvasCompiledVertex vertex)
        {
            return vertex.SourcePosition + Vector2.one * 0.5f;
        }

        private static float Cross2(Vector2 left, Vector2 right)
        {
            return left.x * right.y - left.y * right.x;
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
