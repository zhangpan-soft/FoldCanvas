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

            if (seam.SampleCount < 0)
            {
                AddSampleCountDiagnostic(
                    seam,
                    operationId,
                    boundaryA.VertexIndices.Count,
                    boundaryB.VertexIndices.Count,
                    result);
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

            List<float> parameters = new List<float>();
            AddParameters(parameters, pathA.Parameters);
            AddParameters(parameters, pathB.Parameters);
            AddUniformParameters(
                parameters,
                seam.SampleCount,
                pathA.IsClosed);
            parameters.Sort();
            RemoveNearDuplicateParameters(parameters);

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
            samples = new List<int>(parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
            {
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

                float parameter = parameters[i];
                if (TryFindExistingSample(
                    path,
                    parameter,
                    out int existingVertex))
                {
                    samples.Add(existingVertex);
                    continue;
                }

                if (!TryInsertSample(
                        path,
                        parameter,
                        buffer,
                        seam,
                        operationId,
                        result,
                        out int insertedVertex))
                {
                    return false;
                }

                samples.Add(insertedVertex);
            }

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

        private static bool TryFindExistingSample(
            BoundaryPath path,
            float parameter,
            out int vertexIndex)
        {
            for (int i = 0; i < path.Parameters.Length; i++)
            {
                if (Mathf.Abs(path.Parameters[i] - parameter) <=
                    ParameterTolerance)
                {
                    vertexIndex = path.OrderedIndices[i];
                    return true;
                }
            }

            vertexIndex = -1;
            return false;
        }

        private static bool TryInsertSample(
            BoundaryPath path,
            float parameter,
            MeshBuildBuffer buffer,
            SeamDefinition seam,
            string operationId,
            FoldCanvasCompileResult result,
            out int insertedVertex)
        {
            insertedVertex = -1;
            float targetDistance = parameter * path.TotalLength;
            int segmentCount = path.CumulativeLengths.Length - 1;
            int segmentIndex = segmentCount - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                if (targetDistance <
                    path.CumulativeLengths[i + 1] -
                    FoldCanvasGeometryTolerances
                        .MinimumStitchBoundaryLength)
                {
                    segmentIndex = i;
                    break;
                }
            }

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
                FoldCanvasGeometryTolerances.MinimumStitchBoundaryLength)
            {
                AddSubdivisionDiagnostic(
                    path,
                    seam,
                    operationId,
                    parameter,
                    result);
                return false;
            }

            if (!TryFindUniqueIncidentTriangle(
                    buffer,
                    fromIndex,
                    toIndex,
                    out int triangleOffset,
                    out int orientedFrom,
                    out int orientedTo,
                    out int thirdVertex))
            {
                AddSubdivisionDiagnostic(
                    path,
                    seam,
                    operationId,
                    parameter,
                    result);
                return false;
            }

            float alpha =
                (targetDistance -
                    path.CumulativeLengths[segmentIndex]) /
                segmentLength;
            MeshBuildVertex from = buffer.Vertices[fromIndex];
            MeshBuildVertex to = buffer.Vertices[toIndex];
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
            SphericalWrapBuildRecord sphericalWrap = null;
            if (buffer.TryGetSphericalWrap(
                    path.Panel.PanelIndex,
                    out sphericalWrap))
            {
                sourcePosition =
                    sphericalWrap.InterpolateBoundarySourcePosition(
                        path.Boundary.Id,
                        from.SourcePosition,
                        to.SourcePosition,
                        alpha);
                sourceUv =
                    sphericalWrap.EvaluateSourceUv(sourcePosition);
                Vector3 sphericalPosition =
                    sphericalWrap.Evaluate(sourcePosition);
                if (!FiniteMath.IsFinite(sphericalPosition))
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

                currentPosition = sphericalPosition;
            }

            insertedVertex = buffer.AddVertex(
                currentPosition,
                sourcePosition,
                sourceUv,
                path.Panel.PanelIndex);
            sphericalWrap?.RegisterSurfaceVertex(insertedVertex);

            buffer.Triangles[triangleOffset] = orientedFrom;
            buffer.Triangles[triangleOffset + 1] = insertedVertex;
            buffer.Triangles[triangleOffset + 2] = thirdVertex;
            buffer.Triangles.Add(insertedVertex);
            buffer.Triangles.Add(orientedTo);
            buffer.Triangles.Add(thirdVertex);

            InsertIntoAuthoredBoundary(
                path.Boundary.VertexIndices,
                fromIndex,
                toIndex,
                insertedVertex,
                path.Boundary.IsClosed);
            return true;
        }

        private static bool TryFindUniqueIncidentTriangle(
            MeshBuildBuffer buffer,
            int first,
            int second,
            out int triangleOffset,
            out int orientedFrom,
            out int orientedTo,
            out int thirdVertex)
        {
            triangleOffset = -1;
            orientedFrom = -1;
            orientedTo = -1;
            thirdVertex = -1;
            int matches = 0;
            for (int offset = 0;
                offset < buffer.Triangles.Count;
                offset += 3)
            {
                int a = buffer.Triangles[offset];
                int b = buffer.Triangles[offset + 1];
                int c = buffer.Triangles[offset + 2];
                int[] triangle = { a, b, c };
                for (int edge = 0; edge < 3; edge++)
                {
                    int from = triangle[edge];
                    int to = triangle[(edge + 1) % 3];
                    if (!((from == first && to == second) ||
                        (from == second && to == first)))
                    {
                        continue;
                    }

                    matches++;
                    triangleOffset = offset;
                    orientedFrom = from;
                    orientedTo = to;
                    thirdVertex = triangle[(edge + 2) % 3];
                }
            }

            return matches == 1;
        }

        private static void InsertIntoAuthoredBoundary(
            List<int> indices,
            int first,
            int second,
            int inserted,
            bool explicitlyClosed)
        {
            for (int i = 0; i < indices.Count - 1; i++)
            {
                if ((indices[i] == first && indices[i + 1] == second) ||
                    (indices[i] == second && indices[i + 1] == first))
                {
                    indices.Insert(i + 1, inserted);
                    return;
                }
            }

            if (explicitlyClosed &&
                ((indices[indices.Count - 1] == first &&
                    indices[0] == second) ||
                 (indices[indices.Count - 1] == second &&
                    indices[0] == first)))
            {
                indices.Add(inserted);
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
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.StitchSampleCountMismatch,
                FoldCanvasDiagnosticSeverity.Error,
                "Stitch sampleCount cannot be negative.",
                operationId: operationId,
                seamId: seam.Id,
                values: new[]
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
                }));
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
