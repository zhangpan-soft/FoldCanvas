using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal sealed class FoldScriptDecoder
    {
        private readonly List<FoldCanvasDiagnostic> diagnostics;

        private FoldScriptDecoder(List<FoldCanvasDiagnostic> target)
        {
            diagnostics = target;
        }

        public static FoldScriptDocument Decode(
            FoldScriptJsonValue root,
            List<FoldCanvasDiagnostic> diagnostics)
        {
            FoldScriptDecoder decoder = new FoldScriptDecoder(diagnostics);
            return decoder.TryDecodeDocument(root, out FoldScriptDocument value)
                ? value
                : null;
        }

        private bool TryDecodeDocument(
            FoldScriptJsonValue value,
            out FoldScriptDocument document)
        {
            document = null;
            if (!TryObject(
                    value,
                    "$",
                    new[]
                    {
                        "schemaVersion", "assetId", "displayName", "units",
                        "canvas", "panels", "seams", "operations", "compile"
                    },
                    new[]
                    {
                        "schemaVersion", "assetId", "displayName", "units",
                        "canvas", "panels", "seams", "operations", "compile",
                        "extensions"
                    },
                    out Dictionary<string, FoldScriptJsonValue> rootObject))
            {
                return false;
            }

            if (!TryString(
                    rootObject["schemaVersion"],
                    "$.schemaVersion",
                    out string schemaVersion))
            {
                return false;
            }

            if (!string.Equals(
                    schemaVersion,
                    FoldCanvasSourceMetadata.CurrentSchemaVersion,
                    StringComparison.Ordinal))
            {
                AddError(
                    FoldCanvasDiagnosticCodes.UnsupportedFoldScriptVersion,
                    $"FoldScript schemaVersion '{schemaVersion}' is not supported; expected '{FoldCanvasSourceMetadata.CurrentSchemaVersion}'.");
                return false;
            }

            if (!TryIdentifier(
                    rootObject["assetId"],
                    "$.assetId",
                    out string assetId) ||
                !TryBoundedString(
                    rootObject["displayName"],
                    "$.displayName",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptDisplayNameLength,
                    out string displayName) ||
                !TryUnit(rootObject["units"], "$.units", out FoldScriptUnit unit) ||
                !TryCanvas(
                    rootObject["canvas"],
                    out FoldScriptCanvas canvas) ||
                !TryPanels(
                    rootObject["panels"],
                    out List<FoldScriptPanel> panels) ||
                !TrySeams(
                    rootObject["seams"],
                    out List<FoldScriptSeam> seams) ||
                !TryOperations(
                    rootObject["operations"],
                    out List<FoldScriptOperation> operations) ||
                !TryCompileSettings(
                    rootObject["compile"],
                    out FoldScriptCompileSettings compile))
            {
                return false;
            }

            string extensionsJson = string.Empty;
            if (rootObject.TryGetValue(
                    "extensions",
                    out FoldScriptJsonValue extensions))
            {
                if (extensions.Kind != FoldScriptJsonKind.Object)
                {
                    AddStructureError(
                        "$.extensions must be a JSON object.");
                    return false;
                }

                extensionsJson = FoldScriptJsonCanonicalWriter.Write(extensions);
            }

            document = new FoldScriptDocument
            {
                SchemaVersion = schemaVersion,
                AssetId = assetId,
                DisplayName = displayName,
                Units = unit,
                Canvas = canvas,
                Compile = compile,
                ExtensionsJson = extensionsJson
            };
            document.Panels.AddRange(panels);
            document.Seams.AddRange(seams);
            document.Operations.AddRange(operations);

            return ValidateIdentityAndReferences(document);
        }

        private bool TryCanvas(
            FoldScriptJsonValue value,
            out FoldScriptCanvas canvas)
        {
            canvas = null;
            if (!TryObject(
                    value,
                    "$.canvas",
                    new[] { "appearance", "width", "height" },
                    new[] { "appearance", "width", "height" },
                    out Dictionary<string, FoldScriptJsonValue> fields) ||
                !TryBoundedString(
                    fields["appearance"],
                    "$.canvas.appearance",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptAppearancePathLength,
                    out string appearance) ||
                !TryInteger(
                    fields["width"],
                    "$.canvas.width",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptCanvasDimension,
                    out int width) ||
                !TryInteger(
                    fields["height"],
                    "$.canvas.height",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptCanvasDimension,
                    out int height))
            {
                return false;
            }

            canvas = new FoldScriptCanvas
            {
                Appearance = appearance,
                Width = width,
                Height = height
            };
            return true;
        }

        private bool TryPanels(
            FoldScriptJsonValue value,
            out List<FoldScriptPanel> panels)
        {
            panels = null;
            if (!TryArray(
                    value,
                    "$.panels",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptPanels,
                    out List<FoldScriptJsonValue> values))
            {
                return false;
            }

            panels = new List<FoldScriptPanel>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                string path = $"$.panels[{i}]";
                if (!TryObject(
                        values[i],
                        path,
                        new[]
                        {
                            "id", "shape", "canvasRect", "physicalSize",
                            "tessellation"
                        },
                        new[]
                        {
                            "id", "shape", "canvasRect", "physicalSize",
                            "tessellation"
                        },
                        out Dictionary<string, FoldScriptJsonValue> fields) ||
                    !TryIdentifier(
                        fields["id"],
                        path + ".id",
                        out string id) ||
                    !TryString(
                        fields["shape"],
                        path + ".shape",
                        out string shapeText) ||
                    !TryRect(
                        fields["canvasRect"],
                        path + ".canvasRect",
                        out Rect canvasRect) ||
                    !TryPositiveVector2(
                        fields["physicalSize"],
                        path + ".physicalSize",
                        out Vector2 physicalSize))
                {
                    return false;
                }

                PanelShape shape;
                if (string.Equals(
                        shapeText,
                        "rectangle",
                        StringComparison.Ordinal))
                {
                    shape = PanelShape.Rectangle;
                }
                else if (string.Equals(
                    shapeText,
                    "disk",
                    StringComparison.Ordinal))
                {
                    shape = PanelShape.Disk;
                }
                else
                {
                    AddStructureError(
                        $"{path}.shape must be 'rectangle' or 'disk'.",
                        panelId: id);
                    return false;
                }

                FoldScriptPanel panel = new FoldScriptPanel
                {
                    Id = id,
                    Shape = shape,
                    CanvasRect = canvasRect,
                    PhysicalSize = physicalSize
                };

                if (!TryPanelTessellation(
                        fields["tessellation"],
                        path + ".tessellation",
                        panel))
                {
                    return false;
                }

                panels.Add(panel);
            }

            return true;
        }

        private bool TryPanelTessellation(
            FoldScriptJsonValue value,
            string path,
            FoldScriptPanel panel)
        {
            if (panel.Shape == PanelShape.Rectangle)
            {
                if (!TryObject(
                        value,
                        path,
                        new[] { "uSegments", "vSegments" },
                        new[] { "uSegments", "vSegments" },
                        out Dictionary<string, FoldScriptJsonValue> fields) ||
                    !TryInteger(
                        fields["uSegments"],
                        path + ".uSegments",
                        1,
                        2048,
                        out int uSegments) ||
                    !TryInteger(
                        fields["vSegments"],
                        path + ".vSegments",
                        1,
                        2048,
                        out int vSegments))
                {
                    return false;
                }

                panel.USegments = uSegments;
                panel.VSegments = vSegments;
                return true;
            }

            if (!TryObject(
                    value,
                    path,
                    new[] { "radialSegments", "radialRings" },
                    new[] { "radialSegments", "radialRings" },
                    out Dictionary<string, FoldScriptJsonValue> diskFields) ||
                !TryInteger(
                    diskFields["radialSegments"],
                    path + ".radialSegments",
                    3,
                    4096,
                    out int radialSegments) ||
                !TryInteger(
                    diskFields["radialRings"],
                    path + ".radialRings",
                    1,
                    2048,
                    out int radialRings))
            {
                return false;
            }

            panel.RadialSegments = radialSegments;
            panel.RadialRings = radialRings;
            return true;
        }

        private bool TrySeams(
            FoldScriptJsonValue value,
            out List<FoldScriptSeam> seams)
        {
            seams = null;
            if (!TryArray(
                    value,
                    "$.seams",
                    0,
                    FoldCanvasLimits.MaximumFoldScriptSeams,
                    out List<FoldScriptJsonValue> values))
            {
                return false;
            }

            seams = new List<FoldScriptSeam>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                string path = $"$.seams[{i}]";
                if (!TryObject(
                        values[i],
                        path,
                        new[]
                        {
                            "id", "a", "b", "mode", "reverseB",
                            "sampleCount"
                        },
                        new[]
                        {
                            "id", "a", "b", "mode", "reverseB",
                            "sampleCount"
                        },
                        out Dictionary<string, FoldScriptJsonValue> fields) ||
                    !TryIdentifier(
                        fields["id"],
                        path + ".id",
                        out string id) ||
                    !TryBoundaryReference(
                        fields["a"],
                        path + ".a",
                        out FoldScriptBoundaryReference a) ||
                    !TryBoundaryReference(
                        fields["b"],
                        path + ".b",
                        out FoldScriptBoundaryReference b) ||
                    !TrySeamMode(
                        fields["mode"],
                        path + ".mode",
                        out SeamMode mode) ||
                    !TryBoolean(
                        fields["reverseB"],
                        path + ".reverseB",
                        out bool reverseB) ||
                    !TryInteger(
                        fields["sampleCount"],
                        path + ".sampleCount",
                        FoldCanvasLimits.MinimumStitchSampleCount,
                        FoldCanvasLimits.MaximumStitchSampleCount,
                        out int sampleCount))
                {
                    return false;
                }

                seams.Add(new FoldScriptSeam
                {
                    Id = id,
                    A = a,
                    B = b,
                    Mode = mode,
                    ReverseB = reverseB,
                    SampleCount = sampleCount
                });
            }

            return true;
        }

        private bool TryBoundaryReference(
            FoldScriptJsonValue value,
            string path,
            out FoldScriptBoundaryReference reference)
        {
            reference = null;
            if (!TryObject(
                    value,
                    path,
                    new[] { "panel", "boundary" },
                    new[] { "panel", "boundary" },
                    out Dictionary<string, FoldScriptJsonValue> fields) ||
                !TryIdentifier(
                    fields["panel"],
                    path + ".panel",
                    out string panelId) ||
                !TryIdentifier(
                    fields["boundary"],
                    path + ".boundary",
                    out string boundaryId))
            {
                return false;
            }

            reference = new FoldScriptBoundaryReference
            {
                PanelId = panelId,
                BoundaryId = boundaryId
            };
            return true;
        }

        private bool TryOperations(
            FoldScriptJsonValue value,
            out List<FoldScriptOperation> operations)
        {
            operations = null;
            if (!TryArray(
                    value,
                    "$.operations",
                    0,
                    FoldCanvasLimits.MaximumFoldScriptOperations,
                    out List<FoldScriptJsonValue> values))
            {
                return false;
            }

            operations = new List<FoldScriptOperation>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                string path = $"$.operations[{i}]";
                if (values[i].Kind != FoldScriptJsonKind.Object ||
                    !values[i].ObjectValue.TryGetValue(
                        "type",
                        out FoldScriptJsonValue typeValue) ||
                    !TryString(typeValue, path + ".type", out string type))
                {
                    if (diagnostics.Count == 0)
                    {
                        AddStructureError(
                            $"{path} must contain a string 'type' property.");
                    }

                    return false;
                }

                FoldScriptOperation operation;
                switch (type)
                {
                    case "rigidTransform":
                        operation = DecodeRigidTransform(values[i], path);
                        break;
                    case "fold":
                        operation = DecodeFold(values[i], path);
                        break;
                    case "roll":
                        operation = DecodeRoll(values[i], path);
                        break;
                    case "sphericalWrap":
                        operation = DecodeSphericalWrap(values[i], path);
                        break;
                    case "stitch":
                        operation = DecodeStitch(values[i], path);
                        break;
                    case "solidify":
                        operation = DecodeSolidify(values[i], path);
                        break;
                    default:
                        AddError(
                            FoldCanvasDiagnosticCodes.UnknownFoldScriptOperation,
                            $"{path}.type '{type}' is not a supported FoldScript 0.1 operation.",
                            values: new[]
                            {
                                new FoldCanvasDiagnosticValue(
                                    "operationIndex",
                                    i)
                            });
                        return false;
                }

                if (operation == null)
                {
                    return false;
                }

                operations.Add(operation);
            }

            return true;
        }

        private FoldScriptRigidTransformOperation DecodeRigidTransform(
            FoldScriptJsonValue value,
            string path)
        {
            if (!TryOperationObject(
                    value,
                    path,
                    new[]
                    {
                        "id", "type", "panel", "translation",
                        "rotationEuler", "scale"
                    },
                    new[]
                    {
                        "id", "enabled", "type", "panel", "translation",
                        "rotationEuler", "scale"
                    },
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string id,
                    out bool enabled) ||
                !TryIdentifier(
                    fields["panel"],
                    path + ".panel",
                    out string panelId) ||
                !TryVector3(
                    fields["translation"],
                    path + ".translation",
                    -1000000000d,
                    1000000000d,
                    out Vector3 translation) ||
                !TryVector3(
                    fields["rotationEuler"],
                    path + ".rotationEuler",
                    -1000000000d,
                    1000000000d,
                    out Vector3 rotation) ||
                !TryVector3(
                    fields["scale"],
                    path + ".scale",
                    -1000000000d,
                    1000000000d,
                    out Vector3 scale))
            {
                return null;
            }

            return new FoldScriptRigidTransformOperation
            {
                Id = id,
                Enabled = enabled,
                PanelId = panelId,
                Translation = translation,
                RotationEuler = rotation,
                Scale = scale
            };
        }

        private FoldScriptFoldOperation DecodeFold(
            FoldScriptJsonValue value,
            string path)
        {
            if (!TryOperationObject(
                    value,
                    path,
                    new[]
                    {
                        "id", "type", "panel", "line", "side",
                        "angleDegrees", "falloff"
                    },
                    new[]
                    {
                        "id", "enabled", "type", "panel", "line", "side",
                        "angleDegrees", "falloff"
                    },
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string id,
                    out bool enabled) ||
                !TryIdentifier(
                    fields["panel"],
                    path + ".panel",
                    out string panelId) ||
                !TryArray(
                    fields["line"],
                    path + ".line",
                    2,
                    2,
                    out List<FoldScriptJsonValue> line) ||
                !TryVector2(
                    line[0],
                    path + ".line[0]",
                    0d,
                    1d,
                    out Vector2 lineStart) ||
                !TryVector2(
                    line[1],
                    path + ".line[1]",
                    0d,
                    1d,
                    out Vector2 lineEnd) ||
                !TryFoldSide(
                    fields["side"],
                    path + ".side",
                    out FoldSide side) ||
                !TryFloat(
                    fields["angleDegrees"],
                    path + ".angleDegrees",
                    -3600d,
                    3600d,
                    out float angle) ||
                !TryFloat(
                    fields["falloff"],
                    path + ".falloff",
                    0d,
                    1d,
                    out float falloff))
            {
                return null;
            }

            return new FoldScriptFoldOperation
            {
                Id = id,
                Enabled = enabled,
                PanelId = panelId,
                LineStart = lineStart,
                LineEnd = lineEnd,
                Side = side,
                AngleDegrees = angle,
                Falloff = falloff
            };
        }

        private FoldScriptRollOperation DecodeRoll(
            FoldScriptJsonValue value,
            string path)
        {
            if (!TryOperationObject(
                    value,
                    path,
                    new[]
                    {
                        "id", "type", "panel", "direction", "angleDegrees",
                        "radiusMode", "startAngleDegrees"
                    },
                    new[]
                    {
                        "id", "enabled", "type", "panel", "direction",
                        "angleDegrees", "radiusMode", "radius", "targetSeam",
                        "startAngleDegrees"
                    },
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string id,
                    out bool enabled) ||
                !TryIdentifier(
                    fields["panel"],
                    path + ".panel",
                    out string panelId) ||
                !TryRollDirection(
                    fields["direction"],
                    path + ".direction",
                    out RollDirection direction) ||
                !TryFloat(
                    fields["angleDegrees"],
                    path + ".angleDegrees",
                    -360d,
                    360d,
                    out float angle) ||
                !TryRollRadiusMode(
                    fields["radiusMode"],
                    path + ".radiusMode",
                    out RollRadiusMode radiusMode) ||
                !TryFloat(
                    fields["startAngleDegrees"],
                    path + ".startAngleDegrees",
                    -3600d,
                    3600d,
                    out float startAngle))
            {
                return null;
            }

            float radius = 0.05f;
            string targetSeam = string.Empty;
            if (radiusMode == RollRadiusMode.Explicit)
            {
                if (!fields.TryGetValue("radius", out FoldScriptJsonValue radiusValue))
                {
                    AddStructureError(
                        $"{path}.radius is required for explicit radiusMode.",
                        operationId: id);
                    return null;
                }

                if (!TryPositiveFloat(
                        radiusValue,
                        path + ".radius",
                        1000000d,
                        out radius))
                {
                    return null;
                }
            }
            else if (fields.TryGetValue(
                "radius",
                out FoldScriptJsonValue optionalRadius) &&
                !TryPositiveFloat(
                    optionalRadius,
                    path + ".radius",
                    1000000d,
                    out radius))
            {
                return null;
            }

            if (radiusMode == RollRadiusMode.FitTargetBoundary)
            {
                if (!fields.TryGetValue(
                        "targetSeam",
                        out FoldScriptJsonValue targetValue))
                {
                    AddStructureError(
                        $"{path}.targetSeam is required for fitTargetBoundary radiusMode.",
                        operationId: id);
                    return null;
                }

                if (!TryIdentifier(
                        targetValue,
                        path + ".targetSeam",
                        out targetSeam))
                {
                    return null;
                }
            }
            else if (fields.TryGetValue(
                "targetSeam",
                out FoldScriptJsonValue optionalTarget) &&
                !TryIdentifier(
                    optionalTarget,
                    path + ".targetSeam",
                    out targetSeam))
            {
                return null;
            }

            return new FoldScriptRollOperation
            {
                Id = id,
                Enabled = enabled,
                PanelId = panelId,
                Direction = direction,
                AngleDegrees = angle,
                RadiusMode = radiusMode,
                ExplicitRadius = radius,
                TargetSeamId = targetSeam,
                StartAngleDegrees = startAngle
            };
        }

        private FoldScriptSphericalWrapOperation DecodeSphericalWrap(
            FoldScriptJsonValue value,
            string path)
        {
            if (!TryOperationObject(
                    value,
                    path,
                    new[]
                    {
                        "id", "type", "panel", "radius", "latitudeRange",
                        "longitudeRange", "wrapDirection", "poleMode",
                        "subdivisionMode"
                    },
                    new[]
                    {
                        "id", "enabled", "type", "panel", "radius",
                        "latitudeRange", "longitudeRange", "wrapDirection",
                        "poleMode", "subdivisionMode"
                    },
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string id,
                    out bool enabled) ||
                !TryIdentifier(
                    fields["panel"],
                    path + ".panel",
                    out string panelId) ||
                !TryPositiveFloat(
                    fields["radius"],
                    path + ".radius",
                    1000000000d,
                    out float radius) ||
                !TryVector2(
                    fields["latitudeRange"],
                    path + ".latitudeRange",
                    -90d,
                    90d,
                    out Vector2 latitude) ||
                !TryVector2(
                    fields["longitudeRange"],
                    path + ".longitudeRange",
                    -360d,
                    360d,
                    out Vector2 longitude) ||
                !TrySphericalWrapDirection(
                    fields["wrapDirection"],
                    path + ".wrapDirection",
                    out SphericalWrapDirection wrapDirection) ||
                !TrySphericalPoleMode(
                    fields["poleMode"],
                    path + ".poleMode",
                    out SphericalPoleMode poleMode) ||
                !TrySphericalSubdivisionMode(
                    fields["subdivisionMode"],
                    path + ".subdivisionMode",
                    out SphericalSubdivisionMode subdivisionMode))
            {
                return null;
            }

            return new FoldScriptSphericalWrapOperation
            {
                Id = id,
                Enabled = enabled,
                PanelId = panelId,
                Radius = radius,
                LatitudeRange = latitude,
                LongitudeRange = longitude,
                WrapDirection = wrapDirection,
                PoleMode = poleMode,
                SubdivisionMode = subdivisionMode
            };
        }

        private FoldScriptStitchOperation DecodeStitch(
            FoldScriptJsonValue value,
            string path)
        {
            if (!TryOperationObject(
                    value,
                    path,
                    new[] { "id", "type", "seams" },
                    new[] { "id", "enabled", "type", "seams" },
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string id,
                    out bool enabled) ||
                !TryIdentifierArray(
                    fields["seams"],
                    path + ".seams",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptSeams,
                    out List<string> seamIds))
            {
                return null;
            }

            FoldScriptStitchOperation operation =
                new FoldScriptStitchOperation
                {
                    Id = id,
                    Enabled = enabled
                };
            operation.SeamIds.AddRange(seamIds);
            return operation;
        }

        private FoldScriptSolidifyOperation DecodeSolidify(
            FoldScriptJsonValue value,
            string path)
        {
            if (!TryOperationObject(
                    value,
                    path,
                    new[]
                    {
                        "id", "type", "targets", "thickness", "direction"
                    },
                    new[]
                    {
                        "id", "enabled", "type", "targets", "thickness",
                        "direction"
                    },
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string id,
                    out bool enabled) ||
                !TryIdentifierArray(
                    fields["targets"],
                    path + ".targets",
                    1,
                    FoldCanvasLimits.MaximumFoldScriptPanels,
                    out List<string> panelIds) ||
                !TryPositiveFloat(
                    fields["thickness"],
                    path + ".thickness",
                    1000000000d,
                    out float thickness) ||
                !TrySolidifyDirection(
                    fields["direction"],
                    path + ".direction",
                    out SolidifyDirection direction))
            {
                return null;
            }

            FoldScriptSolidifyOperation operation =
                new FoldScriptSolidifyOperation
                {
                    Id = id,
                    Enabled = enabled,
                    Thickness = thickness,
                    Direction = direction
                };
            operation.PanelIds.AddRange(panelIds);
            return operation;
        }

        private bool TryOperationObject(
            FoldScriptJsonValue value,
            string path,
            string[] required,
            string[] allowed,
            out Dictionary<string, FoldScriptJsonValue> fields,
            out string id,
            out bool enabled)
        {
            fields = null;
            id = null;
            enabled = true;
            if (!TryObject(value, path, required, allowed, out fields) ||
                !TryIdentifier(fields["id"], path + ".id", out id))
            {
                return false;
            }

            if (fields.TryGetValue(
                    "enabled",
                    out FoldScriptJsonValue enabledValue) &&
                !TryBoolean(
                    enabledValue,
                    path + ".enabled",
                    out enabled))
            {
                return false;
            }

            return true;
        }

        private bool TryCompileSettings(
            FoldScriptJsonValue value,
            out FoldScriptCompileSettings settings)
        {
            settings = null;
            if (!TryObject(
                    value,
                    "$.compile",
                    new[]
                    {
                        "weldEpsilon", "recalculateNormals", "validationLevel"
                    },
                    new[]
                    {
                        "weldEpsilon", "recalculateNormals", "validationLevel",
                        "maxGeneratedVertices", "maxGeneratedTriangles"
                    },
                    out Dictionary<string, FoldScriptJsonValue> fields) ||
                !TryPositiveFloat(
                    fields["weldEpsilon"],
                    "$.compile.weldEpsilon",
                    1d,
                    out float weldEpsilon) ||
                !TryBoolean(
                    fields["recalculateNormals"],
                    "$.compile.recalculateNormals",
                    out bool recalculateNormals) ||
                !TryValidationLevel(
                    fields["validationLevel"],
                    "$.compile.validationLevel",
                    out FoldCanvasValidationLevel validationLevel))
            {
                return false;
            }

            int maxVertices =
                FoldCanvasCompileSettings.DefaultMaxGeneratedVertices;
            int maxTriangles =
                FoldCanvasCompileSettings.DefaultMaxGeneratedTriangles;
            if (fields.TryGetValue(
                    "maxGeneratedVertices",
                    out FoldScriptJsonValue maxVerticesValue) &&
                !TryInteger(
                    maxVerticesValue,
                    "$.compile.maxGeneratedVertices",
                    1,
                    int.MaxValue,
                    out maxVertices))
            {
                return false;
            }

            if (fields.TryGetValue(
                    "maxGeneratedTriangles",
                    out FoldScriptJsonValue maxTrianglesValue) &&
                !TryInteger(
                    maxTrianglesValue,
                    "$.compile.maxGeneratedTriangles",
                    1,
                    int.MaxValue,
                    out maxTriangles))
            {
                return false;
            }

            settings = new FoldScriptCompileSettings
            {
                WeldEpsilon = weldEpsilon,
                RecalculateNormals = recalculateNormals,
                ValidationLevel = validationLevel,
                MaxGeneratedVertices = maxVertices,
                MaxGeneratedTriangles = maxTriangles
            };
            return true;
        }

        private bool ValidateIdentityAndReferences(FoldScriptDocument document)
        {
            Dictionary<string, PanelShape> panels =
                new Dictionary<string, PanelShape>(StringComparer.Ordinal);
            for (int i = 0; i < document.Panels.Count; i++)
            {
                FoldScriptPanel panel = document.Panels[i];
                if (panels.ContainsKey(panel.Id))
                {
                    AddError(
                        FoldCanvasDiagnosticCodes.DuplicateFoldScriptIdentifier,
                        $"FoldScript panel ID '{panel.Id}' is duplicated.",
                        panelId: panel.Id);
                    return false;
                }

                panels.Add(panel.Id, panel.Shape);
            }

            HashSet<string> seams = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < document.Seams.Count; i++)
            {
                FoldScriptSeam seam = document.Seams[i];
                if (!seams.Add(seam.Id))
                {
                    AddError(
                        FoldCanvasDiagnosticCodes.DuplicateFoldScriptIdentifier,
                        $"FoldScript seam ID '{seam.Id}' is duplicated.",
                        seamId: seam.Id);
                    return false;
                }

                if (!ValidateBoundaryReference(seam.A, panels, seam.Id, "a") ||
                    !ValidateBoundaryReference(seam.B, panels, seam.Id, "b"))
                {
                    return false;
                }
            }

            HashSet<string> operations =
                new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < document.Operations.Count; i++)
            {
                FoldScriptOperation operation = document.Operations[i];
                if (!operations.Add(operation.Id))
                {
                    AddError(
                        FoldCanvasDiagnosticCodes.DuplicateFoldScriptIdentifier,
                        $"FoldScript operation ID '{operation.Id}' is duplicated.",
                        operationId: operation.Id);
                    return false;
                }

                if (!ValidateOperationReferences(
                        operation,
                        panels,
                        seams))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateBoundaryReference(
            FoldScriptBoundaryReference reference,
            Dictionary<string, PanelShape> panels,
            string seamId,
            string endpoint)
        {
            if (!panels.TryGetValue(reference.PanelId, out PanelShape shape))
            {
                AddError(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptReference,
                    $"Seam '{seamId}' endpoint {endpoint} references missing panel '{reference.PanelId}'.",
                    panelId: reference.PanelId,
                    seamId: seamId,
                    boundaryId: reference.BoundaryId);
                return false;
            }

            bool boundaryExists = shape == PanelShape.Rectangle
                ? reference.BoundaryId == "uMin" ||
                  reference.BoundaryId == "uMax" ||
                  reference.BoundaryId == "vMin" ||
                  reference.BoundaryId == "vMax"
                : reference.BoundaryId == "perimeter";
            if (!boundaryExists)
            {
                AddError(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptReference,
                    $"Seam '{seamId}' endpoint {endpoint} references boundary '{reference.BoundaryId}' that does not exist on panel '{reference.PanelId}'.",
                    panelId: reference.PanelId,
                    seamId: seamId,
                    boundaryId: reference.BoundaryId);
                return false;
            }

            return true;
        }

        private bool ValidateOperationReferences(
            FoldScriptOperation operation,
            Dictionary<string, PanelShape> panels,
            HashSet<string> seams)
        {
            string panelId = null;
            if (operation is FoldScriptRigidTransformOperation rigid)
            {
                panelId = rigid.PanelId;
            }
            else if (operation is FoldScriptFoldOperation fold)
            {
                panelId = fold.PanelId;
            }
            else if (operation is FoldScriptRollOperation roll)
            {
                panelId = roll.PanelId;
                if (!string.IsNullOrEmpty(roll.TargetSeamId) &&
                    !seams.Contains(roll.TargetSeamId))
                {
                    AddMissingSeam(operation.Id, roll.TargetSeamId);
                    return false;
                }
            }
            else if (operation is FoldScriptSphericalWrapOperation sphere)
            {
                panelId = sphere.PanelId;
            }
            else if (operation is FoldScriptStitchOperation stitch)
            {
                for (int i = 0; i < stitch.SeamIds.Count; i++)
                {
                    if (!seams.Contains(stitch.SeamIds[i]))
                    {
                        AddMissingSeam(operation.Id, stitch.SeamIds[i]);
                        return false;
                    }
                }
            }
            else if (operation is FoldScriptSolidifyOperation solidify)
            {
                for (int i = 0; i < solidify.PanelIds.Count; i++)
                {
                    if (!panels.ContainsKey(solidify.PanelIds[i]))
                    {
                        AddMissingPanel(operation.Id, solidify.PanelIds[i]);
                        return false;
                    }
                }
            }

            if (panelId != null && !panels.ContainsKey(panelId))
            {
                AddMissingPanel(operation.Id, panelId);
                return false;
            }

            return true;
        }

        private void AddMissingPanel(string operationId, string panelId)
        {
            AddError(
                FoldCanvasDiagnosticCodes.InvalidFoldScriptReference,
                $"Operation '{operationId}' references missing panel '{panelId}'.",
                panelId: panelId,
                operationId: operationId);
        }

        private void AddMissingSeam(string operationId, string seamId)
        {
            AddError(
                FoldCanvasDiagnosticCodes.InvalidFoldScriptReference,
                $"Operation '{operationId}' references missing seam '{seamId}'.",
                operationId: operationId,
                seamId: seamId);
        }

        private bool TryObject(
            FoldScriptJsonValue value,
            string path,
            string[] required,
            string[] allowed,
            out Dictionary<string, FoldScriptJsonValue> fields)
        {
            fields = null;
            if (value == null || value.Kind != FoldScriptJsonKind.Object)
            {
                AddStructureError($"{path} must be a JSON object.");
                return false;
            }

            fields = value.ObjectValue;
            for (int i = 0; i < required.Length; i++)
            {
                if (!fields.ContainsKey(required[i]))
                {
                    AddStructureError(
                        $"{path} is missing required property '{required[i]}'.");
                    return false;
                }
            }

            HashSet<string> allowedSet =
                new HashSet<string>(allowed, StringComparer.Ordinal);
            List<string> names = new List<string>(fields.Keys);
            names.Sort(StringComparer.Ordinal);
            for (int i = 0; i < names.Count; i++)
            {
                if (!allowedSet.Contains(names[i]))
                {
                    AddStructureError(
                        $"{path} contains unsupported property '{names[i]}'.");
                    return false;
                }
            }

            return true;
        }

        private bool TryArray(
            FoldScriptJsonValue value,
            string path,
            int minimumCount,
            int maximumCount,
            out List<FoldScriptJsonValue> values)
        {
            values = null;
            if (value == null || value.Kind != FoldScriptJsonKind.Array)
            {
                AddStructureError($"{path} must be a JSON array.");
                return false;
            }

            if (value.ArrayValue.Count < minimumCount ||
                value.ArrayValue.Count > maximumCount)
            {
                string code = value.ArrayValue.Count > maximumCount
                    ? FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded
                    : FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure;
                AddError(
                    code,
                    $"{path} must contain between {minimumCount} and {maximumCount} items.",
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "itemCount",
                            value.ArrayValue.Count),
                        new FoldCanvasDiagnosticValue(
                            "maximumItemCount",
                            maximumCount)
                    });
                return false;
            }

            values = value.ArrayValue;
            return true;
        }

        private bool TryString(
            FoldScriptJsonValue value,
            string path,
            out string result)
        {
            result = null;
            if (value == null || value.Kind != FoldScriptJsonKind.String)
            {
                AddStructureError($"{path} must be a JSON string.");
                return false;
            }

            result = value.StringValue;
            return true;
        }

        private bool TryBoundedString(
            FoldScriptJsonValue value,
            string path,
            int minimumLength,
            int maximumLength,
            out string result)
        {
            if (!TryString(value, path, out result))
            {
                return false;
            }

            if (result.Length < minimumLength || result.Length > maximumLength)
            {
                AddStructureError(
                    $"{path} length must be between {minimumLength} and {maximumLength} characters.");
                return false;
            }

            return true;
        }

        private bool TryIdentifier(
            FoldScriptJsonValue value,
            string path,
            out string result)
        {
            if (!TryBoundedString(
                    value,
                    path,
                    1,
                    FoldCanvasLimits.MaximumFoldScriptIdentifierLength,
                    out result))
            {
                return false;
            }

            if (!FoldScriptIdentifier.IsValid(result))
            {
                AddStructureError(
                    $"{path} must match [A-Za-z][A-Za-z0-9_.-]{{0,63}}.");
                return false;
            }

            return true;
        }

        private bool TryBoolean(
            FoldScriptJsonValue value,
            string path,
            out bool result)
        {
            result = false;
            if (value == null || value.Kind != FoldScriptJsonKind.Boolean)
            {
                AddStructureError($"{path} must be a JSON boolean.");
                return false;
            }

            result = value.BooleanValue;
            return true;
        }

        private bool TryInteger(
            FoldScriptJsonValue value,
            string path,
            int minimum,
            int maximum,
            out int result)
        {
            result = 0;
            if (!TryNumber(value, path, out double number) ||
                number < minimum || number > maximum ||
                Math.Truncate(number) != number)
            {
                if (diagnostics.Count == 0)
                {
                    AddStructureError(
                        $"{path} must be an integer between {minimum} and {maximum}.");
                }

                return false;
            }

            result = (int)number;
            return true;
        }

        private bool TryNumber(
            FoldScriptJsonValue value,
            string path,
            out double result)
        {
            result = 0d;
            if (value == null || value.Kind != FoldScriptJsonKind.Number)
            {
                AddStructureError($"{path} must be a JSON number.");
                return false;
            }

            result = value.NumberValue;
            return true;
        }

        private bool TryFloat(
            FoldScriptJsonValue value,
            string path,
            double minimum,
            double maximum,
            out float result)
        {
            result = 0f;
            if (!TryNumber(value, path, out double number))
            {
                return false;
            }

            if (number < minimum || number > maximum)
            {
                AddStructureError(
                    $"{path} must be between {minimum} and {maximum}.");
                return false;
            }

            result = (float)number;
            if (float.IsNaN(result) || float.IsInfinity(result))
            {
                AddError(
                    FoldCanvasDiagnosticCodes.NonFiniteFoldScriptNumber,
                    $"{path} cannot be represented as a finite native number.");
                return false;
            }

            return true;
        }

        private bool TryPositiveFloat(
            FoldScriptJsonValue value,
            string path,
            double maximum,
            out float result)
        {
            if (!TryFloat(value, path, 0d, maximum, out result))
            {
                return false;
            }

            if (result <= 0f)
            {
                AddStructureError(
                    $"{path} must be greater than zero and at most {maximum}.");
                return false;
            }

            return true;
        }

        private bool TryVector2(
            FoldScriptJsonValue value,
            string path,
            double minimum,
            double maximum,
            out Vector2 result)
        {
            result = default;
            if (!TryArray(value, path, 2, 2, out List<FoldScriptJsonValue> values) ||
                !TryFloat(values[0], path + "[0]", minimum, maximum, out float x) ||
                !TryFloat(values[1], path + "[1]", minimum, maximum, out float y))
            {
                return false;
            }

            result = new Vector2(x, y);
            return true;
        }

        private bool TryPositiveVector2(
            FoldScriptJsonValue value,
            string path,
            out Vector2 result)
        {
            result = default;
            if (!TryArray(value, path, 2, 2, out List<FoldScriptJsonValue> values) ||
                !TryPositiveFloat(
                    values[0],
                    path + "[0]",
                    1000000000d,
                    out float x) ||
                !TryPositiveFloat(
                    values[1],
                    path + "[1]",
                    1000000000d,
                    out float y))
            {
                return false;
            }

            result = new Vector2(x, y);
            return true;
        }

        private bool TryVector3(
            FoldScriptJsonValue value,
            string path,
            double minimum,
            double maximum,
            out Vector3 result)
        {
            result = default;
            if (!TryArray(value, path, 3, 3, out List<FoldScriptJsonValue> values) ||
                !TryFloat(values[0], path + "[0]", minimum, maximum, out float x) ||
                !TryFloat(values[1], path + "[1]", minimum, maximum, out float y) ||
                !TryFloat(values[2], path + "[2]", minimum, maximum, out float z))
            {
                return false;
            }

            result = new Vector3(x, y, z);
            return true;
        }

        private bool TryRect(
            FoldScriptJsonValue value,
            string path,
            out Rect result)
        {
            result = default;
            if (!TryArray(value, path, 4, 4, out List<FoldScriptJsonValue> values) ||
                !TryFloat(values[0], path + "[0]", 0d, 1d, out float x) ||
                !TryFloat(values[1], path + "[1]", 0d, 1d, out float y) ||
                !TryPositiveFloat(values[2], path + "[2]", 1d, out float width) ||
                !TryPositiveFloat(values[3], path + "[3]", 1d, out float height))
            {
                return false;
            }

            if (x + width > 1f + 1e-6f || y + height > 1f + 1e-6f)
            {
                AddStructureError(
                    $"{path} must remain inside normalized canvas space.");
                return false;
            }

            result = new Rect(x, y, width, height);
            return true;
        }

        private bool TryIdentifierArray(
            FoldScriptJsonValue value,
            string path,
            int minimum,
            int maximum,
            out List<string> identifiers)
        {
            identifiers = null;
            if (!TryArray(value, path, minimum, maximum, out List<FoldScriptJsonValue> values))
            {
                return false;
            }

            identifiers = new List<string>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                if (!TryIdentifier(
                        values[i],
                        $"{path}[{i}]",
                        out string identifier))
                {
                    return false;
                }

                identifiers.Add(identifier);
            }

            return true;
        }

        private bool TryUnit(
            FoldScriptJsonValue value,
            string path,
            out FoldScriptUnit result)
        {
            result = default;
            if (!TryString(value, path, out string text))
            {
                return false;
            }

            switch (text)
            {
                case "meter": result = FoldScriptUnit.Meter; return true;
                case "centimeter": result = FoldScriptUnit.Centimeter; return true;
                case "millimeter": result = FoldScriptUnit.Millimeter; return true;
                default:
                    AddStructureError(
                        $"{path} must be meter, centimeter, or millimeter.");
                    return false;
            }
        }

        private bool TrySeamMode(
            FoldScriptJsonValue value,
            string path,
            out SeamMode result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            switch (text)
            {
                case "weld": result = SeamMode.Weld; return true;
                case "hinge": result = SeamMode.Hinge; return true;
                case "keepOpen": result = SeamMode.KeepOpen; return true;
                case "bridge": result = SeamMode.Bridge; return true;
                default: AddStructureError($"{path} has an unsupported seam mode."); return false;
            }
        }

        private bool TryFoldSide(FoldScriptJsonValue value, string path, out FoldSide result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            if (text == "positive") { result = FoldSide.Positive; return true; }
            if (text == "negative") { result = FoldSide.Negative; return true; }
            AddStructureError($"{path} must be positive or negative.");
            return false;
        }

        private bool TryRollDirection(FoldScriptJsonValue value, string path, out RollDirection result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            if (text == "u") { result = RollDirection.U; return true; }
            if (text == "v") { result = RollDirection.V; return true; }
            AddStructureError($"{path} must be u or v.");
            return false;
        }

        private bool TryRollRadiusMode(FoldScriptJsonValue value, string path, out RollRadiusMode result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            switch (text)
            {
                case "preserveArcLength": result = RollRadiusMode.PreserveArcLength; return true;
                case "explicit": result = RollRadiusMode.Explicit; return true;
                case "fitTargetBoundary": result = RollRadiusMode.FitTargetBoundary; return true;
                default: AddStructureError($"{path} has an unsupported radius mode."); return false;
            }
        }

        private bool TrySphericalWrapDirection(FoldScriptJsonValue value, string path, out SphericalWrapDirection result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            if (text == "longitudeAlongU") { result = SphericalWrapDirection.LongitudeAlongU; return true; }
            if (text == "longitudeAlongV") { result = SphericalWrapDirection.LongitudeAlongV; return true; }
            AddStructureError($"{path} has an unsupported spherical wrap direction.");
            return false;
        }

        private bool TrySphericalPoleMode(FoldScriptJsonValue value, string path, out SphericalPoleMode result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            if (text == "merge") { result = SphericalPoleMode.Merge; return true; }
            if (text == "keepFan") { result = SphericalPoleMode.KeepFan; return true; }
            AddStructureError($"{path} has an unsupported spherical pole mode.");
            return false;
        }

        private bool TrySphericalSubdivisionMode(FoldScriptJsonValue value, string path, out SphericalSubdivisionMode result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            if (text == "panelGrid") { result = SphericalSubdivisionMode.PanelGrid; return true; }
            AddStructureError($"{path} must be panelGrid in FoldScript 0.1.");
            return false;
        }

        private bool TrySolidifyDirection(FoldScriptJsonValue value, string path, out SolidifyDirection result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            switch (text)
            {
                case "inward": result = SolidifyDirection.Inward; return true;
                case "outward": result = SolidifyDirection.Outward; return true;
                case "centered": result = SolidifyDirection.Centered; return true;
                default: AddStructureError($"{path} has an unsupported solidify direction."); return false;
            }
        }

        private bool TryValidationLevel(FoldScriptJsonValue value, string path, out FoldCanvasValidationLevel result)
        {
            result = default;
            if (!TryString(value, path, out string text)) return false;
            switch (text)
            {
                case "basic": result = FoldCanvasValidationLevel.Basic; return true;
                case "standard": result = FoldCanvasValidationLevel.Standard; return true;
                case "strict": result = FoldCanvasValidationLevel.Strict; return true;
                default: AddStructureError($"{path} has an unsupported validation level."); return false;
            }
        }

        private void AddStructureError(
            string message,
            string panelId = null,
            string operationId = null,
            string seamId = null)
        {
            AddError(
                FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                message,
                panelId,
                operationId,
                seamId);
        }

        private void AddError(
            string code,
            string message,
            string panelId = null,
            string operationId = null,
            string seamId = null,
            string boundaryId = null,
            IEnumerable<FoldCanvasDiagnosticValue> values = null)
        {
            if (diagnostics.Count > 0)
            {
                return;
            }

            diagnostics.Add(new FoldCanvasDiagnostic(
                code,
                FoldCanvasDiagnosticSeverity.Error,
                message,
                panelId,
                operationId,
                seamId,
                values,
                boundaryId: boundaryId));
        }
    }

    internal static class FoldScriptIdentifier
    {
        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length > FoldCanvasLimits.MaximumFoldScriptIdentifierLength ||
                !IsAsciiLetter(value[0]))
            {
                return false;
            }

            for (int i = 1; i < value.Length; i++)
            {
                char current = value[i];
                if (!IsAsciiLetter(current) &&
                    (current < '0' || current > '9') &&
                    current != '_' && current != '.' && current != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') ||
                (value >= 'a' && value <= 'z');
        }
    }
}
