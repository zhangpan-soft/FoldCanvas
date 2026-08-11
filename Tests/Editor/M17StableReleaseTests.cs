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
    public sealed class M17StableReleaseTests
    {
        private const string StableVersion = "1.0.0";
        private const string QualifiedUnityVersion = "6000.3.20f1";
        private const string NormalizedVersionToken = "{PACKAGE_VERSION}";

        [Test]
        public void StablePackage_VersionAndCompatibilityAreExact()
        {
            string root = PackageRoot();
            PackageManifest package = JsonUtility.FromJson<PackageManifest>(
                File.ReadAllText(Path.Combine(root, "package.json")));
            StableReleaseContract contract = LoadContract(root);

            Assert.That(package.name, Is.EqualTo("com.foldcanvas.core"));
            Assert.That(package.version, Is.EqualTo(StableVersion));
            Assert.That(package.unity, Is.EqualTo("6000.3"));
            Assert.That(package.unityRelease, Is.EqualTo("20f1"));
            Assert.That(FoldCanvasVersion.Package, Is.EqualTo(StableVersion));
            Assert.That(FoldCanvasVersion.Compiler, Is.EqualTo(StableVersion));
            Assert.That(contract.packageVersion, Is.EqualTo(StableVersion));
            Assert.That(contract.tag, Is.EqualTo("v" + StableVersion));
            Assert.That(contract.stableRelease, Is.True);
            Assert.That(contract.foldScriptVersion, Is.EqualTo("0.1"));
            Assert.That(contract.unityVersion,
                Is.EqualTo(QualifiedUnityVersion));
        }

        [Test]
        public void StableReadiness_ExactReportIsReadyAndDigestBound()
        {
            string root = PackageRoot();
            StableReleaseContract contract = LoadContract(root);
            string reportPath = Path.Combine(
                root,
                "Documentation~/m17-stable-readiness-report.json");
            StableReadinessReport report =
                JsonUtility.FromJson<StableReadinessReport>(
                    File.ReadAllText(reportPath));

            Assert.That(Sha256(File.ReadAllBytes(reportPath)),
                Is.EqualTo(contract.stableQualification.reportSha256));
            Assert.That(report.status, Is.EqualTo("ready"));
            Assert.That(report.targetVersion, Is.EqualTo(StableVersion));
            Assert.That(report.soakHours, Is.GreaterThanOrEqualTo(168d));
            Assert.That(report.qualifyingScheduledLongRuns,
                Is.GreaterThanOrEqualTo(2));
            Assert.That(report.satisfiedGateCount,
                Is.EqualTo(report.requiredGateCount));
            Assert.That(report.openReleaseBlockerCount, Is.Zero);
            Assert.That(report.blockers, Is.Empty);
        }

        [Test]
        public void StablePublicRuntimeApi_MatchesCurrentAndNormalizedRc2Shape()
        {
            StableReleaseContract contract = LoadContract(PackageRoot());
            FoldCanvasPublicApiManifestDocument current =
                FoldCanvasPublicApiManifest.CreateDocument(
                    typeof(FoldCanvasCompiler).Assembly,
                    FoldCanvasVersion.Package);

            Assert.That(current.packageVersion, Is.EqualTo(StableVersion));
            Assert.That(current.signatureCount,
                Is.EqualTo(contract.publicRuntimeApi.signatureCount));
            Assert.That(current.sha256,
                Is.EqualTo(
                    "81423d50792126cc9682fa32a6985f68bc24af28f8b9f93bdeeb5c43eee0c82a"));
            string[] normalized = current.signatures
                .Select(signature => signature.Replace(
                    StableVersion,
                    NormalizedVersionToken))
                .ToArray();
            Assert.That(SignatureSha256(normalized),
                Is.EqualTo(contract.publicRuntimeApi.normalizedSha256));
        }

        [Test]
        public void StableProductionCorpus_RetainsOrderedRc2CaseIdentity()
        {
            string root = PackageRoot();
            ProductionCorpus corpus = JsonUtility.FromJson<ProductionCorpus>(
                File.ReadAllText(Path.Combine(
                    root,
                    "Documentation~/m11-production-corpus.json")));

            Assert.That(corpus.packageVersion, Is.EqualTo(StableVersion));
            Assert.That(corpus.foldScriptVersion, Is.EqualTo("0.1"));
            Assert.That(corpus.unityVersion,
                Is.EqualTo(QualifiedUnityVersion));
            Assert.That(corpus.cases.Select(value => value.id), Is.EqualTo(new[]
            {
                "cyclic-torus",
                "invalid-off-grid-fold",
                "planar-artwork",
                "production-cup",
                "registered-wave",
                "sphere-gores",
            }));
        }

        [Test]
        public void StableRollback_IsImmutableRc2SourceLineage()
        {
            StableReleaseContract contract = LoadContract(PackageRoot());

            Assert.That(contract.rollback.packageVersion,
                Is.EqualTo("1.0.0-rc.2"));
            Assert.That(contract.rollback.tag, Is.EqualTo("v1.0.0-rc.2"));
            Assert.That(contract.rollback.gitCommit,
                Is.EqualTo(
                    "4db988ffac6dad4362d126001e5c9a67081ef2b7"));
            Assert.That(contract.rollback.archiveSha256,
                Is.EqualTo(
                    "72c4191ed8c466f966e30b77cf76f61cb0f51ab12d5853b5f1bc893a5c46d707"));
            Assert.That(contract.rollback.sourceAuthority,
                Is.EqualTo("2d-canvas-plus-foldscript"));
        }

        private static string PackageRoot()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            return package.resolvedPath;
        }

        private static StableReleaseContract LoadContract(string root)
        {
            StableReleaseContract contract =
                JsonUtility.FromJson<StableReleaseContract>(
                    File.ReadAllText(Path.Combine(
                        root,
                        "Documentation~/m17-stable-release.json")));
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.format,
                Is.EqualTo("foldcanvas-stable-release"));
            Assert.That(contract.version, Is.EqualTo("1"));
            return contract;
        }

        private static string SignatureSha256(
            IReadOnlyList<string> signatures)
        {
            return Sha256(Encoding.UTF8.GetBytes(
                string.Join("\n", signatures) + "\n"));
        }

        private static string Sha256(byte[] bytes)
        {
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
        private sealed class StableReleaseContract
        {
            public string format;
            public string version;
            public string packageVersion;
            public string tag;
            public bool stableRelease;
            public string foldScriptVersion;
            public string unityVersion;
            public StableQualification stableQualification;
            public PublicRuntimeApi publicRuntimeApi;
            public Rollback rollback;
        }

        [Serializable]
        private sealed class StableQualification
        {
            public string reportSha256;
        }

        [Serializable]
        private sealed class PublicRuntimeApi
        {
            public int signatureCount;
            public string normalizedSha256;
        }

        [Serializable]
        private sealed class Rollback
        {
            public string packageVersion;
            public string tag;
            public string gitCommit;
            public string archiveSha256;
            public string sourceAuthority;
        }

        [Serializable]
        private sealed class StableReadinessReport
        {
            public string status;
            public string targetVersion;
            public double soakHours;
            public int qualifyingScheduledLongRuns;
            public int satisfiedGateCount;
            public int requiredGateCount;
            public int openReleaseBlockerCount;
            public string[] blockers;
        }

        [Serializable]
        private sealed class ProductionCorpus
        {
            public string packageVersion;
            public string foldScriptVersion;
            public string unityVersion;
            public ProductionCase[] cases;
        }

        [Serializable]
        private sealed class ProductionCase
        {
            public string id;
        }
    }
}
