using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoldCanvas
{
    public static class FoldCanvasCompiler
    {
        private const float MinimumTriangleAreaSquared = 1e-18f;

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

            MeshBuildBuffer buffer = new MeshBuildBuffer();
            ValidateAndBuildPanels(asset, buffer, result);
            ValidateOperationIds(asset, result);

            if (!result.HasErrors())
            {
                ExecuteOperations(asset, buffer, result);
            }

            if (asset.Seams.Count > 0)
            {
                for (int i = 0; i < asset.Seams.Count; i++)
                {
                    SeamDefinition seam = asset.Seams[i];
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.UnsupportedSeam,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Seam compilation is intentionally deferred to milestone M04.",
                        seamId: seam != null ? seam.Id : null));
                }
            }

            if (!result.HasErrors())
            {
                ValidateGeneratedGeometry(buffer, result);
            }

            if (result.HasErrors())
            {
                return result;
            }

            Mesh mesh = new Mesh
            {
                name = string.IsNullOrWhiteSpace(asset.name)
                    ? "FoldCanvas_Generated"
                    : asset.name + "_Generated"
            };

            if (buffer.Vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(buffer.Vertices);
            mesh.SetUVs(0, buffer.Uvs);
            mesh.SetTriangles(buffer.Triangles, 0, true);

            if (asset.CompileSettings == null || asset.CompileSettings.RecalculateNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            result.Mesh = mesh;
            return result;
        }

        private static void ValidateAndBuildPanels(
            FoldCanvasAsset asset,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            HashSet<string> panelIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < asset.Panels.Count; i++)
            {
                PanelDefinition panel = asset.Panels[i];
                if (panel == null)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidPanelDimensions,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Panel at index {i} is null."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(panel.Id) || !panelIds.Add(panel.Id))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.DuplicatePanelId,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel IDs must be non-empty and unique.",
                        panel.Id));
                    continue;
                }

                if (!FiniteMath.IsFinite(panel.PhysicalSize) ||
                    panel.PhysicalSize.x <= 0f ||
                    panel.PhysicalSize.y <= 0f)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidPanelDimensions,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel physical dimensions must be finite and greater than zero.",
                        panel.Id));
                    continue;
                }

                Rect rect = panel.CanvasRect;
                if (!FiniteMath.IsFinite(new Vector2(rect.x, rect.y)) ||
                    !FiniteMath.IsFinite(new Vector2(rect.width, rect.height)) ||
                    rect.width <= 0f ||
                    rect.height <= 0f ||
                    rect.xMin < 0f ||
                    rect.yMin < 0f ||
                    rect.xMax > 1f ||
                    rect.yMax > 1f)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.CanvasRectOutOfRange,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel canvas rect must be positive and remain inside normalized UV space.",
                        panel.Id));
                    continue;
                }

                bool tessellationValid = panel.Shape == PanelShape.Rectangle
                    ? panel.USegments >= 1 && panel.VSegments >= 1
                    : panel.RadialSegments >= 3 && panel.RadialRings >= 1;

                if (!tessellationValid)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes.InvalidTessellation,
                        FoldCanvasDiagnosticSeverity.Error,
                        "Panel tessellation counts are below the minimum.",
                        panel.Id));
                    continue;
                }

                PanelBuildRecord record = PanelTessellator.Append(panel, buffer);
                buffer.Panels.Add(panel.Id, record);
            }
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
                if (!FiniteMath.IsFinite(buffer.Vertices[i]))
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
                Vector3 a = buffer.Vertices[buffer.Triangles[i]];
                Vector3 b = buffer.Vertices[buffer.Triangles[i + 1]];
                Vector3 c = buffer.Vertices[buffer.Triangles[i + 2]];
                float areaSquared = Vector3.Cross(b - a, c - a).sqrMagnitude;
                if (areaSquared <= MinimumTriangleAreaSquared)
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
