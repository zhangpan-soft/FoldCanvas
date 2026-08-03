using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace FoldCanvas.Tests
{
    public sealed class M10GalleryManifestTests
    {
        [Test]
        public void GalleryManifest_CanonicalPackageManifestParses()
        {
            string json = M08FoldScriptFixtures.LoadPackageText(
                "Samples~/Gallery/gallery.json");

            bool parsed = FoldCanvasGalleryManifest.TryParse(
                json,
                out FoldCanvasGalleryManifest manifest,
                out IReadOnlyList<FoldCanvasDiagnostic> diagnostics);

            Assert.That(parsed, Is.True, Diagnostics(diagnostics));
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Format,
                Is.EqualTo(FoldCanvasGalleryManifest.FormatName));
            Assert.That(manifest.Version,
                Is.EqualTo(FoldCanvasGalleryManifest.CurrentVersion));
            Assert.That(manifest.Entries.Count, Is.EqualTo(4));
        }

        [Test]
        public void GalleryManifest_EntryOrderIsPreserved()
        {
            string json = M08FoldScriptFixtures.LoadPackageText(
                "Samples~/Gallery/gallery.json");

            FoldCanvasGalleryManifest.TryParse(
                json,
                out FoldCanvasGalleryManifest manifest,
                out IReadOnlyList<FoldCanvasDiagnostic> diagnostics);

            Assert.That(diagnostics, Is.Empty);
            Assert.That(manifest.Entries[0].Id,
                Is.EqualTo("bootstrap-panel"));
            Assert.That(manifest.Entries[1].Id,
                Is.EqualTo("sphere-gores"));
            Assert.That(manifest.Entries[2].Id,
                Is.EqualTo("cyclic-topology"));
            Assert.That(manifest.Entries[3].Id,
                Is.EqualTo("operation-extension"));
        }

        [Test]
        public void GalleryManifest_DuplicateIdReturnsStableDiagnostic()
        {
            string json = Root(Entry("same"), Entry("same"));

            bool parsed = FoldCanvasGalleryManifest.TryParse(
                json,
                out FoldCanvasGalleryManifest manifest,
                out IReadOnlyList<FoldCanvasDiagnostic> diagnostics);

            Assert.That(parsed, Is.False);
            Assert.That(manifest, Is.Null);
            AssertOnlyCode(
                diagnostics,
                FoldCanvasDiagnosticCodes.DuplicateGalleryEntryId);
            Assert.That(diagnostics[0].Values[0].Key,
                Is.EqualTo("entryIndex"));
            Assert.That(diagnostics[0].Values[0].Value, Is.EqualTo(1d));
        }

        [Test]
        public void GalleryManifest_UnsafePathReturnsStableDiagnostic()
        {
            string json = Root(Entry(
                "unsafe",
                "Samples~/../Project~/secret.txt"));

            bool parsed = FoldCanvasGalleryManifest.TryParse(
                json,
                out _,
                out IReadOnlyList<FoldCanvasDiagnostic> diagnostics);

            Assert.That(parsed, Is.False);
            AssertOnlyCode(
                diagnostics,
                FoldCanvasDiagnosticCodes.InvalidGalleryEntry);
        }

        [Test]
        public void GalleryManifest_UnknownVersionReturnsStableDiagnostic()
        {
            string json = Root(Entry("entry"))
                .Replace("\"version\":\"1\"", "\"version\":\"2\"");

            bool parsed = FoldCanvasGalleryManifest.TryParse(
                json,
                out _,
                out IReadOnlyList<FoldCanvasDiagnostic> diagnostics);

            Assert.That(parsed, Is.False);
            AssertOnlyCode(
                diagnostics,
                FoldCanvasDiagnosticCodes.UnsupportedGalleryVersion);
        }

        [Test]
        public void GalleryManifest_MalformedJsonReturnsGalleryDiagnostic()
        {
            bool parsed = FoldCanvasGalleryManifest.TryParse(
                "{",
                out _,
                out IReadOnlyList<FoldCanvasDiagnostic> diagnostics);

            Assert.That(parsed, Is.False);
            AssertOnlyCode(
                diagnostics,
                FoldCanvasDiagnosticCodes.MalformedGalleryManifest);
        }

        [Test]
        public void GalleryWindow_OpensCanonicalManifest()
        {
            FoldCanvas.Editor.FoldCanvasSampleGalleryWindow window =
                EditorWindow.GetWindow<
                    FoldCanvas.Editor.FoldCanvasSampleGalleryWindow>();
            try
            {
                window.LoadManifestForTests();

                Assert.That(window.DiagnosticsForTests, Is.Empty);
                Assert.That(window.EntriesForTests, Is.Not.Null);
                Assert.That(window.EntriesForTests.Count, Is.EqualTo(4));
            }
            finally
            {
                window.Close();
            }
        }

        private static string Root(params string[] entries)
        {
            return "{" +
                "\"format\":\"foldcanvas-gallery\"," +
                "\"version\":\"1\"," +
                "\"entries\":[" + string.Join(",", entries) + "]}";
        }

        private static string Entry(
            string id,
            string samplePath = "Samples~/BootstrapPanel")
        {
            return "{" +
                "\"id\":\"" + id + "\"," +
                "\"title\":\"Title\"," +
                "\"description\":\"Description\"," +
                "\"samplePath\":\"" + samplePath + "\"," +
                "\"minimumPackageVersion\":\"0.1.0-preview.1\"," +
                "\"tags\":[\"sample\"]}";
        }

        private static void AssertOnlyCode(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics,
            string code)
        {
            Assert.That(diagnostics.Count, Is.EqualTo(1),
                Diagnostics(diagnostics));
            Assert.That(diagnostics[0].Code, Is.EqualTo(code));
        }

        private static string Diagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < diagnostics.Count; i++)
            {
                builder.AppendLine(diagnostics[i].ToString());
            }

            return builder.ToString();
        }
    }
}
