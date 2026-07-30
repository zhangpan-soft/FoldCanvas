using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal static class FoldCanvasSourceValidator
    {
        public static void Validate(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            ValidatePanels(asset, result);
            ValidateSeams(asset, result);
        }

        private static void ValidatePanels(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            FoldCanvasCompileSettings settings = asset.CompileSettings;
            int maxVertices = settings != null
                ? settings.MaxGeneratedVertices
                : FoldCanvasCompileSettings.DefaultMaxGeneratedVertices;
            int maxTriangles = settings != null
                ? settings.MaxGeneratedTriangles
                : FoldCanvasCompileSettings.DefaultMaxGeneratedTriangles;
            float weldEpsilon = settings != null
                ? settings.WeldEpsilon
                : FoldCanvasCompileSettings.DefaultWeldEpsilon;

            bool limitsValid = maxVertices > 0 && maxTriangles > 0;
            if (!limitsValid)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidCompileLimits,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Generated vertex and triangle safety limits must be greater than zero."));
            }

            if (!FiniteMath.IsFinite(weldEpsilon) || weldEpsilon <= 0f)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidWeldEpsilon,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The weld epsilon must be finite and greater than zero."));
            }

            long cumulativeVertices = 0;
            long cumulativeTriangles = 0;
            HashSet<string> panelIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                if (panel == null)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidPanelDimensions,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Panel at index {i} is null."));
                    continue;
                }

                ValidatePanelId(panel, panelIds, result);

                bool shapeSupported =
                    panel.Shape == PanelShape.Rectangle ||
                    panel.Shape == PanelShape.Disk;
                if (!shapeSupported)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsupportedPanelShape,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Panel shape value {(int)panel.Shape} is not supported.",
                        panel.Id));
                }

                if (!FiniteMath.IsFinite(panel.PhysicalSize))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonFinitePanelSize,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel physical dimensions must be finite.",
                        panel.Id));
                }
                else if (panel.PhysicalSize.x <= 0f || panel.PhysicalSize.y <= 0f)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonPositivePanelSize,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel physical dimensions must be greater than zero.",
                        panel.Id));
                }

                if (!IsCanvasRectValid(panel.CanvasRect))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.CanvasRectOutOfRange,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel canvas rect must be finite, positive, and remain inside normalized UV space.",
                        panel.Id));
                }

                bool tessellationValid =
                    shapeSupported && IsTessellationMinimumValid(panel);
                if (shapeSupported && !tessellationValid)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidTessellation,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel tessellation counts are below the minimum.",
                        panel.Id));
                }

                if (!tessellationValid)
                {
                    continue;
                }

                if (!TryEstimateGeometry(
                    panel,
                    out long panelVertices,
                    out long panelTriangles))
                {
                    AddExcessiveTessellationDiagnostic(
                        panel,
                        maxVertices,
                        maxTriangles,
                        result);
                    continue;
                }

                bool exceedsLimits =
                    !limitsValid ||
                    panelVertices > maxVertices ||
                    panelTriangles > maxTriangles ||
                    cumulativeVertices > maxVertices - panelVertices ||
                    cumulativeTriangles > maxTriangles - panelTriangles;

                if (exceedsLimits)
                {
                    AddExcessiveTessellationDiagnostic(
                        panel,
                        maxVertices,
                        maxTriangles,
                        result);
                    continue;
                }

                cumulativeVertices += panelVertices;
                cumulativeTriangles += panelTriangles;
            }
        }

        private static void ValidatePanelId(
            PanelDefinition panel,
            HashSet<string> panelIds,
            FoldCanvasCompileResult result)
        {
            if (string.IsNullOrWhiteSpace(panel.Id))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.EmptyPanelId,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Panel IDs must be non-empty.",
                    panel.Id));
                return;
            }

            if (!panelIds.Add(panel.Id))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.DuplicatePanelId,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Panel IDs must be unique.",
                    panel.Id));
            }
        }

        private static bool IsCanvasRectValid(Rect rect)
        {
            return
                FiniteMath.IsFinite(new Vector2(rect.x, rect.y)) &&
                FiniteMath.IsFinite(new Vector2(rect.width, rect.height)) &&
                rect.width > 0f &&
                rect.height > 0f &&
                rect.xMin >= 0f &&
                rect.yMin >= 0f &&
                rect.xMax <= 1f &&
                rect.yMax <= 1f;
        }

        private static bool IsTessellationMinimumValid(PanelDefinition panel)
        {
            switch (panel.Shape)
            {
                case PanelShape.Rectangle:
                    return panel.USegments >= 1 && panel.VSegments >= 1;
                case PanelShape.Disk:
                    return panel.RadialSegments >= 3 && panel.RadialRings >= 1;
                default:
                    return false;
            }
        }

        internal static bool TryEstimateGeometry(
            PanelDefinition panel,
            out long vertices,
            out long triangles)
        {
            try
            {
                checked
                {
                    switch (panel.Shape)
                    {
                        case PanelShape.Rectangle:
                            vertices =
                                ((long)panel.USegments + 1L) *
                                ((long)panel.VSegments + 1L);
                            triangles =
                                2L * panel.USegments * panel.VSegments;
                            return true;
                        case PanelShape.Disk:
                            vertices =
                                1L +
                                (long)panel.RadialSegments * panel.RadialRings;
                            triangles =
                                (long)panel.RadialSegments *
                                (2L * panel.RadialRings - 1L);
                            return true;
                        default:
                            vertices = 0;
                            triangles = 0;
                            return false;
                    }
                }
            }
            catch (OverflowException)
            {
                vertices = long.MaxValue;
                triangles = long.MaxValue;
                return false;
            }
        }

        private static void AddExcessiveTessellationDiagnostic(
            PanelDefinition panel,
            int maxVertices,
            int maxTriangles,
            FoldCanvasCompileResult result)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.ExcessiveTessellation,
                FoldCanvasDiagnosticSeverity.Error,
                $"Panel tessellation would exceed the configured cumulative safety limits of {maxVertices} vertices and {maxTriangles} triangles.",
                panel.Id));
        }

        private static void ValidateSeams(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            for (int i = 0; i < asset.Seams.Count; i++)
            {
                SeamDefinition seam = asset.Seams[i];
                if (seam == null)
                {
                    continue;
                }

                if (seam.SampleCount >
                    FoldCanvasLimits.MaximumStitchSampleCount)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .StitchSampleCountOutOfRange,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Stitch sampleCount must be between {FoldCanvasLimits.MinimumStitchSampleCount} and {FoldCanvasLimits.MaximumStitchSampleCount} for both JSON and native assets.",
                        seamId: seam.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "requestedSampleCount",
                                seam.SampleCount),
                            new FoldCanvasDiagnosticValue(
                                "minimumSampleCount",
                                FoldCanvasLimits
                                    .MinimumStitchSampleCount),
                            new FoldCanvasDiagnosticValue(
                                "maximumSampleCount",
                                FoldCanvasLimits
                                    .MaximumStitchSampleCount)
                        }));
                }
            }
        }
    }
}
