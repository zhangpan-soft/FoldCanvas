using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    public static class FoldCanvasCompiler
    {
        public static FoldCanvasCompileResult Compile(FoldCanvasAsset asset)
        {
            FoldCanvasCompileResult result = new FoldCanvasCompileResult();
            if (asset == null)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NullAsset,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The FoldCanvas asset is null."));
                return result;
            }

            FoldCanvasSourceValidator.Validate(asset, result);
            ValidateOperationIds(asset, result);
            Dictionary<string, SphericalTessellationHint>
                sphericalTessellationHints = null;
            SphereValidationPlan sphereValidationPlan = null;
            if (!result.HasErrors())
            {
                TryBuildSphericalTessellationHints(
                    asset,
                    result,
                    out sphericalTessellationHints);
            }

            if (!result.HasErrors())
            {
                sphereValidationPlan =
                    SphereValidationPlan.Build(asset);
            }

            MeshBuildBuffer buffer = null;
            if (!result.HasErrors())
            {
                FoldCanvasCompileSettings settings =
                    asset.CompileSettings;
                int maxVertices = settings != null
                    ? settings.MaxGeneratedVertices
                    : FoldCanvasCompileSettings
                        .DefaultMaxGeneratedVertices;
                int maxTriangles = settings != null
                    ? settings.MaxGeneratedTriangles
                    : FoldCanvasCompileSettings
                        .DefaultMaxGeneratedTriangles;
                float positionTolerance = settings != null
                    ? settings.WeldEpsilon
                    : FoldCanvasCompileSettings
                        .DefaultWeldEpsilon;
                buffer = new MeshBuildBuffer(
                    maxVertices,
                    maxTriangles,
                    positionTolerance);
                try
                {
                    for (int i = 0; i < asset.Panels.Count; i++)
                    {
                        PanelDefinition panel = asset.Panels[i];
                        SphericalTessellationHint? sphericalHint = null;
                        if (sphericalTessellationHints.TryGetValue(
                                panel.Id,
                                out SphericalTessellationHint resolvedHint))
                        {
                            sphericalHint = resolvedHint;
                        }

                        FoldCanvasSourceValidator.TryEstimateGeometry(
                            panel,
                            out long panelVertices,
                            out long panelTriangles);
                        if (!buffer.TryBeginGeometryOperation(
                                panelVertices,
                                panelTriangles,
                                panel.Id,
                                "PanelTessellation",
                                result,
                                out GeometryOperationScope panelScope))
                        {
                            break;
                        }

                        using (panelScope)
                        {
                            PanelTessellator.Append(
                                panel,
                                buffer,
                                sphericalHint);
                        }
                    }

                    if (!result.HasErrors())
                    {
                        ExecuteOperations(
                            asset,
                            buffer,
                            result,
                            sphereValidationPlan);
                    }
                }
                catch (GeometryBudgetExceededException exception)
                {
                    result.Add(exception.Diagnostic);
                }
            }

            if (!result.HasErrors())
            {
                result.GeometryValidationReport =
                    FoldCanvasGeometryValidator.Validate(
                        buffer,
                        asset,
                        result);
            }

            if (result.HasErrors())
            {
                return result;
            }

            string meshName = string.IsNullOrWhiteSpace(asset.name)
                ? "FoldCanvas_Generated"
                : asset.name + "_Generated";
            bool recalculateNormals =
                asset.CompileSettings == null ||
                asset.CompileSettings.RecalculateNormals;

            result.CompiledData = buffer.Freeze();
            result.ClosedVolumeReport =
                FoldCanvasClosedVolumeValidator.Analyze(
                    result.CompiledData);
            if (result.SphereReport == null)
            {
                result.AddSphereReport(
                    FoldCanvasSphereValidator.Analyze(
                        result.CompiledData));
            }

            result.Mesh = UnityMeshConverter.Create(
                result.CompiledData,
                meshName,
                recalculateNormals);
            return result;
        }

        internal static void AddSphereValidationDiagnostic(
            FoldCanvasCompileResult result,
            FoldCanvasSphereReport report)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.SphereValidationFailed,
                FoldCanvasDiagnosticSeverity.Error,
                $"Spherical component '{report.ComponentId}' did not compile into one closed outward sphere at stage {report.ValidationStage}.",
                report.PanelIds.Count > 0
                    ? report.PanelIds[0]
                    : null,
                report.ValidationOperationId,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "validationStage",
                        (int)report.ValidationStage),
                    new FoldCanvasDiagnosticValue(
                        "validationOperationIndex",
                        report.ValidationOperationIndex),
                    new FoldCanvasDiagnosticValue(
                        "sphericalPanelCount",
                        report.SphericalPanelCount),
                    new FoldCanvasDiagnosticValue(
                        "connectedComponentCount",
                        report.ConnectedComponentCount),
                    new FoldCanvasDiagnosticValue(
                        "openEdgeCount",
                        report.OpenEdgeCount),
                    new FoldCanvasDiagnosticValue(
                        "nonManifoldEdgeCount",
                        report.NonManifoldEdgeCount),
                    new FoldCanvasDiagnosticValue(
                        "orientationConflictEdgeCount",
                        report.OrientationConflictEdgeCount),
                    new FoldCanvasDiagnosticValue(
                        "isolatedTopologyVertexCount",
                        report.IsolatedTopologyVertexCount),
                    new FoldCanvasDiagnosticValue(
                        "eulerCharacteristic",
                        report.EulerCharacteristic),
                    new FoldCanvasDiagnosticValue(
                        "northPoleTopologyCount",
                        report.NorthPoleTopologyCount),
                    new FoldCanvasDiagnosticValue(
                        "southPoleTopologyCount",
                        report.SouthPoleTopologyCount),
                    new FoldCanvasDiagnosticValue(
                        "inwardTriangleCount",
                        report.InwardTriangleCount),
                    new FoldCanvasDiagnosticValue(
                        "inconsistentFrameCount",
                        report.InconsistentFrameCount),
                    new FoldCanvasDiagnosticValue(
                        "maximumRadiusError",
                        report.MaximumRadiusError,
                        "m"),
                    new FoldCanvasDiagnosticValue(
                        "radiusTolerance",
                        report.RadiusTolerance,
                        "m")
                }));
        }

        private static void ValidateOperationIds(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            HashSet<string> operationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                FoldOperationDefinition operation = asset.Operations[i];
                if (operation == null)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.DuplicateOperationId,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Operation at index {i} is null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(operation.Id) || !operationIds.Add(operation.Id))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.DuplicateOperationId,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Operation IDs must be non-empty and unique.",
                        operationId: operation.Id));
                }
            }
        }

        private static void ExecuteOperations(
            FoldCanvasAsset asset,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            SphereValidationPlan sphereValidationPlan)
        {
            HashSet<string> stitchedPanelIds =
                new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                FoldOperationDefinition operation = asset.Operations[i];
                if (operation == null || !operation.Enabled)
                {
                    continue;
                }

                if (operation is RigidTransformOperationDefinition rigidTransform)
                {
                    if (RejectPostStitchDeformation(
                        rigidTransform.PanelId,
                        rigidTransform.Id,
                        stitchedPanelIds,
                        result))
                    {
                        return;
                    }

                    if (!RigidTransformExecutor.TryExecute(
                        rigidTransform,
                        buffer,
                        result))
                    {
                        return;
                    }

                    continue;
                }

                if (operation is FoldLineOperationDefinition fold)
                {
                    if (RejectPostStitchDeformation(
                        fold.PanelId,
                        fold.Id,
                        stitchedPanelIds,
                        result))
                    {
                        return;
                    }

                    if (!FoldLineExecutor.TryExecute(fold, buffer, result))
                    {
                        return;
                    }

                    continue;
                }

                if (operation is RollOperationDefinition roll)
                {
                    if (RejectPostStitchDeformation(
                        roll.PanelId,
                        roll.Id,
                        stitchedPanelIds,
                        result))
                    {
                        return;
                    }

                    if (!RollExecutor.TryExecute(roll, buffer, result))
                    {
                        return;
                    }

                    continue;
                }

                if (operation is
                    SphericalWrapOperationDefinition sphericalWrap)
                {
                    if (RejectPostStitchDeformation(
                        sphericalWrap.PanelId,
                        sphericalWrap.Id,
                        stitchedPanelIds,
                        result))
                    {
                        return;
                    }

                    if (!SphericalWrapExecutor.TryExecute(
                        sphericalWrap,
                        buffer,
                        result))
                    {
                        return;
                    }

                    continue;
                }

                if (operation is StitchOperationDefinition stitch)
                {
                    if (!StitchExecutor.TryExecute(
                        stitch,
                        asset,
                        buffer,
                        result,
                        out List<string> selectedPanelIds))
                    {
                        return;
                    }

                    for (int panelIndex = 0;
                        panelIndex < selectedPanelIds.Count;
                        panelIndex++)
                    {
                        stitchedPanelIds.Add(selectedPanelIds[panelIndex]);
                    }

                    if (!sphereValidationPlan.TryValidateAfterOperation(
                            i,
                            buffer,
                            result))
                    {
                        return;
                    }

                    continue;
                }

                if (operation is SolidifyOperationDefinition solidify)
                {
                    if (!sphereValidationPlan.TryValidateBeforeSolidify(
                            i,
                            solidify,
                            buffer,
                            result))
                    {
                        return;
                    }

                    if (!SolidifyExecutor.TryExecute(
                        solidify,
                        buffer,
                        result))
                    {
                        return;
                    }

                    continue;
                }

                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedOperation,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Operation type '{operation.Type}' is not implemented in the bootstrap compiler. Follow the active milestone instead of silently ignoring it.",
                    operationId: operation.Id));
                return;
            }

            sphereValidationPlan.TryValidateRemaining(
                buffer,
                result);
        }

        private static bool TryBuildSphericalTessellationHints(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result,
            out Dictionary<string, SphericalTessellationHint> hints)
        {
            hints = new Dictionary<string, SphericalTessellationHint>(
                StringComparer.Ordinal);
            for (int operationIndex = 0;
                operationIndex < asset.Operations.Count;
                operationIndex++)
            {
                if (!(asset.Operations[operationIndex] is
                        SphericalWrapOperationDefinition sphericalWrap) ||
                    !sphericalWrap.Enabled)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        sphericalWrap.PanelId))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .SphericalWrapTargetMissing,
                        FoldCanvasDiagnosticSeverity.Error,
                        "SphericalWrap panelId must be non-empty and reference an existing panel.",
                        sphericalWrap.PanelId,
                        sphericalWrap.Id));
                    return false;
                }

                if (hints.ContainsKey(sphericalWrap.PanelId))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .DuplicateSphericalWrapTarget,
                        FoldCanvasDiagnosticSeverity.Error,
                        "M05 supports one enabled SphericalWrap operation per panel.",
                        sphericalWrap.PanelId,
                        sphericalWrap.Id));
                    return false;
                }

                if (HasUnsupportedPreWrapDeformation(
                    asset,
                    operationIndex,
                    sphericalWrap.PanelId))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .UnsupportedSphericalEmbedding,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Only RigidTransform may position a panel before SphericalWrap in M05. Prior Fold, Roll, Solidify, or SphericalWrap is unsupported.",
                        sphericalWrap.PanelId,
                        sphericalWrap.Id));
                    return false;
                }

                bool validHintContract =
                    sphericalWrap.WrapDirection ==
                        SphericalWrapDirection.LongitudeAlongU ||
                    sphericalWrap.WrapDirection ==
                        SphericalWrapDirection.LongitudeAlongV;
                validHintContract &=
                    sphericalWrap.PoleMode == SphericalPoleMode.Merge ||
                    sphericalWrap.PoleMode ==
                        SphericalPoleMode.KeepFan;
                validHintContract &=
                    sphericalWrap.SubdivisionMode ==
                        SphericalSubdivisionMode.PanelGrid;
                validHintContract &=
                    FiniteMath.IsFinite(
                        sphericalWrap.LatitudeRange);
                hints.Add(
                    sphericalWrap.PanelId,
                    validHintContract
                        ? new SphericalTessellationHint(
                            sphericalWrap.WrapDirection,
                            sphericalWrap.PoleMode,
                            SphericalWrapExecutor.IsPole(
                                sphericalWrap.LatitudeRange.x,
                                sphericalWrap.Radius,
                                GetPositionTolerance(asset)),
                            SphericalWrapExecutor.IsPole(
                                sphericalWrap.LatitudeRange.y,
                                sphericalWrap.Radius,
                                GetPositionTolerance(asset)))
                        : default);
            }

            return true;
        }

        private static float GetPositionTolerance(
            FoldCanvasAsset asset)
        {
            return asset.CompileSettings != null
                ? asset.CompileSettings.WeldEpsilon
                : FoldCanvasCompileSettings.DefaultWeldEpsilon;
        }

        private static bool HasUnsupportedPreWrapDeformation(
            FoldCanvasAsset asset,
            int sphericalWrapIndex,
            string panelId)
        {
            for (int i = 0; i < sphericalWrapIndex; i++)
            {
                FoldOperationDefinition previous = asset.Operations[i];
                if (previous == null || !previous.Enabled)
                {
                    continue;
                }

                if (previous is FoldLineOperationDefinition fold &&
                    string.Equals(
                        fold.PanelId,
                        panelId,
                        StringComparison.Ordinal))
                {
                    return true;
                }

                if (previous is RollOperationDefinition roll &&
                    string.Equals(
                        roll.PanelId,
                        panelId,
                        StringComparison.Ordinal))
                {
                    return true;
                }

                if (previous is
                        SphericalWrapOperationDefinition earlierWrap &&
                    string.Equals(
                        earlierWrap.PanelId,
                        panelId,
                        StringComparison.Ordinal))
                {
                    return true;
                }

                if (previous is SolidifyOperationDefinition solidify)
                {
                    for (int panelIndex = 0;
                        panelIndex < solidify.PanelIds.Count;
                        panelIndex++)
                    {
                        if (string.Equals(
                            solidify.PanelIds[panelIndex],
                            panelId,
                            StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool RejectPostStitchDeformation(
            string panelId,
            string operationId,
            HashSet<string> stitchedPanelIds,
            FoldCanvasCompileResult result)
        {
            if (!stitchedPanelIds.Contains(panelId))
            {
                return false;
            }

            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.StitchMustBeTerminalForSelectedPanels,
                FoldCanvasDiagnosticSeverity.Error,
                "Stitch must be the terminal position-deforming operation for every panel selected by that Stitch until topology-group deformation propagation is implemented.",
                panelId,
                operationId));
            return true;
        }

    }
}
