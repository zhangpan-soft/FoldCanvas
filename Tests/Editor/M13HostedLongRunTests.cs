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
    public sealed class M13HostedLongRunTests
    {
        private const ulong LocalSeed = 0x4C4F4E4752554E31UL;

        [Test]
        public void LongRunReport_ContainsReplayIdentityForEveryUnexpectedCase()
        {
            FoldCanvasRobustnessReport report =
                new FoldCanvasRobustnessReport
                {
                    generatorVersion = "m13-splitmix64-v1",
                    semanticSha256 = new string('a', 64),
                    unexpectedCount = 1
                };
            report.cases.Add(new FoldCanvasRobustnessCaseResult
            {
                generatorVersion = "m13-splitmix64-v1",
                suiteId = "planar-valid",
                seedHex = "464f4c4443414e56",
                ordinal = 17,
                caseId =
                    "m13-splitmix64-v1/planar-valid/464f4c4443414e56/17",
                expectedSuccess = true,
                actualSuccess = false,
                exceptionType = "SyntheticUnexpectedResult",
                sourceSha256 = new string('b', 64),
                passed = false
            });

            FoldCanvasRobustnessReplayDocument replays =
                FoldCanvasRobustnessLongRunEvidence
                    .BuildReplayDocument(report);

            Assert.That(replays.unexpectedCount, Is.EqualTo(1));
            Assert.That(replays.records.Count, Is.EqualTo(1));
            Assert.That(
                FoldCanvasRobustnessLongRunEvidence
                    .HasCompleteReplayIdentity(replays.records[0]),
                Is.True);
            Assert.That(replays.records[0].suiteId,
                Is.EqualTo("planar-valid"));
            Assert.That(replays.records[0].seedHex,
                Is.EqualTo("464f4c4443414e56"));
            Assert.That(replays.records[0].ordinal, Is.EqualTo(17));
        }

        [Test]
        [Timeout(900000)]
        public void HostedLongRun_WritesCompleteEvidenceWhenRequested()
        {
            bool requested =
                FoldCanvasRobustnessLongRunEvidence.IsRequested();
            string[] assetPathsBefore = AssetDatabase.GetAllAssetPaths();
            int sceneRootCountBefore =
                SceneManager.GetActiveScene().rootCount;
            int[] meshIdsBefore = LoadedMeshIds();
            UnityEngine.Object[] selectionBefore = Selection.objects;

            TemporaryDirectory temporary = null;
            try
            {
                FoldCanvasRobustnessLongRunResult evidence;
                if (!requested)
                {
                    temporary = new TemporaryDirectory();
                    evidence = FoldCanvasRobustnessLongRunEvidence
                        .RunForTests(1, LocalSeed, temporary.Path);
                }
                else
                {
                    evidence = FoldCanvasRobustnessLongRunEvidence
                        .RunFromEnvironment();
                }

                Assert.That(evidence.Report.complete, Is.True);
                Assert.That(evidence.Report.caseCount,
                    Is.EqualTo(
                        evidence.Report.suiteCount *
                        evidence.Report.casesPerSuite));
                Assert.That(evidence.Report.passedCount,
                    Is.EqualTo(evidence.Report.caseCount));
                Assert.That(evidence.Report.unexpectedCount, Is.Zero);
                Assert.That(evidence.Report.semanticSha256.Length,
                    Is.EqualTo(64));
                Assert.That(evidence.Replays.unexpectedCount, Is.Zero);
                Assert.That(evidence.Replays.records, Is.Empty);
                Assert.That(evidence.ResourceReport.complete, Is.True);
                Assert.That(evidence.ResourceReport.scenarioCount,
                    Is.EqualTo(5));
                Assert.That(evidence.ResourceReport.passedScenarioCount,
                    Is.EqualTo(5));
                Assert.That(evidence.ResourceReport.failedScenarioCount,
                    Is.Zero);
                Assert.That(evidence.Environment.complete, Is.True);
                Assert.That(evidence.Environment.caseCount,
                    Is.EqualTo(evidence.Report.caseCount));
                Assert.That(evidence.Environment.reportSemanticSha256,
                    Is.EqualTo(evidence.Report.semanticSha256));
                Assert.That(evidence.Environment.resourceSemanticSha256,
                    Is.EqualTo(
                        evidence.ResourceReport.semanticSha256));

                string[] expectedFiles =
                {
                    FoldCanvasRobustnessLongRunEvidence
                        .EnvironmentFileName,
                    FoldCanvasRobustnessLongRunEvidence.ReplayFileName,
                    FoldCanvasRobustnessLongRunEvidence
                        .ResourceReportFileName,
                    FoldCanvasRobustnessLongRunEvidence
                        .RobustnessReportFileName
                };
                Assert.That(
                    Directory.GetFiles(evidence.EvidenceDirectory)
                        .Select(Path.GetFileName)
                        .OrderBy(item => item),
                    Is.EqualTo(expectedFiles.OrderBy(item => item)));
                foreach (string fileName in expectedFiles)
                {
                    Assert.That(
                        new FileInfo(Path.Combine(
                            evidence.EvidenceDirectory,
                            fileName)).Length,
                        Is.GreaterThan(0L));
                }

                Debug.Log(
                    "FoldCanvas M13 hosted long run evidence: " +
                    evidence.Report.passedCount + "/" +
                    evidence.Report.caseCount +
                    " cases; semantic SHA-256 " +
                    evidence.Report.semanticSha256 +
                    "; directory " +
                    evidence.EvidenceDirectory);
            }
            finally
            {
                temporary?.Dispose();
            }

            Assert.That(AssetDatabase.GetAllAssetPaths(),
                Is.EqualTo(assetPathsBefore));
            Assert.That(SceneManager.GetActiveScene().rootCount,
                Is.EqualTo(sceneRootCountBefore));
            Assert.That(Selection.objects, Is.EqualTo(selectionBefore));
            Assert.That(LoadedMeshIds(), Is.EqualTo(meshIdsBefore));
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
                    "foldcanvas-m13-long-run-" +
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

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
