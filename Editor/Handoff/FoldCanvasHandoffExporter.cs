using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasHandoffExporter
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static FoldCanvasHandoffExportResult Export(
            FoldCanvasAsset source,
            FoldCanvasHandoffExportOptions options)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (source == null || options == null ||
                string.IsNullOrWhiteSpace(options.DestinationPath))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff export requires a FoldCanvas source and explicit destination path."));
                return Failure(diagnostics);
            }

            string destination;
            try
            {
                destination = Path.GetFullPath(options.DestinationPath);
            }
            catch (Exception exception)
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff destination path is invalid: " +
                    exception.Message));
                return Failure(diagnostics);
            }

            if (!destination.EndsWith(
                    ".foldcanvas.zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff destination must end with '.foldcanvas.zip'."));
                return Failure(diagnostics);
            }

            if (ContainsUnsupportedOperation(source))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff version 1 supports only operations represented by canonical FoldScript 0.1; native custom or null operations are not portable."));
                return Failure(diagnostics);
            }

            if (!TryReadAppearance(
                    source,
                    out byte[] appearanceBytes,
                    out FoldCanvasHandoffTextureContract textureContract,
                    out int canvasWidth,
                    out int canvasHeight,
                    out FoldCanvasDiagnostic appearanceDiagnostic))
            {
                diagnostics.Add(appearanceDiagnostic);
                return Failure(diagnostics);
            }

            Texture2D detachedTexture = null;
            FoldCanvasAsset detachedAsset = null;
            FoldCanvasCompileResult compileResult = null;
            string temporaryPath = null;
            try
            {
                FoldScriptDocument document =
                    FoldScriptAssetConverter.ToDocument(
                        source,
                        FoldCanvasHandoffFormat.AppearanceEntry);
                FoldScriptWriteResult write =
                    FoldScriptSerializer.Write(document);
                if (!write.Success)
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                        "FoldCanvas source cannot be represented as canonical FoldScript 0.1."));
                    return Failure(diagnostics);
                }

                string canonicalSource = write.Json;
                detachedTexture = DecodePng(
                    appearanceBytes,
                    textureContract.SrgbTexture);
                if (detachedTexture == null ||
                    detachedTexture.width != canvasWidth ||
                    detachedTexture.height != canvasHeight)
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                        "The appearance PNG could not be decoded at the source canvas dimensions."));
                    return Failure(diagnostics);
                }

                FoldScriptImportResult import = FoldScriptImporter.Import(
                    canonicalSource,
                    new FoldScriptImportOptions
                    {
                        SourceProjectPath =
                            "Assets/FoldCanvasHandoff/source.foldcanvas.json",
                        AppearanceResolver =
                            new DetachedAppearanceResolver(detachedTexture),
                        RequireAppearance = true,
                    });
                if (!import.Success)
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                        "Canonical handoff source could not be reimported without project state."));
                    return Failure(diagnostics);
                }

                detachedAsset = import.Asset;
                compileResult = FoldCanvasCompiler.Compile(detachedAsset);
                if (!compileResult.Success ||
                    compileResult.CompiledData == null)
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.HandoffCompileFailed,
                        "Canonical handoff source did not compile successfully; no archive was produced."));
                    return Failure(diagnostics);
                }

                FoldCanvasTextExportResult obj =
                    FoldCanvasObjExporter.Export(
                        compileResult.CompiledData,
                        new FoldCanvasObjExportOptions
                        {
                            ObjectName = document.AssetId,
                            FlipTextureV = false,
                            IncludeMetadataComments = true,
                        });
                if (!obj.Success)
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.HandoffCompileFailed,
                        "Compiled handoff geometry could not be exported as deterministic OBJ evidence."));
                    return Failure(diagnostics);
                }

                FoldCanvasHandoffEvidence evidence =
                    FoldCanvasHandoffEvidenceBuilder.Build(
                        document.AssetId,
                        canonicalSource,
                        appearanceBytes,
                        obj.Content,
                        compileResult);
                string evidenceJson =
                    FoldCanvasHandoffJson.WriteEvidence(evidence);
                string readme = BuildReadme(document);

                Dictionary<string, byte[]> entries =
                    new Dictionary<string, byte[]>(StringComparer.Ordinal)
                    {
                        [FoldCanvasHandoffFormat.SourceEntry] =
                            Utf8.GetBytes(canonicalSource),
                        [FoldCanvasHandoffFormat.AppearanceEntry] =
                            appearanceBytes,
                        [FoldCanvasHandoffFormat.ObjEntry] =
                            Utf8.GetBytes(NormalizeLf(obj.Content)),
                        [FoldCanvasHandoffFormat.EvidenceEntry] =
                            Utf8.GetBytes(evidenceJson),
                        [FoldCanvasHandoffFormat.ReadmeEntry] =
                            Utf8.GetBytes(readme),
                    };
                FoldCanvasHandoffManifest manifest = BuildManifest(
                    document,
                    canvasWidth,
                    canvasHeight,
                    textureContract,
                    entries);
                entries.Add(
                    FoldCanvasHandoffFormat.ManifestEntry,
                    Utf8.GetBytes(
                        FoldCanvasHandoffJson.WriteManifest(manifest)));

                string directory = Path.GetDirectoryName(destination);
                if (string.IsNullOrEmpty(directory))
                {
                    diagnostics.Add(Error(
                        FoldCanvasDiagnosticCodes.HandoffInputMissing,
                        "Handoff destination has no containing directory."));
                    return Failure(diagnostics);
                }

                Directory.CreateDirectory(directory);
                temporaryPath = Path.Combine(
                    directory,
                    "." + Path.GetFileName(destination) + "." +
                    Guid.NewGuid().ToString("N") + ".tmp");
                using (FileStream output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    FoldCanvasHandoffArchive.Write(output, entries);
                    output.Flush(true);
                }

                if (!FoldCanvasHandoffArchive.TryRead(
                        temporaryPath,
                        out FoldCanvasHandoffArchiveContent archive,
                        out FoldCanvasDiagnostic archiveDiagnostic))
                {
                    diagnostics.Add(archiveDiagnostic);
                    return Failure(diagnostics);
                }

                ReplaceDestination(temporaryPath, destination);
                temporaryPath = null;
                return new FoldCanvasHandoffExportResult(
                    destination,
                    archive.ArchiveSha256,
                    manifest,
                    evidence,
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Clear();
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffAssetCreationFailed,
                    "Handoff export failed without replacing the destination: " +
                    exception.Message));
                return Failure(diagnostics);
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryPath) &&
                    File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                if (compileResult?.Mesh != null &&
                    !AssetDatabase.Contains(compileResult.Mesh))
                {
                    UnityEngine.Object.DestroyImmediate(compileResult.Mesh);
                }

                if (detachedAsset != null &&
                    !AssetDatabase.Contains(detachedAsset))
                {
                    UnityEngine.Object.DestroyImmediate(detachedAsset);
                }

                if (detachedTexture != null &&
                    !AssetDatabase.Contains(detachedTexture))
                {
                    UnityEngine.Object.DestroyImmediate(detachedTexture);
                }
            }
        }

        private static FoldCanvasHandoffManifest BuildManifest(
            FoldScriptDocument document,
            int canvasWidth,
            int canvasHeight,
            FoldCanvasHandoffTextureContract textureContract,
            IReadOnlyDictionary<string, byte[]> entries)
        {
            FoldCanvasHandoffManifest manifest =
                new FoldCanvasHandoffManifest
                {
                    PackageVersion = FoldCanvasVersion.Package,
                    CompilerVersion = FoldCanvasVersion.Compiler,
                    FoldScriptVersion = document.SchemaVersion,
                    AssetId = document.AssetId,
                    DisplayName = document.DisplayName,
                    CanvasWidth = canvasWidth,
                    CanvasHeight = canvasHeight,
                    Texture = textureContract,
                };
            AddPayload(
                manifest,
                entries,
                FoldCanvasHandoffFormat.SourceEntry,
                "source");
            AddPayload(
                manifest,
                entries,
                FoldCanvasHandoffFormat.AppearanceEntry,
                "appearance");
            AddPayload(
                manifest,
                entries,
                FoldCanvasHandoffFormat.ObjEntry,
                "derived-obj");
            AddPayload(
                manifest,
                entries,
                FoldCanvasHandoffFormat.EvidenceEntry,
                "compile-evidence");
            AddPayload(
                manifest,
                entries,
                FoldCanvasHandoffFormat.ReadmeEntry,
                "instructions");
            return manifest;
        }

        private static void AddPayload(
            FoldCanvasHandoffManifest manifest,
            IReadOnlyDictionary<string, byte[]> entries,
            string path,
            string role)
        {
            byte[] bytes = entries[path];
            manifest.Payloads.Add(new FoldCanvasHandoffPayload
            {
                Path = path,
                Role = role,
                ByteLength = bytes.LongLength,
                Sha256 = FoldCanvasHandoffHash.Bytes(bytes),
            });
        }

        private static bool TryReadAppearance(
            FoldCanvasAsset source,
            out byte[] bytes,
            out FoldCanvasHandoffTextureContract textureContract,
            out int width,
            out int height,
            out FoldCanvasDiagnostic diagnostic)
        {
            bytes = null;
            textureContract = null;
            width = 0;
            height = 0;
            if (source.Appearance == null)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff version 1 requires one persisted PNG appearance source.");
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(source.Appearance);
            if (string.IsNullOrWhiteSpace(assetPath) ||
                !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                !TryGetAbsoluteAssetPath(assetPath, out string absolutePath) ||
                !File.Exists(absolutePath))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff appearance must be a real PNG under Assets/ or an installed package.");
                return false;
            }

            FileInfo info = new FileInfo(absolutePath);
            if (info.Length <= 0 ||
                info.Length > FoldCanvasHandoffLimits.MaximumAppearanceBytes)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffLimitExceeded,
                    "Handoff appearance PNG exceeds its byte limit.",
                    new FoldCanvasDiagnosticValue(
                        "appearanceBytes",
                        Math.Max(0L, info.Length)),
                    new FoldCanvasDiagnosticValue(
                        "maximumAppearanceBytes",
                        FoldCanvasHandoffLimits.MaximumAppearanceBytes));
                return false;
            }

            bytes = File.ReadAllBytes(absolutePath);
            if (!TryReadPngDimensions(bytes, out width, out height))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Appearance PNG does not contain valid positive IHDR dimensions.");
                return false;
            }

            if (!FoldCanvasHandoffLimits.IsSupportedCanvasSize(width, height))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffLimitExceeded,
                    "Appearance PNG decoded pixel count exceeds the handoff safety limit.",
                    new FoldCanvasDiagnosticValue(
                        "canvasPixels",
                        (long)width * height),
                    new FoldCanvasDiagnosticValue(
                        "maximumCanvasPixels",
                        FoldCanvasHandoffLimits.MaximumDecodedCanvasPixels));
                return false;
            }

            if (
                width != source.Appearance.width ||
                height != source.Appearance.height ||
                width != source.SourceMetadata.CanvasPixelWidth ||
                height != source.SourceMetadata.CanvasPixelHeight)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Appearance PNG, imported texture, and source canvas dimensions must agree exactly.");
                return false;
            }

            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            float pixelsPerUnit = importer != null
                ? importer.spritePixelsPerUnit
                : 100f;
            int integralPixelsPerUnit = Mathf.RoundToInt(pixelsPerUnit);
            if (!Mathf.Approximately(
                    pixelsPerUnit,
                    integralPixelsPerUnit) ||
                integralPixelsPerUnit <= 0)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff version 1 requires a positive integral texture pixels-per-unit setting.");
                return false;
            }

            textureContract = new FoldCanvasHandoffTextureContract
            {
                FilterMode = (importer != null
                    ? importer.filterMode
                    : source.Appearance.filterMode).ToString(),
                WrapModeU = (importer != null
                    ? importer.wrapModeU
                    : source.Appearance.wrapModeU).ToString(),
                WrapModeV = (importer != null
                    ? importer.wrapModeV
                    : source.Appearance.wrapModeV).ToString(),
                MipmapEnabled = importer != null && importer.mipmapEnabled,
                SrgbTexture = importer == null || importer.sRGBTexture,
                AlphaIsTransparency =
                    importer != null && importer.alphaIsTransparency,
                AnisoLevel = importer != null
                    ? importer.anisoLevel
                    : source.Appearance.anisoLevel,
                PixelsPerUnit = integralPixelsPerUnit,
            };
            diagnostic = null;
            return true;
        }

        private static bool ContainsUnsupportedOperation(
            FoldCanvasAsset source)
        {
            for (int i = 0; i < source.Operations.Count; i++)
            {
                FoldOperationDefinition operation = source.Operations[i];
                if (operation == null ||
                    operation.Type == FoldOperationType.Custom)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetAbsoluteAssetPath(
            string assetPath,
            out string absolutePath)
        {
            absolutePath = null;
            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                string projectRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
                absolutePath = Path.GetFullPath(
                    Path.Combine(projectRoot, assetPath));
                return true;
            }

            if (!assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                return false;
            }

            PackageInfo package = PackageInfo.FindForAssetPath(assetPath);
            if (package == null ||
                string.IsNullOrWhiteSpace(package.assetPath) ||
                string.IsNullOrWhiteSpace(package.resolvedPath) ||
                !assetPath.StartsWith(
                    package.assetPath + "/",
                    StringComparison.Ordinal))
            {
                return false;
            }

            string relative = assetPath.Substring(package.assetPath.Length + 1);
            absolutePath = Path.GetFullPath(
                Path.Combine(package.resolvedPath, relative));
            return true;
        }

        private static bool TryReadPngDimensions(
            byte[] bytes,
            out int width,
            out int height)
        {
            width = 0;
            height = 0;
            byte[] signature =
            {
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A,
            };
            if (bytes == null || bytes.Length < 24)
            {
                return false;
            }

            for (int i = 0; i < signature.Length; i++)
            {
                if (bytes[i] != signature[i])
                {
                    return false;
                }
            }

            if (bytes[12] != (byte)'I' ||
                bytes[13] != (byte)'H' ||
                bytes[14] != (byte)'D' ||
                bytes[15] != (byte)'R')
            {
                return false;
            }

            uint rawWidth = BigEndianUInt32(bytes, 16);
            uint rawHeight = BigEndianUInt32(bytes, 20);
            if (rawWidth == 0 || rawWidth > int.MaxValue ||
                rawHeight == 0 || rawHeight > int.MaxValue)
            {
                return false;
            }

            width = (int)rawWidth;
            height = (int)rawHeight;
            return true;
        }

        private static uint BigEndianUInt32(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] << 24 |
                bytes[offset + 1] << 16 |
                bytes[offset + 2] << 8 |
                bytes[offset + 3]);
        }

        private static Texture2D DecodePng(byte[] bytes, bool srgb)
        {
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false,
                !srgb);
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            return texture;
        }

        private static string BuildReadme(FoldScriptDocument document)
        {
            return NormalizeLf(
                "# FoldCanvas production handoff\n\n" +
                $"Asset: {document.AssetId}\n\n" +
                "Authoritative source:\n\n" +
                "- `source.foldcanvas.json`\n" +
                "- `appearance.png`\n\n" +
                "`derived/model.obj`, the compile report, imported Mesh, " +
                "Material, Prefab, and receipt are derived evidence or " +
                "runtime outputs. Do not edit them as geometry source.\n\n" +
                $"Import with FoldCanvas {FoldCanvasVersion.Package} and " +
                $"FoldScript {FoldCanvasSourceMetadata.CurrentSchemaVersion}. " +
                "After import, use Rebuild from the received FoldCanvas " +
                "source asset to regenerate the derived outputs.\n");
        }

        private static string NormalizeLf(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
        }

        private static void ReplaceDestination(
            string temporaryPath,
            string destination)
        {
            if (File.Exists(destination))
            {
                File.Replace(temporaryPath, destination, null);
            }
            else
            {
                File.Move(temporaryPath, destination);
            }
        }

        private static FoldCanvasHandoffExportResult Failure(
            IList<FoldCanvasDiagnostic> diagnostics)
        {
            return new FoldCanvasHandoffExportResult(
                null,
                null,
                null,
                null,
                diagnostics);
        }

        private static FoldCanvasDiagnostic Error(
            string code,
            string message,
            params FoldCanvasDiagnosticValue[] values)
        {
            return new FoldCanvasDiagnostic(
                code,
                FoldCanvasDiagnosticSeverity.Error,
                message,
                values: values);
        }

        private sealed class DetachedAppearanceResolver :
            IFoldScriptAppearanceResolver
        {
            private readonly Texture2D texture;

            public DetachedAppearanceResolver(Texture2D sourceTexture)
            {
                texture = sourceTexture;
            }

            public bool TryResolve(
                string projectRelativePath,
                out Texture2D appearance)
            {
                bool matches = string.Equals(
                    projectRelativePath,
                    "Assets/FoldCanvasHandoff/appearance.png",
                    StringComparison.Ordinal);
                appearance = matches ? texture : null;
                return matches;
            }
        }
    }
}
