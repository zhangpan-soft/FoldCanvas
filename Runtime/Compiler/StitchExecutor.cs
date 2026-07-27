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
            FoldCanvasCompileResult result)
        {
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

                if (seam.Mode != SeamMode.Weld)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsupportedStitchSeamMode,
                        FoldCanvasDiagnosticSeverity.Error,
                        "The M03 Stitch gate supports Weld seams only. Hinge, KeepOpen, and Bridge execution remain deferred.",
                        operationId: operation.Id,
                        seamId: seam.Id));
                    return false;
                }

                if (!TryResolveBoundary(
                        seam.A,
                        seam.Id,
                        operation.Id,
                        buffer,
                        result,
                        out List<int> boundaryA) ||
                    !TryResolveBoundary(
                        seam.B,
                        seam.Id,
                        operation.Id,
                        buffer,
                        result,
                        out List<int> boundaryB))
                {
                    return false;
                }

                RemoveRepeatedTerminalTopologySample(boundaryA, buffer);
                RemoveRepeatedTerminalTopologySample(boundaryB, buffer);
                if (seam.ReverseB)
                {
                    boundaryB.Reverse();
                }

                int commonCount = boundaryA.Count;
                bool requestedCountMatches =
                    seam.SampleCount == 0 ||
                    seam.SampleCount == commonCount;
                if (commonCount == 0 ||
                    boundaryB.Count != commonCount ||
                    seam.SampleCount < 0 ||
                    !requestedCountMatches)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.StitchSampleCountMismatch,
                        FoldCanvasDiagnosticSeverity.Error,
                        "The M03 Stitch gate requires equal existing boundary sample counts; deterministic resampling is not implemented.",
                        operationId: operation.Id,
                        seamId: seam.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "boundaryACount",
                                boundaryA.Count),
                            new FoldCanvasDiagnosticValue(
                                "boundaryBCount",
                                boundaryB.Count),
                            new FoldCanvasDiagnosticValue(
                                "requestedSampleCount",
                                seam.SampleCount)
                        }));
                    return false;
                }

                float maximumGap = 0f;
                for (int sample = 0; sample < commonCount; sample++)
                {
                    Vector3 positionA =
                        buffer.Vertices[boundaryA[sample]].Position;
                    Vector3 positionB =
                        buffer.Vertices[boundaryB[sample]].Position;
                    maximumGap = Mathf.Max(
                        maximumGap,
                        Vector3.Distance(positionA, positionB));
                }

                if (maximumGap > weldEpsilon)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.StitchPositionMismatch,
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

                for (int sample = 0; sample < commonCount; sample++)
                {
                    buffer.UnionTopology(
                        boundaryA[sample],
                        boundaryB[sample]);
                }

                buffer.SnapWeldedTopologyPositions();
            }

            return true;
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

        private static bool TryResolveBoundary(
            BoundaryReference reference,
            string seamId,
            string operationId,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            out List<int> boundary)
        {
            boundary = null;
            if (!buffer.TryGetPanel(
                    reference.PanelId,
                    out PanelBuildRecord panel) ||
                !panel.TryGetBoundary(
                    reference.BoundaryId,
                    out BoundaryBuildRecord sourceBoundary))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.StitchBoundaryMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "A Stitch seam references a panel boundary that does not exist.",
                    reference.PanelId,
                    operationId,
                    seamId));
                return false;
            }

            boundary = new List<int>(sourceBoundary.VertexIndices);
            return true;
        }

        private static void RemoveRepeatedTerminalTopologySample(
            List<int> boundary,
            MeshBuildBuffer buffer)
        {
            if (boundary.Count > 1 &&
                buffer.GetTopologyId(boundary[0]) ==
                buffer.GetTopologyId(boundary[boundary.Count - 1]))
            {
                boundary.RemoveAt(boundary.Count - 1);
            }
        }
    }
}
