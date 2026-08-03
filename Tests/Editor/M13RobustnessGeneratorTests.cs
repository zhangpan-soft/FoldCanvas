using System;
using System.Collections.Generic;
using System.Linq;
using FoldCanvas.Editor;
using NUnit.Framework;

namespace FoldCanvas.Tests
{
    public sealed class M13RobustnessGeneratorTests
    {
        private const ulong TestSeed = 0x0123456789ABCDEFUL;

        [Test]
        public void RobustnessGenerator_SameIdentityProducesCanonicalSource()
        {
            using (FoldCanvasRobustnessGeneratedCase first =
                FoldCanvasRobustnessGenerator.Generate(
                    FoldCanvasRobustnessGenerator.RollValidSuite,
                    TestSeed,
                    7))
            using (FoldCanvasRobustnessGeneratedCase second =
                FoldCanvasRobustnessGenerator.Generate(
                    FoldCanvasRobustnessGenerator.RollValidSuite,
                    TestSeed,
                    7))
            {
                Assert.That(second.CaseId, Is.EqualTo(first.CaseId));
                Assert.That(
                    second.CanonicalSource,
                    Is.EqualTo(first.CanonicalSource));
                Assert.That(
                    second.ExpectedSuccess,
                    Is.EqualTo(first.ExpectedSuccess));
                Assert.That(
                    second.ExpectedDiagnosticCode,
                    Is.EqualTo(first.ExpectedDiagnosticCode));
            }
        }

        [Test]
        public void RobustnessGenerator_DifferentOrderDoesNotChangeCase()
        {
            Dictionary<string, string> ascending = GenerateSources(
                new[] { 0, 1, 2, 3, 4, 5 });
            Dictionary<string, string> descending = GenerateSources(
                new[] { 5, 4, 3, 2, 1, 0 });

            Assert.That(descending.Count, Is.EqualTo(ascending.Count));
            foreach (KeyValuePair<string, string> item in ascending)
            {
                Assert.That(descending.ContainsKey(item.Key), Is.True);
                Assert.That(descending[item.Key], Is.EqualTo(item.Value));
            }
        }

        [Test]
        public void RobustnessSmokeCorpus_RepeatedRunHasStableSemanticDigest()
        {
            FoldCanvasRobustnessReport first =
                FoldCanvasRobustnessRunner.RunSmoke(4, TestSeed);
            FoldCanvasRobustnessReport second =
                FoldCanvasRobustnessRunner.RunSmoke(4, TestSeed);

            Assert.That(second.semanticSha256,
                Is.EqualTo(first.semanticSha256));
            Assert.That(second.caseCount, Is.EqualTo(first.caseCount));
            for (int i = 0; i < first.cases.Count; i++)
            {
                Assert.That(second.cases[i].caseId,
                    Is.EqualTo(first.cases[i].caseId));
                Assert.That(second.cases[i].sourceSha256,
                    Is.EqualTo(first.cases[i].sourceSha256));
                Assert.That(second.cases[i].geometrySha256,
                    Is.EqualTo(first.cases[i].geometrySha256));
                Assert.That(second.cases[i].diagnosticSha256,
                    Is.EqualTo(first.cases[i].diagnosticSha256));
            }
        }

        [Test]
        public void RobustnessSmokeCorpus_ValidCasesProduceFiniteDeterministicGeometry()
        {
            FoldCanvasRobustnessReport report =
                FoldCanvasRobustnessRunner.RunSmoke(4, TestSeed);
            FoldCanvasRobustnessCaseResult[] valid = report.cases
                .Where(item => item.expectedSuccess)
                .ToArray();

            Assert.That(valid.Length, Is.EqualTo(8));
            Assert.That(valid.All(item => item.actualSuccess), Is.True);
            Assert.That(valid.All(item => item.meshReturned), Is.True);
            Assert.That(valid.All(item => item.compiledDataReturned), Is.True);
            Assert.That(valid.All(item => item.finiteBuffers), Is.True);
            Assert.That(valid.All(item => item.renderVertexCount > 0), Is.True);
            Assert.That(valid.All(item => item.triangleCount > 0), Is.True);
            Assert.That(valid.All(item => item.geometrySha256.Length == 64),
                Is.True);
        }

        [Test]
        public void RobustnessSmokeCorpus_InvalidCasesReturnStableDiagnosticsWithoutMesh()
        {
            FoldCanvasRobustnessReport first =
                FoldCanvasRobustnessRunner.RunSmoke(4, TestSeed);
            FoldCanvasRobustnessReport second =
                FoldCanvasRobustnessRunner.RunSmoke(4, TestSeed);
            FoldCanvasRobustnessCaseResult[] invalid = first.cases
                .Where(item => !item.expectedSuccess)
                .ToArray();

            Assert.That(invalid.Length, Is.EqualTo(8));
            Assert.That(invalid.All(item => !item.actualSuccess), Is.True);
            Assert.That(invalid.All(item => !item.meshReturned), Is.True);
            Assert.That(invalid.All(item => !item.compiledDataReturned),
                Is.True);
            Assert.That(invalid.All(item =>
                    item.actualFirstErrorCode ==
                    item.expectedDiagnosticCode),
                Is.True);
            Assert.That(second.cases.Select(item => item.diagnosticSha256),
                Is.EqualTo(first.cases.Select(item =>
                    item.diagnosticSha256)));
        }

        [Test]
        public void RobustnessSmokeCorpus_DoesNotThrowOrMutateSource()
        {
            FoldCanvasRobustnessReport report =
                FoldCanvasRobustnessRunner.RunSmoke(8, TestSeed);

            Assert.That(report.complete, Is.True);
            Assert.That(report.caseCount, Is.EqualTo(32));
            Assert.That(report.passedCount, Is.EqualTo(report.caseCount));
            Assert.That(report.unexpectedCount, Is.Zero);
            Assert.That(report.packageVersion,
                Is.EqualTo(FoldCanvasVersion.Package));
            Assert.That(report.compilerVersion,
                Is.EqualTo(FoldCanvasVersion.Compiler));
            Assert.That(report.foldScriptVersion,
                Is.EqualTo(FoldCanvasSourceMetadata.CurrentSchemaVersion));
            Assert.That(report.unityVersion, Is.Not.Empty);
            Assert.That(report.platform, Is.Not.Empty);
            Assert.That(report.cases.All(item => item.passed), Is.True);
            Assert.That(report.cases.All(item => item.sourceUnchanged),
                Is.True);
            Assert.That(report.cases.All(item =>
                    string.IsNullOrEmpty(item.exceptionType)),
                Is.True);
            Assert.That(report.semanticSha256.Length, Is.EqualTo(64));
        }

        private static Dictionary<string, string> GenerateSources(
            IEnumerable<int> ordinals)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (int ordinal in ordinals)
            {
                using (FoldCanvasRobustnessGeneratedCase generated =
                    FoldCanvasRobustnessGenerator.Generate(
                        FoldCanvasRobustnessGenerator.PlanarValidSuite,
                        TestSeed,
                        ordinal))
                {
                    result.Add(
                        generated.CaseId,
                        generated.CanonicalSource);
                }
            }

            return result;
        }
    }
}
