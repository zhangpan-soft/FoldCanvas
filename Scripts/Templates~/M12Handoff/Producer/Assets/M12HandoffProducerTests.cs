using System;
using System.IO;
using System.IO.Compression;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.M12Proof
{
    public sealed class M12HandoffProducerTests
    {
        private const string SourcePath =
            "Assets/Fixture/m12-production-cup.foldcanvas.json";

        private const string AppearancePath =
            "Assets/Fixture/M04ProductionCupCanvas.png";

        [Test]
        public void ProductionCup_ExportsByteStableSourceFirstHandoff()
        {
            string sourceJson = File.ReadAllText(Absolute(SourcePath));
            Texture2D appearance =
                AssetDatabase.LoadAssetAtPath<Texture2D>(AppearancePath);
            Assert.That(appearance, Is.Not.Null);
            FoldScriptImportResult import = FoldScriptImporter.Import(
                sourceJson,
                new FoldScriptImportOptions
                {
                    SourceProjectPath = SourcePath,
                    AppearanceResolver = new ProjectAppearanceResolver(),
                    RequireAppearance = true,
                });
            Assert.That(import.Success, Is.True, Diagnostics(import));

            string evidenceRoot = Path.Combine(ProjectRoot(), "M12Evidence");
            Directory.CreateDirectory(evidenceRoot);
            string firstPath = Path.Combine(
                evidenceRoot,
                "production-cup.foldcanvas.zip");
            string secondPath = Path.Combine(
                evidenceRoot,
                "production-cup-repeat.foldcanvas.zip");
            try
            {
                FoldCanvasHandoffExportResult first =
                    FoldCanvasHandoffExporter.Export(
                        import.Asset,
                        new FoldCanvasHandoffExportOptions
                        {
                            DestinationPath = firstPath,
                        });
                FoldCanvasHandoffExportResult second =
                    FoldCanvasHandoffExporter.Export(
                        import.Asset,
                        new FoldCanvasHandoffExportOptions
                        {
                            DestinationPath = secondPath,
                        });
                Assert.That(first.Success, Is.True,
                    Diagnostics(first.Diagnostics));
                Assert.That(second.Success, Is.True,
                    Diagnostics(second.Diagnostics));
                Assert.That(second.ArchiveSha256,
                    Is.EqualTo(first.ArchiveSha256));
                CollectionAssert.AreEqual(
                    File.ReadAllBytes(firstPath),
                    File.ReadAllBytes(secondPath));
                Assert.That(first.Evidence.IsClosedVolume, Is.True);
                Assert.That(first.Evidence.IsSingleClosedVolume, Is.True);
                Assert.That(first.Evidence.OpenEdgeCount, Is.Zero);
                Assert.That(first.Evidence.NonManifoldEdgeCount, Is.Zero);

                CopyArchiveEntry(
                    firstPath,
                    "evidence/compile-report.json",
                    Path.Combine(evidenceRoot, "compile-report.json"));
                WriteReport(
                    Path.Combine(evidenceRoot, "producer-report.json"),
                    new ProofReport
                    {
                        format = "foldcanvas-m12-handoff-proof",
                        version = "1",
                        role = "producer",
                        packageVersion = FoldCanvasVersion.Package,
                        compilerVersion = FoldCanvasVersion.Compiler,
                        projectPath = ProjectRoot(),
                        archiveSha256 = first.ArchiveSha256,
                        sourceSha256 = first.Evidence.SourceSha256,
                        appearanceSha256 = first.Evidence.AppearanceSha256,
                        geometrySha256 = first.Evidence.GeometrySha256,
                        objSha256 = first.Evidence.ObjSha256,
                        diagnosticSha256 = first.Evidence.DiagnosticSha256,
                        validationSha256 = first.Evidence.ValidationSha256,
                        closedVolumeSha256 =
                            first.Evidence.ClosedVolumeSha256,
                        renderVertexCount =
                            first.Evidence.RenderVertexCount,
                        topologyVertexCount =
                            first.Evidence.TopologyVertexCount,
                        triangleCount = first.Evidence.TriangleCount,
                        isClosedVolume = first.Evidence.IsClosedVolume,
                        isSingleClosedVolume =
                            first.Evidence.IsSingleClosedVolume,
                    });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(import.Asset);
            }
        }

        private static void CopyArchiveEntry(
            string archivePath,
            string entryName,
            string outputPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            using (Stream input = archive.GetEntry(entryName).Open())
            using (FileStream output = File.Create(outputPath))
            {
                input.CopyTo(output);
            }
        }

        private static void WriteReport(string path, ProofReport report)
        {
            File.WriteAllText(
                path,
                JsonUtility.ToJson(report, true) + "\n",
                new System.Text.UTF8Encoding(false));
        }

        private static string Absolute(string projectPath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot(), projectPath));
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string Diagnostics(FoldScriptImportResult result)
        {
            return Diagnostics(result.Diagnostics);
        }

        private static string Diagnostics(
            System.Collections.Generic.IReadOnlyList<
                FoldCanvasDiagnostic> diagnostics)
        {
            return string.Join("\n", diagnostics);
        }

        private sealed class ProjectAppearanceResolver :
            IFoldScriptAppearanceResolver
        {
            public bool TryResolve(
                string projectRelativePath,
                out Texture2D appearance)
            {
                appearance = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    projectRelativePath);
                return appearance != null;
            }
        }

        [Serializable]
        private sealed class ProofReport
        {
            public string format;
            public string version;
            public string role;
            public string packageVersion;
            public string compilerVersion;
            public string projectPath;
            public string archiveSha256;
            public string sourceSha256;
            public string appearanceSha256;
            public string geometrySha256;
            public string objSha256;
            public string diagnosticSha256;
            public string validationSha256;
            public string closedVolumeSha256;
            public int renderVertexCount;
            public int topologyVertexCount;
            public int triangleCount;
            public bool isClosedVolume;
            public bool isSingleClosedVolume;
        }
    }
}
