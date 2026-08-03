using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal sealed class FoldCanvasVerifiedHandoff : IDisposable
    {
        public FoldCanvasHandoffArchiveContent Archive { get; set; }

        public FoldCanvasHandoffManifest Manifest { get; set; }

        public FoldCanvasHandoffEvidence Evidence { get; set; }

        public string CanonicalSource { get; set; }

        public string ObjText { get; set; }

        public Texture2D Appearance { get; set; }

        public FoldCanvasAsset SourceAsset { get; set; }

        public FoldCanvasCompileResult CompileResult { get; set; }

        public void Dispose()
        {
            if (CompileResult?.Mesh != null &&
                !AssetDatabase.Contains(CompileResult.Mesh))
            {
                UnityEngine.Object.DestroyImmediate(CompileResult.Mesh);
            }

            if (SourceAsset != null && !AssetDatabase.Contains(SourceAsset))
            {
                UnityEngine.Object.DestroyImmediate(SourceAsset);
            }

            if (Appearance != null && !AssetDatabase.Contains(Appearance))
            {
                UnityEngine.Object.DestroyImmediate(Appearance);
            }

            CompileResult = null;
            SourceAsset = null;
            Appearance = null;
        }
    }

    public static class FoldCanvasHandoffImporter
    {
        private static readonly UTF8Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        public static FoldCanvasHandoffImportResult Import(
            FoldCanvasHandoffImportOptions options)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (options == null ||
                string.IsNullOrWhiteSpace(options.ArchivePath) ||
                string.IsNullOrWhiteSpace(options.DestinationFolder))
            {
                diagnostics.Add(Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff import requires an archive and explicit Assets destination folder."));
                return Failure(diagnostics);
            }

            if (!TryNormalizeDestination(
                    options.DestinationFolder,
                    out string destination,
                    out FoldCanvasDiagnostic destinationDiagnostic))
            {
                diagnostics.Add(destinationDiagnostic);
                return Failure(diagnostics);
            }

            if (!TryVerifyArchive(
                    options.ArchivePath,
                    out FoldCanvasVerifiedHandoff verified,
                    out FoldCanvasDiagnostic verificationDiagnostic))
            {
                diagnostics.Add(verificationDiagnostic);
                return Failure(diagnostics);
            }

            using (verified)
            {
                return FoldCanvasHandoffPersistence.Persist(
                    verified,
                    destination);
            }
        }

        internal static bool TryVerifyArchive(
            string archivePath,
            out FoldCanvasVerifiedHandoff verified,
            out FoldCanvasDiagnostic diagnostic)
        {
            verified = null;
            if (!FoldCanvasHandoffArchive.TryRead(
                    archivePath,
                    out FoldCanvasHandoffArchiveContent archive,
                    out diagnostic))
            {
                return false;
            }

            if (!TryDecodeText(
                    archive.Entries[FoldCanvasHandoffFormat.ManifestEntry],
                    out string manifestJson) ||
                !FoldCanvasHandoffJson.TryReadManifest(
                    manifestJson,
                    out FoldCanvasHandoffManifest manifest,
                    out diagnostic))
            {
                diagnostic = diagnostic ?? Error(
                    FoldCanvasDiagnosticCodes.InvalidHandoffManifest,
                    "Handoff manifest is not valid UTF-8 JSON.");
                return false;
            }

            if (!string.Equals(
                    manifest.Format,
                    FoldCanvasHandoffFormat.Format,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    manifest.Version,
                    FoldCanvasHandoffFormat.Version,
                    StringComparison.Ordinal))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffVersion,
                    "Handoff format or archive version is unsupported.");
                return false;
            }

            if (!string.Equals(
                    manifest.PackageVersion,
                    FoldCanvasVersion.Package,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    manifest.CompilerVersion,
                    FoldCanvasVersion.Compiler,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    manifest.FoldScriptVersion,
                    FoldCanvasSourceMetadata.CurrentSchemaVersion,
                    StringComparison.Ordinal))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffCompatibilityMismatch,
                    "Handoff version 1 requires exact package, compiler, and FoldScript versions.");
                return false;
            }

            if (!FoldCanvasHandoffLimits.IsSupportedCanvasSize(
                    manifest.CanvasWidth,
                    manifest.CanvasHeight))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffLimitExceeded,
                    "Handoff canvas decoded pixel count exceeds the supported safety limit.",
                    new FoldCanvasDiagnosticValue(
                        "canvasPixels",
                        (long)manifest.CanvasWidth * manifest.CanvasHeight),
                    new FoldCanvasDiagnosticValue(
                        "maximumCanvasPixels",
                        FoldCanvasHandoffLimits.MaximumDecodedCanvasPixels));
                return false;
            }

            for (int i = 0; i < manifest.Payloads.Count; i++)
            {
                FoldCanvasHandoffPayload payload = manifest.Payloads[i];
                byte[] bytes = archive.Entries[payload.Path];
                string hash = FoldCanvasHandoffHash.Bytes(bytes);
                if (bytes.LongLength != payload.ByteLength ||
                    !string.Equals(
                        hash,
                        payload.Sha256,
                        StringComparison.Ordinal))
                {
                    diagnostic = Error(
                        FoldCanvasDiagnosticCodes.HandoffIntegrityMismatch,
                        $"Handoff payload '{payload.Path}' does not match its manifest length or SHA-256.",
                        new FoldCanvasDiagnosticValue(
                            "actualByteLength",
                            bytes.LongLength),
                        new FoldCanvasDiagnosticValue(
                            "expectedByteLength",
                            payload.ByteLength));
                    return false;
                }
            }

            if (!TryDecodeText(
                    archive.Entries[FoldCanvasHandoffFormat.SourceEntry],
                    out string sourceJson))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff FoldScript source is not valid UTF-8.");
                return false;
            }

            FoldScriptWriteResult canonical =
                FoldScriptSerializer.Canonicalize(sourceJson);
            if (!canonical.Success ||
                !string.Equals(
                    canonical.Json,
                    sourceJson,
                    StringComparison.Ordinal))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff FoldScript source must be canonical version 0.1 JSON.");
                return false;
            }

            FoldScriptReadResult read = FoldScriptSerializer.Read(sourceJson);
            if (!read.Success ||
                !string.Equals(
                    read.Document.Canvas.Appearance,
                    FoldCanvasHandoffFormat.AppearanceEntry,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    read.Document.AssetId,
                    manifest.AssetId,
                    StringComparison.Ordinal) ||
                read.Document.Canvas.Width != manifest.CanvasWidth ||
                read.Document.Canvas.Height != manifest.CanvasHeight)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                    "Handoff source identity, appearance reference, or canvas dimensions do not match the manifest.");
                return false;
            }

            if (!TryDecodeText(
                    archive.Entries[FoldCanvasHandoffFormat.EvidenceEntry],
                    out string evidenceJson) ||
                !FoldCanvasHandoffJson.TryReadEvidence(
                    evidenceJson,
                    out FoldCanvasHandoffEvidence evidence,
                    out diagnostic))
            {
                diagnostic = diagnostic ?? Error(
                    FoldCanvasDiagnosticCodes.InvalidHandoffManifest,
                    "Handoff compile evidence is not valid UTF-8 JSON.");
                return false;
            }

            if (!EvidenceHeaderMatches(manifest, evidence))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch,
                    "Handoff evidence identity or compiler versions do not match the manifest.");
                return false;
            }

            byte[] appearanceBytes =
                archive.Entries[FoldCanvasHandoffFormat.AppearanceEntry];
            Texture2D appearance = DecodePng(
                appearanceBytes,
                manifest.Texture.SrgbTexture);
            if (appearance == null ||
                appearance.width != manifest.CanvasWidth ||
                appearance.height != manifest.CanvasHeight)
            {
                if (appearance != null)
                {
                    UnityEngine.Object.DestroyImmediate(appearance);
                }

                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffIntegrityMismatch,
                    "Handoff appearance PNG does not decode at the manifest dimensions.");
                return false;
            }

            FoldCanvasVerifiedHandoff result =
                new FoldCanvasVerifiedHandoff
                {
                    Archive = archive,
                    Manifest = manifest,
                    Evidence = evidence,
                    CanonicalSource = sourceJson,
                    Appearance = appearance,
                };
            try
            {
                FoldScriptImportResult import = FoldScriptImporter.Import(
                    sourceJson,
                    new FoldScriptImportOptions
                    {
                        SourceProjectPath =
                            "Assets/FoldCanvasHandoff/source.foldcanvas.json",
                        AppearanceResolver =
                            new DetachedAppearanceResolver(appearance),
                        RequireAppearance = true,
                    });
                if (!import.Success)
                {
                    diagnostic = Error(
                        FoldCanvasDiagnosticCodes.UnsupportedHandoffSource,
                        "Handoff source could not be converted into a detached FoldCanvas asset.");
                    result.Dispose();
                    return false;
                }

                result.SourceAsset = import.Asset;
                result.CompileResult =
                    FoldCanvasCompiler.Compile(result.SourceAsset);
                if (!result.CompileResult.Success ||
                    result.CompileResult.CompiledData == null)
                {
                    diagnostic = Error(
                        FoldCanvasDiagnosticCodes.HandoffCompileFailed,
                        "Handoff source did not compile successfully in the receiving compiler.");
                    result.Dispose();
                    return false;
                }

                FoldCanvasTextExportResult obj =
                    FoldCanvasObjExporter.Export(
                        result.CompileResult.CompiledData,
                        new FoldCanvasObjExportOptions
                        {
                            ObjectName = manifest.AssetId,
                            FlipTextureV = false,
                            IncludeMetadataComments = true,
                        });
                if (!obj.Success)
                {
                    diagnostic = Error(
                        FoldCanvasDiagnosticCodes.HandoffCompileFailed,
                        "Receiver could not regenerate deterministic OBJ evidence.");
                    result.Dispose();
                    return false;
                }

                if (!TryDecodeText(
                        archive.Entries[FoldCanvasHandoffFormat.ObjEntry],
                        out string archivedObj) ||
                    !string.Equals(
                        archivedObj,
                        obj.Content,
                        StringComparison.Ordinal))
                {
                    diagnostic = Error(
                        FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch,
                        "Receiver OBJ regeneration does not match the archived derived evidence.");
                    result.Dispose();
                    return false;
                }

                FoldCanvasHandoffEvidence regenerated =
                    FoldCanvasHandoffEvidenceBuilder.Build(
                        manifest.AssetId,
                        sourceJson,
                        appearanceBytes,
                        obj.Content,
                        result.CompileResult);
                if (!string.Equals(
                        FoldCanvasHandoffJson.WriteEvidence(regenerated),
                        evidenceJson,
                        StringComparison.Ordinal))
                {
                    diagnostic = Error(
                        FoldCanvasDiagnosticCodes.HandoffEvidenceMismatch,
                        "Receiver compilation does not match the producer's complete evidence.");
                    result.Dispose();
                    return false;
                }

                result.ObjText = obj.Content;
                verified = result;
                diagnostic = null;
                return true;
            }
            catch (Exception exception)
            {
                result.Dispose();
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffCompileFailed,
                    "Handoff detached verification failed: " +
                    exception.Message);
                return false;
            }
        }

        internal static bool TryNormalizeDestination(
            string value,
            out string destination,
            out FoldCanvasDiagnostic diagnostic)
        {
            destination = null;
            diagnostic = null;
            string candidate = value?.TrimEnd('/', '\\');
            if (string.IsNullOrWhiteSpace(candidate) ||
                !candidate.StartsWith("Assets/", StringComparison.Ordinal) ||
                !FoldScriptProjectPath.TryNormalizeProjectPath(
                    candidate,
                    out string normalized,
                    out _) ||
                string.Equals(normalized, "Assets", StringComparison.Ordinal))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff destination must be one explicit project-relative folder below Assets/." );
                return false;
            }

            string parent = normalized.Substring(
                0,
                normalized.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(parent))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffDestinationOccupied,
                    "Handoff destination parent must already exist so import owns exactly one new folder.");
                return false;
            }

            destination = normalized;
            return true;
        }

        private static bool EvidenceHeaderMatches(
            FoldCanvasHandoffManifest manifest,
            FoldCanvasHandoffEvidence evidence)
        {
            return string.Equals(
                    evidence.AssetId,
                    manifest.AssetId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    evidence.PackageVersion,
                    manifest.PackageVersion,
                    StringComparison.Ordinal) &&
                string.Equals(
                    evidence.CompilerVersion,
                    manifest.CompilerVersion,
                    StringComparison.Ordinal) &&
                string.Equals(
                    evidence.FoldScriptVersion,
                    manifest.FoldScriptVersion,
                    StringComparison.Ordinal);
        }

        private static bool TryDecodeText(byte[] bytes, out string text)
        {
            try
            {
                text = StrictUtf8.GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                text = null;
                return false;
            }
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
