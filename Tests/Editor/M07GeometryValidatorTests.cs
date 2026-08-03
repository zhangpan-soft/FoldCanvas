using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M07GeometryValidatorTests
    {
        [Test]
        public void InvalidTriangleIndex_ReturnsStableDiagnosticWithoutThrow()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.InvalidTriangleIndex(),
                FoldCanvasValidationLevel.Basic))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.InvalidTriangleIndex);
                Assert.That(run.Report.InvalidTriangleIndexCount, Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.TriangleIndex,
                    Is.EqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.VertexIndex,
                    Is.EqualTo(8));
            }
        }

        [Test]
        public void IncompleteTriangleIndexBuffer_ReturnsStableDiagnostic()
        {
            Assert.DoesNotThrow(() =>
            {
                using (M07GeometryValidationRun run = Validate(
                    M07AdversarialGeometryFixtures
                        .IncompleteTriangleIndexBuffer(),
                    FoldCanvasValidationLevel.Basic))
                {
                    AssertOnlyError(
                        run,
                        FoldCanvasDiagnosticCodes
                            .IncompleteTriangleIndexBuffer);
                    Assert.That(
                        run.Report.HasIncompleteTriangleIndexBuffer,
                        Is.True);
                }
            });
        }

        [Test]
        public void NonFiniteVertex_ReturnsLocalizedStableDiagnostic()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.NonFiniteVertex(),
                FoldCanvasValidationLevel.Basic))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.NonFiniteVertex);
                Assert.That(run.Report.NonFiniteVertexCount, Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.VertexIndex,
                    Is.EqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.TopologyVertexId,
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void ZeroAreaTriangle_ReturnsLocalizedStableDiagnostic()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.ZeroAreaTriangle(),
                FoldCanvasValidationLevel.Basic))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.ZeroAreaTriangle);
                Assert.That(run.Report.ZeroAreaTriangleCount, Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.TriangleIndex,
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void DuplicateFace_ReturnsDuplicateTriangleWithoutNonManifoldFlood()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.DuplicateFace(),
                FoldCanvasValidationLevel.Standard))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.DuplicateTriangle);
                Assert.That(run.Report.DuplicateTriangleCount, Is.EqualTo(1));
                Assert.That(run.Report.NonManifoldEdgeCount, Is.Zero);
                Assert.That(
                    diagnostic.GeometryContext.TriangleIndex,
                    Is.EqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.RelatedTriangleIndex,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void NonManifoldEdge_ReturnsSortedEdgeContext()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.NonManifoldEdge(),
                FoldCanvasValidationLevel.Basic))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.NonManifoldTopology);
                Assert.That(run.Report.NonManifoldEdgeCount, Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.EdgeTopologyVertexA,
                    Is.EqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.EdgeTopologyVertexB,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void InvertedFace_ReturnsInconsistentWinding()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.InvertedFace(),
                FoldCanvasValidationLevel.Basic))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.InconsistentWinding);
                Assert.That(
                    run.Report.OrientationConflictEdgeCount,
                    Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.TriangleIndex,
                    Is.EqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.RelatedTriangleIndex,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void BowTieVertex_ReturnsTopologyVertexContext()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.BowTieVertex(),
                FoldCanvasValidationLevel.Standard))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.BowTieTopologyVertex);
                Assert.That(
                    run.Report.BowTieTopologyVertexCount,
                    Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.TopologyVertexId,
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void OpenSeam_ReturnsWarningAndKeepsIntentionalSheetSuccessful()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                FoldCanvasValidationLevel.Standard);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.GeometryValidationReport.IsValid, Is.True);
            Assert.That(result.GeometryValidationReport.OpenEdgeCount, Is.EqualTo(4));
            AssertDiagnostic(
                result.Diagnostics,
                FoldCanvasDiagnosticCodes.OpenTopologyBoundary,
                FoldCanvasDiagnosticSeverity.Warning);
            Destroy(result, asset);
        }

        [Test]
        public void ZeroLengthBoundary_ReturnsStableBoundaryDiagnostic()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.ZeroLengthBoundary(),
                FoldCanvasValidationLevel.Standard))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.DegenerateCompiledBoundary);
                Assert.That(run.Report.DegenerateBoundaryCount, Is.EqualTo(1));
                Assert.That(diagnostic.PanelId, Is.EqualTo("panel"));
                Assert.That(
                    diagnostic.BoundaryId,
                    Is.EqualTo("collapsed-test-boundary"));
            }
        }

        [Test]
        public void DisconnectedComponents_ReturnStableComponentEvidence()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.DisconnectedComponents(),
                FoldCanvasValidationLevel.Standard))
            {
                Assert.That(run.Report.IsValid, Is.True);
                Assert.That(run.Report.ComponentCount, Is.EqualTo(2));
                Assert.That(
                    run.Report.Components[0].MinimumTopologyVertexId,
                    Is.EqualTo(0));
                Assert.That(
                    run.Report.Components[1].MinimumTopologyVertexId,
                    Is.EqualTo(3));
                Assert.That(run.Result.Diagnostics.Count, Is.EqualTo(2));
                Assert.That(
                    run.Result.Diagnostics[0].Code,
                    Is.EqualTo(FoldCanvasDiagnosticCodes.OpenTopologyBoundary));
                Assert.That(
                    run.Result.Diagnostics[1].Code,
                    Is.EqualTo(FoldCanvasDiagnosticCodes.DisconnectedGeometry));
                Assert.That(
                    run.Result.Diagnostics[1].GeometryContext.ComponentIndex,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void WeldSeamClosureGap_ReturnsSeamAndOperationContext()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.WeldSeamClosureGap(),
                FoldCanvasValidationLevel.Standard))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.SeamClosureMismatch);
                Assert.That(run.Report.SeamMismatchCount, Is.EqualTo(1));
                Assert.That(diagnostic.OperationId, Is.EqualTo("stitch-gap"));
                Assert.That(diagnostic.SeamId, Is.EqualTo("open-seam"));
                Assert.That(run.Report.Seams[0].MaximumGap, Is.EqualTo(3d));
            }
        }

        [Test]
        public void TopologyPositionConflict_ReturnsStableDiagnostic()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.TopologyPositionConflict(),
                FoldCanvasValidationLevel.Standard))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.TopologyPositionConflict);
                Assert.That(
                    run.Report.TopologyPositionConflictCount,
                    Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.TopologyVertexId,
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void InvertedClosedComponent_ReturnsStableDiagnostic()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.InvertedClosedTetrahedron(),
                FoldCanvasValidationLevel.Standard))
            {
                FoldCanvasDiagnostic diagnostic = AssertOnlyError(
                    run,
                    FoldCanvasDiagnosticCodes.InvertedClosedComponent);
                Assert.That(
                    run.Report.InvertedClosedComponentCount,
                    Is.EqualTo(1));
                Assert.That(run.Report.Components[0].SignedVolume, Is.LessThan(0d));
                Assert.That(
                    diagnostic.GeometryContext.ComponentIndex,
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void BasicLevel_DoesNotEmitStandardDiagnostics()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.DisconnectedComponents(),
                FoldCanvasValidationLevel.Basic))
            {
                Assert.That(run.Report.IsValid, Is.True);
                Assert.That(run.Report.ComponentCount, Is.EqualTo(2));
                Assert.That(run.Result.Diagnostics, Is.Empty);
                Assert.That(run.Report.StrictChecksExecuted, Is.False);
            }
        }

        [Test]
        public void SelfIntersectingRoll_ReturnsConfirmedTrianglePair()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.SelfIntersectingRoll(),
                FoldCanvasValidationLevel.Strict))
            {
                FoldCanvasDiagnostic diagnostic = AssertError(
                    run,
                    FoldCanvasDiagnosticCodes.SelfIntersection);
                Assert.That(run.Report.ExactIntersectionPairCount, Is.EqualTo(1));
                Assert.That(run.Report.Intersections.Count, Is.EqualTo(1));
                Assert.That(
                    diagnostic.GeometryContext.TriangleIndex,
                    Is.EqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.RelatedTriangleIndex,
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void ThicknessOverlap_ReturnsConfirmedTrianglePair()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.ThicknessOverlap(),
                FoldCanvasValidationLevel.Strict))
            {
                AssertError(
                    run,
                    FoldCanvasDiagnosticCodes.SelfIntersection);
                Assert.That(run.Report.ExactIntersectionPairCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void StrictLevel_ReportsBroadPhaseAndExactCounts()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.SelfIntersectingRoll(),
                FoldCanvasValidationLevel.Strict))
            {
                Assert.That(run.Report.StrictChecksExecuted, Is.True);
                Assert.That(run.Report.BroadPhaseCandidatePairCount, Is.EqualTo(1));
                Assert.That(run.Report.ExactIntersectionPairCount, Is.EqualTo(1));
                Assert.That(run.Report.StrictCandidateBudgetExceeded, Is.False);
            }
        }

        [Test]
        public void StrictCandidateBudgetExceeded_ReturnsStableDiagnostic()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures
                    .StrictCandidateBudgetOverflow(),
                FoldCanvasValidationLevel.Strict))
            {
                FoldCanvasDiagnostic diagnostic = AssertError(
                    run,
                    FoldCanvasDiagnosticCodes
                        .StrictValidationBudgetExceeded);
                Assert.That(
                    run.Report.StrictCandidateBudgetExceeded,
                    Is.True);
                Assert.That(
                    run.Report.BroadPhaseCandidatePairCount,
                    Is.EqualTo(
                        FoldCanvasLimits
                            .MaximumStrictValidationCandidatePairs + 1));
                Assert.That(
                    diagnostic.GeometryContext.TriangleIndex,
                    Is.GreaterThanOrEqualTo(0));
                Assert.That(
                    diagnostic.GeometryContext.RelatedTriangleIndex,
                    Is.GreaterThan(
                        diagnostic.GeometryContext.TriangleIndex));
            }
        }

        [Test]
        public void StandardLevel_DoesNotRunExactIntersection()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.SelfIntersectingRoll(),
                FoldCanvasValidationLevel.Standard))
            {
                Assert.That(run.Report.IsValid, Is.True);
                Assert.That(run.Report.StrictChecksExecuted, Is.False);
                Assert.That(run.Report.BroadPhaseCandidatePairCount, Is.Zero);
                Assert.That(run.Report.ExactIntersectionPairCount, Is.Zero);
                AssertNoDiagnostic(
                    run.Result.Diagnostics,
                    FoldCanvasDiagnosticCodes.SelfIntersection);
            }
        }

        [Test]
        public void TopologyAdjacentTriangles_AreNotSelfIntersections()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.OpenSeam(),
                FoldCanvasValidationLevel.Strict))
            {
                Assert.That(run.Report.IsValid, Is.True);
                Assert.That(run.Report.StrictChecksExecuted, Is.True);
                Assert.That(run.Report.ExactIntersectionPairCount, Is.Zero);
                AssertNoDiagnostic(
                    run.Result.Diagnostics,
                    FoldCanvasDiagnosticCodes.SelfIntersection);
            }
        }

        [Test]
        public void StrictValidation_IsDeterministicAcrossRepeatedRuns()
        {
            string first;
            string second;
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.SelfIntersectingRoll(),
                FoldCanvasValidationLevel.Strict))
            {
                first = Fingerprint(run);
            }

            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.SelfIntersectingRoll(),
                FoldCanvasValidationLevel.Strict))
            {
                second = Fingerprint(run);
            }

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void GeometryValidator_DoesNotMutateInputBuffer()
        {
            MeshBuildBuffer buffer = M07AdversarialGeometryFixtures.OpenSeam();
            FoldCanvasCompiledData before = buffer.Freeze();
            using (M07GeometryValidationRun run = Validate(
                buffer,
                FoldCanvasValidationLevel.Strict))
            {
                FoldCanvasCompiledData after = buffer.Freeze();
                Assert.That(after.Vertices.Count, Is.EqualTo(before.Vertices.Count));
                for (int i = 0; i < before.Vertices.Count; i++)
                {
                    Assert.That(after.Vertices[i], Is.EqualTo(before.Vertices[i]));
                }

                CollectionAssert.AreEqual(
                    before.TriangleIndices,
                    after.TriangleIndices);
            }
        }

        [Test]
        public void GeometryValidationReport_CollectionsAreReadOnly()
        {
            using (M07GeometryValidationRun run = Validate(
                M07AdversarialGeometryFixtures.SelfIntersectingRoll(),
                FoldCanvasValidationLevel.Strict))
            {
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<FoldCanvasGeometryComponentReport>)
                        run.Report.Components).Clear());
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<FoldCanvasTriangleIntersectionRecord>)
                        run.Report.Intersections).Clear());
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<FoldCanvasGeometrySeamReport>)
                        run.Report.Seams).Clear());
            }
        }

        [Test]
        public void ValidRectangle_BasicStillCompiles()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                FoldCanvasValidationLevel.Basic);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.GeometryValidationReport, Is.Not.Null);
            Assert.That(result.GeometryValidationReport.IsValid, Is.True);
            Assert.That(result.GeometryValidationReport.ErrorCount, Is.Zero);
            Assert.That(result.GeometryValidationReport.WarningCount, Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void ValidProductionCup_StrictHasNoConfirmedIntersection()
        {
            FoldCanvasAsset asset =
                M07AdversarialGeometryFixtures.CreateProductionCupAsset(
                    FoldCanvasValidationLevel.Strict);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.GeometryValidationReport.IsValid, Is.True);
            Assert.That(result.GeometryValidationReport.ComponentCount, Is.EqualTo(1));
            Assert.That(result.GeometryValidationReport.OpenEdgeCount, Is.Zero);
            Assert.That(result.GeometryValidationReport.ExactIntersectionPairCount, Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void ValidSphere_StrictHasNoConfirmedIntersection()
        {
            FoldCanvasAsset asset =
                SphereCompilerTests.CreateGoldenSphereAsset();
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Strict;
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.GeometryValidationReport.IsValid, Is.True);
            Assert.That(result.GeometryValidationReport.ComponentCount, Is.EqualTo(1));
            Assert.That(result.GeometryValidationReport.OpenEdgeCount, Is.Zero);
            Assert.That(result.GeometryValidationReport.ExactIntersectionPairCount, Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void InvalidValidationLevel_ReturnsStableSourceDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangleAsset(
                FoldCanvasValidationLevel.Basic);
            asset.CompileSettings.ValidationLevel =
                (FoldCanvasValidationLevel)99;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.GeometryValidationReport, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidValidationLevel));
            Destroy(result, asset);
        }

        private static M07GeometryValidationRun Validate(
            MeshBuildBuffer buffer,
            FoldCanvasValidationLevel validationLevel)
        {
            return M07AdversarialGeometryFixtures.Validate(
                buffer,
                validationLevel);
        }

        private static FoldCanvasAsset CreateRectangleAsset(
            FoldCanvasValidationLevel validationLevel)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.CompileSettings.ValidationLevel = validationLevel;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));
            return asset;
        }

        private static FoldCanvasDiagnostic AssertOnlyError(
            M07GeometryValidationRun run,
            string expectedCode)
        {
            Assert.That(run.Report.IsValid, Is.False);
            Assert.That(run.Report.ErrorCount, Is.EqualTo(1));
            Assert.That(run.Result.Diagnostics.Count, Is.EqualTo(1));
            return AssertError(run, expectedCode);
        }

        private static FoldCanvasDiagnostic AssertError(
            M07GeometryValidationRun run,
            string expectedCode)
        {
            FoldCanvasDiagnostic diagnostic = AssertDiagnostic(
                run.Result.Diagnostics,
                expectedCode,
                FoldCanvasDiagnosticSeverity.Error);
            Assert.That(run.Report.IsValid, Is.False);
            return diagnostic;
        }

        private static FoldCanvasDiagnostic AssertDiagnostic(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics,
            string expectedCode,
            FoldCanvasDiagnosticSeverity expectedSeverity)
        {
            for (int i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Code == expectedCode)
                {
                    Assert.That(
                        diagnostics[i].Severity,
                        Is.EqualTo(expectedSeverity));
                    return diagnostics[i];
                }
            }

            Assert.Fail(
                $"Expected diagnostic {expectedCode}. Actual:\n" +
                Diagnostics(diagnostics));
            return null;
        }

        private static void AssertNoDiagnostic(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics,
            string code)
        {
            for (int i = 0; i < diagnostics.Count; i++)
            {
                Assert.That(diagnostics[i].Code, Is.Not.EqualTo(code));
            }
        }

        private static string Fingerprint(M07GeometryValidationRun run)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            builder.Append(run.Report.ValidationLevel);
            builder.Append('|').Append(run.Report.ComponentCount);
            builder.Append('|').Append(run.Report.BroadPhaseCandidatePairCount);
            builder.Append('|').Append(run.Report.ExactIntersectionPairCount);
            for (int i = 0; i < run.Result.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic diagnostic =
                    run.Result.Diagnostics[i];
                builder.Append('|').Append(diagnostic.Code);
                builder.Append(':').Append(diagnostic.Severity);
                builder.Append(':').Append(diagnostic.PanelId);
                builder.Append(':').Append(diagnostic.OperationId);
                builder.Append(':').Append(diagnostic.SeamId);
                builder.Append(':').Append(diagnostic.BoundaryId);
                for (int valueIndex = 0;
                    valueIndex < diagnostic.Values.Count;
                    valueIndex++)
                {
                    FoldCanvasDiagnosticValue value =
                        diagnostic.Values[valueIndex];
                    builder.Append(':').Append(value.Key);
                    builder.Append('=').Append(
                        value.Value.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                }

                FoldCanvasDiagnosticGeometryContext context =
                    diagnostic.GeometryContext;
                if (context != null)
                {
                    builder.Append(":g=")
                        .Append(context.VertexIndex).Append(',')
                        .Append(context.TopologyVertexId).Append(',')
                        .Append(context.ComponentIndex).Append(',')
                        .Append(context.TriangleIndex).Append(',')
                        .Append(context.RelatedTriangleIndex).Append(',')
                        .Append(context.EdgeTopologyVertexA).Append(',')
                        .Append(context.EdgeTopologyVertexB);
                }
            }

            return builder.ToString();
        }

        private static string Diagnostics(FoldCanvasCompileResult result)
        {
            return Diagnostics(result.Diagnostics);
        }

        private static string Diagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < diagnostics.Count; i++)
            {
                builder.AppendLine(diagnostics[i].ToString());
            }

            return builder.ToString();
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
