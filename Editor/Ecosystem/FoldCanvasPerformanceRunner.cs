using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FoldCanvas.Editor
{
    [Serializable]
    public sealed class FoldCanvasPerformanceScenarioResult
    {
        public string id;
        public int warmupIterations;
        public int measuredIterations;
        public int vertexCount;
        public int triangleCount;
        public double minimumMilliseconds;
        public double medianMilliseconds;
        public double maximumMilliseconds;
        public string geometrySha256;
        public bool withinBaseline;
    }

    [Serializable]
    public sealed class FoldCanvasPerformanceReport
    {
        public string format = "foldcanvas-performance-report";
        public string version = "1";
        public string unityVersion;
        public string packageVersion;
        public List<FoldCanvasPerformanceScenarioResult> scenarios =
            new List<FoldCanvasPerformanceScenarioResult>();
    }

    public static class FoldCanvasPerformanceRunner
    {
        private const string BaselineRelativePath =
            "Documentation~/m10-performance-baselines.json";
        private const string ReportRelativePath =
            "Library/FoldCanvas/M10PerformanceReport.json";

        [MenuItem("Tools/FoldCanvas/Run M10 Performance Baselines")]
        public static void RunFromMenu()
        {
            FoldCanvasPerformanceReport report = Run();
            string fullPath = Path.GetFullPath(ReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(
                fullPath,
                JsonUtility.ToJson(report, true),
                new UTF8Encoding(false));
            UnityEngine.Debug.Log(
                $"FoldCanvas M10 performance baselines completed: {report.scenarios.Count} scenarios. Derived report: {fullPath}");
        }

        public static FoldCanvasPerformanceReport Run()
        {
            FoldCanvasPerformanceBaselineDocument baselines =
                LoadBaselines();
            return RunBaselines(baselines, null, null);
        }

        internal static FoldCanvasPerformanceReport RunForTests(
            int warmupIterations,
            int measuredIterations)
        {
            return RunBaselines(
                LoadBaselines(),
                warmupIterations,
                measuredIterations);
        }

        private static FoldCanvasPerformanceReport RunBaselines(
            FoldCanvasPerformanceBaselineDocument baselines,
            int? warmupOverride,
            int? measuredOverride)
        {
            FoldCanvasPerformanceReport report =
                new FoldCanvasPerformanceReport
                {
                    unityVersion = Application.unityVersion,
                    packageVersion = FoldCanvasVersion.Package
                };
            for (int i = 0; i < baselines.scenarios.Length; i++)
            {
                FoldCanvasPerformanceBaseline scenario =
                    baselines.scenarios[i];
                report.scenarios.Add(RunScenario(
                    scenario,
                    warmupOverride ?? scenario.warmupIterations,
                    measuredOverride ?? scenario.measuredIterations));
            }

            return report;
        }

        private static FoldCanvasPerformanceScenarioResult RunScenario(
            FoldCanvasPerformanceBaseline baseline,
            int warmupIterations,
            int measuredIterations)
        {
            if (warmupIterations < 0 || measuredIterations < 1)
            {
                throw new InvalidDataException(
                    "Performance iteration counts are invalid.");
            }

            FoldCanvasAsset asset = CreateScenarioAsset(
                baseline.id,
                out FoldCanvasOperationRegistry registry);
            try
            {
                for (int i = 0; i < warmupIterations; i++)
                {
                    FoldCanvasCompileResult warmup =
                        FoldCanvasCompiler.Compile(asset, registry);
                    EnsureSuccess(warmup, baseline.id);
                    UnityEngine.Object.DestroyImmediate(warmup.Mesh);
                }

                double[] durations = new double[measuredIterations];
                int vertexCount = 0;
                int triangleCount = 0;
                string geometryHash = null;
                for (int i = 0; i < measuredIterations; i++)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    FoldCanvasCompileResult compile =
                        FoldCanvasCompiler.Compile(asset, registry);
                    stopwatch.Stop();
                    EnsureSuccess(compile, baseline.id);
                    durations[i] = stopwatch.Elapsed.TotalMilliseconds;
                    vertexCount = compile.CompiledData.Vertices.Count;
                    triangleCount =
                        compile.CompiledData.TriangleIndices.Count / 3;
                    string currentHash = GeometryHash(
                        compile.CompiledData);
                    if (geometryHash != null &&
                        !string.Equals(
                            geometryHash,
                            currentHash,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"Performance scenario '{baseline.id}' produced non-deterministic geometry.");
                    }

                    geometryHash = currentHash;
                    UnityEngine.Object.DestroyImmediate(compile.Mesh);
                }

                Array.Sort(durations);
                double median = durations.Length % 2 == 1
                    ? durations[durations.Length / 2]
                    : (durations[durations.Length / 2 - 1] +
                       durations[durations.Length / 2]) * 0.5;
                bool countsMatch =
                    vertexCount == baseline.expectedVertices &&
                    triangleCount == baseline.expectedTriangles;
                return new FoldCanvasPerformanceScenarioResult
                {
                    id = baseline.id,
                    warmupIterations = warmupIterations,
                    measuredIterations = measuredIterations,
                    vertexCount = vertexCount,
                    triangleCount = triangleCount,
                    minimumMilliseconds = durations[0],
                    medianMilliseconds = median,
                    maximumMilliseconds = durations[durations.Length - 1],
                    geometrySha256 = geometryHash,
                    withinBaseline = countsMatch &&
                        median <= baseline.maximumMedianMilliseconds
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static FoldCanvasAsset CreateScenarioAsset(
            string id,
            out FoldCanvasOperationRegistry registry)
        {
            registry = null;
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = id;
            if (string.Equals(
                    id,
                    "planar-grid-64x32",
                    StringComparison.Ordinal))
            {
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    "panel",
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(2f, 1f),
                    64,
                    32));
                return asset;
            }

            if (string.Equals(
                    id,
                    "full-roll-64x8",
                    StringComparison.Ordinal))
            {
                const float radius = 0.5f;
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    "panel",
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(2f * Mathf.PI * radius, 1f),
                    64,
                    8));
                asset.Operations.Add(new RollOperationDefinition
                {
                    Id = "roll",
                    PanelId = "panel",
                    Direction = RollDirection.U,
                    AngleDegrees = 360f,
                    RadiusMode = RollRadiusMode.PreserveArcLength
                });
                return asset;
            }

            if (string.Equals(
                    id,
                    "registered-wave-48x24",
                    StringComparison.Ordinal))
            {
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    "surface",
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(2.4f, 1.4f),
                    48,
                    24));
                asset.Operations.Add(new M10WaveOperationDefinition
                {
                    Id = "wave",
                    PanelId = "surface",
                    Amplitude = 0.22f,
                    Cycles = 2.5f
                });
                registry = FoldCanvasSampleCreator.CreateM10Registry();
                return asset;
            }

            UnityEngine.Object.DestroyImmediate(asset);
            throw new InvalidDataException(
                $"Unknown performance scenario '{id}'.");
        }

        private static void EnsureSuccess(
            FoldCanvasCompileResult result,
            string scenarioId)
        {
            if (result.Success)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            throw new InvalidDataException(
                $"Performance scenario '{scenarioId}' failed:\n{builder}");
        }

        private static string GeometryHash(
            FoldCanvasCompiledData data)
        {
            string obj = FoldCanvasObjExporter.Export(
                data,
                new FoldCanvasObjExportOptions
                {
                    ObjectName = "Performance",
                    IncludeMetadataComments = false
                }).Content;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(
                    Encoding.UTF8.GetBytes(obj));
                StringBuilder builder =
                    new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static FoldCanvasPerformanceBaselineDocument
            LoadBaselines()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                "Packages/com.foldcanvas.core");
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package path.");
            }

            string path = Path.Combine(
                package.resolvedPath,
                BaselineRelativePath);
            FoldCanvasPerformanceBaselineDocument document =
                JsonUtility.FromJson<
                    FoldCanvasPerformanceBaselineDocument>(
                    File.ReadAllText(path));
            if (document == null ||
                document.format !=
                    "foldcanvas-performance-baselines" ||
                document.version != "1" ||
                document.scenarios == null ||
                document.scenarios.Length == 0)
            {
                throw new InvalidDataException(
                    "The FoldCanvas performance baseline document is invalid.");
            }

            return document;
        }

        [Serializable]
        private sealed class FoldCanvasPerformanceBaselineDocument
        {
            public string format;
            public string version;
            public string unityVersion;
            public FoldCanvasPerformanceBaseline[] scenarios;
        }

        [Serializable]
        private sealed class FoldCanvasPerformanceBaseline
        {
            public string id;
            public int warmupIterations;
            public int measuredIterations;
            public int expectedVertices;
            public int expectedTriangles;
            public double maximumMedianMilliseconds;
        }
    }
}
