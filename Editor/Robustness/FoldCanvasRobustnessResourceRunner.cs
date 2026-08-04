using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using Unity.Profiling;
using UnityEngine;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Editor
{
    [Serializable]
    public sealed class FoldCanvasRobustnessResourceScenarioResult
    {
        public string id;
        public int warmupIterations;
        public int measuredIterations;
        public int renderVertexCount;
        public int topologyVertexCount;
        public int triangleCount;
        public string geometrySha256;
        public double minimumMilliseconds;
        public double medianMilliseconds;
        public double maximumMilliseconds;
        public long minimumManagedBytes;
        public long medianManagedBytes;
        public long maximumManagedBytes;
        public string managedMeasurementMethod;
        public bool managedMeasurementAvailable;
        public double maximumMedianMilliseconds;
        public long maximumMedianManagedBytes;
        public bool countsMatch;
        public bool geometryHashMatches;
        public bool elapsedWithinEnvelope;
        public bool managedAllocationWithinEnvelope;
        public bool withinEnvelope;
    }

    [Serializable]
    public sealed class FoldCanvasRobustnessResourceReport
    {
        public string format = "foldcanvas-robustness-resource-report";
        public string version = "1";
        public string envelopeVersion;
        public string envelopeSha256;
        public string packageVersion;
        public string compilerVersion;
        public string foldScriptVersion;
        public string unityVersion;
        public string platform;
        public int scenarioCount;
        public int passedScenarioCount;
        public int failedScenarioCount;
        public bool complete;
        public string semanticSha256;
        public List<FoldCanvasRobustnessResourceScenarioResult> scenarios =
            new List<FoldCanvasRobustnessResourceScenarioResult>();
    }

    public static class FoldCanvasRobustnessResourceRunner
    {
        internal const string EnvelopeRelativePath =
            "Documentation~/m13-resource-envelopes.json";

        internal const string ReportRelativePath =
            "Library/FoldCanvas/M13ResourceReport.json";

        internal const string EnvelopeFormat =
            "foldcanvas-robustness-resource-envelopes";

        internal const string EnvelopeVersion = "1";

        private static readonly string[] ScenarioIds =
        {
            "large-planar",
            "large-cup",
            "large-sphere",
            "large-torus",
            "large-stitch"
        };

        [MenuItem("Tools/FoldCanvas/Run M13 Resource Envelopes")]
        public static void RunFromMenu()
        {
            FoldCanvasRobustnessResourceReport report = Run();
            string path = WriteCompleteReportAtomically(report);
            string message =
                "FoldCanvas M13 resource envelopes completed: " +
                report.passedScenarioCount + "/" +
                report.scenarioCount + " within reviewed envelopes. " +
                "Derived report: " + path;
            if (report.failedScenarioCount > 0)
            {
                UnityEngine.Debug.LogError(message);
                throw new InvalidDataException(
                    "M13 resource evidence exceeded a reviewed envelope. " +
                    "Inspect " + path + ".");
            }

            UnityEngine.Debug.Log(message);
        }

        public static FoldCanvasRobustnessResourceReport Run()
        {
            return RunBaselines(LoadEnvelope(), null, null);
        }

        internal static FoldCanvasRobustnessResourceReport RunForTests(
            int warmupIterations,
            int measuredIterations)
        {
            return RunBaselines(
                LoadEnvelope(),
                warmupIterations,
                measuredIterations);
        }

        internal static string SemanticHash(
            FoldCanvasRobustnessResourceReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            StringBuilder builder = new StringBuilder();
            Append(builder, report.format);
            Append(builder, report.version);
            Append(builder, report.envelopeVersion);
            Append(builder, report.envelopeSha256);
            Append(builder, report.packageVersion);
            Append(builder, report.compilerVersion);
            Append(builder, report.foldScriptVersion);
            Append(builder, report.scenarioCount);
            Append(builder, report.passedScenarioCount);
            Append(builder, report.failedScenarioCount);
            for (int i = 0; i < report.scenarios.Count; i++)
            {
                FoldCanvasRobustnessResourceScenarioResult scenario =
                    report.scenarios[i];
                Append(builder, scenario.id);
                Append(builder, scenario.warmupIterations);
                Append(builder, scenario.measuredIterations);
                Append(builder, scenario.renderVertexCount);
                Append(builder, scenario.topologyVertexCount);
                Append(builder, scenario.triangleCount);
                Append(builder, scenario.geometrySha256);
                Append(builder, scenario.maximumMedianMilliseconds);
                Append(builder, scenario.maximumMedianManagedBytes);
                Append(builder, scenario.managedMeasurementAvailable);
                Append(builder, scenario.countsMatch);
                Append(builder, scenario.geometryHashMatches);
                Append(builder, scenario.elapsedWithinEnvelope);
                Append(
                    builder,
                    scenario.managedAllocationWithinEnvelope);
                Append(builder, scenario.withinEnvelope);
            }

            return FoldCanvasHandoffHash.Text(builder.ToString());
        }

        internal static string SerializeReport(
            FoldCanvasRobustnessResourceReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            return JsonUtility.ToJson(report, true) + "\n";
        }

        internal static string WriteCompleteReportAtomically(
            FoldCanvasRobustnessResourceReport report,
            string reportPath = null,
            Action beforeReplace = null)
        {
            if (report == null || !report.complete)
            {
                throw new InvalidDataException(
                    "Only a complete M13 resource report may replace the last report.");
            }

            string path = string.IsNullOrWhiteSpace(reportPath)
                ? ResolveDefaultReportPath()
                : Path.GetFullPath(reportPath);
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidDataException(
                    "The M13 resource report path has no containing directory.");
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

        private static FoldCanvasRobustnessResourceReport RunBaselines(
            FoldCanvasRobustnessResourceEnvelopeDocument envelope,
            int? warmupOverride,
            int? measuredOverride)
        {
            ValidateEnvelope(envelope);
            FoldCanvasRobustnessResourceReport report =
                new FoldCanvasRobustnessResourceReport
                {
                    envelopeVersion = envelope.version,
                    envelopeSha256 = envelope.sha256,
                    packageVersion = FoldCanvasVersion.Package,
                    compilerVersion = FoldCanvasVersion.Compiler,
                    foldScriptVersion =
                        FoldCanvasSourceMetadata.CurrentSchemaVersion,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    complete = false,
                    semanticSha256 = string.Empty
                };

            for (int i = 0; i < envelope.scenarios.Length; i++)
            {
                FoldCanvasRobustnessResourceEnvelope scenario =
                    envelope.scenarios[i];
                FoldCanvasRobustnessResourceScenarioResult result =
                    RunScenario(
                        scenario,
                        warmupOverride ?? scenario.warmupIterations,
                        measuredOverride ?? scenario.measuredIterations);
                report.scenarios.Add(result);
                if (result.withinEnvelope)
                {
                    report.passedScenarioCount++;
                }
                else
                {
                    report.failedScenarioCount++;
                }
            }

            report.scenarioCount = report.scenarios.Count;
            report.complete = true;
            report.semanticSha256 = SemanticHash(report);
            return report;
        }

        private static FoldCanvasRobustnessResourceScenarioResult
            RunScenario(
                FoldCanvasRobustnessResourceEnvelope envelope,
                int warmupIterations,
                int measuredIterations)
        {
            if (warmupIterations < 0 || measuredIterations < 1)
            {
                throw new InvalidDataException(
                    "M13 resource iteration counts are invalid.");
            }

            ManagedAllocationMeasurementKind measurementKind =
                DetectManagedAllocationMeasurement();

            for (int iteration = 0;
                iteration < warmupIterations;
                iteration++)
            {
                RunObservation(envelope.id, measurementKind);
            }

            double[] durations = new double[measuredIterations];
            long[] allocations = new long[measuredIterations];
            FoldCanvasResourceObservation first = default;
            for (int iteration = 0;
                iteration < measuredIterations;
                iteration++)
            {
                FoldCanvasResourceObservation observation =
                    RunObservation(envelope.id, measurementKind);
                durations[iteration] = observation.Milliseconds;
                allocations[iteration] = observation.ManagedBytes;
                if (iteration == 0)
                {
                    first = observation;
                }
                else if (observation.RenderVertexCount !=
                        first.RenderVertexCount ||
                    observation.TopologyVertexCount !=
                        first.TopologyVertexCount ||
                    observation.TriangleCount != first.TriangleCount ||
                    !string.Equals(
                        observation.GeometrySha256,
                        first.GeometrySha256,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "M13 resource scenario '" + envelope.id +
                        "' changed geometry across measured iterations.");
                }
            }

            Array.Sort(durations);
            Array.Sort(allocations);
            double medianMilliseconds = Median(durations);
            long medianManagedBytes = Median(allocations);
            bool countsMatch =
                first.RenderVertexCount ==
                    envelope.expectedRenderVertices &&
                first.TopologyVertexCount ==
                    envelope.expectedTopologyVertices &&
                first.TriangleCount == envelope.expectedTriangles;
            bool hashMatches = string.Equals(
                first.GeometrySha256,
                envelope.expectedGeometrySha256,
                StringComparison.Ordinal);
            bool elapsedWithin = medianMilliseconds <=
                envelope.maximumMedianMilliseconds;
            bool measurementAvailable = allocations.All(item => item > 0L);
            bool allocationWithin = measurementAvailable &&
                medianManagedBytes <=
                    envelope.maximumMedianManagedBytes;
            return new FoldCanvasRobustnessResourceScenarioResult
            {
                id = envelope.id,
                warmupIterations = warmupIterations,
                measuredIterations = measuredIterations,
                renderVertexCount = first.RenderVertexCount,
                topologyVertexCount = first.TopologyVertexCount,
                triangleCount = first.TriangleCount,
                geometrySha256 = first.GeometrySha256,
                minimumMilliseconds = durations[0],
                medianMilliseconds = medianMilliseconds,
                maximumMilliseconds = durations[durations.Length - 1],
                minimumManagedBytes = allocations[0],
                medianManagedBytes = medianManagedBytes,
                maximumManagedBytes = allocations[allocations.Length - 1],
                managedMeasurementMethod =
                    ManagedMeasurementName(measurementKind),
                managedMeasurementAvailable = measurementAvailable,
                maximumMedianMilliseconds =
                    envelope.maximumMedianMilliseconds,
                maximumMedianManagedBytes =
                    envelope.maximumMedianManagedBytes,
                countsMatch = countsMatch,
                geometryHashMatches = hashMatches,
                elapsedWithinEnvelope = elapsedWithin,
                managedAllocationWithinEnvelope = allocationWithin,
                withinEnvelope = countsMatch &&
                    hashMatches &&
                    elapsedWithin &&
                    allocationWithin
            };
        }

        private static FoldCanvasResourceObservation RunObservation(
            string scenarioId,
            ManagedAllocationMeasurementKind measurementKind)
        {
            FoldCanvasAsset asset = CreateScenarioAsset(scenarioId);
            FoldCanvasCompileResult result = null;
            ProfilerRecorder allocationRecorder = default;
            try
            {
                string sourceBefore =
                    FoldCanvasRobustnessGenerator.CanonicalSource(asset);
                if (measurementKind ==
                    ManagedAllocationMeasurementKind.ManagedHeapGrowth)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                if (measurementKind ==
                    ManagedAllocationMeasurementKind.ProfilerCounter)
                {
                    allocationRecorder = ProfilerRecorder.StartNew(
                        ProfilerCategory.Memory,
                        "GC Allocated In Frame",
                        1);
                }

                Stopwatch stopwatch = new Stopwatch();
                long allocatedBefore = ManagedAllocationValue(
                    measurementKind,
                    allocationRecorder);
                stopwatch.Start();
                result = FoldCanvasCompiler.Compile(asset);
                stopwatch.Stop();
                long allocatedAfter = ManagedAllocationValue(
                    measurementKind,
                    allocationRecorder);
                EnsureSuccess(result, scenarioId);
                string sourceAfter =
                    FoldCanvasRobustnessGenerator.CanonicalSource(asset);
                if (!string.Equals(
                        sourceBefore,
                        sourceAfter,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        "M13 resource scenario '" + scenarioId +
                        "' mutated its source asset.");
                }

                return new FoldCanvasResourceObservation(
                    stopwatch.Elapsed.TotalMilliseconds,
                    Math.Max(0L, allocatedAfter - allocatedBefore),
                    result.CompiledData.Vertices.Count,
                    result.CompiledData.TopologyVertexCount,
                    result.CompiledData.TriangleIndices.Count / 3,
                    FoldCanvasHandoffEvidenceBuilder.HashCompiledData(
                        result.CompiledData));
            }
            finally
            {
                if (allocationRecorder.Valid)
                {
                    allocationRecorder.Dispose();
                }
                if (result?.Mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(result.Mesh);
                }

                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static ManagedAllocationMeasurementKind
            DetectManagedAllocationMeasurement()
        {
            long threadBefore = GC.GetAllocatedBytesForCurrentThread();
            byte[] threadProbe = new byte[64 * 1024];
            long threadAfter = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(threadProbe);
            if (threadAfter - threadBefore >= threadProbe.Length)
            {
                return ManagedAllocationMeasurementKind.ThreadAllocatedBytes;
            }

            using (ProfilerRecorder recorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory,
                "GC Allocated In Frame",
                1))
            {
                if (recorder.Valid)
                {
                    long profilerBefore = recorder.CurrentValue;
                    byte[] profilerProbe = new byte[64 * 1024];
                    long profilerAfter = recorder.CurrentValue;
                    GC.KeepAlive(profilerProbe);
                    if (profilerAfter - profilerBefore >=
                        profilerProbe.Length)
                    {
                        return ManagedAllocationMeasurementKind
                            .ProfilerCounter;
                    }
                }
            }

            return ManagedAllocationMeasurementKind.ManagedHeapGrowth;
        }

        private static long ManagedAllocationValue(
            ManagedAllocationMeasurementKind kind,
            ProfilerRecorder recorder)
        {
            switch (kind)
            {
                case ManagedAllocationMeasurementKind.ThreadAllocatedBytes:
                    return GC.GetAllocatedBytesForCurrentThread();
                case ManagedAllocationMeasurementKind.ProfilerCounter:
                    return recorder.Valid ? recorder.CurrentValue : 0L;
                default:
                    return GC.GetTotalMemory(false);
            }
        }

        private static string ManagedMeasurementName(
            ManagedAllocationMeasurementKind kind)
        {
            switch (kind)
            {
                case ManagedAllocationMeasurementKind.ThreadAllocatedBytes:
                    return "gc-thread-allocated-bytes";
                case ManagedAllocationMeasurementKind.ProfilerCounter:
                    return "profiler-gc-allocated-in-frame";
                default:
                    return "managed-live-heap-growth";
            }
        }

        private static FoldCanvasAsset CreateScenarioAsset(string id)
        {
            switch (id)
            {
                case "large-planar":
                    return FoldCanvasRobustnessScaleFixtures
                        .CreateLargePlanarAsset();
                case "large-cup":
                    return FoldCanvasRobustnessScaleFixtures
                        .CreateLargeCupAsset();
                case "large-sphere":
                    return FoldCanvasRobustnessScaleFixtures
                        .CreateLargeSphereAsset();
                case "large-torus":
                    return FoldCanvasRobustnessScaleFixtures
                        .CreateLargeTorusAsset();
                case "large-stitch":
                    return FoldCanvasRobustnessScaleFixtures
                        .CreateLargeStitchAsset();
                default:
                    throw new InvalidDataException(
                        "Unknown M13 resource scenario '" + id + "'.");
            }
        }

        private static void EnsureSuccess(
            FoldCanvasCompileResult result,
            string scenarioId)
        {
            if (result != null && result.Success)
            {
                return;
            }

            string diagnostics = result == null
                ? "null result"
                : string.Join(" | ", result.Diagnostics.Select(
                    item => item.ToString()));
            throw new InvalidDataException(
                "M13 resource scenario '" + scenarioId +
                "' failed: " + diagnostics);
        }

        private static FoldCanvasRobustnessResourceEnvelopeDocument
            LoadEnvelope()
        {
            PackageManagerInfo package =
                PackageManagerInfo.FindForAssembly(
                    typeof(FoldCanvasRobustnessResourceRunner).Assembly);
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package root for M13 resource evidence.");
            }

            string path = Path.Combine(
                package.resolvedPath,
                EnvelopeRelativePath);
            string json = File.ReadAllText(path);
            FoldCanvasRobustnessResourceEnvelopeDocument envelope =
                JsonUtility.FromJson<
                    FoldCanvasRobustnessResourceEnvelopeDocument>(json);
            if (envelope != null)
            {
                envelope.sha256 = FoldCanvasHandoffHash.Text(json);
            }

            return envelope;
        }

        private static void ValidateEnvelope(
            FoldCanvasRobustnessResourceEnvelopeDocument envelope)
        {
            if (envelope == null ||
                !string.Equals(
                    envelope.format,
                    EnvelopeFormat,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    envelope.version,
                    EnvelopeVersion,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    envelope.unityVersion,
                    "6000.3.20f1",
                    StringComparison.Ordinal) ||
                envelope.scenarios == null ||
                envelope.scenarios.Length != ScenarioIds.Length)
            {
                throw new InvalidDataException(
                    "The M13 resource envelope document is invalid.");
            }

            for (int i = 0; i < envelope.scenarios.Length; i++)
            {
                FoldCanvasRobustnessResourceEnvelope scenario =
                    envelope.scenarios[i];
                if (scenario == null ||
                    !string.Equals(
                        scenario.id,
                        ScenarioIds[i],
                        StringComparison.Ordinal) ||
                    scenario.warmupIterations < 0 ||
                    scenario.measuredIterations < 1 ||
                    scenario.expectedRenderVertices < 1 ||
                    scenario.expectedTopologyVertices < 1 ||
                    scenario.expectedTriangles < 1 ||
                    string.IsNullOrEmpty(
                        scenario.expectedGeometrySha256) ||
                    scenario.expectedGeometrySha256.Length != 64 ||
                    scenario.maximumMedianMilliseconds <= 0d ||
                    scenario.maximumMedianManagedBytes <= 0L)
                {
                    throw new InvalidDataException(
                        "M13 resource envelope scenario " + i +
                        " is invalid.");
                }
            }
        }

        private static string ResolveDefaultReportPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidDataException(
                    "Could not resolve the Unity project root for the M13 resource report.");
            }

            return Path.GetFullPath(Path.Combine(
                projectRoot,
                ReportRelativePath));
        }

        private static double Median(double[] sortedValues)
        {
            return sortedValues.Length % 2 == 1
                ? sortedValues[sortedValues.Length / 2]
                : (sortedValues[sortedValues.Length / 2 - 1] +
                   sortedValues[sortedValues.Length / 2]) * 0.5d;
        }

        private static long Median(long[] sortedValues)
        {
            if (sortedValues.Length % 2 == 1)
            {
                return sortedValues[sortedValues.Length / 2];
            }

            long lower = sortedValues[sortedValues.Length / 2 - 1];
            long upper = sortedValues[sortedValues.Length / 2];
            return lower + (upper - lower) / 2L;
        }

        private static void Append(StringBuilder builder, object value)
        {
            builder.Append(
                Convert.ToString(value, CultureInfo.InvariantCulture) ??
                string.Empty);
            builder.Append('\n');
        }

        private readonly struct FoldCanvasResourceObservation
        {
            public FoldCanvasResourceObservation(
                double milliseconds,
                long managedBytes,
                int renderVertexCount,
                int topologyVertexCount,
                int triangleCount,
                string geometrySha256)
            {
                Milliseconds = milliseconds;
                ManagedBytes = managedBytes;
                RenderVertexCount = renderVertexCount;
                TopologyVertexCount = topologyVertexCount;
                TriangleCount = triangleCount;
                GeometrySha256 = geometrySha256;
            }

            public double Milliseconds { get; }

            public long ManagedBytes { get; }

            public int RenderVertexCount { get; }

            public int TopologyVertexCount { get; }

            public int TriangleCount { get; }

            public string GeometrySha256 { get; }
        }

        private enum ManagedAllocationMeasurementKind
        {
            ThreadAllocatedBytes = 0,
            ProfilerCounter = 1,
            ManagedHeapGrowth = 2
        }

        [Serializable]
        private sealed class FoldCanvasRobustnessResourceEnvelopeDocument
        {
            public string format;
            public string version;
            public string unityVersion;
            public FoldCanvasRobustnessResourceEnvelope[] scenarios;

            [NonSerialized]
            public string sha256;
        }

        [Serializable]
        private sealed class FoldCanvasRobustnessResourceEnvelope
        {
            public string id;
            public int warmupIterations;
            public int measuredIterations;
            public int expectedRenderVertices;
            public int expectedTopologyVertices;
            public int expectedTriangles;
            public string expectedGeometrySha256;
            public double maximumMedianMilliseconds;
            public long maximumMedianManagedBytes;
        }
    }
}
