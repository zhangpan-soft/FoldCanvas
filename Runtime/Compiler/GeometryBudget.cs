using System;

namespace FoldCanvas
{
    internal sealed class GeometryBudget
    {
        public GeometryBudget(int maxVertices, int maxTriangles)
        {
            if (maxVertices <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxVertices));
            }

            if (maxTriangles <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTriangles));
            }

            MaxVertices = maxVertices;
            MaxTriangles = maxTriangles;
        }

        public int MaxVertices { get; }

        public int MaxTriangles { get; }

        public int UsedVertices { get; private set; }

        public int UsedTriangles { get; private set; }

        public bool TryReserve(
            long additionalVertices,
            long additionalTriangles,
            string operationId,
            string operationType,
            out GeometryReservation reservation,
            out FoldCanvasDiagnostic diagnostic)
        {
            reservation = null;
            diagnostic = null;
            if (additionalVertices < 0L ||
                additionalTriangles < 0L ||
                additionalVertices > int.MaxValue ||
                additionalTriangles > int.MaxValue)
            {
                diagnostic = CreateOverflowDiagnostic(
                    operationId,
                    operationType,
                    additionalVertices,
                    additionalTriangles);
                return false;
            }

            if (additionalVertices >
                (long)MaxVertices - UsedVertices)
            {
                diagnostic = CreateLimitDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded,
                    "vertex",
                    UsedVertices,
                    additionalVertices,
                    MaxVertices,
                    operationId,
                    operationType);
                return false;
            }

            if (additionalTriangles >
                (long)MaxTriangles - UsedTriangles)
            {
                diagnostic = CreateLimitDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .GeneratedTriangleLimitExceeded,
                    "triangle",
                    UsedTriangles,
                    additionalTriangles,
                    MaxTriangles,
                    operationId,
                    operationType);
                return false;
            }

            reservation = new GeometryReservation(
                this,
                (int)additionalVertices,
                (int)additionalTriangles,
                operationId,
                operationType);
            return true;
        }

        internal bool TryConsumeVertex(
            GeometryReservation reservation,
            out FoldCanvasDiagnostic diagnostic)
        {
            diagnostic = null;
            if (reservation == null ||
                reservation.ConsumedVertices >=
                    reservation.ReservedVertices)
            {
                diagnostic = CreateReservationMismatchDiagnostic(
                    reservation,
                    1,
                    0);
                return false;
            }

            if (UsedVertices >= MaxVertices)
            {
                diagnostic = CreateLimitDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .GeneratedVertexLimitExceeded,
                    "vertex",
                    UsedVertices,
                    1,
                    MaxVertices,
                    reservation.OperationId,
                    reservation.OperationType);
                return false;
            }

            reservation.ConsumedVertices++;
            UsedVertices++;
            return true;
        }

        internal bool TryConsumeTriangle(
            GeometryReservation reservation,
            out FoldCanvasDiagnostic diagnostic)
        {
            diagnostic = null;
            if (reservation == null ||
                reservation.ConsumedTriangles >=
                    reservation.ReservedTriangles)
            {
                diagnostic = CreateReservationMismatchDiagnostic(
                    reservation,
                    0,
                    1);
                return false;
            }

            if (UsedTriangles >= MaxTriangles)
            {
                diagnostic = CreateLimitDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .GeneratedTriangleLimitExceeded,
                    "triangle",
                    UsedTriangles,
                    1,
                    MaxTriangles,
                    reservation.OperationId,
                    reservation.OperationType);
                return false;
            }

            reservation.ConsumedTriangles++;
            UsedTriangles++;
            return true;
        }

        internal bool TryConsumeImmediateVertex(
            out FoldCanvasDiagnostic diagnostic)
        {
            if (!TryReserve(
                    1,
                    0,
                    null,
                    "MeshBuildBuffer",
                    out GeometryReservation reservation,
                    out diagnostic))
            {
                return false;
            }

            return TryConsumeVertex(reservation, out diagnostic);
        }

        internal bool TryConsumeImmediateTriangle(
            out FoldCanvasDiagnostic diagnostic)
        {
            if (!TryReserve(
                    0,
                    1,
                    null,
                    "MeshBuildBuffer",
                    out GeometryReservation reservation,
                    out diagnostic))
            {
                return false;
            }

            return TryConsumeTriangle(reservation, out diagnostic);
        }

        internal void RestoreUsage(int vertices, int triangles)
        {
            if (vertices < 0 ||
                vertices > MaxVertices ||
                triangles < 0 ||
                triangles > MaxTriangles)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vertices),
                    "Geometry budget usage snapshots must remain within configured limits.");
            }

            UsedVertices = vertices;
            UsedTriangles = triangles;
        }

        private static FoldCanvasDiagnostic CreateLimitDiagnostic(
            string code,
            string resourceName,
            int current,
            long requested,
            int maximum,
            string operationId,
            string operationType)
        {
            return new FoldCanvasDiagnostic(
                code,
                FoldCanvasDiagnosticSeverity.Error,
                $"{operationType ?? "Geometry"} cannot reserve the requested generated {resourceName} count without exceeding the cumulative compile limit.",
                operationId: operationId,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "currentUsed",
                        current),
                    new FoldCanvasDiagnosticValue(
                        "requestedAdditional",
                        requested),
                    new FoldCanvasDiagnosticValue(
                        "maximumAllowed",
                        maximum)
                });
        }

        private FoldCanvasDiagnostic CreateOverflowDiagnostic(
            string operationId,
            string operationType,
            long additionalVertices,
            long additionalTriangles)
        {
            return new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.GeometryBudgetOverflow,
                FoldCanvasDiagnosticSeverity.Error,
                $"{operationType ?? "Geometry"} requested geometry counts that cannot be represented safely.",
                operationId: operationId,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "currentVertices",
                        UsedVertices),
                    new FoldCanvasDiagnosticValue(
                        "currentTriangles",
                        UsedTriangles),
                    new FoldCanvasDiagnosticValue(
                        "requestedVertices",
                        additionalVertices),
                    new FoldCanvasDiagnosticValue(
                        "requestedTriangles",
                        additionalTriangles)
                });
        }

        private FoldCanvasDiagnostic CreateReservationMismatchDiagnostic(
            GeometryReservation reservation,
            int requestedVertices,
            int requestedTriangles)
        {
            return new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.GeometryBudgetOverflow,
                FoldCanvasDiagnosticSeverity.Error,
                "A geometry operation attempted to emit more geometry than its preflight reservation.",
                operationId: reservation?.OperationId,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "currentVertices",
                        UsedVertices),
                    new FoldCanvasDiagnosticValue(
                        "currentTriangles",
                        UsedTriangles),
                    new FoldCanvasDiagnosticValue(
                        "requestedVertices",
                        requestedVertices),
                    new FoldCanvasDiagnosticValue(
                        "requestedTriangles",
                        requestedTriangles)
                });
        }
    }

    internal sealed class GeometryReservation : IDisposable
    {
        private readonly Action<GeometryReservation> disposeAction;

        internal GeometryReservation(
            GeometryBudget budget,
            int reservedVertices,
            int reservedTriangles,
            string operationId,
            string operationType,
            Action<GeometryReservation> onDispose = null)
        {
            Budget = budget ??
                throw new ArgumentNullException(nameof(budget));
            ReservedVertices = reservedVertices;
            ReservedTriangles = reservedTriangles;
            OperationId = operationId;
            OperationType = operationType;
            disposeAction = onDispose;
        }

        internal GeometryBudget Budget { get; }

        internal int ReservedVertices { get; }

        internal int ReservedTriangles { get; }

        internal int ConsumedVertices { get; set; }

        internal int ConsumedTriangles { get; set; }

        internal string OperationId { get; }

        internal string OperationType { get; }

        public void Dispose()
        {
            disposeAction?.Invoke(this);
        }
    }

    internal sealed class GeometryBudgetExceededException : Exception
    {
        public GeometryBudgetExceededException(
            FoldCanvasDiagnostic diagnostic)
            : base(diagnostic?.Message)
        {
            Diagnostic = diagnostic ??
                throw new ArgumentNullException(nameof(diagnostic));
        }

        public FoldCanvasDiagnostic Diagnostic { get; }
    }
}
