using System;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M10OperationRegistryTests
    {
        [Test]
        public void Compile_WithoutCustomOperations_IsIdenticalWithEmptyRegistry()
        {
            FoldCanvasAsset asset = CreateAsset(false);

            FoldCanvasCompileResult baseline =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult registered =
                FoldCanvasCompiler.Compile(
                    asset,
                    new FoldCanvasOperationRegistry());

            Assert.That(baseline.Success, Is.True, Diagnostics(baseline));
            Assert.That(registered.Success, Is.True, Diagnostics(registered));
            CollectionAssert.AreEqual(
                baseline.Mesh.vertices,
                registered.Mesh.vertices);
            CollectionAssert.AreEqual(
                baseline.Mesh.triangles,
                registered.Mesh.triangles);
            CollectionAssert.AreEqual(
                baseline.Mesh.uv,
                registered.Mesh.uv);
            Destroy(baseline, false);
            Destroy(registered, false);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void RegisteredPositionOperation_ExecutesInAuthoredOrder()
        {
            FoldCanvasAsset asset = CreateAsset(true);
            asset.Operations.Insert(0,
                new RigidTransformOperationDefinition
                {
                    Id = "place",
                    PanelId = "panel",
                    Translation = new Vector3(1f, 0f, 0f),
                    RotationEuler = Vector3.zero,
                    Scale = Vector3.one
                });
            FoldCanvasOperationRegistry registry = CreateRegistry();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset, registry);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                Vector3 source = new Vector3(
                    result.CompiledData.Vertices[i].SourcePosition.x,
                    result.CompiledData.Vertices[i].SourcePosition.y,
                    0f);
                Assert.That(
                    result.CompiledData.Vertices[i].Position,
                    Is.EqualTo(source + new Vector3(1f, 0f, 0.25f)));
            }

            Destroy(result, true, asset);
        }

        [Test]
        public void RegisteredPositionOperation_PreservesUvTopologyAndProvenance()
        {
            FoldCanvasAsset baselineAsset = CreateAsset(false);
            FoldCanvasAsset customAsset = CreateAsset(true);

            FoldCanvasCompileResult baseline =
                FoldCanvasCompiler.Compile(baselineAsset);
            FoldCanvasCompileResult custom =
                FoldCanvasCompiler.Compile(
                    customAsset,
                    CreateRegistry());

            Assert.That(baseline.Success, Is.True, Diagnostics(baseline));
            Assert.That(custom.Success, Is.True, Diagnostics(custom));
            Assert.That(
                custom.CompiledData.Vertices.Count,
                Is.EqualTo(baseline.CompiledData.Vertices.Count));
            CollectionAssert.AreEqual(
                baseline.CompiledData.TriangleIndices,
                custom.CompiledData.TriangleIndices);
            for (int i = 0; i < baseline.CompiledData.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex before =
                    baseline.CompiledData.Vertices[i];
                FoldCanvasCompiledVertex after =
                    custom.CompiledData.Vertices[i];
                Assert.That(after.SourcePosition, Is.EqualTo(before.SourcePosition));
                Assert.That(after.SourceUv, Is.EqualTo(before.SourceUv));
                Assert.That(after.PanelIndex, Is.EqualTo(before.PanelIndex));
                Assert.That(after.ProvenanceId, Is.EqualTo(before.ProvenanceId));
                Assert.That(after.TopologyVertexId, Is.EqualTo(before.TopologyVertexId));
            }

            Destroy(baseline, true, baselineAsset);
            Destroy(custom, true, customAsset);
        }

        [Test]
        public void Registry_DescriptorsAreOrdinalAndRegistrationOrderIndependent()
        {
            FoldCanvasOperationRegistry first =
                new FoldCanvasOperationRegistry();
            FoldCanvasOperationRegistry second =
                new FoldCanvasOperationRegistry();
            M10OffsetOperationExecutor zeta =
                new M10OffsetOperationExecutor(
                    "org.example.zeta");
            M10OffsetOperationExecutor alpha =
                new M10OffsetOperationExecutor(
                    "org.example.alpha",
                    typeof(M10SecondaryOperationDefinition));

            Assert.That(first.TryRegister(zeta, out _), Is.True);
            Assert.That(first.TryRegister(alpha, out _), Is.True);
            Assert.That(second.TryRegister(alpha, out _), Is.True);
            Assert.That(second.TryRegister(zeta, out _), Is.True);

            Assert.That(first.Descriptors.Count, Is.EqualTo(2));
            Assert.That(first.Descriptors[0].OperationTypeId,
                Is.EqualTo("org.example.alpha"));
            Assert.That(first.Descriptors[1].OperationTypeId,
                Is.EqualTo("org.example.zeta"));
            Assert.That(second.Descriptors[0].OperationTypeId,
                Is.EqualTo(first.Descriptors[0].OperationTypeId));
            Assert.That(second.Descriptors[1].OperationTypeId,
                Is.EqualTo(first.Descriptors[1].OperationTypeId));
        }

        [Test]
        public void UnregisteredCustomOperation_ReturnsStableDiagnosticWithoutMesh()
        {
            FoldCanvasAsset asset = CreateAsset(true);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            AssertOnlyCode(
                first,
                FoldCanvasDiagnosticCodes
                    .UnregisteredExtensionOperation);
            AssertOnlyCode(
                second,
                first.Diagnostics[0].Code);
            Destroy(first, false);
            Destroy(second, false);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void DuplicateRegistration_ReturnsStableDiagnostic()
        {
            FoldCanvasOperationRegistry registry = CreateRegistry();

            bool registered = registry.TryRegister(
                new M10OffsetOperationExecutor(),
                out FoldCanvasDiagnostic diagnostic);

            Assert.That(registered, Is.False);
            Assert.That(diagnostic, Is.Not.Null);
            Assert.That(
                diagnostic.Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .DuplicateExtensionRegistration));
            Assert.That(registry.Descriptors.Count, Is.EqualTo(1));
        }

        [Test]
        public void InvalidRegistration_ReturnsStableDiagnostic()
        {
            FoldCanvasOperationRegistry registry =
                new FoldCanvasOperationRegistry();

            bool registered = registry.TryRegister(
                new M10OffsetOperationExecutor("Invalid Type"),
                out FoldCanvasDiagnostic diagnostic);

            Assert.That(registered, Is.False);
            Assert.That(
                diagnostic.Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .InvalidExtensionRegistration));
            Assert.That(registry.Descriptors, Is.Empty);
        }

        [Test]
        public void CustomOperation_MissingPanelReturnsStableDiagnosticBeforeTessellation()
        {
            FoldCanvasAsset asset = CreateAsset(true);
            ((M10OffsetOperationDefinition)asset.Operations[0]).PanelId =
                "missing";

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset, CreateRegistry());

            AssertOnlyCode(
                result,
                FoldCanvasDiagnosticCodes
                    .ExtensionOperationTargetMissing);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void CustomOperation_FailedValidationDoesNotExecute()
        {
            AssertBehaviorDiagnostic(
                M10ExtensionBehavior.FailValidation,
                FoldCanvasDiagnosticCodes
                    .ExtensionOperationValidationFailed);
        }

        [Test]
        public void CustomOperation_FailedExecutionRollsBack()
        {
            FoldCanvasAsset asset = CreateAsset(true);
            FoldCanvasCompileResult result = new FoldCanvasCompileResult();
            FoldCanvasOperationRegistry registry =
                CreateRegistry(M10ExtensionBehavior.FailExecution);
            FoldCanvasExtensionOperationPlan plan =
                FoldCanvasExtensionOperationPlan.Build(
                    asset,
                    registry,
                    result);
            MeshBuildBuffer buffer = new MeshBuildBuffer();
            PanelTessellator.Append(asset.Panels[0], buffer);
            Vector3[] before = CapturePositions(buffer);

            bool executed = plan.TryExecute(
                0,
                asset.Operations[0],
                buffer,
                result);

            Assert.That(executed, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .ExtensionOperationExecutionFailed));
            CollectionAssert.AreEqual(before, CapturePositions(buffer));
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void CustomOperation_NonFiniteMutationRollsBack()
        {
            AssertBehaviorDiagnostic(
                M10ExtensionBehavior.WriteNonFinite,
                FoldCanvasDiagnosticCodes
                    .ExtensionOperationInvalidMutation);
        }

        [Test]
        public void CustomOperation_ExceptionReturnsStableDiagnosticAndRollsBack()
        {
            AssertBehaviorDiagnostic(
                M10ExtensionBehavior.ThrowDuringExecution,
                FoldCanvasDiagnosticCodes.ExtensionOperationException);
        }

        [Test]
        public void CustomOperation_AfterSelectedStitchReturnsTerminalDiagnostic()
        {
            FoldCanvasAsset asset = CreateAdjacentAssetWithCustomAfterStitch();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset, CreateRegistry());

            AssertOnlyCode(
                result,
                FoldCanvasDiagnosticCodes
                    .StitchMustBeTerminalForSelectedPanels);
            Assert.That(result.Diagnostics[0].PanelId, Is.EqualTo("left"));
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void ContributorFixture_CompilesThroughPublicApi()
        {
            FoldCanvasAsset asset = CreateAsset(true);
            FoldCanvasOperationRegistry registry =
                new FoldCanvasOperationRegistry();
            Assert.That(
                registry.TryRegister(
                    new M10OffsetOperationExecutor(),
                    out FoldCanvasDiagnostic registrationDiagnostic),
                Is.True,
                registrationDiagnostic?.ToString());

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset, registry);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.Mesh, Is.Not.Null);
            Destroy(result, true, asset);
        }

        private static void AssertBehaviorDiagnostic(
            M10ExtensionBehavior behavior,
            string expectedCode)
        {
            FoldCanvasAsset asset = CreateAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(
                    asset,
                    CreateRegistry(behavior));

            AssertOnlyCode(result, expectedCode);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        private static FoldCanvasAsset CreateAsset(bool custom)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M10RegistryTest";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(2f, 1f),
                2,
                1));
            if (custom)
            {
                asset.Operations.Add(
                    new M10OffsetOperationDefinition
                    {
                        Id = "custom-offset",
                        PanelId = "panel",
                        Offset = new Vector3(0f, 0f, 0.25f)
                    });
            }

            return asset;
        }

        private static FoldCanvasAsset
            CreateAdjacentAssetWithCustomAfterStitch()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "left",
                new Rect(0f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "right",
                new Rect(0.6f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                1));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "join",
                A = new BoundaryReference("left", "uMax"),
                B = new BoundaryReference("right", "uMin"),
                Mode = SeamMode.Weld,
                SampleCount = 2
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-right",
                PanelId = "right",
                Translation = Vector3.right,
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("join");
            asset.Operations.Add(stitch);
            asset.Operations.Add(new M10OffsetOperationDefinition
            {
                Id = "too-late",
                PanelId = "left",
                Offset = Vector3.forward * 0.1f
            });
            return asset;
        }

        private static FoldCanvasOperationRegistry CreateRegistry(
            M10ExtensionBehavior behavior =
                M10ExtensionBehavior.Succeed)
        {
            FoldCanvasOperationRegistry registry =
                new FoldCanvasOperationRegistry();
            bool registered = registry.TryRegister(
                new M10OffsetOperationExecutor(
                    executionBehavior: behavior),
                out FoldCanvasDiagnostic diagnostic);
            Assert.That(
                registered,
                Is.True,
                diagnostic?.ToString());
            return registry;
        }

        private static Vector3[] CapturePositions(
            MeshBuildBuffer buffer)
        {
            Vector3[] positions = new Vector3[buffer.Vertices.Count];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = buffer.Vertices[i].Position;
            }

            return positions;
        }

        private static void AssertOnlyCode(
            FoldCanvasCompileResult result,
            string code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1), Diagnostics(result));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        }

        private static string Diagnostics(
            FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            bool destroyAsset,
            FoldCanvasAsset asset = null)
        {
            if (result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }

            if (destroyAsset && asset != null)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
