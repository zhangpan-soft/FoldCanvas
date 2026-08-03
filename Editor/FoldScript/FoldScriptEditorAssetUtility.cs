using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Editor
{
    internal sealed class FoldScriptEditorAppearanceResolver :
        IFoldScriptAppearanceResolver
    {
        public bool TryResolve(
            string projectRelativePath,
            out Texture2D appearance)
        {
            appearance = AssetDatabase.LoadAssetAtPath<Texture2D>(
                projectRelativePath);
            return appearance != null;
        }
    }

    internal static class FoldScriptEditorAssetUtility
    {
        public static bool TryImport(
            string foldScriptProjectPath,
            string outputAssetPath,
            out FoldCanvasAsset asset,
            out FoldScriptImportResult importResult)
        {
            asset = null;
            importResult = null;
            if (!FoldScriptProjectPath.TryNormalizeProjectPath(
                    foldScriptProjectPath,
                    out string normalizedSource,
                    out FoldCanvasDiagnostic sourceDiagnostic))
            {
                importResult = Failure(sourceDiagnostic);
                return false;
            }

            FoldCanvasDiagnostic outputDiagnostic = null;
            string normalizedOutput = null;
            bool outputPathValid = !string.IsNullOrWhiteSpace(outputAssetPath) &&
                outputAssetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                outputAssetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) &&
                FoldScriptProjectPath.TryNormalizeProjectPath(
                    outputAssetPath,
                    out normalizedOutput,
                    out outputDiagnostic);
            if (!outputPathValid)
            {
                importResult = Failure(outputDiagnostic ??
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsafeAppearancePath,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Imported FoldCanvas assets must be explicit .asset paths under Assets/."));
                return false;
            }

            if (!TryGetAbsoluteProjectPath(
                    normalizedSource,
                    out string absoluteSource) ||
                !File.Exists(absoluteSource))
            {
                importResult = Failure(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"FoldScript source '{normalizedSource}' does not exist inside the Unity project."));
                return false;
            }

            string json;
            try
            {
                FileInfo info = new FileInfo(absoluteSource);
                if (info.Length > FoldCanvasLimits.MaximumFoldScriptCharacters * 4L)
                {
                    importResult = Failure(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded,
                        FoldCanvasDiagnosticSeverity.Error,
                        "FoldScript source file exceeds the byte-size safety limit before reading."));
                    return false;
                }

                json = File.ReadAllText(absoluteSource);
            }
            catch (Exception exception)
            {
                importResult = Failure(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.MalformedFoldScript,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript source could not be read: " + exception.Message));
                return false;
            }

            importResult = FoldScriptImporter.Import(
                json,
                new FoldScriptImportOptions
                {
                    SourceProjectPath = normalizedSource,
                    AppearanceResolver =
                        new FoldScriptEditorAppearanceResolver(),
                    RequireAppearance = true
                });
            if (!importResult.Success)
            {
                return false;
            }

            FoldCanvasAsset imported = importResult.Asset;
            UnityEngine.Object occupied =
                AssetDatabase.LoadMainAssetAtPath(normalizedOutput);
            FoldCanvasAsset existing =
                occupied as FoldCanvasAsset;
            if (occupied != null && existing == null)
            {
                UnityEngine.Object.DestroyImmediate(imported);
                importResult = Failure(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Import target '{normalizedOutput}' already contains a non-FoldCanvas asset."));
                return false;
            }

            try
            {
                if (existing == null)
                {
                    EnsureAssetFolder(normalizedOutput);
                    AssetDatabase.CreateAsset(imported, normalizedOutput);
                    asset = imported;
                }
                else
                {
                    Undo.RecordObject(existing, "Import FoldScript");
                    EditorUtility.CopySerialized(imported, existing);
                    UnityEngine.Object.DestroyImmediate(imported);
                    EditorUtility.SetDirty(existing);
                    asset = existing;
                    importResult = new FoldScriptImportResult(
                        importResult.Document,
                        existing,
                        importResult.CanonicalJson,
                        importResult.ResolvedAppearancePath,
                        new List<FoldCanvasDiagnostic>());
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(normalizedOutput);
                return true;
            }
            catch (Exception exception)
            {
                if (imported != null && !AssetDatabase.Contains(imported))
                {
                    UnityEngine.Object.DestroyImmediate(imported);
                }

                asset = null;
                importResult = Failure(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript source asset could not be persisted: " +
                    exception.Message));
                return false;
            }
        }

        public static bool TryExport(
            FoldCanvasAsset asset,
            string foldScriptProjectPath,
            out FoldScriptWriteResult writeResult)
        {
            writeResult = null;
            if (asset == null)
            {
                writeResult = FailureWrite(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Select a FoldCanvas source before exporting FoldScript."));
                return false;
            }

            if (!FoldScriptProjectPath.TryNormalizeProjectPath(
                    foldScriptProjectPath,
                    out string normalizedOutput,
                    out FoldCanvasDiagnostic pathDiagnostic) ||
                !normalizedOutput.StartsWith("Assets/", StringComparison.Ordinal))
            {
                writeResult = FailureWrite(pathDiagnostic ??
                    new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsafeAppearancePath,
                        FoldCanvasDiagnosticSeverity.Error,
                        "FoldScript export must target an explicit path under Assets/."));
                return false;
            }

            string appearancePath = asset.Appearance != null
                ? AssetDatabase.GetAssetPath(asset.Appearance)
                : asset.SourceMetadata.AppearanceReference;
            FoldScriptDocument document;
            try
            {
                document = FoldScriptAssetConverter.ToDocument(
                    asset,
                    appearancePath);
            }
            catch (Exception exception)
            {
                writeResult = FailureWrite(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript export conversion failed: " + exception.Message));
                return false;
            }

            writeResult = FoldScriptSerializer.Write(document);
            if (!writeResult.Success)
            {
                return false;
            }

            if (!TryGetAbsoluteProjectPath(
                    normalizedOutput,
                    out string absoluteOutput))
            {
                writeResult = FailureWrite(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsafeAppearancePath,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript export path could not be mapped inside the Unity project."));
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutput));
                File.WriteAllText(
                    absoluteOutput,
                    writeResult.Json,
                    new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(normalizedOutput);
                return true;
            }
            catch (Exception exception)
            {
                writeResult = FailureWrite(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript export failed: " + exception.Message));
                return false;
            }
        }

        public static bool TryCreateRepairPayload(
            FoldCanvasAsset asset,
            out string payload,
            out FoldCanvasCompileResult compileResult,
            out FoldCanvasDiagnostic error)
        {
            payload = null;
            compileResult = null;
            error = null;
            if (asset == null)
            {
                error = new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Select a FoldCanvas source before creating a repair payload.");
                return false;
            }

            try
            {
                string appearancePath = asset.Appearance != null
                    ? AssetDatabase.GetAssetPath(asset.Appearance)
                    : asset.SourceMetadata.AppearanceReference;
                FoldScriptDocument document =
                    FoldScriptAssetConverter.ToDocument(asset, appearancePath);
                FoldScriptWriteResult write =
                    FoldScriptSerializer.Write(document);
                if (!write.Success)
                {
                    error = write.Diagnostics[0];
                    return false;
                }

                compileResult = FoldCanvasCompiler.Compile(asset);
                FoldCanvasRepairRequest request =
                    FoldCanvasRepairRequestBuilder.Create(
                        document,
                        compileResult,
                        write.Json);
                payload = request.ToCanonicalJson();
                return true;
            }
            catch (Exception exception)
            {
                if (compileResult?.Mesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(compileResult.Mesh);
                }

                compileResult = null;
                error = new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Repair payload creation rejected invalid source: " +
                    exception.Message);
                return false;
            }
        }

        public static bool TryMakeProjectRelative(
            string absolutePath,
            out string projectRelativePath)
        {
            projectRelativePath = null;
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            string fullPath = Path.GetFullPath(absolutePath)
                .Replace('\\', '/');
            string projectRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/')
                .TrimEnd('/');
            string assetsRoot = projectRoot + "/Assets/";
            if (fullPath.StartsWith(assetsRoot, StringComparison.Ordinal))
            {
                projectRelativePath = "Assets/" +
                    fullPath.Substring(assetsRoot.Length);
                return true;
            }

            PackageManagerPackageInfo[] packages =
                PackageManagerPackageInfo.GetAllRegisteredPackages() ??
                Array.Empty<PackageManagerPackageInfo>();
            Array.Sort(
                packages,
                (left, right) => string.CompareOrdinal(
                    left?.name,
                    right?.name));
            for (int i = 0; i < packages.Length; i++)
            {
                if (packages[i] == null ||
                    string.IsNullOrWhiteSpace(packages[i].name) ||
                    string.IsNullOrWhiteSpace(packages[i].resolvedPath))
                {
                    continue;
                }

                string packageRoot = Path.GetFullPath(packages[i].resolvedPath)
                    .Replace('\\', '/')
                    .TrimEnd('/') + "/";
                if (fullPath.StartsWith(
                        packageRoot,
                        StringComparison.Ordinal))
                {
                    projectRelativePath = "Packages/" + packages[i].name + "/" +
                        fullPath.Substring(packageRoot.Length);
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetAbsoluteProjectPath(
            string projectRelativePath,
            out string absolutePath)
        {
            absolutePath = null;
            if (projectRelativePath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                string projectRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
                absolutePath = Path.GetFullPath(
                    Path.Combine(projectRoot, projectRelativePath));
                return true;
            }

            if (!projectRelativePath.StartsWith(
                    "Packages/",
                    StringComparison.Ordinal))
            {
                return false;
            }

            string[] segments = projectRelativePath.Split('/');
            if (segments.Length < 3)
            {
                return false;
            }

            string packageName = segments[1];
            PackageManagerPackageInfo[] packages =
                PackageManagerPackageInfo.GetAllRegisteredPackages() ??
                Array.Empty<PackageManagerPackageInfo>();
            for (int i = 0; i < packages.Length; i++)
            {
                if (packages[i] == null ||
                    string.IsNullOrWhiteSpace(packages[i].name) ||
                    string.IsNullOrWhiteSpace(packages[i].resolvedPath))
                {
                    continue;
                }

                if (!string.Equals(
                        packages[i].name,
                        packageName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string relative = string.Join(
                    "/",
                    segments,
                    2,
                    segments.Length - 2);
                absolutePath = Path.GetFullPath(
                    Path.Combine(packages[i].resolvedPath, relative));
                return true;
            }

            return false;
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            if (slash <= "Assets".Length)
            {
                return;
            }

            string folder = assetPath.Substring(0, slash);
            string[] segments = folder.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static FoldScriptImportResult Failure(
            FoldCanvasDiagnostic diagnostic)
        {
            return new FoldScriptImportResult(
                null,
                null,
                null,
                null,
                new List<FoldCanvasDiagnostic> { diagnostic });
        }

        private static FoldScriptWriteResult FailureWrite(
            FoldCanvasDiagnostic diagnostic)
        {
            return new FoldScriptWriteResult(
                null,
                new List<FoldCanvasDiagnostic> { diagnostic });
        }
    }
}
