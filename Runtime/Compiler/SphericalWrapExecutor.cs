using System;
using UnityEngine;

namespace FoldCanvas
{
    internal static class SphericalWrapExecutor
    {
        public static bool TryExecute(
            SphericalWrapOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (!buffer.TryGetPanel(
                    operation.PanelId,
                    out PanelBuildRecord panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.SphericalWrapTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "SphericalWrap references a panel that does not exist.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (panel.Shape != PanelShape.Rectangle)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedSphericalWrapPanelShape,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M05 SphericalWrap supports rectangle parameter panels only.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (!FiniteMath.IsFinite(operation.Radius) ||
                !FiniteMath.IsFinite(operation.LatitudeRange) ||
                !FiniteMath.IsFinite(operation.LongitudeRange))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .NonFiniteSphericalWrapParameter,
                    FoldCanvasDiagnosticSeverity.Error,
                    "SphericalWrap radius and angular ranges must be finite.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.Radius <=
                FoldCanvasGeometryTolerances.MinimumSphericalRadius)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidSphericalRadius,
                    FoldCanvasDiagnosticSeverity.Error,
                    "SphericalWrap radius must be greater than the minimum positive spherical radius.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "radius",
                            operation.Radius,
                            "m"),
                        new FoldCanvasDiagnosticValue(
                            "minimumRadius",
                            FoldCanvasGeometryTolerances
                                .MinimumSphericalRadius,
                            "m")
                    }));
                return false;
            }

            if (operation.WrapDirection !=
                    SphericalWrapDirection.LongitudeAlongU &&
                operation.WrapDirection !=
                    SphericalWrapDirection.LongitudeAlongV)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .InvalidSphericalWrapDirection,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"SphericalWrap direction value {(int)operation.WrapDirection} is not supported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.PoleMode != SphericalPoleMode.Merge &&
                operation.PoleMode != SphericalPoleMode.KeepFan)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.InvalidSphericalPoleMode,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Spherical pole mode value {(int)operation.PoleMode} is not supported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            if (operation.SubdivisionMode !=
                SphericalSubdivisionMode.PanelGrid)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedSphericalSubdivisionMode,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Spherical subdivision mode value {(int)operation.SubdivisionMode} is not supported in M05.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            double latitudeSpan =
                (double)operation.LatitudeRange.y -
                operation.LatitudeRange.x;
            double longitudeSpan =
                (double)operation.LongitudeRange.y -
                operation.LongitudeRange.x;
            if (Math.Abs(latitudeSpan) <=
                    FoldCanvasGeometryTolerances
                        .MinimumSphericalRangeDegrees ||
                Math.Abs(longitudeSpan) <=
                    FoldCanvasGeometryTolerances
                        .MinimumSphericalRangeDegrees)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.CollapsedSphericalRange,
                    FoldCanvasDiagnosticSeverity.Error,
                    "SphericalWrap latitude and longitude ranges must each have a non-zero angular span.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "latitudeSpan",
                            latitudeSpan,
                            "deg"),
                        new FoldCanvasDiagnosticValue(
                            "longitudeSpan",
                            longitudeSpan,
                            "deg")
                    }));
                return false;
            }

            if (!IsSupportedLatitude(operation.LatitudeRange.x) ||
                !IsSupportedLatitude(operation.LatitudeRange.y))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.CollapsedSphericalRange,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M05 SphericalWrap latitude endpoints must remain within [-90, 90] degrees.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "latitudeStart",
                            operation.LatitudeRange.x,
                            "deg"),
                        new FoldCanvasDiagnosticValue(
                            "latitudeEnd",
                            operation.LatitudeRange.y,
                            "deg")
                    }));
                return false;
            }

            if (Math.Abs(longitudeSpan) >
                FoldCanvasGeometryTolerances
                    .MaximumSphericalLongitudeSpanDegrees +
                FoldCanvasGeometryTolerances
                    .SphericalAngleToleranceDegrees)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedSphericalMultiTurn,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M05 SphericalWrap supports at most one signed longitude turn. Multi-layer or spiral spherical wrapping is not implemented.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "longitudeSpan",
                            longitudeSpan,
                            "deg"),
                        new FoldCanvasDiagnosticValue(
                            "maximumAbsoluteSpan",
                            FoldCanvasGeometryTolerances
                                .MaximumSphericalLongitudeSpanDegrees,
                            "deg")
                    }));
                return false;
            }

            bool startIsPole = IsPole(operation.LatitudeRange.x);
            bool endIsPole = IsPole(operation.LatitudeRange.y);
            int latitudeSegments =
                operation.WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? panel.VSegments
                    : panel.USegments;
            if (startIsPole && endIsPole && latitudeSegments < 2)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .InsufficientSphericalPoleTessellation,
                    FoldCanvasDiagnosticSeverity.Error,
                    "A panel spanning both spherical poles requires at least two authored latitude segments so one non-pole row exists.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "latitudeSegments",
                            latitudeSegments),
                        new FoldCanvasDiagnosticValue(
                            "minimumLatitudeSegments",
                            2d)
                    }));
                return false;
            }

            if (!TryResolveCurrentFrame(
                    panel,
                    buffer,
                    out SphericalFrame frame))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedSphericalEmbedding,
                    FoldCanvasDiagnosticSeverity.Error,
                    "M05 SphericalWrap requires a congruent, non-degenerate planar embedding of the source panel. Metric-changing scale, shear, collapse, and prior curvature are unsupported.",
                    panel.PanelId,
                    operation.Id));
                return false;
            }

            SphericalWrapBuildRecord sphericalWrap =
                new SphericalWrapBuildRecord(
                    operation,
                    panel,
                    frame.CurrentOrigin,
                    frame.CurrentU,
                    frame.CurrentV,
                    frame.CurrentNormal);
            Vector3[] mappedPositions = new Vector3[panel.VertexCount];
            double maximumRadiusError = 0d;
            double radiusTolerance =
                FoldCanvasGeometryTolerances
                    .SphericalRadiusAbsoluteTolerance +
                FoldCanvasGeometryTolerances
                    .SphericalRadiusRelativeTolerance *
                operation.Radius;
            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex vertex =
                    buffer.Vertices[panel.VertexStart + offset];
                Vector3 position =
                    sphericalWrap.Evaluate(vertex.SourcePosition);
                if (!FiniteMath.IsFinite(position))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .NonFiniteSphericalWrapParameter,
                        FoldCanvasDiagnosticSeverity.Error,
                        "SphericalWrap parameters produce a non-finite vertex position.",
                        panel.PanelId,
                        operation.Id));
                    return false;
                }

                double radiusError = Math.Abs(
                    Vector3.Distance(position, frame.CurrentOrigin) -
                    operation.Radius);
                maximumRadiusError = Math.Max(
                    maximumRadiusError,
                    radiusError);
                mappedPositions[offset] = position;
            }

            if (maximumRadiusError > radiusTolerance)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.SphericalRadiusError,
                    FoldCanvasDiagnosticSeverity.Error,
                    "SphericalWrap exceeded the deterministic radius tolerance.",
                    panel.PanelId,
                    operation.Id,
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "maximumRadiusError",
                            maximumRadiusError,
                            "m"),
                        new FoldCanvasDiagnosticValue(
                            "radiusTolerance",
                            radiusTolerance,
                            "m"),
                        new FoldCanvasDiagnosticValue(
                            "radius",
                            operation.Radius,
                            "m")
                    }));
                return false;
            }

            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                int vertexIndex = panel.VertexStart + offset;
                MeshBuildVertex vertex = buffer.Vertices[vertexIndex];
                vertex.Position = mappedPositions[offset];
                buffer.Vertices[vertexIndex] = vertex;
                sphericalWrap.RegisterSurfaceVertex(vertexIndex);
            }

            if (!TryOrientOutward(
                    panel,
                    frame.CurrentOrigin,
                    buffer,
                    result,
                    operation.Id))
            {
                return false;
            }

            if (!TryAssignPoleMetadata(
                    operation,
                    panel,
                    buffer,
                    sphericalWrap,
                    result))
            {
                return false;
            }

            buffer.AddSphericalWrap(sphericalWrap);
            return true;
        }

        internal static bool IsPole(float latitudeDegrees)
        {
            return Math.Abs(
                Math.Abs((double)latitudeDegrees) - 90d) <=
                FoldCanvasGeometryTolerances
                    .SphericalAngleToleranceDegrees;
        }

        private static bool IsSupportedLatitude(float latitudeDegrees)
        {
            return latitudeDegrees >=
                    -90d -
                    FoldCanvasGeometryTolerances
                        .SphericalAngleToleranceDegrees &&
                latitudeDegrees <=
                    90d +
                    FoldCanvasGeometryTolerances
                        .SphericalAngleToleranceDegrees;
        }

        private static bool TryResolveCurrentFrame(
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            out SphericalFrame frame)
        {
            frame = default;
            if (panel.VertexCount < 3)
            {
                return false;
            }

            int anchorIndex = panel.VertexStart;
            MeshBuildVertex anchor = buffer.Vertices[anchorIndex];
            int firstIndex = -1;
            int secondIndex = -1;
            Vector2 firstDelta = default;
            double determinant = 0d;
            double sourceScale = Math.Max(
                panel.PhysicalSize.x,
                panel.PhysicalSize.y);
            double sourceDeltaTolerance =
                Math.Max(1d, sourceScale) * 1e-8d;

            for (int offset = 1; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex candidate =
                    buffer.Vertices[panel.VertexStart + offset];
                Vector2 delta =
                    candidate.SourcePosition - anchor.SourcePosition;
                if (delta.sqrMagnitude <=
                    sourceDeltaTolerance * sourceDeltaTolerance)
                {
                    continue;
                }

                if (firstIndex < 0)
                {
                    firstIndex = panel.VertexStart + offset;
                    firstDelta = delta;
                    continue;
                }

                double candidateDeterminant =
                    firstDelta.x * delta.y -
                    firstDelta.y * delta.x;
                if (Math.Abs(candidateDeterminant) <=
                    sourceDeltaTolerance * sourceDeltaTolerance)
                {
                    continue;
                }

                secondIndex = panel.VertexStart + offset;
                determinant = candidateDeterminant;
                break;
            }

            if (firstIndex < 0 || secondIndex < 0)
            {
                return false;
            }

            MeshBuildVertex first = buffer.Vertices[firstIndex];
            MeshBuildVertex second = buffer.Vertices[secondIndex];
            Vector2 secondDelta =
                second.SourcePosition - anchor.SourcePosition;
            Vector3 firstCurrentDelta =
                first.Position - anchor.Position;
            Vector3 secondCurrentDelta =
                second.Position - anchor.Position;
            Vector3 currentU = (
                firstCurrentDelta * secondDelta.y -
                secondCurrentDelta * firstDelta.y) /
                (float)determinant;
            Vector3 currentV = (
                secondCurrentDelta * firstDelta.x -
                firstCurrentDelta * secondDelta.x) /
                (float)determinant;
            float uLength = currentU.magnitude;
            float vLength = currentV.magnitude;
            float tolerance =
                FoldCanvasGeometryTolerances.RollFrameAbsoluteTolerance +
                FoldCanvasGeometryTolerances.RollFrameRelativeTolerance *
                (float)sourceScale;
            if (!FiniteMath.IsFinite(currentU) ||
                !FiniteMath.IsFinite(currentV) ||
                Math.Abs(uLength - 1f) > tolerance ||
                Math.Abs(vLength - 1f) > tolerance ||
                uLength <=
                    FoldCanvasGeometryTolerances.MinimumCurrentHingeLength ||
                vLength <=
                    FoldCanvasGeometryTolerances.MinimumCurrentHingeLength)
            {
                return false;
            }

            currentU /= uLength;
            currentV /= vLength;
            if (Math.Abs(Vector3.Dot(currentU, currentV)) >
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
                anchor.Position -
                anchor.SourcePosition.x * currentU -
                anchor.SourcePosition.y * currentV;
            for (int offset = 0; offset < panel.VertexCount; offset++)
            {
                MeshBuildVertex vertex =
                    buffer.Vertices[panel.VertexStart + offset];
                Vector3 expected =
                    currentOrigin +
                    vertex.SourcePosition.x * currentU +
                    vertex.SourcePosition.y * currentV;
                if (!FiniteMath.IsFinite(vertex.Position) ||
                    Vector3.Distance(vertex.Position, expected) >
                        tolerance)
                {
                    return false;
                }
            }

            frame = new SphericalFrame(
                currentOrigin,
                currentU,
                currentV,
                currentNormal);
            return true;
        }

        private static bool TryOrientOutward(
            PanelBuildRecord panel,
            Vector3 center,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result,
            string operationId)
        {
            bool foundOrientation = false;
            bool reverse = false;
            int triangleEnd =
                panel.TriangleIndexStart + panel.TriangleIndexCount;
            for (int offset = panel.TriangleIndexStart;
                offset < triangleEnd;
                offset += 3)
            {
                int aIndex = buffer.Triangles[offset];
                int bIndex = buffer.Triangles[offset + 1];
                int cIndex = buffer.Triangles[offset + 2];
                int aTopology = buffer.GetTopologyId(aIndex);
                int bTopology = buffer.GetTopologyId(bIndex);
                int cTopology = buffer.GetTopologyId(cIndex);
                if (aTopology == bTopology ||
                    bTopology == cTopology ||
                    cTopology == aTopology)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .InvalidSphericalPoleTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"SphericalWrap triangle {offset / 3} repeats a logical pole identity.",
                        panel.PanelId,
                        operationId));
                    return false;
                }

                Vector3 a = buffer.Vertices[aIndex].Position;
                Vector3 b = buffer.Vertices[bIndex].Position;
                Vector3 c = buffer.Vertices[cIndex].Position;
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude <=
                    FoldCanvasGeometryTolerances
                        .MinimumTriangleDoubleAreaSquared)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .InvalidSphericalPoleTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"SphericalWrap triangle {offset / 3} has zero or near-zero area.",
                        panel.PanelId,
                        operationId));
                    return false;
                }

                if (!foundOrientation)
                {
                    Vector3 centroid = (a + b + c) / 3f;
                    float radialDot = Vector3.Dot(
                        cross,
                        centroid - center);
                    if (Math.Abs(radialDot) <=
                        FoldCanvasGeometryTolerances
                            .MinimumCurrentHingeLength)
                    {
                        continue;
                    }

                    reverse = radialDot < 0f;
                    foundOrientation = true;
                }
            }

            if (!foundOrientation)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .InvalidSphericalPoleTopology,
                    FoldCanvasDiagnosticSeverity.Error,
                    "SphericalWrap could not resolve an outward orientation from any emitted triangle.",
                    panel.PanelId,
                    operationId));
                return false;
            }

            if (reverse)
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
                Vector3 a =
                    buffer.Vertices[buffer.Triangles[offset]].Position;
                Vector3 b =
                    buffer.Vertices[
                        buffer.Triangles[offset + 1]].Position;
                Vector3 c =
                    buffer.Vertices[
                        buffer.Triangles[offset + 2]].Position;
                Vector3 cross = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                if (Vector3.Dot(cross, centroid - center) <= 0f)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .InvalidSphericalPoleTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"SphericalWrap triangle {offset / 3} does not face outward after deterministic winding resolution.",
                        panel.PanelId,
                        operationId));
                    return false;
                }
            }

            return true;
        }

        private static bool TryAssignPoleMetadata(
            SphericalWrapOperationDefinition operation,
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            SphericalWrapBuildRecord sphericalWrap,
            FoldCanvasCompileResult result)
        {
            string latitudeStartBoundary =
                operation.WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? "vMin"
                    : "uMin";
            string latitudeEndBoundary =
                operation.WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? "vMax"
                    : "uMax";

            if (IsPole(operation.LatitudeRange.x) &&
                !TryAssignPole(
                    operation.LatitudeRange.x,
                    latitudeStartBoundary,
                    panel,
                    buffer,
                    sphericalWrap,
                    result,
                    operation.Id))
            {
                return false;
            }

            if (IsPole(operation.LatitudeRange.y) &&
                !TryAssignPole(
                    operation.LatitudeRange.y,
                    latitudeEndBoundary,
                    panel,
                    buffer,
                    sphericalWrap,
                    result,
                    operation.Id))
            {
                return false;
            }

            return true;
        }

        private static bool TryAssignPole(
            float latitudeDegrees,
            string boundaryId,
            PanelBuildRecord panel,
            MeshBuildBuffer buffer,
            SphericalWrapBuildRecord sphericalWrap,
            FoldCanvasCompileResult result,
            string operationId)
        {
            if (!panel.TryGetBoundary(
                    boundaryId,
                    out BoundaryBuildRecord boundary) ||
                boundary.VertexIndices.Count == 0)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .InvalidSphericalPoleTopology,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Spherical pole boundary '{boundaryId}' is missing.",
                    panel.PanelId,
                    operationId));
                return false;
            }

            int representative = boundary.VertexIndices[0];
            int topologyId = buffer.GetTopologyId(representative);
            for (int i = 1; i < boundary.VertexIndices.Count; i++)
            {
                if (buffer.GetTopologyId(boundary.VertexIndices[i]) !=
                    topologyId)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .InvalidSphericalPoleTopology,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Spherical pole boundary '{boundaryId}' does not share one logical topology identity.",
                        panel.PanelId,
                        operationId));
                    return false;
                }
            }

            if (latitudeDegrees > 0f)
            {
                sphericalWrap.NorthPoleVertexIndex = representative;
            }
            else
            {
                sphericalWrap.SouthPoleVertexIndex = representative;
            }

            return true;
        }

        private readonly struct SphericalFrame
        {
            public SphericalFrame(
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
