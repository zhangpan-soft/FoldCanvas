using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public enum FoldCanvasOperationMutationKind
    {
        PositionOnly = 0
    }

    public sealed class FoldCanvasOperationDescriptor
    {
        internal FoldCanvasOperationDescriptor(
            string operationTypeId,
            string displayName,
            Type definitionType,
            FoldCanvasOperationMutationKind mutationKind)
        {
            OperationTypeId = operationTypeId;
            DisplayName = displayName;
            DefinitionType = definitionType;
            MutationKind = mutationKind;
        }

        public string OperationTypeId { get; }

        public string DisplayName { get; }

        public Type DefinitionType { get; }

        public FoldCanvasOperationMutationKind MutationKind { get; }
    }

    /// <summary>
    /// Implements one explicitly registered, single-panel position operation.
    /// The executor cannot mutate topology, triangles, UVs, provenance, or the
    /// geometry budget through the public execution context.
    /// </summary>
    public interface IFoldCanvasOperationExecutor
    {
        string OperationTypeId { get; }

        string DisplayName { get; }

        Type OperationDefinitionType { get; }

        FoldCanvasOperationMutationKind MutationKind { get; }

        bool TryValidate(
            FoldOperationDefinition operation,
            FoldCanvasOperationValidationContext context,
            out string targetPanelId);

        bool TryExecute(
            FoldOperationDefinition operation,
            FoldCanvasOperationExecutionContext context);
    }

    /// <summary>
    /// An explicit registry value supplied to one compiler invocation. It is
    /// never discovered globally and registration order does not affect the
    /// descriptor or execution order.
    /// </summary>
    public sealed class FoldCanvasOperationRegistry
    {
        private const int MaximumOperationTypeIdLength = 160;
        private const int MaximumDisplayNameLength = 160;

        private readonly object sync = new object();
        private readonly List<FoldCanvasOperationRegistration> registrations =
            new List<FoldCanvasOperationRegistration>();

        public IReadOnlyList<FoldCanvasOperationDescriptor> Descriptors
        {
            get
            {
                lock (sync)
                {
                    FoldCanvasOperationRegistration[] snapshot =
                        CreateSnapshotLocked();
                    FoldCanvasOperationDescriptor[] descriptors =
                        new FoldCanvasOperationDescriptor[snapshot.Length];
                    for (int i = 0; i < snapshot.Length; i++)
                    {
                        descriptors[i] = snapshot[i].Descriptor;
                    }

                    return Array.AsReadOnly(descriptors);
                }
            }
        }

        public bool TryRegister(
            IFoldCanvasOperationExecutor executor,
            out FoldCanvasDiagnostic diagnostic)
        {
            diagnostic = null;
            if (!TryCreateRegistration(
                    executor,
                    out FoldCanvasOperationRegistration registration,
                    out diagnostic))
            {
                return false;
            }

            lock (sync)
            {
                for (int i = 0; i < registrations.Count; i++)
                {
                    FoldCanvasOperationRegistration existing =
                        registrations[i];
                    if (string.Equals(
                            existing.Descriptor.OperationTypeId,
                            registration.Descriptor.OperationTypeId,
                            StringComparison.Ordinal) ||
                        existing.Descriptor.DefinitionType ==
                            registration.Descriptor.DefinitionType)
                    {
                        diagnostic = new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .DuplicateExtensionRegistration,
                            FoldCanvasDiagnosticSeverity.Error,
                            "Extension operation type IDs and definition types must be unique within one registry.");
                        return false;
                    }
                }

                registrations.Add(registration);
                return true;
            }
        }

        internal FoldCanvasOperationRegistration[] CreateSnapshot()
        {
            lock (sync)
            {
                return CreateSnapshotLocked();
            }
        }

        private FoldCanvasOperationRegistration[] CreateSnapshotLocked()
        {
            FoldCanvasOperationRegistration[] snapshot =
                registrations.ToArray();
            Array.Sort(snapshot, CompareRegistrations);
            return snapshot;
        }

        private static int CompareRegistrations(
            FoldCanvasOperationRegistration first,
            FoldCanvasOperationRegistration second)
        {
            return string.CompareOrdinal(
                first.Descriptor.OperationTypeId,
                second.Descriptor.OperationTypeId);
        }

        private static bool TryCreateRegistration(
            IFoldCanvasOperationExecutor executor,
            out FoldCanvasOperationRegistration registration,
            out FoldCanvasDiagnostic diagnostic)
        {
            registration = null;
            diagnostic = null;
            if (executor == null)
            {
                diagnostic = InvalidRegistration(
                    "An extension executor cannot be null.");
                return false;
            }

            string operationTypeId;
            string displayName;
            Type definitionType;
            FoldCanvasOperationMutationKind mutationKind;
            try
            {
                operationTypeId = executor.OperationTypeId;
                displayName = executor.DisplayName;
                definitionType = executor.OperationDefinitionType;
                mutationKind = executor.MutationKind;
            }
            catch (Exception)
            {
                diagnostic = InvalidRegistration(
                    "An extension executor threw while describing its registration.");
                return false;
            }

            if (!IsValidOperationTypeId(operationTypeId))
            {
                diagnostic = InvalidRegistration(
                    "Extension operationTypeId must be a bounded lowercase reverse-domain identifier.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName) ||
                displayName.Length > MaximumDisplayNameLength)
            {
                diagnostic = InvalidRegistration(
                    "Extension displayName must be non-empty and bounded.");
                return false;
            }

            if (definitionType == null ||
                definitionType.IsAbstract ||
                !typeof(FoldOperationDefinition).IsAssignableFrom(
                    definitionType) ||
                IsBuiltInDefinitionType(definitionType))
            {
                diagnostic = InvalidRegistration(
                    "Extension definitionType must be one concrete custom FoldOperationDefinition type.");
                return false;
            }

            if (mutationKind != FoldCanvasOperationMutationKind.PositionOnly)
            {
                diagnostic = InvalidRegistration(
                    "M10 supports only PositionOnly extension mutations.");
                return false;
            }

            FoldCanvasOperationDescriptor descriptor =
                new FoldCanvasOperationDescriptor(
                    operationTypeId,
                    displayName,
                    definitionType,
                    mutationKind);
            registration = new FoldCanvasOperationRegistration(
                descriptor,
                executor);
            return true;
        }

        private static bool IsValidOperationTypeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > MaximumOperationTypeIdLength ||
                value.IndexOf('.') <= 0 ||
                value[value.Length - 1] == '.')
            {
                return false;
            }

            bool previousWasDot = false;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                bool validCharacter =
                    current >= 'a' && current <= 'z' ||
                    current >= '0' && current <= '9' ||
                    current == '-' ||
                    current == '.';
                if (!validCharacter ||
                    (current == '.' && previousWasDot))
                {
                    return false;
                }

                if ((i == 0 || previousWasDot) &&
                    !(current >= 'a' && current <= 'z'))
                {
                    return false;
                }

                previousWasDot = current == '.';
            }

            return true;
        }

        private static bool IsBuiltInDefinitionType(Type definitionType)
        {
            return definitionType == typeof(RigidTransformOperationDefinition) ||
                definitionType == typeof(FoldLineOperationDefinition) ||
                definitionType == typeof(RollOperationDefinition) ||
                definitionType == typeof(StitchOperationDefinition) ||
                definitionType == typeof(SolidifyOperationDefinition) ||
                definitionType == typeof(SphericalWrapOperationDefinition) ||
                definitionType == typeof(ToroidalWrapOperationDefinition);
        }

        private static FoldCanvasDiagnostic InvalidRegistration(
            string message)
        {
            return new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.InvalidExtensionRegistration,
                FoldCanvasDiagnosticSeverity.Error,
                message);
        }
    }

    public sealed class FoldCanvasOperationValidationContext
    {
        private readonly FoldCanvasAsset asset;
        private readonly FoldCanvasCompileResult result;

        internal FoldCanvasOperationValidationContext(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result,
            string operationId,
            string operationTypeId)
        {
            this.asset = asset;
            this.result = result;
            OperationId = operationId;
            OperationTypeId = operationTypeId;
        }

        public string OperationId { get; }

        public string OperationTypeId { get; }

        public bool ContainsPanel(string panelId)
        {
            if (string.IsNullOrWhiteSpace(panelId))
            {
                return false;
            }

            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                if (panel != null && string.Equals(
                        panel.Id,
                        panelId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddDiagnostic(FoldCanvasDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            result.Add(diagnostic);
        }
    }

    public readonly struct FoldCanvasOperationVertex
    {
        internal FoldCanvasOperationVertex(
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

        public Vector3 Position { get; }

        public Vector2 SourcePosition { get; }

        public Vector2 SourceUv { get; }

        public int PanelIndex { get; }

        public int ProvenanceId { get; }
    }

    public sealed class FoldCanvasOperationExecutionContext
    {
        private readonly MeshBuildBuffer buffer;
        private readonly PanelBuildRecord panel;
        private readonly FoldCanvasCompileResult result;
        private bool invalidMutation;

        internal FoldCanvasOperationExecutionContext(
            MeshBuildBuffer buffer,
            PanelBuildRecord panel,
            FoldCanvasCompileResult result,
            string operationId,
            string operationTypeId)
        {
            this.buffer = buffer;
            this.panel = panel;
            this.result = result;
            OperationId = operationId;
            OperationTypeId = operationTypeId;
        }

        public string OperationId { get; }

        public string OperationTypeId { get; }

        public string PanelId => panel.PanelId;

        public PanelShape PanelShape => panel.Shape;

        public Rect CanvasRect => panel.CanvasRect;

        public Vector2 PhysicalSize => panel.PhysicalSize;

        public int VertexCount => panel.VertexCount;

        public float PositionTolerance => buffer.PositionTolerance;

        internal bool InvalidMutation => invalidMutation;

        public FoldCanvasOperationVertex GetVertex(int localVertexIndex)
        {
            int absoluteIndex = GetAbsoluteVertexIndex(localVertexIndex);
            MeshBuildVertex vertex = buffer.Vertices[absoluteIndex];
            return new FoldCanvasOperationVertex(
                vertex.Position,
                vertex.SourcePosition,
                vertex.SourceUv,
                vertex.PanelIndex,
                vertex.ProvenanceId);
        }

        public bool TrySetPosition(
            int localVertexIndex,
            Vector3 position)
        {
            if (localVertexIndex < 0 ||
                localVertexIndex >= panel.VertexCount ||
                !FiniteMath.IsFinite(position))
            {
                if (!invalidMutation)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .ExtensionOperationInvalidMutation,
                        FoldCanvasDiagnosticSeverity.Error,
                        "An extension operation attempted a non-finite or out-of-range vertex-position mutation.",
                        panel.PanelId,
                        OperationId));
                }

                invalidMutation = true;
                return false;
            }

            int absoluteIndex = panel.VertexStart + localVertexIndex;
            MeshBuildVertex vertex = buffer.Vertices[absoluteIndex];
            vertex.Position = position;
            buffer.Vertices[absoluteIndex] = vertex;
            return true;
        }

        public void AddDiagnostic(FoldCanvasDiagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            result.Add(diagnostic);
        }

        private int GetAbsoluteVertexIndex(int localVertexIndex)
        {
            if (localVertexIndex < 0 ||
                localVertexIndex >= panel.VertexCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localVertexIndex));
            }

            return panel.VertexStart + localVertexIndex;
        }
    }

    internal sealed class FoldCanvasOperationRegistration
    {
        public FoldCanvasOperationRegistration(
            FoldCanvasOperationDescriptor descriptor,
            IFoldCanvasOperationExecutor executor)
        {
            Descriptor = descriptor;
            Executor = executor;
        }

        public FoldCanvasOperationDescriptor Descriptor { get; }

        public IFoldCanvasOperationExecutor Executor { get; }
    }
}
