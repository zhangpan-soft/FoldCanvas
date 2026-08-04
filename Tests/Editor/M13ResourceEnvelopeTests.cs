using System;
using System.IO;
using System.Linq;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Tests
{
    public sealed class M13ResourceEnvelopeTests
    {
        [Test]
        [Timeout(120000)]
        public void RobustnessReport_TimeAndAllocationEnvelopesAreExplicit()
        {
            string[] assetPathsBefore = AssetDatabase.GetAllAssetPaths();
            int sceneRootCountBefore =
                SceneManager.GetActiveScene().rootCount;
            int[] meshIdsBefore = LoadedMeshIds();
            UnityEngine.Object[] selectionBefore = Selection.objects;

            FoldCanvasRobustnessResourceReport report =
                FoldCanvasRobustnessResourceRunner.RunForTests(0, 1);

            Assert.That(report.complete, Is.True);
            Assert.That(report.envelopeVersion, Is.EqualTo("1"));
            Assert.That(report.envelopeSha256.Length, Is.EqualTo(64));
            Assert.That(report.scenarioCount, Is.EqualTo(5));
            Assert.That(report.scenarios.Count,
                Is.EqualTo(report.scenarioCount));
            Assert.That(report.passedScenarioCount,
                Is.EqualTo(report.scenarioCount));
            Assert.That(report.failedScenarioCount, Is.Zero);
            Assert.That(report.semanticSha256.Length, Is.EqualTo(64));
            Assert.That(report.scenarios.Select(item => item.id),
                Is.EqualTo(new[]
                {
                    "large-planar",
                    "large-cup",
                    "large-sphere",
                    "large-torus",
                    "large-stitch"
                }));
            foreach (FoldCanvasRobustnessResourceScenarioResult scenario
                in report.scenarios)
            {
                Assert.That(scenario.warmupIterations, Is.Zero);
                Assert.That(scenario.measuredIterations, Is.EqualTo(1));
                Assert.That(scenario.minimumMilliseconds,
                    Is.GreaterThanOrEqualTo(0d));
                Assert.That(scenario.minimumMilliseconds,
                    Is.EqualTo(scenario.medianMilliseconds));
                Assert.That(scenario.maximumMilliseconds,
                    Is.EqualTo(scenario.medianMilliseconds));
                Assert.That(scenario.minimumManagedBytes,
                    Is.GreaterThan(0L));
                Assert.That(scenario.minimumManagedBytes,
                    Is.EqualTo(scenario.medianManagedBytes));
                Assert.That(scenario.maximumManagedBytes,
                    Is.EqualTo(scenario.medianManagedBytes));
                Assert.That(scenario.maximumMedianMilliseconds,
                    Is.GreaterThan(0d));
                Assert.That(scenario.maximumMedianManagedBytes,
                    Is.GreaterThan(0L));
                Assert.That(scenario.managedMeasurementMethod,
                    Is.Not.Empty);
                Assert.That(scenario.managedMeasurementAvailable,
                    Is.True);
                Assert.That(scenario.countsMatch, Is.True);
                Assert.That(scenario.geometryHashMatches, Is.True);
                Assert.That(scenario.elapsedWithinEnvelope, Is.True);
                Assert.That(
                    scenario.managedAllocationWithinEnvelope,
                    Is.True);
                Assert.That(scenario.withinEnvelope, Is.True);
                Debug.Log(
                    "FoldCanvas M13 resource evidence: " + scenario.id +
                    " milliseconds=" + scenario.medianMilliseconds +
                    " managedBytes=" + scenario.medianManagedBytes +
                    " method=" + scenario.managedMeasurementMethod);
            }

            Assert.That(AssetDatabase.GetAllAssetPaths(),
                Is.EqualTo(assetPathsBefore));
            Assert.That(SceneManager.GetActiveScene().rootCount,
                Is.EqualTo(sceneRootCountBefore));
            Assert.That(Selection.objects, Is.EqualTo(selectionBefore));
            Assert.That(LoadedMeshIds(), Is.EqualTo(meshIdsBefore));
        }

        [Test]
        [Timeout(120000)]
        public void ResourceReport_RawObservationsDoNotChangeSemanticProjection()
        {
            FoldCanvasRobustnessResourceReport report =
                FoldCanvasRobustnessResourceRunner.RunForTests(0, 1);
            string original =
                FoldCanvasRobustnessResourceRunner.SemanticHash(report);
            FoldCanvasRobustnessResourceReport changed =
                JsonUtility.FromJson<FoldCanvasRobustnessResourceReport>(
                    FoldCanvasRobustnessResourceRunner.SerializeReport(
                        report));
            FoldCanvasRobustnessResourceScenarioResult scenario =
                changed.scenarios[0];
            scenario.minimumMilliseconds += 100000d;
            scenario.medianMilliseconds += 100000d;
            scenario.maximumMilliseconds += 100000d;
            scenario.minimumManagedBytes += 1000000000L;
            scenario.medianManagedBytes += 1000000000L;
            scenario.maximumManagedBytes += 1000000000L;

            Assert.That(
                FoldCanvasRobustnessResourceRunner.SemanticHash(changed),
                Is.EqualTo(original));

            scenario.elapsedWithinEnvelope = false;
            scenario.withinEnvelope = false;
            Assert.That(
                FoldCanvasRobustnessResourceRunner.SemanticHash(changed),
                Is.Not.EqualTo(original));
        }

        [Test]
        public void ResourceReport_PersistenceFailurePreservesPreviousBytes()
        {
            using (TemporaryDirectory temporary =
                new TemporaryDirectory())
            {
                string reportPath = temporary.File("resource.json");
                FoldCanvasRobustnessResourceReport baseline =
                    new FoldCanvasRobustnessResourceReport
                    {
                        complete = true,
                        semanticSha256 = "baseline"
                    };
                FoldCanvasRobustnessResourceRunner
                    .WriteCompleteReportAtomically(
                        baseline,
                        reportPath);
                byte[] bytesBefore = File.ReadAllBytes(reportPath);
                FoldCanvasRobustnessResourceReport replacement =
                    new FoldCanvasRobustnessResourceReport
                    {
                        complete = true,
                        semanticSha256 = "replacement"
                    };

                Assert.Throws<IOException>(() =>
                    FoldCanvasRobustnessResourceRunner
                        .WriteCompleteReportAtomically(
                            replacement,
                            reportPath,
                            () => throw new IOException(
                                "Injected resource persistence failure.")));

                Assert.That(
                    File.ReadAllBytes(reportPath),
                    Is.EqualTo(bytesBefore));
                Assert.That(
                    Directory.GetFiles(temporary.Path),
                    Is.EqualTo(new[] { reportPath }));
            }
        }

        private static int[] LoadedMeshIds()
        {
            return Resources.FindObjectsOfTypeAll<Mesh>()
                .Select(item => item.GetInstanceID())
                .OrderBy(item => item)
                .ToArray();
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "foldcanvas-m13-resource-" +
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public string File(string name)
            {
                return System.IO.Path.Combine(Path, name);
            }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }
}
