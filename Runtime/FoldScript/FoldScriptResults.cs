using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoldCanvas
{
    public sealed class FoldScriptReadResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldScriptReadResult(
            FoldScriptDocument document,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            Document = document;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public FoldScriptDocument Document { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;

        public bool Success => Document != null && diagnostics.Count == 0;
    }

    public sealed class FoldScriptWriteResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldScriptWriteResult(
            string json,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            Json = json;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public string Json { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;

        public bool Success => Json != null && diagnostics.Count == 0;
    }

    public sealed class FoldScriptImportResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldScriptImportResult(
            FoldScriptDocument document,
            FoldCanvasAsset asset,
            string canonicalJson,
            string resolvedAppearancePath,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            Document = document;
            Asset = asset;
            CanonicalJson = canonicalJson;
            ResolvedAppearancePath = resolvedAppearancePath;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public FoldScriptDocument Document { get; }

        public FoldCanvasAsset Asset { get; }

        public string CanonicalJson { get; }

        public string ResolvedAppearancePath { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;

        public bool Success => Asset != null && diagnostics.Count == 0;
    }
}
