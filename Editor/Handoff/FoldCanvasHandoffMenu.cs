using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal static class FoldCanvasHandoffMenu
    {
        [MenuItem(
            "Tools/FoldCanvas/Handoff/Export Selected Source...",
            priority = 120)]
        private static void ExportSelected()
        {
            FoldCanvasAsset source = Selection.activeObject as FoldCanvasAsset;
            if (source == null)
            {
                EditorUtility.DisplayDialog(
                    "FoldCanvas Handoff",
                    "Select a FoldCanvas source asset before exporting.",
                    "OK");
                return;
            }

            string safeName = SafeName(source.SourceMetadata.AssetId);
            string destination = EditorUtility.SaveFilePanel(
                "Export FoldCanvas production handoff",
                string.Empty,
                safeName + ".foldcanvas",
                "zip");
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            if (!destination.EndsWith(
                    ".foldcanvas.zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                destination = destination.EndsWith(
                    ".zip",
                    StringComparison.OrdinalIgnoreCase)
                    ? destination.Substring(0, destination.Length - 4) +
                        ".foldcanvas.zip"
                    : destination + ".foldcanvas.zip";
            }

            FoldCanvasHandoffExportResult result =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = destination,
                    });
            Report("Export", result.Success, result.Diagnostics,
                result.Success
                    ? $"Archive: {result.ArchivePath}\nSHA-256: " +
                        result.ArchiveSha256
                    : null);
        }

        [MenuItem(
            "Tools/FoldCanvas/Handoff/Export Selected Source...",
            true)]
        private static bool CanExportSelected()
        {
            return Selection.activeObject is FoldCanvasAsset;
        }

        [MenuItem(
            "Tools/FoldCanvas/Handoff/Import Archive...",
            priority = 121)]
        private static void ImportArchive()
        {
            string archive = EditorUtility.OpenFilePanel(
                "Import FoldCanvas production handoff",
                string.Empty,
                "zip");
            if (string.IsNullOrWhiteSpace(archive))
            {
                return;
            }

            string parentAbsolute = EditorUtility.OpenFolderPanel(
                "Choose existing Assets parent folder",
                Application.dataPath,
                string.Empty);
            if (string.IsNullOrWhiteSpace(parentAbsolute))
            {
                return;
            }

            string parent = FileUtil.GetProjectRelativePath(parentAbsolute)
                .TrimEnd('/');
            if (string.IsNullOrWhiteSpace(parent) ||
                !parent.StartsWith("Assets", StringComparison.Ordinal) ||
                !AssetDatabase.IsValidFolder(parent))
            {
                EditorUtility.DisplayDialog(
                    "FoldCanvas Handoff",
                    "The import parent must be an existing folder under Assets/.",
                    "OK");
                return;
            }

            string fileName = Path.GetFileName(archive);
            const string suffix = ".foldcanvas.zip";
            string logicalName = fileName.EndsWith(
                    suffix,
                    StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - suffix.Length)
                : Path.GetFileNameWithoutExtension(fileName);
            string destination = parent + "/" + SafeName(logicalName);
            FoldCanvasHandoffImportResult result =
                FoldCanvasHandoffImporter.Import(
                    new FoldCanvasHandoffImportOptions
                    {
                        ArchivePath = archive,
                        DestinationFolder = destination,
                    });
            if (result.Success)
            {
                FoldCanvasAsset source =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                        result.Receipt.SourceAssetPath);
                Selection.activeObject = source;
                EditorGUIUtility.PingObject(source);
            }

            Report("Import", result.Success, result.Diagnostics,
                result.Success
                    ? $"Destination: {result.DestinationFolder}\n" +
                        (result.WasAlreadyImported
                            ? "The same intact archive was already imported; no files changed."
                            : "Editable source and runtime outputs were created.")
                    : null);
        }

        [MenuItem(
            "Tools/FoldCanvas/Handoff/Rebuild Selected Import",
            priority = 122)]
        private static void RebuildSelectedImport()
        {
            string selectedPath =
                AssetDatabase.GetAssetPath(Selection.activeObject);
            string folder = AssetDatabase.IsValidFolder(selectedPath)
                ? selectedPath
                : Path.GetDirectoryName(selectedPath)?.Replace('\\', '/');
            FoldCanvasHandoffRebuildResult result =
                FoldCanvasHandoffRebuilder.Rebuild(folder);
            Report("Rebuild", result.Success, result.Diagnostics,
                result.Success
                    ? "Receipt-owned Mesh, Material, and Prefab were rebuilt from the editable 2D source."
                    : null);
        }

        [MenuItem(
            "Tools/FoldCanvas/Handoff/Rebuild Selected Import",
            true)]
        private static bool CanRebuildSelectedImport()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string folder = AssetDatabase.IsValidFolder(path)
                ? path
                : Path.GetDirectoryName(path)?.Replace('\\', '/');
            return !string.IsNullOrWhiteSpace(folder) &&
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    folder + "/handoff-receipt.json") != null;
        }

        private static void Report(
            string action,
            bool success,
            System.Collections.Generic.IReadOnlyList<
                FoldCanvasDiagnostic> diagnostics,
            string successMessage)
        {
            if (success)
            {
                Debug.Log("FoldCanvas handoff " + action.ToLowerInvariant() +
                    " succeeded. " + successMessage);
                EditorUtility.DisplayDialog(
                    "FoldCanvas Handoff " + action,
                    successMessage,
                    "OK");
                return;
            }

            string message = diagnostics != null && diagnostics.Count > 0
                ? diagnostics[0].ToString()
                : "The operation failed without a diagnostic.";
            Debug.LogError(message);
            EditorUtility.DisplayDialog(
                "FoldCanvas Handoff " + action,
                message,
                "OK");
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "FoldCanvasAsset";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string safe = value.Trim();
            for (int i = 0; i < invalid.Length; i++)
            {
                safe = safe.Replace(invalid[i], '_');
            }

            safe = safe.Replace('/', '_').Replace('\\', '_');
            return string.IsNullOrWhiteSpace(safe)
                ? "FoldCanvasAsset"
                : safe;
        }
    }
}
