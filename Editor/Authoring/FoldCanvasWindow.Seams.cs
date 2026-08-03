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
        private void BuildSeamsTab(VisualElement root)
        {
            FoldCanvasAsset source = controller?.Source;
            if (source == null)
            {
                root.Add(new HelpBox(
                    "Select a FoldCanvas source before pairing boundaries.",
                    HelpBoxMessageType.Info));
                return;
            }

            VisualElement row = new VisualElement();
            row.AddToClassList("authoring-row");
            root.Add(row);

            VisualElement listColumn = new VisualElement();
            listColumn.AddToClassList("source-list");
            row.Add(listColumn);

            Button add = new Button(AddDefaultSeam)
            {
                text = "+ Seam",
                name = "add-seam-button"
            };
            add.SetEnabled(source.Panels.Count > 0);
            listColumn.Add(add);

            ScrollView list = new ScrollView();
            list.style.flexGrow = 1f;
            list.name = "seam-list";
            for (int i = 0; i < source.Seams.Count; i++)
            {
                SeamDefinition seam = source.Seams[i];
                if (seam == null)
                {
                    continue;
                }

                string seamId = seam.Id;
                Button item = new Button(() => SelectSeam(seamId))
                {
                    text = $"{i + 1:00}  {seam.Id}  ·  {seam.Mode}",
                    name = "seam-list-item-" + i
                };
                item.tooltip =
                    $"{seam.A.PanelId}.{seam.A.BoundaryId} ↔ " +
                    $"{seam.B.PanelId}.{seam.B.BoundaryId}";
                item.AddToClassList("list-item-button");
                if (string.Equals(
                        seam.Id,
                        selectedSeamId,
                        StringComparison.Ordinal))
                {
                    item.AddToClassList("selected-list-item");
                }

                list.Add(item);
            }

            listColumn.Add(list);

            VisualElement inspector = new VisualElement();
            inspector.AddToClassList("source-inspector");
            row.Add(inspector);
            BuildSeamInspector(inspector, controller.FindSeam(selectedSeamId));
        }

        private void AddDefaultSeam()
        {
            if (controller.Source.Panels.Count == 0)
            {
                return;
            }

            PanelDefinition panelA =
                controller.FindPanel(selectedPanelId) ??
                controller.Source.Panels[0];
            PanelDefinition panelB = controller.Source.Panels.Count > 1
                ? controller.Source.Panels[1]
                : panelA;
            IReadOnlyList<string> boundariesA =
                FoldCanvasAuthoringController.GetBoundaryIds(panelA);
            IReadOnlyList<string> boundariesB =
                FoldCanvasAuthoringController.GetBoundaryIds(panelB);
            string boundaryA = boundariesA.ContainsValue(selectedBoundaryId)
                ? selectedBoundaryId
                : boundariesA[0];
            string boundaryB = panelA == panelB && boundariesB.Count > 1
                ? boundariesB[1]
                : boundariesB[0];
            SeamDefinition seam = controller.AddSeam(
                panelA.Id,
                boundaryA,
                panelB.Id,
                boundaryB,
                SeamMode.Weld,
                EditorApplication.timeSinceStartup);
            selectedSeamId = seam.Id;
            SelectSeam(seam.Id);
        }

        private void BuildSeamInspector(
            VisualElement root,
            SeamDefinition seam)
        {
            if (seam == null)
            {
                root.Add(new HelpBox(
                    "Create a Seam, then choose two ordered named boundaries.",
                    HelpBoxMessageType.Info));
                return;
            }

            Label title = new Label($"Seam · {seam.Id}");
            title.AddToClassList("section-title");
            root.Add(title);

            string originalId = seam.Id;
            TextField idField = new TextField("Stable Id")
            {
                value = seam.Id,
                isDelayed = true,
                name = "seam-id-field"
            };
            idField.RegisterValueChangedCallback(evt =>
            {
                try
                {
                    if (controller.RenameSeam(
                            originalId,
                            evt.newValue,
                            EditorApplication.timeSinceStartup))
                    {
                        selectedSeamId = evt.newValue.Trim();
                    }
                }
                catch (ArgumentException exception)
                {
                    ShowNotification(new GUIContent(exception.Message));
                    idField.SetValueWithoutNotify(originalId);
                }
            });
            root.Add(idField);

            Label aTitle = new Label("Endpoint A");
            aTitle.AddToClassList("section-title");
            root.Add(aTitle);
            root.Add(CreateEndpointPanelPopup(
                "Panel A",
                seam.A,
                true));
            root.Add(CreateBoundaryPopup(
                "Boundary A",
                seam.A,
                true));
            AddBoundarySpanFields(root, seam.A, true);

            Label bTitle = new Label("Endpoint B");
            bTitle.AddToClassList("section-title");
            root.Add(bTitle);
            root.Add(CreateEndpointPanelPopup(
                "Panel B",
                seam.B,
                false));
            root.Add(CreateBoundaryPopup(
                "Boundary B",
                seam.B,
                false));
            AddBoundarySpanFields(root, seam.B, false);

            EnumField mode = new EnumField("Mode", seam.Mode);
            mode.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Edit Seam Mode",
                    changed => changed.Mode = (SeamMode)evt.newValue));
            root.Add(mode);

            Toggle reverse = new Toggle("Reverse B")
            {
                value = seam.ReverseB
            };
            reverse.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Edit Seam Direction",
                    changed => changed.ReverseB = evt.newValue));
            root.Add(reverse);

            IntegerField samples = new IntegerField("Sample Count (0 = auto)")
            {
                value = seam.SampleCount
            };
            samples.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Edit Seam Sample Count",
                    changed => changed.SampleCount = evt.newValue));
            root.Add(samples);

            root.Add(new HelpBox(
                "Cyan is endpoint A and pink is endpoint B in both the 2D " +
                "canvas and the optional 3D Seam overlay. A Seam is source " +
                "topology; it changes geometry only when selected by Stitch.",
                HelpBoxMessageType.Info));

            Button remove = new Button(() =>
            {
                controller.RemoveSeam(
                    seam.Id,
                    EditorApplication.timeSinceStartup);
                selectedSeamId = null;
            })
            {
                text = "Remove Seam",
                name = "remove-seam-button"
            };
            remove.AddToClassList("danger-button");
            remove.style.marginTop = 6f;
            root.Add(remove);
        }

        private VisualElement CreateEndpointPanelPopup(
            string label,
            BoundaryReference endpoint,
            bool endpointA)
        {
            List<string> panels = GetPanelIds();
            if (panels.Count == 0)
            {
                TextField empty = new TextField(label)
                {
                    value = "<no panels>"
                };
                empty.SetEnabled(false);
                return empty;
            }

            int index = panels.IndexOf(endpoint.PanelId);
            PopupField<string> popup = new PopupField<string>(
                label,
                panels,
                index >= 0 ? index : 0);
            popup.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Edit Seam Panel Reference",
                    changed =>
                    {
                        PanelDefinition panel =
                            controller.FindPanel(evt.newValue);
                        IReadOnlyList<string> boundaries =
                            FoldCanvasAuthoringController.GetBoundaryIds(panel);
                        BoundaryReference replacement = ReplaceTarget(
                            endpoint,
                            evt.newValue,
                            boundaries.Count > 0 ? boundaries[0] : null);
                        if (endpointA)
                        {
                            changed.A = replacement;
                        }
                        else
                        {
                            changed.B = replacement;
                        }
                    }));
            return popup;
        }

        private VisualElement CreateBoundaryPopup(
            string label,
            BoundaryReference endpoint,
            bool endpointA)
        {
            PanelDefinition panel = controller.FindPanel(endpoint.PanelId);
            IReadOnlyList<string> boundaryIds =
                FoldCanvasAuthoringController.GetBoundaryIds(panel);
            List<string> choices = new List<string>(boundaryIds.Count);
            for (int i = 0; i < boundaryIds.Count; i++)
            {
                choices.Add(boundaryIds[i]);
            }

            if (choices.Count == 0)
            {
                TextField empty = new TextField(label)
                {
                    value = "<no boundaries>"
                };
                empty.SetEnabled(false);
                return empty;
            }

            int index = choices.IndexOf(endpoint.BoundaryId);
            PopupField<string> popup = new PopupField<string>(
                label,
                choices,
                index >= 0 ? index : 0);
            popup.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Edit Seam Boundary Reference",
                    changed =>
                    {
                        if (endpointA)
                        {
                            changed.A = ReplaceTarget(
                                changed.A,
                                changed.A.PanelId,
                                evt.newValue);
                        }
                        else
                        {
                            changed.B = ReplaceTarget(
                                changed.B,
                                changed.B.PanelId,
                                evt.newValue);
                        }
                    }));
            return popup;
        }

        private void AddBoundarySpanFields(
            VisualElement root,
            BoundaryReference endpoint,
            bool endpointA)
        {
            Toggle useSpan = new Toggle("Use Boundary Span")
            {
                value = endpoint.UseSpan
            };
            useSpan.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Toggle Seam Boundary Span",
                    changed =>
                    {
                        BoundaryReference current = endpointA
                            ? changed.A
                            : changed.B;
                        BoundaryReference replacement = evt.newValue
                            ? new BoundaryReference(
                                current.PanelId,
                                current.BoundaryId,
                                current.Span)
                            : new BoundaryReference(
                                current.PanelId,
                                current.BoundaryId);
                        if (endpointA)
                        {
                            changed.A = replacement;
                        }
                        else
                        {
                            changed.B = replacement;
                        }
                    }));
            root.Add(useSpan);

            Vector2Field span = new Vector2Field("Normalized Span")
            {
                value = endpoint.Span
            };
            span.SetEnabled(endpoint.UseSpan);
            span.RegisterValueChangedCallback(evt =>
                MutateSelectedSeam(
                    "Edit Seam Boundary Span",
                    changed =>
                    {
                        BoundaryReference current = endpointA
                            ? changed.A
                            : changed.B;
                        BoundaryReference replacement = new BoundaryReference(
                            current.PanelId,
                            current.BoundaryId,
                            evt.newValue);
                        if (endpointA)
                        {
                            changed.A = replacement;
                        }
                        else
                        {
                            changed.B = replacement;
                        }
                    }));
            root.Add(span);
        }

        private static BoundaryReference ReplaceTarget(
            BoundaryReference source,
            string panelId,
            string boundaryId)
        {
            return source.UseSpan
                ? new BoundaryReference(panelId, boundaryId, source.Span)
                : new BoundaryReference(panelId, boundaryId);
        }

        private void MutateSelectedSeam(
            string undoName,
            Action<SeamDefinition> mutation)
        {
            controller.MutateSeam(
                selectedSeamId,
                undoName,
                mutation,
                EditorApplication.timeSinceStartup);
        }
    }

    internal static class FoldCanvasBoundaryListExtensions
    {
        public static bool ContainsValue(
            this IReadOnlyList<string> values,
            string expected)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(
                        values[i],
                        expected,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
