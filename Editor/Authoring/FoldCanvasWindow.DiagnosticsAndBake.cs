using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoldCanvas.Editor
{
    public sealed partial class FoldCanvasWindow
    {
        private const string BakeFolderPreference =
            "FoldCanvas.M06.BakeOutputFolder";

        private void BuildDiagnosticsTab(VisualElement root)
        {
            FoldCanvasCompileResult result = controller?.LastCompileResult;
            if (controller?.Source == null)
            {
                root.Add(new HelpBox(
                    "Select a source to inspect compiler diagnostics.",
                    HelpBoxMessageType.Info));
                return;
            }

            if (controller.HasPendingCompile)
            {
                root.Add(new HelpBox(
                    "The source changed. Waiting for the debounced compile.",
                    HelpBoxMessageType.Info));
            }

            if (result == null)
            {
                Button compile = new Button(CompileNow)
                {
                    text = "Compile Now"
                };
                root.Add(compile);
                return;
            }

            Label summary = new Label(
                result.Success
                    ? "Compilation succeeded."
                    : "Compilation failed; no approximate Mesh was substituted.");
            summary.AddToClassList("section-title");
            root.Add(summary);

            Button repairPayload = new Button(CopyRepairPayload)
            {
                text = "Copy Repair Payload",
                name = "copy-repair-payload-button"
            };
            repairPayload.tooltip =
                "Copies canonical FoldScript plus deterministic diagnostics; " +
                "it never includes the generated Mesh.";
            root.Add(repairPayload);

            if (result.GeometryValidationReport != null)
            {
                FoldCanvasGeometryValidationReport validation =
                    result.GeometryValidationReport;
                root.Add(new HelpBox(
                    $"Geometry validation: {validation.ValidationLevel}; " +
                    $"components={validation.ComponentCount}, " +
                    $"open edges={validation.OpenEdgeCount}, " +
                    $"non-manifold edges={validation.NonManifoldEdgeCount}, " +
                    $"confirmed intersections=" +
                    $"{validation.ExactIntersectionPairCount}.",
                    validation.IsValid
                        ? HelpBoxMessageType.Info
                        : HelpBoxMessageType.Error));
            }

            if (result.Diagnostics.Count == 0)
            {
                root.Add(new HelpBox(
                    "No diagnostics.",
                    HelpBoxMessageType.Info));
                return;
            }

            ScrollView list = new ScrollView();
            list.style.flexGrow = 1f;
            list.name = "diagnostic-list";
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                VisualElement entry = BuildDiagnosticEntry(diagnostic, i);
                list.Add(entry);
            }

            root.Add(list);
        }

        private VisualElement BuildDiagnosticEntry(
            FoldCanvasDiagnostic diagnostic,
            int diagnosticIndex)
        {
            VisualElement entry = new VisualElement
            {
                name = "diagnostic-" + diagnosticIndex
            };
            switch (diagnostic.Severity)
            {
                case FoldCanvasDiagnosticSeverity.Error:
                    entry.AddToClassList("diagnostic-error");
                    break;
                case FoldCanvasDiagnosticSeverity.Warning:
                    entry.AddToClassList("diagnostic-warning");
                    break;
                default:
                    entry.AddToClassList("diagnostic-info");
                    break;
            }

            Label heading = new Label(
                $"{diagnostic.Code} · {diagnostic.Severity}");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            entry.Add(heading);
            entry.Add(new Label(diagnostic.Message));

            string context = BuildDiagnosticContext(diagnostic);
            if (!string.IsNullOrEmpty(context))
            {
                Label contextLabel = new Label(context);
                contextLabel.style.color = new Color(0.65f, 0.78f, 0.9f, 1f);
                entry.Add(contextLabel);
            }

            for (int i = 0; i < diagnostic.Values.Count; i++)
            {
                FoldCanvasDiagnosticValue value = diagnostic.Values[i];
                entry.Add(new Label(
                    $"{value.Key} = {value.Value:R}" +
                    (string.IsNullOrEmpty(value.Unit)
                        ? string.Empty
                        : " " + value.Unit)));
            }

            for (int i = 0; i < diagnostic.RepairSuggestions.Count; i++)
            {
                Label suggestion = new Label(
                    "Repair: " + diagnostic.RepairSuggestions[i]);
                suggestion.style.whiteSpace = WhiteSpace.Normal;
                entry.Add(suggestion);
            }

            if (!string.IsNullOrEmpty(diagnostic.PanelId) ||
                !string.IsNullOrEmpty(diagnostic.OperationId) ||
                !string.IsNullOrEmpty(diagnostic.SeamId))
            {
                Button focus = new Button(() => FocusDiagnostic(diagnostic))
                {
                    text = "Focus Source",
                    name = "focus-diagnostic-" + diagnosticIndex
                };
                entry.Add(focus);
            }

            return entry;
        }

        private static string BuildDiagnosticContext(
            FoldCanvasDiagnostic diagnostic)
        {
            string context = string.Empty;
            if (!string.IsNullOrEmpty(diagnostic.PanelId))
            {
                context += "panel=" + diagnostic.PanelId;
            }

            if (!string.IsNullOrEmpty(diagnostic.OperationId))
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "operation=" + diagnostic.OperationId;
            }

            if (!string.IsNullOrEmpty(diagnostic.SeamId))
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "seam=" + diagnostic.SeamId;
            }

            if (!string.IsNullOrEmpty(diagnostic.BoundaryId))
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "boundary=" + diagnostic.BoundaryId;
            }

            FoldCanvasDiagnosticGeometryContext geometry =
                diagnostic.GeometryContext;
            if (geometry == null || !geometry.HasValue)
            {
                return context;
            }

            if (geometry.VertexIndex >= 0)
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "vertex=" + geometry.VertexIndex;
            }

            if (geometry.TopologyVertexId >= 0)
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "topologyVertex=" + geometry.TopologyVertexId;
            }

            if (geometry.ComponentIndex >= 0)
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "component=" + geometry.ComponentIndex;
            }

            if (geometry.TriangleIndex >= 0)
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "triangle=" + geometry.TriangleIndex;
            }

            if (geometry.RelatedTriangleIndex >= 0)
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    "relatedTriangle=" +
                    geometry.RelatedTriangleIndex;
            }

            if (geometry.EdgeTopologyVertexA >= 0 &&
                geometry.EdgeTopologyVertexB >= 0)
            {
                context += (context.Length == 0 ? string.Empty : " · ") +
                    $"edge=({geometry.EdgeTopologyVertexA}," +
                    $"{geometry.EdgeTopologyVertexB})";
            }

            return context;
        }

        private void FocusDiagnostic(FoldCanvasDiagnostic diagnostic)
        {
            if (!string.IsNullOrEmpty(diagnostic.PanelId) &&
                controller.FindPanel(diagnostic.PanelId) != null)
            {
                selectedPanelId = diagnostic.PanelId;
                canvasView.SetSelection(selectedPanelId);
                SwitchTab(WorkspaceTab.Panels);
                return;
            }

            if (!string.IsNullOrEmpty(diagnostic.OperationId))
            {
                for (int i = 0; i < controller.Source.Operations.Count; i++)
                {
                    if (string.Equals(
                            controller.Source.Operations[i]?.Id,
                            diagnostic.OperationId,
                            StringComparison.Ordinal))
                    {
                        selectedOperationIndex = i;
                        SwitchTab(WorkspaceTab.Operations);
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(diagnostic.SeamId) &&
                controller.FindSeam(diagnostic.SeamId) != null)
            {
                SelectSeam(diagnostic.SeamId);
            }
        }

        private void BuildBakeTab(VisualElement root)
        {
            FoldCanvasAsset source = controller?.Source;
            if (source == null)
            {
                root.Add(new HelpBox(
                    "Select a source before baking a derived Mesh asset.",
                    HelpBoxMessageType.Info));
                return;
            }

            Label title = new Label("Bake Derived Asset");
            title.AddToClassList("section-title");
            root.Add(title);

            TextField folder = new TextField("Output Folder")
            {
                value = EditorPrefs.GetString(
                    BakeFolderPreference,
                    FoldCanvasBaker.DefaultOutputFolder),
                isDelayed = true,
                name = "bake-folder-field"
            };
            folder.RegisterValueChangedCallback(evt =>
                EditorPrefs.SetString(BakeFolderPreference, evt.newValue));
            root.Add(folder);

            FoldCanvasCompileResult result = controller.LastCompileResult;
            bool canBake = result != null && result.Success &&
                !controller.HasPendingCompile;
            Button bake = new Button(BakeCurrentSource)
            {
                text = "Bake Current Valid Result",
                name = "bake-current-button"
            };
            bake.AddToClassList("primary-button");
            bake.SetEnabled(canBake);
            root.Add(bake);

            if (!string.IsNullOrEmpty(lastBakedPath))
            {
                root.Add(new Label("Last bake: " + lastBakedPath));
            }

            if (result?.ClosedVolumeReport != null)
            {
                FoldCanvasClosedVolumeReport report = result.ClosedVolumeReport;
                root.Add(new HelpBox(
                    $"Closed volume evidence: components=" +
                    $"{report.ComponentCount}, open edges={report.OpenEdgeCount}, " +
                    $"non-manifold edges={report.NonManifoldEdgeCount}.",
                    report.IsClosedVolume
                        ? HelpBoxMessageType.Info
                        : HelpBoxMessageType.Warning));
            }

            root.Add(new HelpBox(
                "Bake recompiles the authoritative source and updates a Mesh " +
                "asset only when compilation succeeds. Preview compilation " +
                "never saves automatically, and an invalid source cannot " +
                "overwrite the last valid bake.",
                HelpBoxMessageType.Info));

            Label handoffTitle = new Label("Production Handoff");
            handoffTitle.AddToClassList("section-title");
            root.Add(handoffTitle);
            Button handoff = new Button(ExportCurrentHandoff)
            {
                text = "Export Source-First Handoff...",
                name = "export-handoff-button"
            };
            handoff.SetEnabled(canBake);
            root.Add(handoff);
            root.Add(new HelpBox(
                "The handoff archive contains canonical FoldScript and the " +
                "exact PNG as authoritative source. OBJ, validation evidence, " +
                "Mesh, Material, and Prefab remain derived and rebuildable.",
                HelpBoxMessageType.Info));
        }

        private void ExportCurrentHandoff()
        {
            FoldCanvasAsset source = controller?.Source;
            if (source == null)
            {
                if (statusLabel != null)
                {
                    statusLabel.text =
                        "Select a source before exporting a handoff.";
                }

                return;
            }

            string fileName = string.IsNullOrWhiteSpace(
                    source.SourceMetadata.AssetId)
                ? "FoldCanvasAsset.foldcanvas"
                : source.SourceMetadata.AssetId + ".foldcanvas";
            string destination = EditorUtility.SaveFilePanel(
                "Export FoldCanvas production handoff",
                string.Empty,
                fileName,
                "zip");
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            if (!destination.EndsWith(
                    ".foldcanvas.zip",
                    StringComparison.OrdinalIgnoreCase))
            {
                destination = destination.EndsWith(
                    ".zip",
                    StringComparison.OrdinalIgnoreCase)
                    ? destination.Substring(0, destination.Length - 4) +
                        ".foldcanvas.zip"
                    : destination + ".foldcanvas.zip";
            }

            FoldCanvasHandoffExportResult result =
                FoldCanvasHandoffExporter.Export(
                    source,
                    new FoldCanvasHandoffExportOptions
                    {
                        DestinationPath = destination,
                    });
            if (statusLabel != null)
            {
                statusLabel.text = result.Success
                    ? "Handoff exported: " + result.ArchiveSha256
                    : result.Diagnostics.Count > 0
                        ? result.Diagnostics[0].ToString()
                        : "Handoff export failed.";
            }
        }

        private void BakeCurrentSource()
        {
            if (controller?.Source == null)
            {
                return;
            }

            controller.CompileImmediately(EditorApplication.timeSinceStartup);
            if (controller.LastCompileResult == null ||
                !controller.LastCompileResult.Success)
            {
                SwitchTab(WorkspaceTab.Diagnostics);
                ShowNotification(new GUIContent(
                    "Bake refused: resolve compiler diagnostics first."));
                return;
            }

            string outputFolder = EditorPrefs.GetString(
                BakeFolderPreference,
                FoldCanvasBaker.DefaultOutputFolder);
            try
            {
                if (!FoldCanvasBaker.BakeMesh(
                        controller.Source,
                        outputFolder,
                        out Mesh savedMesh,
                        out FoldCanvasCompileResult bakeResult))
                {
                    ShowNotification(new GUIContent(
                        "Bake failed: " + JoinDiagnosticMessages(bakeResult)));
                    SwitchTab(WorkspaceTab.Diagnostics);
                    return;
                }

                lastBakedPath = AssetDatabase.GetAssetPath(savedMesh);
                ShowNotification(new GUIContent(
                    "Baked derived Mesh: " + lastBakedPath));
                UpdateStatus();
                if (activeTab == WorkspaceTab.Bake)
                {
                    RebuildActiveTab();
                }
            }
            catch (Exception exception)
            {
                ShowNotification(new GUIContent(exception.Message));
            }
        }

        private static string JoinDiagnosticMessages(
            FoldCanvasCompileResult result)
        {
            if (result == null || result.Diagnostics.Count == 0)
            {
                return "unknown compiler failure";
            }

            string message = result.Diagnostics[0].Code;
            if (result.Diagnostics.Count > 1)
            {
                message += $" (+{result.Diagnostics.Count - 1} more)";
            }

            return message;
        }
    }
}
