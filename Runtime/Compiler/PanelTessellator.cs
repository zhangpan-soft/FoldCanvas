using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal readonly struct SphericalTessellationHint
    {
        public SphericalTessellationHint(
            SphericalWrapDirection direction,
            SphericalPoleMode poleMode,
            bool collapseLatitudeStart,
            bool collapseLatitudeEnd)
        {
            Direction = direction;
            PoleMode = poleMode;
            CollapseLatitudeStart = collapseLatitudeStart;
            CollapseLatitudeEnd = collapseLatitudeEnd;
        }

        public SphericalWrapDirection Direction { get; }

        public SphericalPoleMode PoleMode { get; }

        public bool CollapseLatitudeStart { get; }

        public bool CollapseLatitudeEnd { get; }

        public bool HasCollapsedPole =>
            CollapseLatitudeStart || CollapseLatitudeEnd;
    }

    internal static class PanelTessellator
    {
        public static PanelBuildRecord Append(
            PanelDefinition panel,
            MeshBuildBuffer buffer,
            SphericalTessellationHint? sphericalHint = null)
        {
            switch (panel.Shape)
            {
                case PanelShape.Rectangle:
                    if (sphericalHint.HasValue &&
                        sphericalHint.Value.HasCollapsedPole)
                    {
                        return AppendPoleAwareRectangle(
                            panel,
                            buffer,
                            sphericalHint.Value);
                    }

                    return AppendRectangle(panel, buffer);
                case PanelShape.Disk:
                    return AppendDisk(panel, buffer);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(panel.Shape), panel.Shape, null);
            }
        }

        private static PanelBuildRecord AppendRectangle(PanelDefinition panel, MeshBuildBuffer buffer)
        {
            int start = buffer.Vertices.Count;
            int triangleIndexStart = buffer.Triangles.Count;
            int panelIndex = buffer.PanelCount;
            int uSegments = panel.USegments;
            int vSegments = panel.VSegments;
            Vector2 size = panel.PhysicalSize;
            Rect canvasRect = panel.CanvasRect;

            for (int v = 0; v <= vSegments; v++)
            {
                float fv = v / (float)vSegments;
                float y = (fv - 0.5f) * size.y;

                for (int u = 0; u <= uSegments; u++)
                {
                    float fu = u / (float)uSegments;
                    float x = (fu - 0.5f) * size.x;
                    Vector2 sourcePosition = new Vector2(x, y);
                    Vector2 sourceUv = new Vector2(
                        canvasRect.xMin + fu * canvasRect.width,
                        canvasRect.yMin + fv * canvasRect.height);
                    buffer.AddVertex(
                        new Vector3(sourcePosition.x, sourcePosition.y, 0f),
                        sourcePosition,
                        sourceUv,
                        panelIndex);
                }
            }

            int rowLength = uSegments + 1;
            for (int v = 0; v < vSegments; v++)
            {
                for (int u = 0; u < uSegments; u++)
                {
                    int i0 = start + v * rowLength + u;
                    int i1 = i0 + 1;
                    int i3 = i0 + rowLength;
                    int i2 = i3 + 1;

                    buffer.Triangles.Add(i0);
                    buffer.Triangles.Add(i1);
                    buffer.Triangles.Add(i2);

                    buffer.Triangles.Add(i0);
                    buffer.Triangles.Add(i2);
                    buffer.Triangles.Add(i3);
                }
            }

            PanelBuildRecord record = new PanelBuildRecord(
                panel,
                panelIndex,
                start,
                (uSegments + 1) * (vSegments + 1),
                triangleIndexStart,
                buffer.Triangles.Count - triangleIndexStart);

            int[] uMin = new int[vSegments + 1];
            int[] uMax = new int[vSegments + 1];
            for (int v = 0; v <= vSegments; v++)
            {
                uMin[v] = start + v * rowLength;
                uMax[v] = start + v * rowLength + uSegments;
            }

            int[] vMin = new int[uSegments + 1];
            int[] vMax = new int[uSegments + 1];
            for (int u = 0; u <= uSegments; u++)
            {
                vMin[u] = start + u;
                vMax[u] = start + vSegments * rowLength + u;
            }

            record.AddBoundary("uMin", uMin);
            record.AddBoundary("uMax", uMax);
            record.AddBoundary("vMin", vMin);
            record.AddBoundary("vMax", vMax);
            buffer.AddPanel(record);
            return record;
        }

        private static PanelBuildRecord AppendPoleAwareRectangle(
            PanelDefinition panel,
            MeshBuildBuffer buffer,
            SphericalTessellationHint hint)
        {
            int start = buffer.Vertices.Count;
            int triangleIndexStart = buffer.Triangles.Count;
            int panelIndex = buffer.PanelCount;
            int longitudeSegments =
                hint.Direction == SphericalWrapDirection.LongitudeAlongU
                    ? panel.USegments
                    : panel.VSegments;
            int latitudeSegments =
                hint.Direction == SphericalWrapDirection.LongitudeAlongU
                    ? panel.VSegments
                    : panel.USegments;
            List<int>[] latitudeBands =
                new List<int>[latitudeSegments + 1];

            for (int latitudeIndex = 0;
                latitudeIndex <= latitudeSegments;
                latitudeIndex++)
            {
                bool collapsed =
                    latitudeIndex == 0
                        ? hint.CollapseLatitudeStart
                        : latitudeIndex == latitudeSegments &&
                          hint.CollapseLatitudeEnd;
                int bandVertexCount = collapsed
                    ? hint.PoleMode == SphericalPoleMode.Merge
                        ? 1
                        : longitudeSegments
                    : longitudeSegments + 1;
                List<int> band = new List<int>(bandVertexCount);
                float latitudeT =
                    latitudeIndex / (float)latitudeSegments;
                for (int longitudeIndex = 0;
                    longitudeIndex < bandVertexCount;
                    longitudeIndex++)
                {
                    float longitudeT;
                    if (!collapsed)
                    {
                        longitudeT =
                            longitudeIndex / (float)longitudeSegments;
                    }
                    else if (hint.PoleMode == SphericalPoleMode.Merge)
                    {
                        longitudeT = 0.5f;
                    }
                    else
                    {
                        longitudeT =
                            (longitudeIndex + 0.5f) /
                            longitudeSegments;
                    }

                    GetSourceCoordinates(
                        panel,
                        hint.Direction,
                        longitudeT,
                        latitudeT,
                        out Vector2 sourcePosition,
                        out Vector2 sourceUv);
                    band.Add(buffer.AddVertex(
                        new Vector3(
                            sourcePosition.x,
                            sourcePosition.y,
                            0f),
                        sourcePosition,
                        sourceUv,
                        panelIndex));
                }

                if (collapsed && band.Count > 1)
                {
                    for (int i = 1; i < band.Count; i++)
                    {
                        buffer.UnionTopology(band[0], band[i]);
                    }
                }

                latitudeBands[latitudeIndex] = band;
            }

            for (int latitudeCell = 0;
                latitudeCell < latitudeSegments;
                latitudeCell++)
            {
                List<int> lower = latitudeBands[latitudeCell];
                List<int> upper = latitudeBands[latitudeCell + 1];
                bool lowerCollapsed =
                    IsCollapsedBand(lower, longitudeSegments);
                bool upperCollapsed =
                    IsCollapsedBand(upper, longitudeSegments);
                if (lowerCollapsed && upperCollapsed)
                {
                    continue;
                }

                for (int longitudeCell = 0;
                    longitudeCell < longitudeSegments;
                    longitudeCell++)
                {
                    if (lowerCollapsed)
                    {
                        AddTriangle(
                            buffer,
                            PoleVertex(lower, longitudeCell),
                            upper[longitudeCell + 1],
                            upper[longitudeCell],
                            hint.Direction);
                        continue;
                    }

                    if (upperCollapsed)
                    {
                        AddTriangle(
                            buffer,
                            lower[longitudeCell],
                            lower[longitudeCell + 1],
                            PoleVertex(upper, longitudeCell),
                            hint.Direction);
                        continue;
                    }

                    AddTriangle(
                        buffer,
                        lower[longitudeCell],
                        lower[longitudeCell + 1],
                        upper[longitudeCell + 1],
                        hint.Direction);
                    AddTriangle(
                        buffer,
                        lower[longitudeCell],
                        upper[longitudeCell + 1],
                        upper[longitudeCell],
                        hint.Direction);
                }
            }

            PanelBuildRecord record = new PanelBuildRecord(
                panel,
                panelIndex,
                start,
                buffer.Vertices.Count - start,
                triangleIndexStart,
                buffer.Triangles.Count - triangleIndexStart);

            if (hint.Direction ==
                SphericalWrapDirection.LongitudeAlongU)
            {
                record.AddBoundary(
                    "uMin",
                    BuildLongitudeBoundary(
                        latitudeBands,
                        longitudeSegments,
                        false));
                record.AddBoundary(
                    "uMax",
                    BuildLongitudeBoundary(
                        latitudeBands,
                        longitudeSegments,
                        true));
                record.AddBoundary(
                    "vMin",
                    latitudeBands[0].ToArray());
                record.AddBoundary(
                    "vMax",
                    latitudeBands[latitudeSegments].ToArray());
            }
            else
            {
                record.AddBoundary(
                    "uMin",
                    latitudeBands[0].ToArray());
                record.AddBoundary(
                    "uMax",
                    latitudeBands[latitudeSegments].ToArray());
                record.AddBoundary(
                    "vMin",
                    BuildLongitudeBoundary(
                        latitudeBands,
                        longitudeSegments,
                        false));
                record.AddBoundary(
                    "vMax",
                    BuildLongitudeBoundary(
                        latitudeBands,
                        longitudeSegments,
                        true));
            }

            buffer.AddPanel(record);
            return record;
        }

        private static void GetSourceCoordinates(
            PanelDefinition panel,
            SphericalWrapDirection direction,
            float longitudeT,
            float latitudeT,
            out Vector2 sourcePosition,
            out Vector2 sourceUv)
        {
            float sourceU = direction ==
                SphericalWrapDirection.LongitudeAlongU
                ? longitudeT
                : latitudeT;
            float sourceV = direction ==
                SphericalWrapDirection.LongitudeAlongU
                ? latitudeT
                : longitudeT;
            sourcePosition = new Vector2(
                (sourceU - 0.5f) * panel.PhysicalSize.x,
                (sourceV - 0.5f) * panel.PhysicalSize.y);
            sourceUv = new Vector2(
                panel.CanvasRect.xMin +
                    sourceU * panel.CanvasRect.width,
                panel.CanvasRect.yMin +
                    sourceV * panel.CanvasRect.height);
        }

        private static bool IsCollapsedBand(
            IReadOnlyList<int> band,
            int longitudeSegments)
        {
            return band.Count != longitudeSegments + 1;
        }

        private static int PoleVertex(
            IReadOnlyList<int> band,
            int longitudeCell)
        {
            return band.Count == 1 ? band[0] : band[longitudeCell];
        }

        private static void AddTriangle(
            MeshBuildBuffer buffer,
            int a,
            int b,
            int c,
            SphericalWrapDirection direction)
        {
            buffer.Triangles.Add(a);
            if (direction ==
                SphericalWrapDirection.LongitudeAlongU)
            {
                buffer.Triangles.Add(b);
                buffer.Triangles.Add(c);
            }
            else
            {
                buffer.Triangles.Add(c);
                buffer.Triangles.Add(b);
            }
        }

        private static int[] BuildLongitudeBoundary(
            IReadOnlyList<List<int>> latitudeBands,
            int longitudeSegments,
            bool maximum)
        {
            int[] boundary = new int[latitudeBands.Count];
            for (int i = 0; i < latitudeBands.Count; i++)
            {
                IReadOnlyList<int> band = latitudeBands[i];
                boundary[i] = maximum
                    ? band[band.Count == longitudeSegments + 1
                        ? longitudeSegments
                        : band.Count - 1]
                    : band[0];
            }

            return boundary;
        }

        private static PanelBuildRecord AppendDisk(PanelDefinition panel, MeshBuildBuffer buffer)
        {
            int start = buffer.Vertices.Count;
            int triangleIndexStart = buffer.Triangles.Count;
            int panelIndex = buffer.PanelCount;
            int segments = panel.RadialSegments;
            int rings = panel.RadialRings;
            Vector2 radius = panel.PhysicalSize * 0.5f;
            Rect canvasRect = panel.CanvasRect;

            buffer.AddVertex(
                Vector3.zero,
                Vector2.zero,
                canvasRect.center,
                panelIndex);

            for (int ring = 1; ring <= rings; ring++)
            {
                float radius01 = ring / (float)rings;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    float unitX = Mathf.Cos(angle) * radius01;
                    float unitY = Mathf.Sin(angle) * radius01;
                    Vector2 sourcePosition = new Vector2(
                        unitX * radius.x,
                        unitY * radius.y);
                    Vector2 sourceUv = new Vector2(
                        canvasRect.xMin + (unitX * 0.5f + 0.5f) * canvasRect.width,
                        canvasRect.yMin + (unitY * 0.5f + 0.5f) * canvasRect.height);
                    buffer.AddVertex(
                        new Vector3(sourcePosition.x, sourcePosition.y, 0f),
                        sourcePosition,
                        sourceUv,
                        panelIndex);
                }
            }

            int firstRingStart = start + 1;
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                buffer.Triangles.Add(start);
                buffer.Triangles.Add(firstRingStart + segment);
                buffer.Triangles.Add(firstRingStart + next);
            }

            for (int ring = 2; ring <= rings; ring++)
            {
                int previousStart = start + 1 + (ring - 2) * segments;
                int currentStart = start + 1 + (ring - 1) * segments;

                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = previousStart + segment;
                    int b = previousStart + next;
                    int c = currentStart + next;
                    int d = currentStart + segment;

                    buffer.Triangles.Add(a);
                    buffer.Triangles.Add(d);
                    buffer.Triangles.Add(c);

                    buffer.Triangles.Add(a);
                    buffer.Triangles.Add(c);
                    buffer.Triangles.Add(b);
                }
            }

            PanelBuildRecord record = new PanelBuildRecord(
                panel,
                panelIndex,
                start,
                1 + rings * segments,
                triangleIndexStart,
                buffer.Triangles.Count - triangleIndexStart);

            int perimeterStart = start + 1 + (rings - 1) * segments;
            int[] perimeter = new int[segments];
            for (int segment = 0; segment < segments; segment++)
            {
                perimeter[segment] = perimeterStart + segment;
            }

            record.AddBoundary("perimeter", perimeter, true);
            buffer.AddPanel(record);
            return record;
        }
    }
}
