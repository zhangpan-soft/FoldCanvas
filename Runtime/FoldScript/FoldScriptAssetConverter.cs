using System;
using UnityEngine;

namespace FoldCanvas
{
    public interface IFoldScriptAppearanceResolver
    {
        bool TryResolve(
            string projectRelativePath,
            out Texture2D appearance);
    }

    public sealed class FoldScriptImportOptions
    {
        public string SourceProjectPath { get; set; } =
            "Assets/FoldScript.foldcanvas.json";

        public IFoldScriptAppearanceResolver AppearanceResolver { get; set; }

        public bool RequireAppearance { get; set; }
    }

    public static class FoldScriptImporter
    {
        public static FoldScriptImportResult Import(
            string json,
            FoldScriptImportOptions options = null)
        {
            options = options ?? new FoldScriptImportOptions();
            FoldScriptReadResult read = FoldScriptSerializer.Read(json);
            if (!read.Success)
            {
                return new FoldScriptImportResult(
                    null,
                    null,
                    null,
                    null,
                    new System.Collections.Generic.List<FoldCanvasDiagnostic>(
                        read.Diagnostics));
            }

            System.Collections.Generic.List<FoldCanvasDiagnostic> diagnostics =
                new System.Collections.Generic.List<FoldCanvasDiagnostic>();
            if (!FoldScriptProjectPath.TryResolveAppearance(
                    options.SourceProjectPath,
                    read.Document.Canvas.Appearance,
                    out string resolvedAppearancePath,
                    out FoldCanvasDiagnostic pathDiagnostic))
            {
                diagnostics.Add(pathDiagnostic);
                return new FoldScriptImportResult(
                    read.Document,
                    null,
                    null,
                    null,
                    diagnostics);
            }

            Texture2D appearance = null;
            if (options.AppearanceResolver != null)
            {
                if (!options.AppearanceResolver.TryResolve(
                        resolvedAppearancePath,
                        out appearance) ||
                    appearance == null)
                {
                    diagnostics.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .FoldScriptAppearanceResolutionFailed,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"FoldScript appearance '{resolvedAppearancePath}' could not be resolved inside the Unity project."));
                    return new FoldScriptImportResult(
                        read.Document,
                        null,
                        null,
                        resolvedAppearancePath,
                        diagnostics);
                }
            }
            else if (options.RequireAppearance)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .FoldScriptAppearanceResolutionFailed,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript import requires an appearance resolver."));
                return new FoldScriptImportResult(
                    read.Document,
                    null,
                    null,
                    resolvedAppearancePath,
                    diagnostics);
            }

            FoldCanvasAsset asset;
            try
            {
                asset = FoldScriptAssetConverter.ToAsset(
                    read.Document,
                    appearance);
            }
            catch (Exception exception)
            {
                diagnostics.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldScriptStructure,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript conversion failed without mutating a persisted asset: " +
                    exception.Message));
                return new FoldScriptImportResult(
                    read.Document,
                    null,
                    null,
                    resolvedAppearancePath,
                    diagnostics);
            }

            FoldScriptWriteResult canonical =
                FoldScriptSerializer.Write(read.Document);
            if (!canonical.Success)
            {
                UnityEngine.Object.DestroyImmediate(asset);
                diagnostics.AddRange(canonical.Diagnostics);
                return new FoldScriptImportResult(
                    read.Document,
                    null,
                    null,
                    resolvedAppearancePath,
                    diagnostics);
            }

            return new FoldScriptImportResult(
                read.Document,
                asset,
                canonical.Json,
                resolvedAppearancePath,
                diagnostics);
        }
    }

    public static class FoldScriptAssetConverter
    {
        public static FoldCanvasAsset ToAsset(
            FoldScriptDocument document,
            Texture2D appearance = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            float lengthScale = UnitToMeters(document.Units);
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = document.AssetId;
            asset.Appearance = appearance;
            FoldCanvasSourceMetadata metadata = asset.SourceMetadata;
            metadata.SchemaVersion = document.SchemaVersion;
            metadata.AssetId = document.AssetId;
            metadata.DisplayName = document.DisplayName;
            metadata.Units = document.Units;
            metadata.AppearanceReference = document.Canvas.Appearance;
            metadata.CanvasPixelWidth = document.Canvas.Width;
            metadata.CanvasPixelHeight = document.Canvas.Height;
            metadata.ExtensionsJson = document.ExtensionsJson;

            for (int i = 0; i < document.Panels.Count; i++)
            {
                FoldScriptPanel source = document.Panels[i];
                Vector2 physicalSize = source.PhysicalSize * lengthScale;
                PanelDefinition panel = source.Shape == PanelShape.Rectangle
                    ? PanelDefinition.CreateRectangle(
                        source.Id,
                        source.CanvasRect,
                        physicalSize,
                        source.USegments,
                        source.VSegments)
                    : PanelDefinition.CreateDisk(
                        source.Id,
                        source.CanvasRect,
                        physicalSize,
                        source.RadialSegments,
                        source.RadialRings);
                asset.Panels.Add(panel);
            }

            for (int i = 0; i < document.Seams.Count; i++)
            {
                FoldScriptSeam source = document.Seams[i];
                asset.Seams.Add(new SeamDefinition
                {
                    Id = source.Id,
                    A = new BoundaryReference(
                        source.A.PanelId,
                        source.A.BoundaryId),
                    B = new BoundaryReference(
                        source.B.PanelId,
                        source.B.BoundaryId),
                    Mode = source.Mode,
                    ReverseB = source.ReverseB,
                    SampleCount = source.SampleCount
                });
            }

            for (int i = 0; i < document.Operations.Count; i++)
            {
                asset.Operations.Add(ToNativeOperation(
                    document.Operations[i],
                    lengthScale));
            }

            FoldScriptCompileSettings compile = document.Compile;
            asset.CompileSettings.WeldEpsilon =
                compile.WeldEpsilon * lengthScale;
            asset.CompileSettings.RecalculateNormals =
                compile.RecalculateNormals;
            asset.CompileSettings.ValidationLevel = compile.ValidationLevel;
            asset.CompileSettings.MaxGeneratedVertices =
                compile.MaxGeneratedVertices;
            asset.CompileSettings.MaxGeneratedTriangles =
                compile.MaxGeneratedTriangles;
            return asset;
        }

        public static FoldScriptDocument ToDocument(
            FoldCanvasAsset asset,
            string appearanceReference = null)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            FoldCanvasSourceMetadata metadata = asset.SourceMetadata;
            FoldScriptUnit units = metadata.Units;
            float metersToUnit = 1f / UnitToMeters(units);
            string reference = appearanceReference ??
                metadata.AppearanceReference;
            FoldScriptDocument document = new FoldScriptDocument
            {
                SchemaVersion = string.IsNullOrWhiteSpace(metadata.SchemaVersion)
                    ? FoldCanvasSourceMetadata.CurrentSchemaVersion
                    : metadata.SchemaVersion,
                AssetId = metadata.AssetId,
                DisplayName = metadata.DisplayName,
                Units = units,
                Canvas = new FoldScriptCanvas
                {
                    Appearance = reference,
                    Width = asset.Appearance != null
                        ? asset.Appearance.width
                        : metadata.CanvasPixelWidth,
                    Height = asset.Appearance != null
                        ? asset.Appearance.height
                        : metadata.CanvasPixelHeight
                },
                ExtensionsJson = metadata.ExtensionsJson,
                Compile = new FoldScriptCompileSettings
                {
                    WeldEpsilon =
                        asset.CompileSettings.WeldEpsilon * metersToUnit,
                    RecalculateNormals =
                        asset.CompileSettings.RecalculateNormals,
                    ValidationLevel =
                        asset.CompileSettings.ValidationLevel,
                    MaxGeneratedVertices =
                        asset.CompileSettings.MaxGeneratedVertices,
                    MaxGeneratedTriangles =
                        asset.CompileSettings.MaxGeneratedTriangles
                }
            };

            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition source = asset.Panels[i];
                document.Panels.Add(new FoldScriptPanel
                {
                    Id = source.Id,
                    Shape = source.Shape,
                    CanvasRect = source.CanvasRect,
                    PhysicalSize = source.PhysicalSize * metersToUnit,
                    USegments = source.USegments,
                    VSegments = source.VSegments,
                    RadialSegments = source.RadialSegments,
                    RadialRings = source.RadialRings
                });
            }

            for (int i = 0; i < asset.Seams.Count; i++)
            {
                SeamDefinition source = asset.Seams[i];
                document.Seams.Add(new FoldScriptSeam
                {
                    Id = source.Id,
                    A = new FoldScriptBoundaryReference
                    {
                        PanelId = source.A.PanelId,
                        BoundaryId = source.A.BoundaryId
                    },
                    B = new FoldScriptBoundaryReference
                    {
                        PanelId = source.B.PanelId,
                        BoundaryId = source.B.BoundaryId
                    },
                    Mode = source.Mode,
                    ReverseB = source.ReverseB,
                    SampleCount = source.SampleCount
                });
            }

            for (int i = 0; i < asset.Operations.Count; i++)
            {
                document.Operations.Add(ToFoldScriptOperation(
                    asset.Operations[i],
                    metersToUnit));
            }

            return document;
        }

        private static FoldOperationDefinition ToNativeOperation(
            FoldScriptOperation operation,
            float lengthScale)
        {
            FoldOperationDefinition native;
            switch (operation)
            {
                case FoldScriptRigidTransformOperation rigid:
                    native = new RigidTransformOperationDefinition
                    {
                        PanelId = rigid.PanelId,
                        Translation = rigid.Translation * lengthScale,
                        RotationEuler = rigid.RotationEuler,
                        Scale = rigid.Scale
                    };
                    break;
                case FoldScriptFoldOperation fold:
                    native = new FoldLineOperationDefinition
                    {
                        PanelId = fold.PanelId,
                        LineStart = fold.LineStart,
                        LineEnd = fold.LineEnd,
                        Side = fold.Side,
                        AngleDegrees = fold.AngleDegrees,
                        Falloff = fold.Falloff
                    };
                    break;
                case FoldScriptRollOperation roll:
                    native = new RollOperationDefinition
                    {
                        PanelId = roll.PanelId,
                        Direction = roll.Direction,
                        AngleDegrees = roll.AngleDegrees,
                        RadiusMode = roll.RadiusMode,
                        ExplicitRadius = roll.ExplicitRadius * lengthScale,
                        TargetSeamId = roll.TargetSeamId,
                        StartAngleDegrees = roll.StartAngleDegrees
                    };
                    break;
                case FoldScriptSphericalWrapOperation sphere:
                    native = new SphericalWrapOperationDefinition
                    {
                        PanelId = sphere.PanelId,
                        Radius = sphere.Radius * lengthScale,
                        LatitudeRange = sphere.LatitudeRange,
                        LongitudeRange = sphere.LongitudeRange,
                        WrapDirection = sphere.WrapDirection,
                        PoleMode = sphere.PoleMode,
                        SubdivisionMode = sphere.SubdivisionMode
                    };
                    break;
                case FoldScriptStitchOperation stitch:
                    StitchOperationDefinition nativeStitch =
                        new StitchOperationDefinition();
                    nativeStitch.SeamIds.AddRange(stitch.SeamIds);
                    native = nativeStitch;
                    break;
                case FoldScriptSolidifyOperation solidify:
                    SolidifyOperationDefinition nativeSolidify =
                        new SolidifyOperationDefinition
                        {
                            Thickness = solidify.Thickness * lengthScale,
                            Direction = solidify.Direction
                        };
                    nativeSolidify.PanelIds.AddRange(solidify.PanelIds);
                    native = nativeSolidify;
                    break;
                default:
                    throw new ArgumentException(
                        "Unsupported FoldScript operation DTO.",
                        nameof(operation));
            }

            native.Id = operation.Id;
            native.Enabled = operation.Enabled;
            return native;
        }

        private static FoldScriptOperation ToFoldScriptOperation(
            FoldOperationDefinition operation,
            float metersToUnit)
        {
            FoldScriptOperation result;
            switch (operation)
            {
                case RigidTransformOperationDefinition rigid:
                    result = new FoldScriptRigidTransformOperation
                    {
                        PanelId = rigid.PanelId,
                        Translation = rigid.Translation * metersToUnit,
                        RotationEuler = rigid.RotationEuler,
                        Scale = rigid.Scale
                    };
                    break;
                case FoldLineOperationDefinition fold:
                    result = new FoldScriptFoldOperation
                    {
                        PanelId = fold.PanelId,
                        LineStart = fold.LineStart,
                        LineEnd = fold.LineEnd,
                        Side = fold.Side,
                        AngleDegrees = fold.AngleDegrees,
                        Falloff = fold.Falloff
                    };
                    break;
                case RollOperationDefinition roll:
                    result = new FoldScriptRollOperation
                    {
                        PanelId = roll.PanelId,
                        Direction = roll.Direction,
                        AngleDegrees = roll.AngleDegrees,
                        RadiusMode = roll.RadiusMode,
                        ExplicitRadius = roll.ExplicitRadius * metersToUnit,
                        TargetSeamId = roll.TargetSeamId,
                        StartAngleDegrees = roll.StartAngleDegrees
                    };
                    break;
                case SphericalWrapOperationDefinition sphere:
                    result = new FoldScriptSphericalWrapOperation
                    {
                        PanelId = sphere.PanelId,
                        Radius = sphere.Radius * metersToUnit,
                        LatitudeRange = sphere.LatitudeRange,
                        LongitudeRange = sphere.LongitudeRange,
                        WrapDirection = sphere.WrapDirection,
                        PoleMode = sphere.PoleMode,
                        SubdivisionMode = sphere.SubdivisionMode
                    };
                    break;
                case StitchOperationDefinition stitch:
                    FoldScriptStitchOperation scriptStitch =
                        new FoldScriptStitchOperation();
                    scriptStitch.SeamIds.AddRange(stitch.SeamIds);
                    result = scriptStitch;
                    break;
                case SolidifyOperationDefinition solidify:
                    FoldScriptSolidifyOperation scriptSolidify =
                        new FoldScriptSolidifyOperation
                        {
                            Thickness = solidify.Thickness * metersToUnit,
                            Direction = solidify.Direction
                        };
                    scriptSolidify.PanelIds.AddRange(solidify.PanelIds);
                    result = scriptSolidify;
                    break;
                default:
                    throw new ArgumentException(
                        "Unsupported native FoldCanvas operation.",
                        nameof(operation));
            }

            result.Id = operation.Id;
            result.Enabled = operation.Enabled;
            return result;
        }

        private static float UnitToMeters(FoldScriptUnit units)
        {
            switch (units)
            {
                case FoldScriptUnit.Meter: return 1f;
                case FoldScriptUnit.Centimeter: return 0.01f;
                case FoldScriptUnit.Millimeter: return 0.001f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(units));
            }
        }
    }
}
