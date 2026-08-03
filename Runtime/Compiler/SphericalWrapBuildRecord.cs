using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal sealed class SphericalWrapBuildRecord
    {
        private readonly List<int> surfaceVertexIndices =
            new List<int>();
        private readonly HashSet<int> surfaceVertexSet =
            new HashSet<int>();

        public SphericalWrapBuildRecord(
            SphericalWrapOperationDefinition operation,
            PanelBuildRecord panel,
            Vector3 center,
            Vector3 currentU,
            Vector3 currentV,
            Vector3 currentNormal)
        {
            OperationId = operation.Id;
            PanelId = panel.PanelId;
            PanelIndex = panel.PanelIndex;
            PhysicalSize = panel.PhysicalSize;
            CanvasRect = panel.CanvasRect;
            Center = center;
            CurrentU = currentU;
            CurrentV = currentV;
            CurrentNormal = currentNormal;
            Radius = operation.Radius;
            LatitudeRange = operation.LatitudeRange;
            LongitudeRange = operation.LongitudeRange;
            WrapDirection = operation.WrapDirection;
            PoleMode = operation.PoleMode;
            SubdivisionMode = operation.SubdivisionMode;
            NorthPoleVertexIndex = -1;
            SouthPoleVertexIndex = -1;
        }

        public string OperationId { get; }

        public string PanelId { get; }

        public int PanelIndex { get; }

        public Vector2 PhysicalSize { get; }

        public Rect CanvasRect { get; }

        public Vector3 Center { get; }

        public Vector3 CurrentU { get; }

        public Vector3 CurrentV { get; }

        public Vector3 CurrentNormal { get; }

        public float Radius { get; }

        public Vector2 LatitudeRange { get; }

        public Vector2 LongitudeRange { get; }

        public SphericalWrapDirection WrapDirection { get; }

        public SphericalPoleMode PoleMode { get; }

        public SphericalSubdivisionMode SubdivisionMode { get; }

        public int NorthPoleVertexIndex { get; set; }

        public int SouthPoleVertexIndex { get; set; }

        public void RegisterSurfaceVertex(int vertexIndex)
        {
            if (surfaceVertexSet.Add(vertexIndex))
            {
                surfaceVertexIndices.Add(vertexIndex);
            }
        }

        internal int CaptureSurfaceVertexCount()
        {
            return surfaceVertexIndices.Count;
        }

        internal void RestoreSurfaceVertexCount(int count)
        {
            if (count < 0 || count > surfaceVertexIndices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (int i = surfaceVertexIndices.Count - 1;
                i >= count;
                i--)
            {
                surfaceVertexSet.Remove(surfaceVertexIndices[i]);
                surfaceVertexIndices.RemoveAt(i);
            }
        }

        public Vector3 Evaluate(Vector2 sourcePosition)
        {
            double normalizedU =
                sourcePosition.x / PhysicalSize.x + 0.5d;
            double normalizedV =
                sourcePosition.y / PhysicalSize.y + 0.5d;
            double longitudeT =
                WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? normalizedU
                    : normalizedV;
            double latitudeT =
                WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? normalizedV
                    : normalizedU;
            double longitudeDegrees =
                LongitudeRange.x +
                (LongitudeRange.y - LongitudeRange.x) *
                longitudeT;
            double latitudeDegrees =
                LatitudeRange.x +
                (LatitudeRange.y - LatitudeRange.x) *
                latitudeT;
            double longitudeRadians =
                longitudeDegrees * Math.PI / 180d;
            double latitudeRadians =
                latitudeDegrees * Math.PI / 180d;
            double cosineLatitude = Math.Cos(latitudeRadians);
            float x = (float)(
                Radius * cosineLatitude * Math.Cos(longitudeRadians));
            float y = (float)(Radius * Math.Sin(latitudeRadians));
            float z = (float)(
                Radius * cosineLatitude * Math.Sin(longitudeRadians));
            return Center +
                x * CurrentU +
                y * CurrentV +
                z * CurrentNormal;
        }

        public Vector2 InterpolateBoundarySourcePosition(
            string boundaryId,
            Vector2 from,
            Vector2 to,
            float alpha)
        {
            Vector2 sourcePosition =
                Vector2.LerpUnclamped(from, to, alpha);
            if (WrapDirection ==
                SphericalWrapDirection.LongitudeAlongU)
            {
                if (string.Equals(
                    boundaryId,
                    "uMin",
                    StringComparison.Ordinal))
                {
                    sourcePosition.x = -PhysicalSize.x * 0.5f;
                }
                else if (string.Equals(
                    boundaryId,
                    "uMax",
                    StringComparison.Ordinal))
                {
                    sourcePosition.x = PhysicalSize.x * 0.5f;
                }
            }
            else
            {
                if (string.Equals(
                    boundaryId,
                    "vMin",
                    StringComparison.Ordinal))
                {
                    sourcePosition.y = -PhysicalSize.y * 0.5f;
                }
                else if (string.Equals(
                    boundaryId,
                    "vMax",
                    StringComparison.Ordinal))
                {
                    sourcePosition.y = PhysicalSize.y * 0.5f;
                }
            }

            return sourcePosition;
        }

        public Vector2 EvaluateSourceUv(Vector2 sourcePosition)
        {
            float normalizedU =
                sourcePosition.x / PhysicalSize.x + 0.5f;
            float normalizedV =
                sourcePosition.y / PhysicalSize.y + 0.5f;
            return new Vector2(
                CanvasRect.xMin +
                    normalizedU * CanvasRect.width,
                CanvasRect.yMin +
                    normalizedV * CanvasRect.height);
        }

        public bool TryEvaluateBoundaryArcLength(
            string boundaryId,
            Vector2 from,
            Vector2 to,
            out float arcLength)
        {
            bool isLongitudeBoundary =
                WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? string.Equals(
                          boundaryId,
                          "uMin",
                          StringComparison.Ordinal) ||
                      string.Equals(
                          boundaryId,
                          "uMax",
                          StringComparison.Ordinal)
                    : string.Equals(
                          boundaryId,
                          "vMin",
                          StringComparison.Ordinal) ||
                      string.Equals(
                          boundaryId,
                          "vMax",
                          StringComparison.Ordinal);
            if (!isLongitudeBoundary)
            {
                arcLength = 0f;
                return false;
            }

            double latitudeFrom =
                EvaluateLatitudeDegrees(from);
            double latitudeTo =
                EvaluateLatitudeDegrees(to);
            arcLength = (float)(
                Radius *
                Math.Abs(latitudeTo - latitudeFrom) *
                Math.PI /
                180d);
            return true;
        }

        private double EvaluateLatitudeDegrees(Vector2 sourcePosition)
        {
            double normalizedU =
                sourcePosition.x / PhysicalSize.x + 0.5d;
            double normalizedV =
                sourcePosition.y / PhysicalSize.y + 0.5d;
            double latitudeT =
                WrapDirection ==
                    SphericalWrapDirection.LongitudeAlongU
                    ? normalizedV
                    : normalizedU;
            return LatitudeRange.x +
                (LatitudeRange.y - LatitudeRange.x) *
                latitudeT;
        }

        public FoldCanvasCompiledSphericalSurface Freeze(
            MeshBuildBuffer buffer)
        {
            double maximumRadiusError = 0d;
            for (int i = 0; i < surfaceVertexIndices.Count; i++)
            {
                Vector3 position =
                    buffer.Vertices[surfaceVertexIndices[i]].Position;
                double error = Math.Abs(
                    Vector3.Distance(position, Center) - Radius);
                maximumRadiusError = Math.Max(
                    maximumRadiusError,
                    error);
            }

            double minimumStretch = double.PositiveInfinity;
            double maximumStretch = 0d;
            double stretchTotal = 0d;
            int stretchCount = 0;
            for (int offset = 0;
                offset < buffer.Triangles.Count;
                offset += 3)
            {
                int aIndex = buffer.Triangles[offset];
                int bIndex = buffer.Triangles[offset + 1];
                int cIndex = buffer.Triangles[offset + 2];
                if (!surfaceVertexSet.Contains(aIndex) ||
                    !surfaceVertexSet.Contains(bIndex) ||
                    !surfaceVertexSet.Contains(cIndex))
                {
                    continue;
                }

                MeshBuildVertex a = buffer.Vertices[aIndex];
                MeshBuildVertex b = buffer.Vertices[bIndex];
                MeshBuildVertex c = buffer.Vertices[cIndex];
                double sourceDoubleArea = Math.Abs(
                    (b.SourcePosition.x - a.SourcePosition.x) *
                    (c.SourcePosition.y - a.SourcePosition.y) -
                    (b.SourcePosition.y - a.SourcePosition.y) *
                    (c.SourcePosition.x - a.SourcePosition.x));
                if (sourceDoubleArea <= double.Epsilon)
                {
                    continue;
                }

                double currentDoubleArea = Vector3.Cross(
                    b.Position - a.Position,
                    c.Position - a.Position).magnitude;
                double stretch = currentDoubleArea / sourceDoubleArea;
                minimumStretch = Math.Min(minimumStretch, stretch);
                maximumStretch = Math.Max(maximumStretch, stretch);
                stretchTotal += stretch;
                stretchCount++;
            }

            if (stretchCount == 0)
            {
                minimumStretch = 0d;
            }

            return new FoldCanvasCompiledSphericalSurface(
                OperationId,
                PanelId,
                PanelIndex,
                Center,
                CurrentU,
                CurrentV,
                CurrentNormal,
                Radius,
                LatitudeRange,
                LongitudeRange,
                WrapDirection,
                PoleMode,
                SubdivisionMode,
                NorthPoleVertexIndex >= 0
                    ? buffer.GetTopologyId(NorthPoleVertexIndex)
                    : -1,
                SouthPoleVertexIndex >= 0
                    ? buffer.GetTopologyId(SouthPoleVertexIndex)
                    : -1,
                maximumRadiusError,
                minimumStretch,
                maximumStretch,
                stretchCount == 0 ? 0d : stretchTotal / stretchCount,
                surfaceVertexIndices);
        }
    }
}
