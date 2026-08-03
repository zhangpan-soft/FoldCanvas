using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FoldCanvas
{
    public interface IFoldCanvasSourceProposer
    {
        FoldCanvasSourceProposal Propose(
            FoldCanvasSourceProposalRequest request);
    }

    public interface IFoldCanvasSourceRepairer
    {
        FoldCanvasRepairResponse Repair(FoldCanvasRepairRequest request);
    }

    public sealed class FoldCanvasSourceProposalRequest
    {
        public FoldCanvasSourceProposalRequest(
            string intent,
            string schemaVersion =
                FoldCanvasSourceMetadata.CurrentSchemaVersion)
        {
            Intent = intent ?? throw new ArgumentNullException(nameof(intent));
            SchemaVersion = schemaVersion ??
                throw new ArgumentNullException(nameof(schemaVersion));
        }

        public string Intent { get; }

        public string SchemaVersion { get; }
    }

    public sealed class FoldCanvasSourceProposal
    {
        public FoldCanvasSourceProposal(string foldScriptJson)
        {
            FoldScriptJson = foldScriptJson ??
                throw new ArgumentNullException(nameof(foldScriptJson));
        }

        public string FoldScriptJson { get; }
    }

    public sealed class FoldCanvasRepairResponse
    {
        public FoldCanvasRepairResponse(string foldScriptJson)
        {
            FoldScriptJson = foldScriptJson ??
                throw new ArgumentNullException(nameof(foldScriptJson));
        }

        public string FoldScriptJson { get; }
    }

    public sealed class FoldCanvasRepairDiagnostic
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnosticValue> values;
        private readonly ReadOnlyCollection<string> suggestions;

        internal FoldCanvasRepairDiagnostic(FoldCanvasDiagnostic diagnostic)
        {
            Code = diagnostic.Code;
            Severity = diagnostic.Severity;
            Message = diagnostic.Message;
            PanelId = diagnostic.PanelId;
            OperationId = diagnostic.OperationId;
            SeamId = diagnostic.SeamId;
            BoundaryId = diagnostic.BoundaryId;
            GeometryContext = diagnostic.GeometryContext;
            values = new List<FoldCanvasDiagnosticValue>(diagnostic.Values)
                .AsReadOnly();
            suggestions = new List<string>(diagnostic.RepairSuggestions)
                .AsReadOnly();
        }

        public string Code { get; }

        public FoldCanvasDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public string PanelId { get; }

        public string OperationId { get; }

        public string SeamId { get; }

        public string BoundaryId { get; }

        public FoldCanvasDiagnosticGeometryContext GeometryContext { get; }

        public IReadOnlyList<FoldCanvasDiagnosticValue> Values => values;

        public IReadOnlyList<string> Suggestions => suggestions;
    }

    public sealed class FoldCanvasRepairRequest
    {
        private readonly ReadOnlyCollection<FoldCanvasRepairDiagnostic>
            diagnostics;

        internal FoldCanvasRepairRequest(
            string schemaVersion,
            string compilerVersion,
            string assetId,
            string canonicalSourceJson,
            IList<FoldCanvasRepairDiagnostic> sourceDiagnostics)
        {
            SchemaVersion = schemaVersion;
            CompilerVersion = compilerVersion;
            AssetId = assetId;
            CanonicalSourceJson = canonicalSourceJson;
            diagnostics = new List<FoldCanvasRepairDiagnostic>(
                sourceDiagnostics).AsReadOnly();
        }

        public string SchemaVersion { get; }

        public string CompilerVersion { get; }

        public string AssetId { get; }

        public string CanonicalSourceJson { get; }

        public IReadOnlyList<FoldCanvasRepairDiagnostic> Diagnostics =>
            diagnostics;

        public string ToCanonicalJson()
        {
            return FoldCanvasRepairRequestWriter.Write(this);
        }
    }

    public static class FoldCanvasRepairRequestBuilder
    {
        public static FoldCanvasRepairRequest Create(
            FoldScriptDocument document,
            FoldCanvasCompileResult compileResult,
            string canonicalSourceJson = null,
            string compilerVersion = FoldCanvasVersion.Compiler)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (compileResult == null)
            {
                throw new ArgumentNullException(nameof(compileResult));
            }

            FoldScriptWriteResult write =
                FoldScriptSerializer.Write(document);
            if (!write.Success)
            {
                throw new ArgumentException(
                    "Cannot create a repair request from invalid FoldScript.",
                    nameof(document));
            }

            if (canonicalSourceJson != null &&
                !string.Equals(
                    canonicalSourceJson,
                    write.Json,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A supplied repair source must be the canonical JSON for the supplied document.",
                    nameof(canonicalSourceJson));
            }

            canonicalSourceJson = write.Json;

            List<FoldCanvasRepairDiagnostic> diagnostics =
                new List<FoldCanvasRepairDiagnostic>(
                    compileResult.Diagnostics.Count);
            for (int i = 0; i < compileResult.Diagnostics.Count; i++)
            {
                diagnostics.Add(new FoldCanvasRepairDiagnostic(
                    compileResult.Diagnostics[i]));
            }

            return new FoldCanvasRepairRequest(
                document.SchemaVersion,
                compilerVersion ?? throw new ArgumentNullException(
                    nameof(compilerVersion)),
                document.AssetId,
                canonicalSourceJson,
                diagnostics);
        }
    }

    public sealed class FoldCanvasRepairAttemptResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldCanvasRepairAttemptResult(
            FoldScriptImportResult importResult,
            FoldCanvasCompileResult compileResult,
            IList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            ImportResult = importResult;
            CompileResult = compileResult;
            diagnostics = new List<FoldCanvasDiagnostic>(sourceDiagnostics)
                .AsReadOnly();
        }

        public FoldScriptImportResult ImportResult { get; }

        public FoldCanvasCompileResult CompileResult { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;

        public bool Success =>
            ImportResult != null &&
            ImportResult.Success &&
            CompileResult != null &&
            CompileResult.Success &&
            diagnostics.Count == 0;
    }

    public static class FoldCanvasRepairCoordinator
    {
        public static FoldCanvasRepairAttemptResult Apply(
            FoldCanvasRepairResponse response,
            FoldScriptImportOptions options = null)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (response == null ||
                string.IsNullOrWhiteSpace(response.FoldScriptJson))
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidRepairResponse,
                    FoldCanvasDiagnosticSeverity.Error,
                    "A repair response must contain replacement FoldScript JSON."));
                return new FoldCanvasRepairAttemptResult(
                    null,
                    null,
                    diagnostics);
            }

            FoldScriptImportResult import =
                FoldScriptImporter.Import(response.FoldScriptJson, options);
            if (!import.Success)
            {
                diagnostics.AddRange(import.Diagnostics);
                return new FoldCanvasRepairAttemptResult(
                    import,
                    null,
                    diagnostics);
            }

            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(import.Asset);
            if (!compile.Success)
            {
                diagnostics.AddRange(compile.Diagnostics);
            }

            return new FoldCanvasRepairAttemptResult(
                import,
                compile,
                diagnostics);
        }
    }

    internal static class FoldCanvasRepairRequestWriter
    {
        public static string Write(FoldCanvasRepairRequest request)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{\n");
            StringProperty(builder, 1, "schemaVersion", request.SchemaVersion, true);
            StringProperty(builder, 1, "compilerVersion", request.CompilerVersion, true);
            StringProperty(builder, 1, "assetId", request.AssetId, true);
            StringProperty(builder, 1, "source", request.CanonicalSourceJson, true);
            Indent(builder, 1);
            builder.Append("\"diagnostics\": [");
            if (request.Diagnostics.Count == 0)
            {
                builder.Append("]\n");
            }
            else
            {
                builder.Append('\n');
                for (int i = 0; i < request.Diagnostics.Count; i++)
                {
                    WriteDiagnostic(
                        builder,
                        request.Diagnostics[i],
                        i + 1 < request.Diagnostics.Count);
                }

                Indent(builder, 1);
                builder.Append("]\n");
            }

            builder.Append("}\n");
            return builder.ToString();
        }

        private static void WriteDiagnostic(
            StringBuilder builder,
            FoldCanvasRepairDiagnostic diagnostic,
            bool comma)
        {
            Indent(builder, 2);
            builder.Append("{\n");
            StringProperty(builder, 3, "code", diagnostic.Code, true);
            StringProperty(
                builder,
                3,
                "severity",
                diagnostic.Severity.ToString().ToLowerInvariant(),
                true);
            StringProperty(builder, 3, "message", diagnostic.Message, true);
            Indent(builder, 3);
            builder.Append("\"context\": {");
            List<KeyValuePair<string, string>> stringContext =
                new List<KeyValuePair<string, string>>();
            AddIfPresent(stringContext, "panel", diagnostic.PanelId);
            AddIfPresent(stringContext, "operation", diagnostic.OperationId);
            AddIfPresent(stringContext, "seam", diagnostic.SeamId);
            AddIfPresent(stringContext, "boundary", diagnostic.BoundaryId);
            if (stringContext.Count > 0 ||
                diagnostic.GeometryContext?.HasValue == true)
            {
                builder.Append('\n');
                int emitted = 0;
                int geometryCount = GeometryFieldCount(
                    diagnostic.GeometryContext);
                int total = stringContext.Count + geometryCount;
                for (int i = 0; i < stringContext.Count; i++)
                {
                    StringProperty(
                        builder,
                        4,
                        stringContext[i].Key,
                        stringContext[i].Value,
                        ++emitted < total);
                }

                WriteGeometryFields(
                    builder,
                    diagnostic.GeometryContext,
                    ref emitted,
                    total);
                Indent(builder, 3);
            }

            builder.Append("},\n");
            Indent(builder, 3);
            builder.Append("\"values\": [");
            for (int i = 0; i < diagnostic.Values.Count; i++)
            {
                if (i > 0) builder.Append(',');
                FoldCanvasDiagnosticValue value = diagnostic.Values[i];
                builder.Append("{\"key\":");
                FoldScriptJsonCanonicalWriter.AppendEscapedString(
                    builder,
                    value.Key);
                builder.Append(",\"value\":");
                FoldScriptJsonCanonicalWriter.AppendNumber(
                    builder,
                    value.Value);
                if (!string.IsNullOrEmpty(value.Unit))
                {
                    builder.Append(",\"unit\":");
                    FoldScriptJsonCanonicalWriter.AppendEscapedString(
                        builder,
                        value.Unit);
                }

                builder.Append('}');
            }

            builder.Append("],\n");
            Indent(builder, 3);
            builder.Append("\"suggestions\": [");
            for (int i = 0; i < diagnostic.Suggestions.Count; i++)
            {
                if (i > 0) builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendEscapedString(
                    builder,
                    diagnostic.Suggestions[i]);
            }

            builder.Append("]\n");
            Indent(builder, 2);
            builder.Append(comma ? "},\n" : "}\n");
        }

        private static int GeometryFieldCount(
            FoldCanvasDiagnosticGeometryContext context)
        {
            if (context == null) return 0;
            int count = 0;
            if (context.VertexIndex >= 0) count++;
            if (context.TopologyVertexId >= 0) count++;
            if (context.ComponentIndex >= 0) count++;
            if (context.TriangleIndex >= 0) count++;
            if (context.RelatedTriangleIndex >= 0) count++;
            if (context.EdgeTopologyVertexA >= 0) count++;
            if (context.EdgeTopologyVertexB >= 0) count++;
            return count;
        }

        private static void WriteGeometryFields(
            StringBuilder builder,
            FoldCanvasDiagnosticGeometryContext context,
            ref int emitted,
            int total)
        {
            if (context == null) return;
            WriteOptionalInteger(builder, "vertexIndex", context.VertexIndex, ref emitted, total);
            WriteOptionalInteger(builder, "topologyVertexId", context.TopologyVertexId, ref emitted, total);
            WriteOptionalInteger(builder, "componentIndex", context.ComponentIndex, ref emitted, total);
            WriteOptionalInteger(builder, "triangleIndex", context.TriangleIndex, ref emitted, total);
            WriteOptionalInteger(builder, "relatedTriangleIndex", context.RelatedTriangleIndex, ref emitted, total);
            WriteOptionalInteger(builder, "edgeTopologyVertexA", context.EdgeTopologyVertexA, ref emitted, total);
            WriteOptionalInteger(builder, "edgeTopologyVertexB", context.EdgeTopologyVertexB, ref emitted, total);
        }

        private static void WriteOptionalInteger(
            StringBuilder builder,
            string name,
            int value,
            ref int emitted,
            int total)
        {
            if (value < 0) return;
            IntegerProperty(builder, 4, name, value, ++emitted < total);
        }

        private static void AddIfPresent(
            List<KeyValuePair<string, string>> values,
            string key,
            string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                values.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        private static void StringProperty(
            StringBuilder builder,
            int indent,
            string name,
            string value,
            bool comma)
        {
            Indent(builder, indent);
            FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
            builder.Append(": ");
            FoldScriptJsonCanonicalWriter.AppendEscapedString(
                builder,
                value ?? string.Empty);
            builder.Append(comma ? ",\n" : "\n");
        }

        private static void IntegerProperty(
            StringBuilder builder,
            int indent,
            string name,
            int value,
            bool comma)
        {
            Indent(builder, indent);
            FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
            builder.Append(": ");
            builder.Append(value);
            builder.Append(comma ? ",\n" : "\n");
        }

        private static void Indent(StringBuilder builder, int count)
        {
            builder.Append(' ', count * 2);
        }
    }
}
