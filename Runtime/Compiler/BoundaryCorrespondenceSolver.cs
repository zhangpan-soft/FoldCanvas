using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal sealed class SeamCorrespondence
    {
        public SeamCorrespondence(
            List<int> boundaryA,
            List<int> boundaryB,
            bool isClosed)
        {
            BoundaryA = boundaryA;
            BoundaryB = boundaryB;
            IsClosed = isClosed;
        }

        public List<int> BoundaryA { get; }

        public List<int> BoundaryB { get; }

        public bool IsClosed { get; }
    }

    internal static class BoundaryCorrespondenceSolver
    {
        private const float ParameterTolerance = 0.000001f;

        public static bool TryCreate(
            SeamDefinition seam,
            string operationId,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            out SeamCorrespondence correspondence)
        {
            correspondence = null;
            if (!TryResolveBoundary(
                    seam.A,
                    seam.Id,
                    operationId,
                    buffer,
                    result,
                    out PanelBuildRecord panelA,
                    out BoundaryBuildRecord boundaryA) ||
                !TryResolveBoundary(
                    seam.B,
                    seam.Id,
                    operationId,
                    buffer,
                    result,
                    out PanelBuildRecord panelB,
                    out BoundaryBuildRecord boundaryB))
            {
                return false;
            }

            if (!ValidateSampleCount(
                    seam,
                    operationId,
                    boundaryA.VertexIndices.Count,
                    boundaryB.VertexIndices.Count,
                    result))
            {
                return false;
            }

            if (!TryBuildPath(
                    panelA,
                    boundaryA,
                    false,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out BoundaryPath pathA) ||
                !TryBuildPath(
                    panelB,
                    boundaryB,
                    seam.ReverseB,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out BoundaryPath pathB))
            {
                return false;
            }

            if (pathA.IsClosed != pathB.IsClosed)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.StitchBoundaryClosureMismatch,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch boundaries must both be open chains or both be closed loops.",
                    operationId: operationId,
                    seamId: seam.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "boundaryAClosed",
                            pathA.IsClosed ? 1d : 0d),
                        new FoldCanvasDiagnosticValue(
                            "boundaryBClosed",
                            pathB.IsClosed ? 1d : 0d)
                    }));
                return false;
            }

            List<float> parameters = BuildCommonParameters(
                pathA,
                pathB,
                seam.SampleCount);

            if (!TryEnsureParameters(
                    panelA,
                    boundaryA,
                    false,
                    parameters,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out List<int> samplesA) ||
                !TryEnsureParameters(
                    panelB,
                    boundaryB,
                    seam.ReverseB,
                    parameters,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out List<int> samplesB))
            {
                return false;
            }

            correspondence = new SeamCorrespondence(
                samplesA,
                samplesB,
                pathA.IsClosed);
            return true;
        }

        public static bool TryEstimateAdditionalGeometry(
            SeamDefinition seam,
            string operationId,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            out long additionalVertices,
            out long additionalTriangles)
        {
            additionalVertices = 0L;
            additionalTriangles = 0L;
            if (!TryResolveBoundary(
                    seam.A,
                    seam.Id,
                    operationId,
                    buffer,
                    result,
                    out PanelBuildRecord panelA,
                    out BoundaryBuildRecord boundaryA) ||
                !TryResolveBoundary(
                    seam.B,
                    seam.Id,
                    operationId,
                    buffer,
                    result,
                    out PanelBuildRecord panelB,
                    out BoundaryBuildRecord boundaryB))
            {
                return false;
            }

            if (!ValidateSampleCount(
                    seam,
                    operationId,
                    boundaryA.VertexIndices.Count,
                    boundaryB.VertexIndices.Count,
                    result))
            {
                return false;
            }

            if (!TryBuildPath(
                    panelA,
                    boundaryA,
                    false,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out BoundaryPath pathA) ||
                !TryBuildPath(
                    panelB,
                    boundaryB,
                    seam.ReverseB,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out BoundaryPath pathB))
            {
                return false;
            }

            if (pathA.IsClosed != pathB.IsClosed)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .StitchBoundaryClosureMismatch,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch boundaries must both be open chains or both be closed loops.",
                    operationId: operationId,
                    seamId: seam.Id));
                return false;
            }

            List<float> parameters = BuildCommonParameters(
                pathA,
                pathB,
                seam.SampleCount);
            try
            {
                checked
                {
                    additionalVertices =
                        CountMissingParameters(pathA, parameters) +
                        CountMissingParameters(pathB, parameters);
                    additionalTriangles = additionalVertices;
                    if (seam.Mode == SeamMode.Bridge)
                    {
                        long segmentCount = pathA.IsClosed
                            ? parameters.Count
                            : parameters.Count - 1L;
                        additionalTriangles +=
                            2L * segmentCount;
                    }
                }
            }
            catch (OverflowException)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .GeometryBudgetOverflow,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Stitch geometry preflight overflowed its count representation.",
                    operationId: operationId,
                    seamId: seam.Id));
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
            out PanelBuildRecord panel,
            out BoundaryBuildRecord boundary)
        {
            boundary = null;
            if (!buffer.TryGetPanel(reference.PanelId, out panel) ||
                !panel.TryGetBoundary(reference.BoundaryId, out boundary))
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

            return true;
        }

        private static bool TryEnsureParameters(
            PanelBuildRecord panel,
            BoundaryBuildRecord boundary,
            bool reverse,
            IReadOnlyList<float> parameters,
            MeshBuildBuffer buffer,
            SeamDefinition seam,
            string operationId,
            FoldCanvasCompileResult result,
            out List<int> samples)
        {
            samples = null;
            if (!TryBuildPath(
                    panel,
                    boundary,
                    reverse,
                    buffer,
                    seam,
                    operationId,
                    result,
                    out BoundaryPath path))
            {
                return false;
            }

            int[] sampleIndices = new int[parameters.Count];
            List<BoundarySampleInsertion>[] insertionsBySegment =
                new List<BoundarySampleInsertion>[
                    path.CumulativeLengths.Length - 1];
            for (int parameterIndex = 0;
                parameterIndex < parameters.Count;
                parameterIndex++)
            {
                float parameter = parameters[parameterIndex];
                int existingIndex =
                    FindExistingSampleIndex(path, parameter);
                if (existingIndex >= 0)
                {
                    sampleIndices[parameterIndex] =
                        path.OrderedIndices[existingIndex];
                    continue;
                }

                int segmentIndex =
                    FindSegmentIndex(path, parameter);
                List<BoundarySampleInsertion> segmentInsertions =
                    insertionsBySegment[segmentIndex];
                if (segmentInsertions == null)
                {
                    segmentInsertions =
                        new List<BoundarySampleInsertion>();
                    insertionsBySegment[segmentIndex] =
                        segmentInsertions;
                }

                segmentInsertions.Add(
                    new BoundarySampleInsertion(
                        parameterIndex,
                        parameter));
            }

            TriangleAdjacencyIndex adjacency =
                new TriangleAdjacencyIndex(buffer);
            for (int segmentIndex = 0;
                segmentIndex < insertionsBySegment.Length;
                segmentIndex++)
            {
                List<BoundarySampleInsertion> segmentInsertions =
                    insertionsBySegment[segmentIndex];
                if (segmentInsertions == null)
                {
                    continue;
                }

                if (!TryInsertSegmentSamples(
                        path,
                        segmentIndex,
                        segmentInsertions,
                        buffer,
                        adjacency,
                        seam,
                        operationId,
                        result,
                        sampleIndices))
                {
                    return false;
                }
            }

            samples = new List<int>(sampleIndices);
            return true;
        }

        private static bool TryBuildPath(
            PanelBuildRecord panel,
            BoundaryBuildRecord boundary,
            bool reverse,
            MeshBuildBuffer buffer,
            SeamDefinition seam,
            string operationId,
            FoldCanvasCompileResult result,
            out BoundaryPath path)
        {
            path = null;
            List<int> ordered = new List<int>(boundary.VertexIndices);
            if (reverse)
            {
                ordered.Reverse();
            }

            bool repeatedTerminal =
                ordered.Count > 1 &&
                buffer.GetTopologyId(ordered[0]) ==
                buffer.GetTopologyId(ordered[ordered.Count - 1]);
            bool isClosed = boundary.IsClosed || repeatedTerminal;
            int segmentCount = isClosed
                ? (repeatedTerminal ? ordered.Count - 1 : ordered.Count)
                : ordered.Count - 1;
            if (segmentCount <= 0)
            {
                AddZeroLengthDiagnostic(
                    panel,
                    boundary,
                    seam,
                    operationId,
                    result);
                return false;
            }

            float[] cumulative = new float[segmentCount + 1];
            for (int segment = 0; segment < segmentCount; segment++)
            {
                int next = repeatedTerminal
                    ? segment + 1
                    : (segment + 1) % ordered.Count;
                if (!isClosed)
                {
                    next = segment + 1;
                }

                MeshBuildVertex fromVertex =
                    buffer.Vertices[ordered[segment]];
                MeshBuildVertex toVertex =
                    buffer.Vertices[ordered[next]];
                float length;
                if (!buffer.TryGetSphericalWrap(
                        panel.PanelIndex,
                        out SphericalWrapBuildRecord sphericalWrap) ||
                    !sphericalWrap.TryEvaluateBoundaryArcLength(
                        boundary.Id,
                        fromVertex.SourcePosition,
                        toVertex.SourcePosition,
                        out length))
                {
                    length = Vector3.Distance(
                        fromVertex.Position,
                        toVertex.Position);
                }

                if (!FiniteMath.IsFinite(length))
                {
                    AddZeroLengthDiagnostic(
                        panel,
                        boundary,
                        seam,
                        operationId,
                        result);
                    return false;
                }

                cumulative[segment + 1] =
                    cumulative[segment] + length;
            }

            float totalLength = cumulative[segmentCount];
            if (!FiniteMath.IsFinite(totalLength) ||
                totalLength <=
                    FoldCanvasGeometryTolerances.MinimumStitchBoundaryLength)
            {
                AddZeroLengthDiagnostic(
                    panel,
                    boundary,
                    seam,
                    operationId,
                    result);
                return false;
            }

            int parameterCount = isClosed
                ? segmentCount
                : segmentCount + 1;
            float[] normalized = new float[parameterCount];
            for (int i = 0; i < parameterCount; i++)
            {
                normalized[i] = cumulative[i] / totalLength;
            }

            path = new BoundaryPath(
                panel,
                boundary,
                ordered,
                repeatedTerminal,
                isClosed,
                cumulative,
                normalized,
                totalLength);
            return true;
        }

        private static int FindExistingSampleIndex(
            BoundaryPath path,
            float parameter)
        {
            int low = 0;
            int high = path.Parameters.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                float difference =
                    path.Parameters[middle] - parameter;
                if (Mathf.Abs(difference) <= ParameterTolerance)
                {
                    return middle;
                }

                if (difference < 0f)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return -1;
        }

        private static int FindSegmentIndex(
            BoundaryPath path,
            float parameter)
        {
            float targetDistance = parameter * path.TotalLength;
            int segmentCount = path.CumulativeLengths.Length - 1;
            int low = 0;
            int high = segmentCount - 1;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (targetDistance <
                    path.CumulativeLengths[middle + 1] -
                    FoldCanvasGeometryTolerances
                        .MinimumStitchBoundaryLength)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }

        private static bool TryInsertSegmentSamples(
            BoundaryPath path,
            int segmentIndex,
            IReadOnlyList<BoundarySampleInsertion> insertions,
            MeshBuildBuffer buffer,
            TriangleAdjacencyIndex adjacency,
            SeamDefinition seam,
            string operationId,
            FoldCanvasCompileResult result,
            int[] sampleIndices)
        {
            int nextIndex = path.RepeatedTerminal
                ? segmentIndex + 1
                : (segmentIndex + 1) % path.OrderedIndices.Count;
            if (!path.IsClosed)
            {
                nextIndex = segmentIndex + 1;
            }

            int fromIndex = path.OrderedIndices[segmentIndex];
            int toIndex = path.OrderedIndices[nextIndex];
            float segmentLength =
                path.CumulativeLengths[segmentIndex + 1] -
                path.CumulativeLengths[segmentIndex];
            if (segmentLength <=
                FoldCanvasGeometryTolerances.MinimumStitchBoundaryLength ||
                !adjacency.TryGetUnique(
                    fromIndex,
                    toIndex,
                    out TriangleEdgeOccurrence occurrence))
            {
                AddSubdivisionDiagnostic(
                    path,
                    seam,
                    operationId,
                    insertions[0].Parameter,
                    result);
                return false;
            }

            MeshBuildVertex from = buffer.Vertices[fromIndex];
            MeshBuildVertex to = buffer.Vertices[toIndex];
            List<int> insertedVertices =
                new List<int>(insertions.Count);
            SphericalWrapBuildRecord sphericalWrap = null;
            buffer.TryGetSphericalWrap(
                path.Panel.PanelIndex,
                out sphericalWrap);
            for (int i = 0; i < insertions.Count; i++)
            {
                BoundarySampleInsertion insertion = insertions[i];
                float targetDistance =
                    insertion.Parameter * path.TotalLength;
                float alpha =
                    (targetDistance -
                        path.CumulativeLengths[segmentIndex]) /
                    segmentLength;
                Vector2 sourcePosition = Vector2.LerpUnclamped(
                    from.SourcePosition,
                    to.SourcePosition,
                    alpha);
                Vector2 sourceUv = Vector2.LerpUnclamped(
                    from.SourceUv,
                    to.SourceUv,
                    alpha);
                Vector3 currentPosition = Vector3.LerpUnclamped(
                    from.Position,
                    to.Position,
                    alpha);
                if (sphericalWrap != null)
                {
                    sourcePosition =
                        sphericalWrap.InterpolateBoundarySourcePosition(
                            path.Boundary.Id,
                            from.SourcePosition,
                            to.SourcePosition,
                            alpha);
                    sourceUv =
                        sphericalWrap.EvaluateSourceUv(sourcePosition);
                    currentPosition =
                        sphericalWrap.Evaluate(sourcePosition);
                    if (!FiniteMath.IsFinite(currentPosition))
                    {
                        result.Add(new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .NonFiniteSphericalWrapParameter,
                            FoldCanvasDiagnosticSeverity.Error,
                            "Spherical seam subdivision produced a non-finite mapped position.",
                            path.Panel.PanelId,
                            operationId,
                            seam.Id));
                        return false;
                    }
                }

                int insertedVertex = buffer.AddVertex(
                    currentPosition,
                    sourcePosition,
                    sourceUv,
                    path.Panel.PanelIndex);
                sphericalWrap?.RegisterSurfaceVertex(insertedVertex);
                insertedVertices.Add(insertedVertex);
                sampleIndices[insertion.ParameterIndex] =
                    insertedVertex;
            }

            List<int> orientedChain =
                new List<int>(insertedVertices.Count + 2);
            bool sameOrientation =
                occurrence.From == fromIndex &&
                occurrence.To == toIndex;
            if (sameOrientation)
            {
                orientedChain.Add(fromIndex);
                orientedChain.AddRange(insertedVertices);
                orientedChain.Add(toIndex);
            }
            else
            {
                orientedChain.Add(toIndex);
                for (int i = insertedVertices.Count - 1; i >= 0; i--)
                {
                    orientedChain.Add(insertedVertices[i]);
                }

                orientedChain.Add(fromIndex);
            }

            adjacency.ReplaceTriangle(
                occurrence.TriangleOffset,
                orientedChain[0],
                orientedChain[1],
                occurrence.Third);
            for (int i = 1; i < orientedChain.Count - 1; i++)
            {
                adjacency.AppendTriangle(
                    orientedChain[i],
                    orientedChain[i + 1],
                    occurrence.Third);
            }

            InsertChainIntoAuthoredBoundary(
                path.Boundary.VertexIndices,
                fromIndex,
                toIndex,
                insertedVertices,
                path.Boundary.IsClosed);
            return true;
        }

        private static void InsertChainIntoAuthoredBoundary(
            List<int> indices,
            int pathFrom,
            int pathTo,
            IReadOnlyList<int> insertedPathOrder,
            bool explicitlyClosed)
        {
            for (int i = 0; i < indices.Count - 1; i++)
            {
                if (indices[i] == pathFrom &&
                    indices[i + 1] == pathTo)
                {
                    InsertRange(
                        indices,
                        i + 1,
                        insertedPathOrder,
                        false);
                    return;
                }

                if (indices[i] == pathTo &&
                    indices[i + 1] == pathFrom)
                {
                    InsertRange(
                        indices,
                        i + 1,
                        insertedPathOrder,
                        true);
                    return;
                }
            }

            if (!explicitlyClosed)
            {
                return;
            }

            int last = indices.Count - 1;
            if (indices[last] == pathFrom && indices[0] == pathTo)
            {
                InsertRange(
                    indices,
                    indices.Count,
                    insertedPathOrder,
                    false);
            }
            else if (
                indices[last] == pathTo &&
                indices[0] == pathFrom)
            {
                InsertRange(
                    indices,
                    indices.Count,
                    insertedPathOrder,
                    true);
            }
        }

        private static void InsertRange(
            List<int> indices,
            int insertionIndex,
            IReadOnlyList<int> insertedPathOrder,
            bool reverse)
        {
            if (!reverse)
            {
                for (int i = 0; i < insertedPathOrder.Count; i++)
                {
                    indices.Insert(
                        insertionIndex + i,
                        insertedPathOrder[i]);
                }

                return;
            }

            for (int i = insertedPathOrder.Count - 1; i >= 0; i--)
            {
                indices.Insert(
                    insertionIndex +
                        (insertedPathOrder.Count - 1 - i),
                    insertedPathOrder[i]);
            }
        }

        private static void AddParameters(
            List<float> destination,
            IReadOnlyList<float> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                destination.Add(source[i]);
            }
        }

        private static List<float> BuildCommonParameters(
            BoundaryPath pathA,
            BoundaryPath pathB,
            int sampleCount)
        {
            List<float> parameters = new List<float>();
            AddParameters(parameters, pathA.Parameters);
            AddParameters(parameters, pathB.Parameters);
            AddUniformParameters(
                parameters,
                sampleCount,
                pathA.IsClosed);
            parameters.Sort();
            RemoveNearDuplicateParameters(parameters);
            return parameters;
        }

        private static long CountMissingParameters(
            BoundaryPath path,
            IReadOnlyList<float> parameters)
        {
            long missing = 0L;
            int existingIndex = 0;
            for (int parameterIndex = 0;
                parameterIndex < parameters.Count;
                parameterIndex++)
            {
                float parameter = parameters[parameterIndex];
                while (existingIndex < path.Parameters.Length &&
                    path.Parameters[existingIndex] <
                        parameter - ParameterTolerance)
                {
                    existingIndex++;
                }

                bool exists =
                    existingIndex < path.Parameters.Length &&
                    Mathf.Abs(
                        path.Parameters[existingIndex] -
                        parameter) <= ParameterTolerance;
                if (!exists)
                {
                    missing++;
                }
            }

            return missing;
        }

        private static bool ValidateSampleCount(
            SeamDefinition seam,
            string operationId,
            int boundaryACount,
            int boundaryBCount,
            FoldCanvasCompileResult result)
        {
            if (seam.SampleCount >=
                    FoldCanvasLimits.MinimumStitchSampleCount &&
                seam.SampleCount <=
                    FoldCanvasLimits.MaximumStitchSampleCount)
            {
                return true;
            }

            AddSampleCountDiagnostic(
                seam,
                operationId,
                boundaryACount,
                boundaryBCount,
                result);
            return false;
        }

        private static void AddUniformParameters(
            List<float> parameters,
            int sampleCount,
            bool isClosed)
        {
            if (sampleCount <= 0)
            {
                return;
            }

            if (isClosed)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    parameters.Add(i / (float)sampleCount);
                }

                return;
            }

            if (sampleCount == 1)
            {
                parameters.Add(0.5f);
                return;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                parameters.Add(i / (float)(sampleCount - 1));
            }
        }

        private static void RemoveNearDuplicateParameters(
            List<float> parameters)
        {
            for (int i = parameters.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(parameters[i] - parameters[i - 1]) <=
                    ParameterTolerance)
                {
                    parameters.RemoveAt(i);
                }
            }
        }

        private static void AddZeroLengthDiagnostic(
            PanelBuildRecord panel,
            BoundaryBuildRecord boundary,
            SeamDefinition seam,
            string operationId,
            FoldCanvasCompileResult result)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.ZeroLengthStitchBoundary,
                FoldCanvasDiagnosticSeverity.Error,
                $"Stitch boundary '{boundary.Id}' has no usable current-space length.",
                panel.PanelId,
                operationId,
                seam.Id));
        }

        private static void AddSubdivisionDiagnostic(
            BoundaryPath path,
            SeamDefinition seam,
            string operationId,
            float parameter,
            FoldCanvasCompileResult result)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.StitchBoundarySubdivisionFailed,
                FoldCanvasDiagnosticSeverity.Error,
                $"Stitch boundary '{path.Boundary.Id}' cannot be subdivided through exactly one adjacent source-surface triangle.",
                path.Panel.PanelId,
                operationId,
                seam.Id,
                new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "parameter",
                        parameter)
                }));
        }

        private static void AddSampleCountDiagnostic(
            SeamDefinition seam,
            string operationId,
            int boundaryACount,
            int boundaryBCount,
            FoldCanvasCompileResult result)
        {
            bool isBelowMinimum =
                seam.SampleCount <
                FoldCanvasLimits.MinimumStitchSampleCount;
            List<FoldCanvasDiagnosticValue> values =
                new List<FoldCanvasDiagnosticValue>
                {
                    new FoldCanvasDiagnosticValue(
                        "boundaryACount",
                        boundaryACount),
                    new FoldCanvasDiagnosticValue(
                        "boundaryBCount",
                        boundaryBCount),
                    new FoldCanvasDiagnosticValue(
                        "requestedSampleCount",
                        seam.SampleCount)
                };
            if (!isBelowMinimum)
            {
                values.Add(new FoldCanvasDiagnosticValue(
                    "maximumSampleCount",
                    FoldCanvasLimits.MaximumStitchSampleCount));
            }

            result.Add(new FoldCanvasDiagnostic(
                isBelowMinimum
                    ? FoldCanvasDiagnosticCodes
                        .StitchSampleCountMismatch
                    : FoldCanvasDiagnosticCodes
                        .StitchSampleCountOutOfRange,
                FoldCanvasDiagnosticSeverity.Error,
                $"Stitch sampleCount must be between {FoldCanvasLimits.MinimumStitchSampleCount} and {FoldCanvasLimits.MaximumStitchSampleCount}.",
                operationId: operationId,
                seamId: seam.Id,
                values: values));
        }

        private readonly struct BoundarySampleInsertion
        {
            public BoundarySampleInsertion(
                int parameterIndex,
                float parameter)
            {
                ParameterIndex = parameterIndex;
                Parameter = parameter;
            }

            public int ParameterIndex { get; }

            public float Parameter { get; }
        }

        private readonly struct TriangleEdgeKey :
            IEquatable<TriangleEdgeKey>
        {
            public TriangleEdgeKey(int first, int second)
            {
                Minimum = Math.Min(first, second);
                Maximum = Math.Max(first, second);
            }

            private int Minimum { get; }

            private int Maximum { get; }

            public bool Equals(TriangleEdgeKey other)
            {
                return
                    Minimum == other.Minimum &&
                    Maximum == other.Maximum;
            }

            public override bool Equals(object obj)
            {
                return
                    obj is TriangleEdgeKey other &&
                    Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Minimum * 397) ^ Maximum;
                }
            }
        }

        private readonly struct TriangleEdgeOccurrence
        {
            public TriangleEdgeOccurrence(
                int triangleOffset,
                int from,
                int to,
                int third)
            {
                TriangleOffset = triangleOffset;
                From = from;
                To = to;
                Third = third;
            }

            public int TriangleOffset { get; }

            public int From { get; }

            public int To { get; }

            public int Third { get; }
        }

        private sealed class TriangleAdjacencyIndex
        {
            private readonly MeshBuildBuffer buffer;

            private readonly Dictionary<
                TriangleEdgeKey,
                List<TriangleEdgeOccurrence>> occurrences =
                    new Dictionary<
                        TriangleEdgeKey,
                        List<TriangleEdgeOccurrence>>();

            public TriangleAdjacencyIndex(MeshBuildBuffer buffer)
            {
                this.buffer = buffer;
                for (int offset = 0;
                    offset < buffer.Triangles.Count;
                    offset += 3)
                {
                    AddTriangle(offset);
                }
            }

            public bool TryGetUnique(
                int first,
                int second,
                out TriangleEdgeOccurrence occurrence)
            {
                if (occurrences.TryGetValue(
                        new TriangleEdgeKey(first, second),
                        out List<TriangleEdgeOccurrence> matches) &&
                    matches.Count == 1)
                {
                    occurrence = matches[0];
                    return true;
                }

                occurrence = default;
                return false;
            }

            public void ReplaceTriangle(
                int offset,
                int first,
                int second,
                int third)
            {
                RemoveTriangle(offset);
                buffer.Triangles[offset] = first;
                buffer.Triangles[offset + 1] = second;
                buffer.Triangles[offset + 2] = third;
                AddTriangle(offset);
            }

            public void AppendTriangle(
                int first,
                int second,
                int third)
            {
                int offset = buffer.Triangles.Count;
                buffer.Triangles.Add(first);
                buffer.Triangles.Add(second);
                buffer.Triangles.Add(third);
                AddTriangle(offset);
            }

            private void AddTriangle(int offset)
            {
                int first = buffer.Triangles[offset];
                int second = buffer.Triangles[offset + 1];
                int third = buffer.Triangles[offset + 2];
                AddOccurrence(new TriangleEdgeOccurrence(
                    offset,
                    first,
                    second,
                    third));
                AddOccurrence(new TriangleEdgeOccurrence(
                    offset,
                    second,
                    third,
                    first));
                AddOccurrence(new TriangleEdgeOccurrence(
                    offset,
                    third,
                    first,
                    second));
            }

            private void AddOccurrence(
                TriangleEdgeOccurrence occurrence)
            {
                TriangleEdgeKey key = new TriangleEdgeKey(
                    occurrence.From,
                    occurrence.To);
                if (!occurrences.TryGetValue(
                        key,
                        out List<TriangleEdgeOccurrence> matches))
                {
                    matches = new List<TriangleEdgeOccurrence>();
                    occurrences.Add(key, matches);
                }

                matches.Add(occurrence);
            }

            private void RemoveTriangle(int offset)
            {
                int first = buffer.Triangles[offset];
                int second = buffer.Triangles[offset + 1];
                int third = buffer.Triangles[offset + 2];
                RemoveOccurrence(
                    new TriangleEdgeKey(first, second),
                    offset);
                RemoveOccurrence(
                    new TriangleEdgeKey(second, third),
                    offset);
                RemoveOccurrence(
                    new TriangleEdgeKey(third, first),
                    offset);
            }

            private void RemoveOccurrence(
                TriangleEdgeKey key,
                int triangleOffset)
            {
                if (!occurrences.TryGetValue(
                        key,
                        out List<TriangleEdgeOccurrence> matches))
                {
                    return;
                }

                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    if (matches[i].TriangleOffset == triangleOffset)
                    {
                        matches.RemoveAt(i);
                    }
                }

                if (matches.Count == 0)
                {
                    occurrences.Remove(key);
                }
            }
        }

        private sealed class BoundaryPath
        {
            public BoundaryPath(
                PanelBuildRecord panel,
                BoundaryBuildRecord boundary,
                List<int> orderedIndices,
                bool repeatedTerminal,
                bool isClosed,
                float[] cumulativeLengths,
                float[] parameters,
                float totalLength)
            {
                Panel = panel;
                Boundary = boundary;
                OrderedIndices = orderedIndices;
                RepeatedTerminal = repeatedTerminal;
                IsClosed = isClosed;
                CumulativeLengths = cumulativeLengths;
                Parameters = parameters;
                TotalLength = totalLength;
            }

            public PanelBuildRecord Panel { get; }

            public BoundaryBuildRecord Boundary { get; }

            public List<int> OrderedIndices { get; }

            public bool RepeatedTerminal { get; }

            public bool IsClosed { get; }

            public float[] CumulativeLengths { get; }

            public float[] Parameters { get; }

            public float TotalLength { get; }
        }
    }
}
