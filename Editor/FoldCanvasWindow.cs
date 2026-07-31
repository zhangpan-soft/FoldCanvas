using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoldCanvas.Editor
{
    public sealed partial class FoldCanvasWindow : EditorWindow
    {
        private const string UxmlPath =
            "Packages/com.foldcanvas.core/Editor/Authoring/UI/" +
            "FoldCanvasAuthoringWindow.uxml";
        private const string UssPath =
            "Packages/com.foldcanvas.core/Editor/Authoring/UI/" +
            "FoldCanvasAuthoringWindow.uss";
        private const string SplitRatioPreference =
            "FoldCanvas.M06.CanvasSplitRatio";

        [SerializeField]
        private FoldCanvasAsset persistedSource;

        private FoldCanvasAuthoringController controller;
        private FoldCanvasPreviewRenderer previewRenderer;
        private FoldCanvasCanvasView canvasView;
        private IMGUIContainer previewContainer;
        private ObjectField assetField;
        private VisualElement tabContent;
        private Label statusLabel;
        private VisualElement mainRow;
        private VisualElement canvasPane;
        private VisualElement mainSplitter;
        private readonly Dictionary<WorkspaceTab, Button> tabButtons =
            new Dictionary<WorkspaceTab, Button>();
        private WorkspaceTab activeTab = WorkspaceTab.Panels;
        private string selectedPanelId;
        private string selectedBoundaryId;
        private string selectedSeamId;
        private int selectedOperationIndex = -1;
        private bool continuousCanvasEdit;
        [SerializeField]
        private string lastBakedPath;

        internal FoldCanvasAuthoringController Controller => controller;

        internal FoldCanvasPreviewRenderer PreviewRenderer => previewRenderer;

        internal FoldCanvasCanvasView CanvasView => canvasView;

        internal FoldCanvasAsset Source => controller?.Source;

        internal string SelectedPanelIdForTests => selectedPanelId;

        internal string SelectedSeamIdForTests => selectedSeamId;

        internal int SelectedOperationIndexForTests => selectedOperationIndex;

        [MenuItem("Tools/FoldCanvas/Open Authoring Workspace", priority = 1)]
        [MenuItem("Window/FoldCanvas/Authoring Workspace")]
        public static FoldCanvasWindow OpenAuthoringWorkspace()
        {
            FoldCanvasWindow window = FindOpenWorkspace();
            if (window == null)
            {
                if (Application.isBatchMode)
                {
                    window = CreateInstance<FoldCanvasWindow>();
                    window.CreateGUI();
                }
                else
                {
                    window = GetWindow<FoldCanvasWindow>();
                }
            }

            window.titleContent = new GUIContent("FoldCanvas Workspace");
            window.minSize = new Vector2(980f, 680f);
            if (!Application.isBatchMode)
            {
                window.Show();
            }

            return window;
        }

        private static FoldCanvasWindow FindOpenWorkspace()
        {
            FoldCanvasWindow[] windows =
                Resources.FindObjectsOfTypeAll<FoldCanvasWindow>();
            return windows.Length > 0 ? windows[0] : null;
        }

        public static void Open()
        {
            OpenAuthoringWorkspace();
        }

        private void OnEnable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedo;
            previewRenderer?.Dispose();
            previewRenderer = null;
            controller?.Dispose();
            controller = null;
            canvasView = null;
            previewContainer = null;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is FoldCanvasAsset selected &&
                !ReferenceEquals(selected, controller?.Source))
            {
                SetSource(selected);
            }
        }

        public void CreateGUI()
        {
            previewRenderer?.Dispose();
            controller?.Dispose();
            rootVisualElement.Clear();

            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (tree == null)
            {
                rootVisualElement.Add(new HelpBox(
                    "FoldCanvas M06 authoring UXML could not be loaded.",
                    HelpBoxMessageType.Error));
                return;
            }

            tree.CloneTree(rootVisualElement);
            StyleSheet styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            controller = new FoldCanvasAuthoringController();
            previewRenderer = new FoldCanvasPreviewRenderer();
            controller.SourceChanged += OnControllerSourceChanged;
            controller.CompileCompleted += OnCompileCompleted;

            QueryCoreElements();
            BuildAssetField();
            BuildCanvasPane();
            BuildPreviewPane();
            BindCoreButtons();
            BindTabs();
            ConfigureSplitter();

            FoldCanvasAsset initialSource = persistedSource != null
                ? persistedSource
                : Selection.activeObject as FoldCanvasAsset;
            SetSource(initialSource);
            SwitchTab(activeTab);
        }

        internal void SetSourceForTests(FoldCanvasAsset source)
        {
            SetSource(source);
        }

        internal void CompileNowForTests()
        {
            CompileNow();
        }

        internal void FocusDiagnosticForTests(
            FoldCanvasDiagnostic diagnostic)
        {
            FocusDiagnostic(diagnostic);
        }

        private void QueryCoreElements()
        {
            tabContent = rootVisualElement.Q<VisualElement>("tab-content");
            statusLabel = rootVisualElement.Q<Label>("status-label");
            mainRow = rootVisualElement.Q<VisualElement>("main-row");
            canvasPane = rootVisualElement.Q<VisualElement>("canvas-pane");
            mainSplitter = rootVisualElement.Q<VisualElement>("main-splitter");
        }

        private void BuildAssetField()
        {
            VisualElement host =
                rootVisualElement.Q<VisualElement>("asset-field-host");
            assetField = new ObjectField("Source")
            {
                objectType = typeof(FoldCanvasAsset),
                allowSceneObjects = false,
                name = "source-asset-field"
            };
            assetField.style.flexGrow = 1f;
            assetField.RegisterValueChangedCallback(evt =>
                SetSource(evt.newValue as FoldCanvasAsset));
            host.Add(assetField);
        }

        private void BuildCanvasPane()
        {
            VisualElement host =
                rootVisualElement.Q<VisualElement>("canvas-host");
            canvasView = new FoldCanvasCanvasView();
            canvasView.PanelSelected += SelectPanel;
            canvasView.PanelCanvasRectEditStarted += panelId =>
            {
                continuousCanvasEdit = true;
                selectedPanelId = panelId;
                controller.BeginContinuousEdit(
                    "Resize FoldCanvas Panel Canvas Rect");
            };
            canvasView.PanelCanvasRectChanged += (panelId, rect) =>
            {
                controller.MutatePanel(
                    panelId,
                    "Resize FoldCanvas Panel Canvas Rect",
                    panel => panel.CanvasRect = rect,
                    EditorApplication.timeSinceStartup,
                    false);
                canvasView.RefreshSourceLayout();
            };
            canvasView.PanelCanvasRectEditCompleted += _ =>
            {
                Undo.FlushUndoRecordObjects();
                continuousCanvasEdit = false;
                RefreshWorkspace();
            };
            host.Add(canvasView);
        }

        private void BuildPreviewPane()
        {
            VisualElement host =
                rootVisualElement.Q<VisualElement>("preview-host");
            previewContainer = new IMGUIContainer(
                previewRenderer.OnPreviewGUI)
            {
                name = "preview-imgui"
            };
            previewContainer.style.flexGrow = 1f;
            host.Add(previewContainer);

            BindToggle("solid-toggle", previewRenderer.SetSolid);
            BindToggle("wireframe-toggle", previewRenderer.SetWireframe);
            BindToggle("panel-colors-toggle", previewRenderer.SetPanelColors);
            BindToggle("seams-toggle", previewRenderer.SetSeams);
            BindToggle("normals-toggle", previewRenderer.SetNormals);
            BindToggle("thickness-toggle", previewRenderer.SetThickness);
            rootVisualElement.Q<Button>("frame-preview-button").clicked += () =>
            {
                previewRenderer.FrameResult();
                previewContainer.MarkDirtyRepaint();
            };
        }

        private void BindToggle(string elementName, Action<bool> setter)
        {
            Toggle toggle = rootVisualElement.Q<Toggle>(elementName);
            toggle.RegisterValueChangedCallback(evt =>
            {
                setter(evt.newValue);
                previewContainer.MarkDirtyRepaint();
            });
        }

        private void BindCoreButtons()
        {
            rootVisualElement.Q<Button>("new-source-button").clicked +=
                CreateNewSource;
            rootVisualElement.Q<Button>("compile-button").clicked += CompileNow;
            rootVisualElement.Q<Button>("bake-toolbar-button").clicked +=
                BakeCurrentSource;
            rootVisualElement.Q<Button>("add-rectangle-button").clicked += () =>
                AddPanel(PanelShape.Rectangle);
            rootVisualElement.Q<Button>("add-disk-button").clicked += () =>
                AddPanel(PanelShape.Disk);
            rootVisualElement.Q<Button>("frame-canvas-button").clicked += () =>
                canvasView.FrameCanvas();
        }

        private void BindTabs()
        {
            BindTab(WorkspaceTab.Panels, "panels-tab");
            BindTab(WorkspaceTab.Operations, "operations-tab");
            BindTab(WorkspaceTab.Seams, "seams-tab");
            BindTab(WorkspaceTab.Diagnostics, "diagnostics-tab");
            BindTab(WorkspaceTab.Bake, "bake-tab");
        }

        private void BindTab(WorkspaceTab tab, string elementName)
        {
            Button button = rootVisualElement.Q<Button>(elementName);
            tabButtons[tab] = button;
            button.clicked += () => SwitchTab(tab);
        }

        private void SwitchTab(WorkspaceTab tab)
        {
            activeTab = tab;
            foreach (KeyValuePair<WorkspaceTab, Button> entry in tabButtons)
            {
                entry.Value.EnableInClassList(
                    "active-tab",
                    entry.Key == activeTab);
            }

            RebuildActiveTab();
        }

        private void RebuildActiveTab()
        {
            if (tabContent == null)
            {
                return;
            }

            tabContent.Clear();
            switch (activeTab)
            {
                case WorkspaceTab.Panels:
                    BuildPanelsTab(tabContent);
                    break;
                case WorkspaceTab.Operations:
                    BuildOperationsTab(tabContent);
                    break;
                case WorkspaceTab.Seams:
                    BuildSeamsTab(tabContent);
                    break;
                case WorkspaceTab.Diagnostics:
                    BuildDiagnosticsTab(tabContent);
                    break;
                case WorkspaceTab.Bake:
                    BuildBakeTab(tabContent);
                    break;
            }
        }

        private void SetSource(FoldCanvasAsset source)
        {
            if (controller == null)
            {
                persistedSource = source;
                return;
            }

            if (!ReferenceEquals(controller.Source, source))
            {
                lastBakedPath = null;
            }

            persistedSource = source;
            assetField?.SetValueWithoutNotify(source);
            selectedPanelId = source != null && source.Panels.Count > 0
                ? source.Panels[0]?.Id
                : null;
            selectedBoundaryId = null;
            selectedSeamId = source != null && source.Seams.Count > 0
                ? source.Seams[0]?.Id
                : null;
            selectedOperationIndex = source != null &&
                source.Operations.Count > 0
                ? 0
                : -1;
            controller.SetSource(source, EditorApplication.timeSinceStartup);
            RefreshWorkspace();
        }

        private void CreateNewSource()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create FoldCanvas Source",
                "NewFoldCanvasAsset",
                "asset",
                "Choose where to save the authoritative FoldCanvas source.");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            FoldCanvasAsset created =
                FoldCanvasAuthoringController.CreateAssetAtPath(path);
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            SetSource(created);
        }

        private void CompileNow()
        {
            if (controller?.Source == null)
            {
                UpdateStatus();
                return;
            }

            controller.CompileImmediately(EditorApplication.timeSinceStartup);
        }

        private void OnEditorUpdate()
        {
            if (controller == null)
            {
                return;
            }

            if (controller.TryCompile(EditorApplication.timeSinceStartup))
            {
                previewContainer?.MarkDirtyRepaint();
            }

            UpdateStatus();
        }

        private void OnCompileCompleted(FoldCanvasCompileResult result)
        {
            previewRenderer.SetResult(
                result,
                controller.Source?.Appearance,
                controller.FindSeam(selectedSeamId));
            previewContainer.MarkDirtyRepaint();
            UpdateStatus();
            if (activeTab == WorkspaceTab.Diagnostics)
            {
                RebuildActiveTab();
            }
        }

        private void OnControllerSourceChanged()
        {
            if (continuousCanvasEdit)
            {
                canvasView.RefreshSourceLayout();
                UpdateStatus();
                return;
            }

            EnsureSelectionsAreValid();
            RefreshWorkspace();
        }

        private void OnUndoRedo()
        {
            controller?.NotifyExternalSourceChange(
                EditorApplication.timeSinceStartup);
            RefreshWorkspace();
        }

        private void EnsureSelectionsAreValid()
        {
            FoldCanvasAsset source = controller?.Source;
            if (source == null)
            {
                selectedPanelId = null;
                selectedSeamId = null;
                selectedOperationIndex = -1;
                return;
            }

            if (controller.FindPanel(selectedPanelId) == null)
            {
                selectedPanelId = source.Panels.Count > 0
                    ? source.Panels[0]?.Id
                    : null;
            }

            if (controller.FindSeam(selectedSeamId) == null)
            {
                selectedSeamId = source.Seams.Count > 0
                    ? source.Seams[0]?.Id
                    : null;
            }

            if (selectedOperationIndex < 0 ||
                selectedOperationIndex >= source.Operations.Count)
            {
                selectedOperationIndex = source.Operations.Count > 0 ? 0 : -1;
            }
        }

        private void RefreshWorkspace()
        {
            if (controller == null || canvasView == null)
            {
                return;
            }

            canvasView.SetSource(controller.Source);
            canvasView.SetSelection(selectedPanelId);
            SeamDefinition selectedSeam = controller.FindSeam(selectedSeamId);
            canvasView.SetBoundaryHighlights(selectedSeam);
            previewRenderer.SetHighlightedSeam(selectedSeam);
            previewContainer?.MarkDirtyRepaint();
            RebuildActiveTab();
            UpdateStatus();
            titleContent = new GUIContent(
                controller.Source == null
                    ? "FoldCanvas Workspace"
                    : $"FoldCanvas · {controller.Source.name}");
        }

        private void UpdateStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            if (controller?.Source == null)
            {
                statusLabel.text =
                    "Select or create an authoritative FoldCanvas source.";
                return;
            }

            if (controller.HasPendingCompile)
            {
                statusLabel.text =
                    $"Editing {controller.Source.name} · compile pending " +
                    $"(revision {controller.SourceRevision})" +
                    BuildSourceStateSuffix() + ".";
                return;
            }

            FoldCanvasCompileResult result = controller.LastCompileResult;
            if (result == null)
            {
                statusLabel.text =
                    $"Editing {controller.Source.name} · no compile yet.";
                return;
            }

            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Severity ==
                    FoldCanvasDiagnosticSeverity.Error)
                {
                    errors++;
                }
                else if (result.Diagnostics[i].Severity ==
                    FoldCanvasDiagnosticSeverity.Warning)
                {
                    warnings++;
                }
            }

            statusLabel.text = result.Mesh == null
                ? $"Compile failed · {errors} errors · {warnings} warnings" +
                    BuildSourceStateSuffix() + "."
                : $"{(result.Success ? "Valid" : "Invalid")} derived Mesh · " +
                    $"{result.Mesh.vertexCount} vertices · " +
                    $"{result.Mesh.triangles.Length / 3} triangles · " +
                    $"{errors} errors · {warnings} warnings" +
                    BuildSourceStateSuffix() + ".";
        }

        private string BuildSourceStateSuffix()
        {
            string suffix = EditorUtility.IsDirty(controller.Source)
                ? " · source dirty"
                : string.Empty;
            if (!string.IsNullOrEmpty(lastBakedPath))
            {
                suffix += " · baked";
            }

            return suffix;
        }

        private void AddPanel(PanelShape shape)
        {
            if (controller?.Source == null)
            {
                return;
            }

            PanelDefinition panel = controller.AddPanel(
                shape,
                EditorApplication.timeSinceStartup);
            SelectPanel(panel.Id);
        }

        private void SelectPanel(string panelId)
        {
            selectedPanelId = panelId;
            activeTab = WorkspaceTab.Panels;
            canvasView.SetSelection(selectedPanelId);
            SwitchTab(WorkspaceTab.Panels);
        }

        private void SelectSeam(string seamId)
        {
            selectedSeamId = seamId;
            SeamDefinition seam = controller.FindSeam(seamId);
            canvasView.SetBoundaryHighlights(seam);
            previewRenderer.SetHighlightedSeam(seam);
            previewContainer.MarkDirtyRepaint();
            SwitchTab(WorkspaceTab.Seams);
        }

        private void SelectOperation(int index)
        {
            selectedOperationIndex = index;
            SwitchTab(WorkspaceTab.Operations);
        }

        private void ConfigureSplitter()
        {
            float ratio = Mathf.Clamp(
                EditorPrefs.GetFloat(SplitRatioPreference, 0.5f),
                0.25f,
                0.75f);
            mainRow.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (mainRow.resolvedStyle.width > 0f &&
                    float.IsNaN(canvasPane.resolvedStyle.width))
                {
                    canvasPane.style.width =
                        mainRow.resolvedStyle.width * ratio;
                }
            });
            canvasPane.style.flexGrow = 0f;
            canvasPane.style.width = Length.Percent(ratio * 100f);

            int pointerId = -1;
            float startWidth = 0f;
            Vector2 startPosition = default;
            mainSplitter.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                pointerId = evt.pointerId;
                startWidth = canvasPane.resolvedStyle.width;
                startPosition = evt.position;
                mainSplitter.CapturePointer(pointerId);
                evt.StopPropagation();
            });
            mainSplitter.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (pointerId < 0 ||
                    evt.pointerId != pointerId ||
                    !mainSplitter.HasPointerCapture(pointerId))
                {
                    return;
                }

                float total = Mathf.Max(1f, mainRow.resolvedStyle.width);
                float next = Mathf.Clamp(
                    startWidth + ((Vector2)evt.position - startPosition).x,
                    260f,
                    total - 260f);
                canvasPane.style.width = next;
                EditorPrefs.SetFloat(
                    SplitRatioPreference,
                    Mathf.Clamp01(next / total));
                evt.StopPropagation();
            });
            mainSplitter.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (pointerId < 0 || evt.pointerId != pointerId)
                {
                    return;
                }

                if (mainSplitter.HasPointerCapture(pointerId))
                {
                    mainSplitter.ReleasePointer(pointerId);
                }

                pointerId = -1;
                evt.StopPropagation();
            });
        }

        private enum WorkspaceTab
        {
            Panels,
            Operations,
            Seams,
            Diagnostics,
            Bake
        }
    }
}
