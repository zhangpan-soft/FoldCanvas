using UnityEngine;

namespace FoldCanvas
{
    internal static class PanelTessellator
    {
        public static PanelBuildRecord Append(PanelDefinition panel, MeshBuildBuffer buffer)
        {
            switch (panel.Shape)
            {
                case PanelShape.Rectangle:
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

            record.AddBoundary("perimeter", perimeter);
            buffer.AddPanel(record);
            return record;
        }
    }
}
