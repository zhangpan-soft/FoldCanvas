using System;
using UnityEngine;

namespace FoldCanvas.Samples.OperationExtension
{
    public sealed class WaveOperationExecutor :
        IFoldCanvasOperationExecutor
    {
        public string OperationTypeId =>
            "com.example.foldcanvas.wave";

        public string DisplayName => "Example Wave";

        public Type OperationDefinitionType =>
            typeof(WaveOperationDefinition);

        public FoldCanvasOperationMutationKind MutationKind =>
            FoldCanvasOperationMutationKind.PositionOnly;

        public bool TryValidate(
            FoldOperationDefinition operation,
            FoldCanvasOperationValidationContext context,
            out string targetPanelId)
        {
            if (!(operation is WaveOperationDefinition wave))
            {
                targetPanelId = null;
                return false;
            }

            targetPanelId = wave.PanelId;
            bool valid = IsFinite(wave.Amplitude) &&
                IsFinite(wave.Cycles) &&
                wave.Amplitude >= 0f &&
                wave.Cycles > 0f;
            if (!valid)
            {
                context.AddDiagnostic(new FoldCanvasDiagnostic(
                    "EXAMPLE1001",
                    FoldCanvasDiagnosticSeverity.Error,
                    "Wave amplitude must be finite and non-negative; cycles must be finite and positive.",
                    wave.PanelId,
                    wave.Id));
            }

            return valid;
        }

        public bool TryExecute(
            FoldOperationDefinition operation,
            FoldCanvasOperationExecutionContext context)
        {
            WaveOperationDefinition wave =
                (WaveOperationDefinition)operation;
            for (int i = 0; i < context.VertexCount; i++)
            {
                FoldCanvasOperationVertex vertex =
                    context.GetVertex(i);
                float u = vertex.SourcePosition.x /
                    context.PhysicalSize.x + 0.5f;
                float v = vertex.SourcePosition.y /
                    context.PhysicalSize.y + 0.5f;
                float envelope = 0.35f + 0.65f *
                    Mathf.Sin(Mathf.PI * v);
                float height = wave.Amplitude * envelope *
                    Mathf.Sin(2f * Mathf.PI * wave.Cycles * u);
                if (!context.TrySetPosition(
                        i,
                        vertex.Position + Vector3.forward * height))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}
