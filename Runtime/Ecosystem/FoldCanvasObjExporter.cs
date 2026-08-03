using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace FoldCanvas
{
    public sealed class FoldCanvasObjExportOptions
    {
        public string ObjectName { get; set; } = "FoldCanvas";

        public bool FlipTextureV { get; set; }

        public bool IncludeMetadataComments { get; set; } = true;
    }

    public sealed class FoldCanvasTextExportResult
    {
        private readonly ReadOnlyCollection<FoldCanvasDiagnostic> diagnostics;

        internal FoldCanvasTextExportResult(
            string content,
            IReadOnlyList<FoldCanvasDiagnostic> sourceDiagnostics)
        {
            Content = content;
            FoldCanvasDiagnostic[] copiedDiagnostics =
                new FoldCanvasDiagnostic[sourceDiagnostics.Count];
            for (int i = 0; i < copiedDiagnostics.Length; i++)
            {
                copiedDiagnostics[i] = sourceDiagnostics[i];
            }

            diagnostics = Array.AsReadOnly(copiedDiagnostics);
        }

        public bool Success =>
            Content != null && diagnostics.Count == 0;

        public string Content { get; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;
    }

    /// <summary>
    /// Exports immutable compiled render vertices to deterministic OBJ text.
    /// This helper performs no file I/O and never mutates source, compiled data,
    /// or Unity Mesh objects.
    /// </summary>
    public static class FoldCanvasObjExporter
    {
        public static FoldCanvasTextExportResult Export(
            FoldCanvasCompiledData data,
            FoldCanvasObjExportOptions options = null)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (data == null)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.ExportInputMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "OBJ export requires successful immutable FoldCanvas compiled data."));
                return new FoldCanvasTextExportResult(
                    null,
                    diagnostics);
            }

            options = options ?? new FoldCanvasObjExportOptions();
            if (options.ObjectName != null &&
                options.ObjectName.Length > 256)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidExportOptions,
                    FoldCanvasDiagnosticSeverity.Error,
                    "OBJ objectName must contain at most 256 characters."));
                return new FoldCanvasTextExportResult(
                    null,
                    diagnostics);
            }

            if (data.TriangleIndices.Count % 3 != 0)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidExportGeometry,
                    FoldCanvasDiagnosticSeverity.Error,
                    "OBJ export requires complete triangle triples."));
                return new FoldCanvasTextExportResult(
                    null,
                    diagnostics);
            }

            for (int i = 0; i < data.TriangleIndices.Count; i++)
            {
                int index = data.TriangleIndices[i];
                if (index < 0 || index >= data.Vertices.Count)
                {
                    diagnostics.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidExportGeometry,
                        FoldCanvasDiagnosticSeverity.Error,
                        "OBJ export encountered an out-of-range triangle index.",
                        geometryContext:
                            new FoldCanvasDiagnosticGeometryContext(
                                triangleIndex: i / 3)));
                    return new FoldCanvasTextExportResult(
                        null,
                        diagnostics);
                }
            }

            string objectName = SanitizeObjectName(options.ObjectName);
            StringBuilder builder = new StringBuilder(
                Math.Max(256, data.Vertices.Count * 72));
            builder.Append("# FoldCanvas deterministic OBJ\n");
            if (options.IncludeMetadataComments)
            {
                builder.Append("# renderVertices ");
                builder.Append(data.Vertices.Count);
                builder.Append("\n# topologyVertices ");
                builder.Append(data.TopologyVertexCount);
                builder.Append("\n# triangles ");
                builder.Append(data.TriangleIndices.Count / 3);
                builder.Append('\n');
            }

            builder.Append("o ");
            builder.Append(objectName);
            builder.Append('\n');

            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                builder.Append("v ");
                AppendFloat(builder, vertex.Position.x);
                builder.Append(' ');
                AppendFloat(builder, vertex.Position.y);
                builder.Append(' ');
                AppendFloat(builder, vertex.Position.z);
                builder.Append('\n');
            }

            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                builder.Append("vt ");
                AppendFloat(builder, vertex.SourceUv.x);
                builder.Append(' ');
                AppendFloat(
                    builder,
                    options.FlipTextureV
                        ? 1f - vertex.SourceUv.y
                        : vertex.SourceUv.y);
                builder.Append('\n');
            }

            for (int i = 0; i < data.TriangleIndices.Count; i += 3)
            {
                builder.Append("f ");
                AppendFaceVertex(builder, data.TriangleIndices[i]);
                builder.Append(' ');
                AppendFaceVertex(builder, data.TriangleIndices[i + 1]);
                builder.Append(' ');
                AppendFaceVertex(builder, data.TriangleIndices[i + 2]);
                builder.Append('\n');
            }

            return new FoldCanvasTextExportResult(
                builder.ToString(),
                diagnostics);
        }

        private static void AppendFaceVertex(
            StringBuilder builder,
            int zeroBasedIndex)
        {
            int objIndex = zeroBasedIndex + 1;
            builder.Append(objIndex);
            builder.Append('/');
            builder.Append(objIndex);
        }

        private static void AppendFloat(
            StringBuilder builder,
            float value)
        {
            float canonical = value == 0f ? 0f : value;
            builder.Append(canonical.ToString(
                "R",
                CultureInfo.InvariantCulture));
        }

        private static string SanitizeObjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "FoldCanvas";
            }

            StringBuilder builder = new StringBuilder(value.Length);
            bool previousUnderscore = false;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                bool safe = current >= 'a' && current <= 'z' ||
                    current >= 'A' && current <= 'Z' ||
                    current >= '0' && current <= '9' ||
                    current == '-' || current == '.' || current == '_';
                char output = safe ? current : '_';
                if (output == '_' && previousUnderscore)
                {
                    continue;
                }

                builder.Append(output);
                previousUnderscore = output == '_';
            }

            string sanitized = builder.ToString().Trim('_');
            return sanitized.Length == 0
                ? "FoldCanvas"
                : sanitized;
        }
    }
}
