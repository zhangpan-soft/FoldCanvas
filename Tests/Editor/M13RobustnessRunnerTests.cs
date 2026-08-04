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
    public sealed class M13RobustnessRunnerTests
    {
        private const ulong BaselineSeed = 0x424153454C494E45UL;
        private const ulong RetrySeed = 0x524554525952554EUL;

        [Test]
        public void RobustnessRun_CancelBetweenCasesLeavesAssetsAndLastReportUnchanged()
        {
            using (TemporaryDirectory temporary =
                new TemporaryDirectory())
            {
                string reportPath = temporary.File("report.json");
                FoldCanvasRobustnessReport baseline =
                    FoldCanvasRobustnessRunner.RunSmoke(
                        1,
                        BaselineSeed);
                FoldCanvasRobustnessRunner
                    .WriteCompleteReportAtomically(
                        baseline,
                        reportPath);
                byte[] reportBefore = File.ReadAllBytes(reportPath);
                string[] assetPathsBefore =
                    AssetDatabase.GetAllAssetPaths();
                int sceneRootCountBefore =
                    SceneManager.GetActiveScene().rootCount;
                int[] meshIdsBefore = LoadedMeshIds();
                UnityEngine.Object[] previousSelection =
                    Selection.objects;
                FoldCanvasAsset selectionSentinel =
                    ScriptableObject.CreateInstance<FoldCanvasAsset>();
                try
                {
                    Selection.activeObject = selectionSentinel;

                    FoldCanvasRobustnessRunOutcome outcome =
                        FoldCanvasRobustnessRunner.RunAndWrite(
                            2,
                            RetrySeed,
                            reportPath,
                            progress =>
                                progress.Phase ==
                                    FoldCanvasRobustnessRunPhase
                                        .BeforeCase &&
                                progress.CompletedCaseCount == 3);

                    Assert.That(outcome.Cancelled, Is.True);
                    Assert.That(outcome.Report.complete, Is.False);
                    Assert.That(outcome.Report.caseCount, Is.EqualTo(3));
                    Assert.That(outcome.Report.passedCount, Is.EqualTo(3));
                    Assert.That(outcome.Report.unexpectedCount, Is.Zero);
                    Assert.That(outcome.Report.semanticSha256, Is.Empty);
                    Assert.That(outcome.ReportPath, Is.Empty);
                    Assert.That(
                        File.ReadAllBytes(reportPath),
                        Is.EqualTo(reportBefore));
                    Assert.That(
                        AssetDatabase.GetAllAssetPaths(),
                        Is.EqualTo(assetPathsBefore));
                    Assert.That(
                        SceneManager.GetActiveScene().rootCount,
                        Is.EqualTo(sceneRootCountBefore));
                    Assert.That(
                        Selection.activeObject,
                        Is.SameAs(selectionSentinel));
                    Assert.That(
                        LoadedMeshIds(),
                        Is.EqualTo(meshIdsBefore));
                }
                finally
                {
                    Selection.objects = previousSelection;
                    UnityEngine.Object.DestroyImmediate(
                        selectionSentinel);
                }
            }
        }

        [Test]
        public void RobustnessRun_CancelBeforeReportReplaceLeavesLastReportUnchanged()
        {
            using (TemporaryDirectory temporary =
                new TemporaryDirectory())
            {
                string reportPath = temporary.File("report.json");
                FoldCanvasRobustnessRunner
                    .WriteCompleteReportAtomically(
                        FoldCanvasRobustnessRunner.RunSmoke(
                            1,
                            BaselineSeed),
                        reportPath);
                byte[] reportBefore = File.ReadAllBytes(reportPath);

                FoldCanvasRobustnessRunOutcome outcome =
                    FoldCanvasRobustnessRunner.RunAndWrite(
                        2,
                        RetrySeed,
                        reportPath,
                        progress => progress.Phase ==
                            FoldCanvasRobustnessRunPhase
                                .BeforeReportReplace);

                Assert.That(outcome.Cancelled, Is.True);
                Assert.That(outcome.Report.caseCount, Is.EqualTo(8));
                Assert.That(outcome.Report.passedCount, Is.EqualTo(8));
                Assert.That(outcome.Report.unexpectedCount, Is.Zero);
                Assert.That(outcome.Report.complete, Is.False);
                Assert.That(outcome.Report.semanticSha256, Is.Empty);
                Assert.That(outcome.ReportPath, Is.Empty);
                Assert.That(
                    File.ReadAllBytes(reportPath),
                    Is.EqualTo(reportBefore));
            }
        }

        [Test]
        public void RobustnessRun_RetryAfterCancellationMatchesCleanRun()
        {
            FoldCanvasRobustnessReport clean =
                FoldCanvasRobustnessRunner.RunSmoke(3, RetrySeed);
            FoldCanvasRobustnessRunOutcome cancelled =
                FoldCanvasRobustnessRunner.RunSmokeCancellable(
                    3,
                    RetrySeed,
                    progress =>
                        progress.Phase ==
                            FoldCanvasRobustnessRunPhase.BeforeCase &&
                        progress.CompletedCaseCount == 5);
            FoldCanvasRobustnessReport retry =
                FoldCanvasRobustnessRunner.RunSmoke(3, RetrySeed);

            Assert.That(cancelled.Cancelled, Is.True);
            Assert.That(cancelled.Report.caseCount, Is.EqualTo(5));
            Assert.That(cancelled.Report.complete, Is.False);
            Assert.That(cancelled.Report.semanticSha256, Is.Empty);
            Assert.That(retry.semanticSha256,
                Is.EqualTo(clean.semanticSha256));
            Assert.That(
                FoldCanvasRobustnessRunner.SerializeReport(retry),
                Is.EqualTo(
                    FoldCanvasRobustnessRunner.SerializeReport(clean)));
        }

        [Test]
        public void RobustnessReport_CanonicalProjectionIsByteStable()
        {
            FoldCanvasRobustnessReport first =
                FoldCanvasRobustnessRunner.RunSmoke(2, BaselineSeed);
            FoldCanvasRobustnessReport second =
                FoldCanvasRobustnessRunner.RunSmoke(2, BaselineSeed);

            Assert.That(second.semanticSha256,
                Is.EqualTo(first.semanticSha256));
            Assert.That(
                FoldCanvasRobustnessRunner.SerializeReport(second),
                Is.EqualTo(
                    FoldCanvasRobustnessRunner.SerializeReport(first)));
        }

        [Test]
        public void RobustnessRun_PersistenceFailureLeavesLastReportUnchanged()
        {
            using (TemporaryDirectory temporary =
                new TemporaryDirectory())
            {
                string reportPath = temporary.File("report.json");
                FoldCanvasRobustnessRunner
                    .WriteCompleteReportAtomically(
                        FoldCanvasRobustnessRunner.RunSmoke(
                            1,
                            BaselineSeed),
                        reportPath);
                byte[] reportBefore = File.ReadAllBytes(reportPath);
                FoldCanvasRobustnessReport replacement =
                    FoldCanvasRobustnessRunner.RunSmoke(
                        1,
                        RetrySeed);

                Assert.Throws<IOException>(() =>
                    FoldCanvasRobustnessRunner
                        .WriteCompleteReportAtomically(
                            replacement,
                            reportPath,
                            () => throw new IOException(
                                "Injected M13 persistence failure.")));

                Assert.That(
                    File.ReadAllBytes(reportPath),
                    Is.EqualTo(reportBefore));
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
                    "foldcanvas-m13-runner-" +
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
