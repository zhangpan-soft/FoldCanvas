using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FoldCanvas.Editor;
using NUnit.Framework;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Tests
{
    public sealed class M11ProductionCorpusTests
    {
        [Test]
        public void ProductionCorpus_MatchesVersionedDeterministicBaseline()
        {
            FoldCanvasProductionCorpusDocument report =
                FoldCanvasProductionCorpusRunner.RunAndValidate();

            Assert.That(report.cases.Count, Is.EqualTo(6));
            Assert.That(
                report.cases.Select(item => item.id),
                Is.EqualTo(new[]
                {
                    "cyclic-torus",
                    "invalid-off-grid-fold",
                    "planar-artwork",
                    "production-cup",
                    "registered-wave",
                    "sphere-gores",
                }));
            Assert.That(
                report.cases.Select(item => item.validationLevel)
                    .Distinct(StringComparer.Ordinal),
                Is.EquivalentTo(new[] { "Basic", "Standard", "Strict" }));
            Assert.That(report.cases.Count(item => item.success),
                Is.EqualTo(5));
            FoldCanvasProductionCorpusCase invalid = report.cases.Single(
                item => item.id == "invalid-off-grid-fold");
            Assert.That(invalid.success, Is.False);
            Assert.That(invalid.renderVertices, Is.Zero);
            Assert.That(
                invalid.errorDiagnosticCode,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .FoldCreaseRequiresTopologySplit));
        }

        [Test]
        public void ProductionCorpus_BaselineIsOrdinalAndComplete()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            string path = Path.Combine(
                package.resolvedPath,
                FoldCanvasProductionCorpusRunner.BaselineRelativePath);
            FoldCanvasProductionCorpusDocument baseline =
                FoldCanvasProductionCorpusRunner.Parse(
                    File.ReadAllText(path));

            Assert.That(baseline.format,
                Is.EqualTo(FoldCanvasProductionCorpusRunner.BaselineFormat));
            Assert.That(baseline.version,
                Is.EqualTo(FoldCanvasProductionCorpusRunner.CorpusVersion));
            Assert.That(baseline.packageVersion,
                Is.EqualTo(FoldCanvasVersion.Package));
            Assert.That(baseline.foldScriptVersion,
                Is.EqualTo(FoldCanvasSourceMetadata.CurrentSchemaVersion));
            Assert.That(baseline.unityVersion, Is.EqualTo("6000.3.20f1"));
            Assert.That(
                baseline.cases.Select(item => item.id),
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(
                baseline.cases.Select(item => item.id)
                    .Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(baseline.cases.Count));
            Assert.That(
                baseline.cases.All(item =>
                    item.sourceSha256?.Length == 64 &&
                    item.geometrySha256?.Length == 64 &&
                    item.objSha256?.Length == 64 &&
                    item.diagnosticSha256?.Length == 64),
                Is.True);
        }

        [Test]
        public void ProductionCorpus_DifferenceReportsChangedCase()
        {
            FoldCanvasProductionCorpusDocument expected =
                new FoldCanvasProductionCorpusDocument
                {
                    packageVersion = "1",
                    foldScriptVersion = "0.1",
                    unityVersion = "6000.3.20f1",
                    cases = new List<FoldCanvasProductionCorpusCase>
                    {
                        new FoldCanvasProductionCorpusCase
                        {
                            id = "case",
                            sourceSha256 = new string('0', 64),
                        },
                    },
                };
            FoldCanvasProductionCorpusDocument actual =
                FoldCanvasProductionCorpusRunner.Parse(
                    FoldCanvasProductionCorpusRunner.Serialize(expected));
            actual.cases[0].sourceSha256 = new string('1', 64);

            string difference =
                FoldCanvasProductionCorpusRunner.DescribeDifference(
                    expected,
                    actual);

            Assert.That(difference, Does.Contain("case[0] expected"));
            Assert.That(difference, Does.Contain("case[0] actual"));
        }
    }
}
