using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class RollCompilerTests
    {
        private const float PositionTolerance = 1e-5f;

        [TestCase(0f)]
        [TestCase(0.0001f)]
        [TestCase(-0.0001f)]
        public void ZeroOrNearZeroAngle_ReturnsStableDiagnostic(float angle)
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 4, 2);
            asset.Operations.Add(CreateRoll(
                "near-zero",
                "panel",
                RollDirection.U,
                angle,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.NearZeroRollAngle,
                "panel",
                "near-zero");
            Destroy(result, asset);
        }

        [Test]
        public void HalfTurnPreserveArcLength_ProducesExpectedHalfCylinder()
        {
            const float width = 2f;
            float expectedRadius = width / Mathf.PI;
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(width, 1f),
                2,
                1);
            asset.Operations.Add(CreateRoll(
                "half",
                "panel",
                RollDirection.U,
                180f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertPosition(
                result.CompiledData.Vertices[0].Position,
                new Vector3(-expectedRadius, -0.5f, 0f));
            AssertPosition(
                result.CompiledData.Vertices[1].Position,
                new Vector3(0f, -0.5f, expectedRadius));
            AssertPosition(
                result.CompiledData.Vertices[2].Position,
                new Vector3(expectedRadius, -0.5f, 0f));
            Destroy(result, asset);
        }

        [Test]
        public void FullRoll_MinAndMaxBoundariesCoincideButRemainTopologicallySeparate()
        {
            const float expectedRadius = 0.25f;
            const int vSegments = 3;
            float width = 2f * Mathf.PI * expectedRadius;
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(width, 0.75f),
                24,
                vSegments);
            asset.Operations.Add(CreateRoll(
                "full",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledPanel panel = result.CompiledData.Panels[0];
            FoldCanvasCompiledBoundary uMin = panel.GetBoundary("uMin");
            FoldCanvasCompiledBoundary uMax = panel.GetBoundary("uMax");
            float maximumSeamDistance = 0f;
            for (int i = 0; i <= vSegments; i++)
            {
                int firstIndex = uMin.VertexIndices[i];
                int lastIndex = uMax.VertexIndices[i];
                Assert.That(firstIndex, Is.Not.EqualTo(lastIndex));
                Vector3 first =
                    result.CompiledData.Vertices[firstIndex].Position;
                Vector3 last =
                    result.CompiledData.Vertices[lastIndex].Position;
                maximumSeamDistance = Mathf.Max(
                    maximumSeamDistance,
                    Vector3.Distance(first, last));
                Assert.That(
                    Mathf.Sqrt(first.x * first.x + first.z * first.z),
                    Is.EqualTo(expectedRadius).Within(PositionTolerance));
                Assert.That(
                    first.y,
                    Is.EqualTo(
                        result.CompiledData.Vertices[firstIndex]
                            .SourcePosition.y));
            }

            Assert.That(
                maximumSeamDistance,
                Is.LessThanOrEqualTo(PositionTolerance));
            Destroy(result, asset);
        }

        [Test]
        public void PartialRoll_RemainsOpen()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(2f, 1f),
                8,
                2);
            asset.Operations.Add(CreateRoll(
                "open",
                "panel",
                RollDirection.U,
                180f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            FoldCanvasCompiledPanel panel = result.CompiledData.Panels[0];
            Vector3 first = result.CompiledData.Vertices[
                panel.GetBoundary("uMin").VertexIndices[1]].Position;
            Vector3 last = result.CompiledData.Vertices[
                panel.GetBoundary("uMax").VertexIndices[1]].Position;
            Assert.That(
                Vector3.Distance(first, last),
                Is.EqualTo(4f / Mathf.PI).Within(PositionTolerance));
            Destroy(result, asset);
        }

        [Test]
        public void VDirectionAndStartAngle_FollowDocumentedMapping()
        {
            const float expectedRadius = 0.4f;
            float height = Mathf.PI * expectedRadius;
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(0.6f, height),
                1,
                2);
            RollOperationDefinition roll = CreateRoll(
                "v-start",
                "panel",
                RollDirection.V,
                -180f,
                RollRadiusMode.PreserveArcLength);
            roll.StartAngleDegrees = 90f;
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertPosition(
                result.CompiledData.Vertices[0].Position,
                new Vector3(-0.3f, 0f, expectedRadius));
            AssertPosition(
                result.CompiledData.Vertices[2].Position,
                new Vector3(-0.3f, -expectedRadius, 0f));
            AssertPosition(
                result.CompiledData.Vertices[4].Position,
                new Vector3(-0.3f, 0f, -expectedRadius));
            Assert.That(
                result.CompiledData.Vertices[1].Position.x,
                Is.EqualTo(0.3f));
            Destroy(result, asset);
        }

        [Test]
        public void ReversedAngle_ReversesIntermediateRadialOrientation()
        {
            FoldCanvasAsset positiveAsset = CreateRectangleAsset(
                new Vector2(2f, 1f),
                2,
                1);
            positiveAsset.Operations.Add(CreateRoll(
                "positive",
                "panel",
                RollDirection.U,
                180f,
                RollRadiusMode.PreserveArcLength));
            FoldCanvasAsset negativeAsset = CreateRectangleAsset(
                new Vector2(2f, 1f),
                2,
                1);
            negativeAsset.Operations.Add(CreateRoll(
                "negative",
                "panel",
                RollDirection.U,
                -180f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult positive =
                FoldCanvasCompiler.Compile(positiveAsset);
            FoldCanvasCompileResult negative =
                FoldCanvasCompiler.Compile(negativeAsset);

            Assert.That(positive.Success, Is.True, JoinDiagnostics(positive));
            Assert.That(negative.Success, Is.True, JoinDiagnostics(negative));
            Assert.That(
                positive.CompiledData.Vertices[1].Position.z,
                Is.GreaterThan(0f));
            Assert.That(
                negative.CompiledData.Vertices[1].Position.z,
                Is.LessThan(0f));
            Assert.That(
                Mathf.Abs(positive.CompiledData.Vertices[1].Position.z),
                Is.EqualTo(
                    Mathf.Abs(negative.CompiledData.Vertices[1].Position.z))
                    .Within(PositionTolerance));

            Destroy(positive, positiveAsset);
            Destroy(negative, negativeAsset);
        }

        [Test]
        public void PositiveFullRoll_HasOutwardWinding()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(2f * Mathf.PI * 0.25f, 1f),
                16,
                2);
            asset.Operations.Add(CreateRoll(
                "positive-outward",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertRadialMeshNormal(result, 21, 1f);
            Destroy(result, asset);
        }

        [Test]
        public void NegativeFullRoll_ReversesOrientationPredictably()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(2f * Mathf.PI * 0.25f, 1f),
                16,
                2);
            asset.Operations.Add(CreateRoll(
                "negative-inward",
                "panel",
                RollDirection.U,
                -360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertRadialMeshNormal(result, 21, -1f);
            Destroy(result, asset);
        }

        [Test]
        public void RollU_And_RollV_HaveDocumentedHandedness()
        {
            const float radius = 0.25f;
            float span = 2f * Mathf.PI * radius;
            FoldCanvasAsset uAsset = CreateRectangleAsset(
                new Vector2(span, 1f),
                4,
                1);
            uAsset.Operations.Add(CreateRoll(
                "roll-u",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));
            FoldCanvasAsset vAsset = CreateRectangleAsset(
                new Vector2(1f, span),
                1,
                4);
            vAsset.Operations.Add(CreateRoll(
                "roll-v",
                "panel",
                RollDirection.V,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult uResult =
                FoldCanvasCompiler.Compile(uAsset);
            FoldCanvasCompileResult vResult =
                FoldCanvasCompiler.Compile(vAsset);

            Assert.That(uResult.Success, Is.True, JoinDiagnostics(uResult));
            Assert.That(vResult.Success, Is.True, JoinDiagnostics(vResult));
            AssertPosition(
                uResult.CompiledData.Vertices[0].Position,
                new Vector3(-radius, -0.5f, 0f));
            AssertPosition(
                uResult.CompiledData.Vertices[1].Position,
                new Vector3(0f, -0.5f, radius));
            AssertPosition(
                vResult.CompiledData.Vertices[0].Position,
                new Vector3(-0.5f, -radius, 0f));
            AssertPosition(
                vResult.CompiledData.Vertices[2].Position,
                new Vector3(-0.5f, 0f, radius));
            Destroy(uResult, uAsset);
            Destroy(vResult, vAsset);
        }

        [Test]
        public void Roll_PreservesSourceUvProvenanceTopologyAndBoundaries()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(1.5f, 0.8f),
                5,
                3,
                new Rect(0.15f, 0.2f, 0.6f, 0.7f));
            FoldCanvasCompileResult planar = FoldCanvasCompiler.Compile(asset);
            asset.Operations.Add(CreateRoll(
                "preserve",
                "panel",
                RollDirection.U,
                -270f,
                RollRadiusMode.PreserveArcLength));
            FoldCanvasCompileResult rolled = FoldCanvasCompiler.Compile(asset);

            Assert.That(planar.Success, Is.True, JoinDiagnostics(planar));
            Assert.That(rolled.Success, Is.True, JoinDiagnostics(rolled));
            Assert.That(
                rolled.CompiledData.Vertices.Count,
                Is.EqualTo(planar.CompiledData.Vertices.Count));
            for (int i = 0; i < planar.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex before =
                    planar.CompiledData.Vertices[i];
                FoldCanvasCompiledVertex after =
                    rolled.CompiledData.Vertices[i];
                Assert.That(after.SourcePosition, Is.EqualTo(before.SourcePosition));
                Assert.That(after.SourceUv, Is.EqualTo(before.SourceUv));
                Assert.That(after.PanelIndex, Is.EqualTo(before.PanelIndex));
                Assert.That(after.ProvenanceId, Is.EqualTo(before.ProvenanceId));
            }

            CollectionAssert.AreEqual(
                planar.CompiledData.TriangleIndices,
                rolled.CompiledData.TriangleIndices);
            for (int boundaryIndex = 0;
                boundaryIndex < planar.CompiledData.Panels[0].Boundaries.Count;
                boundaryIndex++)
            {
                FoldCanvasCompiledBoundary before =
                    planar.CompiledData.Panels[0].Boundaries[boundaryIndex];
                FoldCanvasCompiledBoundary after =
                    rolled.CompiledData.Panels[0].Boundaries[boundaryIndex];
                Assert.That(after.Id, Is.EqualTo(before.Id));
                CollectionAssert.AreEqual(
                    before.VertexIndices,
                    after.VertexIndices);
            }

            Object.DestroyImmediate(planar.Mesh);
            Object.DestroyImmediate(rolled.Mesh);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void ExplicitRadius_ReportsOrderedStructuredStretchValues()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                Vector2.one,
                8,
                1);
            RollOperationDefinition roll = CreateRoll(
                "explicit",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.Explicit);
            roll.ExplicitRadius = 1f;
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.RollStretchReport));
            Assert.That(
                result.Diagnostics[0].Severity,
                Is.EqualTo(FoldCanvasDiagnosticSeverity.Warning));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("panel"));
            Assert.That(result.Diagnostics[0].OperationId, Is.EqualTo("explicit"));
            Assert.That(result.Diagnostics[0].Values.Count, Is.EqualTo(3));
            AssertDiagnosticValue(
                result.Diagnostics[0].Values[0],
                "sourceSpan",
                1d,
                "m");
            AssertDiagnosticValue(
                result.Diagnostics[0].Values[1],
                "arcLength",
                System.Math.PI * 2d,
                "m");
            AssertDiagnosticValue(
                result.Diagnostics[0].Values[2],
                "stretchRatio",
                System.Math.PI * 2d,
                null);
            Assert.That(result.Diagnostics[0].RepairSuggestions, Is.Empty);
            Vector3 position = result.CompiledData.Vertices[0].Position;
            Assert.That(
                Mathf.Sqrt(position.x * position.x + position.z * position.z),
                Is.EqualTo(1f).Within(PositionTolerance));
            Destroy(result, asset);
        }

        [Test]
        public void ExplicitRadiusWithinGuidanceRange_ProducesInfoReport()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                Vector2.one,
                8,
                1);
            RollOperationDefinition roll = CreateRoll(
                "explicit-guided",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.Explicit);
            roll.ExplicitRadius = 1f / (2f * Mathf.PI);
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.RollStretchReport));
            Assert.That(
                result.Diagnostics[0].Severity,
                Is.EqualTo(FoldCanvasDiagnosticSeverity.Info));
            Assert.That(
                result.Diagnostics[0].Values[2].Value,
                Is.EqualTo(1d).Within(1e-6d));
            Destroy(result, asset);
        }

        [Test]
        public void Roll_AfterRigidTransform_PreservesCurrentFrame()
        {
            const float radius = 0.25f;
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(2f * Mathf.PI * radius, 1f),
                4,
                1);
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place",
                PanelId = "panel",
                Translation = new Vector3(2f, 3f, 4f),
                RotationEuler = new Vector3(0f, 0f, 90f),
                Scale = Vector3.one
            });
            asset.Operations.Add(CreateRoll(
                "roll-after-place",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            AssertPosition(
                result.CompiledData.Vertices[0].Position,
                new Vector3(2.5f, 2.75f, 4f));
            Destroy(result, asset);
        }

        [Test]
        public void Roll_AfterNonPlanarFold_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 2, 2);
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "make-non-planar",
                PanelId = "panel",
                LineStart = new Vector2(0.5f, 0f),
                LineEnd = new Vector2(0.5f, 1f),
                Side = FoldSide.Negative,
                AngleDegrees = 90f,
                Falloff = 0f
            });
            asset.Operations.Add(CreateRoll(
                "roll-after-fold",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedRollEmbedding,
                "panel",
                "roll-after-fold");
            Destroy(result, asset);
        }

        [Test]
        public void PriorScale_ReturnsUnsupportedEmbeddingInsteadOfOverwritingIt()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 4, 2);
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "scale",
                PanelId = "panel",
                Translation = Vector3.zero,
                RotationEuler = Vector3.zero,
                Scale = Vector3.one * 2f
            });
            asset.Operations.Add(CreateRoll(
                "roll-scaled",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedRollEmbedding,
                "panel",
                "roll-scaled");
            Destroy(result, asset);
        }

        [Test]
        public void RepeatedCompile_ProducesExactlyEqualRollGeometry()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                new Vector2(1.7f, 0.9f),
                12,
                4);
            RollOperationDefinition roll = CreateRoll(
                "stable",
                "panel",
                RollDirection.U,
                -315f,
                RollRadiusMode.Explicit);
            roll.ExplicitRadius = 0.31f;
            roll.StartAngleDegrees = 17f;
            asset.Operations.Add(roll);

            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, JoinDiagnostics(first));
            Assert.That(second.Success, Is.True, JoinDiagnostics(second));
            Assert.That(
                second.Diagnostics.Count,
                Is.EqualTo(first.Diagnostics.Count));
            for (int i = 0; i < first.Diagnostics.Count; i++)
            {
                AssertDiagnosticsEqual(
                    first.Diagnostics[i],
                    second.Diagnostics[i]);
            }

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
            CollectionAssert.AreEqual(first.Mesh.vertices, second.Mesh.vertices);
            CollectionAssert.AreEqual(first.Mesh.uv, second.Mesh.uv);
            CollectionAssert.AreEqual(first.Mesh.triangles, second.Mesh.triangles);
            Destroy(first, null);
            Destroy(second, asset);
        }

        [Test]
        public void MissingTarget_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 2, 2);
            asset.Operations.Add(CreateRoll(
                "missing",
                "does-not-exist",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.RollTargetMissing,
                "does-not-exist",
                "missing");
            Destroy(result, asset);
        }

        [Test]
        public void DiskTarget_ReturnsUnsupportedShapeDiagnostic()
        {
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "disk",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                16,
                2));
            asset.Operations.Add(CreateRoll(
                "roll-disk",
                "disk",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedRollPanelShape,
                "disk",
                "roll-disk");
            Destroy(result, asset);
        }

        [Test]
        public void NonFiniteParameter_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 2, 2);
            RollOperationDefinition roll = CreateRoll(
                "nan-angle",
                "panel",
                RollDirection.U,
                float.NaN,
                RollRadiusMode.PreserveArcLength);
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.NonFiniteRollParameter,
                "panel",
                "nan-angle");
            Destroy(result, asset);
        }

        [Test]
        public void NonPositiveExplicitRadius_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 2, 2);
            RollOperationDefinition roll = CreateRoll(
                "bad-radius",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.Explicit);
            roll.ExplicitRadius = 0f;
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.InvalidExplicitRollRadius,
                "panel",
                "bad-radius");
            Destroy(result, asset);
        }

        [Test]
        public void FitTargetBoundary_ReturnsSingleStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 2, 2);
            RollOperationDefinition roll = CreateRoll(
                "fit",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.FitTargetBoundary);
            roll.ExplicitRadius = float.NaN;
            roll.TargetSeamId = "future-seam";
            asset.Seams.Add(new SeamDefinition
            {
                Id = "future-seam",
                A = new BoundaryReference("panel", "uMin"),
                B = new BoundaryReference("panel", "uMax"),
                Mode = SeamMode.Weld
            });
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedFitTargetBoundary,
                "panel",
                "fit");
            Assert.That(
                result.Diagnostics[0].SeamId,
                Is.EqualTo("future-seam"));
            Destroy(result, asset);
        }

        [TestCase(99, 0, "FC3019")]
        [TestCase(0, 99, "FC3020")]
        public void InvalidEnum_ReturnsStableDiagnostic(
            int direction,
            int radiusMode,
            string expectedCode)
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 2, 2);
            RollOperationDefinition roll = CreateRoll(
                "bad-enum",
                "panel",
                (RollDirection)direction,
                360f,
                (RollRadiusMode)radiusMode);
            asset.Operations.Add(roll);

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(result, expectedCode, "panel", "bad-enum");
            Destroy(result, asset);
        }

        [Test]
        public void FullRoll_WithOneSegment_ReturnsInsufficientTessellation()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 1, 1);
            asset.Operations.Add(CreateRoll(
                "undersampled",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            AssertOnlyError(
                result,
                FoldCanvasDiagnosticCodes.InsufficientRollTessellation,
                "panel",
                "undersampled");
            Destroy(result, asset);
        }

        [Test]
        public void FullRoll_WithThreeSegments_DoesNotGenerateZeroAreaTriangles()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(Vector2.one, 3, 1);
            asset.Operations.Add(CreateRoll(
                "three-segments",
                "panel",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            for (int i = 0;
                i < result.CompiledData.TriangleIndices.Count;
                i += 3)
            {
                Vector3 a = result.CompiledData.Vertices[
                    result.CompiledData.TriangleIndices[i]].Position;
                Vector3 b = result.CompiledData.Vertices[
                    result.CompiledData.TriangleIndices[i + 1]].Position;
                Vector3 c = result.CompiledData.Vertices[
                    result.CompiledData.TriangleIndices[i + 2]].Position;
                Assert.That(
                    Vector3.Cross(b - a, c - a).sqrMagnitude,
                    Is.GreaterThan(1e-18f));
            }

            Destroy(result, asset);
        }

        [Test]
        public void DiagnosticValues_AreCopiedOrderedAndReadOnly()
        {
            List<FoldCanvasDiagnosticValue> values =
                new List<FoldCanvasDiagnosticValue>
                {
                    new FoldCanvasDiagnosticValue("first", 1d),
                    new FoldCanvasDiagnosticValue("second", 2d, "m")
                };
            List<string> suggestions =
                new List<string> { "Increase tessellation." };
            FoldCanvasDiagnostic diagnostic = new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.RollStretchReport,
                FoldCanvasDiagnosticSeverity.Info,
                "Structured report.",
                values: values,
                repairSuggestions: suggestions);

            values.Clear();
            suggestions.Clear();

            Assert.That(diagnostic.Values.Count, Is.EqualTo(2));
            Assert.That(diagnostic.Values[0].Key, Is.EqualTo("first"));
            Assert.That(diagnostic.Values[1].Key, Is.EqualTo("second"));
            Assert.That(diagnostic.RepairSuggestions.Count, Is.EqualTo(1));
            Assert.Throws<System.NotSupportedException>(() =>
                ((IList<FoldCanvasDiagnosticValue>)diagnostic.Values).Add(
                    new FoldCanvasDiagnosticValue("third", 3d)));
            Assert.Throws<System.NotSupportedException>(() =>
                ((IList<string>)diagnostic.RepairSuggestions).Add(
                    "Change radius."));
        }

        [Test]
        public void DecoratedCupGeometry_AlignsUnweldedWallAndBottom()
        {
            const float radius = 0.05f;
            const float height = 0.12f;
            const int wallSegments = 64;
            const int wallRows = 12;
            const int diskSegments = 64;
            const int diskRings = 8;
            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0.06f, 0.46f, 0.88f, 0.44f),
                new Vector2(2f * Mathf.PI * radius, height),
                wallSegments,
                wallRows));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.32f, 0.02f, 0.36f, 0.36f),
                Vector2.one * (2f * radius),
                diskSegments,
                diskRings));
            asset.Operations.Add(CreateRoll(
                "roll-wall",
                "wall",
                RollDirection.U,
                360f,
                RollRadiusMode.PreserveArcLength));
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(0f, -height * 0.5f, 0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });

            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.CompiledData.Vertices.Count, Is.EqualTo(1358));
            Assert.That(
                result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(2496));
            Assert.That(result.CompiledData.Panels.Count, Is.EqualTo(2));

            FoldCanvasCompiledPanel wall =
                result.CompiledData.GetPanel("wall");
            FoldCanvasCompiledPanel bottom =
                result.CompiledData.GetPanel("bottom");
            Assert.That(wall.VertexStart, Is.EqualTo(0));
            Assert.That(bottom.VertexStart, Is.EqualTo(845));
            Assert.That(
                result.CompiledData.Vertices[wall.VertexStart].SourceUv,
                Is.EqualTo(new Vector2(0.06f, 0.46f)));
            Assert.That(
                result.CompiledData.Vertices[bottom.VertexStart].SourceUv,
                Is.EqualTo(new Vector2(0.5f, 0.2f)));

            Vector3 diskCenter =
                result.CompiledData.Vertices[bottom.VertexStart].Position;
            AssertPosition(
                diskCenter,
                new Vector3(0f, -height * 0.5f, 0f));

            int diskPerimeterStart =
                bottom.VertexStart + 1 + (diskRings - 1) * diskSegments;
            float maximumBottomGap = 0f;
            for (int u = 0; u < wallSegments; u++)
            {
                Vector3 wallPosition =
                    result.CompiledData.Vertices[wall.VertexStart + u].Position;
                int diskSegment =
                    (diskSegments / 2 - u + diskSegments) % diskSegments;
                Vector3 diskPosition =
                    result.CompiledData.Vertices[
                        diskPerimeterStart + diskSegment].Position;
                maximumBottomGap = Mathf.Max(
                    maximumBottomGap,
                    Vector3.Distance(wallPosition, diskPosition));
                Assert.That(
                    Mathf.Sqrt(
                        wallPosition.x * wallPosition.x +
                        wallPosition.z * wallPosition.z),
                    Is.EqualTo(radius).Within(PositionTolerance));
                Assert.That(
                    wallPosition.y,
                    Is.EqualTo(-height * 0.5f).Within(PositionTolerance));
                Assert.That(
                    diskPosition.y,
                    Is.EqualTo(-height * 0.5f).Within(PositionTolerance));
            }

            Assert.That(maximumBottomGap, Is.LessThan(PositionTolerance));
            Assert.That(
                Vector3.Distance(
                    result.CompiledData.Vertices[wall.VertexStart].Position,
                    result.CompiledData.Vertices[
                        wall.VertexStart + wallSegments].Position),
                Is.LessThan(PositionTolerance));
            Assert.That(
                wall.GetBoundary("uMin").VertexIndices[0],
                Is.Not.EqualTo(wall.GetBoundary("uMax").VertexIndices[0]));

            int i0 = result.CompiledData.TriangleIndices[0];
            int i1 = result.CompiledData.TriangleIndices[1];
            int i2 = result.CompiledData.TriangleIndices[2];
            Vector3 p0 = result.CompiledData.Vertices[i0].Position;
            Vector3 p1 = result.CompiledData.Vertices[i1].Position;
            Vector3 p2 = result.CompiledData.Vertices[i2].Position;
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            Vector3 radial = new Vector3(
                (p0.x + p1.x + p2.x) / 3f,
                0f,
                (p0.z + p1.z + p2.z) / 3f).normalized;
            Assert.That(Vector3.Dot(normal, radial), Is.GreaterThan(0.99f));
            Destroy(result, asset);
        }

        private static FoldCanvasAsset CreateRectangleAsset(
            Vector2 size,
            int uSegments,
            int vSegments,
            Rect? canvasRect = null)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                canvasRect ?? new Rect(0f, 0f, 1f, 1f),
                size,
                uSegments,
                vSegments));
            return asset;
        }

        private static RollOperationDefinition CreateRoll(
            string id,
            string panelId,
            RollDirection direction,
            float angle,
            RollRadiusMode radiusMode)
        {
            return new RollOperationDefinition
            {
                Id = id,
                PanelId = panelId,
                Direction = direction,
                AngleDegrees = angle,
                RadiusMode = radiusMode,
                ExplicitRadius = 0.05f,
                StartAngleDegrees = 0f
            };
        }

        private static void AssertOnlyError(
            FoldCanvasCompileResult result,
            string code,
            string panelId,
            string operationId)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.CompiledData, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(
                result.Diagnostics[0].Severity,
                Is.EqualTo(FoldCanvasDiagnosticSeverity.Error));
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo(panelId));
            Assert.That(result.Diagnostics[0].OperationId, Is.EqualTo(operationId));
        }

        private static void AssertPosition(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(PositionTolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(PositionTolerance));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(PositionTolerance));
        }

        private static void AssertRadialMeshNormal(
            FoldCanvasCompileResult result,
            int vertexIndex,
            float expectedSign)
        {
            Vector3 position = result.Mesh.vertices[vertexIndex];
            Vector3 radial = new Vector3(
                position.x,
                0f,
                position.z).normalized;
            Vector3 normal = result.Mesh.normals[vertexIndex].normalized;
            Assert.That(
                Vector3.Dot(normal, radial) * expectedSign,
                Is.GreaterThan(0.99f));
        }

        private static void AssertDiagnosticValue(
            FoldCanvasDiagnosticValue actual,
            string expectedKey,
            double expectedValue,
            string expectedUnit)
        {
            Assert.That(actual.Key, Is.EqualTo(expectedKey));
            Assert.That(
                actual.Value,
                Is.EqualTo(expectedValue).Within(1e-6d));
            Assert.That(actual.Unit, Is.EqualTo(expectedUnit));
        }

        private static void AssertDiagnosticsEqual(
            FoldCanvasDiagnostic expected,
            FoldCanvasDiagnostic actual)
        {
            Assert.That(actual.Code, Is.EqualTo(expected.Code));
            Assert.That(actual.Severity, Is.EqualTo(expected.Severity));
            Assert.That(actual.Message, Is.EqualTo(expected.Message));
            Assert.That(actual.PanelId, Is.EqualTo(expected.PanelId));
            Assert.That(actual.OperationId, Is.EqualTo(expected.OperationId));
            Assert.That(actual.SeamId, Is.EqualTo(expected.SeamId));
            Assert.That(actual.Values.Count, Is.EqualTo(expected.Values.Count));
            for (int i = 0; i < expected.Values.Count; i++)
            {
                Assert.That(
                    actual.Values[i].Key,
                    Is.EqualTo(expected.Values[i].Key));
                Assert.That(
                    actual.Values[i].Value,
                    Is.EqualTo(expected.Values[i].Value));
                Assert.That(
                    actual.Values[i].Unit,
                    Is.EqualTo(expected.Values[i].Unit));
            }

            CollectionAssert.AreEqual(
                expected.RepairSuggestions,
                actual.RepairSuggestions);
        }

        private static string JoinDiagnostics(FoldCanvasCompileResult result)
        {
            if (result == null)
            {
                return "No compile result was returned.";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset)
        {
            if (result != null && result.Mesh != null)
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
