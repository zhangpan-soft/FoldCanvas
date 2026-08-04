using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal sealed class FoldCanvasRobustnessLongRunResult
    {
        public FoldCanvasRobustnessLongRunResult(
            FoldCanvasRobustnessReport report,
            FoldCanvasRobustnessReplayDocument replays,
            FoldCanvasRobustnessResourceReport resourceReport,
            FoldCanvasRobustnessLongRunEnvironment environment,
            string evidenceDirectory)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            Replays = replays ??
                throw new ArgumentNullException(nameof(replays));
            ResourceReport = resourceReport ??
                throw new ArgumentNullException(nameof(resourceReport));
            Environment = environment ??
                throw new ArgumentNullException(nameof(environment));
            EvidenceDirectory = evidenceDirectory ?? string.Empty;
        }

        public FoldCanvasRobustnessReport Report { get; }

        public FoldCanvasRobustnessReplayDocument Replays { get; }

        public FoldCanvasRobustnessResourceReport ResourceReport { get; }

        public FoldCanvasRobustnessLongRunEnvironment Environment { get; }

        public string EvidenceDirectory { get; }
    }

    public static class FoldCanvasRobustnessLongRunEvidence
    {
        internal const string CasesEnvironmentVariable =
            "FOLDCANVAS_M13_LONG_CASES_PER_SUITE";

        internal const string SeedEnvironmentVariable =
            "FOLDCANVAS_M13_LONG_SEED_HEX";

        internal const string DirectoryEnvironmentVariable =
            "FOLDCANVAS_M13_LONG_EVIDENCE_DIRECTORY";

        private const string CasesCommandLineArgument =
            "-foldCanvasM13CasesPerSuite";

        private const string SeedCommandLineArgument =
            "-foldCanvasM13SeedHex";

        private const string DirectoryCommandLineArgument =
            "-foldCanvasM13EvidenceDirectory";

        internal const string RobustnessReportFileName =
            "robustness-report.json";

        internal const string ReplayFileName = "replay-records.json";

        internal const string ResourceReportFileName =
            "resource-report.json";

        internal const string EnvironmentFileName = "environment.json";

        internal static FoldCanvasRobustnessLongRunResult
            RunFromEnvironment()
        {
            int casesPerSuite = ParseCasesPerSuite(
                ReadSetting(
                    CasesEnvironmentVariable,
                    CasesCommandLineArgument));
            ulong seed = ParseSeed(
                ReadSetting(
                    SeedEnvironmentVariable,
                    SeedCommandLineArgument));
            string directory = ResolveEvidenceDirectory(
                ReadSetting(
                    DirectoryEnvironmentVariable,
                    DirectoryCommandLineArgument));
            return Run(
                casesPerSuite,
                seed,
                directory,
                true);
        }

        internal static bool IsRequested()
        {
            return !string.IsNullOrWhiteSpace(ReadSetting(
                DirectoryEnvironmentVariable,
                DirectoryCommandLineArgument));
        }

        internal static FoldCanvasRobustnessLongRunResult RunForTests(
            int casesPerSuite,
            ulong seed,
            string evidenceDirectory)
        {
            return Run(
                casesPerSuite,
                seed,
                evidenceDirectory,
                false);
        }

        internal static FoldCanvasRobustnessReplayDocument
            BuildReplayDocument(FoldCanvasRobustnessReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            FoldCanvasRobustnessReplayDocument document =
                new FoldCanvasRobustnessReplayDocument
                {
                    generatorVersion = report.generatorVersion,
                    reportSemanticSha256 = report.semanticSha256
                };
            for (int i = 0; i < report.cases.Count; i++)
            {
                FoldCanvasRobustnessCaseResult item = report.cases[i];
                if (item.passed)
                {
                    continue;
                }

                document.records.Add(
                    new FoldCanvasRobustnessReplayRecord
                    {
                        generatorVersion = item.generatorVersion,
                        suiteId = item.suiteId,
                        seedHex = item.seedHex,
                        ordinal = item.ordinal,
                        caseId = item.caseId,
                        expectedSuccess = item.expectedSuccess,
                        expectedDiagnosticCode =
                            item.expectedDiagnosticCode,
                        actualSuccess = item.actualSuccess,
                        actualFirstErrorCode =
                            item.actualFirstErrorCode,
                        exceptionType = item.exceptionType,
                        exceptionMessage = item.exceptionMessage,
                        sourceSha256 = item.sourceSha256
                    });
            }

            document.unexpectedCount = document.records.Count;
            if (document.unexpectedCount != report.unexpectedCount)
            {
                throw new InvalidDataException(
                    "M13 report unexpected count does not match its failed cases.");
            }

            return document;
        }

        internal static bool HasCompleteReplayIdentity(
            FoldCanvasRobustnessReplayRecord record)
        {
            return record != null &&
                !string.IsNullOrWhiteSpace(record.generatorVersion) &&
                !string.IsNullOrWhiteSpace(record.suiteId) &&
                !string.IsNullOrWhiteSpace(record.seedHex) &&
                record.seedHex.Length == 16 &&
                record.ordinal >= 0;
        }

        private static FoldCanvasRobustnessLongRunResult Run(
            int casesPerSuite,
            ulong seed,
            string evidenceDirectory,
            bool useReviewedResourceIterations)
        {
            if (casesPerSuite < 1 ||
                casesPerSuite >
                    FoldCanvasRobustnessRunner.MaximumCasesPerSuite)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(casesPerSuite));
            }

            if (string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                throw new ArgumentException(
                    "The M13 long-run evidence directory must be non-empty.",
                    nameof(evidenceDirectory));
            }

            string directory = Path.GetFullPath(evidenceDirectory);
            Directory.CreateDirectory(directory);
            FoldCanvasRobustnessReport report =
                FoldCanvasRobustnessRunner.RunSmoke(
                    casesPerSuite,
                    seed);
            FoldCanvasRobustnessRunner.WriteCompleteReportAtomically(
                report,
                Path.Combine(directory, RobustnessReportFileName));

            FoldCanvasRobustnessReplayDocument replays =
                BuildReplayDocument(report);
            WriteJsonAtomically(
                replays,
                Path.Combine(directory, ReplayFileName));

            FoldCanvasRobustnessResourceReport resourceReport =
                useReviewedResourceIterations
                    ? FoldCanvasRobustnessResourceRunner.Run()
                    : FoldCanvasRobustnessResourceRunner
                        .RunForTests(0, 1);
            FoldCanvasRobustnessResourceRunner
                .WriteCompleteReportAtomically(
                    resourceReport,
                    Path.Combine(
                        directory,
                        ResourceReportFileName));

            FoldCanvasRobustnessLongRunEnvironment environment =
                new FoldCanvasRobustnessLongRunEnvironment
                {
                    packageVersion = report.packageVersion,
                    compilerVersion = report.compilerVersion,
                    foldScriptVersion = report.foldScriptVersion,
                    generatorVersion = report.generatorVersion,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    operatingSystem = SystemInfo.operatingSystem,
                    processorType = SystemInfo.processorType,
                    processorCount = SystemInfo.processorCount,
                    systemMemoryMegabytes = SystemInfo.systemMemorySize,
                    casesPerSuite = report.casesPerSuite,
                    suiteCount = report.suiteCount,
                    caseCount = report.caseCount,
                    seedHex = report.seedHex,
                    reportSemanticSha256 = report.semanticSha256,
                    resourceEnvelopeSha256 =
                        resourceReport.envelopeSha256,
                    resourceSemanticSha256 =
                        resourceReport.semanticSha256,
                    complete = report.complete &&
                        resourceReport.complete &&
                        replays.unexpectedCount ==
                            report.unexpectedCount
                };
            WriteJsonAtomically(
                environment,
                Path.Combine(directory, EnvironmentFileName));

            return new FoldCanvasRobustnessLongRunResult(
                report,
                replays,
                resourceReport,
                environment,
                directory);
        }

        private static int ParseCasesPerSuite(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return FoldCanvasRobustnessRunner.DefaultCasesPerSuite;
            }

            if (!int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int casesPerSuite) ||
                casesPerSuite < 1 ||
                casesPerSuite >
                    FoldCanvasRobustnessRunner.MaximumCasesPerSuite)
            {
                throw new InvalidDataException(
                    $"{CasesEnvironmentVariable} must be between 1 and {FoldCanvasRobustnessRunner.MaximumCasesPerSuite}.");
            }

            return casesPerSuite;
        }

        private static ulong ParseSeed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return FoldCanvasRobustnessRunner.DefaultSeed;
            }

            string normalized = value.StartsWith(
                "0x",
                StringComparison.OrdinalIgnoreCase)
                ? value.Substring(2)
                : value;
            if (normalized.Length != 16 ||
                !ulong.TryParse(
                    normalized,
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out ulong seed))
            {
                throw new InvalidDataException(
                    $"{SeedEnvironmentVariable} must be exactly 16 hexadecimal digits.");
            }

            return seed;
        }

        private static string ReadSetting(
            string environmentVariable,
            string commandLineArgument)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!string.Equals(
                        arguments[i],
                        commandLineArgument,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (i + 1 >= arguments.Length ||
                    string.IsNullOrWhiteSpace(arguments[i + 1]))
                {
                    throw new InvalidDataException(
                        commandLineArgument +
                        " requires a following value.");
                }

                return arguments[i + 1];
            }

            return Environment.GetEnvironmentVariable(
                environmentVariable);
        }

        private static string ResolveEvidenceDirectory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException(
                    $"{DirectoryEnvironmentVariable} must identify a derived evidence directory.");
            }

            if (Path.IsPathRooted(value))
            {
                return Path.GetFullPath(value);
            }

            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidDataException(
                    "Could not resolve the project root for M13 long-run evidence.");
            }

            return Path.GetFullPath(Path.Combine(projectRoot, value));
        }

        private static void WriteJsonAtomically(
            object value,
            string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidDataException(
                    "The M13 evidence path has no containing directory.");
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
                    JsonUtility.ToJson(value, true) + "\n",
                    new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }

                temporary = null;
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
    }
}
