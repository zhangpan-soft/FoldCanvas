using System;
using UnityEngine;

namespace FoldCanvas.Tests
{
    [Serializable]
    internal sealed class M10OffsetOperationDefinition :
        FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField]
        private Vector3 offset;

        public override FoldOperationType Type =>
            FoldOperationType.Custom;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public Vector3 Offset
        {
            get => offset;
            set => offset = value;
        }
    }

    [Serializable]
    internal sealed class M10SecondaryOperationDefinition :
        FoldOperationDefinition
    {
        public override FoldOperationType Type =>
            FoldOperationType.Custom;
    }

    internal enum M10ExtensionBehavior
    {
        Succeed,
        FailValidation,
        FailExecution,
        WriteNonFinite,
        ThrowDuringValidation,
        ThrowDuringExecution
    }

    internal sealed class M10OffsetOperationExecutor :
        IFoldCanvasOperationExecutor
    {
        private readonly string operationTypeId;
        private readonly Type definitionType;
        private readonly M10ExtensionBehavior behavior;

        public M10OffsetOperationExecutor(
            string typeId = "com.foldcanvas.tests.offset",
            Type registeredDefinitionType = null,
            M10ExtensionBehavior executionBehavior =
                M10ExtensionBehavior.Succeed)
        {
            operationTypeId = typeId;
            definitionType = registeredDefinitionType ??
                typeof(M10OffsetOperationDefinition);
            behavior = executionBehavior;
        }

        public string OperationTypeId => operationTypeId;

        public string DisplayName => "M10 Test Offset";

        public Type OperationDefinitionType => definitionType;

        public FoldCanvasOperationMutationKind MutationKind =>
            FoldCanvasOperationMutationKind.PositionOnly;

        public bool TryValidate(
            FoldOperationDefinition operation,
            FoldCanvasOperationValidationContext context,
            out string targetPanelId)
        {
            if (behavior ==
                M10ExtensionBehavior.ThrowDuringValidation)
            {
                throw new InvalidOperationException(
                    "Intentional test exception.");
            }

            if (!(operation is
                    M10OffsetOperationDefinition offset))
            {
                targetPanelId = null;
                return false;
            }

            targetPanelId = offset.PanelId;
            if (behavior == M10ExtensionBehavior.FailValidation)
            {
                return false;
            }

            if (!IsFinite(offset.Offset))
            {
                context.AddDiagnostic(new FoldCanvasDiagnostic(
                    "EXT1001",
                    FoldCanvasDiagnosticSeverity.Error,
                    "Offset must be finite.",
                    offset.PanelId,
                    operation.Id));
                return false;
            }

            return true;
        }

        public bool TryExecute(
            FoldOperationDefinition operation,
            FoldCanvasOperationExecutionContext context)
        {
            if (behavior ==
                M10ExtensionBehavior.ThrowDuringExecution)
            {
                throw new InvalidOperationException(
                    "Intentional test exception.");
            }

            M10OffsetOperationDefinition offset =
                (M10OffsetOperationDefinition)operation;
            for (int i = 0; i < context.VertexCount; i++)
            {
                FoldCanvasOperationVertex vertex =
                    context.GetVertex(i);
                Vector3 next = behavior ==
                        M10ExtensionBehavior.WriteNonFinite && i == 0
                    ? new Vector3(float.NaN, 0f, 0f)
                    : vertex.Position + offset.Offset;
                if (!context.TrySetPosition(i, next))
                {
                    return false;
                }
            }

            return behavior !=
                M10ExtensionBehavior.FailExecution;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) &&
                !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) &&
                !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) &&
                !float.IsInfinity(value.z);
        }
    }
}
