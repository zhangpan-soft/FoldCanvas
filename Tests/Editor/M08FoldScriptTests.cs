using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Tests
{
    public sealed class M08FoldScriptTests
    {
        private const string TestFolder = "Assets/FoldCanvasM08Tests";
        private readonly List<UnityEngine.Object> transientObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = transientObjects.Count - 1; i >= 0; i--)
            {
                if (transientObjects[i] != null &&
                    !AssetDatabase.Contains(transientObjects[i]))
                {
                    UnityEngine.Object.DestroyImmediate(transientObjects[i]);
                }
            }

            transientObjects.Clear();
            if (AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }
        }

        [Test]
        public void CupFoldScript_CanonicalRoundTrip_PreservesSemanticSource()
        {
            string input = M08FoldScriptFixtures.LoadPackageText(
                "Schema/gpt-cup.example.foldcanvas.json");

            FoldScriptReadResult first = FoldScriptSerializer.Read(input);
            FoldScriptWriteResult write = FoldScriptSerializer.Write(first.Document);
            FoldScriptReadResult second = FoldScriptSerializer.Read(write.Json);

            Assert.That(first.Success, Is.True, FirstDiagnostic(first.Diagnostics));
            Assert.That(write.Success, Is.True, FirstDiagnostic(write.Diagnostics));
            Assert.That(second.Success, Is.True, FirstDiagnostic(second.Diagnostics));
            AssertSemanticDocumentEqual(first.Document, second.Document);
        }

        [Test]
        public void CanonicalExport_RepeatedCallsAreByteIdentical()
        {
            FoldScriptReadResult read = FoldScriptSerializer.Read(
                M08FoldScriptFixtures.SimpleRectangle());

            FoldScriptWriteResult first = FoldScriptSerializer.Write(read.Document);
            FoldScriptWriteResult second = FoldScriptSerializer.Write(read.Document);

            Assert.That(first.Json, Is.EqualTo(second.Json));
            Assert.That(first.Json.EndsWith("\n", StringComparison.Ordinal), Is.True);
        }

        [Test]
        public void CanonicalExport_IsInvariantAcrossCulture()
        {
            FoldScriptReadResult read = FoldScriptSerializer.Read(
                M08FoldScriptFixtures.SimpleRectangle(
                    physicalWidth: "0.125",
                    physicalHeight: "0.375"));
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                string french = FoldScriptSerializer.Write(read.Document).Json;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                string english = FoldScriptSerializer.Write(read.Document).Json;
                Assert.That(french, Is.EqualTo(english));
                Assert.That(french, Does.Contain("0.125"));
                Assert.That(french, Does.Not.Contain("0,125"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void Import_PreservesPanelSeamAndOperationOrder()
        {
            FoldScriptImportResult import = ImportPackage(
                "Schema/gpt-cup.example.foldcanvas.json");
            transientObjects.Add(import.Asset);

            Assert.That(import.Success, Is.True, FirstDiagnostic(import.Diagnostics));
            Assert.That(import.Asset.Panels.Select(x => x.Id),
                Is.EqualTo(new[] { "wall", "bottom" }));
            Assert.That(import.Asset.Seams.Select(x => x.Id),
                Is.EqualTo(new[] { "close-wall", "attach-bottom" }));
            Assert.That(import.Asset.Operations.Select(x => x.Id),
                Is.EqualTo(new[] { "roll-wall", "place-bottom", "stitch-all", "solidify" }));
        }

        [Test]
        public void ImportExport_PreservesAssetMetadataAndExtensions()
        {
            FoldScriptImportResult import = FoldScriptImporter.Import(
                M08FoldScriptFixtures.SimpleRectangle(
                    extensions: "{\"zeta\":2,\"alpha\":{\"v\":1}}"));
            transientObjects.Add(import.Asset);
            FoldScriptDocument exported =
                FoldScriptAssetConverter.ToDocument(import.Asset);
            FoldScriptWriteResult write = FoldScriptSerializer.Write(exported);

            Assert.That(import.Asset.SourceMetadata.AssetId, Is.EqualTo("test-asset"));
            Assert.That(import.Asset.SourceMetadata.DisplayName, Is.EqualTo("Test Asset"));
            Assert.That(import.Asset.SourceMetadata.Units, Is.EqualTo(FoldScriptUnit.Meter));
            Assert.That(write.Json, Does.Contain("\"extensions\": {\"alpha\":{\"v\":1},\"zeta\":2}"));
        }

        [Test]
        public void CentimeterDocument_ConvertsPhysicalLengthsToMetersAndBack()
        {
            FoldScriptImportResult import = FoldScriptImporter.Import(
                M08FoldScriptFixtures.SimpleRectangle(
                    units: "centimeter",
                    physicalWidth: "25",
                    physicalHeight: "10"));
            transientObjects.Add(import.Asset);

            Assert.That(import.Asset.Panels[0].PhysicalSize.x,
                Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(import.Asset.Panels[0].PhysicalSize.y,
                Is.EqualTo(0.1f).Within(1e-6f));

            FoldScriptDocument document =
                FoldScriptAssetConverter.ToDocument(import.Asset);
            Assert.That(document.Units, Is.EqualTo(FoldScriptUnit.Centimeter));
            Assert.That(document.Panels[0].PhysicalSize.x,
                Is.EqualTo(25f).Within(1e-5f));
            Assert.That(document.Panels[0].PhysicalSize.y,
                Is.EqualTo(10f).Within(1e-5f));
        }

        [Test]
        public void ImportedCup_CompilesToEquivalentGeometry()
        {
            FoldScriptImportResult first = ImportPackage(
                "Schema/gpt-cup.example.foldcanvas.json");
            FoldScriptImportResult second = FoldScriptImporter.Import(
                first.CanonicalJson,
                new FoldScriptImportOptions
                {
                    SourceProjectPath =
                        "Packages/com.foldcanvas.core/Schema/gpt-cup.example.foldcanvas.json"
                });
            transientObjects.Add(first.Asset);
            transientObjects.Add(second.Asset);
            FoldCanvasCompileResult firstCompile = FoldCanvasCompiler.Compile(first.Asset);
            FoldCanvasCompileResult secondCompile = FoldCanvasCompiler.Compile(second.Asset);
            transientObjects.Add(firstCompile.Mesh);
            transientObjects.Add(secondCompile.Mesh);

            Assert.That(firstCompile.Success, Is.True, FirstDiagnostic(firstCompile.Diagnostics));
            Assert.That(secondCompile.Success, Is.True, FirstDiagnostic(secondCompile.Diagnostics));
            Assert.That(firstCompile.Mesh.vertexCount, Is.EqualTo(secondCompile.Mesh.vertexCount));
            Assert.That(firstCompile.Mesh.triangles, Is.EqualTo(secondCompile.Mesh.triangles));
            Assert.That(firstCompile.Mesh.uv, Is.EqualTo(secondCompile.Mesh.uv));
        }

        [Test]
        public void BundledCupSample_ImportsAndCompiles()
        {
            FoldScriptImportResult import = ImportPackage(
                "Samples~/BootstrapPanel/gpt-cup.future-example.foldcanvas.json");
            transientObjects.Add(import.Asset);
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(import.Asset);
            transientObjects.Add(compile.Mesh);

            Assert.That(import.Success, Is.True,
                FirstDiagnostic(import.Diagnostics));
            Assert.That(compile.Success, Is.True,
                FirstDiagnostic(compile.Diagnostics));
            Assert.That(compile.GeometryValidationReport.IsValid, Is.True);
        }

        [Test]
        public void ImportedSphere_CompilesWithFreshSphereAndStrictReports()
        {
            FoldScriptImportResult import = ImportPackage(
                "Samples~/Sphere/sphere-golden.foldcanvas.json");
            transientObjects.Add(import.Asset);
            FoldCanvasCompileResult compile = FoldCanvasCompiler.Compile(import.Asset);
            transientObjects.Add(compile.Mesh);

            Assert.That(compile.Success, Is.True, FirstDiagnostic(compile.Diagnostics));
            Assert.That(compile.SphereReports.Count, Is.EqualTo(1));
            Assert.That(compile.SphereReports[0].IsClosedSphere, Is.True);
            Assert.That(compile.GeometryValidationReport.ValidationLevel,
                Is.EqualTo(FoldCanvasValidationLevel.Strict));
            Assert.That(compile.GeometryValidationReport.ExactIntersectionPairCount,
                Is.Zero);
        }

        [Test]
        public void MalformedJson_ReturnsStableDiagnosticWithoutThrow()
        {
            FoldScriptReadResult result = null;
            Assert.DoesNotThrow(() => result = FoldScriptSerializer.Read("{\"schemaVersion\":"));
            AssertSingleCode(result.Diagnostics, FoldCanvasDiagnosticCodes.MalformedFoldScript);
        }

        [Test]
        public void DuplicateJsonProperty_ReturnsStableDiagnostic()
        {
            FoldScriptReadResult result = FoldScriptSerializer.Read(
                "{\"schemaVersion\":\"0.1\",\"schemaVersion\":\"0.1\"}");
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.DuplicateFoldScriptProperty);
        }

        [Test]
        public void UnknownSchemaVersion_ReturnsStableDiagnostic()
        {
            string json = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"schemaVersion\":\"0.1\"", "\"schemaVersion\":\"1.0\"");
            AssertSingleCode(
                FoldScriptSerializer.Read(json).Diagnostics,
                FoldCanvasDiagnosticCodes.UnsupportedFoldScriptVersion);
        }

        [Test]
        public void UnknownOperation_ReturnsStableDiagnostic()
        {
            string operation = "[{\"id\":\"mystery\",\"type\":\"makeDragon\"}]";
            AssertSingleCode(
                FoldScriptSerializer.Read(
                    M08FoldScriptFixtures.SimpleRectangle(operations: operation)).Diagnostics,
                FoldCanvasDiagnosticCodes.UnknownFoldScriptOperation);
        }

        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        public void NaNAndInfinity_ReturnStableNonFiniteDiagnostic(string token)
        {
            string json = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"weldEpsilon\":0.00001", "\"weldEpsilon\":" + token);
            AssertSingleCode(
                FoldScriptSerializer.Read(json).Diagnostics,
                FoldCanvasDiagnosticCodes.NonFiniteFoldScriptNumber);
        }

        [Test]
        public void OversizedJson_IsRejectedBeforeParsing()
        {
            string json = new string(
                ' ',
                FoldCanvasLimits.MaximumFoldScriptCharacters + 1);
            AssertSingleCode(
                FoldScriptSerializer.Read(json).Diagnostics,
                FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded);

            string manyNodes = "[" + string.Join(
                ",",
                Enumerable.Repeat(
                    "0",
                    FoldCanvasLimits.MaximumFoldScriptJsonNodes)) + "]";
            string nodeHeavy = M08FoldScriptFixtures.SimpleRectangle(
                extensions: manyNodes);
            AssertSingleCode(
                FoldScriptSerializer.Read(nodeHeavy).Diagnostics,
                FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded);
        }

        [Test]
        public void ExcessivePanels_IsRejectedBeforeNativeAllocation()
        {
            FoldScriptReadResult result = FoldScriptSerializer.Read(
                M08FoldScriptFixtures.ExcessivePanels());
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded);
            Assert.That(result.Document, Is.Null);
        }

        [Test]
        public void ExcessiveOperations_IsRejectedBeforeNativeAllocation()
        {
            FoldScriptReadResult result = FoldScriptSerializer.Read(
                M08FoldScriptFixtures.ExcessiveOperations());
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded);
            Assert.That(result.Document, Is.Null);
        }

        [Test]
        public void ExcessiveDepthAndNodeCount_ReturnStableDiagnostics()
        {
            string nested = "0";
            for (int i = 0; i < FoldCanvasLimits.MaximumFoldScriptJsonDepth + 2; i++)
            {
                nested = "{\"v\":" + nested + "}";
            }

            string json = M08FoldScriptFixtures.SimpleRectangle(extensions: nested);
            AssertSingleCode(
                FoldScriptSerializer.Read(json).Diagnostics,
                FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded);
        }

        [Test]
        public void MissingRequiredProperty_ReturnsLocalizedDiagnostic()
        {
            string json = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"displayName\":\"Test Asset\",", string.Empty);
            FoldScriptReadResult result = FoldScriptSerializer.Read(json);
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure);
            Assert.That(result.Diagnostics[0].Message, Does.Contain("displayName"));
        }

        [Test]
        public void UnknownPropertyOutsideExtensions_ReturnsStableDiagnostic()
        {
            string json = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"assetId\":\"test-asset\",",
                    "\"assetId\":\"test-asset\",\"surprise\":true,");
            AssertSingleCode(
                FoldScriptSerializer.Read(json).Diagnostics,
                FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure);
        }

        [Test]
        public void DuplicatePanelSeamAndOperationIds_ReturnStableDiagnostics()
        {
            string duplicatePanel = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"panels\":[{", "\"panels\":[{" )
                .Replace("}],\"seams\":[]",
                    "},{\"id\":\"panel\",\"shape\":\"rectangle\"," +
                    "\"canvasRect\":[0,0,1,1],\"physicalSize\":[1,1]," +
                    "\"tessellation\":{\"uSegments\":1,\"vSegments\":1}}],\"seams\":[]");
            AssertSingleCode(
                FoldScriptSerializer.Read(duplicatePanel).Diagnostics,
                FoldCanvasDiagnosticCodes.DuplicateFoldScriptIdentifier);

            const string seam = "{\"id\":\"same-seam\"," +
                "\"a\":{\"panel\":\"panel\",\"boundary\":\"uMin\"}," +
                "\"b\":{\"panel\":\"panel\",\"boundary\":\"uMax\"}," +
                "\"mode\":\"weld\",\"reverseB\":false,\"sampleCount\":0}";
            string duplicateSeam = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"seams\":[]", "\"seams\":[" + seam + "," + seam + "]");
            AssertSingleCode(
                FoldScriptSerializer.Read(duplicateSeam).Diagnostics,
                FoldCanvasDiagnosticCodes.DuplicateFoldScriptIdentifier);

            string duplicateOperation = M08FoldScriptFixtures.SimpleRectangle(
                operations: "[{\"id\":\"one\",\"type\":\"stitch\",\"seams\":[\"s\"]}," +
                    "{\"id\":\"one\",\"type\":\"stitch\",\"seams\":[\"s\"]}]")
                .Replace("\"seams\":[]", "\"seams\":[{" +
                    "\"id\":\"s\",\"a\":{\"panel\":\"panel\",\"boundary\":\"uMin\"}," +
                    "\"b\":{\"panel\":\"panel\",\"boundary\":\"uMax\"}," +
                    "\"mode\":\"weld\",\"reverseB\":false,\"sampleCount\":0}]");
            AssertSingleCode(
                FoldScriptSerializer.Read(duplicateOperation).Diagnostics,
                FoldCanvasDiagnosticCodes.DuplicateFoldScriptIdentifier);
        }

        [Test]
        public void MissingReferences_ReturnStableDiagnosticsWithoutThrow()
        {
            string seam = "{\"id\":\"bad\"," +
                "\"a\":{\"panel\":\"missing\",\"boundary\":\"uMin\"}," +
                "\"b\":{\"panel\":\"panel\",\"boundary\":\"uMax\"}," +
                "\"mode\":\"weld\",\"reverseB\":false,\"sampleCount\":0}";
            string json = M08FoldScriptFixtures.SimpleRectangle()
                .Replace("\"seams\":[]", "\"seams\":[" + seam + "]");
            FoldScriptReadResult result = null;
            Assert.DoesNotThrow(() => result = FoldScriptSerializer.Read(json));
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.InvalidFoldScriptReference);
        }

        [Test]
        public void RelativeAppearancePath_ResolvesInsideSourceFolder()
        {
            Assert.That(FoldScriptProjectPath.TryResolveAppearance(
                "Assets/FoldScripts/cup.foldcanvas.json",
                "textures/cup.png",
                out string resolved,
                out FoldCanvasDiagnostic diagnostic), Is.True,
                diagnostic?.ToString());
            Assert.That(resolved, Is.EqualTo("Assets/FoldScripts/textures/cup.png"));
        }

        [TestCase("Assets/Art/cup.png", "Assets/Art/cup.png")]
        [TestCase("Packages/com.example.art/cup.png", "Packages/com.example.art/cup.png")]
        public void AssetsAndPackagesPaths_AreAcceptedInsideApprovedRoots(
            string reference,
            string expected)
        {
            Assert.That(FoldScriptProjectPath.TryResolveAppearance(
                "Assets/FoldScripts/cup.foldcanvas.json",
                reference,
                out string resolved,
                out FoldCanvasDiagnostic diagnostic), Is.True,
                diagnostic?.ToString());
            Assert.That(resolved, Is.EqualTo(expected));
        }

        [TestCase("../secret.png")]
        [TestCase("textures/../../secret.png")]
        public void PathTraversal_ReturnsUnsafeAppearancePath(string reference)
        {
            Assert.That(FoldScriptProjectPath.TryResolveAppearance(
                "Assets/FoldScripts/cup.foldcanvas.json",
                reference,
                out _,
                out FoldCanvasDiagnostic diagnostic), Is.False);
            Assert.That(diagnostic.Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.UnsafeAppearancePath));
        }

        [TestCase("/tmp/cup.png")]
        [TestCase("C:/temp/cup.png")]
        [TestCase("https://example.com/cup.png")]
        [TestCase("textures\\cup.png")]
        public void AbsoluteAndUriPaths_ReturnUnsafeAppearancePath(string reference)
        {
            Assert.That(FoldScriptProjectPath.TryResolveAppearance(
                "Assets/FoldScripts/cup.foldcanvas.json",
                reference,
                out _,
                out FoldCanvasDiagnostic diagnostic), Is.False);
            Assert.That(diagnostic.Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.UnsafeAppearancePath));
        }

        [Test]
        public void MissingAppearance_ReturnsStableResolutionDiagnostic()
        {
            FoldScriptImportResult result = FoldScriptImporter.Import(
                M08FoldScriptFixtures.SimpleRectangle(),
                new FoldScriptImportOptions
                {
                    SourceProjectPath = "Assets/Test/cup.foldcanvas.json",
                    AppearanceResolver = new MissingAppearanceResolver(),
                    RequireAppearance = true
                });
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.FoldScriptAppearanceResolutionFailed);
        }

        [Test]
        public void EditorImport_CreatesEditableSourceAndPreservesAppearance()
        {
            string source = PrepareSphereAssetSample();
            const string output = TestFolder + "/ImportedSphere.asset";
            Assert.That(FoldCanvas.Editor.FoldScriptEditorAssetUtility.TryImport(
                source,
                output,
                out FoldCanvasAsset asset,
                out FoldScriptImportResult result), Is.True,
                FirstDiagnostic(result?.Diagnostics));

            Assert.That(asset, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(asset), Is.EqualTo(output));
            Assert.That(asset.Appearance, Is.Not.Null);
            Assert.That(asset.Panels.Count, Is.EqualTo(8));
        }

        [Test]
        public void EditorExport_WritesCanonicalFoldScript()
        {
            string source = PrepareSphereAssetSample();
            const string assetPath = TestFolder + "/ImportedSphere.asset";
            const string jsonPath = TestFolder + "/ExportedSphere.foldcanvas.json";
            Assert.That(FoldCanvas.Editor.FoldScriptEditorAssetUtility.TryImport(
                source, assetPath, out FoldCanvasAsset asset, out _), Is.True);
            Assert.That(FoldCanvas.Editor.FoldScriptEditorAssetUtility.TryExport(
                asset, jsonPath, out FoldScriptWriteResult write), Is.True,
                FirstDiagnostic(write?.Diagnostics));

            TextAsset exported = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            Assert.That(exported, Is.Not.Null);
            Assert.That(exported.text, Is.EqualTo(write.Json));
            Assert.That(FoldScriptSerializer.Read(exported.text).Success, Is.True);
        }

        [Test]
        public void EditorImport_OverwriteReusesExplicitSourceAsset()
        {
            string source = PrepareSphereAssetSample();
            const string output = TestFolder + "/ImportedSphere.asset";
            Assert.That(FoldCanvas.Editor.FoldScriptEditorAssetUtility.TryImport(
                source,
                output,
                out FoldCanvasAsset first,
                out FoldScriptImportResult firstResult), Is.True,
                FirstDiagnostic(firstResult?.Diagnostics));
            int instanceId = first.GetInstanceID();

            Assert.That(FoldCanvas.Editor.FoldScriptEditorAssetUtility.TryImport(
                source,
                output,
                out FoldCanvasAsset second,
                out FoldScriptImportResult secondResult), Is.True,
                FirstDiagnostic(secondResult?.Diagnostics));

            Assert.That(second.GetInstanceID(), Is.EqualTo(instanceId));
            Assert.That(secondResult.Asset, Is.SameAs(second));
            Assert.That(AssetDatabase.GetAssetPath(second), Is.EqualTo(output));
        }

        [Test]
        public void RepairRequest_ContainsStableCompactDiagnosticsWithoutMesh()
        {
            using (RepairFixture fixture = CreateRepairFixture(2))
            {
                FoldCanvasRepairRequest request =
                    FoldCanvasRepairRequestBuilder.Create(
                        fixture.Import.Document,
                        fixture.Compile,
                        fixture.Import.CanonicalJson);
                string payload = request.ToCanonicalJson();

                Assert.That(payload, Does.Contain(FoldCanvasDiagnosticCodes.InsufficientRollTessellation));
                Assert.That(payload, Does.Contain("\"source\""));
                Assert.That(payload.ToLowerInvariant(), Does.Not.Contain("\"mesh\""));
                Assert.That(payload.ToLowerInvariant(), Does.Not.Contain("\"positions\""));
                Assert.That(payload.ToLowerInvariant(), Does.Not.Contain("\"triangles\":["));
                Assert.That(FoldScriptJsonReader.TryParse(payload, out _, out _), Is.True);
            }
        }

        [Test]
        public void RepairRequest_CollectionsAreReadOnly()
        {
            using (RepairFixture fixture = CreateRepairFixture(2))
            {
                FoldCanvasRepairRequest request =
                    FoldCanvasRepairRequestBuilder.Create(
                        fixture.Import.Document,
                        fixture.Compile,
                        fixture.Import.CanonicalJson);
                Assert.That(request.Diagnostics,
                    Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<FoldCanvasRepairDiagnostic>>());
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<FoldCanvasRepairDiagnostic>)request.Diagnostics)
                        .Add(request.Diagnostics[0]));
            }
        }

        [Test]
        public void RepairRequest_NonCanonicalOverrideCannotReplaceDocument()
        {
            using (RepairFixture fixture = CreateRepairFixture(2))
            {
                Assert.Throws<ArgumentException>(() =>
                    FoldCanvasRepairRequestBuilder.Create(
                        fixture.Import.Document,
                        fixture.Compile,
                        M08FoldScriptFixtures.FullRoll(2)));
            }
        }

        [Test]
        public void InvalidRepairResponse_ReentersImporterAndIsRejected()
        {
            FoldCanvasRepairAttemptResult result =
                FoldCanvasRepairCoordinator.Apply(
                    new FoldCanvasRepairResponse("{not-json"));
            Assert.That(result.Success, Is.False);
            AssertSingleCode(result.Diagnostics,
                FoldCanvasDiagnosticCodes.MalformedFoldScript);
        }

        [Test]
        public void CorrectedRepairResponse_ClearsTargetDiagnosticAndCompiles()
        {
            using (RepairFixture invalid = CreateRepairFixture(2))
            {
                Assert.That(invalid.Compile.Success, Is.False);
                Assert.That(invalid.Compile.Diagnostics.Any(x =>
                    x.Code == FoldCanvasDiagnosticCodes.InsufficientRollTessellation), Is.True);
            }

            FoldCanvasRepairAttemptResult corrected =
                FoldCanvasRepairCoordinator.Apply(
                    new FoldCanvasRepairResponse(
                        M08FoldScriptFixtures.FullRoll(3)));
            transientObjects.Add(corrected.ImportResult?.Asset);
            transientObjects.Add(corrected.CompileResult?.Mesh);
            Assert.That(corrected.Success, Is.True,
                FirstDiagnostic(corrected.Diagnostics));
            Assert.That(corrected.CompileResult.Diagnostics.Any(x =>
                x.Code == FoldCanvasDiagnosticCodes.InsufficientRollTessellation), Is.False);
        }

        [Test]
        public void RepairPayload_IsDeterministicAcrossRepeatedCompiles()
        {
            using (RepairFixture first = CreateRepairFixture(2))
            using (RepairFixture second = CreateRepairFixture(2))
            {
                string a = FoldCanvasRepairRequestBuilder.Create(
                    first.Import.Document,
                    first.Compile,
                    first.Import.CanonicalJson).ToCanonicalJson();
                string b = FoldCanvasRepairRequestBuilder.Create(
                    second.Import.Document,
                    second.Compile,
                    second.Import.CanonicalJson).ToCanonicalJson();
                Assert.That(a, Is.EqualTo(b));
            }
        }

        [Test]
        public void RepairPayload_InvalidNativeSourceReturnsStableDiagnostic()
        {
            FoldCanvasAsset invalid =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            transientObjects.Add(invalid);
            bool success = true;
            FoldCanvasDiagnostic diagnostic = null;

            Assert.DoesNotThrow(() => success =
                FoldCanvas.Editor.FoldScriptEditorAssetUtility
                    .TryCreateRepairPayload(
                        invalid,
                        out _,
                        out _,
                        out diagnostic));

            Assert.That(success, Is.False);
            Assert.That(diagnostic, Is.Not.Null);
            Assert.That(diagnostic.Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure));
        }

        [Test]
        public void ProviderContracts_DoNotReferenceProviderOrNetworkAssemblies()
        {
            Assembly runtime = typeof(IFoldCanvasSourceRepairer).Assembly;
            string[] names = runtime.GetReferencedAssemblies()
                .Select(x => x.Name)
                .ToArray();
            Assert.That(names.Any(x =>
                x.IndexOf("OpenAI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.IndexOf("Http", StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.IndexOf("Newtonsoft", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False);
            Assert.That(typeof(FoldCanvasRepairRequest).GetProperties()
                .Any(x => typeof(Mesh).IsAssignableFrom(x.PropertyType)), Is.False);
        }

        private FoldScriptImportResult ImportPackage(string relativePath)
        {
            string json = M08FoldScriptFixtures.LoadPackageText(relativePath);
            return FoldScriptImporter.Import(
                json,
                new FoldScriptImportOptions
                {
                    SourceProjectPath =
                        "Packages/com.foldcanvas.core/" + relativePath
                });
        }

        private static string PrepareSphereAssetSample()
        {
            string absoluteFolder = Path.Combine(
                Application.dataPath,
                "FoldCanvasM08Tests");
            Directory.CreateDirectory(absoluteFolder);
            PackageManagerPackageInfo package =
                PackageManagerPackageInfo.FindForAssetPath(
                    "Packages/com.foldcanvas.core");
            File.Copy(
                Path.Combine(
                    package.resolvedPath,
                    "Samples~/Sphere/sphere-canvas.png"),
                Path.Combine(absoluteFolder, "sphere-canvas.png"),
                true);
            File.Copy(
                Path.Combine(
                    package.resolvedPath,
                    "Samples~/Sphere/sphere-golden.foldcanvas.json"),
                Path.Combine(
                    absoluteFolder,
                    "sphere-golden.foldcanvas.json"),
                true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return TestFolder + "/sphere-golden.foldcanvas.json";
        }

        private static RepairFixture CreateRepairFixture(int uSegments)
        {
            FoldScriptImportResult import = FoldScriptImporter.Import(
                M08FoldScriptFixtures.FullRoll(uSegments));
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(import.Asset);
            return new RepairFixture(import, compile);
        }

        private static void AssertSemanticDocumentEqual(
            FoldScriptDocument expected,
            FoldScriptDocument actual)
        {
            Assert.That(actual.SchemaVersion, Is.EqualTo(expected.SchemaVersion));
            Assert.That(actual.AssetId, Is.EqualTo(expected.AssetId));
            Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
            Assert.That(actual.Units, Is.EqualTo(expected.Units));
            Assert.That(actual.Canvas.Appearance, Is.EqualTo(expected.Canvas.Appearance));
            Assert.That(actual.Panels.Select(x => x.Id),
                Is.EqualTo(expected.Panels.Select(x => x.Id)));
            Assert.That(actual.Seams.Select(x => x.Id),
                Is.EqualTo(expected.Seams.Select(x => x.Id)));
            Assert.That(actual.Operations.Select(x => x.Id),
                Is.EqualTo(expected.Operations.Select(x => x.Id)));
            Assert.That(actual.Operations.Select(x => x.Type),
                Is.EqualTo(expected.Operations.Select(x => x.Type)));
            Assert.That(actual.Compile.ValidationLevel,
                Is.EqualTo(expected.Compile.ValidationLevel));
        }

        private static void AssertSingleCode(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics,
            string code)
        {
            Assert.That(diagnostics.Count, Is.EqualTo(1),
                string.Join("\n", diagnostics.Select(x => x.ToString())));
            Assert.That(diagnostics[0].Code, Is.EqualTo(code));
        }

        private static string FirstDiagnostic(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            return diagnostics == null || diagnostics.Count == 0
                ? string.Empty
                : diagnostics[0].ToString();
        }

        private sealed class MissingAppearanceResolver :
            IFoldScriptAppearanceResolver
        {
            public bool TryResolve(string projectRelativePath, out Texture2D appearance)
            {
                appearance = null;
                return false;
            }
        }

        private sealed class RepairFixture : IDisposable
        {
            public RepairFixture(
                FoldScriptImportResult import,
                FoldCanvasCompileResult compile)
            {
                Import = import;
                Compile = compile;
            }

            public FoldScriptImportResult Import { get; }

            public FoldCanvasCompileResult Compile { get; }

            public void Dispose()
            {
                if (Compile?.Mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(Compile.Mesh);
                }

                if (Import?.Asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(Import.Asset);
                }
            }
        }
    }
}
