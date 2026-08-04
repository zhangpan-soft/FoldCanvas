using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FoldCanvas;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.M15.Upgrade
{
    public sealed class M15SourceUpgradeTests
    {
        private const string SourceFile =
            "m12-production-cup.foldcanvas.json";

        private const string AppearanceFile =
            "M04ProductionCupCanvas.png";

        private static readonly string[] ExpectedInputFiles =
        {
            AppearanceFile,
            SourceFile,
        };

        [Test]
        public void AuthoritativeSource_RebuildsAfterPackageReplacement()
        {
            string projectRoot = ProjectRoot();
            UpgradeInput expected = JsonUtility.FromJson<UpgradeInput>(
                File.ReadAllText(Path.Combine(
                    projectRoot,
                    "M15Expected.json")));
            Assert.That(expected, Is.Not.Null);
            Assert.That(
                expected.phase == "before" || expected.phase == "after",
                Is.True);

            string inputRoot = Path.Combine(projectRoot, "M15Input");
            string[] inputFiles = EnumerateInputFiles(inputRoot);
            CollectionAssert.AreEqual(ExpectedInputFiles, inputFiles,
                "M15Input must contain only authoritative FoldScript and PNG source.");

            string sourcePath = Path.Combine(inputRoot, SourceFile);
            string appearancePath = Path.Combine(inputRoot, AppearanceFile);
            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            byte[] appearanceBytes = File.ReadAllBytes(appearancePath);
            Assert.That(Sha256(sourceBytes),
                Is.EqualTo(expected.sourceRawSha256));
            Assert.That(Sha256(appearanceBytes),
                Is.EqualTo(expected.appearanceSha256));

            PackageInfo package = PackageInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            Assert.That(package.name, Is.EqualTo(expected.packageName));
            Assert.That(package.version,
                Is.EqualTo(expected.packageVersion));
            Assert.That(package.source.ToString(), Is.EqualTo("LocalTarball"));
            string resolvedPackagePath = Path.GetFullPath(package.resolvedPath);
            Assert.That(resolvedPackagePath,
                Does.StartWith(Path.Combine(projectRoot, "Library")));

            string packageArchive = ResolvePackageArchive(
                projectRoot,
                expected.manifestDependency);
            Assert.That(File.Exists(packageArchive), Is.True, packageArchive);
            Assert.That(Sha256(File.ReadAllBytes(packageArchive)),
                Is.EqualTo(expected.packageSha256));

            string sourceJson = new UTF8Encoding(false, true)
                .GetString(sourceBytes);
            FoldScriptImportResult import = FoldScriptImporter.Import(
                sourceJson,
                new FoldScriptImportOptions
                {
                    // The source is intentionally consumer-owned outside Assets.
                    // This approved logical path resolves its relative PNG name;
                    // geometry compilation does not consume an imported Texture.
                    SourceProjectPath =
                        "Assets/M15Input/" + SourceFile,
                });
            Assert.That(import.Success, Is.True, Diagnostics(import.Diagnostics));
            Assert.That(Sha256(import.CanonicalJson),
                Is.EqualTo(expected.canonicalSourceSha256));

            FoldCanvasCompileResult first = null;
            FoldCanvasCompileResult second = null;
            try
            {
                first = FoldCanvasCompiler.Compile(import.Asset);
                second = FoldCanvasCompiler.Compile(import.Asset);
                Assert.That(first.Success, Is.True, Diagnostics(first.Diagnostics));
                Assert.That(second.Success, Is.True,
                    Diagnostics(second.Diagnostics));
                AssertProductionCup(first);
                AssertProductionCup(second);

                string firstGeometry = HashCompiledData(first.CompiledData);
                string secondGeometry = HashCompiledData(second.CompiledData);
                Assert.That(secondGeometry, Is.EqualTo(firstGeometry));

                FoldCanvasTextExportResult firstObj =
                    FoldCanvasObjExporter.Export(
                        first.CompiledData,
                        new FoldCanvasObjExportOptions
                        {
                            ObjectName = "m12-production-cup",
                            FlipTextureV = false,
                            IncludeMetadataComments = false,
                        });
                FoldCanvasTextExportResult secondObj =
                    FoldCanvasObjExporter.Export(
                        second.CompiledData,
                        new FoldCanvasObjExportOptions
                        {
                            ObjectName = "m12-production-cup",
                            FlipTextureV = false,
                            IncludeMetadataComments = false,
                        });
                Assert.That(firstObj.Success, Is.True,
                    Diagnostics(firstObj.Diagnostics));
                Assert.That(secondObj.Success, Is.True,
                    Diagnostics(secondObj.Diagnostics));
                Assert.That(secondObj.Content, Is.EqualTo(firstObj.Content));

                string firstDiagnostics = HashDiagnostics(first);
                string secondDiagnostics = HashDiagnostics(second);
                Assert.That(secondDiagnostics,
                    Is.EqualTo(firstDiagnostics));
                string firstValidation = HashValidation(first);
                string secondValidation = HashValidation(second);
                Assert.That(secondValidation,
                    Is.EqualTo(firstValidation));

                UpgradeReport report = new UpgradeReport
                {
                    format = "foldcanvas-m15-source-upgrade-proof",
                    version = "1",
                    phase = expected.phase,
                    packageName = package.name,
                    packageVersion = FoldCanvasVersion.Package,
                    packageSha256 = expected.packageSha256,
                    unityVersion = Application.unityVersion,
                    packageSource = package.source.ToString(),
                    resolvedPackagePath = resolvedPackagePath,
                    sourceInputFiles = inputFiles,
                    sourceInputCount = inputFiles.Length,
                    derivedInputCount = 0,
                    sourceRawSha256 = Sha256(sourceBytes),
                    canonicalSourceSha256 =
                        Sha256(import.CanonicalJson),
                    appearanceSha256 = Sha256(appearanceBytes),
                    geometrySha256 = firstGeometry,
                    objSha256 = Sha256(firstObj.Content),
                    diagnosticSha256 = firstDiagnostics,
                    validationSha256 = firstValidation,
                    renderVertices = first.CompiledData.Vertices.Count,
                    topologyVertices =
                        first.CompiledData.TopologyVertexCount,
                    triangles =
                        first.CompiledData.TriangleIndices.Count / 3,
                    diagnostics = first.Diagnostics.Count,
                    openEdges =
                        first.GeometryValidationReport.OpenEdgeCount,
                    nonManifoldEdges =
                        first.GeometryValidationReport.NonManifoldEdgeCount,
                    components =
                        first.GeometryValidationReport.ComponentCount,
                    isClosedVolume =
                        first.ClosedVolumeReport.IsClosedVolume,
                    isSingleClosedVolume =
                        first.ClosedVolumeReport.IsSingleClosedVolume,
                };
                string evidenceRoot = Path.Combine(
                    projectRoot,
                    "M15Evidence");
                Directory.CreateDirectory(evidenceRoot);
                File.WriteAllText(
                    Path.Combine(evidenceRoot, expected.phase + ".json"),
                    JsonUtility.ToJson(report, true) + "\n",
                    new UTF8Encoding(false));
            }
            finally
            {
                Destroy(first);
                Destroy(second);
                if (import.Asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(import.Asset);
                }
            }
        }

        private static void AssertProductionCup(
            FoldCanvasCompileResult result)
        {
            Assert.That(result.CompiledData.Vertices.Count,
                Is.EqualTo(2972));
            Assert.That(result.CompiledData.TopologyVertexCount,
                Is.EqualTo(2562));
            Assert.That(result.CompiledData.TriangleIndices.Count / 3,
                Is.EqualTo(5120));
            Assert.That(result.Diagnostics.Count, Is.Zero);
            Assert.That(result.GeometryValidationReport, Is.Not.Null);
            Assert.That(result.GeometryValidationReport.IsValid, Is.True);
            Assert.That(result.GeometryValidationReport.OpenEdgeCount,
                Is.Zero);
            Assert.That(result.GeometryValidationReport.NonManifoldEdgeCount,
                Is.Zero);
            Assert.That(result.GeometryValidationReport.ComponentCount,
                Is.EqualTo(1));
            Assert.That(result.ClosedVolumeReport, Is.Not.Null);
            Assert.That(result.ClosedVolumeReport.IsClosedVolume, Is.True);
            Assert.That(result.ClosedVolumeReport.IsSingleClosedVolume,
                Is.True);
        }

        private static string[] EnumerateInputFiles(string inputRoot)
        {
            Assert.That(Directory.Exists(inputRoot), Is.True, inputRoot);
            List<string> files = new List<string>();
            string[] paths = Directory.GetFiles(
                inputRoot,
                "*",
                SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                FileAttributes attributes = File.GetAttributes(paths[i]);
                Assert.That(
                    (attributes & FileAttributes.ReparsePoint) == 0,
                    Is.True,
                    "Upgrade source cannot be a symlink: " + paths[i]);
                files.Add(Path.GetRelativePath(inputRoot, paths[i])
                    .Replace('\\', '/'));
            }

            files.Sort(StringComparer.Ordinal);
            return files.ToArray();
        }

        private static string ResolvePackageArchive(
            string projectRoot,
            string manifestDependency)
        {
            Assert.That(manifestDependency, Does.StartWith("file:"));
            Assert.That(manifestDependency, Does.EndWith(".tgz"));
            string relative = manifestDependency.Substring("file:".Length)
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                "Packages",
                relative));
        }

        private static string HashCompiledData(
            FoldCanvasCompiledData data)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(
                stream,
                new UTF8Encoding(false),
                true))
            {
                writer.Write(data.Vertices.Count);
                writer.Write(data.TopologyVertexCount);
                for (int i = 0; i < data.Vertices.Count; i++)
                {
                    FoldCanvasCompiledVertex vertex = data.Vertices[i];
                    WriteVector3(writer, vertex.Position);
                    WriteVector2(writer, vertex.SourcePosition);
                    WriteVector2(writer, vertex.SourceUv);
                    writer.Write(vertex.PanelIndex);
                    writer.Write(vertex.ProvenanceId);
                    writer.Write(vertex.TopologyVertexId);
                }

                writer.Write(data.TriangleIndices.Count);
                for (int i = 0; i < data.TriangleIndices.Count; i++)
                {
                    writer.Write(data.TriangleIndices[i]);
                }

                writer.Write(data.Panels.Count);
                for (int i = 0; i < data.Panels.Count; i++)
                {
                    FoldCanvasCompiledPanel panel = data.Panels[i];
                    writer.Write(panel.Id);
                    writer.Write(panel.PanelIndex);
                    writer.Write((int)panel.Shape);
                    WriteRect(writer, panel.CanvasRect);
                    WriteVector2(writer, panel.PhysicalSize);
                    writer.Write(panel.VertexStart);
                    writer.Write(panel.VertexCount);
                    writer.Write(panel.Boundaries.Count);
                    for (int boundaryIndex = 0;
                        boundaryIndex < panel.Boundaries.Count;
                        boundaryIndex++)
                    {
                        FoldCanvasCompiledBoundary boundary =
                            panel.Boundaries[boundaryIndex];
                        writer.Write(boundary.Id);
                        writer.Write(boundary.VertexIndices.Count);
                        for (int vertexIndex = 0;
                            vertexIndex < boundary.VertexIndices.Count;
                            vertexIndex++)
                        {
                            writer.Write(
                                boundary.VertexIndices[vertexIndex]);
                        }
                    }
                }

                writer.Write(data.CornerSegments.Count);
                for (int i = 0; i < data.CornerSegments.Count; i++)
                {
                    FoldCanvasCompiledCornerSegment corner =
                        data.CornerSegments[i];
                    writer.Write(corner.OperationId);
                    writer.Write(corner.OuterFromVertexIndex);
                    writer.Write(corner.OuterToVertexIndex);
                    writer.Write(corner.InnerFromVertexIndex);
                    writer.Write(corner.InnerToVertexIndex);
                }

                writer.Write(data.SphericalSurfaces.Count);
                writer.Flush();
                return Sha256(stream.ToArray());
            }
        }

        private static string HashDiagnostics(
            FoldCanvasCompileResult result)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(
                stream,
                new UTF8Encoding(false),
                true))
            {
                writer.Write(result.Diagnostics.Count);
                for (int i = 0; i < result.Diagnostics.Count; i++)
                {
                    FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                    writer.Write(diagnostic.Code ?? string.Empty);
                    writer.Write((int)diagnostic.Severity);
                    writer.Write(diagnostic.Message ?? string.Empty);
                    writer.Write(diagnostic.PanelId ?? string.Empty);
                    writer.Write(diagnostic.OperationId ?? string.Empty);
                    writer.Write(diagnostic.SeamId ?? string.Empty);
                    writer.Write(diagnostic.BoundaryId ?? string.Empty);
                }

                writer.Flush();
                return Sha256(stream.ToArray());
            }
        }

        private static string HashValidation(
            FoldCanvasCompileResult result)
        {
            FoldCanvasGeometryValidationReport validation =
                result.GeometryValidationReport;
            FoldCanvasClosedVolumeReport closed =
                result.ClosedVolumeReport;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(
                stream,
                new UTF8Encoding(false),
                true))
            {
                writer.Write(validation != null);
                if (validation != null)
                {
                    writer.Write((int)validation.ValidationLevel);
                    writer.Write(validation.VertexCount);
                    writer.Write(validation.TriangleCount);
                    writer.Write(validation.OpenEdgeCount);
                    writer.Write(validation.NonManifoldEdgeCount);
                    writer.Write(validation.OrientationConflictEdgeCount);
                    writer.Write(validation.TopologyPositionConflictCount);
                    writer.Write(validation.ComponentCount);
                    writer.Write(validation.ErrorCount);
                    writer.Write(validation.WarningCount);
                    writer.Write(validation.IsValid);
                }

                writer.Write(closed != null);
                if (closed != null)
                {
                    writer.Write(closed.OperationId ?? string.Empty);
                    writer.Write(closed.TriangleCount);
                    writer.Write(closed.TopologyVertexCount);
                    writer.Write(closed.UniqueEdgeCount);
                    writer.Write(closed.TwoIncidentEdgeCount);
                    writer.Write(closed.OpenEdgeCount);
                    writer.Write(closed.NonManifoldEdgeCount);
                    writer.Write(closed.OrientationConflictEdgeCount);
                    writer.Write(closed.CollapsedTopologyEdgeCount);
                    writer.Write(closed.TopologyPositionConflictCount);
                    writer.Write(closed.PartiallySelectedTriangleCount);
                    writer.Write(closed.ComponentCount);
                    writer.Write(closed.ZeroVolumeComponentCount);
                    writer.Write(closed.SignedVolume);
                    writer.Write(closed.TotalAbsoluteVolume);
                    writer.Write(closed.IsClosedVolume);
                    writer.Write(closed.IsSingleClosedVolume);
                }

                writer.Flush();
                return Sha256(stream.ToArray());
            }
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        private static void WriteVector2(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        private static void WriteRect(BinaryWriter writer, Rect value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.width);
            writer.Write(value.height);
        }

        private static string Sha256(string value)
        {
            return Sha256(new UTF8Encoding(false).GetBytes(
                value ?? string.Empty));
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] digest = algorithm.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(
                    digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static string Diagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            return string.Join("\n", diagnostics);
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                ".."));
        }

        private static void Destroy(FoldCanvasCompileResult result)
        {
            if (result?.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }
        }

        [Serializable]
        private sealed class UpgradeInput
        {
            public string format;
            public string version;
            public string phase;
            public string packageName;
            public string packageVersion;
            public string packageSha256;
            public string manifestDependency;
            public string sourceRawSha256;
            public string canonicalSourceSha256;
            public string appearanceSha256;
        }

        [Serializable]
        private sealed class UpgradeReport
        {
            public string format;
            public string version;
            public string phase;
            public string packageName;
            public string packageVersion;
            public string packageSha256;
            public string unityVersion;
            public string packageSource;
            public string resolvedPackagePath;
            public string[] sourceInputFiles;
            public int sourceInputCount;
            public int derivedInputCount;
            public string sourceRawSha256;
            public string canonicalSourceSha256;
            public string appearanceSha256;
            public string geometrySha256;
            public string objSha256;
            public string diagnosticSha256;
            public string validationSha256;
            public int renderVertices;
            public int topologyVertices;
            public int triangles;
            public int diagnostics;
            public int openEdges;
            public int nonManifoldEdges;
            public int components;
            public bool isClosedVolume;
            public bool isSingleClosedVolume;
        }
    }
}
