using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal static class FoldCanvasHandoffPersistence
    {
        private const string SourceJsonName = "source.foldcanvas.json";

        private const string AppearanceName = "appearance.png";

        private const string SourceAssetName = "FoldCanvasSource.asset";

        private const string MeshName = "GeneratedMesh.asset";

        private const string MaterialName = "Appearance.mat";

        private const string PrefabName = "Runtime.prefab";

        private const string ReceiptName = "handoff-receipt.json";

        private const string OneSidedShader =
            "FoldCanvas/One-Sided Unlit Texture";

        public static FoldCanvasHandoffImportResult Persist(
            FoldCanvasVerifiedHandoff verified,
            string destination)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            FoldCanvasHandoffReceipt expectedReceipt = CreateReceipt(
                verified,
                destination);
            if (AssetDatabase.IsValidFolder(destination) ||
                ProjectPathExists(destination))
            {
                if (TryValidateExistingImport(
                        verified,
                        expectedReceipt,
                        out FoldCanvasHandoffReceipt existingReceipt))
                {
                    return new FoldCanvasHandoffImportResult(
                        destination,
                        verified.Archive.ArchiveSha256,
                        verified.Manifest,
                        verified.Evidence,
                        existingReceipt,
                        true,
                        diagnostics);
                }

                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffDestinationOccupied,
                    "Handoff destination already exists but is not an intact receipt-owned import of this archive."));
                return Failure(diagnostics);
            }

            string parent = destination.Substring(
                0,
                destination.LastIndexOf('/'));
            string folderName = destination.Substring(
                destination.LastIndexOf('/') + 1);
            bool created = false;
            FoldCanvasCompileResult persistedCompile = null;
            GameObject prefabRoot = null;
            try
            {
                string guid = AssetDatabase.CreateFolder(parent, folderName);
                if (string.IsNullOrWhiteSpace(guid) ||
                    !AssetDatabase.IsValidFolder(destination))
                {
                    throw new InvalidOperationException(
                        "Unity did not create the owned destination folder.");
                }

                created = true;
                string sourceJsonPath = PathAt(
                    destination,
                    SourceJsonName);
                string appearancePath = PathAt(
                    destination,
                    AppearanceName);
                WriteProjectFile(
                    sourceJsonPath,
                    verified.Archive.Entries[
                        FoldCanvasHandoffFormat.SourceEntry]);
                WriteProjectFile(
                    appearancePath,
                    verified.Archive.Entries[
                        FoldCanvasHandoffFormat.AppearanceEntry]);
                AssetDatabase.ImportAsset(
                    sourceJsonPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(
                    appearancePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                if (!TryConfigureTexture(
                        appearancePath,
                        verified.Manifest.Texture))
                {
                    throw new InvalidOperationException(
                        "Imported PNG texture settings could not be applied.");
                }

                Texture2D texture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(appearancePath);
                if (texture == null ||
                    texture.width != verified.Manifest.CanvasWidth ||
                    texture.height != verified.Manifest.CanvasHeight)
                {
                    throw new InvalidOperationException(
                        "Imported PNG dimensions do not match the handoff manifest.");
                }

                FoldScriptImportResult import = FoldScriptImporter.Import(
                    verified.CanonicalSource,
                    new FoldScriptImportOptions
                    {
                        SourceProjectPath = sourceJsonPath,
                        AppearanceResolver =
                            new FoldScriptEditorAppearanceResolver(),
                        RequireAppearance = true,
                    });
                if (!import.Success)
                {
                    throw new InvalidOperationException(
                        "Canonical FoldScript could not be converted to a persisted source asset.");
                }

                string sourceAssetPath = PathAt(
                    destination,
                    SourceAssetName);
                AssetDatabase.CreateAsset(import.Asset, sourceAssetPath);
                FoldCanvasAsset persistedSource = import.Asset;
                persistedCompile = FoldCanvasCompiler.Compile(persistedSource);
                if (!persistedCompile.Success ||
                    persistedCompile.CompiledData == null)
                {
                    throw new InvalidOperationException(
                        "Persisted FoldCanvas source did not compile successfully.");
                }

                if (!EvidenceMatches(
                        verified,
                        persistedSource,
                        persistedCompile,
                        out string persistedObj))
                {
                    throw new InvalidOperationException(
                        "Persisted source evidence differs from detached verification.");
                }

                if (!string.Equals(
                        persistedObj,
                        verified.ObjText,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Persisted OBJ evidence differs from the verified handoff.");
                }

                string meshPath = PathAt(destination, MeshName);
                persistedCompile.Mesh.name = "FoldCanvas Generated Mesh";
                AssetDatabase.CreateAsset(persistedCompile.Mesh, meshPath);

                Shader shader = Shader.Find(OneSidedShader);
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        $"Required shader '{OneSidedShader}' is unavailable.");
                }

                Material material = new Material(shader)
                {
                    name = "FoldCanvas One-Sided Appearance",
                    mainTexture = texture,
                    color = Color.white,
                };
                string materialPath = PathAt(destination, MaterialName);
                AssetDatabase.CreateAsset(material, materialPath);

                prefabRoot = new GameObject(
                    SafeObjectName(verified.Manifest.DisplayName));
                MeshFilter filter = prefabRoot.AddComponent<MeshFilter>();
                filter.sharedMesh = persistedCompile.Mesh;
                MeshRenderer renderer =
                    prefabRoot.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                string prefabPath = PathAt(destination, PrefabName);
                if (PrefabUtility.SaveAsPrefabAsset(
                        prefabRoot,
                        prefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Runtime Prefab could not be created.");
                }

                UnityEngine.Object.DestroyImmediate(prefabRoot);
                prefabRoot = null;

                string receiptPath = PathAt(destination, ReceiptName);
                WriteProjectFile(
                    receiptPath,
                    new UTF8Encoding(false).GetBytes(
                        FoldCanvasHandoffJson.WriteReceipt(
                            expectedReceipt)));
                AssetDatabase.ImportAsset(
                    receiptPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();

                if (!TryValidateExistingImport(
                        verified,
                        expectedReceipt,
                        out FoldCanvasHandoffReceipt finalReceipt))
                {
                    throw new InvalidOperationException(
                        "Created handoff assets failed their ownership receipt verification.");
                }

                return new FoldCanvasHandoffImportResult(
                    destination,
                    verified.Archive.ArchiveSha256,
                    verified.Manifest,
                    verified.Evidence,
                    finalReceipt,
                    false,
                    diagnostics);
            }
            catch (Exception exception)
            {
                if (prefabRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(prefabRoot);
                }

                if (persistedCompile?.Mesh != null &&
                    !AssetDatabase.Contains(persistedCompile.Mesh))
                {
                    UnityEngine.Object.DestroyImmediate(
                        persistedCompile.Mesh);
                }

                if (created)
                {
                    AssetDatabase.DeleteAsset(destination);
                    AssetDatabase.Refresh();
                }

                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffAssetCreationFailed,
                    "Handoff persistence failed and the new owned folder was rolled back: " +
                    exception.Message));
                return Failure(diagnostics);
            }
        }

        private static bool TryValidateExistingImport(
            FoldCanvasVerifiedHandoff verified,
            FoldCanvasHandoffReceipt expected,
            out FoldCanvasHandoffReceipt receipt)
        {
            receipt = null;
            if (!TryReadProjectText(
                    expected.ReceiptPath,
                    out string receiptJson) ||
                !FoldCanvasHandoffJson.TryReadReceipt(
                    receiptJson,
                    out FoldCanvasHandoffReceipt actual,
                    out _) ||
                !ReceiptsMatch(expected, actual))
            {
                return false;
            }

            if (!TryReadProjectBytes(
                    actual.SourceJsonPath,
                    out byte[] sourceBytes) ||
                !TryReadProjectBytes(
                    actual.AppearancePath,
                    out byte[] appearanceBytes) ||
                !string.Equals(
                    FoldCanvasHandoffHash.Bytes(sourceBytes),
                    actual.SourceSha256,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    FoldCanvasHandoffHash.Bytes(appearanceBytes),
                    actual.AppearanceSha256,
                    StringComparison.Ordinal))
            {
                return false;
            }

            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    actual.SourceAssetPath);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(actual.MeshPath);
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(actual.MaterialPath);
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(actual.PrefabPath);
            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    actual.AppearancePath);
            if (source == null || mesh == null || material == null ||
                prefab == null || texture == null ||
                material.shader == null ||
                !string.Equals(
                    material.shader.name,
                    OneSidedShader,
                    StringComparison.Ordinal) ||
                material.mainTexture != texture)
            {
                return false;
            }

            MeshFilter filter = prefab.GetComponent<MeshFilter>();
            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null ||
                filter.sharedMesh != mesh ||
                renderer.sharedMaterial != material)
            {
                return false;
            }

            FoldCanvasCompileResult compile = FoldCanvasCompiler.Compile(source);
            try
            {
                if (!compile.Success || compile.CompiledData == null ||
                    !EvidenceMatches(
                        verified,
                        source,
                        compile,
                        out string obj) ||
                    !string.Equals(
                        obj,
                        verified.ObjText,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
            finally
            {
                if (compile.Mesh != null &&
                    !AssetDatabase.Contains(compile.Mesh))
                {
                    UnityEngine.Object.DestroyImmediate(compile.Mesh);
                }
            }

            receipt = actual;
            return true;
        }

        private static bool EvidenceMatches(
            FoldCanvasVerifiedHandoff verified,
            FoldCanvasAsset source,
            FoldCanvasCompileResult compile,
            out string objText)
        {
            objText = null;
            FoldScriptDocument document;
            FoldScriptWriteResult write;
            try
            {
                document = FoldScriptAssetConverter.ToDocument(
                    source,
                    FoldCanvasHandoffFormat.AppearanceEntry);
                write = FoldScriptSerializer.Write(document);
            }
            catch
            {
                return false;
            }

            if (!write.Success ||
                !string.Equals(
                    write.Json,
                    verified.CanonicalSource,
                    StringComparison.Ordinal))
            {
                return false;
            }

            FoldCanvasTextExportResult obj =
                FoldCanvasObjExporter.Export(
                    compile.CompiledData,
                    new FoldCanvasObjExportOptions
                    {
                        ObjectName = verified.Manifest.AssetId,
                        FlipTextureV = false,
                        IncludeMetadataComments = true,
                    });
            if (!obj.Success)
            {
                return false;
            }

            byte[] appearanceBytes =
                verified.Archive.Entries[
                    FoldCanvasHandoffFormat.AppearanceEntry];
            FoldCanvasHandoffEvidence evidence =
                FoldCanvasHandoffEvidenceBuilder.Build(
                    document.AssetId,
                    write.Json,
                    appearanceBytes,
                    obj.Content,
                    compile);
            objText = obj.Content;
            return string.Equals(
                FoldCanvasHandoffJson.WriteEvidence(evidence),
                FoldCanvasHandoffJson.WriteEvidence(verified.Evidence),
                StringComparison.Ordinal);
        }

        private static FoldCanvasHandoffReceipt CreateReceipt(
            FoldCanvasVerifiedHandoff verified,
            string destination)
        {
            return new FoldCanvasHandoffReceipt
            {
                ArchiveSha256 = verified.Archive.ArchiveSha256,
                AssetId = verified.Manifest.AssetId,
                PackageVersion = verified.Manifest.PackageVersion,
                CompilerVersion = verified.Manifest.CompilerVersion,
                SourceSha256 = verified.Evidence.SourceSha256,
                AppearanceSha256 = verified.Evidence.AppearanceSha256,
                GeometrySha256 = verified.Evidence.GeometrySha256,
                ObjSha256 = verified.Evidence.ObjSha256,
                SourceJsonPath = PathAt(destination, SourceJsonName),
                AppearancePath = PathAt(destination, AppearanceName),
                SourceAssetPath = PathAt(destination, SourceAssetName),
                MeshPath = PathAt(destination, MeshName),
                MaterialPath = PathAt(destination, MaterialName),
                PrefabPath = PathAt(destination, PrefabName),
                ReceiptPath = PathAt(destination, ReceiptName),
            };
        }

        private static bool ReceiptsMatch(
            FoldCanvasHandoffReceipt expected,
            FoldCanvasHandoffReceipt actual)
        {
            return string.Equals(
                FoldCanvasHandoffJson.WriteReceipt(expected),
                FoldCanvasHandoffJson.WriteReceipt(actual),
                StringComparison.Ordinal);
        }

        private static bool TryConfigureTexture(
            string path,
            FoldCanvasHandoffTextureContract contract)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null ||
                !Enum.TryParse(contract.FilterMode, out FilterMode filter) ||
                !Enum.TryParse(
                    contract.WrapModeU,
                    out TextureWrapMode wrapU) ||
                !Enum.TryParse(
                    contract.WrapModeV,
                    out TextureWrapMode wrapV))
            {
                return false;
            }

            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.filterMode = filter;
            importer.wrapModeU = wrapU;
            importer.wrapModeV = wrapV;
            importer.mipmapEnabled = contract.MipmapEnabled;
            importer.sRGBTexture = contract.SrgbTexture;
            importer.alphaIsTransparency = contract.AlphaIsTransparency;
            importer.anisoLevel = contract.AnisoLevel;
            importer.spritePixelsPerUnit = contract.PixelsPerUnit;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return true;
        }

        private static string SafeObjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "FoldCanvas Runtime Asset";
            }

            return value.Replace('/', '_').Replace('\\', '_');
        }

        private static string PathAt(string folder, string fileName)
        {
            return folder + "/" + fileName;
        }

        private static bool ProjectPathExists(string projectPath)
        {
            return TryGetAbsoluteProjectPath(
                projectPath,
                out string absolute) &&
                (Directory.Exists(absolute) || File.Exists(absolute));
        }

        private static void WriteProjectFile(
            string projectPath,
            byte[] bytes)
        {
            if (!TryGetAbsoluteProjectPath(
                    projectPath,
                    out string absolutePath))
            {
                throw new InvalidOperationException(
                    "Owned project path could not be resolved.");
            }

            File.WriteAllBytes(absolutePath, bytes);
        }

        private static bool TryReadProjectText(
            string projectPath,
            out string text)
        {
            text = null;
            if (!TryReadProjectBytes(projectPath, out byte[] bytes))
            {
                return false;
            }

            try
            {
                text = new UTF8Encoding(false, true).GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static bool TryReadProjectBytes(
            string projectPath,
            out byte[] bytes)
        {
            bytes = null;
            if (!TryGetAbsoluteProjectPath(
                    projectPath,
                    out string absolutePath) ||
                !File.Exists(absolutePath))
            {
                return false;
            }

            bytes = File.ReadAllBytes(absolutePath);
            return true;
        }

        private static bool TryGetAbsoluteProjectPath(
            string projectPath,
            out string absolutePath)
        {
            absolutePath = null;
            if (string.IsNullOrWhiteSpace(projectPath) ||
                !projectPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }

            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            string rootPrefix = projectRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(
                Path.Combine(projectRoot, projectPath));
            if (!candidate.StartsWith(rootPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            absolutePath = candidate;
            return true;
        }

        private static FoldCanvasHandoffImportResult Failure(
            IList<FoldCanvasDiagnostic> diagnostics)
        {
            return new FoldCanvasHandoffImportResult(
                null,
                null,
                null,
                null,
                null,
                false,
                diagnostics);
        }

        private static FoldCanvasDiagnostic Error(
            string code,
            string message)
        {
            return new FoldCanvasDiagnostic(
                code,
                FoldCanvasDiagnosticSeverity.Error,
                message);
        }
    }
}
