using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    public sealed class FoldScriptDocument
    {
        public string SchemaVersion { get; set; } =
            FoldCanvasSourceMetadata.CurrentSchemaVersion;

        public string AssetId { get; set; } = "foldcanvas-asset";

        public string DisplayName { get; set; } = "FoldCanvas Asset";

        public FoldScriptUnit Units { get; set; } = FoldScriptUnit.Meter;

        public FoldScriptCanvas Canvas { get; set; } = new FoldScriptCanvas();

        public List<FoldScriptPanel> Panels { get; } =
            new List<FoldScriptPanel>();

        public List<FoldScriptSeam> Seams { get; } =
            new List<FoldScriptSeam>();

        public List<FoldScriptOperation> Operations { get; } =
            new List<FoldScriptOperation>();

        public FoldScriptCompileSettings Compile { get; set; } =
            new FoldScriptCompileSettings();

        /// <summary>
        /// Optional canonical JSON object for namespaced metadata. It is
        /// preserved but never interpreted by the geometry compiler.
        /// </summary>
        public string ExtensionsJson { get; set; } = string.Empty;
    }

    public sealed class FoldScriptCanvas
    {
        public string Appearance { get; set; } = string.Empty;

        public int Width { get; set; } = 1;

        public int Height { get; set; } = 1;
    }

    public sealed class FoldScriptPanel
    {
        public string Id { get; set; } = "panel";

        public PanelShape Shape { get; set; } = PanelShape.Rectangle;

        public Rect CanvasRect { get; set; } = new Rect(0f, 0f, 1f, 1f);

        public Vector2 PhysicalSize { get; set; } = Vector2.one;

        public int USegments { get; set; } = 1;

        public int VSegments { get; set; } = 1;

        public int RadialSegments { get; set; } = 32;

        public int RadialRings { get; set; } = 4;
    }

    public sealed class FoldScriptBoundaryReference
    {
        public string PanelId { get; set; } = string.Empty;

        public string BoundaryId { get; set; } = string.Empty;

        public bool UseSpan { get; set; }

        public Vector2 Span { get; set; } = new Vector2(0f, 1f);
    }

    public sealed class FoldScriptSeam
    {
        public string Id { get; set; } = "seam";

        public FoldScriptBoundaryReference A { get; set; } =
            new FoldScriptBoundaryReference();

        public FoldScriptBoundaryReference B { get; set; } =
            new FoldScriptBoundaryReference();

        public SeamMode Mode { get; set; } = SeamMode.Weld;

        public bool ReverseB { get; set; }

        public int SampleCount { get; set; }
    }

    public abstract class FoldScriptOperation
    {
        public string Id { get; set; } = "operation";

        public bool Enabled { get; set; } = true;

        public abstract FoldOperationType Type { get; }
    }

    public sealed class FoldScriptRigidTransformOperation :
        FoldScriptOperation
    {
        public override FoldOperationType Type =>
            FoldOperationType.RigidTransform;

        public string PanelId { get; set; } = "panel";

        public Vector3 Translation { get; set; }

        public Vector3 RotationEuler { get; set; }

        public Vector3 Scale { get; set; } = Vector3.one;
    }

    public sealed class FoldScriptFoldOperation : FoldScriptOperation
    {
        public override FoldOperationType Type => FoldOperationType.Fold;

        public string PanelId { get; set; } = "panel";

        public Vector2 LineStart { get; set; } = new Vector2(0f, 0.5f);

        public Vector2 LineEnd { get; set; } = new Vector2(1f, 0.5f);

        public FoldSide Side { get; set; } = FoldSide.Positive;

        public float AngleDegrees { get; set; } = 90f;

        public float Falloff { get; set; }
    }

    public sealed class FoldScriptRollOperation : FoldScriptOperation
    {
        public override FoldOperationType Type => FoldOperationType.Roll;

        public string PanelId { get; set; } = "panel";

        public RollDirection Direction { get; set; } = RollDirection.U;

        public float AngleDegrees { get; set; } = 360f;

        public RollRadiusMode RadiusMode { get; set; } =
            RollRadiusMode.PreserveArcLength;

        public float ExplicitRadius { get; set; } = 0.05f;

        public string TargetSeamId { get; set; } = string.Empty;

        public float StartAngleDegrees { get; set; }
    }

    public sealed class FoldScriptSphericalWrapOperation :
        FoldScriptOperation
    {
        public override FoldOperationType Type =>
            FoldOperationType.SphericalWrap;

        public string PanelId { get; set; } = "panel";

        public float Radius { get; set; } = 0.5f;

        public Vector2 LatitudeRange { get; set; } =
            new Vector2(-90f, 90f);

        public Vector2 LongitudeRange { get; set; } =
            new Vector2(0f, 45f);

        public SphericalWrapDirection WrapDirection { get; set; } =
            SphericalWrapDirection.LongitudeAlongU;

        public SphericalPoleMode PoleMode { get; set; } =
            SphericalPoleMode.Merge;

        public SphericalSubdivisionMode SubdivisionMode { get; set; } =
            SphericalSubdivisionMode.PanelGrid;
    }

    public sealed class FoldScriptToroidalWrapOperation :
        FoldScriptOperation
    {
        public override FoldOperationType Type =>
            FoldOperationType.ToroidalWrap;

        public string PanelId { get; set; } = "panel";

        public float MajorRadius { get; set; } = 0.5f;

        public float MinorRadius { get; set; } = 0.15f;

        public Vector2 MajorAngleRange { get; set; } =
            new Vector2(0f, 360f);

        public Vector2 MinorAngleRange { get; set; } =
            new Vector2(0f, 360f);

        public ToroidalWrapDirection WrapDirection { get; set; } =
            ToroidalWrapDirection.MajorAlongU;
    }

    public sealed class FoldScriptStitchOperation : FoldScriptOperation
    {
        public override FoldOperationType Type => FoldOperationType.Stitch;

        public List<string> SeamIds { get; } = new List<string>();
    }

    public sealed class FoldScriptSolidifyOperation : FoldScriptOperation
    {
        public override FoldOperationType Type => FoldOperationType.Solidify;

        public List<string> PanelIds { get; } = new List<string>();

        public float Thickness { get; set; } = 0.002f;

        public SolidifyDirection Direction { get; set; } =
            SolidifyDirection.Inward;
    }

    public sealed class FoldScriptCompileSettings
    {
        public float WeldEpsilon { get; set; } =
            FoldCanvasCompileSettings.DefaultWeldEpsilon;

        public bool RecalculateNormals { get; set; } = true;

        public FoldCanvasValidationLevel ValidationLevel { get; set; } =
            FoldCanvasValidationLevel.Basic;

        public int MaxGeneratedVertices { get; set; } =
            FoldCanvasCompileSettings.DefaultMaxGeneratedVertices;

        public int MaxGeneratedTriangles { get; set; } =
            FoldCanvasCompileSettings.DefaultMaxGeneratedTriangles;
    }
}
