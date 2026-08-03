using System;
using System.IO;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.M12Proof
{
    public sealed class M12HandoffReceiverTests
    {
        [Test]
        public void ProductionHandoff_ImportsAndRebuildsFromReceivedSource()
        {
            string root = ProjectRoot();
            string archive = Path.Combine(
                root,
                "M12Input",
                "production-cup.foldcanvas.zip");
            Assert.That(File.Exists(archive), Is.True, archive);
            const string destination = "Assets/ReceivedProductionCup";
            FoldCanvasHandoffImportResult import =
                FoldCanvasHandoffImporter.Import(
                    new FoldCanvasHandoffImportOptions
                    {
                        ArchivePath = archive,
                        DestinationFolder = destination,
                    });
            Assert.That(import.Success, Is.True,
                Diagnostics(import.Diagnostics));
            Assert.That(import.WasAlreadyImported, Is.False);
            Assert.That(import.Evidence.IsClosedVolume, Is.True);
            Assert.That(import.Evidence.IsSingleClosedVolume, Is.True);

            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    import.Receipt.SourceAssetPath);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                import.Receipt.MeshPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                import.Receipt.MaterialPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                import.Receipt.PrefabPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(mesh, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(material.shader.name,
                Is.EqualTo("FoldCanvas/One-Sided Unlit Texture"));
            Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(mesh));
            Assert.That(prefab.GetComponent<MeshRenderer>().sharedMaterial,
                Is.SameAs(material));

            Assert.That(AssetDatabase.DeleteAsset(import.Receipt.MeshPath),
                Is.True);
            Assert.That(AssetDatabase.DeleteAsset(import.Receipt.MaterialPath),
                Is.True);
            Assert.That(AssetDatabase.DeleteAsset(import.Receipt.PrefabPath),
                Is.True);
            FoldCanvasHandoffRebuildResult rebuild =
                FoldCanvasHandoffRebuilder.Rebuild(destination);
            Assert.That(rebuild.Success, Is.True,
                Diagnostics(rebuild.Diagnostics));
            Assert.That(rebuild.Evidence.GeometrySha256,
                Is.EqualTo(import.Evidence.GeometrySha256));
            Assert.That(rebuild.Evidence.ObjSha256,
                Is.EqualTo(import.Evidence.ObjSha256));

            string evidenceRoot = Path.Combine(root, "M12Evidence");
            Directory.CreateDirectory(evidenceRoot);
            File.Copy(
                Absolute(import.Receipt.SourceJsonPath),
                Path.Combine(evidenceRoot, "source.foldcanvas.json"),
                true);
            File.Copy(
                Absolute(import.Receipt.ReceiptPath),
                Path.Combine(evidenceRoot, "handoff-receipt.json"),
                true);
            WriteReport(
                Path.Combine(evidenceRoot, "receiver-report.json"),
                new ProofReport
                {
                    format = "foldcanvas-m12-handoff-proof",
                    version = "1",
                    role = "receiver",
                    packageVersion = FoldCanvasVersion.Package,
                    compilerVersion = FoldCanvasVersion.Compiler,
                    projectPath = root,
                    archiveSha256 = import.ArchiveSha256,
                    sourceSha256 = import.Evidence.SourceSha256,
                    appearanceSha256 = import.Evidence.AppearanceSha256,
                    geometrySha256 = import.Evidence.GeometrySha256,
                    objSha256 = import.Evidence.ObjSha256,
                    diagnosticSha256 = import.Evidence.DiagnosticSha256,
                    validationSha256 = import.Evidence.ValidationSha256,
                    closedVolumeSha256 =
                        import.Evidence.ClosedVolumeSha256,
                    renderVertexCount =
                        import.Evidence.RenderVertexCount,
                    topologyVertexCount =
                        import.Evidence.TopologyVertexCount,
                    triangleCount = import.Evidence.TriangleCount,
                    isClosedVolume = import.Evidence.IsClosedVolume,
                    isSingleClosedVolume =
                        import.Evidence.IsSingleClosedVolume,
                });
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

        private static string Diagnostics(
            System.Collections.Generic.IReadOnlyList<
                FoldCanvasDiagnostic> diagnostics)
        {
            return string.Join("\n", diagnostics);
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
