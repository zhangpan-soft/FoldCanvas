using System;
using System.Collections.Generic;

namespace FoldCanvas.Editor
{
    internal enum FoldCanvasRobustnessRunPhase
    {
        BeforeCase = 0,
        BeforeReportReplace = 1
    }

    internal readonly struct FoldCanvasRobustnessRunProgress
    {
        public FoldCanvasRobustnessRunProgress(
            FoldCanvasRobustnessRunPhase phase,
            int completedCaseCount,
            int totalCaseCount,
            string suiteId,
            int ordinal)
        {
            Phase = phase;
            CompletedCaseCount = completedCaseCount;
            TotalCaseCount = totalCaseCount;
            SuiteId = suiteId ?? string.Empty;
            Ordinal = ordinal;
        }

        public FoldCanvasRobustnessRunPhase Phase { get; }

        public int CompletedCaseCount { get; }

        public int TotalCaseCount { get; }

        public string SuiteId { get; }

        public int Ordinal { get; }
    }

    internal sealed class FoldCanvasRobustnessRunOutcome
    {
        public FoldCanvasRobustnessRunOutcome(
            FoldCanvasRobustnessReport report,
            bool cancelled,
            string reportPath = null)
        {
            Report = report ??
                throw new ArgumentNullException(nameof(report));
            Cancelled = cancelled;
            ReportPath = reportPath ?? string.Empty;
        }

        public FoldCanvasRobustnessReport Report { get; }

        public bool Cancelled { get; }

        public string ReportPath { get; }
    }

    [Serializable]
    public sealed class FoldCanvasRobustnessCaseResult
    {
        public string generatorVersion;
        public string suiteId;
        public string seedHex;
        public int ordinal;
        public string caseId;
        public bool expectedSuccess;
        public string expectedDiagnosticCode;
        public bool actualSuccess;
        public string actualFirstErrorCode;
        public int diagnosticCount;
        public bool meshReturned;
        public bool compiledDataReturned;
        public bool finiteBuffers;
        public bool sourceUnchanged;
        public int renderVertexCount;
        public int topologyVertexCount;
        public int triangleCount;
        public string sourceSha256;
        public string geometrySha256;
        public string diagnosticSha256;
        public string exceptionType;
        public string exceptionMessage;
        public bool passed;
    }

    [Serializable]
    public sealed class FoldCanvasRobustnessReport
    {
        public string format = "foldcanvas-robustness-report";
        public string version = "1";
        public string generatorVersion;
        public string packageVersion;
        public string compilerVersion;
        public string foldScriptVersion;
        public string unityVersion;
        public string platform;
        public string seedHex;
        public int casesPerSuite;
        public int suiteCount;
        public int caseCount;
        public int passedCount;
        public int unexpectedCount;
        public bool complete;
        public string semanticSha256;
        public List<FoldCanvasRobustnessCaseResult> cases =
            new List<FoldCanvasRobustnessCaseResult>();
    }

    internal sealed class FoldCanvasRobustnessGeneratedCase : IDisposable
    {
        public FoldCanvasRobustnessGeneratedCase(
            string generatorVersion,
            string suiteId,
            ulong seed,
            int ordinal,
            string caseId,
            FoldCanvasAsset asset,
            string canonicalSource,
            bool expectedSuccess,
            string expectedDiagnosticCode)
        {
            GeneratorVersion = generatorVersion;
            SuiteId = suiteId;
            Seed = seed;
            Ordinal = ordinal;
            CaseId = caseId;
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            CanonicalSource = canonicalSource ??
                throw new ArgumentNullException(nameof(canonicalSource));
            ExpectedSuccess = expectedSuccess;
            ExpectedDiagnosticCode = expectedDiagnosticCode ?? string.Empty;
        }

        public string GeneratorVersion { get; }

        public string SuiteId { get; }

        public ulong Seed { get; }

        public int Ordinal { get; }

        public string CaseId { get; }

        public FoldCanvasAsset Asset { get; private set; }

        public string CanonicalSource { get; }

        public bool ExpectedSuccess { get; }

        public string ExpectedDiagnosticCode { get; }

        public void Dispose()
        {
            if (Asset != null)
            {
                UnityEngine.Object.DestroyImmediate(Asset);
                Asset = null;
            }
        }
    }
}
