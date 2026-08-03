using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal static class FoldCanvasHandoffHash
    {
        public static string Bytes(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string Text(string text)
        {
            return Bytes(new UTF8Encoding(false).GetBytes(
                text ?? string.Empty));
        }

        public static string File(string path)
        {
            using (FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!(character >= '0' && character <= '9') &&
                    !(character >= 'a' && character <= 'f'))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal static class FoldCanvasHandoffEvidenceBuilder
    {
        public static FoldCanvasHandoffEvidence Build(
            string assetId,
            string canonicalSource,
            byte[] appearanceBytes,
            string objText,
            FoldCanvasCompileResult compileResult)
        {
            if (compileResult == null)
            {
                throw new ArgumentNullException(nameof(compileResult));
            }

            FoldCanvasCompiledData data = compileResult.CompiledData;
            FoldCanvasGeometryValidationReport validation =
                compileResult.GeometryValidationReport;
            FoldCanvasClosedVolumeReport closedVolume =
                compileResult.ClosedVolumeReport;
            return new FoldCanvasHandoffEvidence
            {
                AssetId = assetId,
                PackageVersion = FoldCanvasVersion.Package,
                CompilerVersion = FoldCanvasVersion.Compiler,
                FoldScriptVersion =
                    FoldCanvasSourceMetadata.CurrentSchemaVersion,
                SourceSha256 = FoldCanvasHandoffHash.Text(canonicalSource),
                AppearanceSha256 =
                    FoldCanvasHandoffHash.Bytes(appearanceBytes),
                GeometrySha256 = HashCompiledData(data),
                ObjSha256 = FoldCanvasHandoffHash.Text(objText),
                DiagnosticSha256 = HashDiagnostics(compileResult),
                ValidationSha256 = HashValidation(validation),
                ClosedVolumeSha256 = HashClosedVolume(compileResult),
                SphereReportsSha256 = HashSphereReports(compileResult),
                ValidationLevel = validation?.ValidationLevel.ToString() ??
                    string.Empty,
                RenderVertexCount = data?.Vertices.Count ?? 0,
                TopologyVertexCount = data?.TopologyVertexCount ?? 0,
                TriangleCount = data?.TriangleIndices.Count / 3 ?? 0,
                DiagnosticCount = compileResult.Diagnostics.Count,
                ValidationErrorCount = validation?.ErrorCount ?? 0,
                ValidationWarningCount = validation?.WarningCount ?? 0,
                OpenEdgeCount = validation?.OpenEdgeCount ?? 0,
                NonManifoldEdgeCount = validation?.NonManifoldEdgeCount ?? 0,
                ComponentCount = validation?.ComponentCount ?? 0,
                ValidationIsValid = validation?.IsValid ?? false,
                IsClosedVolume = closedVolume?.IsClosedVolume ?? false,
                IsSingleClosedVolume =
                    closedVolume?.IsSingleClosedVolume ?? false,
                SphereReportCount = compileResult.SphereReports.Count,
            };
        }

        internal static string HashCompiledData(FoldCanvasCompiledData data)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = CreateWriter(stream))
            {
                writer.Write(data != null);
                if (data != null)
                {
                    writer.Write(data.Vertices.Count);
                    writer.Write(data.TopologyVertexCount);
                    for (int i = 0; i < data.Vertices.Count; i++)
                    {
                        FoldCanvasCompiledVertex vertex = data.Vertices[i];
                        WriteVector3(writer, vertex.Position);
                        WriteVector2(writer, vertex.SourcePosition);
                        WriteVector2(writer, vertex.SourceUv);
                        writer.Write(vertex.PanelIndex);
                        writer.Write(vertex.ProvenanceId);
                        writer.Write(vertex.TopologyVertexId);
                    }

                    writer.Write(data.TriangleIndices.Count);
                    for (int i = 0; i < data.TriangleIndices.Count; i++)
                    {
                        writer.Write(data.TriangleIndices[i]);
                    }

                    writer.Write(data.Panels.Count);
                    for (int i = 0; i < data.Panels.Count; i++)
                    {
                        FoldCanvasCompiledPanel panel = data.Panels[i];
                        writer.Write(panel.Id);
                        writer.Write(panel.PanelIndex);
                        writer.Write((int)panel.Shape);
                        WriteRect(writer, panel.CanvasRect);
                        WriteVector2(writer, panel.PhysicalSize);
                        writer.Write(panel.VertexStart);
                        writer.Write(panel.VertexCount);
                        writer.Write(panel.Boundaries.Count);
                        for (int boundaryIndex = 0;
                            boundaryIndex < panel.Boundaries.Count;
                            boundaryIndex++)
                        {
                            FoldCanvasCompiledBoundary boundary =
                                panel.Boundaries[boundaryIndex];
                            writer.Write(boundary.Id);
                            writer.Write(boundary.VertexIndices.Count);
                            for (int vertexIndex = 0;
                                vertexIndex < boundary.VertexIndices.Count;
                                vertexIndex++)
                            {
                                writer.Write(
                                    boundary.VertexIndices[vertexIndex]);
                            }
                        }
                    }

                    writer.Write(data.CornerSegments.Count);
                    for (int i = 0; i < data.CornerSegments.Count; i++)
                    {
                        FoldCanvasCompiledCornerSegment corner =
                            data.CornerSegments[i];
                        writer.Write(corner.OperationId);
                        writer.Write(corner.OuterFromVertexIndex);
                        writer.Write(corner.OuterToVertexIndex);
                        writer.Write(corner.InnerFromVertexIndex);
                        writer.Write(corner.InnerToVertexIndex);
                    }

                    writer.Write(data.SphericalSurfaces.Count);
                    for (int i = 0; i < data.SphericalSurfaces.Count; i++)
                    {
                        FoldCanvasCompiledSphericalSurface surface =
                            data.SphericalSurfaces[i];
                        writer.Write(surface.OperationId);
                        writer.Write(surface.PanelId);
                        writer.Write(surface.PanelIndex);
                        WriteVector3(writer, surface.Center);
                        WriteVector3(writer, surface.CurrentU);
                        WriteVector3(writer, surface.CurrentV);
                        WriteVector3(writer, surface.CurrentNormal);
                        writer.Write(surface.Radius);
                        WriteVector2(writer, surface.LatitudeRange);
                        WriteVector2(writer, surface.LongitudeRange);
                        writer.Write((int)surface.WrapDirection);
                        writer.Write((int)surface.PoleMode);
                        writer.Write((int)surface.SubdivisionMode);
                        writer.Write(surface.NorthPoleTopologyVertexId);
                        writer.Write(surface.SouthPoleTopologyVertexId);
                        writer.Write(surface.MaximumRadiusError);
                        writer.Write(surface.MinimumAreaStretchRatio);
                        writer.Write(surface.MaximumAreaStretchRatio);
                        writer.Write(surface.AverageAreaStretchRatio);
                        writer.Write(surface.SurfaceVertexIndices.Count);
                        for (int vertexIndex = 0;
                            vertexIndex < surface.SurfaceVertexIndices.Count;
                            vertexIndex++)
                        {
                            writer.Write(
                                surface.SurfaceVertexIndices[vertexIndex]);
                        }
                    }
                }

                writer.Flush();
                return FoldCanvasHandoffHash.Bytes(stream.ToArray());
            }
        }

        internal static string HashDiagnostics(
            FoldCanvasCompileResult result)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = CreateWriter(stream))
            {
                writer.Write(result.Diagnostics.Count);
                for (int i = 0; i < result.Diagnostics.Count; i++)
                {
                    FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                    writer.Write(diagnostic.Code ?? string.Empty);
                    writer.Write((int)diagnostic.Severity);
                    writer.Write(diagnostic.Message ?? string.Empty);
                    writer.Write(diagnostic.PanelId ?? string.Empty);
                    writer.Write(diagnostic.OperationId ?? string.Empty);
                    writer.Write(diagnostic.SeamId ?? string.Empty);
                    writer.Write(diagnostic.BoundaryId ?? string.Empty);
                    writer.Write(diagnostic.Values.Count);
                    for (int valueIndex = 0;
                        valueIndex < diagnostic.Values.Count;
                        valueIndex++)
                    {
                        FoldCanvasDiagnosticValue value =
                            diagnostic.Values[valueIndex];
                        writer.Write(value.Key);
                        writer.Write(value.Value);
                        writer.Write(value.Unit ?? string.Empty);
                    }

                    writer.Write(diagnostic.RepairSuggestions.Count);
                    for (int suggestionIndex = 0;
                        suggestionIndex <
                        diagnostic.RepairSuggestions.Count;
                        suggestionIndex++)
                    {
                        writer.Write(
                            diagnostic.RepairSuggestions[suggestionIndex]);
                    }

                    FoldCanvasDiagnosticGeometryContext context =
                        diagnostic.GeometryContext;
                    writer.Write(context != null);
                    if (context != null)
                    {
                        writer.Write(context.VertexIndex);
                        writer.Write(context.TopologyVertexId);
                        writer.Write(context.ComponentIndex);
                        writer.Write(context.TriangleIndex);
                        writer.Write(context.RelatedTriangleIndex);
                        writer.Write(context.EdgeTopologyVertexA);
                        writer.Write(context.EdgeTopologyVertexB);
                    }
                }

                writer.Flush();
                return FoldCanvasHandoffHash.Bytes(stream.ToArray());
            }
        }

        internal static string HashValidation(
            FoldCanvasGeometryValidationReport report)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = CreateWriter(stream))
            {
                writer.Write(report != null);
                if (report != null)
                {
                    writer.Write((int)report.ValidationLevel);
                    writer.Write(report.VertexCount);
                    writer.Write(report.TriangleIndexCount);
                    writer.Write(report.NonFiniteVertexCount);
                    writer.Write(report.InvalidTriangleIndexCount);
                    writer.Write(report.HasIncompleteTriangleIndexBuffer);
                    writer.Write(report.CollapsedTopologyTriangleCount);
                    writer.Write(report.ZeroAreaTriangleCount);
                    writer.Write(report.DuplicateTriangleCount);
                    writer.Write(report.UniqueTopologyEdgeCount);
                    writer.Write(report.OpenEdgeCount);
                    writer.Write(report.OpenBoundaryCount);
                    writer.Write(report.NonManifoldEdgeCount);
                    writer.Write(report.OrientationConflictEdgeCount);
                    writer.Write(report.TopologyPositionConflictCount);
                    writer.Write(report.BowTieTopologyVertexCount);
                    writer.Write(report.DegenerateBoundaryCount);
                    writer.Write(report.InvertedClosedComponentCount);
                    writer.Write(report.SeamMismatchCount);
                    writer.Write(report.BroadPhaseCandidatePairCount);
                    writer.Write(report.ExactIntersectionPairCount);
                    writer.Write(report.StrictChecksExecuted);
                    writer.Write(report.StrictCandidateBudgetExceeded);
                    writer.Write(report.ErrorCount);
                    writer.Write(report.WarningCount);
                    writer.Write(report.IsValid);
                    writer.Write(report.Components.Count);
                    for (int i = 0; i < report.Components.Count; i++)
                    {
                        FoldCanvasGeometryComponentReport component =
                            report.Components[i];
                        writer.Write(component.ComponentIndex);
                        writer.Write(component.MinimumTopologyVertexId);
                        writer.Write(component.TopologyVertexCount);
                        writer.Write(component.TriangleCount);
                        writer.Write(component.UniqueEdgeCount);
                        writer.Write(component.OpenEdgeCount);
                        writer.Write(component.SignedVolume);
                        writer.Write(component.MinimumAcceptedAbsoluteVolume);
                    }

                    writer.Write(report.Seams.Count);
                    for (int i = 0; i < report.Seams.Count; i++)
                    {
                        FoldCanvasGeometrySeamReport seam = report.Seams[i];
                        writer.Write(seam.OperationId ?? string.Empty);
                        writer.Write(seam.SeamId ?? string.Empty);
                        writer.Write(seam.SampleCount);
                        writer.Write(seam.MaximumGap);
                        writer.Write(seam.Tolerance);
                        writer.Write(seam.HasMatchingSampleCount);
                    }

                    writer.Write(report.Intersections.Count);
                    for (int i = 0; i < report.Intersections.Count; i++)
                    {
                        FoldCanvasTriangleIntersectionRecord intersection =
                            report.Intersections[i];
                        writer.Write(intersection.TriangleA);
                        writer.Write(intersection.TriangleB);
                        writer.Write(intersection.PanelIdA ?? string.Empty);
                        writer.Write(intersection.PanelIdB ?? string.Empty);
                    }
                }

                writer.Flush();
                return FoldCanvasHandoffHash.Bytes(stream.ToArray());
            }
        }

        internal static string HashClosedVolume(
            FoldCanvasCompileResult result)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = CreateWriter(stream))
            {
                WriteClosedVolumeReport(writer, result.ClosedVolumeReport);
                writer.Write(result.SolidifyClosedVolumeReports.Count);
                for (int i = 0;
                    i < result.SolidifyClosedVolumeReports.Count;
                    i++)
                {
                    WriteClosedVolumeReport(
                        writer,
                        result.SolidifyClosedVolumeReports[i]);
                }

                writer.Flush();
                return FoldCanvasHandoffHash.Bytes(stream.ToArray());
            }
        }

        internal static string HashSphereReports(
            FoldCanvasCompileResult result)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = CreateWriter(stream))
            {
                writer.Write(result.SphereReports.Count);
                for (int i = 0; i < result.SphereReports.Count; i++)
                {
                    FoldCanvasSphereReport report = result.SphereReports[i];
                    writer.Write(report.ComponentId);
                    writer.Write(report.PanelIds.Count);
                    for (int panelIndex = 0;
                        panelIndex < report.PanelIds.Count;
                        panelIndex++)
                    {
                        writer.Write(report.PanelIds[panelIndex]);
                    }

                    writer.Write(report.SphericalWrapOperationIds.Count);
                    for (int operationIndex = 0;
                        operationIndex <
                        report.SphericalWrapOperationIds.Count;
                        operationIndex++)
                    {
                        writer.Write(
                            report.SphericalWrapOperationIds[operationIndex]);
                    }

                    writer.Write((int)report.ValidationStage);
                    writer.Write(report.ValidationOperationId ?? string.Empty);
                    writer.Write(report.ValidationOperationIndex);
                    writer.Write(report.SphericalPanelCount);
                    writer.Write(report.SurfaceRenderVertexCount);
                    writer.Write(report.SurfaceTopologyVertexCount);
                    writer.Write(report.TriangleCount);
                    writer.Write(report.UniqueEdgeCount);
                    writer.Write(report.OpenEdgeCount);
                    writer.Write(report.NonManifoldEdgeCount);
                    writer.Write(report.OrientationConflictEdgeCount);
                    writer.Write(report.IsolatedTopologyVertexCount);
                    writer.Write(report.ConnectedComponentCount);
                    writer.Write(report.EulerCharacteristic);
                    writer.Write(report.NorthPoleTopologyCount);
                    writer.Write(report.SouthPoleTopologyCount);
                    writer.Write(report.InwardTriangleCount);
                    writer.Write(report.InconsistentFrameCount);
                    writer.Write(report.MaximumRadiusError);
                    writer.Write(report.RadiusTolerance);
                }

                writer.Flush();
                return FoldCanvasHandoffHash.Bytes(stream.ToArray());
            }
        }

        private static void WriteClosedVolumeReport(
            BinaryWriter writer,
            FoldCanvasClosedVolumeReport report)
        {
            writer.Write(report != null);
            if (report == null)
            {
                return;
            }

            writer.Write(report.OperationId ?? string.Empty);
            writer.Write(report.TriangleCount);
            writer.Write(report.TopologyVertexCount);
            writer.Write(report.UniqueEdgeCount);
            writer.Write(report.TwoIncidentEdgeCount);
            writer.Write(report.OpenEdgeCount);
            writer.Write(report.NonManifoldEdgeCount);
            writer.Write(report.OrientationConflictEdgeCount);
            writer.Write(report.CollapsedTopologyEdgeCount);
            writer.Write(report.TopologyPositionConflictCount);
            writer.Write(report.PartiallySelectedTriangleCount);
            writer.Write(report.Components.Count);
            for (int i = 0; i < report.Components.Count; i++)
            {
                FoldCanvasClosedVolumeComponentReport component =
                    report.Components[i];
                writer.Write(component.MinimumTopologyVertexId);
                writer.Write(component.TopologyVertexCount);
                writer.Write(component.TriangleCount);
                writer.Write(component.SignedVolume);
                writer.Write(component.MinimumAcceptedAbsoluteVolume);
            }
        }

        private static BinaryWriter CreateWriter(Stream stream)
        {
            return new BinaryWriter(stream, new UTF8Encoding(false), true);
        }

        private static void WriteVector2(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        private static void WriteRect(BinaryWriter writer, Rect value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.width);
            writer.Write(value.height);
        }
    }
}
