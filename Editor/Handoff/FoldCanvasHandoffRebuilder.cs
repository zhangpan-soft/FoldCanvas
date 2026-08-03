using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasHandoffRebuilder
    {
        private const string OneSidedShader =
            "FoldCanvas/One-Sided Unlit Texture";

        public static FoldCanvasHandoffRebuildResult Rebuild(
            string destinationFolder)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (!FoldCanvasHandoffImporter.TryNormalizeDestination(
                    destinationFolder,
                    out string destination,
                    out FoldCanvasDiagnostic destinationDiagnostic))
            {
                diagnostics.Add(destinationDiagnostic);
                return Failure(diagnostics);
            }

            string receiptPath = destination + "/handoff-receipt.json";
            if (!TryReadProjectText(receiptPath, out string receiptJson) ||
                !FoldCanvasHandoffJson.TryReadReceipt(
                    receiptJson,
                    out FoldCanvasHandoffReceipt receipt,
                    out _))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Rebuild requires an intact canonical handoff receipt."));
                return Failure(diagnostics);
            }

            if (!ReceiptOwnsExactDestination(receipt, destination))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffDestinationOccupied,
                    "Rebuild receipt does not own the exact requested destination paths."));
                return Failure(diagnostics);
            }

            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    receipt.SourceAssetPath);
            Texture2D appearance =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    receipt.AppearancePath);
            if (source == null || appearance == null ||
                !TryReadProjectBytes(
                    receipt.SourceJsonPath,
                    out byte[] sourceBytes) ||
                !TryReadProjectBytes(
                    receipt.AppearancePath,
                    out byte[] appearanceBytes))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Rebuild requires the receipt-owned source asset, FoldScript, and PNG."));
                return Failure(diagnostics);
            }

            FoldCanvasCompileResult compile = null;
            GameObject root = null;
            try
            {
                FoldScriptDocument document =
                    FoldScriptAssetConverter.ToDocument(
                        source,
                        FoldCanvasHandoffFormat.AppearanceEntry);
                FoldScriptWriteResult write =
                    FoldScriptSerializer.Write(document);
                if (!write.Success ||
                    !ByteEquals(
                        sourceBytes,
                        new UTF8Encoding(false).GetBytes(write.Json)) ||
                    !string.Equals(
                        FoldCanvasHandoffHash.Text(write.Json),
                        receipt.SourceSha256,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        FoldCanvasHandoffHash.Bytes(appearanceBytes),
                        receipt.AppearanceSha256,
                        StringComparison.Ordinal))
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch,
                        "Receipt-owned source or appearance changed; rebuild will not overwrite derived evidence under the original receipt."));
                    return Failure(diagnostics);
                }

                compile = FoldCanvasCompiler.Compile(source);
                if (!compile.Success || compile.CompiledData == null)
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.HandoffCompileFailed,
                        "Receipt-owned source no longer compiles successfully."));
                    return Failure(diagnostics);
                }

                FoldCanvasTextExportResult obj =
                    FoldCanvasObjExporter.Export(
                        compile.CompiledData,
                        new FoldCanvasObjExportOptions
                        {
                            ObjectName = receipt.AssetId,
                            FlipTextureV = false,
                            IncludeMetadataComments = true,
                        });
                FoldCanvasHandoffEvidence evidence = obj.Success
                    ? FoldCanvasHandoffEvidenceBuilder.Build(
                        receipt.AssetId,
                        write.Json,
                        appearanceBytes,
                        obj.Content,
                        compile)
                    : null;
                if (evidence == null ||
                    !string.Equals(
                        evidence.GeometrySha256,
                        receipt.GeometrySha256,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        evidence.ObjSha256,
                        receipt.ObjSha256,
                        StringComparison.Ordinal))
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch,
                        "Recompiled geometry or OBJ does not match the handoff receipt."));
                    return Failure(diagnostics);
                }

                Mesh mesh = SaveOrReplaceMesh(
                    compile.Mesh,
                    receipt.MeshPath);
                compile.Mesh = mesh;
                Material material = SaveOrReplaceMaterial(
                    appearance,
                    receipt.MaterialPath);
                root = new GameObject(SafeObjectName(
                    source.SourceMetadata.DisplayName));
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                root.AddComponent<MeshRenderer>().sharedMaterial = material;
                if (PrefabUtility.SaveAsPrefabAsset(
                        root,
                        receipt.PrefabPath) == null)
                {
                    throw new InvalidOperationException(
                        "Receipt-owned Prefab could not be rebuilt.");
                }

                UnityEngine.Object.DestroyImmediate(root);
                root = null;
                AssetDatabase.SaveAssets();
                return new FoldCanvasHandoffRebuildResult(
                    receipt,
                    evidence,
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Clear();
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffAssetCreationFailed,
                    "Receipt-owned derived assets could not be rebuilt: " +
                    exception.Message));
                return Failure(diagnostics);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                if (compile?.Mesh != null &&
                    !AssetDatabase.Contains(compile.Mesh))
                {
                    UnityEngine.Object.DestroyImmediate(compile.Mesh);
                }
            }
        }

        private static Mesh SaveOrReplaceMesh(Mesh generated, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                generated.name = "FoldCanvas Generated Mesh";
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Material SaveOrReplaceMaterial(
            Texture2D appearance,
            string path)
        {
            Shader shader = Shader.Find(OneSidedShader);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Required shader '{OneSidedShader}' is unavailable.");
            }

            Material replacement = new Material(shader)
            {
                name = "FoldCanvas One-Sided Appearance",
                mainTexture = appearance,
                color = Color.white,
            };
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(replacement, path);
                return replacement;
            }

            EditorUtility.CopySerialized(replacement, existing);
            UnityEngine.Object.DestroyImmediate(replacement);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static bool ReceiptOwnsExactDestination(
            FoldCanvasHandoffReceipt receipt,
            string destination)
        {
            return string.Equals(
                    receipt.SourceJsonPath,
                    destination + "/source.foldcanvas.json",
                    StringComparison.Ordinal) &&
                string.Equals(
                    receipt.AppearancePath,
                    destination + "/appearance.png",
                    StringComparison.Ordinal) &&
                string.Equals(
                    receipt.SourceAssetPath,
                    destination + "/FoldCanvasSource.asset",
                    StringComparison.Ordinal) &&
                string.Equals(
                    receipt.MeshPath,
                    destination + "/GeneratedMesh.asset",
                    StringComparison.Ordinal) &&
                string.Equals(
                    receipt.MaterialPath,
                    destination + "/Appearance.mat",
                    StringComparison.Ordinal) &&
                string.Equals(
                    receipt.PrefabPath,
                    destination + "/Runtime.prefab",
                    StringComparison.Ordinal) &&
                string.Equals(
                    receipt.ReceiptPath,
                    destination + "/handoff-receipt.json",
                    StringComparison.Ordinal);
        }

        private static bool ByteEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string SafeObjectName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "FoldCanvas Runtime Asset"
                : value.Replace('/', '_').Replace('\\', '_');
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
            if (!TryAbsolute(projectPath, out string path) ||
                !File.Exists(path))
            {
                return false;
            }

            bytes = File.ReadAllBytes(path);
            return true;
        }

        private static bool TryAbsolute(
            string projectPath,
            out string absolutePath)
        {
            absolutePath = null;
            if (string.IsNullOrWhiteSpace(projectPath) ||
                !projectPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }

            string root = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            string prefix = root.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, projectPath));
            if (!path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            absolutePath = path;
            return true;
        }

        private static FoldCanvasHandoffRebuildResult Failure(
            IList<FoldCanvasDiagnostic> diagnostics)
        {
            return new FoldCanvasHandoffRebuildResult(
                null,
                null,
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
