using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoldCanvas.Editor
{
    public sealed partial class FoldCanvasWindow
    {
        private void BuildPanelsTab(VisualElement root)
        {
            FoldCanvasAsset source = controller?.Source;
            if (source == null)
            {
                root.Add(new HelpBox(
                    "Create or select a FoldCanvas source before adding panels.",
                    HelpBoxMessageType.Info));
                return;
            }

            VisualElement row = new VisualElement();
            row.AddToClassList("authoring-row");
            root.Add(row);

            VisualElement listColumn = new VisualElement();
            listColumn.AddToClassList("source-list");
            row.Add(listColumn);

            VisualElement addRow = new VisualElement();
            addRow.AddToClassList("inline-row");
            Button addRectangle = new Button(
                () => AddPanel(PanelShape.Rectangle))
            {
                text = "+ Rectangle",
                name = "panel-add-rectangle"
            };
            Button addDisk = new Button(() => AddPanel(PanelShape.Disk))
            {
                text = "+ Disk",
                name = "panel-add-disk"
            };
            addRow.Add(addRectangle);
            addRow.Add(addDisk);
            listColumn.Add(addRow);

            ScrollView panelList = new ScrollView();
            panelList.style.flexGrow = 1f;
            panelList.name = "panel-list";
            for (int i = 0; i < source.Panels.Count; i++)
            {
                PanelDefinition panel = source.Panels[i];
                if (panel == null)
                {
                    continue;
                }

                string panelId = panel.Id;
                Button item = new Button(() => SelectPanel(panelId))
                {
                    text = $"{i + 1:00}  {panel.Id}  ·  {panel.Shape}",
                    name = "panel-list-item-" + i
                };
                item.AddToClassList("list-item-button");
                if (string.Equals(
                        panel.Id,
                        selectedPanelId,
                        StringComparison.Ordinal))
                {
                    item.AddToClassList("selected-list-item");
                }

                panelList.Add(item);
            }

            listColumn.Add(panelList);

            VisualElement inspector = new VisualElement();
            inspector.AddToClassList("source-inspector");
            row.Add(inspector);
            BuildPanelInspector(inspector, controller.FindPanel(selectedPanelId));
        }

        private void BuildPanelInspector(
            VisualElement root,
            PanelDefinition panel)
        {
            ObjectField appearanceField = new ObjectField("Appearance Canvas")
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false,
                value = controller.Source.Appearance,
                name = "appearance-field"
            };
            appearanceField.RegisterValueChangedCallback(evt =>
                controller.SetAppearance(
                    evt.newValue as Texture2D,
                    EditorApplication.timeSinceStartup));
            root.Add(appearanceField);

            if (panel == null)
            {
                root.Add(new HelpBox(
                    "Add or select a panel to edit its authoritative 2D domain.",
                    HelpBoxMessageType.Info));
                return;
            }

            Label title = new Label($"Panel · {panel.Id}");
            title.AddToClassList("section-title");
            root.Add(title);

            TextField idField = new TextField("Stable Id")
            {
                value = panel.Id,
                isDelayed = true,
                name = "panel-id-field"
            };
            string originalId = panel.Id;
            idField.RegisterValueChangedCallback(evt =>
            {
                try
                {
                    if (controller.RenamePanel(
                            originalId,
                            evt.newValue,
                            EditorApplication.timeSinceStartup))
                    {
                        selectedPanelId = evt.newValue.Trim();
                    }
                }
                catch (ArgumentException exception)
                {
                    ShowNotification(new GUIContent(exception.Message));
                    idField.SetValueWithoutNotify(originalId);
                }
            });
            root.Add(idField);

            TextField shapeField = new TextField("Shape")
            {
                value = panel.Shape.ToString()
            };
            shapeField.SetEnabled(false);
            root.Add(shapeField);

            RectField canvasRectField = new RectField("Canvas Rect (UV)")
            {
                value = panel.CanvasRect,
                name = "panel-canvas-rect-field"
            };
            canvasRectField.RegisterValueChangedCallback(evt =>
                controller.MutatePanel(
                    selectedPanelId,
                    "Edit Panel Canvas Rect",
                    changed => changed.CanvasRect = evt.newValue,
                    EditorApplication.timeSinceStartup));
            root.Add(canvasRectField);

            Vector2Field sizeField = new Vector2Field("Physical Size (m)")
            {
                value = panel.PhysicalSize,
                name = "panel-size-field"
            };
            sizeField.RegisterValueChangedCallback(evt =>
                controller.MutatePanel(
                    selectedPanelId,
                    "Edit Panel Physical Size",
                    changed => changed.PhysicalSize = evt.newValue,
                    EditorApplication.timeSinceStartup));
            root.Add(sizeField);

            if (panel.Shape == PanelShape.Rectangle)
            {
                IntegerField uSegments = new IntegerField("U Segments")
                {
                    value = panel.USegments
                };
                uSegments.RegisterValueChangedCallback(evt =>
                    controller.MutatePanel(
                        selectedPanelId,
                        "Edit Panel U Segments",
                        changed => changed.USegments = evt.newValue,
                        EditorApplication.timeSinceStartup));
                root.Add(uSegments);

                IntegerField vSegments = new IntegerField("V Segments")
                {
                    value = panel.VSegments
                };
                vSegments.RegisterValueChangedCallback(evt =>
                    controller.MutatePanel(
                        selectedPanelId,
                        "Edit Panel V Segments",
                        changed => changed.VSegments = evt.newValue,
                        EditorApplication.timeSinceStartup));
                root.Add(vSegments);
            }
            else
            {
                IntegerField radialSegments =
                    new IntegerField("Radial Segments")
                    {
                        value = panel.RadialSegments
                    };
                radialSegments.RegisterValueChangedCallback(evt =>
                    controller.MutatePanel(
                        selectedPanelId,
                        "Edit Panel Radial Segments",
                        changed => changed.RadialSegments = evt.newValue,
                        EditorApplication.timeSinceStartup));
                root.Add(radialSegments);

                IntegerField radialRings = new IntegerField("Radial Rings")
                {
                    value = panel.RadialRings
                };
                radialRings.RegisterValueChangedCallback(evt =>
                    controller.MutatePanel(
                        selectedPanelId,
                        "Edit Panel Radial Rings",
                        changed => changed.RadialRings = evt.newValue,
                        EditorApplication.timeSinceStartup));
                root.Add(radialRings);
            }

            Label boundaryTitle = new Label("Named Boundaries");
            boundaryTitle.AddToClassList("section-title");
            root.Add(boundaryTitle);

            VisualElement boundaryRow = new VisualElement();
            boundaryRow.AddToClassList("inline-row");
            IReadOnlyList<string> boundaryIds =
                FoldCanvasAuthoringController.GetBoundaryIds(panel);
            for (int i = 0; i < boundaryIds.Count; i++)
            {
                string boundaryId = boundaryIds[i];
                Button boundaryButton = new Button(() =>
                {
                    selectedBoundaryId = boundaryId;
                    canvasView.SetSingleBoundaryHighlight(
                        selectedPanelId,
                        selectedBoundaryId);
                })
                {
                    text = boundaryId,
                    name = "boundary-" + boundaryId
                };
                boundaryRow.Add(boundaryButton);
            }

            root.Add(boundaryRow);
            root.Add(new HelpBox(
                "Drag the four orange handles in the 2D view to edit the " +
                "CanvasRect. Alt-drag or middle-drag pans; the wheel zooms. " +
                "Boundary order follows the documented FoldCanvas contract.",
                HelpBoxMessageType.Info));

            Button remove = new Button(() =>
            {
                string removedId = panel.Id;
                controller.RemovePanel(
                    removedId,
                    EditorApplication.timeSinceStartup);
                selectedPanelId = null;
            })
            {
                text = "Remove Panel",
                name = "remove-panel-button"
            };
            remove.AddToClassList("danger-button");
            remove.style.marginTop = 6f;
            root.Add(remove);
        }
    }
}
