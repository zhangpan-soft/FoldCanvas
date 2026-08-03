using System;
using UnityEngine;

namespace FoldCanvas
{
    internal static class ToroidalWrapExecutor
    {
        private const double DegreesToRadians = Math.PI / 180d;

        public static bool TryExecute(
            ToroidalWrapOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (!buffer.TryGetPanel(
                    operation.PanelId,
                    out PanelBuildRecord panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.ToroidalWrapTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "ToroidalWrap references a panel that does not exist.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (panel.Shape != PanelShape.Rectangle)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedToroidalWrapPanelShape,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M09 ToroidalWrap supports rectangle parameter panels only.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (!FiniteMath.IsFinite(operation.MajorRadius) ||
                !FiniteMath.IsFinite(operation.MinorRadius) ||
                !FiniteMath.IsFinite(operation.MajorAngleRange) ||
                !FiniteMath.IsFinite(operation.MinorAngleRange))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .NonFiniteToroidalWrapParameter,
                    FoldCanvasDiagnosticSeverity.Error,
                    "ToroidalWrap radii and angular ranges must be finite.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.MinorRadius <= 0f ||
                operation.MajorRadius <= operation.MinorRadius)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidToroidalRadius,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M09 ToroidalWrap requires majorRadius > minorRadius > 0 so the generated toroidal surface does not self-intersect.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "majorRadius",
                            operation.MajorRadius,
                            "m"),
                        new FoldCanvasDiagnosticValue(
                            "minorRadius",
                            operation.MinorRadius,
                            "m")
                    }));
                return false;
            }

            if (operation.WrapDirection !=
                    ToroidalWrapDirection.MajorAlongU &&
                operation.WrapDirection !=
                    ToroidalWrapDirection.MajorAlongV)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .InvalidToroidalWrapDirection,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"ToroidalWrap direction value {(int)operation.WrapDirection} is not supported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            double majorSpan =
                (double)operation.MajorAngleRange.y -
                operation.MajorAngleRange.x;
            double minorSpan =
                (double)operation.MinorAngleRange.y -
                operation.MinorAngleRange.x;
            if (Math.Abs(majorSpan) <=
                    FoldCanvasGeometryTolerances.MinimumRollAngleDegrees ||
                Math.Abs(minorSpan) <=
                    FoldCanvasGeometryTolerances.MinimumRollAngleDegrees)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.CollapsedToroidalRange,
                    FoldCanvasDiagnosticSeverity.Error,
                    "ToroidalWrap major and minor angular ranges must each have a non-zero span.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "majorAngleSpan",
                            majorSpan,
                            "deg"),
                        new FoldCanvasDiagnosticValue(
                            "minorAngleSpan",
                            minorSpan,
                            "deg")
                    }));
                return false;
            }

            if (!IsAtMostOneTurn(majorSpan) ||
                !IsAtMostOneTurn(minorSpan))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedToroidalMultiTurn,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M09 ToroidalWrap supports at most one signed turn on each parameter axis. Layered and spiral toroidal surfaces are not implemented.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "majorAngleSpan",
                            majorSpan,
                            "deg"),
                        new FoldCanvasDiagnosticValue(
                            "minorAngleSpan",
                            minorSpan,
                            "deg")
                    }));
                return false;
            }

            int majorSegments = operation.WrapDirection ==
                ToroidalWrapDirection.MajorAlongU
                    ? panel.USegments
                    : panel.VSegments;
            int minorSegments = operation.WrapDirection ==
                ToroidalWrapDirection.MajorAlongU
                    ? panel.VSegments
                    : panel.USegments;
            if ((IsFullTurn(majorSpan) && majorSegments < 3) ||
                (IsFullTurn(minorSpan) && minorSegments < 3))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .InsufficientToroidalTessellation,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Every closed full-turn ToroidalWrap axis requires at least three authored source segments.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "majorSegments",
                            majorSegments),
                        new FoldCanvasDiagnosticValue(
                            "minorSegments",
                            minorSegments)
                    }));
                return false;
            }

            if (!TryResolveCurrentFrame(
                    panel,
                    buffer,
                    out ToroidalFrame frame))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedToroidalEmbedding,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M09 ToroidalWrap requires a congruent, non-degenerate planar embedding. Metric-changing scale, shear, collapse, and prior curvature are unsupported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            Vector3[] mapped = new Vector3[panel.VertexCount];
            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex vertex =
                    buffer.Vertices[panel.VertexStart + offset];
                EvaluateParameters(
                    operation,
                    panel,
                    vertex.SourcePosition,
                    out double majorAngle,
                    out double minorAngle);
                Vector3 position = Evaluate(
                    frame,
                    operation.MajorRadius,
                    operation.MinorRadius,
                    majorAngle,
                    minorAngle);
                if (!FiniteMath.IsFinite(position))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .NonFiniteToroidalWrapParameter,
                        FoldCanvasDiagnosticSeverity.Error,
                        "ToroidalWrap parameters produced a non-finite vertex position.",
                        panel.PanelId,
                        operation.Id));
                    return false;
                }

                mapped[offset] = position;
            }

            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                int vertexIndex = panel.VertexStart + offset;
                MeshBuildVertex vertex = buffer.Vertices[vertexIndex];
                vertex.Position = mapped[offset];
                buffer.Vertices[vertexIndex] = vertex;
            }

            if (!TryOrientOutward(
                    operation,
                    panel,
                    frame,
                    buffer,
                    result))
            {
                return false;
            }

            return true;
        }

        private static bool IsAtMostOneTurn(double spanDegrees)
        {
            return Math.Abs(spanDegrees) <=
                FoldCanvasGeometryTolerances
                    .MaximumCircularRollAngleDegrees +
                FoldCanvasGeometryTolerances
                    .FullRollAngleToleranceDegrees;
        }

        private static bool IsFullTurn(double spanDegrees)
        {
            return Math.Abs(
                    Math.Abs(spanDegrees) -
                    FoldCanvasGeometryTolerances
                        .MaximumCircularRollAngleDegrees) <=
                FoldCanvasGeometryTolerances
                    .FullRollAngleToleranceDegrees;
        }

        private static void EvaluateParameters(
            ToroidalWrapOperationDefinition operation,
            PanelBuildRecord panel,
            Vector2 sourcePosition,
            out double majorAngle,
            out double minorAngle)
        {
            double normalizedU =
                sourcePosition.x / panel.PhysicalSize.x + 0.5d;
            double normalizedV =
                sourcePosition.y / panel.PhysicalSize.y + 0.5d;
            double majorT = operation.WrapDirection ==
                ToroidalWrapDirection.MajorAlongU
                    ? normalizedU
                    : normalizedV;
            double minorT = operation.WrapDirection ==
                ToroidalWrapDirection.MajorAlongU
                    ? normalizedV
                    : normalizedU;
            majorAngle = (
                operation.MajorAngleRange.x +
                majorT * (
                    operation.MajorAngleRange.y -
                    operation.MajorAngleRange.x)) * DegreesToRadians;
            minorAngle = (
                operation.MinorAngleRange.x +
                minorT * (
                    operation.MinorAngleRange.y -
                    operation.MinorAngleRange.x)) * DegreesToRadians;
        }

        private static Vector3 Evaluate(
            ToroidalFrame frame,
            float majorRadius,
            float minorRadius,
            double majorAngle,
            double minorAngle)
        {
            Vector3 radial =
                (float)Math.Cos(majorAngle) * frame.CurrentU +
                (float)Math.Sin(majorAngle) * frame.CurrentNormal;
            return frame.CurrentOrigin +
                (majorRadius +
                    minorRadius * (float)Math.Cos(minorAngle)) * radial +
                minorRadius * (float)Math.Sin(minorAngle) *
                    frame.CurrentV;
        }

        private static Vector3 EvaluateOutward(
            ToroidalFrame frame,
            double majorAngle,
            double minorAngle)
        {
            Vector3 radial =
                (float)Math.Cos(majorAngle) * frame.CurrentU +
                (float)Math.Sin(majorAngle) * frame.CurrentNormal;
            return (
                (float)Math.Cos(minorAngle) * radial +
                (float)Math.Sin(minorAngle) * frame.CurrentV).normalized;
        }

        private static bool TryOrientOutward(
            ToroidalWrapOperationDefinition operation,
            PanelBuildRecord panel,
            ToroidalFrame frame,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            bool? reverse = null;
            int triangleEnd =
                panel.TriangleIndexStart + panel.TriangleIndexCount;
            for (int offset = panel.TriangleIndexStart;
                offset < triangleEnd;
                offset += 3)
            {
                Vector3 cross = TriangleCross(buffer, offset);
                if (cross.sqrMagnitude <=
                    FoldCanvasGeometryTolerances
                        .MinimumTriangleDoubleAreaSquared)
                {
                    continue;
                }

                Vector2 sourceCentroid =
                    SourceCentroid(buffer, offset);
                EvaluateParameters(
                    operation,
                    panel,
                    sourceCentroid,
                    out double majorAngle,
                    out double minorAngle);
                Vector3 outward = EvaluateOutward(
                    frame,
                    majorAngle,
                    minorAngle);
                reverse = Vector3.Dot(cross, outward) < 0f;
                break;
            }

            if (!reverse.HasValue)
            {
                AddWindingDiagnostic(
                    operation,
                    panel,
                    result,
                    -1);
                return false;
            }

            if (reverse.Value)
            {
                for (int offset = panel.TriangleIndexStart;
                    offset < triangleEnd;
                    offset += 3)
                {
                    int second = buffer.Triangles[offset + 1];
                    buffer.Triangles[offset + 1] =
                        buffer.Triangles[offset + 2];
                    buffer.Triangles[offset + 2] = second;
                }
            }

            for (int offset = panel.TriangleIndexStart;
                offset < triangleEnd;
                offset += 3)
            {
                Vector3 cross = TriangleCross(buffer, offset);
                Vector2 sourceCentroid =
                    SourceCentroid(buffer, offset);
                EvaluateParameters(
                    operation,
                    panel,
                    sourceCentroid,
                    out double majorAngle,
                    out double minorAngle);
                Vector3 outward = EvaluateOutward(
                    frame,
                    majorAngle,
                    minorAngle);
                if (cross.sqrMagnitude <=
                        FoldCanvasGeometryTolerances
                            .MinimumTriangleDoubleAreaSquared ||
                    Vector3.Dot(cross, outward) <= 0f)
                {
                    AddWindingDiagnostic(
                        operation,
                        panel,
                        result,
                        offset / 3);
                    return false;
                }
            }

            return true;
        }

        private static Vector3 TriangleCross(
            MeshBuildBuffer buffer,
            int triangleOffset)
        {
            Vector3 a = buffer.Vertices[
                buffer.Triangles[triangleOffset]].Position;
            Vector3 b = buffer.Vertices[
                buffer.Triangles[triangleOffset + 1]].Position;
            Vector3 c = buffer.Vertices[
                buffer.Triangles[triangleOffset + 2]].Position;
            return Vector3.Cross(b - a, c - a);
        }

        private static Vector2 SourceCentroid(
            MeshBuildBuffer buffer,
            int triangleOffset)
        {
            return (
                buffer.Vertices[
                    buffer.Triangles[triangleOffset]].SourcePosition +
                buffer.Vertices[
                    buffer.Triangles[triangleOffset + 1]].SourcePosition +
                buffer.Vertices[
                    buffer.Triangles[triangleOffset + 2]].SourcePosition) /
                3f;
        }

        private static void AddWindingDiagnostic(
            ToroidalWrapOperationDefinition operation,
            PanelBuildRecord panel,
            FoldCanvasCompileResult result,
            int triangleIndex)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.ToroidalWindingFailed,
                FoldCanvasDiagnosticSeverity.Error,
                "ToroidalWrap could not produce one non-degenerate consistently outward triangle surface.",
                panel.PanelId,
                operation.Id,
                geometryContext: triangleIndex >= 0
                    ? new FoldCanvasDiagnosticGeometryContext(
                        triangleIndex: triangleIndex)
                    : null));
        }

        private static bool TryResolveCurrentFrame(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            out ToroidalFrame frame)
        {
            frame = default;
            if (panel.USegments < 1 || panel.VSegments < 1)
            {
                return false;
            }

            int rowLength = panel.USegments + 1;
            Vector3 bottomLeft =
                buffer.Vertices[panel.VertexStart].Position;
            Vector3 bottomRight = buffer.Vertices[
                panel.VertexStart + panel.USegments].Position;
            Vector3 topLeft = buffer.Vertices[
                panel.VertexStart + panel.VSegments * rowLength].Position;
            Vector3 uVector = bottomRight - bottomLeft;
            Vector3 vVector = topLeft - bottomLeft;
            float uLength = uVector.magnitude;
            float vLength = vVector.magnitude;
            float sourceScale = Mathf.Max(
                panel.PhysicalSize.x,
                panel.PhysicalSize.y);
            float tolerance =
                FoldCanvasGeometryTolerances.RollFrameAbsoluteTolerance +
                FoldCanvasGeometryTolerances.RollFrameRelativeTolerance *
                sourceScale;

            if (!FiniteMath.IsFinite(uVector) ||
                !FiniteMath.IsFinite(vVector) ||
                Mathf.Abs(uLength - panel.PhysicalSize.x) > tolerance ||
                Mathf.Abs(vLength - panel.PhysicalSize.y) > tolerance ||
                uLength <=
                    FoldCanvasGeometryTolerances.MinimumCurrentHingeLength ||
                vLength <=
                    FoldCanvasGeometryTolerances.MinimumCurrentHingeLength)
            {
                return false;
            }

            Vector3 currentU = uVector / uLength;
            Vector3 currentV = vVector / vLength;
            if (Mathf.Abs(Vector3.Dot(currentU, currentV)) >
                FoldCanvasGeometryTolerances
                    .RollFrameOrthogonalityTolerance)
            {
                return false;
            }

            Vector3 currentNormal =
                Vector3.Cross(currentU, currentV);
            float normalLength = currentNormal.magnitude;
            if (!FiniteMath.IsFinite(currentNormal) ||
                normalLength <=
                    FoldCanvasGeometryTolerances.MinimumCurrentHingeLength)
            {
                return false;
            }

            currentNormal /= normalLength;
            Vector3 currentOrigin =
                bottomLeft +
                currentU * (panel.PhysicalSize.x * 0.5f) +
                currentV * (panel.PhysicalSize.y * 0.5f);
            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex vertex =
                    buffer.Vertices[panel.VertexStart + offset];
                Vector3 expected =
                    currentOrigin +
                    vertex.SourcePosition.x * currentU +
                    vertex.SourcePosition.y * currentV;
                if (!FiniteMath.IsFinite(vertex.Position) ||
                    Vector3.Distance(vertex.Position, expected) > tolerance)
                {
                    return false;
                }
            }

            frame = new ToroidalFrame(
                currentOrigin,
                currentU,
                currentV,
                currentNormal);
            return true;
        }

        private readonly struct ToroidalFrame
        {
            public ToroidalFrame(
                Vector3 currentOrigin,
                Vector3 currentU,
                Vector3 currentV,
                Vector3 currentNormal)
            {
                CurrentOrigin = currentOrigin;
                CurrentU = currentU;
                CurrentV = currentV;
                CurrentNormal = currentNormal;
            }

            public Vector3 CurrentOrigin { get; }

            public Vector3 CurrentU { get; }

            public Vector3 CurrentV { get; }

            public Vector3 CurrentNormal { get; }
        }
    }
}
