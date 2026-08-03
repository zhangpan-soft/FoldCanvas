using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Tests
{
    public sealed class M12HandoffTests
    {
        private const string AssetRoot = "Assets/FoldCanvasM12Tests";

        private string externalRoot;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(AssetRoot);
            AssetDatabase.CreateFolder("Assets", "FoldCanvasM12Tests");
            externalRoot = Path.Combine(
                Path.GetTempPath(),
                "FoldCanvasM12Tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(externalRoot);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(AssetRoot);
            if (Directory.Exists(externalRoot))
            {
                Directory.Delete(externalRoot, true);
            }
        }

        [Test]
        public void ExportTwice_ProducesByteIdenticalArchive()
        {
            FoldCanvasAsset source = CreateSource();
            string firstPath = Path.Combine(
                externalRoot,
                "first.foldcanvas.zip");
            string secondPath = Path.Combine(
                externalRoot,
                "second.foldcanvas.zip");

            FoldCanvasHandoffExportResult first =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = firstPath,
                    });
            FoldCanvasHandoffExportResult second =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = secondPath,
                    });

            Assert.That(first.Success, Is.True, Diagnostics(first.Diagnostics));
            Assert.That(second.Success, Is.True, Diagnostics(second.Diagnostics));
            Assert.That(second.ArchiveSha256, Is.EqualTo(first.ArchiveSha256));
            CollectionAssert.AreEqual(
                File.ReadAllBytes(firstPath),
                File.ReadAllBytes(secondPath));
        }

        [Test]
        public void Export_DoesNotMutateSourceAppearanceSelectionOrScene()
        {
            FoldCanvasAsset source = CreateSource();
            string texturePath = AssetDatabase.GetAssetPath(source.Appearance);
            TextureImporter importer =
                (TextureImporter)AssetImporter.GetAtPath(texturePath);
            string sourceBefore = EditorJsonUtility.ToJson(source);
            byte[] appearanceBefore = File.ReadAllBytes(
                ProjectAbsolute(texturePath));
            FilterMode filterBefore = importer.filterMode;
            TextureWrapMode wrapUBefore = importer.wrapModeU;
            TextureWrapMode wrapVBefore = importer.wrapModeV;
            bool mipmapsBefore = importer.mipmapEnabled;
            int sceneRootsBefore = SceneManager.GetActiveScene().rootCount;
            UnityEngine.Object previousSelection = Selection.activeObject;
            Selection.activeObject = source;

            try
            {
                string archivePath = Export(source);

                Assert.That(File.Exists(archivePath), Is.True);
                Assert.That(EditorJsonUtility.ToJson(source),
                    Is.EqualTo(sourceBefore));
                CollectionAssert.AreEqual(
                    appearanceBefore,
                    File.ReadAllBytes(ProjectAbsolute(texturePath)));
                importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                Assert.That(importer.filterMode, Is.EqualTo(filterBefore));
                Assert.That(importer.wrapModeU, Is.EqualTo(wrapUBefore));
                Assert.That(importer.wrapModeV, Is.EqualTo(wrapVBefore));
                Assert.That(importer.mipmapEnabled, Is.EqualTo(mipmapsBefore));
                Assert.That(Selection.activeObject, Is.SameAs(source));
                Assert.That(SceneManager.GetActiveScene().rootCount,
                    Is.EqualTo(sceneRootsBefore));
            }
            finally
            {
                Selection.activeObject = previousSelection;
            }
        }

        [Test]
        public void Archive_UsesCanonicalFixedLayoutAndPayloadHashes()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);

            Assert.That(
                FoldCanvasHandoffArchive.TryRead(
                    archivePath,
                    out FoldCanvasHandoffArchiveContent archive,
                    out FoldCanvasDiagnostic diagnostic),
                Is.True,
                diagnostic?.ToString());
            CollectionAssert.AreEqual(
                FoldCanvasHandoffFormat.OrderedEntries,
                archive.Entries.Keys);

            string manifestJson = System.Text.Encoding.UTF8.GetString(
                archive.Entries[FoldCanvasHandoffFormat.ManifestEntry]);
            Assert.That(
                FoldCanvasHandoffJson.TryReadManifest(
                    manifestJson,
                    out FoldCanvasHandoffManifest manifest,
                    out diagnostic),
                Is.True,
                diagnostic?.ToString());
            Assert.That(manifest.Payloads.Count, Is.EqualTo(5));
            for (int i = 0; i < manifest.Payloads.Count; i++)
            {
                FoldCanvasHandoffPayload payload = manifest.Payloads[i];
                Assert.That(
                    payload.ByteLength,
                    Is.EqualTo(archive.Entries[payload.Path].LongLength));
                Assert.That(
                    payload.Sha256,
                    Is.EqualTo(FoldCanvasHandoffHash.Bytes(
                        archive.Entries[payload.Path])));
            }
        }

        [Test]
        public void Import_CreatesSourceAndDerivedRuntimeAssets()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            const string destination = AssetRoot + "/Imported";

            FoldCanvasHandoffImportResult import =
                FoldCanvasHandoffImporter.Import(
                    new FoldCanvasHandoffImportOptions
                    {
                        ArchivePath = archivePath,
                        DestinationFolder = destination,
                    });

            Assert.That(import.Success, Is.True, Diagnostics(import.Diagnostics));
            Assert.That(import.WasAlreadyImported, Is.False);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    destination + "/FoldCanvasSource.asset"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    destination + "/appearance.png"),
                Is.Not.Null);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                destination + "/GeneratedMesh.asset");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                destination + "/Appearance.mat");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                destination + "/Runtime.prefab");
            Assert.That(mesh, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(
                material.shader.name,
                Is.EqualTo("FoldCanvas/One-Sided Unlit Texture"));
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(mesh));
            Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial,
                Is.SameAs(material));
            Assert.That(
                File.Exists(ProjectAbsolute(
                    destination + "/handoff-receipt.json")),
                Is.True);
        }

        [Test]
        public void ImportSameArchiveTwice_IsNoWriteIdempotent()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            const string destination = AssetRoot + "/Imported";
            FoldCanvasHandoffImportOptions options =
                new FoldCanvasHandoffImportOptions
                {
                    ArchivePath = archivePath,
                    DestinationFolder = destination,
                };

            FoldCanvasHandoffImportResult first =
                FoldCanvasHandoffImporter.Import(options);
            string meshGuid = AssetDatabase.AssetPathToGUID(
                destination + "/GeneratedMesh.asset");
            string prefabGuid = AssetDatabase.AssetPathToGUID(
                destination + "/Runtime.prefab");
            string receipt = File.ReadAllText(ProjectAbsolute(
                destination + "/handoff-receipt.json"));

            FoldCanvasHandoffImportResult second =
                FoldCanvasHandoffImporter.Import(options);

            Assert.That(first.Success, Is.True, Diagnostics(first.Diagnostics));
            Assert.That(second.Success, Is.True, Diagnostics(second.Diagnostics));
            Assert.That(second.WasAlreadyImported, Is.True);
            Assert.That(
                AssetDatabase.AssetPathToGUID(
                    destination + "/GeneratedMesh.asset"),
                Is.EqualTo(meshGuid));
            Assert.That(
                AssetDatabase.AssetPathToGUID(
                    destination + "/Runtime.prefab"),
                Is.EqualTo(prefabGuid));
            Assert.That(
                File.ReadAllText(ProjectAbsolute(
                    destination + "/handoff-receipt.json")),
                Is.EqualTo(receipt));
        }

        [Test]
        public void TamperedPayload_IsRejectedBeforeDestinationCreation()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            Assert.That(
                FoldCanvasHandoffArchive.TryRead(
                    archivePath,
                    out FoldCanvasHandoffArchiveContent archive,
                    out FoldCanvasDiagnostic diagnostic),
                Is.True,
                diagnostic?.ToString());
            Dictionary<string, byte[]> entries =
                new Dictionary<string, byte[]>(
                    archive.Entries,
                    StringComparer.Ordinal);
            byte[] changed = (byte[])entries[
                FoldCanvasHandoffFormat.AppearanceEntry].Clone();
            changed[changed.Length - 1] ^= 0x01;
            entries[FoldCanvasHandoffFormat.AppearanceEntry] = changed;
            string tamperedPath = Path.Combine(
                externalRoot,
                "tampered.foldcanvas.zip");
            using (FileStream stream = File.Create(tamperedPath))
            {
                FoldCanvasHandoffArchive.Write(stream, entries);
            }

            const string destination = AssetRoot + "/Rejected";
            FoldCanvasHandoffImportResult result =
                FoldCanvasHandoffImporter.Import(
                    new FoldCanvasHandoffImportOptions
                    {
                        ArchivePath = tamperedPath,
                        DestinationFolder = destination,
                    });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.HandoffIntegrityMismatch));
            Assert.That(AssetDatabase.IsValidFolder(destination), Is.False);
            Assert.That(Directory.Exists(ProjectAbsolute(destination)), Is.False);
        }

        [Test]
        public void Rebuild_AfterDeletingDerivedOutputs_RecreatesEvidence()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            const string destination = AssetRoot + "/Imported";
            FoldCanvasHandoffImportResult import =
                FoldCanvasHandoffImporter.Import(
                    new FoldCanvasHandoffImportOptions
                    {
                        ArchivePath = archivePath,
                        DestinationFolder = destination,
                    });
            Assert.That(import.Success, Is.True, Diagnostics(import.Diagnostics));
            string sourceGuid = AssetDatabase.AssetPathToGUID(
                destination + "/FoldCanvasSource.asset");
            Assert.That(
                AssetDatabase.DeleteAsset(
                    destination + "/GeneratedMesh.asset"),
                Is.True);
            Assert.That(
                AssetDatabase.DeleteAsset(destination + "/Appearance.mat"),
                Is.True);
            Assert.That(
                AssetDatabase.DeleteAsset(destination + "/Runtime.prefab"),
                Is.True);

            FoldCanvasHandoffRebuildResult rebuild =
                FoldCanvasHandoffRebuilder.Rebuild(destination);

            Assert.That(rebuild.Success, Is.True,
                Diagnostics(rebuild.Diagnostics));
            Assert.That(rebuild.Evidence.GeometrySha256,
                Is.EqualTo(import.Evidence.GeometrySha256));
            Assert.That(rebuild.Evidence.ObjSha256,
                Is.EqualTo(import.Evidence.ObjSha256));
            Assert.That(
                AssetDatabase.AssetPathToGUID(
                    destination + "/FoldCanvasSource.asset"),
                Is.EqualTo(sourceGuid));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    destination + "/GeneratedMesh.asset"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Material>(
                    destination + "/Appearance.mat"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    destination + "/Runtime.prefab"),
                Is.Not.Null);
        }

        [Test]
        public void Rebuild_AfterSourceMutation_RejectsWithoutReplacingMesh()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            const string destination = AssetRoot + "/Imported";
            FoldCanvasHandoffImportResult import =
                FoldCanvasHandoffImporter.Import(
                    new FoldCanvasHandoffImportOptions
                    {
                        ArchivePath = archivePath,
                        DestinationFolder = destination,
                    });
            Assert.That(import.Success, Is.True, Diagnostics(import.Diagnostics));
            string meshGuid = AssetDatabase.AssetPathToGUID(
                destination + "/GeneratedMesh.asset");
            FoldCanvasAsset importedSource =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    destination + "/FoldCanvasSource.asset");
            importedSource.Panels[0].PhysicalSize = new Vector2(2f, 1f);
            EditorUtility.SetDirty(importedSource);
            AssetDatabase.SaveAssets();

            FoldCanvasHandoffRebuildResult rebuild =
                FoldCanvasHandoffRebuilder.Rebuild(destination);

            Assert.That(rebuild.Success, Is.False);
            Assert.That(rebuild.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(rebuild.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch));
            Assert.That(
                AssetDatabase.AssetPathToGUID(
                    destination + "/GeneratedMesh.asset"),
                Is.EqualTo(meshGuid));
        }

        [Test]
        public void UnsupportedArchiveVersion_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) => manifest.Version = "2");

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedHandoffVersion);
        }

        [Test]
        public void IncompatiblePackageVersion_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                    manifest.PackageVersion = "0.0.0-incompatible");

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffCompatibilityMismatch);
        }

        [Test]
        public void IncompatibleCompilerVersion_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                    manifest.CompilerVersion = "0.0.0-incompatible");

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffCompatibilityMismatch);
        }

        [Test]
        public void IncompatibleFoldScriptVersion_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) => manifest.FoldScriptVersion = "9.9");

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffCompatibilityMismatch);
        }

        [Test]
        public void NonCanonicalSource_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                {
                    byte[] original = entries[
                        FoldCanvasHandoffFormat.SourceEntry];
                    byte[] changed = new byte[original.Length + 1];
                    Buffer.BlockCopy(
                        original,
                        0,
                        changed,
                        0,
                        original.Length);
                    changed[changed.Length - 1] = (byte)' ';
                    entries[FoldCanvasHandoffFormat.SourceEntry] = changed;
                });

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.UnsupportedHandoffSource);
        }

        [Test]
        public void ForgedCompileEvidence_ReturnsStableDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                {
                    string json = System.Text.Encoding.UTF8.GetString(
                        entries[FoldCanvasHandoffFormat.EvidenceEntry]);
                    Assert.That(
                        FoldCanvasHandoffJson.TryReadEvidence(
                            json,
                            out FoldCanvasHandoffEvidence evidence,
                            out FoldCanvasDiagnostic diagnostic),
                        Is.True,
                        diagnostic?.ToString());
                    evidence.GeometrySha256 = new string('0', 64);
                    entries[FoldCanvasHandoffFormat.EvidenceEntry] =
                        System.Text.Encoding.UTF8.GetBytes(
                            FoldCanvasHandoffJson.WriteEvidence(evidence));
                });

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch);
        }

        [Test]
        public void PngDimensionMismatch_ReturnsIntegrityDiagnostic()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                    entries[FoldCanvasHandoffFormat.AppearanceEntry] =
                        CreatePngBytes(2, 2));

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffIntegrityMismatch);
        }

        [Test]
        public void OversizedArchive_ReturnsLimitBeforeDestinationCreation()
        {
            string archivePath = Path.Combine(
                externalRoot,
                "oversized.foldcanvas.zip");
            using (FileStream stream = File.Create(archivePath))
            {
                stream.SetLength(
                    FoldCanvasHandoffLimits.MaximumArchiveBytes + 1L);
            }

            const string destination = AssetRoot + "/Rejected";
            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                destination);

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffLimitExceeded);
            Assert.That(AssetDatabase.IsValidFolder(destination), Is.False);
        }

        [Test]
        public void OversizedDecodedCanvas_ReturnsLimitBeforePngDecode()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                {
                    manifest.CanvasWidth = 32768;
                    manifest.CanvasHeight = 32768;
                });

            const string destination = AssetRoot + "/Rejected";
            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                destination);

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffLimitExceeded);
            Assert.That(
                result.Diagnostics[0].Values[0].Key,
                Is.EqualTo("canvasPixels"));
            Assert.That(
                result.Diagnostics[0].Values[0].Value,
                Is.EqualTo(1073741824d));
            Assert.That(
                result.Diagnostics[0].Values[1].Key,
                Is.EqualTo("maximumCanvasPixels"));
            Assert.That(
                result.Diagnostics[0].Values[1].Value,
                Is.EqualTo(
                    (double)FoldCanvasHandoffLimits
                        .MaximumDecodedCanvasPixels));
            Assert.That(AssetDatabase.IsValidFolder(destination), Is.False);
        }

        [Test]
        public void UnownedDestination_IsNeverOverwritten()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            const string destination = AssetRoot + "/Occupied";
            AssetDatabase.CreateFolder(AssetRoot, "Occupied");
            string sentinelPath = destination + "/sentinel.txt";
            File.WriteAllText(ProjectAbsolute(sentinelPath), "keep");
            AssetDatabase.ImportAsset(sentinelPath);

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                destination);

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffDestinationOccupied);
            Assert.That(
                File.ReadAllText(ProjectAbsolute(sentinelPath)),
                Is.EqualTo("keep"));
        }

        [Test]
        public void ChangedArchive_IsNeverAppliedToExistingReceiptFolder()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = Export(source);
            const string destination = AssetRoot + "/Imported";
            FoldCanvasHandoffImportResult first = ImportTo(
                archivePath,
                destination);
            Assert.That(first.Success, Is.True, Diagnostics(first.Diagnostics));
            string sourceGuid = AssetDatabase.AssetPathToGUID(
                destination + "/FoldCanvasSource.asset");
            string changedArchive = RewriteArchive(
                archivePath,
                (entries, manifest) =>
                {
                    string readme = System.Text.Encoding.UTF8.GetString(
                        entries[FoldCanvasHandoffFormat.ReadmeEntry]);
                    entries[FoldCanvasHandoffFormat.ReadmeEntry] =
                        System.Text.Encoding.UTF8.GetBytes(
                            readme + "\nAdditional review note.\n");
                });

            FoldCanvasHandoffImportResult second = ImportTo(
                changedArchive,
                destination);

            AssertSingleFailure(
                second,
                FoldCanvasDiagnosticCodes.HandoffDestinationOccupied);
            Assert.That(
                AssetDatabase.AssetPathToGUID(
                    destination + "/FoldCanvasSource.asset"),
                Is.EqualTo(sourceGuid));
        }

        [Test]
        public void InvalidTextureContract_RollsBackNewOwnedFolder()
        {
            FoldCanvasAsset source = CreateSource();
            string archivePath = RewriteArchive(
                Export(source),
                (entries, manifest) =>
                    manifest.Texture.FilterMode = "UnsupportedFilter");
            const string destination = AssetRoot + "/Rollback";

            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                destination);

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffAssetCreationFailed);
            Assert.That(AssetDatabase.IsValidFolder(destination), Is.False);
            Assert.That(Directory.Exists(ProjectAbsolute(destination)), Is.False);
        }

        [Test]
        public void CustomOperation_ExportIsRejectedWithoutArchive()
        {
            FoldCanvasAsset source = CreateSource();
            source.Operations.Add(new TestCustomOperation
            {
                Id = "custom",
            });
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();
            string destination = Path.Combine(
                externalRoot,
                "custom.foldcanvas.zip");

            FoldCanvasHandoffExportResult result =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = destination,
                    });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource));
            Assert.That(File.Exists(destination), Is.False);
        }

        [Test]
        public void FailedExport_PreservesExistingValidArchive()
        {
            FoldCanvasAsset source = CreateSource();
            string destination = Export(source);
            byte[] validArchive = File.ReadAllBytes(destination);
            source.Operations.Add(new TestCustomOperation
            {
                Id = "custom",
            });
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();

            FoldCanvasHandoffExportResult result =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = destination,
                    });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource));
            CollectionAssert.AreEqual(
                validArchive,
                File.ReadAllBytes(destination));
        }

        [Test]
        public void MissingArchive_ReturnsInputMissingDiagnostic()
        {
            FoldCanvasHandoffImportResult result = ImportTo(
                Path.Combine(externalRoot, "missing.foldcanvas.zip"),
                AssetRoot + "/Rejected");

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.HandoffInputMissing);
        }

        [Test]
        public void InvalidZip_ReturnsUnsafeHandoffEntry()
        {
            string path = Path.Combine(externalRoot, "invalid.zip");
            File.WriteAllText(path, "not a ZIP", new UTF8Encoding(false));

            AssertUnsafeArchive(path);
        }

        [Test]
        public void ArchiveWithMissingEntry_ReturnsUnsafeHandoffEntry()
        {
            List<RawArchiveEntry> entries = CanonicalRawEntries();
            entries.RemoveAt(entries.Count - 1);

            AssertUnsafeArchive(WriteRawArchive(entries));
        }

        [Test]
        public void ArchiveWithExtraEntry_ReturnsUnsafeHandoffEntry()
        {
            List<RawArchiveEntry> entries = CanonicalRawEntries();
            entries.Add(new RawArchiveEntry("extra.txt"));

            AssertUnsafeArchive(WriteRawArchive(entries));
        }

        [Test]
        public void ArchiveWithDuplicateEntry_ReturnsUnsafeHandoffEntry()
        {
            List<RawArchiveEntry> entries = CanonicalRawEntries();
            entries[1] = new RawArchiveEntry(
                FoldCanvasHandoffFormat.ManifestEntry);

            AssertUnsafeArchive(WriteRawArchive(entries));
        }

        [Test]
        public void ArchiveWithTraversalEntry_ReturnsUnsafeHandoffEntry()
        {
            List<RawArchiveEntry> entries = CanonicalRawEntries();
            entries[0] = new RawArchiveEntry("../ifest.json");

            AssertUnsafeArchive(WriteRawArchive(entries));
        }

        [Test]
        public void ArchiveWithSymbolicLinkEntry_ReturnsUnsafeHandoffEntry()
        {
            List<RawArchiveEntry> entries = CanonicalRawEntries();
            entries[0] = new RawArchiveEntry(
                FoldCanvasHandoffFormat.ManifestEntry,
                0xA0000000u);

            AssertUnsafeArchive(WriteRawArchive(entries));
        }

        [Test]
        public void ArchiveWithUnsafePathVariants_ReturnsUnsafeHandoffEntry()
        {
            string[] unsafeNames =
            {
                "/manifest.json",
                "manifest.json/",
                "manifest\\json",
                "./manifest.json",
            };
            for (int i = 0; i < unsafeNames.Length; i++)
            {
                List<RawArchiveEntry> entries = CanonicalRawEntries();
                entries[0] = new RawArchiveEntry(unsafeNames[i]);
                AssertUnsafeArchive(WriteRawArchive(entries));
            }
        }

        private string Export(FoldCanvasAsset source)
        {
            string path = Path.Combine(
                externalRoot,
                "asset.foldcanvas.zip");
            FoldCanvasHandoffExportResult result =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = path,
                    });
            Assert.That(result.Success, Is.True, Diagnostics(result.Diagnostics));
            return path;
        }

        private void AssertUnsafeArchive(string archivePath)
        {
            const string destination = AssetRoot + "/UnsafeRejected";
            FoldCanvasHandoffImportResult result = ImportTo(
                archivePath,
                destination);

            AssertSingleFailure(
                result,
                FoldCanvasDiagnosticCodes.UnsafeHandoffEntry);
            Assert.That(AssetDatabase.IsValidFolder(destination), Is.False);
            Assert.That(Directory.Exists(ProjectAbsolute(destination)), Is.False);
        }

        private string WriteRawArchive(IReadOnlyList<RawArchiveEntry> entries)
        {
            string path = Path.Combine(
                externalRoot,
                "unsafe-" + Guid.NewGuid().ToString("N") + ".zip");
            List<RawCentralEntry> central =
                new List<RawCentralEntry>(entries.Count);
            using (FileStream stream = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(
                stream,
                new UTF8Encoding(false),
                true))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    byte[] name = Encoding.UTF8.GetBytes(entries[i].Name);
                    uint offset = checked((uint)stream.Position);
                    writer.Write(0x04034b50u);
                    writer.Write((ushort)20);
                    writer.Write((ushort)0x0800);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)33);
                    writer.Write(0u);
                    writer.Write(0u);
                    writer.Write(0u);
                    writer.Write(checked((ushort)name.Length));
                    writer.Write((ushort)0);
                    writer.Write(name);
                    central.Add(new RawCentralEntry(
                        name,
                        offset,
                        entries[i].ExternalAttributes));
                }

                uint centralOffset = checked((uint)stream.Position);
                for (int i = 0; i < central.Count; i++)
                {
                    RawCentralEntry entry = central[i];
                    writer.Write(0x02014b50u);
                    writer.Write((ushort)20);
                    writer.Write((ushort)20);
                    writer.Write((ushort)0x0800);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)33);
                    writer.Write(0u);
                    writer.Write(0u);
                    writer.Write(0u);
                    writer.Write(checked((ushort)entry.Name.Length));
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(entry.ExternalAttributes);
                    writer.Write(entry.LocalOffset);
                    writer.Write(entry.Name);
                }

                uint centralSize = checked(
                    (uint)(stream.Position - centralOffset));
                writer.Write(0x06054b50u);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write(checked((ushort)entries.Count));
                writer.Write(checked((ushort)entries.Count));
                writer.Write(centralSize);
                writer.Write(centralOffset);
                writer.Write((ushort)0);
            }

            return path;
        }

        private static List<RawArchiveEntry> CanonicalRawEntries()
        {
            List<RawArchiveEntry> entries =
                new List<RawArchiveEntry>(
                    FoldCanvasHandoffFormat.OrderedEntries.Count);
            for (int i = 0;
                i < FoldCanvasHandoffFormat.OrderedEntries.Count;
                i++)
            {
                entries.Add(new RawArchiveEntry(
                    FoldCanvasHandoffFormat.OrderedEntries[i]));
            }

            return entries;
        }

        private static byte[] CreatePngBytes(int width, int height)
        {
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            try
            {
                Color32[] pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(10, 20, 30, 255);
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private string RewriteArchive(
            string sourcePath,
            Action<Dictionary<string, byte[]>, FoldCanvasHandoffManifest>
                mutation)
        {
            Assert.That(
                FoldCanvasHandoffArchive.TryRead(
                    sourcePath,
                    out FoldCanvasHandoffArchiveContent archive,
                    out FoldCanvasDiagnostic diagnostic),
                Is.True,
                diagnostic?.ToString());
            Dictionary<string, byte[]> entries =
                new Dictionary<string, byte[]>(
                    archive.Entries,
                    StringComparer.Ordinal);
            string manifestJson = System.Text.Encoding.UTF8.GetString(
                entries[FoldCanvasHandoffFormat.ManifestEntry]);
            Assert.That(
                FoldCanvasHandoffJson.TryReadManifest(
                    manifestJson,
                    out FoldCanvasHandoffManifest manifest,
                    out diagnostic),
                Is.True,
                diagnostic?.ToString());
            mutation(entries, manifest);
            for (int i = 0; i < manifest.Payloads.Count; i++)
            {
                FoldCanvasHandoffPayload payload = manifest.Payloads[i];
                byte[] bytes = entries[payload.Path];
                payload.ByteLength = bytes.LongLength;
                payload.Sha256 = FoldCanvasHandoffHash.Bytes(bytes);
            }

            entries[FoldCanvasHandoffFormat.ManifestEntry] =
                System.Text.Encoding.UTF8.GetBytes(
                    FoldCanvasHandoffJson.WriteManifest(manifest));
            string output = Path.Combine(
                externalRoot,
                "rewritten-" + Guid.NewGuid().ToString("N") +
                ".foldcanvas.zip");
            using (FileStream stream = File.Create(output))
            {
                FoldCanvasHandoffArchive.Write(stream, entries);
            }

            return output;
        }

        private static FoldCanvasHandoffImportResult ImportTo(
            string archivePath,
            string destination)
        {
            return FoldCanvasHandoffImporter.Import(
                new FoldCanvasHandoffImportOptions
                {
                    ArchivePath = archivePath,
                    DestinationFolder = destination,
                });
        }

        private static void AssertSingleFailure(
            FoldCanvasHandoffImportResult result,
            string code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        }

        private static FoldCanvasAsset CreateSource()
        {
            string texturePath = AssetRoot + "/appearance.png";
            Texture2D generated = new Texture2D(
                4,
                4,
                TextureFormat.RGBA32,
                false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(32, 164, 220, 255);
            }

            generated.SetPixels32(pixels);
            generated.Apply();
            File.WriteAllBytes(ProjectAbsolute(texturePath),
                generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(
                texturePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            TextureImporter importer =
                (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapModeU = TextureWrapMode.Clamp;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            FoldCanvasAsset source =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            source.name = "M12HandoffSource";
            source.Appearance =
                AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            source.SourceMetadata.SchemaVersion =
                FoldCanvasSourceMetadata.CurrentSchemaVersion;
            source.SourceMetadata.AssetId = "m12-handoff-test";
            source.SourceMetadata.DisplayName = "M12 Handoff Test";
            source.SourceMetadata.Units = FoldScriptUnit.Meter;
            source.SourceMetadata.AppearanceReference = "appearance.png";
            source.SourceMetadata.CanvasPixelWidth = 4;
            source.SourceMetadata.CanvasPixelHeight = 4;
            source.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(1f, 1f),
                2,
                2));
            source.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Basic;
            AssetDatabase.CreateAsset(
                source,
                AssetRoot + "/source.asset");
            AssetDatabase.SaveAssets();
            return source;
        }

        private static string ProjectAbsolute(string projectPath)
        {
            string root = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(root, projectPath));
        }

        private static string Diagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < diagnostics.Count; i++)
            {
                builder.AppendLine(diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        [Serializable]
        private sealed class TestCustomOperation : FoldOperationDefinition
        {
            public override FoldOperationType Type => FoldOperationType.Custom;
        }

        private sealed class RawArchiveEntry
        {
            public RawArchiveEntry(
                string name,
                uint externalAttributes = 0u)
            {
                Name = name;
                ExternalAttributes = externalAttributes;
            }

            public string Name { get; }

            public uint ExternalAttributes { get; }
        }

        private sealed class RawCentralEntry
        {
            public RawCentralEntry(
                byte[] name,
                uint localOffset,
                uint externalAttributes)
            {
                Name = name;
                LocalOffset = localOffset;
                ExternalAttributes = externalAttributes;
            }

            public byte[] Name { get; }

            public uint LocalOffset { get; }

            public uint ExternalAttributes { get; }
        }
    }
}
