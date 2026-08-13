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
    public sealed class M14ReleaseCandidateTests
    {
        private const string CandidateVersion = "1.0.0-rc.2";
        private const string CurrentVersion = "1.1.0";
        private const string QualifiedUnityVersion = "6000.3.20f1";

        [Test]
        public void HistoricalReleaseCandidate_AndCurrentStableVersionAreExact()
        {
            string root = PackageRoot();
            PackageManifest package = JsonUtility.FromJson<PackageManifest>(
                File.ReadAllText(Path.Combine(root, "package.json")));
            ReleaseCandidateContract contract = LoadContract(root);

            Assert.That(package.name, Is.EqualTo("com.foldcanvas.core"));
            Assert.That(package.version, Is.EqualTo(CurrentVersion));
            Assert.That(FoldCanvasVersion.Package, Is.EqualTo(CurrentVersion));
            Assert.That(FoldCanvasVersion.Compiler, Is.EqualTo(CurrentVersion));
            Assert.That(package.unity, Is.EqualTo("6000.3"));
            Assert.That(package.unityRelease, Is.EqualTo("20f1"));
            Assert.That(contract.candidateVersion, Is.EqualTo(CandidateVersion));
            Assert.That(contract.stableRelease, Is.False);
            Assert.That(contract.candidateTag, Is.EqualTo("v" + CandidateVersion));
            Assert.That(contract.unityVersion, Is.EqualTo(QualifiedUnityVersion));
        }

        [Test]
        public void HistoricalReleaseCandidate_PublicRuntimeApiIdentityIsFrozen()
        {
            ReleaseCandidateContract contract = LoadContract(PackageRoot());

            Assert.That(contract.publicRuntimeApi.assembly,
                Is.EqualTo("FoldCanvas.Runtime"));
            Assert.That(contract.publicRuntimeApi.signatureCount,
                Is.EqualTo(808));
            Assert.That(contract.publicRuntimeApi.sha256,
                Is.EqualTo(
                    "6bcda14a96fd55c5edae33a7da5c5c783978c4a5c7d300eba7dea525200fc8f4"));
        }

        [Test]
        public void ReleaseCandidate_PublicApiDifferenceReportsBothDirections()
        {
            IReadOnlyList<string> current =
                FoldCanvasPublicApiManifest.CreateSignatures(
                    typeof(FoldCanvasCompiler).Assembly);
            List<string> changed = current.Skip(1).ToList();
            changed.Add("type FoldCanvas.FutureCompatibleAddition");

            string difference = FoldCanvasPublicApiManifest.DescribeDifference(
                current,
                changed);

            Assert.That(difference, Does.Contain("REMOVED " + current[0]));
            Assert.That(
                difference,
                Does.Contain("ADDED type FoldCanvas.FutureCompatibleAddition"));
            Assert.That(
                difference.Replace("\r", string.Empty).Split('\n'),
                Is.EqualTo(new[]
                {
                    "REMOVED " + current[0],
                    "ADDED type FoldCanvas.FutureCompatibleAddition",
                }));
        }

        [Test]
        public void FoldScript01_FrozenFixturesRetainCanonicalHashes()
        {
            string root = PackageRoot();
            ReleaseCandidateContract contract = LoadContract(root);
            Assert.That(contract.foldScriptVersion, Is.EqualTo("0.1"));
            Assert.That(contract.foldScriptFixtures, Is.Not.Empty);

            for (int i = 0; i < contract.foldScriptFixtures.Length; i++)
            {
                FoldScriptFixture fixture = contract.foldScriptFixtures[i];
                byte[] sourceBytes =
                    File.ReadAllBytes(Path.Combine(root, fixture.path));
                string source = Encoding.UTF8.GetString(sourceBytes);
                Assert.That(
                    Sha256(sourceBytes),
                    Is.EqualTo(fixture.sourceSha256),
                    "Source fixture drifted: " + fixture.id);

                FoldScriptWriteResult first = FoldScriptSerializer.Canonicalize(source);
                Assert.That(
                    first.Success,
                    Is.True,
                    FirstDiagnostic(first.Diagnostics));
                string actualCanonicalSha256 =
                    Sha256(Encoding.UTF8.GetBytes(first.Json));
                Assert.That(
                    actualCanonicalSha256,
                    Is.EqualTo(fixture.canonicalSha256),
                    "Actual canonical SHA-256 for " + fixture.id + " is " +
                    actualCanonicalSha256 + ".");

                FoldScriptWriteResult second =
                    FoldScriptSerializer.Canonicalize(first.Json);
                Assert.That(second.Success, Is.True);
                Assert.That(second.Json, Is.EqualTo(first.Json));
            }
        }

        [Test]
        public void FoldScript_UnknownVersionRemainsStableRejectionWithoutDocument()
        {
            string source = File.ReadAllText(
                Path.Combine(
                    PackageRoot(),
                    "Schema/gpt-cup.example.foldcanvas.json"));
            string future = source.Replace(
                "\"schemaVersion\": \"0.1\"",
                "\"schemaVersion\": \"1.0\"");

            FoldScriptReadResult first = FoldScriptSerializer.Read(future);
            FoldScriptReadResult second = FoldScriptSerializer.Read(future);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Document, Is.Null);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(
                first.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.UnsupportedFoldScriptVersion));
            Assert.That(second.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(
                second.Diagnostics[0].ToString(),
                Is.EqualTo(first.Diagnostics[0].ToString()));
        }

        [Test]
        public void ReleaseCandidate_RequiredGatesAndRollbackAreFrozen()
        {
            ReleaseCandidateContract contract = LoadContract(PackageRoot());
            string[] expectedGates =
            {
                "clean-install-a",
                "clean-install-b",
                "deterministic-release",
                "m13-robustness-long-run",
                "production-corpus",
                "production-handoff-producer",
                "production-handoff-receiver",
                "public-release-assets",
                "public-release-consumer-a",
                "public-release-consumer-b",
                "public-runtime-api",
                "repository-validation",
                "source-upgrade",
                "unity-editmode",
            };

            Assert.That(contract.requiredGates, Is.EqualTo(expectedGates));
            Assert.That(
                contract.requiredGates,
                Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(
                contract.requiredGates.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(contract.requiredGates.Length));
            Assert.That(
                contract.rollback.packageVersion,
                Is.EqualTo("1.0.0-rc.1"));
            Assert.That(contract.rollback.tag, Is.EqualTo("v1.0.0-rc.1"));
            Assert.That(
                contract.rollback.gitCommit,
                Is.EqualTo("a8c81e61175dafbc48d1750de7ef6823589517a6"));
            Assert.That(
                contract.rollback.sourceAuthority,
                Is.EqualTo("2d-canvas-plus-foldscript"));
        }

        [Test]
        public void PriorRelease_Rc1PublicAssetDigestsRemainImmutable()
        {
            ReleaseCandidateContract contract = LoadContract(PackageRoot());

            Assert.That(contract.priorRelease.immutable, Is.True);
            Assert.That(contract.priorRelease.tag, Is.EqualTo("v1.0.0-rc.1"));
            Assert.That(contract.priorRelease.releaseId, Is.EqualTo(364684802));
            Assert.That(
                contract.priorRelease.archiveSha256,
                Is.EqualTo(
                    "ff3a065eec3a638701ff51d4f069684df4f075226305253ae04fd6ed2b250fdd"));
            Assert.That(
                contract.priorRelease.manifestSha256,
                Is.EqualTo(
                    "a0b9f19b9b69cace6b430b4546fc67b0f294dadb1efef6f8542b5e53ee5a9aca"));
            Assert.That(
                contract.priorRelease.evidenceSha256,
                Is.EqualTo(
                    "c9603b475bbfa78300a27b64189188a337ca95ef333c8ad00effa7cf808e3c32"));
        }

        private static string PackageRoot()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            Assert.That(package, Is.Not.Null);
            return package.resolvedPath;
        }

        private static ReleaseCandidateContract LoadContract(string root)
        {
            string json = File.ReadAllText(
                Path.Combine(
                    root,
                    "Documentation~/m15-public-distribution.json"));
            ReleaseCandidateContract contract =
                JsonUtility.FromJson<ReleaseCandidateContract>(json);
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.format, Is.EqualTo("foldcanvas-public-distribution"));
            Assert.That(contract.version, Is.EqualTo("1"));
            return contract;
        }

        private static string Sha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(value);
                StringBuilder builder = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static string FirstDiagnostic(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            return diagnostics == null || diagnostics.Count == 0
                ? string.Empty
                : diagnostics[0].ToString();
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
        private sealed class ReleaseCandidateContract
        {
            public string format;
            public string version;
            public string candidateVersion;
            public string candidateTag;
            public bool stableRelease;
            public string foldScriptVersion;
            public string unityVersion;
            public PublicRuntimeApi publicRuntimeApi;
            public FoldScriptFixture[] foldScriptFixtures;
            public string[] requiredGates;
            public Rollback rollback;
            public PriorRelease priorRelease;
        }

        [Serializable]
        private sealed class PublicRuntimeApi
        {
            public string assembly;
            public int signatureCount;
            public string sha256;
        }

        [Serializable]
        private sealed class FoldScriptFixture
        {
            public string id;
            public string path;
            public string sourceSha256;
            public string canonicalSha256;
        }

        [Serializable]
        private sealed class Rollback
        {
            public string packageVersion;
            public string tag;
            public string gitCommit;
            public string sourceAuthority;
        }

        [Serializable]
        private sealed class PriorRelease
        {
            public string tag;
            public int releaseId;
            public string archiveSha256;
            public string manifestSha256;
            public string evidenceSha256;
            public bool immutable;
        }
    }
}
