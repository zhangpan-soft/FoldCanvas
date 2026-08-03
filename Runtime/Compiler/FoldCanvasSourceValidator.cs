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

            if (settings != null &&
                !Enum.IsDefined(
                    typeof(FoldCanvasValidationLevel),
                    settings.ValidationLevel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidValidationLevel,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Validation level must be Basic, Standard, or Strict."));
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
            Dictionary<string, PanelDefinition> panelsById =
                BuildPanelLookup(asset);
            Dictionary<string, int> sphericalWrapIndexByPanel =
                BuildSphericalWrapIndexLookup(asset);
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

            HashSet<SeamDefinition> validatedSelections =
                new HashSet<SeamDefinition>();
            for (int operationIndex = 0;
                operationIndex < asset.Operations.Count;
                operationIndex++)
            {
                if (!(asset.Operations[operationIndex] is
                        StitchOperationDefinition stitch) ||
                    !stitch.Enabled ||
                    stitch.SeamIds == null)
                {
                    continue;
                }

                for (int seamIndex = 0;
                    seamIndex < stitch.SeamIds.Count;
                    seamIndex++)
                {
                    string seamId = stitch.SeamIds[seamIndex];
                    if (!TryResolveSelectedSeam(
                            asset,
                            seamId,
                            stitch.Id,
                            result,
                            out SeamDefinition seam))
                    {
                        continue;
                    }

                    if (validatedSelections.Add(seam))
                    {
                        ValidateBoundaryReference(
                            seam.A,
                            "A",
                            seam.Id,
                            stitch.Id,
                            panelsById,
                            result);
                        ValidateBoundaryReference(
                            seam.B,
                            "B",
                            seam.Id,
                            stitch.Id,
                            panelsById,
                            result);
                    }

                    ValidateSphericalWrapBeforeStitch(
                        seam.A,
                        "A",
                        seam.Id,
                        stitch.Id,
                        operationIndex,
                        sphericalWrapIndexByPanel,
                        result);
                    ValidateSphericalWrapBeforeStitch(
                        seam.B,
                        "B",
                        seam.Id,
                        stitch.Id,
                        operationIndex,
                        sphericalWrapIndexByPanel,
                        result);
                }
            }
        }

        private static Dictionary<string, int>
            BuildSphericalWrapIndexLookup(FoldCanvasAsset asset)
        {
            Dictionary<string, int> operationIndexByPanel =
                new Dictionary<string, int>(StringComparer.Ordinal);
            for (int operationIndex = 0;
                operationIndex < asset.Operations.Count;
                operationIndex++)
            {
                if (!(asset.Operations[operationIndex] is
                        SphericalWrapOperationDefinition sphericalWrap) ||
                    !sphericalWrap.Enabled ||
                    string.IsNullOrWhiteSpace(sphericalWrap.PanelId))
                {
                    continue;
                }

                operationIndexByPanel[sphericalWrap.PanelId] =
                    operationIndex;
            }

            return operationIndexByPanel;
        }

        private static void ValidateSphericalWrapBeforeStitch(
            BoundaryReference reference,
            string endpointName,
            string seamId,
            string stitchOperationId,
            int stitchOperationIndex,
            Dictionary<string, int> sphericalWrapIndexByPanel,
            FoldCanvasCompileResult result)
        {
            string panelId = reference.PanelId;
            if (string.IsNullOrWhiteSpace(panelId) ||
                !sphericalWrapIndexByPanel.TryGetValue(
                    panelId,
                    out int sphericalWrapOperationIndex) ||
                sphericalWrapOperationIndex < stitchOperationIndex)
            {
                return;
            }

            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes
                    .StitchMustBeTerminalForSelectedPanels,
                FoldCanvasDiagnosticSeverity.Error,
                $"SphericalWrap for Stitch-selected seam endpoint {endpointName} must occur before the Stitch because Stitch is terminal for every selected panel.",
                panelId,
                stitchOperationId,
                seamId,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "sphericalWrapOperationIndex",
                        sphericalWrapOperationIndex),
                    new FoldCanvasDiagnosticValue(
                        "stitchOperationIndex",
                        stitchOperationIndex)
                }));
        }

        private static Dictionary<string, PanelDefinition>
            BuildPanelLookup(FoldCanvasAsset asset)
        {
            Dictionary<string, PanelDefinition> panelsById =
                new Dictionary<string, PanelDefinition>(
                    StringComparer.Ordinal);
            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                if (panel == null ||
                    string.IsNullOrWhiteSpace(panel.Id) ||
                    panelsById.ContainsKey(panel.Id))
                {
                    continue;
                }

                panelsById.Add(panel.Id, panel);
            }

            return panelsById;
        }

        private static bool TryResolveSelectedSeam(
            FoldCanvasAsset asset,
            string seamId,
            string operationId,
            FoldCanvasCompileResult result,
            out SeamDefinition seam)
        {
            seam = null;
            if (string.IsNullOrWhiteSpace(seamId))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.StitchSeamMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch seam IDs must be non-empty.",
                    operationId: operationId,
                    seamId: seamId));
                return false;
            }

            int matches = 0;
            for (int i = 0; i < asset.Seams.Count; i++)
            {
                SeamDefinition candidate = asset.Seams[i];
                if (candidate == null ||
                    string.IsNullOrWhiteSpace(candidate.Id) ||
                    !string.Equals(
                        candidate.Id,
                        seamId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                seam = candidate;
                matches++;
            }

            if (matches == 0)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.StitchSeamMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch references a seam that is not declared with a non-empty ID.",
                    operationId: operationId,
                    seamId: seamId));
                return false;
            }

            if (matches > 1)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.DuplicateSeamId,
                    FoldCanvasDiagnosticSeverity.Error,
                    "A Stitch-selected seam ID must resolve to exactly one declaration.",
                    operationId: operationId,
                    seamId: seamId));
                seam = null;
                return false;
            }

            return true;
        }

        private static void ValidateBoundaryReference(
            BoundaryReference reference,
            string endpointName,
            string seamId,
            string operationId,
            Dictionary<string, PanelDefinition> panelsById,
            FoldCanvasCompileResult result)
        {
            string panelId = reference.PanelId;
            if (string.IsNullOrWhiteSpace(panelId))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.MissingPanelReference,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Stitch-selected seam endpoint {endpointName} must contain a non-empty panelId.",
                    panelId,
                    operationId,
                    seamId));
                return;
            }

            if (!panelsById.TryGetValue(
                    panelId,
                    out PanelDefinition panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.MissingPanelReference,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Stitch-selected seam endpoint {endpointName} references a panel that does not exist.",
                    panelId,
                    operationId,
                    seamId));
                return;
            }

            string boundaryId = reference.BoundaryId;
            if (string.IsNullOrWhiteSpace(boundaryId) ||
                !PanelDefinesBoundary(panel, boundaryId))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.StitchBoundaryMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Stitch-selected seam endpoint {endpointName} references a boundary that does not exist on its panel.",
                    panelId,
                    operationId,
                    seamId));
            }
        }

        private static bool PanelDefinesBoundary(
            PanelDefinition panel,
            string boundaryId)
        {
            if (panel == null ||
                string.IsNullOrWhiteSpace(boundaryId))
            {
                return false;
            }

            switch (panel.Shape)
            {
                case PanelShape.Rectangle:
                    return
                        string.Equals(
                            boundaryId,
                            "uMin",
                            StringComparison.Ordinal) ||
                        string.Equals(
                            boundaryId,
                            "uMax",
                            StringComparison.Ordinal) ||
                        string.Equals(
                            boundaryId,
                            "vMin",
                            StringComparison.Ordinal) ||
                        string.Equals(
                            boundaryId,
                            "vMax",
                            StringComparison.Ordinal);
                case PanelShape.Disk:
                    return string.Equals(
                        boundaryId,
                        "perimeter",
                        StringComparison.Ordinal);
                default:
                    return false;
            }
        }
    }
}
