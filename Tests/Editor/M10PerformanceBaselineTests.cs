using NUnit.Framework;

namespace FoldCanvas.Tests
{
    public sealed class M10PerformanceBaselineTests
    {
        [Test]
        public void PerformanceBaselines_CompileWithExpectedCounts()
        {
            FoldCanvas.Editor.FoldCanvasPerformanceReport report =
                FoldCanvas.Editor.FoldCanvasPerformanceRunner
                    .RunForTests(0, 1);

            Assert.That(report.scenarios.Count, Is.EqualTo(3));
            AssertScenario(
                report.scenarios[0],
                "planar-grid-64x32",
                2145,
                4096);
            AssertScenario(
                report.scenarios[1],
                "full-roll-64x8",
                585,
                1024);
            AssertScenario(
                report.scenarios[2],
                "registered-wave-48x24",
                1225,
                2304);
        }

        [Test]
        public void PerformanceBaselines_RepeatedGeometryHashesRemainStable()
        {
            FoldCanvas.Editor.FoldCanvasPerformanceReport first =
                FoldCanvas.Editor.FoldCanvasPerformanceRunner
                    .RunForTests(0, 1);
            FoldCanvas.Editor.FoldCanvasPerformanceReport second =
                FoldCanvas.Editor.FoldCanvasPerformanceRunner
                    .RunForTests(0, 1);

            for (int i = 0; i < first.scenarios.Count; i++)
            {
                Assert.That(
                    second.scenarios[i].id,
                    Is.EqualTo(first.scenarios[i].id));
                Assert.That(
                    second.scenarios[i].geometrySha256,
                    Is.EqualTo(first.scenarios[i].geometrySha256));
            }
        }

        private static void AssertScenario(
            FoldCanvas.Editor.FoldCanvasPerformanceScenarioResult scenario,
            string id,
            int vertices,
            int triangles)
        {
            Assert.That(scenario.id, Is.EqualTo(id));
            Assert.That(scenario.vertexCount, Is.EqualTo(vertices));
            Assert.That(scenario.triangleCount, Is.EqualTo(triangles));
            Assert.That(scenario.minimumMilliseconds,
                Is.GreaterThanOrEqualTo(0d));
            Assert.That(scenario.medianMilliseconds,
                Is.GreaterThanOrEqualTo(0d));
            Assert.That(scenario.maximumMilliseconds,
                Is.GreaterThanOrEqualTo(scenario.minimumMilliseconds));
            Assert.That(scenario.geometrySha256,
                Has.Length.EqualTo(64));
            Assert.That(scenario.withinBaseline, Is.True);
        }
    }
}
