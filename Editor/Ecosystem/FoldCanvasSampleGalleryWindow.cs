using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public sealed class FoldCanvasSampleGalleryWindow : EditorWindow
    {
        private const string ManifestRelativePath =
            "Samples~/Gallery/gallery.json";

        private FoldCanvasGalleryManifest manifest;
        private IReadOnlyList<FoldCanvasDiagnostic> diagnostics;
        private Vector2 scroll;

        [MenuItem("Tools/FoldCanvas/Open Sample Gallery", priority = 2)]
        public static void Open()
        {
            FoldCanvasSampleGalleryWindow window =
                GetWindow<FoldCanvasSampleGalleryWindow>();
            window.titleContent = new GUIContent("FoldCanvas Gallery");
            window.minSize = new Vector2(560f, 360f);
            window.LoadManifest();
            window.Show();
        }

        internal IReadOnlyList<FoldCanvasGalleryEntry> EntriesForTests =>
            manifest?.Entries;

        internal IReadOnlyList<FoldCanvasDiagnostic> DiagnosticsForTests =>
            diagnostics;

        internal void LoadManifestForTests()
        {
            LoadManifest();
        }

        private void OnEnable()
        {
            LoadManifest();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "FoldCanvas Sample Gallery",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Every entry points to maintained two-dimensional source and explicit construction rules. Mesh previews and exports remain derived artifacts.",
                MessageType.Info);

            if (manifest == null)
            {
                DrawDiagnostics();
                if (GUILayout.Button("Reload manifest"))
                {
                    LoadManifest();
                }

                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                FoldCanvasGalleryEntry entry = manifest.Entries[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(
                        entry.Title,
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        entry.Description,
                        EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField(
                        "Source",
                        entry.SamplePath);
                    EditorGUILayout.LabelField(
                        "Minimum package",
                        entry.MinimumPackageVersion);
                    EditorGUILayout.LabelField(
                        "Tags",
                        string.Join(", ", entry.Tags));

                    using (new EditorGUI.DisabledScope(
                        string.IsNullOrEmpty(entry.ProofMenuPath)))
                    {
                        if (GUILayout.Button("Create proof"))
                        {
                            bool invoked = EditorApplication.ExecuteMenuItem(
                                entry.ProofMenuPath);
                            if (!invoked)
                            {
                                Debug.LogError(
                                    $"FoldCanvas gallery proof menu is unavailable: {entry.ProofMenuPath}");
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDiagnostics()
        {
            if (diagnostics == null || diagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The gallery manifest could not be loaded.",
                    MessageType.Error);
                return;
            }

            for (int i = 0; i < diagnostics.Count; i++)
            {
                EditorGUILayout.HelpBox(
                    diagnostics[i].ToString(),
                    MessageType.Error);
            }
        }

        private void LoadManifest()
        {
            manifest = null;
            diagnostics = null;
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(
                "Packages/com.foldcanvas.core");
            if (package == null)
            {
                diagnostics = new[]
                {
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .MalformedGalleryManifest,
                        FoldCanvasDiagnosticSeverity.Error,
                        "The com.foldcanvas.core package path could not be resolved.")
                };
                Repaint();
                return;
            }

            string path = Path.Combine(
                package.resolvedPath,
                ManifestRelativePath);
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException)
            {
                diagnostics = new[]
                {
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .MalformedGalleryManifest,
                        FoldCanvasDiagnosticSeverity.Error,
                        "The package gallery manifest could not be read.")
                };
                Repaint();
                return;
            }

            FoldCanvasGalleryManifest.TryParse(
                json,
                out manifest,
                out diagnostics);
            Repaint();
        }
    }
}
