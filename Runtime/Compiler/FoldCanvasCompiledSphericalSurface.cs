using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public sealed class FoldCanvasCompiledSphericalSurface
    {
        private readonly ReadOnlyCollection<int> surfaceVertexIndices;

        internal FoldCanvasCompiledSphericalSurface(
            string operationId,
            string panelId,
            int panelIndex,
            Vector3 center,
            Vector3 currentU,
            Vector3 currentV,
            Vector3 currentNormal,
            float radius,
            Vector2 latitudeRange,
            Vector2 longitudeRange,
            SphericalWrapDirection wrapDirection,
            SphericalPoleMode poleMode,
            SphericalSubdivisionMode subdivisionMode,
            int northPoleTopologyVertexId,
            int southPoleTopologyVertexId,
            double maximumRadiusError,
            double minimumAreaStretchRatio,
            double maximumAreaStretchRatio,
            double averageAreaStretchRatio,
            IReadOnlyList<int> orderedSurfaceVertexIndices)
        {
            OperationId = operationId ??
                throw new ArgumentNullException(nameof(operationId));
            PanelId = panelId ??
                throw new ArgumentNullException(nameof(panelId));
            PanelIndex = panelIndex;
            Center = center;
            CurrentU = currentU;
            CurrentV = currentV;
            CurrentNormal = currentNormal;
            Radius = radius;
            LatitudeRange = latitudeRange;
            LongitudeRange = longitudeRange;
            WrapDirection = wrapDirection;
            PoleMode = poleMode;
            SubdivisionMode = subdivisionMode;
            NorthPoleTopologyVertexId = northPoleTopologyVertexId;
            SouthPoleTopologyVertexId = southPoleTopologyVertexId;
            MaximumRadiusError = maximumRadiusError;
            MinimumAreaStretchRatio = minimumAreaStretchRatio;
            MaximumAreaStretchRatio = maximumAreaStretchRatio;
            AverageAreaStretchRatio = averageAreaStretchRatio;

            if (orderedSurfaceVertexIndices == null)
            {
                throw new ArgumentNullException(
                    nameof(orderedSurfaceVertexIndices));
            }

            int[] copiedIndices =
                new int[orderedSurfaceVertexIndices.Count];
            for (int i = 0; i < copiedIndices.Length; i++)
            {
                copiedIndices[i] = orderedSurfaceVertexIndices[i];
            }

            surfaceVertexIndices = Array.AsReadOnly(copiedIndices);
        }

        public string OperationId { get; }

        public string PanelId { get; }

        public int PanelIndex { get; }

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

        public int NorthPoleTopologyVertexId { get; }

        public int SouthPoleTopologyVertexId { get; }

        public bool HasNorthPole => NorthPoleTopologyVertexId >= 0;

        public bool HasSouthPole => SouthPoleTopologyVertexId >= 0;

        public double MaximumRadiusError { get; }

        public double MinimumAreaStretchRatio { get; }

        public double MaximumAreaStretchRatio { get; }

        public double AverageAreaStretchRatio { get; }

        public IReadOnlyList<int> SurfaceVertexIndices =>
            surfaceVertexIndices;
    }
}
