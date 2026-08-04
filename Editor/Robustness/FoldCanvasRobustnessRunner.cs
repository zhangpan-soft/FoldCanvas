using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasRobustnessRunner
    {
        public const ulong DefaultSeed = 0x464F4C4443414E56UL;
        public const int DefaultCasesPerSuite = 16;
        public const int MaximumCasesPerSuite = 256;

        private const string ReportRelativePath =
            "Library/FoldCanvas/M13RobustnessSmokeReport.json";

        [MenuItem("Tools/FoldCanvas/Run M13 Robustness Smoke")]
        public static void RunFromMenu()
        {
            FoldCanvasRobustnessRunOutcome outcome;
            try
            {
                outcome = RunAndWrite(
                    DefaultCasesPerSuite,
                    DefaultSeed,
                    ResolveDefaultReportPath(),
                    progress => EditorUtility
                        .DisplayCancelableProgressBar(
                            "FoldCanvas M13 Robustness Smoke",
                            ProgressDescription(progress),
                            progress.TotalCaseCount > 0
                                ? progress.CompletedCaseCount /
                                  (float)progress.TotalCaseCount
                                : 0f));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            FoldCanvasRobustnessReport report = outcome.Report;
            if (outcome.Cancelled)
            {
                Debug.LogWarning(
                    $"FoldCanvas M13 robustness smoke cancelled between cases after {report.caseCount} completed cases. The last complete report was not replaced.");
                return;
            }

            string path = outcome.ReportPath;
            string message =
                $"FoldCanvas M13 robustness smoke completed: {report.passedCount}/{report.caseCount} passed; semantic SHA-256 {report.semanticSha256}. Derived report: {path}";
            if (report.unexpectedCount > 0)
            {
                Debug.LogError(message);
                throw new InvalidDataException(
                    $"M13 robustness smoke found {report.unexpectedCount} unexpected case results. Replay identities are in {path}.");
            }

            Debug.Log(message);
        }

        public static FoldCanvasRobustnessReport RunSmoke(
            int casesPerSuite = DefaultCasesPerSuite,
            ulong seed = DefaultSeed)
        {
            return RunSmokeCancellable(
                casesPerSuite,
                seed,
                null).Report;
        }

        internal static FoldCanvasRobustnessRunOutcome RunAndWrite(
            int casesPerSuite,
            ulong seed,
            string reportPath,
            Func<FoldCanvasRobustnessRunProgress, bool>
                cancellationRequested)
        {
            FoldCanvasRobustnessRunOutcome outcome =
                RunSmokeCancellable(
                    casesPerSuite,
                    seed,
                    cancellationRequested);
            if (outcome.Cancelled)
            {
                return outcome;
            }

            string writtenPath = WriteCompleteReportAtomically(
                outcome.Report,
                reportPath);
            return new FoldCanvasRobustnessRunOutcome(
                outcome.Report,
                false,
                writtenPath);
        }

        internal static FoldCanvasRobustnessRunOutcome
            RunSmokeCancellable(
                int casesPerSuite,
                ulong seed,
                Func<FoldCanvasRobustnessRunProgress, bool>
                    cancellationRequested)
        {
            if (casesPerSuite < 1 ||
                casesPerSuite > MaximumCasesPerSuite)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(casesPerSuite),
                    $"M13 smoke cases per suite must be between 1 and {MaximumCasesPerSuite}.");
            }

            FoldCanvasRobustnessReport report =
                new FoldCanvasRobustnessReport
                {
                    generatorVersion =
                        FoldCanvasRobustnessGenerator.GeneratorVersion,
                    packageVersion = FoldCanvasVersion.Package,
                    compilerVersion = FoldCanvasVersion.Compiler,
                    foldScriptVersion =
                        FoldCanvasSourceMetadata.CurrentSchemaVersion,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    seedHex = SeedHex(seed),
                    casesPerSuite = casesPerSuite,
                    suiteCount = FoldCanvasRobustnessGenerator
                        .SmokeSuiteIds.Length,
                    semanticSha256 = string.Empty,
                    complete = false
                };

            int totalCaseCount = checked(
                casesPerSuite * report.suiteCount);

            for (int suiteIndex = 0;
                suiteIndex < FoldCanvasRobustnessGenerator
                    .SmokeSuiteIds.Length;
                suiteIndex++)
            {
                string suiteId = FoldCanvasRobustnessGenerator
                    .SmokeSuiteIds[suiteIndex];
                for (int ordinal = 0;
                    ordinal < casesPerSuite;
                    ordinal++)
                {
                    if (CancellationRequested(
                            cancellationRequested,
                            new FoldCanvasRobustnessRunProgress(
                                FoldCanvasRobustnessRunPhase
                                    .BeforeCase,
                                report.cases.Count,
                                totalCaseCount,
                                suiteId,
                                ordinal)))
                    {
                        FinalizeCounts(report);
                        return new FoldCanvasRobustnessRunOutcome(
                            report,
                            true);
                    }

                    FoldCanvasRobustnessCaseResult caseResult =
                        RunCase(suiteId, seed, ordinal);
                    report.cases.Add(caseResult);
                    if (caseResult.passed)
                    {
                        report.passedCount++;
                    }
                    else
                    {
                        report.unexpectedCount++;
                    }
                }
            }

            FinalizeCounts(report);
            if (CancellationRequested(
                    cancellationRequested,
                    new FoldCanvasRobustnessRunProgress(
                        FoldCanvasRobustnessRunPhase
                            .BeforeReportReplace,
                        report.caseCount,
                        totalCaseCount,
                        string.Empty,
                        -1)))
            {
                return new FoldCanvasRobustnessRunOutcome(
                    report,
                    true);
            }

            report.complete = true;
            report.semanticSha256 = SemanticHash(report);
            return new FoldCanvasRobustnessRunOutcome(
                report,
                false);
        }

        internal static FoldCanvasRobustnessCaseResult RunCase(
            string suiteId,
            ulong seed,
            int ordinal)
        {
            FoldCanvasRobustnessGeneratedCase generated = null;
            FoldCanvasCompileResult compile = null;
            FoldCanvasRobustnessCaseResult result =
                new FoldCanvasRobustnessCaseResult
                {
                    generatorVersion =
                        FoldCanvasRobustnessGenerator.GeneratorVersion,
                    suiteId = suiteId ?? string.Empty,
                    seedHex = SeedHex(seed),
                    ordinal = ordinal,
                    caseId = string.Empty,
                    expectedDiagnosticCode = string.Empty,
                    actualFirstErrorCode = string.Empty,
                    sourceSha256 = string.Empty,
                    geometrySha256 = string.Empty,
                    diagnosticSha256 = string.Empty,
                    exceptionType = string.Empty,
                    exceptionMessage = string.Empty
                };

            try
            {
                generated = FoldCanvasRobustnessGenerator.Generate(
                    suiteId,
                    seed,
                    ordinal);
                result.caseId = generated.CaseId;
                result.expectedSuccess = generated.ExpectedSuccess;
                result.expectedDiagnosticCode =
                    generated.ExpectedDiagnosticCode;
                result.sourceSha256 = FoldCanvasHandoffHash.Text(
                    generated.CanonicalSource);

                compile = FoldCanvasCompiler.Compile(generated.Asset);
                string afterSource =
                    FoldCanvasRobustnessGenerator.CanonicalSource(
                        generated.Asset);
                result.sourceUnchanged = string.Equals(
                    generated.CanonicalSource,
                    afterSource,
                    StringComparison.Ordinal);
                result.actualSuccess = compile.Success;
                result.actualFirstErrorCode = FirstErrorCode(compile);
                result.diagnosticCount = compile.Diagnostics.Count;
                result.meshReturned = compile.Mesh != null;
                result.compiledDataReturned = compile.CompiledData != null;
                result.finiteBuffers = HasFiniteBuffers(compile);
                result.renderVertexCount =
                    compile.CompiledData?.Vertices.Count ?? 0;
                result.topologyVertexCount =
                    compile.CompiledData?.TopologyVertexCount ?? 0;
                result.triangleCount =
                    compile.CompiledData?.TriangleIndices.Count / 3 ?? 0;
                result.geometrySha256 =
                    FoldCanvasHandoffEvidenceBuilder.HashCompiledData(
                        compile.CompiledData);
                result.diagnosticSha256 =
                    FoldCanvasHandoffEvidenceBuilder.HashDiagnostics(
                        compile);
                result.passed = MatchesExpectation(result);
            }
            catch (Exception exception)
            {
                result.exceptionType = exception.GetType().FullName ??
                    exception.GetType().Name;
                result.exceptionMessage = exception.Message ?? string.Empty;
                result.passed = false;
            }
            finally
            {
                if (compile?.Mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(compile.Mesh);
                }

                generated?.Dispose();
            }

            return result;
        }

        internal static string SemanticHash(
            FoldCanvasRobustnessReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("foldcanvas-robustness-semantic");
            builder.AppendLine(report.version ?? string.Empty);
            builder.AppendLine(report.generatorVersion ?? string.Empty);
            builder.AppendLine(report.packageVersion ?? string.Empty);
            builder.AppendLine(report.compilerVersion ?? string.Empty);
            builder.AppendLine(report.foldScriptVersion ?? string.Empty);
            builder.AppendLine(report.seedHex ?? string.Empty);
            builder.AppendLine(report.casesPerSuite.ToString(
                CultureInfo.InvariantCulture));
            builder.AppendLine(report.suiteCount.ToString(
                CultureInfo.InvariantCulture));
            builder.AppendLine(report.caseCount.ToString(
                CultureInfo.InvariantCulture));
            for (int i = 0; i < report.cases.Count; i++)
            {
                FoldCanvasRobustnessCaseResult item = report.cases[i];
                Append(builder, item.generatorVersion);
                Append(builder, item.suiteId);
                Append(builder, item.seedHex);
                Append(builder, item.ordinal);
                Append(builder, item.caseId);
                Append(builder, item.expectedSuccess);
                Append(builder, item.expectedDiagnosticCode);
                Append(builder, item.actualSuccess);
                Append(builder, item.actualFirstErrorCode);
                Append(builder, item.diagnosticCount);
                Append(builder, item.meshReturned);
                Append(builder, item.compiledDataReturned);
                Append(builder, item.finiteBuffers);
                Append(builder, item.sourceUnchanged);
                Append(builder, item.renderVertexCount);
                Append(builder, item.topologyVertexCount);
                Append(builder, item.triangleCount);
                Append(builder, item.sourceSha256);
                Append(builder, item.geometrySha256);
                Append(builder, item.diagnosticSha256);
                Append(builder, item.exceptionType);
                Append(builder, item.passed);
            }

            return FoldCanvasHandoffHash.Text(builder.ToString());
        }

        internal static string WriteCompleteReportAtomically(
            FoldCanvasRobustnessReport report)
        {
            return WriteCompleteReportAtomically(
                report,
                ResolveDefaultReportPath());
        }

        internal static string WriteCompleteReportAtomically(
            FoldCanvasRobustnessReport report,
            string reportPath,
            Action beforeReplace = null)
        {
            if (report == null || !report.complete)
            {
                throw new InvalidDataException(
                    "Only a complete M13 robustness report may replace the last report.");
            }

            if (string.IsNullOrWhiteSpace(reportPath))
            {
                throw new ArgumentException(
                    "The M13 report path must be non-empty.",
                    nameof(reportPath));
            }

            string path = Path.GetFullPath(reportPath);
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidDataException(
                    "The M13 report path has no containing directory.");
            }

            Directory.CreateDirectory(directory);
            string temporary = Path.Combine(
                directory,
                "." + Path.GetFileName(path) + "." +
                Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(
                    temporary,
                    SerializeReport(report),
                    new UTF8Encoding(false));
                beforeReplace?.Invoke();
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }

                temporary = null;
                return path;
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporary) &&
                    File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        internal static string SerializeReport(
            FoldCanvasRobustnessReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            return JsonUtility.ToJson(report, true) + "\n";
        }

        private static string ResolveDefaultReportPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidDataException(
                    "Could not resolve the Unity project root for the M13 report.");
            }

            return Path.GetFullPath(Path.Combine(
                projectRoot,
                ReportRelativePath));
        }

        private static bool CancellationRequested(
            Func<FoldCanvasRobustnessRunProgress, bool>
                cancellationRequested,
            FoldCanvasRobustnessRunProgress progress)
        {
            return cancellationRequested != null &&
                cancellationRequested(progress);
        }

        private static void FinalizeCounts(
            FoldCanvasRobustnessReport report)
        {
            report.caseCount = report.cases.Count;
            report.complete = false;
            report.semanticSha256 = string.Empty;
        }

        private static string ProgressDescription(
            FoldCanvasRobustnessRunProgress progress)
        {
            if (progress.Phase ==
                FoldCanvasRobustnessRunPhase.BeforeReportReplace)
            {
                return "Preparing complete report replacement";
            }

            return $"Compiling {progress.SuiteId} case {progress.Ordinal}";
        }

        private static bool MatchesExpectation(
            FoldCanvasRobustnessCaseResult result)
        {
            if (!string.IsNullOrEmpty(result.exceptionType) ||
                !result.sourceUnchanged ||
                result.actualSuccess != result.expectedSuccess)
            {
                return false;
            }

            if (result.expectedSuccess)
            {
                return result.meshReturned &&
                    result.compiledDataReturned &&
                    result.finiteBuffers &&
                    string.IsNullOrEmpty(result.actualFirstErrorCode);
            }

            return !result.meshReturned &&
                !result.compiledDataReturned &&
                result.diagnosticCount == 1 &&
                !string.IsNullOrEmpty(result.expectedDiagnosticCode) &&
                string.Equals(
                    result.expectedDiagnosticCode,
                    result.actualFirstErrorCode,
                    StringComparison.Ordinal);
        }

        private static bool HasFiniteBuffers(
            FoldCanvasCompileResult result)
        {
            if (result == null)
            {
                return false;
            }

            if (!result.Success)
            {
                return result.Mesh == null && result.CompiledData == null;
            }

            FoldCanvasCompiledData data = result.CompiledData;
            Mesh mesh = result.Mesh;
            if (data == null || mesh == null ||
                data.Vertices.Count != mesh.vertexCount)
            {
                return false;
            }

            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                if (!Finite(vertex.Position) ||
                    !Finite(vertex.SourcePosition) ||
                    !Finite(vertex.SourceUv))
                {
                    return false;
                }
            }

            for (int i = 0; i < data.TriangleIndices.Count; i++)
            {
                int index = data.TriangleIndices[i];
                if (index < 0 || index >= data.Vertices.Count)
                {
                    return false;
                }
            }

            Vector3[] normals = mesh.normals;
            if (normals.Length != data.Vertices.Count)
            {
                return false;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                if (!Finite(normals[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string FirstErrorCode(
            FoldCanvasCompileResult result)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Severity ==
                    FoldCanvasDiagnosticSeverity.Error)
                {
                    return result.Diagnostics[i].Code ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string SeedHex(ulong seed)
        {
            return seed.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static bool Finite(Vector2 value)
        {
            return Finite(value.x) && Finite(value.y);
        }

        private static bool Finite(Vector3 value)
        {
            return Finite(value.x) && Finite(value.y) && Finite(value.z);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void Append(StringBuilder builder, string value)
        {
            builder.Append(value ?? string.Empty);
            builder.Append('\n');
        }

        private static void Append(StringBuilder builder, int value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
            builder.Append('\n');
        }

        private static void Append(StringBuilder builder, bool value)
        {
            builder.Append(value ? '1' : '0');
            builder.Append('\n');
        }
    }
}
