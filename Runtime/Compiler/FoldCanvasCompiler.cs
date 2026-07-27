using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    public static class FoldCanvasCompiler
    {
        public static FoldCanvasCompileResult Compile(FoldCanvasAsset asset)
        {
            FoldCanvasCompileResult result = new FoldCanvasCompileResult();
            if (asset == null)
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NullAsset,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The FoldCanvas asset is null."));
                return result;
            }

            FoldCanvasSourceValidator.ValidatePanels(asset, result);
            ValidateOperationIds(asset, result);

            MeshBuildBuffer buffer = null;
            if (!result.HasErrors())
            {
                buffer = new MeshBuildBuffer();
                for (int i = 0; i < asset.Panels.Count; i++)
                {
                    PanelTessellator.Append(asset.Panels[i], buffer);
                }

                ExecuteOperations(asset, buffer, result);
            }

            if (!result.HasErrors())
            {
                ValidateGeneratedGeometry(buffer, result);
            }

            if (result.HasErrors())
            {
                return result;
            }

            string meshName = string.IsNullOrWhiteSpace(asset.name)
                ? "FoldCanvas_Generated"
                : asset.name + "_Generated";
            bool recalculateNormals =
                asset.CompileSettings == null ||
                asset.CompileSettings.RecalculateNormals;

            result.CompiledData = buffer.Freeze();
            result.Mesh = UnityMeshConverter.Create(
                result.CompiledData,
                meshName,
                recalculateNormals);
            return result;
        }

        private static void ValidateOperationIds(
            FoldCanvasAsset asset,
            FoldCanvasCompileResult result)
        {
            HashSet<string> operationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                FoldOperationDefinition operation = asset.Operations[i];
                if (operation == null)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.DuplicateOperationId,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Operation at index {i} is null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(operation.Id) || !operationIds.Add(operation.Id))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.DuplicateOperationId,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Operation IDs must be non-empty and unique.",
                        operationId: operation.Id));
                }
            }
        }

        private static void ExecuteOperations(
            FoldCanvasAsset asset,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                FoldOperationDefinition operation = asset.Operations[i];
                if (operation == null || !operation.Enabled)
                {
                    continue;
                }

                if (operation is RigidTransformOperationDefinition rigidTransform)
                {
                    RigidTransformExecutor.TryExecute(rigidTransform, buffer, result);
                    continue;
                }

                if (operation is FoldLineOperationDefinition fold)
                {
                    FoldLineExecutor.TryExecute(fold, buffer, result);
                    continue;
                }

                if (operation is RollOperationDefinition roll)
                {
                    RollExecutor.TryExecute(roll, buffer, result);
                    continue;
                }

                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedOperation,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Operation type '{operation.Type}' is not implemented in the bootstrap compiler. Follow the active milestone instead of silently ignoring it.",
                    operationId: operation.Id));
            }
        }

        private static void ValidateGeneratedGeometry(
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            for (int i = 0; i < buffer.Vertices.Count; i++)
            {
                if (!FiniteMath.IsFinite(buffer.Vertices[i].Position))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.NonFiniteVertex,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Generated vertex {i} contains NaN or infinity."));
                    return;
                }
            }

            for (int i = 0; i < buffer.Triangles.Count; i += 3)
            {
                Vector3 a = buffer.Vertices[buffer.Triangles[i]].Position;
                Vector3 b = buffer.Vertices[buffer.Triangles[i + 1]].Position;
                Vector3 c = buffer.Vertices[buffer.Triangles[i + 2]].Position;
                float areaSquared = Vector3.Cross(b - a, c - a).sqrMagnitude;
                if (areaSquared <=
                    FoldCanvasGeometryTolerances.MinimumTriangleDoubleAreaSquared)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.ZeroAreaTriangle,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Generated triangle {i / 3} has zero or near-zero area."));
                    return;
                }
            }
        }
    }
}
