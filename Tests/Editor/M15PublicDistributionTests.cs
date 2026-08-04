using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Tests
{
    public sealed class M15PublicDistributionTests
    {
        private const string CandidateVersion = "1.0.0-rc.2";

        [Test]
        public void PublicAssets_AreExactAndOrdinal()
        {
            DistributionContract contract = LoadContract();
            string[] expected =
            {
                "com.foldcanvas.core-1.0.0-rc.2.evidence.json",
                "com.foldcanvas.core-1.0.0-rc.2.manifest.json",
                "com.foldcanvas.core-1.0.0-rc.2.tgz",
                "com.foldcanvas.core-1.0.0-rc.2.tgz.sha256",
            };

            Assert.That(contract.publicAssets, Is.EqualTo(expected));
            Assert.That(contract.candidateVersion,
                Is.EqualTo(CandidateVersion));
            Assert.That(contract.candidateTag,
                Is.EqualTo("v" + CandidateVersion));
        }

        [Test]
        public void UpgradeFixture_UsesOnlyFoldScriptAndPngSource()
        {
            UpgradeContract upgrade = LoadContract().upgrade;

            Assert.That(upgrade.sourceAuthority,
                Is.EqualTo("2d-canvas-plus-foldscript"));
            Assert.That(upgrade.fixture.inputFileNames, Is.EqualTo(new[]
            {
                "M04ProductionCupCanvas.png",
                "m12-production-cup.foldcanvas.json",
            }));
            Assert.That(upgrade.fixture.sourcePath,
                Does.EndWith(".foldcanvas.json"));
            Assert.That(upgrade.fixture.appearancePath,
                Does.EndWith(".png"));
            Assert.That(upgrade.fixture.renderVertices, Is.EqualTo(2972));
            Assert.That(upgrade.fixture.topologyVertices, Is.EqualTo(2562));
            Assert.That(upgrade.fixture.triangles, Is.EqualTo(5120));
            Assert.That(upgrade.fixture.closedVolume, Is.True);
        }

        [Test]
        public void UpgradeContract_ForbidsDerivedGeometryInputs()
        {
            UpgradeContract upgrade = LoadContract().upgrade;

            Assert.That(upgrade.derivedInputsForbidden, Is.EqualTo(new[]
            {
                "material",
                "mesh",
                "obj",
                "prefab",
                "receipt",
                "report",
                "screenshot",
            }));
            Assert.That(upgrade.unknownVersionsFailClosed, Is.True);
            Assert.That(upgrade.fromPackageVersions, Is.EqualTo(new[]
            {
                "0.1.0-preview.21",
                "1.0.0-rc.1",
            }));
        }

        [Test]
        public void UpgradeFixture_SourceAndAppearanceHashesAreFrozen()
        {
            string root = PackageRoot();
            UpgradeFixture fixture = LoadContract().upgrade.fixture;
            Assert.That(
                Sha256(File.ReadAllBytes(Path.Combine(root, fixture.sourcePath))),
                Is.EqualTo(fixture.sourceRawSha256));
            Assert.That(
                Sha256(File.ReadAllBytes(Path.Combine(
                    root,
                    fixture.appearancePath))),
                Is.EqualTo(fixture.appearanceSha256));

            string source = File.ReadAllText(
                Path.Combine(root, fixture.sourcePath));
            FoldScriptWriteResult canonical =
                FoldScriptSerializer.Canonicalize(source);
            Assert.That(canonical.Success, Is.True);
            Assert.That(
                Sha256(new UTF8Encoding(false).GetBytes(canonical.Json)),
                Is.EqualTo(fixture.canonicalSourceSha256));
        }

        [Test]
        public void StableExit_RemainsBlockedUntilSoakAndScheduledRuns()
        {
            StableExitContract stableExit = LoadContract().stableExit;

            Assert.That(stableExit.targetVersion, Is.EqualTo("1.0.0"));
            Assert.That(stableExit.minimumSoakHours, Is.EqualTo(168));
            Assert.That(stableExit.minimumScheduledLongRuns, Is.EqualTo(2));
            Assert.That(stableExit.status, Is.EqualTo("blocked"));
            Assert.That(stableExit.blockers, Is.EqualTo(new[]
            {
                "candidate-not-published",
                "public-release-assets-unverified",
                "public-consumer-evidence-missing",
                "source-upgrade-evidence-missing",
                "minimum-soak-incomplete",
                "scheduled-long-runs-incomplete",
                "exact-head-audit-missing",
                "required-gates-incomplete",
            }));
        }

        private static DistributionContract LoadContract()
        {
            string json = File.ReadAllText(Path.Combine(
                PackageRoot(),
                "Documentation~/m15-public-distribution.json"));
            DistributionContract contract =
                UnityEngine.JsonUtility.FromJson<DistributionContract>(json);
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.format,
                Is.EqualTo("foldcanvas-public-distribution"));
            Assert.That(contract.version, Is.EqualTo("1"));
            return contract;
        }

        private static string PackageRoot()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            return package.resolvedPath;
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] digest = algorithm.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(
                    digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        [Serializable]
        private sealed class DistributionContract
        {
            public string format;
            public string version;
            public string candidateVersion;
            public string candidateTag;
            public string[] publicAssets;
            public UpgradeContract upgrade;
            public StableExitContract stableExit;
        }

        [Serializable]
        private sealed class UpgradeContract
        {
            public string[] fromPackageVersions;
            public UpgradeFixture fixture;
            public string sourceAuthority;
            public string[] derivedInputsForbidden;
            public bool unknownVersionsFailClosed;
        }

        [Serializable]
        private sealed class UpgradeFixture
        {
            public string sourcePath;
            public string appearancePath;
            public string[] inputFileNames;
            public string sourceRawSha256;
            public string canonicalSourceSha256;
            public string appearanceSha256;
            public int renderVertices;
            public int topologyVertices;
            public int triangles;
            public bool closedVolume;
        }

        [Serializable]
        private sealed class StableExitContract
        {
            public string targetVersion;
            public int minimumSoakHours;
            public int minimumScheduledLongRuns;
            public string status;
            public string[] blockers;
        }
    }
}
