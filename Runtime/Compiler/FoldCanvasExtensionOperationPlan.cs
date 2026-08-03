using System;
using UnityEngine;

namespace FoldCanvas
{
    internal sealed class FoldCanvasExtensionOperationPlan
    {
        private readonly PlannedExtensionOperation[] operations;

        private FoldCanvasExtensionOperationPlan(int operationCount)
        {
            operations = new PlannedExtensionOperation[operationCount];
        }

        public static FoldCanvasExtensionOperationPlan Build(
            FoldCanvasAsset asset,
            FoldCanvasOperationRegistry registry,
            FoldCanvasCompileResult result)
        {
            FoldCanvasOperationRegistration[] registrations = registry != null
                ? registry.CreateSnapshot()
                : Array.Empty<FoldCanvasOperationRegistration>();
            FoldCanvasExtensionOperationPlan plan =
                new FoldCanvasExtensionOperationPlan(asset.Operations.Count);

            for (int operationIndex = 0;
                operationIndex < asset.Operations.Count;
                operationIndex++)
            {
                FoldOperationDefinition operation =
                    asset.Operations[operationIndex];
                if (operation == null ||
                    !operation.Enabled ||
                    IsBuiltInOperation(operation))
                {
                    continue;
                }

                FoldCanvasOperationRegistration registration =
                    FindRegistration(
                        registrations,
                        operation.GetType());
                if (registration == null)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .UnregisteredExtensionOperation,
                        FoldCanvasDiagnosticSeverity.Error,
                        "A custom operation must have an explicit executor registration for this compile.",
                        operationId: operation.Id));
                    continue;
                }

                FoldOperationType operationType;
                try
                {
                    operationType = operation.Type;
                }
                catch (Exception)
                {
                    AddExceptionDiagnostic(
                        result,
                        operation,
                        registration.Descriptor.OperationTypeId);
                    continue;
                }

                if (operationType != FoldOperationType.Custom)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .ExtensionOperationValidationFailed,
                        FoldCanvasDiagnosticSeverity.Error,
                        "A registered extension definition must report FoldOperationType.Custom.",
                        operationId: operation.Id));
                    continue;
                }

                int diagnosticCountBefore = result.Diagnostics.Count;
                bool valid;
                string targetPanelId;
                try
                {
                    FoldCanvasOperationValidationContext context =
                        new FoldCanvasOperationValidationContext(
                            asset,
                            result,
                            operation.Id,
                            registration.Descriptor.OperationTypeId);
                    valid = registration.Executor.TryValidate(
                        operation,
                        context,
                        out targetPanelId);
                }
                catch (Exception)
                {
                    AddExceptionDiagnostic(
                        result,
                        operation,
                        registration.Descriptor.OperationTypeId);
                    continue;
                }

                if (!valid || HasNewError(result, diagnosticCountBefore))
                {
                    if (!HasNewError(result, diagnosticCountBefore))
                    {
                        result.Add(new FoldCanvasDiagnostic(
                            FoldCanvasDiagnosticCodes
                                .ExtensionOperationValidationFailed,
                            FoldCanvasDiagnosticSeverity.Error,
                            "The extension operation rejected its source during preflight validation.",
                            operationId: operation.Id));
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(targetPanelId) ||
                    !ContainsPanel(asset, targetPanelId))
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .ExtensionOperationTargetMissing,
                        FoldCanvasDiagnosticSeverity.Error,
                        "A custom position operation must target one existing panel during preflight.",
                        targetPanelId,
                        operation.Id));
                    continue;
                }

                plan.operations[operationIndex] =
                    new PlannedExtensionOperation(
                        registration,
                        targetPanelId);
            }

            return plan;
        }

        public bool TryGetTargetPanelId(
            int operationIndex,
            out string panelId)
        {
            PlannedExtensionOperation operation = operations[operationIndex];
            if (operation == null)
            {
                panelId = null;
                return false;
            }

            panelId = operation.TargetPanelId;
            return true;
        }

        public bool TryExecute(
            int operationIndex,
            FoldOperationDefinition operation,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            PlannedExtensionOperation planned = operations[operationIndex];
            if (planned == null)
            {
                return false;
            }

            if (!buffer.TryGetPanel(
                    planned.TargetPanelId,
                    out PanelBuildRecord panel))
            {
                result.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .ExtensionOperationTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The preflight-resolved extension target panel is unavailable during execution.",
                    planned.TargetPanelId,
                    operation.Id));
                return false;
            }

            Vector3[] originalPositions =
                new Vector3[panel.VertexCount];
            for (int i = 0; i < panel.VertexCount; i++)
            {
                originalPositions[i] =
                    buffer.Vertices[panel.VertexStart + i].Position;
            }

            int diagnosticCountBefore = result.Diagnostics.Count;
            bool executed;
            try
            {
                FoldCanvasOperationExecutionContext context =
                    new FoldCanvasOperationExecutionContext(
                        buffer,
                        panel,
                        result,
                        operation.Id,
                        planned.Registration.Descriptor.OperationTypeId);
                executed = planned.Registration.Executor.TryExecute(
                    operation,
                    context);
                if (context.InvalidMutation)
                {
                    executed = false;
                }
            }
            catch (Exception)
            {
                AddExceptionDiagnostic(
                    result,
                    operation,
                    planned.Registration.Descriptor.OperationTypeId);
                executed = false;
            }

            bool hasNewError = HasNewError(
                result,
                diagnosticCountBefore);
            if (!executed || hasNewError)
            {
                RestorePositions(buffer, panel, originalPositions);
                if (!hasNewError)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .ExtensionOperationExecutionFailed,
                        FoldCanvasDiagnosticSeverity.Error,
                        "The extension operation did not complete; all panel positions were rolled back.",
                        panel.PanelId,
                        operation.Id));
                }

                return false;
            }

            return true;
        }

        private static void RestorePositions(
            MeshBuildBuffer buffer,
            PanelBuildRecord panel,
            Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                int vertexIndex = panel.VertexStart + i;
                MeshBuildVertex vertex = buffer.Vertices[vertexIndex];
                vertex.Position = positions[i];
                buffer.Vertices[vertexIndex] = vertex;
            }
        }

        private static FoldCanvasOperationRegistration FindRegistration(
            FoldCanvasOperationRegistration[] registrations,
            Type definitionType)
        {
            for (int i = 0; i < registrations.Length; i++)
            {
                if (registrations[i].Descriptor.DefinitionType ==
                    definitionType)
                {
                    return registrations[i];
                }
            }

            return null;
        }

        private static bool IsBuiltInOperation(
            FoldOperationDefinition operation)
        {
            return operation is RigidTransformOperationDefinition ||
                operation is FoldLineOperationDefinition ||
                operation is RollOperationDefinition ||
                operation is StitchOperationDefinition ||
                operation is SolidifyOperationDefinition ||
                operation is SphericalWrapOperationDefinition ||
                operation is ToroidalWrapOperationDefinition;
        }

        private static bool ContainsPanel(
            FoldCanvasAsset asset,
            string panelId)
        {
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

        private static bool HasNewError(
            FoldCanvasCompileResult result,
            int diagnosticCountBefore)
        {
            for (int i = diagnosticCountBefore;
                i < result.Diagnostics.Count;
                i++)
            {
                if (result.Diagnostics[i].Severity ==
                    FoldCanvasDiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddExceptionDiagnostic(
            FoldCanvasCompileResult result,
            FoldOperationDefinition operation,
            string operationTypeId)
        {
            result.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.ExtensionOperationException,
                FoldCanvasDiagnosticSeverity.Error,
                $"Extension operation '{operationTypeId}' threw; the compiler rejected the operation without returning partial geometry.",
                operationId: operation.Id));
        }

        private sealed class PlannedExtensionOperation
        {
            public PlannedExtensionOperation(
                FoldCanvasOperationRegistration registration,
                string targetPanelId)
            {
                Registration = registration;
                TargetPanelId = targetPanelId;
            }

            public FoldCanvasOperationRegistration Registration { get; }

            public string TargetPanelId { get; }
        }
    }
}
