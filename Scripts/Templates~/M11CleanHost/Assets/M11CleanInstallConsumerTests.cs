using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FoldCanvas.M11.Consumer
{
    public sealed class M11CleanInstallConsumerTests
    {
        [Test]
        public void CleanArchive_CompilesThroughPublicApiAndExportsDeterministically()
        {
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            CleanInstallInput expected = JsonUtility.FromJson<CleanInstallInput>(
                File.ReadAllText(Path.Combine(projectRoot, "M11Expected.json")));

            PackageInfo package = PackageInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            Assert.That(package.name, Is.EqualTo(expected.packageName));
            Assert.That(package.version, Is.EqualTo(expected.packageVersion));
            Assert.That(package.source.ToString(), Is.EqualTo("LocalTarball"));
            Assert.That(expected.manifestDependency, Does.EndWith(".tgz"));
            Assert.That(
                Path.GetFullPath(package.resolvedPath),
                Does.StartWith(Path.Combine(projectRoot, "Library")));

            FoldCanvasAsset source = CreateSource();
            FoldCanvasCompileResult first = FoldCanvasCompiler.Compile(source);
            FoldCanvasCompileResult second = FoldCanvasCompiler.Compile(source);
            try
            {
                Assert.That(first.Success, Is.True, Diagnostics(first));
                Assert.That(second.Success, Is.True, Diagnostics(second));
                Assert.That(first.CompiledData.Vertices.Count, Is.EqualTo(15));
                Assert.That(first.CompiledData.TopologyVertexCount, Is.EqualTo(15));
                Assert.That(first.CompiledData.TriangleIndices.Count / 3,
                    Is.EqualTo(16));

                string firstGeometry = GeometryEvidence(first.CompiledData);
                string secondGeometry = GeometryEvidence(second.CompiledData);
                Assert.That(secondGeometry, Is.EqualTo(firstGeometry));

                FoldCanvasTextExportResult firstObj =
                    FoldCanvasObjExporter.Export(first.CompiledData);
                FoldCanvasTextExportResult secondObj =
                    FoldCanvasObjExporter.Export(second.CompiledData);
                Assert.That(firstObj.Success, Is.True, ExportDiagnostics(firstObj));
                Assert.That(secondObj.Content, Is.EqualTo(firstObj.Content));

                CleanInstallReport report = new CleanInstallReport
                {
                    format = "foldcanvas-clean-install-report",
                    version = "1",
                    packageName = package.name,
                    packageVersion = FoldCanvasVersion.Package,
                    packageSha256 = expected.packageSha256,
                    unityVersion = Application.unityVersion,
                    packageSource = package.source.ToString(),
                    resolvedPackagePath = Path.GetFullPath(package.resolvedPath),
                    sourceSha256 = Sha256(SourceEvidence()),
                    geometrySha256 = Sha256(firstGeometry),
                    objSha256 = Sha256(firstObj.Content),
                    diagnosticSha256 = Sha256(DiagnosticsEvidence(first)),
                    renderVertices = first.CompiledData.Vertices.Count,
                    topologyVertices = first.CompiledData.TopologyVertexCount,
                    triangles = first.CompiledData.TriangleIndices.Count / 3,
                    diagnostics = first.Diagnostics.Count
                };
                string evidenceDirectory = Path.Combine(
                    projectRoot,
                    "M11Evidence");
                Directory.CreateDirectory(evidenceDirectory);
                File.WriteAllText(
                    Path.Combine(evidenceDirectory, "consumer-report.json"),
                    JsonUtility.ToJson(report, true) + "\n",
                    new UTF8Encoding(false));
            }
            finally
            {
                Destroy(first);
                Destroy(second);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static FoldCanvasAsset CreateSource()
        {
            FoldCanvasAsset source =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            source.name = "M11CleanInstallSource";
            source.Panels.Add(PanelDefinition.CreateRectangle(
                "consumer-panel",
                new Rect(0.125f, 0.25f, 0.5f, 0.5f),
                new Vector2(2f, 1f),
                4,
                2));
            source.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Basic;
            return source;
        }

        private static string SourceEvidence()
        {
            return "rectangle|consumer-panel|0.125|0.25|0.5|0.5|2|1|4|2|basic";
        }

        private static string GeometryEvidence(FoldCanvasCompiledData data)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("vertices|");
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                AppendVector3(builder, vertex.Position);
                AppendVector2(builder, vertex.SourcePosition);
                AppendVector2(builder, vertex.SourceUv);
                builder.Append(vertex.PanelIndex).Append('|');
                builder.Append(vertex.ProvenanceId).Append('|');
                builder.Append(vertex.TopologyVertexId).Append(';');
            }

            builder.Append("triangles|");
            for (int i = 0; i < data.TriangleIndices.Count; i++)
            {
                builder.Append(data.TriangleIndices[i]).Append(',');
            }

            return builder.ToString();
        }

        private static void AppendVector3(StringBuilder builder, Vector3 value)
        {
            AppendFloat(builder, value.x);
            AppendFloat(builder, value.y);
            AppendFloat(builder, value.z);
        }

        private static void AppendVector2(StringBuilder builder, Vector2 value)
        {
            AppendFloat(builder, value.x);
            AppendFloat(builder, value.y);
        }

        private static void AppendFloat(StringBuilder builder, float value)
        {
            builder.Append(value == 0f
                ? "0"
                : value.ToString("R", CultureInfo.InvariantCulture));
            builder.Append('|');
        }

        private static string DiagnosticsEvidence(FoldCanvasCompileResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                builder.Append(diagnostic.Code).Append('|');
                builder.Append(diagnostic.Severity).Append('|');
                builder.Append(diagnostic.PanelId).Append('|');
                builder.Append(diagnostic.OperationId).Append(';');
            }

            return builder.ToString();
        }

        private static string Diagnostics(FoldCanvasCompileResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static string ExportDiagnostics(FoldCanvasTextExportResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static string Sha256(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(
                    Encoding.UTF8.GetBytes(value ?? string.Empty));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static void Destroy(FoldCanvasCompileResult result)
        {
            if (result != null && result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }
        }

        [Serializable]
        private sealed class CleanInstallInput
        {
            public string format;
            public string version;
            public string packageName;
            public string packageVersion;
            public string packageArchive;
            public string packageSha256;
            public string manifestDependency;
        }

        [Serializable]
        private sealed class CleanInstallReport
        {
            public string format;
            public string version;
            public string packageName;
            public string packageVersion;
            public string packageSha256;
            public string unityVersion;
            public string packageSource;
            public string resolvedPackagePath;
            public string sourceSha256;
            public string geometrySha256;
            public string objSha256;
            public string diagnosticSha256;
            public int renderVertices;
            public int topologyVertices;
            public int triangles;
            public int diagnostics;
        }
    }
}
