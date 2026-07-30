using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    internal struct MeshBuildVertex
    {
        public MeshBuildVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex,
            int provenanceId)
        {
            Position = position;
            SourcePosition = sourcePosition;
            SourceUv = sourceUv;
            PanelIndex = panelIndex;
            ProvenanceId = provenanceId;
        }

        public Vector3 Position;

        public readonly Vector2 SourcePosition;

        public readonly Vector2 SourceUv;

        public readonly int PanelIndex;

        public readonly int ProvenanceId;
    }

    internal sealed class MeshBuildBuffer
    {
        private readonly GeometryBudget geometryBudget;

        private GeometryReservation activeGeometryReservation;

        private readonly List<PanelBuildRecord> orderedPanels =
            new List<PanelBuildRecord>();

        private readonly Dictionary<string, PanelBuildRecord> panelsById =
            new Dictionary<string, PanelBuildRecord>(StringComparer.Ordinal);

        private readonly List<int> topologyParents = new List<int>();

        private readonly List<FoldCanvasCompiledCornerSegment>
            cornerSegments =
                new List<FoldCanvasCompiledCornerSegment>();

        private readonly List<SphericalWrapBuildRecord> sphericalWraps =
            new List<SphericalWrapBuildRecord>();

        private readonly Dictionary<int, SphericalWrapBuildRecord>
            sphericalWrapsByPanelIndex =
                new Dictionary<int, SphericalWrapBuildRecord>();

        public MeshBuildBuffer(
            int maxGeneratedVertices =
                FoldCanvasCompileSettings.DefaultMaxGeneratedVertices,
            int maxGeneratedTriangles =
                FoldCanvasCompileSettings.DefaultMaxGeneratedTriangles,
            float positionTolerance =
                FoldCanvasCompileSettings.DefaultWeldEpsilon)
        {
            geometryBudget = new GeometryBudget(
                maxGeneratedVertices,
                maxGeneratedTriangles);
            PositionTolerance = positionTolerance;
            Vertices = new MeshBuildVertexCollection();
            Triangles = new MeshBuildTriangleCollection(this);
        }

        public MeshBuildVertexCollection Vertices { get; }

        public MeshBuildTriangleCollection Triangles { get; }

        public int PanelCount => orderedPanels.Count;

        public bool HasTopologyWelds { get; private set; }

        public int UsedGeneratedVertices =>
            geometryBudget.UsedVertices;

        public int UsedGeneratedTriangles =>
            geometryBudget.UsedTriangles;

        public int MaxGeneratedVertices =>
            geometryBudget.MaxVertices;

        public int MaxGeneratedTriangles =>
            geometryBudget.MaxTriangles;

        public float PositionTolerance { get; }

        public bool TryBeginGeometryOperation(
            long additionalVertices,
            long additionalTriangles,
            string operationId,
            string operationType,
            FoldCanvasCompileResult result,
            out GeometryOperationScope scope)
        {
            scope = null;
            if (activeGeometryReservation != null)
            {
                throw new InvalidOperationException(
                    "Geometry budget reservations cannot be nested.");
            }

            if (!geometryBudget.TryReserve(
                    additionalVertices,
                    additionalTriangles,
                    operationId,
                    operationType,
                    out GeometryReservation reservation,
                    out FoldCanvasDiagnostic diagnostic))
            {
                result.Add(diagnostic);
                return false;
            }

            activeGeometryReservation = reservation;
            scope = new GeometryOperationScope(this, reservation);
            return true;
        }

        public MeshBuildBufferTransaction BeginTransaction()
        {
            if (activeGeometryReservation != null)
            {
                throw new InvalidOperationException(
                    "A mesh transaction cannot begin during an active geometry reservation.");
            }

            return new MeshBuildBufferTransaction(
                this,
                CaptureState());
        }

        public int AddVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex)
        {
            return AddVertex(
                position,
                sourcePosition,
                sourceUv,
                panelIndex,
                Vertices.Count);
        }

        public int AddVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex,
            int provenanceId)
        {
            if (!TryConsumeVertex(out FoldCanvasDiagnostic diagnostic))
            {
                throw new GeometryBudgetExceededException(diagnostic);
            }

            int vertexIndex = Vertices.Count;
            Vertices.AddUnchecked(new MeshBuildVertex(
                position,
                sourcePosition,
                sourceUv,
                panelIndex,
                provenanceId));
            topologyParents.Add(vertexIndex);
            return vertexIndex;
        }

        public bool TryAddVertex(
            Vector3 position,
            Vector2 sourcePosition,
            Vector2 sourceUv,
            int panelIndex,
            out int vertexIndex,
            out FoldCanvasDiagnostic diagnostic)
        {
            if (!TryConsumeVertex(out diagnostic))
            {
                vertexIndex = -1;
                return false;
            }

            vertexIndex = Vertices.Count;
            Vertices.AddUnchecked(new MeshBuildVertex(
                position,
                sourcePosition,
                sourceUv,
                panelIndex,
                vertexIndex));
            topologyParents.Add(vertexIndex);
            return true;
        }

        public bool TryAddTriangle(
            int first,
            int second,
            int third,
            out FoldCanvasDiagnostic diagnostic)
        {
            if (Triangles.Count % 3 != 0)
            {
                throw new InvalidOperationException(
                    "Triangle indices must be emitted in complete triples.");
            }

            if (!TryConsumeTriangle(out diagnostic))
            {
                return false;
            }

            Triangles.AddUnchecked(first);
            Triangles.AddUnchecked(second);
            Triangles.AddUnchecked(third);
            return true;
        }

        public void AddPanel(PanelBuildRecord panel)
        {
            orderedPanels.Add(panel);
            panelsById.Add(panel.PanelId, panel);
        }

        public void AddCornerSegment(
            string operationId,
            int outerFromVertexIndex,
            int outerToVertexIndex,
            int innerFromVertexIndex,
            int innerToVertexIndex)
        {
            cornerSegments.Add(new FoldCanvasCompiledCornerSegment(
                operationId,
                outerFromVertexIndex,
                outerToVertexIndex,
                innerFromVertexIndex,
                innerToVertexIndex));
        }

        public void AddSphericalWrap(SphericalWrapBuildRecord sphericalWrap)
        {
            if (sphericalWrap == null)
            {
                throw new ArgumentNullException(nameof(sphericalWrap));
            }

            sphericalWraps.Add(sphericalWrap);
            sphericalWrapsByPanelIndex.Add(
                sphericalWrap.PanelIndex,
                sphericalWrap);
        }

        public bool TryEvaluateSphericalPosition(
            int panelIndex,
            Vector2 sourcePosition,
            out Vector3 position,
            out SphericalWrapBuildRecord sphericalWrap)
        {
            if (!sphericalWrapsByPanelIndex.TryGetValue(
                panelIndex,
                out sphericalWrap))
            {
                position = default;
                return false;
            }

            position = sphericalWrap.Evaluate(sourcePosition);
            return true;
        }

        public bool TryGetSphericalWrap(
            int panelIndex,
            out SphericalWrapBuildRecord sphericalWrap)
        {
            return sphericalWrapsByPanelIndex.TryGetValue(
                panelIndex,
                out sphericalWrap);
        }

        public bool TryGetPanel(string panelId, out PanelBuildRecord panel)
        {
            if (string.IsNullOrWhiteSpace(panelId))
            {
                panel = null;
                return false;
            }

            return panelsById.TryGetValue(panelId, out panel);
        }

        public PanelBuildRecord GetPanelAt(int panelIndex)
        {
            return orderedPanels[panelIndex];
        }

        public int GetTopologyId(int vertexIndex)
        {
            return FindTopologyRoot(vertexIndex);
        }

        public void UnionTopology(int firstVertexIndex, int secondVertexIndex)
        {
            int firstRoot = FindTopologyRoot(firstVertexIndex);
            int secondRoot = FindTopologyRoot(secondVertexIndex);
            if (firstRoot == secondRoot)
            {
                return;
            }

            int representative = Math.Min(firstRoot, secondRoot);
            int merged = Math.Max(firstRoot, secondRoot);
            topologyParents[merged] = representative;
            HasTopologyWelds = true;
        }

        public void SnapWeldedTopologyPositions()
        {
            if (!HasTopologyWelds)
            {
                return;
            }

            for (int i = 0; i < Vertices.Count; i++)
            {
                int representative = FindTopologyRoot(i);
                if (representative == i)
                {
                    continue;
                }

                MeshBuildVertex vertex = Vertices[i];
                vertex.Position = Vertices[representative].Position;
                Vertices[i] = vertex;
            }
        }

        public FoldCanvasCompiledData Freeze()
        {
            FoldCanvasCompiledVertex[] compiledVertices =
                new FoldCanvasCompiledVertex[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                MeshBuildVertex vertex = Vertices[i];
                compiledVertices[i] = new FoldCanvasCompiledVertex(
                    vertex.Position,
                    vertex.SourcePosition,
                    vertex.SourceUv,
                    vertex.PanelIndex,
                    vertex.ProvenanceId,
                    FindTopologyRoot(i));
            }

            FoldCanvasCompiledPanel[] compiledPanels =
                new FoldCanvasCompiledPanel[orderedPanels.Count];
            for (int i = 0; i < orderedPanels.Count; i++)
            {
                compiledPanels[i] = orderedPanels[i].Freeze();
            }

            FoldCanvasCompiledSphericalSurface[] compiledSphericalSurfaces =
                new FoldCanvasCompiledSphericalSurface[
                    sphericalWraps.Count];
            for (int i = 0; i < sphericalWraps.Count; i++)
            {
                compiledSphericalSurfaces[i] =
                    sphericalWraps[i].Freeze(this);
            }

            return new FoldCanvasCompiledData(
                compiledVertices,
                Triangles,
                compiledPanels,
                cornerSegments,
                compiledSphericalSurfaces);
        }

        private int FindTopologyRoot(int vertexIndex)
        {
            int root = vertexIndex;
            while (topologyParents[root] != root)
            {
                root = topologyParents[root];
            }

            int current = vertexIndex;
            while (topologyParents[current] != current)
            {
                int next = topologyParents[current];
                topologyParents[current] = root;
                current = next;
            }

            return root;
        }

        internal void AddTriangleIndex(int vertexIndex)
        {
            if (Triangles.Count % 3 == 0 &&
                !TryConsumeTriangle(out FoldCanvasDiagnostic diagnostic))
            {
                throw new GeometryBudgetExceededException(diagnostic);
            }

            Triangles.AddUnchecked(vertexIndex);
        }

        internal void EndGeometryOperation(
            GeometryReservation reservation)
        {
            if (!ReferenceEquals(
                    activeGeometryReservation,
                    reservation))
            {
                throw new InvalidOperationException(
                    "The active geometry reservation changed unexpectedly.");
            }

            activeGeometryReservation = null;
        }

        private MeshBuildBufferState CaptureState()
        {
            int[][][] panelBoundaryIndices =
                new int[orderedPanels.Count][][];
            for (int i = 0; i < orderedPanels.Count; i++)
            {
                panelBoundaryIndices[i] =
                    orderedPanels[i].CaptureBoundaryIndices();
            }

            int[] sphericalSurfaceVertexCounts =
                new int[sphericalWraps.Count];
            for (int i = 0; i < sphericalWraps.Count; i++)
            {
                sphericalSurfaceVertexCounts[i] =
                    sphericalWraps[i].CaptureSurfaceVertexCount();
            }

            return new MeshBuildBufferState(
                Vertices.Capture(),
                Triangles.Capture(),
                topologyParents.ToArray(),
                panelBoundaryIndices,
                sphericalSurfaceVertexCounts,
                cornerSegments.Count,
                HasTopologyWelds,
                geometryBudget.UsedVertices,
                geometryBudget.UsedTriangles);
        }

        internal void RestoreState(MeshBuildBufferState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (activeGeometryReservation != null)
            {
                throw new InvalidOperationException(
                    "A mesh transaction can only roll back after its geometry reservation has ended.");
            }

            Vertices.Restore(state.Vertices);
            Triangles.Restore(state.Triangles);
            topologyParents.Clear();
            topologyParents.AddRange(state.TopologyParents);
            for (int i = 0; i < orderedPanels.Count; i++)
            {
                orderedPanels[i].RestoreBoundaryIndices(
                    state.PanelBoundaryIndices[i]);
            }

            for (int i = 0; i < sphericalWraps.Count; i++)
            {
                sphericalWraps[i].RestoreSurfaceVertexCount(
                    state.SphericalSurfaceVertexCounts[i]);
            }

            if (cornerSegments.Count > state.CornerSegmentCount)
            {
                cornerSegments.RemoveRange(
                    state.CornerSegmentCount,
                    cornerSegments.Count - state.CornerSegmentCount);
            }

            HasTopologyWelds = state.HasTopologyWelds;
            geometryBudget.RestoreUsage(
                state.UsedVertices,
                state.UsedTriangles);
        }

        private bool TryConsumeVertex(
            out FoldCanvasDiagnostic diagnostic)
        {
            return activeGeometryReservation != null
                ? geometryBudget.TryConsumeVertex(
                    activeGeometryReservation,
                    out diagnostic)
                : geometryBudget.TryConsumeImmediateVertex(
                    out diagnostic);
        }

        private bool TryConsumeTriangle(
            out FoldCanvasDiagnostic diagnostic)
        {
            return activeGeometryReservation != null
                ? geometryBudget.TryConsumeTriangle(
                    activeGeometryReservation,
                    out diagnostic)
                : geometryBudget.TryConsumeImmediateTriangle(
                    out diagnostic);
        }
    }

    internal sealed class MeshBuildBufferState
    {
        public MeshBuildBufferState(
            MeshBuildVertex[] vertices,
            int[] triangles,
            int[] topologyParents,
            int[][][] panelBoundaryIndices,
            int[] sphericalSurfaceVertexCounts,
            int cornerSegmentCount,
            bool hasTopologyWelds,
            int usedVertices,
            int usedTriangles)
        {
            Vertices = vertices;
            Triangles = triangles;
            TopologyParents = topologyParents;
            PanelBoundaryIndices = panelBoundaryIndices;
            SphericalSurfaceVertexCounts =
                sphericalSurfaceVertexCounts;
            CornerSegmentCount = cornerSegmentCount;
            HasTopologyWelds = hasTopologyWelds;
            UsedVertices = usedVertices;
            UsedTriangles = usedTriangles;
        }

        public MeshBuildVertex[] Vertices { get; }

        public int[] Triangles { get; }

        public int[] TopologyParents { get; }

        public int[][][] PanelBoundaryIndices { get; }

        public int[] SphericalSurfaceVertexCounts { get; }

        public int CornerSegmentCount { get; }

        public bool HasTopologyWelds { get; }

        public int UsedVertices { get; }

        public int UsedTriangles { get; }
    }

    internal sealed class MeshBuildBufferTransaction : IDisposable
    {
        private MeshBuildBuffer owner;

        private readonly MeshBuildBufferState state;

        private bool committed;

        public MeshBuildBufferTransaction(
            MeshBuildBuffer owner,
            MeshBuildBufferState state)
        {
            this.owner = owner ??
                throw new ArgumentNullException(nameof(owner));
            this.state = state ??
                throw new ArgumentNullException(nameof(state));
        }

        public void Commit()
        {
            committed = true;
        }

        public void Dispose()
        {
            MeshBuildBuffer currentOwner = owner;
            if (currentOwner == null)
            {
                return;
            }

            owner = null;
            if (!committed)
            {
                currentOwner.RestoreState(state);
            }
        }
    }

    internal sealed class GeometryOperationScope : IDisposable
    {
        private MeshBuildBuffer owner;

        private readonly GeometryReservation reservation;

        public GeometryOperationScope(
            MeshBuildBuffer owner,
            GeometryReservation reservation)
        {
            this.owner = owner ??
                throw new ArgumentNullException(nameof(owner));
            this.reservation = reservation ??
                throw new ArgumentNullException(nameof(reservation));
        }

        public void Dispose()
        {
            MeshBuildBuffer currentOwner = owner;
            if (currentOwner == null)
            {
                return;
            }

            owner = null;
            currentOwner.EndGeometryOperation(reservation);
        }
    }

    internal sealed class MeshBuildVertexCollection :
        IReadOnlyList<MeshBuildVertex>
    {
        private readonly List<MeshBuildVertex> values =
            new List<MeshBuildVertex>();

        public int Count => values.Count;

        public MeshBuildVertex this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        internal void AddUnchecked(MeshBuildVertex vertex)
        {
            values.Add(vertex);
        }

        internal MeshBuildVertex[] Capture()
        {
            return values.ToArray();
        }

        internal void Restore(IReadOnlyList<MeshBuildVertex> snapshot)
        {
            values.Clear();
            for (int i = 0; i < snapshot.Count; i++)
            {
                values.Add(snapshot[i]);
            }
        }

        public IEnumerator<MeshBuildVertex> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class MeshBuildTriangleCollection :
        IReadOnlyList<int>
    {
        private readonly MeshBuildBuffer owner;

        private readonly List<int> values = new List<int>();

        public MeshBuildTriangleCollection(MeshBuildBuffer owner)
        {
            this.owner = owner ??
                throw new ArgumentNullException(nameof(owner));
        }

        public int Count => values.Count;

        public int this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        public void Add(int vertexIndex)
        {
            owner.AddTriangleIndex(vertexIndex);
        }

        internal void AddUnchecked(int vertexIndex)
        {
            values.Add(vertexIndex);
        }

        internal int[] Capture()
        {
            return values.ToArray();
        }

        internal void Restore(IReadOnlyList<int> snapshot)
        {
            values.Clear();
            for (int i = 0; i < snapshot.Count; i++)
            {
                values.Add(snapshot[i]);
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class PanelBuildRecord
    {
        private readonly List<BoundaryBuildRecord> orderedBoundaries =
            new List<BoundaryBuildRecord>();

        public PanelBuildRecord(
            PanelDefinition panel,
            int panelIndex,
            int vertexStart,
            int vertexCount,
            int triangleIndexStart,
            int triangleIndexCount)
        {
            PanelId = panel.Id;
            PanelIndex = panelIndex;
            Shape = panel.Shape;
            CanvasRect = panel.CanvasRect;
            PhysicalSize = panel.PhysicalSize;
            USegments = panel.USegments;
            VSegments = panel.VSegments;
            VertexStart = vertexStart;
            VertexCount = vertexCount;
            TriangleIndexStart = triangleIndexStart;
            TriangleIndexCount = triangleIndexCount;
        }

        public string PanelId { get; }

        public int PanelIndex { get; }

        public PanelShape Shape { get; }

        public Rect CanvasRect { get; }

        public Vector2 PhysicalSize { get; }

        public int USegments { get; }

        public int VSegments { get; }

        public int VertexStart { get; }

        public int VertexCount { get; }

        public int TriangleIndexStart { get; }

        public int TriangleIndexCount { get; }

        public void AddBoundary(
            string boundaryId,
            int[] vertexIndices,
            bool isClosed = false)
        {
            orderedBoundaries.Add(new BoundaryBuildRecord(
                boundaryId,
                vertexIndices,
                isClosed));
        }

        public bool TryGetBoundary(
            string boundaryId,
            out BoundaryBuildRecord boundary)
        {
            if (string.IsNullOrWhiteSpace(boundaryId))
            {
                boundary = null;
                return false;
            }

            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                BoundaryBuildRecord candidate = orderedBoundaries[i];
                if (string.Equals(
                    candidate.Id,
                    boundaryId,
                    StringComparison.Ordinal))
                {
                    boundary = candidate;
                    return true;
                }
            }

            boundary = null;
            return false;
        }

        public FoldCanvasCompiledPanel Freeze()
        {
            FoldCanvasCompiledBoundary[] compiledBoundaries =
                new FoldCanvasCompiledBoundary[orderedBoundaries.Count];
            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                BoundaryBuildRecord boundary = orderedBoundaries[i];
                compiledBoundaries[i] = new FoldCanvasCompiledBoundary(
                    boundary.Id,
                    boundary.VertexIndices);
            }

            return new FoldCanvasCompiledPanel(
                PanelId,
                PanelIndex,
                Shape,
                CanvasRect,
                PhysicalSize,
                VertexStart,
                VertexCount,
                compiledBoundaries);
        }

        internal int[][] CaptureBoundaryIndices()
        {
            int[][] snapshot = new int[orderedBoundaries.Count][];
            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                snapshot[i] =
                    orderedBoundaries[i].VertexIndices.ToArray();
            }

            return snapshot;
        }

        internal void RestoreBoundaryIndices(
            IReadOnlyList<int[]> snapshot)
        {
            if (snapshot.Count != orderedBoundaries.Count)
            {
                throw new InvalidOperationException(
                    "Panel boundary topology changed during a mesh transaction.");
            }

            for (int i = 0; i < orderedBoundaries.Count; i++)
            {
                List<int> indices =
                    orderedBoundaries[i].VertexIndices;
                indices.Clear();
                indices.AddRange(snapshot[i]);
            }
        }
    }

    internal sealed class BoundaryBuildRecord
    {
        public BoundaryBuildRecord(
            string id,
            IReadOnlyList<int> vertexIndices,
            bool isClosed)
        {
            Id = id;
            VertexIndices = new List<int>(vertexIndices);
            IsClosed = isClosed;
        }

        public string Id { get; }

        public List<int> VertexIndices { get; }

        public bool IsClosed { get; }
    }
}
