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
