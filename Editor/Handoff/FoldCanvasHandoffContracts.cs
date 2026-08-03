using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasHandoffFormat
    {
        public const string Format = "com.foldcanvas.handoff";

        public const string Version = "1";

        public const string EvidenceFormat =
            "com.foldcanvas.handoff.compile-report";

        public const string ReceiptFormat =
            "com.foldcanvas.handoff.receipt";

        public const string ManifestEntry = "manifest.json";

        public const string SourceEntry = "source.foldcanvas.json";

        public const string AppearanceEntry = "appearance.png";

        public const string ObjEntry = "derived/model.obj";

        public const string EvidenceEntry =
            "evidence/compile-report.json";

        public const string ReadmeEntry = "README.md";

        private static readonly ReadOnlyCollection<string> orderedEntries =
            Array.AsReadOnly(new[]
            {
                ManifestEntry,
                SourceEntry,
                AppearanceEntry,
                ObjEntry,
                EvidenceEntry,
                ReadmeEntry,
            });

        public static IReadOnlyList<string> OrderedEntries => orderedEntries;
    }

    public static class FoldCanvasHandoffLimits
    {
        public const int EntryCount = 6;

        public const long MaximumArchiveBytes = 256L * 1024L * 1024L;

        public const long MaximumManifestBytes = 1024L * 1024L;

        public const long MaximumSourceBytes = 16L * 1024L * 1024L;

        public const long MaximumAppearanceBytes = 64L * 1024L * 1024L;

        public const long MaximumDecodedCanvasPixels = 64L * 1024L * 1024L;

        public const long MaximumObjBytes = 128L * 1024L * 1024L;

        public const long MaximumEvidenceBytes = 4L * 1024L * 1024L;

        public const long MaximumReadmeBytes = 1024L * 1024L;

        public const long MaximumExpandedBytes =
            MaximumManifestBytes +
            MaximumSourceBytes +
            MaximumAppearanceBytes +
            MaximumObjBytes +
            MaximumEvidenceBytes +
            MaximumReadmeBytes;

        internal static long MaximumBytesForEntry(string entryName)
        {
            switch (entryName)
            {
                case FoldCanvasHandoffFormat.ManifestEntry:
                    return MaximumManifestBytes;
                case FoldCanvasHandoffFormat.SourceEntry:
                    return MaximumSourceBytes;
                case FoldCanvasHandoffFormat.AppearanceEntry:
                    return MaximumAppearanceBytes;
                case FoldCanvasHandoffFormat.ObjEntry:
                    return MaximumObjBytes;
                case FoldCanvasHandoffFormat.EvidenceEntry:
                    return MaximumEvidenceBytes;
                case FoldCanvasHandoffFormat.ReadmeEntry:
                    return MaximumReadmeBytes;
                default:
                    return 0L;
            }
        }

        internal static bool IsSupportedCanvasSize(int width, int height)
        {
            return width > 0 &&
                height > 0 &&
                (long)width * height <= MaximumDecodedCanvasPixels;
        }
    }

    public sealed class FoldCanvasHandoffTextureContract
    {
        public string FilterMode { get; set; } = "Bilinear";

        public string WrapModeU { get; set; } = "Clamp";

        public string WrapModeV { get; set; } = "Clamp";

        public bool MipmapEnabled { get; set; }

        public bool SrgbTexture { get; set; } = true;

        public bool AlphaIsTransparency { get; set; }

        public int AnisoLevel { get; set; } = 1;

        public int PixelsPerUnit { get; set; } = 100;
    }

    public sealed class FoldCanvasHandoffPayload
    {
        public string Path { get; set; }

        public string Role { get; set; }

        public long ByteLength { get; set; }

        public string Sha256 { get; set; }
    }

    public sealed class FoldCanvasHandoffManifest
    {
        public string Format { get; set; } = FoldCanvasHandoffFormat.Format;

        public string Version { get; set; } = FoldCanvasHandoffFormat.Version;

        public string PackageVersion { get; set; }

        public string CompilerVersion { get; set; }

        public string FoldScriptVersion { get; set; }

        public string AssetId { get; set; }

        public string DisplayName { get; set; }

        public int CanvasWidth { get; set; }

        public int CanvasHeight { get; set; }

        public FoldCanvasHandoffTextureContract Texture { get; set; } =
            new FoldCanvasHandoffTextureContract();

        public List<FoldCanvasHandoffPayload> Payloads { get; } =
            new List<FoldCanvasHandoffPayload>();
    }

    public sealed class FoldCanvasHandoffEvidence
    {
        public string Format { get; set; } =
            FoldCanvasHandoffFormat.EvidenceFormat;

        public string Version { get; set; } = FoldCanvasHandoffFormat.Version;

        public string AssetId { get; set; }

        public string PackageVersion { get; set; }

        public string CompilerVersion { get; set; }

        public string FoldScriptVersion { get; set; }

        public string SourceSha256 { get; set; }

        public string AppearanceSha256 { get; set; }

        public string GeometrySha256 { get; set; }

        public string ObjSha256 { get; set; }

        public string DiagnosticSha256 { get; set; }

        public string ValidationSha256 { get; set; }

        public string ClosedVolumeSha256 { get; set; }

        public string SphereReportsSha256 { get; set; }

        public string ValidationLevel { get; set; }

        public int RenderVertexCount { get; set; }

        public int TopologyVertexCount { get; set; }

        public int TriangleCount { get; set; }

        public int DiagnosticCount { get; set; }

        public int ValidationErrorCount { get; set; }

        public int ValidationWarningCount { get; set; }

        public int OpenEdgeCount { get; set; }

        public int NonManifoldEdgeCount { get; set; }

        public int ComponentCount { get; set; }

        public bool ValidationIsValid { get; set; }

        public bool IsClosedVolume { get; set; }

        public bool IsSingleClosedVolume { get; set; }

        public int SphereReportCount { get; set; }
    }

    public sealed class FoldCanvasHandoffReceipt
    {
        public string Format { get; set; } =
            FoldCanvasHandoffFormat.ReceiptFormat;

        public string Version { get; set; } = FoldCanvasHandoffFormat.Version;

        public string ArchiveSha256 { get; set; }

        public string AssetId { get; set; }

        public string PackageVersion { get; set; }

        public string CompilerVersion { get; set; }

        public string SourceSha256 { get; set; }

        public string AppearanceSha256 { get; set; }

        public string GeometrySha256 { get; set; }

        public string ObjSha256 { get; set; }

        public string SourceJsonPath { get; set; }

        public string AppearancePath { get; set; }

        public string SourceAssetPath { get; set; }

        public string MeshPath { get; set; }

        public string MaterialPath { get; set; }

        public string PrefabPath { get; set; }

        public string ReceiptPath { get; set; }
    }

    public sealed class FoldCanvasHandoffExportOptions
    {
        public string DestinationPath { get; set; }
    }

    public sealed class FoldCanvasHandoffImportOptions
    {
        public string ArchivePath { get; set; }

        public string DestinationFolder { get; set; }
    }

    public sealed class FoldCanvasHandoffExportResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldCanvasHandoffExportResult(
            string archivePath,
            string archiveSha256,
            FoldCanvasHandoffManifest manifest,
            FoldCanvasHandoffEvidence evidence,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            ArchivePath = archivePath;
            ArchiveSha256 = archiveSha256;
            Manifest = manifest;
            Evidence = evidence;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public bool Success =>
            ArchivePath != null && diagnostics.Count == 0;

        public string ArchivePath { get; }

        public string ArchiveSha256 { get; }

        public FoldCanvasHandoffManifest Manifest { get; }

        public FoldCanvasHandoffEvidence Evidence { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;
    }

    public sealed class FoldCanvasHandoffImportResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldCanvasHandoffImportResult(
            string destinationFolder,
            string archiveSha256,
            FoldCanvasHandoffManifest manifest,
            FoldCanvasHandoffEvidence evidence,
            FoldCanvasHandoffReceipt receipt,
            bool wasAlreadyImported,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            DestinationFolder = destinationFolder;
            ArchiveSha256 = archiveSha256;
            Manifest = manifest;
            Evidence = evidence;
            Receipt = receipt;
            WasAlreadyImported = wasAlreadyImported;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public bool Success => Receipt != null && diagnostics.Count == 0;

        public string DestinationFolder { get; }

        public string ArchiveSha256 { get; }

        public FoldCanvasHandoffManifest Manifest { get; }

        public FoldCanvasHandoffEvidence Evidence { get; }

        public FoldCanvasHandoffReceipt Receipt { get; }

        public bool WasAlreadyImported { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;
    }

    public sealed class FoldCanvasHandoffRebuildResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldCanvasHandoffRebuildResult(
            FoldCanvasHandoffReceipt receipt,
            FoldCanvasHandoffEvidence evidence,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            Receipt = receipt;
            Evidence = evidence;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public bool Success => Receipt != null && diagnostics.Count == 0;

        public FoldCanvasHandoffReceipt Receipt { get; }

        public FoldCanvasHandoffEvidence Evidence { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;
    }
}
