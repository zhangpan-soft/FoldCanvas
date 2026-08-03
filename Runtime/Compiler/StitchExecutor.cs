using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal static class StitchExecutor
    {
        public static bool TryExecute(
            StitchOperationDefinition operation,
            FoldCanvasAsset asset,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            out List<string> selectedPanelIds)
        {
            selectedPanelIds = new List<string>();
            if (operation.SeamIds == null || operation.SeamIds.Count == 0)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.EmptyStitchSeamList,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch must select at least one declared seam.",
                    operationId: operation.Id));
                return false;
            }

            float weldEpsilon = asset.CompileSettings != null
                ? asset.CompileSettings.WeldEpsilon
                : FoldCanvasCompileSettings.DefaultWeldEpsilon;

            List<SeamDefinition> seams =
                new List<SeamDefinition>(operation.SeamIds.Count);
            for (int i = 0; i < operation.SeamIds.Count; i++)
            {
                string seamId = operation.SeamIds[i];
                if (!TryResolveUniqueSeam(
                    asset,
                    seamId,
                    operation.Id,
                    result,
                    out SeamDefinition seam))
                {
                    return false;
                }

                seams.Add(seam);
                AddPanelId(selectedPanelIds, seam.A.PanelId);
                AddPanelId(selectedPanelIds, seam.B.PanelId);

                if (seam.Mode != SeamMode.Weld &&
                    seam.Mode != SeamMode.Bridge)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsupportedStitchSeamMode,
                        FoldCanvasDiagnosticSeverity.Error,
                        "M04 Stitch executes Weld and Bridge seams. Hinge and KeepOpen remain declarative.",
                        operationId: operation.Id,
                        seamId: seam.Id));
                    return false;
                }

            }

            using (MeshBuildBufferTransaction transaction =
                buffer.BeginTransaction())
            {
                for (int i = 0; i < seams.Count; i++)
                {
                    SeamDefinition seam = seams[i];
                    if (!BoundaryCorrespondenceSolver
                        .TryEstimateAdditionalGeometry(
                            seam,
                            operation.Id,
                            buffer,
                            result,
                            out long seamVertices,
                            out long seamTriangles))
                    {
                        return false;
                    }

                    if (!buffer.TryBeginGeometryOperation(
                            seamVertices,
                            seamTriangles,
                            operation.Id,
                            "Stitch",
                            result,
                            out GeometryOperationScope geometryScope))
                    {
                        return false;
                    }

                    using (geometryScope)
                    {
                        if (!BoundaryCorrespondenceSolver.TryCreate(
                            seam,
                            operation.Id,
                            buffer,
                            result,
                            out SeamCorrespondence correspondence))
                        {
                            return false;
                        }

                        if (seam.Mode == SeamMode.Bridge)
                        {
                            AppendBridge(correspondence, buffer);
                            continue;
                        }

                        int commonCount =
                            correspondence.BoundaryA.Count;
                        float maximumGap = 0f;
                        for (int sample = 0;
                            sample < commonCount;
                            sample++)
                        {
                            Vector3 positionA =
                                buffer.Vertices[
                                    correspondence
                                        .BoundaryA[sample]].Position;
                            Vector3 positionB =
                                buffer.Vertices[
                                    correspondence
                                        .BoundaryB[sample]].Position;
                            maximumGap = Mathf.Max(
                                maximumGap,
                                Vector3.Distance(
                                    positionA,
                                    positionB));
                        }

                        if (maximumGap > weldEpsilon)
                        {
                            result.Add(new FoldCanvasDiagnostic(
                                FoldCanvasDiagnosticCodes
                                    .StitchPositionMismatch,
                                FoldCanvasDiagnosticSeverity.Error,
                                "Stitch boundary samples exceed the configured weld epsilon.",
                                operationId: operation.Id,
                                seamId: seam.Id,
                                values: new[]
                                {
                                    new FoldCanvasDiagnosticValue(
                                        "maximumGap",
                                        maximumGap,
                                        "m"),
                                    new FoldCanvasDiagnosticValue(
                                        "weldEpsilon",
                                        weldEpsilon,
                                        "m"),
                                    new FoldCanvasDiagnosticValue(
                                        "sampleCount",
                                        commonCount)
                                }));
                            return false;
                        }

                        for (int sample = 0;
                            sample < commonCount;
                            sample++)
                        {
                            buffer.UnionTopology(
                                correspondence.BoundaryA[sample],
                                correspondence.BoundaryB[sample]);
                        }

                        buffer.SnapWeldedTopologyPositions();
                        buffer.AddWeldValidationRecord(
                            operation.Id,
                            seam.Id,
                            correspondence.BoundaryA,
                            correspondence.BoundaryB);
                    }
                }

                transaction.Commit();
            }

            return true;
        }

        private static void AppendBridge(
            SeamCorrespondence correspondence,
            MeshBuildBuffer buffer)
        {
            int segmentCount = correspondence.IsClosed
                ? correspondence.BoundaryA.Count
                : correspondence.BoundaryA.Count - 1;
            for (int segment = 0; segment < segmentCount; segment++)
            {
                int next = (segment + 1) %
                    correspondence.BoundaryA.Count;
                int a = correspondence.BoundaryA[segment];
                int aNext = correspondence.BoundaryA[next];
                int b = correspondence.BoundaryB[segment];
                int bNext = correspondence.BoundaryB[next];

                buffer.Triangles.Add(a);
                buffer.Triangles.Add(b);
                buffer.Triangles.Add(bNext);

                buffer.Triangles.Add(a);
                buffer.Triangles.Add(bNext);
                buffer.Triangles.Add(aNext);
            }
        }

        private static void AddPanelId(List<string> panelIds, string panelId)
        {
            for (int i = 0; i < panelIds.Count; i++)
            {
                if (string.Equals(
                    panelIds[i],
                    panelId,
                    StringComparison.Ordinal))
                {
                    return;
                }
            }

            panelIds.Add(panelId);
        }

        private static bool TryResolveUniqueSeam(
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
                if (candidate != null &&
                    string.Equals(
                        candidate.Id,
                        seamId,
                        StringComparison.Ordinal))
                {
                    seam = candidate;
                    matches++;
                }
            }

            if (matches == 0)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.StitchSeamMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch references a seam that is not declared.",
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
                return false;
            }

            return true;
        }

    }
}
