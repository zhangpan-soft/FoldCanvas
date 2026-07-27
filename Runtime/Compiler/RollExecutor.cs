using System;
using System.Globalization;
using UnityEngine;

namespace FoldCanvas
{
    internal static class RollExecutor
    {
        public static bool TryExecute(
            RollOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (!buffer.TryGetPanel(operation.PanelId, out PanelBuildRecord panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.RollTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Roll references a panel that does not exist.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (panel.Shape != PanelShape.Rectangle)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedRollPanelShape,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M03 Roll supports rectangle panels only.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (!FiniteMath.IsFinite(operation.AngleDegrees) ||
                !FiniteMath.IsFinite(operation.StartAngleDegrees))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonFiniteRollParameter,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Roll angle and start angle must be finite.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (Mathf.Abs(operation.AngleDegrees) <=
                FoldCanvasGeometryTolerances.MinimumRollAngleDegrees)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NearZeroRollAngle,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Roll angle magnitude must be greater than {FoldCanvasGeometryTolerances.MinimumRollAngleDegrees} degrees.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            double angleMagnitude = Math.Abs((double)operation.AngleDegrees);
            if (angleMagnitude >
                FoldCanvasGeometryTolerances.MaximumCircularRollAngleDegrees +
                FoldCanvasGeometryTolerances.FullRollAngleToleranceDegrees)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedMultiTurnRoll,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M03 Circular Roll supports a signed angular sweep of at most 360 degrees. Multi-turn SpiralRoll and LayeredRoll are not implemented.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.Direction != RollDirection.U &&
                operation.Direction != RollDirection.V)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidRollDirection,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Roll direction value {(int)operation.Direction} is not supported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.RadiusMode == RollRadiusMode.FitTargetBoundary)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedFitTargetBoundary,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FitTargetBoundary requires M04 seam solving and is not implemented in M03.",
                    panel.PanelId,
                    operation.Id,
                    operation.TargetSeamId));
                return false;
            }

            if (operation.RadiusMode != RollRadiusMode.PreserveArcLength &&
                operation.RadiusMode != RollRadiusMode.Explicit)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidRollRadiusMode,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Roll radius mode value {(int)operation.RadiusMode} is not supported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            int rollSegments = operation.Direction == RollDirection.U
                ? panel.USegments
                : panel.VSegments;
            double sourceSpan = operation.Direction == RollDirection.U
                ? panel.PhysicalSize.x
                : panel.PhysicalSize.y;
            double angleRadians =
                operation.AngleDegrees * Math.PI / 180d;
            double radius;
            double stretchRatio = 1d;
            bool reportStretch = false;

            switch (operation.RadiusMode)
            {
                case RollRadiusMode.PreserveArcLength:
                    radius = sourceSpan / Math.Abs(angleRadians);
                    break;
                case RollRadiusMode.Explicit:
                    if (!FiniteMath.IsFinite(operation.ExplicitRadius))
                    {
                        result.Add(new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes.NonFiniteRollParameter,
                            FoldCanvasDiagnosticSeverity.Error,
                            "An explicit Roll radius must be finite.",
                            panel.PanelId,
                            operation.Id));
                        return false;
                    }

                    if (operation.ExplicitRadius <= 0f)
                    {
                        result.Add(new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes.InvalidExplicitRollRadius,
                            FoldCanvasDiagnosticSeverity.Error,
                            "An explicit Roll radius must be greater than zero.",
                            panel.PanelId,
                            operation.Id));
                        return false;
                    }

                    radius = operation.ExplicitRadius;
                    stretchRatio =
                        radius * Math.Abs(angleRadians) / sourceSpan;
                    reportStretch = true;
                    break;
                default:
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidRollRadiusMode,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Roll radius mode changed during compilation and is no longer supported.",
                        panel.PanelId,
                        operation.Id));
                    return false;
            }

            if (double.IsNaN(radius) ||
                double.IsInfinity(radius) ||
                radius <= 0d ||
                radius > float.MaxValue)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonFiniteRollParameter,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Roll parameters produce a non-finite or unrepresentable radius.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (!TryResolveRigidFrame(panel, buffer, out RollFrame frame))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedRollEmbedding,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M03 Roll requires the current rectangle to remain a congruent, non-degenerate planar embedding of its source.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (IsClosedFullTurn(operation.AngleDegrees) &&
                rollSegments < 3)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InsufficientRollTessellation,
                    FoldCanvasDiagnosticSeverity.Error,
                    "A closed full-turn Roll requires at least three source segments in the selected direction.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            Vector3[] positions = new Vector3[panel.VertexCount];
            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex vertex =
                    buffer.Vertices[panel.VertexStart + offset];
                double normalized = operation.Direction == RollDirection.U
                    ? vertex.SourcePosition.x / sourceSpan + 0.5d
                    : vertex.SourcePosition.y / sourceSpan + 0.5d;
                double theta =
                    (operation.StartAngleDegrees +
                        normalized * operation.AngleDegrees) *
                    Math.PI / 180d;
                float radialAxis = (float)(-radius * Math.Cos(theta));
                float radialNormal = (float)(radius * Math.Sin(theta));

                Vector3 position;
                if (operation.Direction == RollDirection.U)
                {
                    position =
                        frame.CurrentOrigin +
                        vertex.SourcePosition.y * frame.CurrentV +
                        radialAxis * frame.CurrentU +
                        radialNormal * frame.CurrentNormal;
                }
                else
                {
                    position =
                        frame.CurrentOrigin +
                        vertex.SourcePosition.x * frame.CurrentU +
                        radialAxis * frame.CurrentV +
                        radialNormal * frame.CurrentNormal;
                }

                if (!FiniteMath.IsFinite(position))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonFiniteRollParameter,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Roll parameters produce a non-finite vertex position.",
                        panel.PanelId,
                        operation.Id));
                    return false;
                }

                positions[offset] = position;
            }

            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                int vertexIndex = panel.VertexStart + offset;
                MeshBuildVertex vertex = buffer.Vertices[vertexIndex];
                vertex.Position = positions[offset];
                buffer.Vertices[vertexIndex] = vertex;
            }

            if (reportStretch)
            {
                double arcLength = radius * Math.Abs(angleRadians);
                bool outsideGuidance =
                    stretchRatio <
                        FoldCanvasGeometryTolerances.MinimumRollDistortionRatio ||
                    stretchRatio >
                        FoldCanvasGeometryTolerances.MaximumRollDistortionRatio;
                string ratioText = stretchRatio.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture);
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.RollStretchReport,
                    outsideGuidance
                        ? FoldCanvasDiagnosticSeverity.Warning
                        : FoldCanvasDiagnosticSeverity.Info,
                    outsideGuidance
                        ? $"Explicit Roll stretch ratio {ratioText} is outside the guidance range [0.5, 2]. Geometry was generated without clamping."
                        : $"Explicit Roll stretch ratio {ratioText} is within the guidance range [0.5, 2].",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "sourceSpan",
                            sourceSpan,
                            "m"),
                        new FoldCanvasDiagnosticValue(
                            "arcLength",
                            arcLength,
                            "m"),
                        new FoldCanvasDiagnosticValue(
                            "stretchRatio",
                            stretchRatio)
                    }));
            }

            return true;
        }

        private static bool IsClosedFullTurn(float angleDegrees)
        {
            double magnitude = Math.Abs((double)angleDegrees);
            return Math.Abs(
                    magnitude -
                    FoldCanvasGeometryTolerances.MaximumCircularRollAngleDegrees) <=
                FoldCanvasGeometryTolerances.FullRollAngleToleranceDegrees;
        }

        private static bool TryResolveRigidFrame(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            out RollFrame frame)
        {
            frame = default;
            if (panel.USegments < 1 || panel.VSegments < 1)
            {
                return false;
            }

            int rowLength = panel.USegments + 1;
            Vector3 bottomLeft =
                buffer.Vertices[panel.VertexStart].Position;
            Vector3 bottomRight =
                buffer.Vertices[panel.VertexStart + panel.USegments].Position;
            Vector3 topLeft =
                buffer.Vertices[
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
                uLength <= FoldCanvasGeometryTolerances.MinimumCurrentHingeLength ||
                vLength <= FoldCanvasGeometryTolerances.MinimumCurrentHingeLength)
            {
                return false;
            }

            Vector3 uAxis = uVector / uLength;
            Vector3 vAxis = vVector / vLength;
            if (Mathf.Abs(Vector3.Dot(uAxis, vAxis)) >
                FoldCanvasGeometryTolerances.RollFrameOrthogonalityTolerance)
            {
                return false;
            }

            Vector3 normal = Vector3.Cross(uAxis, vAxis);
            float normalLength = normal.magnitude;
            if (!FiniteMath.IsFinite(normal) ||
                normalLength <=
                    FoldCanvasGeometryTolerances.MinimumCurrentHingeLength)
            {
                return false;
            }

            normal /= normalLength;
            Vector3 currentOrigin =
                bottomLeft +
                uAxis * (panel.PhysicalSize.x * 0.5f) +
                vAxis * (panel.PhysicalSize.y * 0.5f);

            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex vertex =
                    buffer.Vertices[panel.VertexStart + offset];
                Vector3 expected =
                    currentOrigin +
                    vertex.SourcePosition.x * uAxis +
                    vertex.SourcePosition.y * vAxis;
                if (!FiniteMath.IsFinite(vertex.Position) ||
                    Vector3.Distance(vertex.Position, expected) > tolerance)
                {
                    return false;
                }
            }

            frame = new RollFrame(
                currentOrigin,
                uAxis,
                vAxis,
                normal);
            return true;
        }

        private readonly struct RollFrame
        {
            public RollFrame(
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
