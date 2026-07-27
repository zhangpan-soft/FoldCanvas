using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal static class FoldLineExecutor
    {
        public static bool TryExecute(
            FoldLineOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (!buffer.TryGetPanel(operation.PanelId, out PanelBuildRecord panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.FoldTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Fold references a panel that does not exist.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            Vector2 lineStart = operation.LineStart;
            Vector2 lineEnd = operation.LineEnd;
            if (!FiniteMath.IsFinite(lineStart) || !FiniteMath.IsFinite(lineEnd))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonFiniteFoldLine,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Fold line coordinates contain NaN or infinity.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            float sourceTolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            if (!IsInsideNormalizedPanel(lineStart, sourceTolerance) ||
                !IsInsideNormalizedPanel(lineEnd, sourceTolerance))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.FoldLineOutOfRange,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Fold line coordinates must remain inside normalized panel coordinates [0,1]^2.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            lineStart = ClampNormalized(lineStart);
            lineEnd = ClampNormalized(lineEnd);
            Vector2 sourceAxis = lineEnd - lineStart;
            float sourceAxisLength = sourceAxis.magnitude;
            if (sourceAxisLength <= sourceTolerance)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.DegenerateFoldLine,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Fold line start and end must define a non-degenerate directed line.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (!FiniteMath.IsFinite(operation.AngleDegrees))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonFiniteFoldAngle,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Fold angle contains NaN or infinity.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (!FiniteMath.IsFinite(operation.Falloff) || operation.Falloff != 0f)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedFoldFalloff,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M02 supports rigid creases only; fold falloff must be exactly zero.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.Side != FoldSide.Positive &&
                operation.Side != FoldSide.Negative)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidFoldSide,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Fold side must be Positive or Negative.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (!IsExistingContinuousEdgeChain(
                    panel,
                    buffer,
                    lineStart,
                    lineEnd))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.FoldCreaseRequiresTopologySplit,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The fold crease is not a continuous chain of existing mesh edges; deterministic topology splitting is required before this crease can execute.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (!TryResolveCurrentHinge(
                    panel,
                    buffer,
                    lineStart,
                    lineEnd,
                    out Vector3 hingeStart,
                    out Vector3 hingeEnd,
                    out string hingeFailure))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.AmbiguousFoldHinge,
                    FoldCanvasDiagnosticSeverity.Error,
                    hingeFailure,
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.AngleDegrees == 0f)
            {
                return true;
            }

            Vector3 currentAxis = hingeEnd - hingeStart;
            Quaternion rotation = Quaternion.AngleAxis(
                operation.AngleDegrees,
                currentAxis.normalized);
            int vertexEnd = panel.VertexStart + panel.VertexCount;
            for (int i = panel.VertexStart; i < vertexEnd; i++)
            {
                MeshBuildVertex vertex = buffer.Vertices[i];
                Vector2 normalized = ToNormalized(panel, vertex.SourcePosition);
                float signedDistance =
                    Cross2(sourceAxis, normalized - lineStart) /
                    sourceAxisLength;

                bool selected = operation.Side == FoldSide.Positive
                    ? signedDistance > sourceTolerance
                    : signedDistance < -sourceTolerance;
                if (!selected)
                {
                    continue;
                }

                vertex.Position =
                    hingeStart + rotation * (vertex.Position - hingeStart);
                buffer.Vertices[i] = vertex;
            }

            return true;
        }

        private static bool IsExistingContinuousEdgeChain(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            Vector2 lineStart,
            Vector2 lineEnd)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            bool hasStartVertex = false;
            bool hasEndVertex = false;
            int vertexEnd = panel.VertexStart + panel.VertexCount;
            for (int i = panel.VertexStart; i < vertexEnd; i++)
            {
                Vector2 normalized = ToNormalized(
                    panel,
                    buffer.Vertices[i].SourcePosition);
                hasStartVertex |=
                    (normalized - lineStart).sqrMagnitude <=
                    tolerance * tolerance;
                hasEndVertex |=
                    (normalized - lineEnd).sqrMagnitude <=
                    tolerance * tolerance;
            }

            if (!hasStartVertex || !hasEndVertex)
            {
                return false;
            }

            List<CreaseInterval> intervals = new List<CreaseInterval>();
            int triangleEnd =
                panel.TriangleIndexStart + panel.TriangleIndexCount;
            for (int i = panel.TriangleIndexStart;
                i < triangleEnd;
                i += 3)
            {
                int a = buffer.Triangles[i];
                int b = buffer.Triangles[i + 1];
                int c = buffer.Triangles[i + 2];
                AddCollinearEdgeInterval(
                    panel,
                    buffer,
                    lineStart,
                    lineEnd,
                    a,
                    b,
                    intervals);
                AddCollinearEdgeInterval(
                    panel,
                    buffer,
                    lineStart,
                    lineEnd,
                    b,
                    c,
                    intervals);
                AddCollinearEdgeInterval(
                    panel,
                    buffer,
                    lineStart,
                    lineEnd,
                    c,
                    a,
                    intervals);
            }

            intervals.Sort(CompareIntervals);
            float coveredUntil = 0f;
            for (int i = 0; i < intervals.Count; i++)
            {
                CreaseInterval interval = intervals[i];
                if (interval.Start > coveredUntil + tolerance)
                {
                    return false;
                }

                coveredUntil = Mathf.Max(coveredUntil, interval.End);
                if (coveredUntil >= 1f - tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddCollinearEdgeInterval(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            Vector2 lineStart,
            Vector2 lineEnd,
            int edgeStartIndex,
            int edgeEndIndex,
            List<CreaseInterval> intervals)
        {
            Vector2 edgeStart = ToNormalized(
                panel,
                buffer.Vertices[edgeStartIndex].SourcePosition);
            Vector2 edgeEnd = ToNormalized(
                panel,
                buffer.Vertices[edgeEndIndex].SourcePosition);
            Vector2 lineDelta = lineEnd - lineStart;
            float lineLength = lineDelta.magnitude;
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            if (Mathf.Abs(Cross2(edgeStart - lineStart, lineDelta)) >
                    tolerance * lineLength ||
                Mathf.Abs(Cross2(edgeEnd - lineStart, lineDelta)) >
                    tolerance * lineLength)
            {
                return;
            }

            float lineLengthSquared = lineDelta.sqrMagnitude;
            float edgeStartParameter =
                Vector2.Dot(edgeStart - lineStart, lineDelta) /
                lineLengthSquared;
            float edgeEndParameter =
                Vector2.Dot(edgeEnd - lineStart, lineDelta) /
                lineLengthSquared;
            float intervalStart = Mathf.Max(
                0f,
                Mathf.Min(edgeStartParameter, edgeEndParameter));
            float intervalEnd = Mathf.Min(
                1f,
                Mathf.Max(edgeStartParameter, edgeEndParameter));
            if (intervalEnd - intervalStart <= tolerance)
            {
                return;
            }

            intervals.Add(new CreaseInterval(intervalStart, intervalEnd));
        }

        private static int CompareIntervals(
            CreaseInterval left,
            CreaseInterval right)
        {
            int startComparison = left.Start.CompareTo(right.Start);
            return startComparison != 0
                ? startComparison
                : left.End.CompareTo(right.End);
        }

        private static bool TryResolveCurrentHinge(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            Vector2 lineStart,
            Vector2 lineEnd,
            out Vector3 hingeStart,
            out Vector3 hingeEnd,
            out string failure)
        {
            List<float> breakpoints = new List<float> { 0f, 1f };
            AddTriangleEdgeBreakpoints(
                panel,
                buffer,
                lineStart,
                lineEnd,
                breakpoints);
            SortAndDeduplicate(breakpoints);

            List<float> sampleParameters =
                new List<float>(breakpoints.Count * 2 - 1);
            for (int i = 0; i < breakpoints.Count; i++)
            {
                sampleParameters.Add(breakpoints[i]);
                if (i + 1 < breakpoints.Count)
                {
                    float midpoint =
                        (breakpoints[i] + breakpoints[i + 1]) * 0.5f;
                    sampleParameters.Add(midpoint);
                }
            }

            Vector3[] mappedSamples = new Vector3[sampleParameters.Count];
            for (int i = 0; i < sampleParameters.Count; i++)
            {
                Vector2 sourcePoint = Vector2.LerpUnclamped(
                    lineStart,
                    lineEnd,
                    sampleParameters[i]);
                if (!TryMapSourcePoint(
                        panel,
                        buffer,
                        sourcePoint,
                        out mappedSamples[i]))
                {
                    hingeStart = default;
                    hingeEnd = default;
                    failure =
                        "The complete fold line cannot be mapped through the target panel's source triangulation.";
                    return false;
                }
            }

            hingeStart = mappedSamples[0];
            hingeEnd = mappedSamples[mappedSamples.Length - 1];
            Vector3 axis = hingeEnd - hingeStart;
            float axisLength = axis.magnitude;
            if (!FiniteMath.IsFinite(hingeStart) ||
                !FiniteMath.IsFinite(hingeEnd) ||
                axisLength <= FoldCanvasGeometryTolerances.MinimumCurrentHingeLength)
            {
                failure =
                    "The current fold hinge is collapsed or contains non-finite coordinates.";
                return false;
            }

            Vector3 axisDirection = axis / axisLength;
            float currentTolerance = Mathf.Max(
                FoldCanvasGeometryTolerances.CurrentHingeAbsoluteTolerance,
                axisLength *
                FoldCanvasGeometryTolerances.CurrentHingeRelativeTolerance);
            float previousProjection = -currentTolerance;
            for (int i = 0; i < mappedSamples.Length; i++)
            {
                Vector3 relative = mappedSamples[i] - hingeStart;
                float distance = Vector3.Cross(axisDirection, relative).magnitude;
                float projection = Vector3.Dot(relative, axisDirection);
                if (!FiniteMath.IsFinite(mappedSamples[i]) ||
                    distance > currentTolerance ||
                    projection < -currentTolerance ||
                    projection > axisLength + currentTolerance ||
                    projection + currentTolerance < previousProjection)
                {
                    failure =
                        "The fold line is non-linear or reverses direction in the panel's current 3D embedding.";
                    return false;
                }

                previousProjection = projection;
            }

            failure = null;
            return true;
        }

        private static void AddTriangleEdgeBreakpoints(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            Vector2 lineStart,
            Vector2 lineEnd,
            List<float> breakpoints)
        {
            int end = panel.TriangleIndexStart + panel.TriangleIndexCount;
            for (int i = panel.TriangleIndexStart; i < end; i += 3)
            {
                int a = buffer.Triangles[i];
                int b = buffer.Triangles[i + 1];
                int c = buffer.Triangles[i + 2];
                AddEdgeBreakpoint(panel, buffer, lineStart, lineEnd, a, b, breakpoints);
                AddEdgeBreakpoint(panel, buffer, lineStart, lineEnd, b, c, breakpoints);
                AddEdgeBreakpoint(panel, buffer, lineStart, lineEnd, c, a, breakpoints);
            }
        }

        private static void AddEdgeBreakpoint(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            Vector2 lineStart,
            Vector2 lineEnd,
            int edgeStartIndex,
            int edgeEndIndex,
            List<float> breakpoints)
        {
            Vector2 edgeStart = ToNormalized(
                panel,
                buffer.Vertices[edgeStartIndex].SourcePosition);
            Vector2 edgeEnd = ToNormalized(
                panel,
                buffer.Vertices[edgeEndIndex].SourcePosition);
            Vector2 lineDelta = lineEnd - lineStart;
            Vector2 edgeDelta = edgeEnd - edgeStart;
            float denominator = Cross2(lineDelta, edgeDelta);
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;

            if (Mathf.Abs(denominator) > tolerance)
            {
                Vector2 offset = edgeStart - lineStart;
                float lineParameter = Cross2(offset, edgeDelta) / denominator;
                float edgeParameter = Cross2(offset, lineDelta) / denominator;
                if (lineParameter >= -tolerance &&
                    lineParameter <= 1f + tolerance &&
                    edgeParameter >= -tolerance &&
                    edgeParameter <= 1f + tolerance)
                {
                    breakpoints.Add(Mathf.Clamp01(lineParameter));
                }

                return;
            }

            float lineLength = lineDelta.magnitude;
            if (Mathf.Abs(Cross2(edgeStart - lineStart, lineDelta)) >
                tolerance * lineLength)
            {
                return;
            }

            float lineLengthSquared = lineDelta.sqrMagnitude;
            AddProjectedEndpoint(
                edgeStart,
                lineStart,
                lineDelta,
                lineLengthSquared,
                tolerance,
                breakpoints);
            AddProjectedEndpoint(
                edgeEnd,
                lineStart,
                lineDelta,
                lineLengthSquared,
                tolerance,
                breakpoints);
        }

        private static void AddProjectedEndpoint(
            Vector2 point,
            Vector2 lineStart,
            Vector2 lineDelta,
            float lineLengthSquared,
            float tolerance,
            List<float> breakpoints)
        {
            float parameter =
                Vector2.Dot(point - lineStart, lineDelta) / lineLengthSquared;
            if (parameter >= -tolerance && parameter <= 1f + tolerance)
            {
                breakpoints.Add(Mathf.Clamp01(parameter));
            }
        }

        private static void SortAndDeduplicate(List<float> values)
        {
            values.Sort();
            float tolerance =
                FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance;
            int writeIndex = 1;
            for (int readIndex = 1; readIndex < values.Count; readIndex++)
            {
                if (Mathf.Abs(values[readIndex] - values[writeIndex - 1]) <=
                    tolerance)
                {
                    continue;
                }

                values[writeIndex] = values[readIndex];
                writeIndex++;
            }

            if (writeIndex < values.Count)
            {
                values.RemoveRange(writeIndex, values.Count - writeIndex);
            }
        }

        private static bool TryMapSourcePoint(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            Vector2 normalizedPoint,
            out Vector3 currentPoint)
        {
            float tolerance =
                FoldCanvasGeometryTolerances.SourceTriangleBarycentricTolerance;
            int end = panel.TriangleIndexStart + panel.TriangleIndexCount;
            for (int i = panel.TriangleIndexStart; i < end; i += 3)
            {
                MeshBuildVertex a = buffer.Vertices[buffer.Triangles[i]];
                MeshBuildVertex b = buffer.Vertices[buffer.Triangles[i + 1]];
                MeshBuildVertex c = buffer.Vertices[buffer.Triangles[i + 2]];
                Vector2 sourceA = ToNormalized(panel, a.SourcePosition);
                Vector2 sourceB = ToNormalized(panel, b.SourcePosition);
                Vector2 sourceC = ToNormalized(panel, c.SourcePosition);
                float denominator = Cross2(sourceB - sourceA, sourceC - sourceA);
                if (Mathf.Abs(denominator) <=
                    FoldCanvasGeometryTolerances.NormalizedFoldLineTolerance)
                {
                    continue;
                }

                Vector2 offset = normalizedPoint - sourceA;
                float weightB = Cross2(offset, sourceC - sourceA) / denominator;
                float weightC = Cross2(sourceB - sourceA, offset) / denominator;
                float weightA = 1f - weightB - weightC;
                if (weightA < -tolerance ||
                    weightB < -tolerance ||
                    weightC < -tolerance ||
                    weightA > 1f + tolerance ||
                    weightB > 1f + tolerance ||
                    weightC > 1f + tolerance)
                {
                    continue;
                }

                currentPoint =
                    a.Position * weightA +
                    b.Position * weightB +
                    c.Position * weightC;
                return FiniteMath.IsFinite(currentPoint);
            }

            currentPoint = default;
            return false;
        }

        private static Vector2 ToNormalized(
            PanelBuildRecord panel,
            Vector2 sourcePosition)
        {
            return new Vector2(
                sourcePosition.x / panel.PhysicalSize.x + 0.5f,
                sourcePosition.y / panel.PhysicalSize.y + 0.5f);
        }

        private static bool IsInsideNormalizedPanel(
            Vector2 point,
            float tolerance)
        {
            return point.x >= -tolerance &&
                point.x <= 1f + tolerance &&
                point.y >= -tolerance &&
                point.y <= 1f + tolerance;
        }

        private static Vector2 ClampNormalized(Vector2 point)
        {
            return new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
        }

        private static float Cross2(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private readonly struct CreaseInterval
        {
            public CreaseInterval(float start, float end)
            {
                Start = start;
                End = end;
            }

            public float Start { get; }

            public float End { get; }
        }
    }
}
