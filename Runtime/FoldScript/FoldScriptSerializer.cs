using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FoldCanvas
{
    public static class FoldScriptSerializer
    {
        public static FoldScriptReadResult Read(string json)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (!FoldScriptJsonReader.TryParse(
                    json,
                    out FoldScriptJsonValue root,
                    out FoldCanvasDiagnostic parseDiagnostic))
            {
                diagnostics.Add(parseDiagnostic);
                return new FoldScriptReadResult(null, diagnostics);
            }

            FoldScriptDocument document =
                FoldScriptDecoder.Decode(root, diagnostics);
            return new FoldScriptReadResult(document, diagnostics);
        }

        public static FoldScriptWriteResult Write(FoldScriptDocument document)
        {
            List<FoldCanvasDiagnostic> diagnostics =
                new List<FoldCanvasDiagnostic>();
            if (!FoldScriptCanonicalDocumentWriter.TryWrite(
                    document,
                    diagnostics,
                    out string json))
            {
                return new FoldScriptWriteResult(null, diagnostics);
            }

            FoldScriptReadResult validation = Read(json);
            if (!validation.Success)
            {
                diagnostics.AddRange(validation.Diagnostics);
                return new FoldScriptWriteResult(null, diagnostics);
            }

            return new FoldScriptWriteResult(json, diagnostics);
        }

        public static FoldScriptWriteResult Canonicalize(string json)
        {
            FoldScriptReadResult read = Read(json);
            if (!read.Success)
            {
                return new FoldScriptWriteResult(
                    null,
                    new List<FoldCanvasDiagnostic>(read.Diagnostics));
            }

            return Write(read.Document);
        }
    }

    internal static class FoldScriptCanonicalDocumentWriter
    {
        public static bool TryWrite(
            FoldScriptDocument document,
            List<FoldCanvasDiagnostic> diagnostics,
            out string json)
        {
            json = null;
            if (document == null)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Cannot export a null FoldScript document."));
                return false;
            }

            try
            {
                Writer writer = new Writer();
                json = writer.WriteDocument(document);
                return true;
            }
            catch (FoldScriptWriteException exception)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    exception.Code,
                    FoldCanvasDiagnosticSeverity.Error,
                    exception.Message,
                    exception.PanelId,
                    exception.OperationId,
                    exception.SeamId));
                return false;
            }
            catch (Exception exception)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript export rejected invalid DTO content: " +
                    exception.Message));
                return false;
            }
        }

        private sealed class Writer
        {
            private readonly StringBuilder builder = new StringBuilder();

            public string WriteDocument(FoldScriptDocument document)
            {
                Require(document.Canvas, "FoldScript canvas cannot be null.");
                Require(document.Compile, "FoldScript compile settings cannot be null.");

                builder.Append("{\n");
                StringProperty(1, "schemaVersion", document.SchemaVersion, true);
                StringProperty(1, "assetId", document.AssetId, true);
                StringProperty(1, "displayName", document.DisplayName, true);
                StringProperty(1, "units", UnitText(document.Units), true);
                CanvasProperty(document.Canvas);
                PanelsProperty(document.Panels);
                SeamsProperty(document.Seams);
                OperationsProperty(document.Operations);
                CompileProperty(document.Compile,
                    !string.IsNullOrWhiteSpace(document.ExtensionsJson));
                if (!string.IsNullOrWhiteSpace(document.ExtensionsJson))
                {
                    ExtensionsProperty(document.ExtensionsJson);
                }

                builder.Append("}\n");
                return builder.ToString();
            }

            private void CanvasProperty(FoldScriptCanvas canvas)
            {
                Indent(1);
                builder.Append("\"canvas\": {\n");
                StringProperty(2, "appearance", canvas.Appearance, true);
                IntegerProperty(2, "width", canvas.Width, true);
                IntegerProperty(2, "height", canvas.Height, false);
                Indent(1);
                builder.Append("},\n");
            }

            private void PanelsProperty(IList<FoldScriptPanel> panels)
            {
                Require(panels, "FoldScript panels cannot be null.");
                Indent(1);
                builder.Append("\"panels\": [");
                if (panels.Count == 0)
                {
                    builder.Append("],\n");
                    return;
                }

                builder.Append('\n');
                for (int i = 0; i < panels.Count; i++)
                {
                    FoldScriptPanel panel = panels[i];
                    Require(panel, $"FoldScript panel at index {i} is null.");
                    Indent(2);
                    builder.Append("{\n");
                    StringProperty(3, "id", panel.Id, true);
                    StringProperty(
                        3,
                        "shape",
                        panel.Shape == PanelShape.Rectangle
                            ? "rectangle"
                            : panel.Shape == PanelShape.Disk
                                ? "disk"
                                : throw new FoldScriptWriteException(
                                    FoldCanvasDiagnosticCodes
                                        .InvalidFoldScriptStructure,
                                    $"Panel '{panel.Id}' has unsupported shape value {(int)panel.Shape}.",
                                    panelId: panel.Id),
                        true);
                    Vector4Property(
                        3,
                        "canvasRect",
                        new Vector4(
                            panel.CanvasRect.x,
                            panel.CanvasRect.y,
                            panel.CanvasRect.width,
                            panel.CanvasRect.height),
                        true);
                    Vector2Property(3, "physicalSize", panel.PhysicalSize, true);
                    Indent(3);
                    builder.Append("\"tessellation\": {\n");
                    if (panel.Shape == PanelShape.Rectangle)
                    {
                        IntegerProperty(4, "uSegments", panel.USegments, true);
                        IntegerProperty(4, "vSegments", panel.VSegments, false);
                    }
                    else
                    {
                        IntegerProperty(
                            4,
                            "radialSegments",
                            panel.RadialSegments,
                            true);
                        IntegerProperty(
                            4,
                            "radialRings",
                            panel.RadialRings,
                            false);
                    }

                    Indent(3);
                    builder.Append("}\n");
                    Indent(2);
                    builder.Append(i + 1 < panels.Count ? "},\n" : "}\n");
                }

                Indent(1);
                builder.Append("],\n");
            }

            private void SeamsProperty(IList<FoldScriptSeam> seams)
            {
                Require(seams, "FoldScript seams cannot be null.");
                Indent(1);
                builder.Append("\"seams\": [");
                if (seams.Count == 0)
                {
                    builder.Append("],\n");
                    return;
                }

                builder.Append('\n');
                for (int i = 0; i < seams.Count; i++)
                {
                    FoldScriptSeam seam = seams[i];
                    Require(seam, $"FoldScript seam at index {i} is null.");
                    Require(seam.A, $"FoldScript seam '{seam.Id}' endpoint A is null.");
                    Require(seam.B, $"FoldScript seam '{seam.Id}' endpoint B is null.");
                    Indent(2);
                    builder.Append("{\n");
                    StringProperty(3, "id", seam.Id, true);
                    BoundaryReferenceProperty(3, "a", seam.A, true);
                    BoundaryReferenceProperty(3, "b", seam.B, true);
                    StringProperty(3, "mode", SeamModeText(seam.Mode), true);
                    BooleanProperty(3, "reverseB", seam.ReverseB, true);
                    IntegerProperty(3, "sampleCount", seam.SampleCount, false);
                    Indent(2);
                    builder.Append(i + 1 < seams.Count ? "},\n" : "}\n");
                }

                Indent(1);
                builder.Append("],\n");
            }

            private void BoundaryReferenceProperty(
                int indent,
                string name,
                FoldScriptBoundaryReference reference,
                bool comma)
            {
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": {\n");
                StringProperty(indent + 1, "panel", reference.PanelId, true);
                StringProperty(indent + 1, "boundary", reference.BoundaryId, false);
                Indent(indent);
                builder.Append(comma ? "},\n" : "}\n");
            }

            private void OperationsProperty(IList<FoldScriptOperation> operations)
            {
                Require(operations, "FoldScript operations cannot be null.");
                Indent(1);
                builder.Append("\"operations\": [");
                if (operations.Count == 0)
                {
                    builder.Append("],\n");
                    return;
                }

                builder.Append('\n');
                for (int i = 0; i < operations.Count; i++)
                {
                    FoldScriptOperation operation = operations[i];
                    Require(
                        operation,
                        $"FoldScript operation at index {i} is null.");
                    WriteOperation(operation);
                    Indent(2);
                    builder.Append(i + 1 < operations.Count ? "},\n" : "}\n");
                }

                Indent(1);
                builder.Append("],\n");
            }

            private void WriteOperation(FoldScriptOperation operation)
            {
                Indent(2);
                builder.Append("{\n");
                StringProperty(3, "id", operation.Id, true);
                BooleanProperty(3, "enabled", operation.Enabled, true);
                switch (operation)
                {
                    case FoldScriptRigidTransformOperation rigid:
                        StringProperty(3, "type", "rigidTransform", true);
                        StringProperty(3, "panel", rigid.PanelId, true);
                        Vector3Property(3, "translation", rigid.Translation, true);
                        Vector3Property(
                            3,
                            "rotationEuler",
                            rigid.RotationEuler,
                            true);
                        Vector3Property(3, "scale", rigid.Scale, false);
                        return;
                    case FoldScriptFoldOperation fold:
                        StringProperty(3, "type", "fold", true);
                        StringProperty(3, "panel", fold.PanelId, true);
                        Indent(3);
                        builder.Append("\"line\": [");
                        AppendVector2(fold.LineStart);
                        builder.Append(',');
                        AppendVector2(fold.LineEnd);
                        builder.Append("],\n");
                        StringProperty(
                            3,
                            "side",
                            fold.Side == FoldSide.Positive
                                ? "positive"
                                : fold.Side == FoldSide.Negative
                                    ? "negative"
                                    : throw UnsupportedEnum(
                                        operation,
                                        "fold side",
                                        (int)fold.Side),
                            true);
                        NumberProperty(
                            3,
                            "angleDegrees",
                            fold.AngleDegrees,
                            true,
                            operation);
                        NumberProperty(
                            3,
                            "falloff",
                            fold.Falloff,
                            false,
                            operation);
                        return;
                    case FoldScriptRollOperation roll:
                        StringProperty(3, "type", "roll", true);
                        StringProperty(3, "panel", roll.PanelId, true);
                        StringProperty(
                            3,
                            "direction",
                            roll.Direction == RollDirection.U
                                ? "u"
                                : roll.Direction == RollDirection.V
                                    ? "v"
                                    : throw UnsupportedEnum(
                                        operation,
                                        "roll direction",
                                        (int)roll.Direction),
                            true);
                        NumberProperty(
                            3,
                            "angleDegrees",
                            roll.AngleDegrees,
                            true,
                            operation);
                        StringProperty(
                            3,
                            "radiusMode",
                            RollRadiusModeText(roll.RadiusMode),
                            true);
                        NumberProperty(
                            3,
                            "radius",
                            roll.ExplicitRadius,
                            true,
                            operation);
                        if (!string.IsNullOrEmpty(roll.TargetSeamId))
                        {
                            StringProperty(
                                3,
                                "targetSeam",
                                roll.TargetSeamId,
                                true);
                        }

                        NumberProperty(
                            3,
                            "startAngleDegrees",
                            roll.StartAngleDegrees,
                            false,
                            operation);
                        return;
                    case FoldScriptSphericalWrapOperation sphere:
                        StringProperty(3, "type", "sphericalWrap", true);
                        StringProperty(3, "panel", sphere.PanelId, true);
                        NumberProperty(
                            3,
                            "radius",
                            sphere.Radius,
                            true,
                            operation);
                        Vector2Property(
                            3,
                            "latitudeRange",
                            sphere.LatitudeRange,
                            true,
                            operation);
                        Vector2Property(
                            3,
                            "longitudeRange",
                            sphere.LongitudeRange,
                            true,
                            operation);
                        StringProperty(
                            3,
                            "wrapDirection",
                            sphere.WrapDirection ==
                                SphericalWrapDirection.LongitudeAlongU
                                ? "longitudeAlongU"
                                : sphere.WrapDirection ==
                                    SphericalWrapDirection.LongitudeAlongV
                                    ? "longitudeAlongV"
                                    : throw UnsupportedEnum(
                                        operation,
                                        "spherical wrap direction",
                                        (int)sphere.WrapDirection),
                            true);
                        StringProperty(
                            3,
                            "poleMode",
                            sphere.PoleMode == SphericalPoleMode.Merge
                                ? "merge"
                                : sphere.PoleMode == SphericalPoleMode.KeepFan
                                    ? "keepFan"
                                    : throw UnsupportedEnum(
                                        operation,
                                        "spherical pole mode",
                                        (int)sphere.PoleMode),
                            true);
                        StringProperty(
                            3,
                            "subdivisionMode",
                            sphere.SubdivisionMode ==
                                SphericalSubdivisionMode.PanelGrid
                                ? "panelGrid"
                                : throw UnsupportedEnum(
                                    operation,
                                    "spherical subdivision mode",
                                    (int)sphere.SubdivisionMode),
                            false);
                        return;
                    case FoldScriptStitchOperation stitch:
                        StringProperty(3, "type", "stitch", true);
                        IdentifierArrayProperty(3, "seams", stitch.SeamIds, false);
                        return;
                    case FoldScriptSolidifyOperation solidify:
                        StringProperty(3, "type", "solidify", true);
                        IdentifierArrayProperty(
                            3,
                            "targets",
                            solidify.PanelIds,
                            true);
                        NumberProperty(
                            3,
                            "thickness",
                            solidify.Thickness,
                            true,
                            operation);
                        StringProperty(
                            3,
                            "direction",
                            SolidifyDirectionText(solidify.Direction),
                            false);
                        return;
                    default:
                        throw new FoldScriptWriteException(
                            FoldCanvasDiagnosticCodes.UnknownFoldScriptOperation,
                            $"Operation '{operation.Id}' uses unsupported DTO type '{operation.GetType().Name}'.",
                            operationId: operation.Id);
                }
            }

            private void CompileProperty(
                FoldScriptCompileSettings compile,
                bool comma)
            {
                Indent(1);
                builder.Append("\"compile\": {\n");
                NumberProperty(2, "weldEpsilon", compile.WeldEpsilon, true);
                BooleanProperty(
                    2,
                    "recalculateNormals",
                    compile.RecalculateNormals,
                    true);
                StringProperty(
                    2,
                    "validationLevel",
                    ValidationLevelText(compile.ValidationLevel),
                    true);
                IntegerProperty(
                    2,
                    "maxGeneratedVertices",
                    compile.MaxGeneratedVertices,
                    true);
                IntegerProperty(
                    2,
                    "maxGeneratedTriangles",
                    compile.MaxGeneratedTriangles,
                    false);
                Indent(1);
                builder.Append(comma ? "},\n" : "}\n");
            }

            private void ExtensionsProperty(string extensionsJson)
            {
                if (!FoldScriptJsonReader.TryParse(
                        extensionsJson,
                        out FoldScriptJsonValue value,
                        out FoldCanvasDiagnostic diagnostic))
                {
                    throw new FoldScriptWriteException(
                        diagnostic.Code,
                        "FoldScript extensions JSON is invalid: " +
                        diagnostic.Message);
                }

                if (value.Kind != FoldScriptJsonKind.Object)
                {
                    throw new FoldScriptWriteException(
                        FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                        "FoldScript extensions JSON must be an object.");
                }

                Indent(1);
                builder.Append("\"extensions\": ");
                builder.Append(FoldScriptJsonCanonicalWriter.Write(value));
                builder.Append('\n');
            }

            private void IdentifierArrayProperty(
                int indent,
                string name,
                IList<string> values,
                bool comma)
            {
                Require(values, $"FoldScript {name} array cannot be null.");
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": [");
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    FoldScriptJsonCanonicalWriter.AppendEscapedString(
                        builder,
                        RequireString(values[i], $"FoldScript {name}[{i}] is null."));
                }

                builder.Append(comma ? "],\n" : "]\n");
            }

            private void StringProperty(
                int indent,
                string name,
                string value,
                bool comma)
            {
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": ");
                FoldScriptJsonCanonicalWriter.AppendEscapedString(
                    builder,
                    RequireString(value, $"FoldScript property '{name}' is null."));
                builder.Append(comma ? ",\n" : "\n");
            }

            private void BooleanProperty(
                int indent,
                string name,
                bool value,
                bool comma)
            {
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(value ? ": true" : ": false");
                builder.Append(comma ? ",\n" : "\n");
            }

            private void IntegerProperty(
                int indent,
                string name,
                int value,
                bool comma)
            {
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": ");
                builder.Append(value.ToString(CultureInfo.InvariantCulture));
                builder.Append(comma ? ",\n" : "\n");
            }

            private void NumberProperty(
                int indent,
                string name,
                float value,
                bool comma,
                FoldScriptOperation operation = null)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new FoldScriptWriteException(
                        FoldCanvasDiagnosticCodes.NonFiniteFoldScriptNumber,
                        $"FoldScript property '{name}' must be finite.",
                        operationId: operation?.Id);
                }

                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": ");
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value);
                builder.Append(comma ? ",\n" : "\n");
            }

            private void Vector2Property(
                int indent,
                string name,
                Vector2 value,
                bool comma,
                FoldScriptOperation operation = null)
            {
                EnsureFinite(value.x, name, operation);
                EnsureFinite(value.y, name, operation);
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": ");
                AppendVector2(value);
                builder.Append(comma ? ",\n" : "\n");
            }

            private void Vector3Property(
                int indent,
                string name,
                Vector3 value,
                bool comma)
            {
                EnsureFinite(value.x, name, null);
                EnsureFinite(value.y, name, null);
                EnsureFinite(value.z, name, null);
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": [");
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.x);
                builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.y);
                builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.z);
                builder.Append(comma ? "],\n" : "]\n");
            }

            private void Vector4Property(
                int indent,
                string name,
                Vector4 value,
                bool comma)
            {
                EnsureFinite(value.x, name, null);
                EnsureFinite(value.y, name, null);
                EnsureFinite(value.z, name, null);
                EnsureFinite(value.w, name, null);
                Indent(indent);
                FoldScriptJsonCanonicalWriter.AppendEscapedString(builder, name);
                builder.Append(": [");
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.x);
                builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.y);
                builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.z);
                builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.w);
                builder.Append(comma ? "],\n" : "]\n");
            }

            private void AppendVector2(Vector2 value)
            {
                EnsureFinite(value.x, "vector", null);
                EnsureFinite(value.y, "vector", null);
                builder.Append('[');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.x);
                builder.Append(',');
                FoldScriptJsonCanonicalWriter.AppendNumber(builder, value.y);
                builder.Append(']');
            }

            private static void EnsureFinite(
                float value,
                string name,
                FoldScriptOperation operation)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new FoldScriptWriteException(
                        FoldCanvasDiagnosticCodes.NonFiniteFoldScriptNumber,
                        $"FoldScript property '{name}' must be finite.",
                        operationId: operation?.Id);
                }
            }

            private void Indent(int count)
            {
                builder.Append(' ', count * 2);
            }

            private static void Require(object value, string message)
            {
                if (value == null)
                {
                    throw new FoldScriptWriteException(
                        FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                        message);
                }
            }

            private static string RequireString(string value, string message)
            {
                if (value == null)
                {
                    throw new FoldScriptWriteException(
                        FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                        message);
                }

                return value;
            }

            private static FoldScriptWriteException UnsupportedEnum(
                FoldScriptOperation operation,
                string label,
                int value)
            {
                return new FoldScriptWriteException(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    $"Operation '{operation.Id}' has unsupported {label} value {value}.",
                    operationId: operation.Id);
            }

            private static string UnitText(FoldScriptUnit value)
            {
                switch (value)
                {
                    case FoldScriptUnit.Meter: return "meter";
                    case FoldScriptUnit.Centimeter: return "centimeter";
                    case FoldScriptUnit.Millimeter: return "millimeter";
                    default:
                        throw new FoldScriptWriteException(
                            FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                            $"Unsupported FoldScript unit value {(int)value}.");
                }
            }

            private static string SeamModeText(SeamMode value)
            {
                switch (value)
                {
                    case SeamMode.Weld: return "weld";
                    case SeamMode.Hinge: return "hinge";
                    case SeamMode.KeepOpen: return "keepOpen";
                    case SeamMode.Bridge: return "bridge";
                    default:
                        throw new FoldScriptWriteException(
                            FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                            $"Unsupported seam mode value {(int)value}.");
                }
            }

            private static string RollRadiusModeText(RollRadiusMode value)
            {
                switch (value)
                {
                    case RollRadiusMode.PreserveArcLength: return "preserveArcLength";
                    case RollRadiusMode.Explicit: return "explicit";
                    case RollRadiusMode.FitTargetBoundary: return "fitTargetBoundary";
                    default:
                        throw new FoldScriptWriteException(
                            FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                            $"Unsupported roll radius mode value {(int)value}.");
                }
            }

            private static string SolidifyDirectionText(SolidifyDirection value)
            {
                switch (value)
                {
                    case SolidifyDirection.Inward: return "inward";
                    case SolidifyDirection.Outward: return "outward";
                    case SolidifyDirection.Centered: return "centered";
                    default:
                        throw new FoldScriptWriteException(
                            FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                            $"Unsupported solidify direction value {(int)value}.");
                }
            }

            private static string ValidationLevelText(
                FoldCanvasValidationLevel value)
            {
                switch (value)
                {
                    case FoldCanvasValidationLevel.Basic: return "basic";
                    case FoldCanvasValidationLevel.Standard: return "standard";
                    case FoldCanvasValidationLevel.Strict: return "strict";
                    default:
                        throw new FoldScriptWriteException(
                            FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                            $"Unsupported validation level value {(int)value}.");
                }
            }
        }
    }

    internal sealed class FoldScriptWriteException : Exception
    {
        public FoldScriptWriteException(
            string code,
            string message,
            string panelId = null,
            string operationId = null,
            string seamId = null)
            : base(message)
        {
            Code = code;
            PanelId = panelId;
            OperationId = operationId;
            SeamId = seamId;
        }

        public string Code { get; }

        public string PanelId { get; }

        public string OperationId { get; }

        public string SeamId { get; }
    }
}
