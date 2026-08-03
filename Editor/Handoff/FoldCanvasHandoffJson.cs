using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FoldCanvas.Editor
{
    internal static class FoldCanvasHandoffJson
    {
        private static readonly string[] ManifestFields =
        {
            "format",
            "version",
            "packageVersion",
            "compilerVersion",
            "foldScriptVersion",
            "asset",
            "canvas",
            "texture",
            "payloads",
        };

        private static readonly string[] EvidenceFields =
        {
            "format",
            "version",
            "assetId",
            "packageVersion",
            "compilerVersion",
            "foldScriptVersion",
            "sourceSha256",
            "appearanceSha256",
            "geometrySha256",
            "objSha256",
            "diagnosticSha256",
            "validationSha256",
            "closedVolumeSha256",
            "sphereReportsSha256",
            "validationLevel",
            "renderVertexCount",
            "topologyVertexCount",
            "triangleCount",
            "diagnosticCount",
            "validationErrorCount",
            "validationWarningCount",
            "openEdgeCount",
            "nonManifoldEdgeCount",
            "componentCount",
            "validationIsValid",
            "isClosedVolume",
            "isSingleClosedVolume",
            "sphereReportCount",
        };

        private static readonly string[] ReceiptFields =
        {
            "format",
            "version",
            "archiveSha256",
            "assetId",
            "packageVersion",
            "compilerVersion",
            "sourceSha256",
            "appearanceSha256",
            "geometrySha256",
            "objSha256",
            "sourceJsonPath",
            "appearancePath",
            "sourceAssetPath",
            "meshPath",
            "materialPath",
            "prefabPath",
            "receiptPath",
        };

        public static string WriteManifest(FoldCanvasHandoffManifest value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            StringBuilder builder = new StringBuilder(2048);
            builder.Append("{\n");
            String(builder, 1, "format", value.Format, true);
            String(builder, 1, "version", value.Version, true);
            String(builder, 1, "packageVersion", value.PackageVersion, true);
            String(builder, 1, "compilerVersion", value.CompilerVersion, true);
            String(builder, 1, "foldScriptVersion", value.FoldScriptVersion, true);
            Indent(builder, 1);
            builder.Append("\"asset\": {\n");
            String(builder, 2, "id", value.AssetId, true);
            String(builder, 2, "displayName", value.DisplayName, false);
            Indent(builder, 1);
            builder.Append("},\n");
            Indent(builder, 1);
            builder.Append("\"canvas\": {\n");
            Integer(builder, 2, "width", value.CanvasWidth, true);
            Integer(builder, 2, "height", value.CanvasHeight, false);
            Indent(builder, 1);
            builder.Append("},\n");
            WriteTexture(builder, value.Texture);
            Indent(builder, 1);
            builder.Append("\"payloads\": [");
            if (value.Payloads.Count == 0)
            {
                builder.Append("]\n");
            }
            else
            {
                builder.Append('\n');
                for (int i = 0; i < value.Payloads.Count; i++)
                {
                    FoldCanvasHandoffPayload payload = value.Payloads[i];
                    Indent(builder, 2);
                    builder.Append("{\n");
                    String(builder, 3, "path", payload.Path, true);
                    String(builder, 3, "role", payload.Role, true);
                    Integer(builder, 3, "byteLength", payload.ByteLength, true);
                    String(builder, 3, "sha256", payload.Sha256, false);
                    Indent(builder, 2);
                    builder.Append(i + 1 < value.Payloads.Count
                        ? "},\n"
                        : "}\n");
                }

                Indent(builder, 1);
                builder.Append("]\n");
            }

            builder.Append("}\n");
            return builder.ToString();
        }

        public static string WriteEvidence(FoldCanvasHandoffEvidence value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            StringBuilder builder = new StringBuilder(2048);
            builder.Append("{\n");
            String(builder, 1, "format", value.Format, true);
            String(builder, 1, "version", value.Version, true);
            String(builder, 1, "assetId", value.AssetId, true);
            String(builder, 1, "packageVersion", value.PackageVersion, true);
            String(builder, 1, "compilerVersion", value.CompilerVersion, true);
            String(builder, 1, "foldScriptVersion", value.FoldScriptVersion, true);
            String(builder, 1, "sourceSha256", value.SourceSha256, true);
            String(builder, 1, "appearanceSha256", value.AppearanceSha256, true);
            String(builder, 1, "geometrySha256", value.GeometrySha256, true);
            String(builder, 1, "objSha256", value.ObjSha256, true);
            String(builder, 1, "diagnosticSha256", value.DiagnosticSha256, true);
            String(builder, 1, "validationSha256", value.ValidationSha256, true);
            String(builder, 1, "closedVolumeSha256", value.ClosedVolumeSha256, true);
            String(builder, 1, "sphereReportsSha256", value.SphereReportsSha256, true);
            String(builder, 1, "validationLevel", value.ValidationLevel, true);
            Integer(builder, 1, "renderVertexCount", value.RenderVertexCount, true);
            Integer(builder, 1, "topologyVertexCount", value.TopologyVertexCount, true);
            Integer(builder, 1, "triangleCount", value.TriangleCount, true);
            Integer(builder, 1, "diagnosticCount", value.DiagnosticCount, true);
            Integer(builder, 1, "validationErrorCount", value.ValidationErrorCount, true);
            Integer(builder, 1, "validationWarningCount", value.ValidationWarningCount, true);
            Integer(builder, 1, "openEdgeCount", value.OpenEdgeCount, true);
            Integer(builder, 1, "nonManifoldEdgeCount", value.NonManifoldEdgeCount, true);
            Integer(builder, 1, "componentCount", value.ComponentCount, true);
            Boolean(builder, 1, "validationIsValid", value.ValidationIsValid, true);
            Boolean(builder, 1, "isClosedVolume", value.IsClosedVolume, true);
            Boolean(builder, 1, "isSingleClosedVolume", value.IsSingleClosedVolume, true);
            Integer(builder, 1, "sphereReportCount", value.SphereReportCount, false);
            builder.Append("}\n");
            return builder.ToString();
        }

        public static string WriteReceipt(FoldCanvasHandoffReceipt value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            StringBuilder builder = new StringBuilder(1536);
            builder.Append("{\n");
            String(builder, 1, "format", value.Format, true);
            String(builder, 1, "version", value.Version, true);
            String(builder, 1, "archiveSha256", value.ArchiveSha256, true);
            String(builder, 1, "assetId", value.AssetId, true);
            String(builder, 1, "packageVersion", value.PackageVersion, true);
            String(builder, 1, "compilerVersion", value.CompilerVersion, true);
            String(builder, 1, "sourceSha256", value.SourceSha256, true);
            String(builder, 1, "appearanceSha256", value.AppearanceSha256, true);
            String(builder, 1, "geometrySha256", value.GeometrySha256, true);
            String(builder, 1, "objSha256", value.ObjSha256, true);
            String(builder, 1, "sourceJsonPath", value.SourceJsonPath, true);
            String(builder, 1, "appearancePath", value.AppearancePath, true);
            String(builder, 1, "sourceAssetPath", value.SourceAssetPath, true);
            String(builder, 1, "meshPath", value.MeshPath, true);
            String(builder, 1, "materialPath", value.MaterialPath, true);
            String(builder, 1, "prefabPath", value.PrefabPath, true);
            String(builder, 1, "receiptPath", value.ReceiptPath, false);
            builder.Append("}\n");
            return builder.ToString();
        }

        public static bool TryReadManifest(
            string json,
            out FoldCanvasHandoffManifest manifest,
            out FoldCanvasDiagnostic diagnostic)
        {
            manifest = null;
            if (!TryParseObject(json, out Dictionary<string, FoldScriptJsonValue> root,
                    out diagnostic) ||
                !HasExactFields(root, ManifestFields))
            {
                diagnostic = diagnostic ?? InvalidManifest(
                    "Handoff manifest fields do not match version 1.");
                return false;
            }

            try
            {
                Dictionary<string, FoldScriptJsonValue> asset =
                    Object(root, "asset", "manifest asset");
                Dictionary<string, FoldScriptJsonValue> canvas =
                    Object(root, "canvas", "manifest canvas");
                Dictionary<string, FoldScriptJsonValue> texture =
                    Object(root, "texture", "manifest texture");
                if (!HasExactFields(asset, "id", "displayName") ||
                    !HasExactFields(canvas, "width", "height") ||
                    !HasExactFields(
                        texture,
                        "filterMode",
                        "wrapModeU",
                        "wrapModeV",
                        "mipmapEnabled",
                        "sRgbTexture",
                        "alphaIsTransparency",
                        "anisoLevel",
                        "pixelsPerUnit"))
                {
                    throw new HandoffJsonException(
                        "Nested manifest fields do not match version 1.");
                }

                FoldCanvasHandoffManifest value =
                    new FoldCanvasHandoffManifest
                    {
                        Format = String(root, "format"),
                        Version = String(root, "version"),
                        PackageVersion = String(root, "packageVersion"),
                        CompilerVersion = String(root, "compilerVersion"),
                        FoldScriptVersion = String(root, "foldScriptVersion"),
                        AssetId = String(asset, "id"),
                        DisplayName = String(asset, "displayName"),
                        CanvasWidth = PositiveInteger(canvas, "width"),
                        CanvasHeight = PositiveInteger(canvas, "height"),
                        Texture = new FoldCanvasHandoffTextureContract
                        {
                            FilterMode = String(texture, "filterMode"),
                            WrapModeU = String(texture, "wrapModeU"),
                            WrapModeV = String(texture, "wrapModeV"),
                            MipmapEnabled = Boolean(texture, "mipmapEnabled"),
                            SrgbTexture = Boolean(texture, "sRgbTexture"),
                            AlphaIsTransparency = Boolean(
                                texture,
                                "alphaIsTransparency"),
                            AnisoLevel = NonNegativeInteger(
                                texture,
                                "anisoLevel"),
                            PixelsPerUnit = PositiveInteger(
                                texture,
                                "pixelsPerUnit"),
                        },
                    };

                List<FoldScriptJsonValue> payloads =
                    Array(root, "payloads", "manifest payloads");
                if (payloads.Count != 5)
                {
                    throw new HandoffJsonException(
                        "Handoff manifest must describe exactly five payload entries.");
                }

                for (int i = 0; i < payloads.Count; i++)
                {
                    Dictionary<string, FoldScriptJsonValue> payload =
                        Object(payloads[i], "manifest payload");
                    if (!HasExactFields(
                            payload,
                            "path",
                            "role",
                            "byteLength",
                            "sha256"))
                    {
                        throw new HandoffJsonException(
                            "Manifest payload fields do not match version 1.");
                    }

                    value.Payloads.Add(new FoldCanvasHandoffPayload
                    {
                        Path = String(payload, "path"),
                        Role = String(payload, "role"),
                        ByteLength = NonNegativeLong(payload, "byteLength"),
                        Sha256 = Sha256(payload, "sha256"),
                    });
                }

                ValidateManifest(value);
                if (!string.Equals(json, WriteManifest(value), StringComparison.Ordinal))
                {
                    throw new HandoffJsonException(
                        "Handoff manifest must use canonical UTF-8 JSON formatting.");
                }

                manifest = value;
                diagnostic = null;
                return true;
            }
            catch (HandoffJsonException exception)
            {
                diagnostic = InvalidManifest(exception.Message);
                return false;
            }
        }

        public static bool TryReadEvidence(
            string json,
            out FoldCanvasHandoffEvidence evidence,
            out FoldCanvasDiagnostic diagnostic)
        {
            evidence = null;
            if (!TryParseObject(json, out Dictionary<string, FoldScriptJsonValue> root,
                    out diagnostic) ||
                !HasExactFields(root, EvidenceFields))
            {
                diagnostic = diagnostic ?? InvalidManifest(
                    "Handoff evidence fields do not match version 1.");
                return false;
            }

            try
            {
                FoldCanvasHandoffEvidence value =
                    new FoldCanvasHandoffEvidence
                    {
                        Format = String(root, "format"),
                        Version = String(root, "version"),
                        AssetId = String(root, "assetId"),
                        PackageVersion = String(root, "packageVersion"),
                        CompilerVersion = String(root, "compilerVersion"),
                        FoldScriptVersion = String(root, "foldScriptVersion"),
                        SourceSha256 = Sha256(root, "sourceSha256"),
                        AppearanceSha256 = Sha256(root, "appearanceSha256"),
                        GeometrySha256 = Sha256(root, "geometrySha256"),
                        ObjSha256 = Sha256(root, "objSha256"),
                        DiagnosticSha256 = Sha256(root, "diagnosticSha256"),
                        ValidationSha256 = Sha256(root, "validationSha256"),
                        ClosedVolumeSha256 = Sha256(root, "closedVolumeSha256"),
                        SphereReportsSha256 = Sha256(root, "sphereReportsSha256"),
                        ValidationLevel = String(root, "validationLevel"),
                        RenderVertexCount = NonNegativeInteger(root, "renderVertexCount"),
                        TopologyVertexCount = NonNegativeInteger(root, "topologyVertexCount"),
                        TriangleCount = NonNegativeInteger(root, "triangleCount"),
                        DiagnosticCount = NonNegativeInteger(root, "diagnosticCount"),
                        ValidationErrorCount = NonNegativeInteger(root, "validationErrorCount"),
                        ValidationWarningCount = NonNegativeInteger(root, "validationWarningCount"),
                        OpenEdgeCount = NonNegativeInteger(root, "openEdgeCount"),
                        NonManifoldEdgeCount = NonNegativeInteger(root, "nonManifoldEdgeCount"),
                        ComponentCount = NonNegativeInteger(root, "componentCount"),
                        ValidationIsValid = Boolean(root, "validationIsValid"),
                        IsClosedVolume = Boolean(root, "isClosedVolume"),
                        IsSingleClosedVolume = Boolean(root, "isSingleClosedVolume"),
                        SphereReportCount = NonNegativeInteger(root, "sphereReportCount"),
                    };

                if (!string.Equals(
                        value.Format,
                        FoldCanvasHandoffFormat.EvidenceFormat,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        value.Version,
                        FoldCanvasHandoffFormat.Version,
                        StringComparison.Ordinal))
                {
                    throw new HandoffJsonException(
                        "Handoff evidence format or version is unsupported.");
                }

                if (!string.Equals(json, WriteEvidence(value), StringComparison.Ordinal))
                {
                    throw new HandoffJsonException(
                        "Handoff evidence must use canonical UTF-8 JSON formatting.");
                }

                evidence = value;
                diagnostic = null;
                return true;
            }
            catch (HandoffJsonException exception)
            {
                diagnostic = InvalidManifest(exception.Message);
                return false;
            }
        }

        public static bool TryReadReceipt(
            string json,
            out FoldCanvasHandoffReceipt receipt,
            out FoldCanvasDiagnostic diagnostic)
        {
            receipt = null;
            if (!TryParseObject(json, out Dictionary<string, FoldScriptJsonValue> root,
                    out diagnostic) ||
                !HasExactFields(root, ReceiptFields))
            {
                diagnostic = diagnostic ?? InvalidManifest(
                    "Handoff receipt fields do not match version 1.");
                return false;
            }

            try
            {
                FoldCanvasHandoffReceipt value = new FoldCanvasHandoffReceipt
                {
                    Format = String(root, "format"),
                    Version = String(root, "version"),
                    ArchiveSha256 = Sha256(root, "archiveSha256"),
                    AssetId = String(root, "assetId"),
                    PackageVersion = String(root, "packageVersion"),
                    CompilerVersion = String(root, "compilerVersion"),
                    SourceSha256 = Sha256(root, "sourceSha256"),
                    AppearanceSha256 = Sha256(root, "appearanceSha256"),
                    GeometrySha256 = Sha256(root, "geometrySha256"),
                    ObjSha256 = Sha256(root, "objSha256"),
                    SourceJsonPath = String(root, "sourceJsonPath"),
                    AppearancePath = String(root, "appearancePath"),
                    SourceAssetPath = String(root, "sourceAssetPath"),
                    MeshPath = String(root, "meshPath"),
                    MaterialPath = String(root, "materialPath"),
                    PrefabPath = String(root, "prefabPath"),
                    ReceiptPath = String(root, "receiptPath"),
                };

                if (!string.Equals(
                        value.Format,
                        FoldCanvasHandoffFormat.ReceiptFormat,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        value.Version,
                        FoldCanvasHandoffFormat.Version,
                        StringComparison.Ordinal) ||
                    !string.Equals(json, WriteReceipt(value), StringComparison.Ordinal))
                {
                    throw new HandoffJsonException(
                        "Handoff receipt format, version, or canonical JSON is invalid.");
                }

                receipt = value;
                diagnostic = null;
                return true;
            }
            catch (HandoffJsonException exception)
            {
                diagnostic = InvalidManifest(exception.Message);
                return false;
            }
        }

        private static void ValidateManifest(FoldCanvasHandoffManifest manifest)
        {
            string[] paths =
            {
                FoldCanvasHandoffFormat.SourceEntry,
                FoldCanvasHandoffFormat.AppearanceEntry,
                FoldCanvasHandoffFormat.ObjEntry,
                FoldCanvasHandoffFormat.EvidenceEntry,
                FoldCanvasHandoffFormat.ReadmeEntry,
            };
            string[] roles =
            {
                "source",
                "appearance",
                "derived-obj",
                "compile-evidence",
                "instructions",
            };
            for (int i = 0; i < paths.Length; i++)
            {
                FoldCanvasHandoffPayload payload = manifest.Payloads[i];
                if (!string.Equals(payload.Path, paths[i], StringComparison.Ordinal) ||
                    !string.Equals(payload.Role, roles[i], StringComparison.Ordinal) ||
                    payload.ByteLength < 0 ||
                    payload.ByteLength >
                    FoldCanvasHandoffLimits.MaximumBytesForEntry(payload.Path))
                {
                    throw new HandoffJsonException(
                        "Manifest payload order, role, or size is invalid.");
                }
            }
        }

        private static void WriteTexture(
            StringBuilder builder,
            FoldCanvasHandoffTextureContract texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            Indent(builder, 1);
            builder.Append("\"texture\": {\n");
            String(builder, 2, "filterMode", texture.FilterMode, true);
            String(builder, 2, "wrapModeU", texture.WrapModeU, true);
            String(builder, 2, "wrapModeV", texture.WrapModeV, true);
            Boolean(builder, 2, "mipmapEnabled", texture.MipmapEnabled, true);
            Boolean(builder, 2, "sRgbTexture", texture.SrgbTexture, true);
            Boolean(builder, 2, "alphaIsTransparency", texture.AlphaIsTransparency, true);
            Integer(builder, 2, "anisoLevel", texture.AnisoLevel, true);
            Integer(builder, 2, "pixelsPerUnit", texture.PixelsPerUnit, false);
            Indent(builder, 1);
            builder.Append("},\n");
        }

        private static bool TryParseObject(
            string json,
            out Dictionary<string, FoldScriptJsonValue> value,
            out FoldCanvasDiagnostic diagnostic)
        {
            value = null;
            if (!FoldScriptJsonReader.TryParse(
                    json,
                    out FoldScriptJsonValue parsed,
                    out FoldCanvasDiagnostic parseDiagnostic) ||
                parsed.Kind != FoldScriptJsonKind.Object)
            {
                diagnostic = InvalidManifest(
                    parseDiagnostic?.Message ??
                    "Handoff JSON root must be an object.");
                return false;
            }

            value = parsed.ObjectValue;
            diagnostic = null;
            return true;
        }

        private static bool HasExactFields(
            Dictionary<string, FoldScriptJsonValue> value,
            params string[] fields)
        {
            if (value.Count != fields.Length)
            {
                return false;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (!value.ContainsKey(fields[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, FoldScriptJsonValue> Object(
            Dictionary<string, FoldScriptJsonValue> value,
            string field,
            string context)
        {
            if (!value.TryGetValue(field, out FoldScriptJsonValue child))
            {
                throw new HandoffJsonException(
                    $"Missing required {context} field '{field}'.");
            }

            return Object(child, context);
        }

        private static Dictionary<string, FoldScriptJsonValue> Object(
            FoldScriptJsonValue value,
            string context)
        {
            if (value == null || value.Kind != FoldScriptJsonKind.Object)
            {
                throw new HandoffJsonException(
                    $"{context} must be a JSON object.");
            }

            return value.ObjectValue;
        }

        private static List<FoldScriptJsonValue> Array(
            Dictionary<string, FoldScriptJsonValue> value,
            string field,
            string context)
        {
            if (!value.TryGetValue(field, out FoldScriptJsonValue child) ||
                child.Kind != FoldScriptJsonKind.Array)
            {
                throw new HandoffJsonException(
                    $"{context} must be a JSON array.");
            }

            return child.ArrayValue;
        }

        private static string String(
            Dictionary<string, FoldScriptJsonValue> value,
            string field)
        {
            if (!value.TryGetValue(field, out FoldScriptJsonValue child) ||
                child.Kind != FoldScriptJsonKind.String ||
                string.IsNullOrWhiteSpace(child.StringValue))
            {
                throw new HandoffJsonException(
                    $"Handoff field '{field}' must be a non-empty string.");
            }

            return child.StringValue;
        }

        private static string Sha256(
            Dictionary<string, FoldScriptJsonValue> value,
            string field)
        {
            string hash = String(value, field);
            if (!FoldCanvasHandoffHash.IsSha256(hash))
            {
                throw new HandoffJsonException(
                    $"Handoff field '{field}' must be a lowercase SHA-256 digest.");
            }

            return hash;
        }

        private static bool Boolean(
            Dictionary<string, FoldScriptJsonValue> value,
            string field)
        {
            if (!value.TryGetValue(field, out FoldScriptJsonValue child) ||
                child.Kind != FoldScriptJsonKind.Boolean)
            {
                throw new HandoffJsonException(
                    $"Handoff field '{field}' must be a boolean.");
            }

            return child.BooleanValue;
        }

        private static int PositiveInteger(
            Dictionary<string, FoldScriptJsonValue> value,
            string field)
        {
            int result = NonNegativeInteger(value, field);
            if (result == 0)
            {
                throw new HandoffJsonException(
                    $"Handoff field '{field}' must be positive.");
            }

            return result;
        }

        private static int NonNegativeInteger(
            Dictionary<string, FoldScriptJsonValue> value,
            string field)
        {
            long result = NonNegativeLong(value, field);
            if (result > int.MaxValue)
            {
                throw new HandoffJsonException(
                    $"Handoff field '{field}' exceeds Int32 range.");
            }

            return (int)result;
        }

        private static long NonNegativeLong(
            Dictionary<string, FoldScriptJsonValue> value,
            string field)
        {
            if (!value.TryGetValue(field, out FoldScriptJsonValue child) ||
                child.Kind != FoldScriptJsonKind.Number ||
                child.NumberValue < 0d ||
                child.NumberValue > long.MaxValue ||
                Math.Floor(child.NumberValue) != child.NumberValue)
            {
                throw new HandoffJsonException(
                    $"Handoff field '{field}' must be a non-negative integer.");
            }

            return checked((long)child.NumberValue);
        }

        private static void String(
            StringBuilder builder,
            int depth,
            string name,
            string value,
            bool comma)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            Indent(builder, depth);
            FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
            builder.Append(": ");
            FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, value);
            builder.Append(comma ? ",\n" : "\n");
        }

        private static void Integer(
            StringBuilder builder,
            int depth,
            string name,
            long value,
            bool comma)
        {
            Indent(builder, depth);
            FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
            builder.Append(": ");
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
            builder.Append(comma ? ",\n" : "\n");
        }

        private static void Boolean(
            StringBuilder builder,
            int depth,
            string name,
            bool value,
            bool comma)
        {
            Indent(builder, depth);
            FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
            builder.Append(value ? ": true" : ": false");
            builder.Append(comma ? ",\n" : "\n");
        }

        private static void Indent(StringBuilder builder, int depth)
        {
            builder.Append(' ', depth * 2);
        }

        private static FoldCanvasDiagnostic InvalidManifest(string message)
        {
            return new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.InvalidHandoffManifest,
                FoldCanvasDiagnosticSeverity.Error,
                message);
        }

        private sealed class HandoffJsonException : Exception
        {
            public HandoffJsonException(string message)
                : base(message)
            {
            }
        }
    }
}
