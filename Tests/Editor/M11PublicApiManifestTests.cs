using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FoldCanvas.Editor;
using NUnit.Framework;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Tests
{
    public sealed class M11PublicApiManifestTests
    {
        [Test]
        public void PublicRuntimeApiManifest_MatchesCompiledAssembly()
        {
            FoldCanvasPublicApiManifestDocument baseline = LoadBaseline();
            FoldCanvasPublicApiManifestDocument current =
                FoldCanvasPublicApiManifest.CreateDocument(
                    typeof(FoldCanvasCompiler).Assembly,
                    FoldCanvasVersion.Package);

            string difference = FoldCanvasPublicApiManifest.DescribeDifference(
                baseline.signatures,
                current.signatures);
            Assert.That(
                difference,
                Is.Empty,
                "The public Runtime API changed. Additions require a reviewed " +
                "baseline update; removals or signature changes also require " +
                "an ADR, migration notes, and a version decision.\n" + difference);
            Assert.That(current.format, Is.EqualTo(baseline.format));
            Assert.That(current.version, Is.EqualTo(baseline.version));
            Assert.That(current.assembly, Is.EqualTo(baseline.assembly));
            Assert.That(current.packageVersion, Is.EqualTo(baseline.packageVersion));
            Assert.That(current.signatureCount, Is.EqualTo(baseline.signatureCount));
            Assert.That(current.sha256, Is.EqualTo(baseline.sha256));
        }

        [Test]
        public void PublicRuntimeApiManifest_IsOrdinalAndRepeatable()
        {
            IReadOnlyList<string> first =
                FoldCanvasPublicApiManifest.CreateSignatures(
                    typeof(FoldCanvasCompiler).Assembly);
            IReadOnlyList<string> second =
                FoldCanvasPublicApiManifest.CreateSignatures(
                    typeof(FoldCanvasCompiler).Assembly);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(
                first,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(first.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(first.Count));
            Assert.That(
                FoldCanvasPublicApiManifest.ComputeSignatureDigest(second),
                Is.EqualTo(
                    FoldCanvasPublicApiManifest.ComputeSignatureDigest(first)));
        }

        [Test]
        public void PublicRuntimeApiManifest_ReportsRemovedSignature()
        {
            IReadOnlyList<string> current =
                FoldCanvasPublicApiManifest.CreateSignatures(
                    typeof(FoldCanvasCompiler).Assembly);
            List<string> changed = current.Skip(1).ToList();

            string difference = FoldCanvasPublicApiManifest.DescribeDifference(
                current,
                changed);

            Assert.That(difference, Does.Contain("REMOVED " + current[0]));
        }

        [Test]
        public void PublicRuntimeApiManifest_ExcludesInternalAndEditorTypes()
        {
            IReadOnlyList<string> signatures =
                FoldCanvasPublicApiManifest.CreateSignatures(
                    typeof(FoldCanvasCompiler).Assembly);
            string joined = string.Join("\n", signatures);

            Assert.That(joined, Does.Not.Contain("MeshBuildBuffer"));
            Assert.That(joined, Does.Not.Contain("FoldCanvas.Editor"));
            Assert.That(joined, Does.Not.Contain("UnityEditor"));
        }

        private static FoldCanvasPublicApiManifestDocument LoadBaseline()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            string path = Path.Combine(
                package.resolvedPath,
                FoldCanvasPublicApiManifest.RelativeBaselinePath);
            Assert.That(File.Exists(path), Is.True, path);
            FoldCanvasPublicApiManifestDocument baseline =
                FoldCanvasPublicApiManifest.Parse(File.ReadAllText(path));
            Assert.That(baseline.format,
                Is.EqualTo(FoldCanvasPublicApiManifest.Format));
            Assert.That(baseline.version,
                Is.EqualTo(FoldCanvasPublicApiManifest.ManifestVersion));
            Assert.That(baseline.assembly,
                Is.EqualTo(FoldCanvasPublicApiManifest.AssemblyName));
            Assert.That(baseline.signatures, Is.Not.Null);
            return baseline;
        }
    }
}
