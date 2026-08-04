using System;
using System.Globalization;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal static class FoldCanvasRobustnessScaleFixtures
    {
        public const int LargeUSegments = 127;
        public const int LargeVSegments = 63;

        public const int LargeBaseVertexCount =
            (LargeUSegments + 1) * (LargeVSegments + 1);

        public const int LargeBaseTriangleCount =
            2 * LargeUSegments * LargeVSegments;

        public const int LargeOpenBoundaryEdgeCount =
            2 * (LargeUSegments + LargeVSegments);

        public const int LargeSolidifiedRenderVertexCount =
            2 * LargeBaseVertexCount +
            4 * LargeOpenBoundaryEdgeCount;

        public const int LargeSolidifiedTopologyVertexCount =
            2 * LargeBaseVertexCount;

        public const int LargeSolidifiedTriangleCount =
            2 * LargeBaseTriangleCount +
            2 * LargeOpenBoundaryEdgeCount;

        public const int StrictBudgetPanelCount = 355;
        public const int StrictBudgetRenderVertexCount =
            StrictBudgetPanelCount * 4;
        public const int StrictBudgetTriangleCount =
            StrictBudgetPanelCount * 2;

        public const string LargeSolidifyAssetId =
            "m13-large-solidify";

        public const string LargeSolidifyGeometrySha256 =
            "2045705d501770fc866354e431c58462e1ddee8f278d3fa738ce03b6e24b47b8";

        public const int LargePlanarUSegments = 191;
        public const int LargePlanarVSegments = 95;
        public const int LargeCupWallSegments = 64;
        public const int LargeCupWallRows = 64;
        public const int LargeCupBottomRadialSegments = 32;
        public const int LargeSphereGoreCount = 16;
        public const int LargeSphereUSegmentsPerGore = 8;
        public const int LargeSphereVSegmentsPerGore = 32;
        public const int LargeTorusUSegments = 96;
        public const int LargeTorusVSegments = 48;
        public const int LargeStitchSampleCount = 1025;
        public const float LargeCupSafeThickness = 0.0005f;
        public const float LargeCupCrossingThickness = 0.004f;

        public const int LargePlanarRenderVertexCount = 18432;
        public const int LargePlanarTopologyVertexCount = 18432;
        public const int LargePlanarTriangleCount = 36290;
        public const int LargeCupRenderVertexCount = 12804;
        public const int LargeCupTopologyVertexCount = 12290;
        public const int LargeCupTriangleCount = 24576;
        public const int LargeSphereRenderVertexCount = 4496;
        public const int LargeSphereTopologyVertexCount = 3970;
        public const int LargeSphereTriangleCount = 7936;
        public const int LargeTorusRenderVertexCount = 4753;
        public const int LargeTorusTopologyVertexCount = 4608;
        public const int LargeTorusTriangleCount = 9216;
        public const int LargeStitchRenderVertexCount = 4626;
        public const int LargeStitchTopologyVertexCount = 3601;
        public const int LargeStitchTriangleCount = 6848;

        public const string LargePlanarGeometrySha256 =
            "e34fb96222ae5f4c439367f59537309cb5fe4e438aa2ce41ec678485fa82dab0";
        public const string LargeCupGeometrySha256 =
            "d4f4e52d93b22cd04f57a24f32f4b3dd83f581b086f0421aa83b16f1791b774c";
        public const string LargeSphereGeometrySha256 =
            "cf268f4675a9187a4f79b51355731e2fefb340e6cfb6f80fadabdc142985d9cb";
        public const string LargeTorusGeometrySha256 =
            "f9ee5decd1c37e4a820170c76e892f3de14c8dd6c70325eaa1aeaf15781b4ada";
        public const string LargeStitchGeometrySha256 =
            "86555fb11c39fd9742fb40a590ea4d3892d6496c2fd13479a353bd50d50f5279";

        public static FoldCanvasAsset CreateLargeSolidifyAsset(
            int maxGeneratedVertices,
            int maxGeneratedTriangles)
        {
            if (maxGeneratedVertices < LargeBaseVertexCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxGeneratedVertices),
                    "The scale fixture limit must admit source tessellation before Solidify.");
            }

            if (maxGeneratedTriangles < LargeBaseTriangleCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxGeneratedTriangles),
                    "The scale fixture limit must admit source tessellation before Solidify.");
            }

            FoldCanvasAsset asset = CreateAsset(LargeSolidifyAssetId);
            asset.CompileSettings.MaxGeneratedVertices =
                maxGeneratedVertices;
            asset.CompileSettings.MaxGeneratedTriangles =
                maxGeneratedTriangles;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(4f, 2f),
                LargeUSegments,
                LargeVSegments));
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify",
                    Thickness = 0.02f,
                    Direction = SolidifyDirection.Centered
                };
            solidify.PanelIds.Add("panel");
            asset.Operations.Add(solidify);
            return asset;
        }

        public static FoldCanvasAsset CreateStrictValidationBudgetAsset()
        {
            FoldCanvasAsset asset = CreateAsset(
                "m13-strict-validation-budget");
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Strict;
            asset.CompileSettings.MaxGeneratedVertices =
                StrictBudgetRenderVertexCount;
            asset.CompileSettings.MaxGeneratedTriangles =
                StrictBudgetTriangleCount;

            for (int panelIndex = 0;
                panelIndex < StrictBudgetPanelCount;
                panelIndex++)
            {
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    "overlap-" + panelIndex.ToString(
                        "D3",
                        CultureInfo.InvariantCulture),
                    new Rect(0f, 0f, 1f, 1f),
                    Vector2.one,
                    1,
                    1));
            }

            return asset;
        }

        public static FoldCanvasAsset CreateLargePlanarAsset()
        {
            FoldCanvasAsset asset = CreateAsset("m13-large-planar");
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Standard;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "planar",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(4f, 2f),
                LargePlanarUSegments,
                LargePlanarVSegments));
            return asset;
        }

        public static FoldCanvasAsset CreateLargeCupAsset()
        {
            return CreateLargeCupAsset(LargeCupSafeThickness);
        }

        public static FoldCanvasAsset CreateLargeCupAsset(float thickness)
        {
            const float radius = 0.05f;
            const float height = 0.12f;
            FoldCanvasAsset asset = CreateAsset("m13-large-cup");
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Strict;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0f, 0.5f, 1f, 0.5f),
                new Vector2(2f * Mathf.PI * radius, height),
                LargeCupWallSegments,
                LargeCupWallRows));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.25f, 0f, 0.5f, 0.5f),
                Vector2.one * (2f * radius),
                LargeCupWallSegments,
                LargeCupBottomRadialSegments));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-wall",
                A = new BoundaryReference("wall", "uMin"),
                B = new BoundaryReference("wall", "uMax"),
                Mode = SeamMode.Weld,
                SampleCount = LargeCupWallRows + 1
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-bottom",
                A = new BoundaryReference("wall", "vMin"),
                B = new BoundaryReference("bottom", "perimeter"),
                Mode = SeamMode.Weld,
                SampleCount = LargeCupWallSegments
            });
            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll-wall",
                PanelId = "wall",
                Direction = RollDirection.U,
                AngleDegrees = 360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                StartAngleDegrees = 180f
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(0f, -height * 0.5f, 0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch-cup" };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            asset.Operations.Add(stitch);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-cup",
                    Thickness = thickness,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("wall");
            solidify.PanelIds.Add("bottom");
            asset.Operations.Add(solidify);
            return asset;
        }

        public static FoldCanvasAsset CreateLargeSphereAsset()
        {
            const float radius = 0.75f;
            FoldCanvasAsset asset = CreateAsset("m13-large-sphere");
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Standard;
            float goreWidth =
                2f * Mathf.PI * radius / LargeSphereGoreCount;
            float goreHeight = Mathf.PI * radius;
            float longitudeStep = 360f / LargeSphereGoreCount;
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch-sphere" };
            for (int goreIndex = 0;
                goreIndex < LargeSphereGoreCount;
                goreIndex++)
            {
                string panelId = "gore-" + goreIndex.ToString(
                    "D2",
                    CultureInfo.InvariantCulture);
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    panelId,
                    new Rect(
                        goreIndex / (float)LargeSphereGoreCount,
                        0f,
                        1f / LargeSphereGoreCount,
                        1f),
                    new Vector2(goreWidth, goreHeight),
                    LargeSphereUSegmentsPerGore,
                    LargeSphereVSegmentsPerGore));
                asset.Operations.Add(
                    new SphericalWrapOperationDefinition
                    {
                        Id = "wrap-" + goreIndex.ToString(
                            "D2",
                            CultureInfo.InvariantCulture),
                        PanelId = panelId,
                        Radius = radius,
                        LatitudeRange = new Vector2(-90f, 90f),
                        LongitudeRange = new Vector2(
                            -180f + goreIndex * longitudeStep,
                            -180f + (goreIndex + 1) * longitudeStep),
                        WrapDirection = SphericalWrapDirection
                            .LongitudeAlongU,
                        PoleMode = SphericalPoleMode.Merge,
                        SubdivisionMode = SphericalSubdivisionMode
                            .PanelGrid
                    });
            }

            for (int goreIndex = 0;
                goreIndex < LargeSphereGoreCount;
                goreIndex++)
            {
                string seamId = "seam-" + goreIndex.ToString(
                    "D2",
                    CultureInfo.InvariantCulture);
                asset.Seams.Add(new SeamDefinition
                {
                    Id = seamId,
                    A = new BoundaryReference(
                        "gore-" + goreIndex.ToString(
                            "D2",
                            CultureInfo.InvariantCulture),
                        "uMax"),
                    B = new BoundaryReference(
                        "gore-" +
                        ((goreIndex + 1) % LargeSphereGoreCount)
                            .ToString(
                                "D2",
                                CultureInfo.InvariantCulture),
                        "uMin"),
                    Mode = SeamMode.Weld
                });
                stitch.SeamIds.Add(seamId);
            }

            asset.Operations.Add(stitch);
            return asset;
        }

        public static FoldCanvasAsset CreateLargeTorusAsset()
        {
            const float majorRadius = 1.1f;
            const float minorRadius = 0.32f;
            FoldCanvasAsset asset = CreateAsset("m13-large-torus");
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Strict;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "torus",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(
                    2f * Mathf.PI * majorRadius,
                    2f * Mathf.PI * minorRadius),
                LargeTorusUSegments,
                LargeTorusVSegments));
            asset.Operations.Add(new ToroidalWrapOperationDefinition
            {
                Id = "wrap-torus",
                PanelId = "torus",
                MajorRadius = majorRadius,
                MinorRadius = minorRadius,
                MajorAngleRange = new Vector2(0f, 360f),
                MinorAngleRange = new Vector2(0f, 360f),
                WrapDirection = ToroidalWrapDirection.MajorAlongU
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-major",
                A = new BoundaryReference("torus", "uMin"),
                B = new BoundaryReference("torus", "uMax"),
                Mode = SeamMode.Weld
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-minor",
                A = new BoundaryReference("torus", "vMin"),
                B = new BoundaryReference("torus", "vMax"),
                Mode = SeamMode.Weld
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch-torus" };
            stitch.SeamIds.Add("close-major");
            stitch.SeamIds.Add("close-minor");
            asset.Operations.Add(stitch);
            return asset;
        }

        public static FoldCanvasAsset CreateLargeStitchAsset()
        {
            FoldCanvasAsset asset = CreateAsset("m13-large-stitch");
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Standard;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "left",
                new Rect(0f, 0f, 0.5f, 1f),
                Vector2.one,
                8,
                64));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "right",
                new Rect(0.5f, 0f, 0.5f, 1f),
                Vector2.one,
                8,
                256));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "join",
                A = new BoundaryReference("left", "uMax"),
                B = new BoundaryReference("right", "uMin"),
                Mode = SeamMode.Weld,
                SampleCount = LargeStitchSampleCount
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-right",
                PanelId = "right",
                Translation = Vector3.right,
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("join");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static FoldCanvasAsset CreateAsset(string assetId)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = assetId;
            asset.SourceMetadata.SchemaVersion =
                FoldCanvasSourceMetadata.CurrentSchemaVersion;
            asset.SourceMetadata.AssetId = assetId;
            asset.SourceMetadata.DisplayName = assetId;
            asset.SourceMetadata.Units = FoldScriptUnit.Meter;
            asset.SourceMetadata.AppearanceReference = "appearance.png";
            asset.SourceMetadata.CanvasPixelWidth = 1024;
            asset.SourceMetadata.CanvasPixelHeight = 1024;
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Basic;
            return asset;
        }
    }
}
