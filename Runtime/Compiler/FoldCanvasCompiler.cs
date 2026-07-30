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

            FoldCanvasSourceValidator.ValidatePanels(asset, result);
            ValidateOperationIds(asset, result);
            Dictionary<string, SphericalTessellationHint>
                sphericalTessellationHints = null;
            if (!result.HasErrors())
            {
                TryBuildSphericalTessellationHints(
                    asset,
                    result,
                    out sphericalTessellationHints);
            }

            MeshBuildBuffer buffer = null;
            if (!result.HasErrors())
            {
                buffer = new MeshBuildBuffer();
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

                    PanelTessellator.Append(
                        panel,
                        buffer,
                        sphericalHint);
                }

                ExecuteOperations(asset, buffer, result);
            }

            if (!result.HasErrors())
            {
                ValidateGeneratedGeometry(buffer, result);
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
            result.SphereReport =
                FoldCanvasSphereValidator.Analyze(
                    result.CompiledData);
            if (RequiresClosedSphereValidation(asset) &&
                !result.SphereReport.IsClosedSphere)
            {
                AddSphereValidationDiagnostic(
                    result,
                    result.SphereReport);
                return result;
            }

            result.Mesh = UnityMeshConverter.Create(
                result.CompiledData,
                meshName,
                recalculateNormals);
            return result;
        }

        private static bool RequiresClosedSphereValidation(
            FoldCanvasAsset asset)
        {
            int sphericalWrapCount = 0;
            bool hasStitch = false;
            bool hasSolidify = false;
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                FoldOperationDefinition operation = asset.Operations[i];
                if (operation == null || !operation.Enabled)
                {
                    continue;
                }

                if (operation is SphericalWrapOperationDefinition)
                {
                    sphericalWrapCount++;
                }
                else if (operation is StitchOperationDefinition)
                {
                    hasStitch = true;
                }
                else if (operation is SolidifyOperationDefinition)
                {
                    hasSolidify = true;
                }
            }

            return sphericalWrapCount > 1 &&
                hasStitch &&
                !hasSolidify;
        }

        private static void AddSphereValidationDiagnostic(
            FoldCanvasCompileResult result,
            FoldCanvasSphereReport report)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.SphereValidationFailed,
                FoldCanvasDiagnosticSeverity.Error,
                "The stitched M05 spherical panel set did not compile into one closed outward sphere.",
                values: new[]
                {
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
            FoldCanvasCompileResult result)
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

                    continue;
                }

                if (operation is SolidifyOperationDefinition solidify)
                {
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
                                sphericalWrap.LatitudeRange.x),
                            SphericalWrapExecutor.IsPole(
                                sphericalWrap.LatitudeRange.y))
                        : default);
            }

            return true;
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

        private static void ValidateGeneratedGeometry(
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            for (int i = 0; i < buffer.Vertices.Count; i++)
            {
                if (!FiniteMath.IsFinite(buffer.Vertices[i].Position))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonFiniteVertex,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Generated vertex {i} contains NaN or infinity."));
                    return;
                }
            }

            ValidateWeldedTopology(buffer, result);
            if (result.HasErrors())
            {
                return;
            }

            for (int i = 0; i < buffer.Triangles.Count; i += 3)
            {
                Vector3 a = buffer.Vertices[buffer.Triangles[i]].Position;
                Vector3 b = buffer.Vertices[buffer.Triangles[i + 1]].Position;
                Vector3 c = buffer.Vertices[buffer.Triangles[i + 2]].Position;
                float areaSquared = Vector3.Cross(b - a, c - a).sqrMagnitude;
                if (areaSquared <=
                    FoldCanvasGeometryTolerances.MinimumTriangleDoubleAreaSquared)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.ZeroAreaTriangle,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Generated triangle {i / 3} has zero or near-zero area."));
                    return;
                }
            }
        }

        private static void ValidateWeldedTopology(
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            Dictionary<ulong, TopologyEdgeUse> edges =
                new Dictionary<ulong, TopologyEdgeUse>();
            for (int i = 0; i < buffer.Triangles.Count; i += 3)
            {
                int a = buffer.GetTopologyId(buffer.Triangles[i]);
                int b = buffer.GetTopologyId(buffer.Triangles[i + 1]);
                int c = buffer.GetTopologyId(buffer.Triangles[i + 2]);
                if (a == b || b == c || c == a)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonManifoldTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Welded topology collapses generated triangle {i / 3}."));
                    return;
                }

                if (!TryAddTopologyEdge(a, b, i / 3, edges, result) ||
                    !TryAddTopologyEdge(b, c, i / 3, edges, result) ||
                    !TryAddTopologyEdge(c, a, i / 3, edges, result))
                {
                    return;
                }
            }
        }

        private static bool TryAddTopologyEdge(
            int from,
            int to,
            int triangleIndex,
            Dictionary<ulong, TopologyEdgeUse> edges,
            FoldCanvasCompileResult result)
        {
            int minimum = Math.Min(from, to);
            int maximum = Math.Max(from, to);
            ulong key =
                ((ulong)(uint)minimum << 32) |
                (uint)maximum;
            int direction = from == minimum ? 1 : -1;
            if (!edges.TryGetValue(key, out TopologyEdgeUse edgeUse))
            {
                edges.Add(key, new TopologyEdgeUse(1, direction));
                return true;
            }

            if (edgeUse.Count >= 2)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonManifoldTopology,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Welded topology edge ({minimum}, {maximum}) has more than two incident triangles; the first excess occurs at triangle {triangleIndex}."));
                return false;
            }

            if (edgeUse.FirstDirection == direction)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonManifoldTopology,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Welded topology edge ({minimum}, {maximum}) has inconsistent triangle orientation at triangle {triangleIndex}."));
                return false;
            }

            edges[key] = new TopologyEdgeUse(
                edgeUse.Count + 1,
                edgeUse.FirstDirection);
            return true;
        }

        private readonly struct TopologyEdgeUse
        {
            public TopologyEdgeUse(int count, int firstDirection)
            {
                Count = count;
                FirstDirection = firstDirection;
            }

            public int Count { get; }

            public int FirstDirection { get; }
        }
    }
}
