using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasObjEditorExporter
    {
        public static bool ExportToAssetPath(
            FoldCanvasCompiledData data,
            string assetPath,
            FoldCanvasObjExportOptions options,
            out FoldCanvasTextExportResult result)
        {
            result = FoldCanvasObjExporter.Export(data, options);
            if (!result.Success)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(assetPath) ||
                !assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !assetPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
                assetPath.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                result = new FoldCanvasTextExportResult(
                    null,
                    new[]
                    {
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes.InvalidExportOptions,
                            FoldCanvasDiagnosticSeverity.Error,
                            "Editor OBJ output must be one normalized .obj path under Assets/.")
                    });
                return false;
            }

            string fullPath = Path.GetFullPath(assetPath);
            string projectAssets = Path.GetFullPath("Assets") +
                Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(
                    projectAssets,
                    StringComparison.Ordinal))
            {
                result = new FoldCanvasTextExportResult(
                    null,
                    new[]
                    {
                        new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes.InvalidExportOptions,
                            FoldCanvasDiagnosticSeverity.Error,
                            "Editor OBJ output escaped the project Assets folder.")
                    });
                return false;
            }

            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                fullPath,
                result.Content,
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            return true;
        }
    }
}
