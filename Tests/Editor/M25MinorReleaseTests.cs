using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FoldCanvas.Editor;
using NUnit.Framework;
using UnityEngine;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Tests
{
    public sealed class M25MinorReleaseTests
    {
        private const string CurrentVersion = "1.1.0";
        private const string BaselineVersion = "1.0.1";
        private const string NormalizedVersionToken = "{PACKAGE_VERSION}";

        [Test]
        public void MinorPackage_VersionCompilerAndContractAreExact()
        {
            string root = PackageRoot();
            PackageManifest package = JsonUtility.FromJson<PackageManifest>(
                File.ReadAllText(Path.Combine(root, "package.json")));
            MinorReleaseContract contract = LoadContract(root);

            Assert.That(package.name, Is.EqualTo("com.foldcanvas.core"));
            Assert.That(package.version, Is.EqualTo(CurrentVersion));
            Assert.That(FoldCanvasVersion.Package, Is.EqualTo(CurrentVersion));
            Assert.That(FoldCanvasVersion.Compiler, Is.EqualTo(CurrentVersion));
            Assert.That(package.unity, Is.EqualTo("6000.3"));
            Assert.That(package.unityRelease, Is.EqualTo("20f1"));
            Assert.That(contract.format, Is.EqualTo("foldcanvas-minor-release"));
            Assert.That(contract.packageVersion, Is.EqualTo(CurrentVersion));
            Assert.That(contract.tag, Is.EqualTo("v" + CurrentVersion));
            Assert.That(contract.stableRelease, Is.True);
            Assert.That(contract.minorRelease, Is.True);
            Assert.That(contract.foldScriptVersion, Is.EqualTo("0.1"));
            Assert.That(contract.unityVersion, Is.EqualTo("6000.3.20f1"));
        }

        [Test]
        public void MinorRuntimeApi_PreservesNormalizedStableShape()
        {
            MinorReleaseContract contract = LoadContract(PackageRoot());
            FoldCanvasPublicApiManifestDocument current =
                FoldCanvasPublicApiManifest.CreateDocument(
                    typeof(FoldCanvasCompiler).Assembly,
                    FoldCanvasVersion.Package);

            Assert.That(current.packageVersion, Is.EqualTo(CurrentVersion));
            Assert.That(current.signatureCount,
                Is.EqualTo(contract.publicRuntimeApi.signatureCount));
            string[] normalized = current.signatures
                .Select(signature => signature.Replace(
                    CurrentVersion,
                    NormalizedVersionToken))
                .ToArray();
            Assert.That(SignatureSha256(normalized),
                Is.EqualTo(contract.publicRuntimeApi.normalizedSha256));
        }

        [Test]
        public void MinorCorpus_PreservesLegacyCasesAndEnablesOffGridFold()
        {
            string root = PackageRoot();
            ProductionCorpus historical = LoadCorpus(
                root,
                "Documentation~/m11-production-corpus.json");
            ProductionCorpus current = LoadCorpus(
                root,
                "Documentation~/m24-production-corpus.json");
            MinorReleaseContract contract = LoadContract(root);

            Assert.That(current.packageVersion, Is.EqualTo(CurrentVersion));
            Assert.That(current.cases.Select(value => value.id),
                Is.EqualTo(contract.productionCorpus.caseIds));
            HashSet<string> unchanged = new HashSet<string>(
                contract.productionCorpus.unchangedCaseIds,
                StringComparer.Ordinal);
            Assert.That(unchanged.Count, Is.EqualTo(5));
            foreach (string id in unchanged.OrderBy(value => value,
                         StringComparer.Ordinal))
            {
                ProductionCase before = historical.cases.Single(
                    value => value.id == id);
                ProductionCase after = current.cases.Single(
                    value => value.id == id);
                Assert.That(CaseEvidence(after), Is.EqualTo(CaseEvidence(before)),
                    id);
            }

            ProductionCase historicalOffGrid = historical.cases.Single(
                value => value.id == "invalid-off-grid-fold");
            ProductionCase offGrid = current.cases.Single(
                value => value.id == contract.productionCorpus.featureCaseId);
            Assert.That(historicalOffGrid.success, Is.False);
            Assert.That(historicalOffGrid.errorDiagnosticCode,
                Is.EqualTo("FC3011"));
            Assert.That(offGrid.success, Is.True);
            Assert.That(offGrid.errorDiagnosticCode, Is.Empty);
            Assert.That(offGrid.renderVertices, Is.EqualTo(7));
            Assert.That(offGrid.topologyVertices, Is.EqualTo(7));
            Assert.That(offGrid.triangles, Is.EqualTo(6));
        }

        [Test]
        public void MinorRollback_IsImmutablePublicV101()
        {
            StableBaseline baseline = LoadContract(PackageRoot()).stableBaseline;

            Assert.That(baseline.packageVersion, Is.EqualTo(BaselineVersion));
            Assert.That(baseline.tag, Is.EqualTo("v" + BaselineVersion));
            Assert.That(baseline.tagCommit,
                Is.EqualTo("867f3bd5501218aa95872db6e7e66cb213031cab"));
            Assert.That(baseline.releaseId, Is.EqualTo(369729288));
            Assert.That(baseline.archiveSha256,
                Is.EqualTo(
                    "4188d23b18b924f6642f9e4eabbc15500fb60fb3d3916f23857a5a19966e1de5"));
            Assert.That(baseline.assetCount, Is.EqualTo(4));
            Assert.That(baseline.immutable, Is.True);
        }

        private static string PackageRoot()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            return package.resolvedPath;
        }

        private static MinorReleaseContract LoadContract(string root)
        {
            MinorReleaseContract contract =
                JsonUtility.FromJson<MinorReleaseContract>(File.ReadAllText(
                    Path.Combine(
                        root,
                        "Documentation~/m25-minor-release.json")));
            Assert.That(contract, Is.Not.Null);
            return contract;
        }

        private static ProductionCorpus LoadCorpus(
            string root,
            string relative)
        {
            ProductionCorpus corpus = JsonUtility.FromJson<ProductionCorpus>(
                File.ReadAllText(Path.Combine(root, relative)));
            Assert.That(corpus, Is.Not.Null);
            Assert.That(corpus.cases, Is.Not.Null);
            return corpus;
        }

        private static string CaseEvidence(ProductionCase value)
        {
            return string.Join("|", new[]
            {
                value.sourceFormat,
                value.validationLevel,
                value.success.ToString(),
                value.errorDiagnosticCode,
                value.diagnosticCount.ToString(),
                value.renderVertices.ToString(),
                value.topologyVertices.ToString(),
                value.triangles.ToString(),
                value.openEdges.ToString(),
                value.nonManifoldEdges.ToString(),
                value.componentCount.ToString(),
                value.closedVolume.ToString(),
                value.sphereEulerCharacteristic.ToString(),
                value.sourceSha256,
                value.geometrySha256,
                value.objSha256,
                value.diagnosticSha256,
            });
        }

        private static string SignatureSha256(
            IReadOnlyList<string> signatures)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(
                string.Join("\n", signatures) + "\n");
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] digest = algorithm.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        [Serializable]
        private sealed class PackageManifest
        {
            public string name;
            public string version;
            public string unity;
            public string unityRelease;
        }

        [Serializable]
        private sealed class MinorReleaseContract
        {
            public string format;
            public string packageVersion;
            public string tag;
            public bool stableRelease;
            public bool minorRelease;
            public string foldScriptVersion;
            public string unityVersion;
            public StableBaseline stableBaseline;
            public PublicRuntimeApi publicRuntimeApi;
            public ProductionCorpusContract productionCorpus;
        }

        [Serializable]
        private sealed class StableBaseline
        {
            public string packageVersion;
            public string tag;
            public string tagCommit;
            public int releaseId;
            public string archiveSha256;
            public int assetCount;
            public bool immutable;
        }

        [Serializable]
        private sealed class PublicRuntimeApi
        {
            public int signatureCount;
            public string normalizedSha256;
        }

        [Serializable]
        private sealed class ProductionCorpusContract
        {
            public string[] caseIds;
            public string[] unchangedCaseIds;
            public string featureCaseId;
        }

        [Serializable]
        private sealed class ProductionCorpus
        {
            public string packageVersion;
            public ProductionCase[] cases;
        }

        [Serializable]
        private sealed class ProductionCase
        {
            public string id;
            public string sourceFormat;
            public string validationLevel;
            public bool success;
            public string errorDiagnosticCode;
            public int diagnosticCount;
            public int renderVertices;
            public int topologyVertices;
            public int triangles;
            public int openEdges;
            public int nonManifoldEdges;
            public int componentCount;
            public bool closedVolume;
            public int sphereEulerCharacteristic;
            public string sourceSha256;
            public string geometrySha256;
            public string objSha256;
            public string diagnosticSha256;
        }
    }
}
