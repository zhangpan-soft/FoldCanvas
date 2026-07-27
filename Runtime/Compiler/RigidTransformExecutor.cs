using UnityEngine;

namespace FoldCanvas
{
    internal static class RigidTransformExecutor
    {
        public static bool TryExecute(
            RigidTransformOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (!buffer.TryGetPanel(operation.PanelId, out PanelBuildRecord panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.MissingPanelReference,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Rigid transform references a panel that does not exist.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            if (!FiniteMath.IsFinite(operation.Translation) ||
                !FiniteMath.IsFinite(operation.RotationEuler) ||
                !FiniteMath.IsFinite(operation.Scale))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonFiniteOperationParameter,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Rigid transform contains NaN or infinity.",
                    operation.PanelId,
                    operation.Id));
                return false;
            }

            Quaternion rotation = Quaternion.Euler(operation.RotationEuler);
            int end = panel.VertexStart + panel.VertexCount;
            for (int i = panel.VertexStart; i < end; i++)
            {
                MeshBuildVertex vertex = buffer.Vertices[i];
                Vector3 scaled = Vector3.Scale(vertex.Position, operation.Scale);
                vertex.Position = rotation * scaled + operation.Translation;
                buffer.Vertices[i] = vertex;
            }

            return true;
        }
    }
}
