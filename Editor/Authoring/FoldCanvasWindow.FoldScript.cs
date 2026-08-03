using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public sealed partial class FoldCanvasWindow
    {
        private void ImportFoldScript()
        {
            string absoluteSource = EditorUtility.OpenFilePanel(
                "Import FoldScript 0.1",
                Application.dataPath,
                "json");
            if (string.IsNullOrEmpty(absoluteSource))
            {
                return;
            }

            if (!FoldScriptEditorAssetUtility.TryMakeProjectRelative(
                    absoluteSource,
                    out string sourceProjectPath))
            {
                ShowNotification(new GUIContent(
                    "FC7006: FoldScript must be under Assets/ or Packages/."));
                return;
            }

            string defaultName = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(absoluteSource));
            string outputPath = EditorUtility.SaveFilePanelInProject(
                "Save imported FoldCanvas source",
                defaultName,
                "asset",
                "Choose the explicit source asset written by this import.");
            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }

            if (!FoldScriptEditorAssetUtility.TryImport(
                    sourceProjectPath,
                    outputPath,
                    out FoldCanvasAsset imported,
                    out FoldScriptImportResult result))
            {
                ShowNotification(new GUIContent(
                    FirstDiagnosticCode(result?.Diagnostics) +
                    ": FoldScript import failed."));
                return;
            }

            Selection.activeObject = imported;
            EditorGUIUtility.PingObject(imported);
            SetSource(imported);
            CompileNow();
            ShowNotification(new GUIContent(
                "Imported canonical FoldScript source."));
        }

        private void ExportFoldScript()
        {
            if (Source == null)
            {
                ShowNotification(new GUIContent(
                    "Select a FoldCanvas source before exporting."));
                return;
            }

            string defaultName = string.IsNullOrWhiteSpace(
                    Source.SourceMetadata.AssetId)
                ? Source.name
                : Source.SourceMetadata.AssetId;
            string outputPath = EditorUtility.SaveFilePanelInProject(
                "Export canonical FoldScript 0.1",
                defaultName + ".foldcanvas",
                "json",
                "Export deterministic source JSON under Assets/.");
            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }

            if (!FoldScriptEditorAssetUtility.TryExport(
                    Source,
                    outputPath,
                    out FoldScriptWriteResult result))
            {
                ShowNotification(new GUIContent(
                    FirstDiagnosticCode(result?.Diagnostics) +
                    ": FoldScript export failed."));
                return;
            }

            AssetDatabase.Refresh();
            UnityEngine.Object exported =
                AssetDatabase.LoadMainAssetAtPath(outputPath);
            if (exported != null)
            {
                EditorGUIUtility.PingObject(exported);
            }

            ShowNotification(new GUIContent(
                "Exported canonical FoldScript: " + outputPath));
        }

        private void CopyRepairPayload()
        {
            if (!FoldScriptEditorAssetUtility.TryCreateRepairPayload(
                    Source,
                    out string payload,
                    out FoldCanvasCompileResult compileResult,
                    out FoldCanvasDiagnostic error))
            {
                ShowNotification(new GUIContent(
                    (error?.Code ?? "FC7011") +
                    ": Repair payload failed."));
                return;
            }

            EditorGUIUtility.systemCopyBuffer = payload;
            if (compileResult?.Mesh != null)
            {
                DestroyImmediate(compileResult.Mesh);
            }

            ShowNotification(new GUIContent(
                "Copied provider-neutral repair payload."));
        }

        private static string FirstDiagnosticCode(
            System.Collections.Generic.IReadOnlyList<FoldCanvasDiagnostic>
                diagnostics)
        {
            return diagnostics != null && diagnostics.Count > 0
                ? diagnostics[0].Code
                : FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure;
        }
    }
}
