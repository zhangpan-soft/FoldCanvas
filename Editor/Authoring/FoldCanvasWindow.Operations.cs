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
        private FoldOperationType operationTypeToAdd = FoldOperationType.Roll;

        private void BuildOperationsTab(VisualElement root)
        {
            FoldCanvasAsset source = controller?.Source;
            if (source == null)
            {
                root.Add(new HelpBox(
                    "Select a FoldCanvas source before authoring operations.",
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
            EnumField typeField = new EnumField(operationTypeToAdd);
            typeField.style.flexGrow = 1f;
            typeField.RegisterValueChangedCallback(evt =>
                operationTypeToAdd = (FoldOperationType)evt.newValue);
            addRow.Add(typeField);
            Button addButton = new Button(AddSelectedOperation)
            {
                text = "+ Operation",
                name = "add-operation-button"
            };
            addRow.Add(addButton);
            listColumn.Add(addRow);

            ScrollView list = new ScrollView();
            list.style.flexGrow = 1f;
            list.name = "operation-list";
            for (int i = 0; i < source.Operations.Count; i++)
            {
                int index = i;
                FoldOperationDefinition operation = source.Operations[i];
                string label = operation == null
                    ? $"{i + 1:00}  <null>"
                    : $"{i + 1:00}  {operation.Id}  ·  {operation.Type}" +
                        (operation.Enabled ? string.Empty : "  (disabled)");
                Button item = new Button(() => SelectOperation(index))
                {
                    text = label,
                    name = "operation-list-item-" + i
                };
                item.AddToClassList("list-item-button");
                if (i == selectedOperationIndex)
                {
                    item.AddToClassList("selected-list-item");
                }

                list.Add(item);
            }

            listColumn.Add(list);

            VisualElement inspector = new VisualElement();
            inspector.AddToClassList("source-inspector");
            row.Add(inspector);
            FoldOperationDefinition selected =
                selectedOperationIndex >= 0 &&
                selectedOperationIndex < source.Operations.Count
                    ? source.Operations[selectedOperationIndex]
                    : null;
            BuildOperationInspector(inspector, selected);
        }

        private void AddSelectedOperation()
        {
            FoldOperationDefinition operation = controller.AddOperation(
                operationTypeToAdd,
                selectedPanelId,
                selectedSeamId,
                EditorApplication.timeSinceStartup);
            selectedOperationIndex =
                controller.Source.Operations.IndexOf(operation);
            SwitchTab(WorkspaceTab.Operations);
        }

        private void BuildOperationInspector(
            VisualElement root,
            FoldOperationDefinition operation)
        {
            if (operation == null)
            {
                root.Add(new HelpBox(
                    "Add or select an ordered FoldCanvas operation.",
                    HelpBoxMessageType.Info));
                return;
            }

            Label title = new Label(
                $"Operation {selectedOperationIndex + 1:00} · {operation.Type}");
            title.AddToClassList("section-title");
            root.Add(title);

            TextField idField = new TextField("Stable Id")
            {
                value = operation.Id,
                isDelayed = true,
                name = "operation-id-field"
            };
            idField.RegisterValueChangedCallback(evt =>
            {
                try
                {
                    controller.RenameOperation(
                        selectedOperationIndex,
                        evt.newValue,
                        EditorApplication.timeSinceStartup);
                }
                catch (ArgumentException exception)
                {
                    ShowNotification(new GUIContent(exception.Message));
                    idField.SetValueWithoutNotify(operation.Id);
                }
            });
            root.Add(idField);

            Toggle enabled = new Toggle("Enabled")
            {
                value = operation.Enabled
            };
            enabled.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Enable FoldCanvas Operation",
                    changed => changed.Enabled = evt.newValue));
            root.Add(enabled);

            VisualElement orderRow = new VisualElement();
            orderRow.AddToClassList("inline-row");
            Button up = new Button(() => MoveSelectedOperation(-1))
            {
                text = "Move Up"
            };
            up.SetEnabled(selectedOperationIndex > 0);
            Button down = new Button(() => MoveSelectedOperation(1))
            {
                text = "Move Down"
            };
            down.SetEnabled(
                selectedOperationIndex + 1 <
                controller.Source.Operations.Count);
            Button remove = new Button(RemoveSelectedOperation)
            {
                text = "Remove"
            };
            remove.AddToClassList("danger-button");
            orderRow.Add(up);
            orderRow.Add(down);
            orderRow.Add(remove);
            root.Add(orderRow);

            switch (operation)
            {
                case RigidTransformOperationDefinition rigid:
                    BuildRigidTransformFields(root, rigid);
                    break;
                case FoldLineOperationDefinition fold:
                    BuildFoldFields(root, fold);
                    break;
                case RollOperationDefinition roll:
                    BuildRollFields(root, roll);
                    break;
                case StitchOperationDefinition stitch:
                    BuildStitchFields(root, stitch);
                    break;
                case SolidifyOperationDefinition solidify:
                    BuildSolidifyFields(root, solidify);
                    break;
                case SphericalWrapOperationDefinition sphericalWrap:
                    BuildSphericalWrapFields(root, sphericalWrap);
                    break;
                default:
                    root.Add(new HelpBox(
                        "This operation type has no M06 form.",
                        HelpBoxMessageType.Warning));
                    break;
            }
        }

        private void BuildRigidTransformFields(
            VisualElement root,
            RigidTransformOperationDefinition rigid)
        {
            root.Add(CreatePanelPopup(rigid.PanelId, panelId =>
                MutateSelectedOperation(
                    "Target Rigid Transform Panel",
                    changed =>
                        ((RigidTransformOperationDefinition)changed).PanelId =
                            panelId)));

            Vector3Field translation = new Vector3Field("Translation")
            {
                value = rigid.Translation
            };
            translation.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Rigid Translation",
                    changed =>
                        ((RigidTransformOperationDefinition)changed)
                            .Translation = evt.newValue));
            root.Add(translation);

            Vector3Field rotation = new Vector3Field("Rotation Euler")
            {
                value = rigid.RotationEuler
            };
            rotation.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Rigid Rotation",
                    changed =>
                        ((RigidTransformOperationDefinition)changed)
                            .RotationEuler = evt.newValue));
            root.Add(rotation);

            Vector3Field scale = new Vector3Field("Scale")
            {
                value = rigid.Scale
            };
            scale.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Rigid Scale",
                    changed =>
                        ((RigidTransformOperationDefinition)changed).Scale =
                            evt.newValue));
            root.Add(scale);
        }

        private void BuildFoldFields(
            VisualElement root,
            FoldLineOperationDefinition fold)
        {
            root.Add(CreatePanelPopup(fold.PanelId, panelId =>
                MutateSelectedOperation(
                    "Target Fold Panel",
                    changed =>
                        ((FoldLineOperationDefinition)changed).PanelId =
                            panelId)));

            Vector2Field lineStart = new Vector2Field("Line Start")
            {
                value = fold.LineStart
            };
            lineStart.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Fold Line Start",
                    changed =>
                        ((FoldLineOperationDefinition)changed).LineStart =
                            evt.newValue));
            root.Add(lineStart);

            Vector2Field lineEnd = new Vector2Field("Line End")
            {
                value = fold.LineEnd
            };
            lineEnd.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Fold Line End",
                    changed =>
                        ((FoldLineOperationDefinition)changed).LineEnd =
                            evt.newValue));
            root.Add(lineEnd);

            EnumField side = new EnumField("Side", fold.Side);
            side.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Fold Side",
                    changed =>
                        ((FoldLineOperationDefinition)changed).Side =
                            (FoldSide)evt.newValue));
            root.Add(side);

            FloatField angle = new FloatField("Angle Degrees")
            {
                value = fold.AngleDegrees
            };
            angle.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Fold Angle",
                    changed =>
                        ((FoldLineOperationDefinition)changed).AngleDegrees =
                            evt.newValue));
            root.Add(angle);

            FloatField falloff = new FloatField("Falloff")
            {
                value = fold.Falloff
            };
            falloff.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Fold Falloff",
                    changed =>
                        ((FoldLineOperationDefinition)changed).Falloff =
                            evt.newValue));
            root.Add(falloff);
        }

        private void BuildRollFields(
            VisualElement root,
            RollOperationDefinition roll)
        {
            root.Add(CreatePanelPopup(roll.PanelId, panelId =>
                MutateSelectedOperation(
                    "Target Roll Panel",
                    changed =>
                        ((RollOperationDefinition)changed).PanelId = panelId)));

            EnumField direction = new EnumField("Direction", roll.Direction);
            direction.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Roll Direction",
                    changed =>
                        ((RollOperationDefinition)changed).Direction =
                            (RollDirection)evt.newValue));
            root.Add(direction);

            FloatField angle = new FloatField("Angle Degrees")
            {
                value = roll.AngleDegrees
            };
            angle.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Roll Angle",
                    changed =>
                        ((RollOperationDefinition)changed).AngleDegrees =
                            evt.newValue));
            root.Add(angle);

            EnumField radiusMode = new EnumField(
                "Radius Mode",
                roll.RadiusMode);
            radiusMode.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Roll Radius Mode",
                    changed =>
                        ((RollOperationDefinition)changed).RadiusMode =
                            (RollRadiusMode)evt.newValue));
            root.Add(radiusMode);

            FloatField explicitRadius = new FloatField("Explicit Radius")
            {
                value = roll.ExplicitRadius
            };
            explicitRadius.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Roll Radius",
                    changed =>
                        ((RollOperationDefinition)changed).ExplicitRadius =
                            evt.newValue));
            root.Add(explicitRadius);

            FloatField startAngle = new FloatField("Start Angle Degrees")
            {
                value = roll.StartAngleDegrees
            };
            startAngle.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Roll Start Angle",
                    changed =>
                        ((RollOperationDefinition)changed).StartAngleDegrees =
                            evt.newValue));
            root.Add(startAngle);

            root.Add(CreateSeamPopup(
                "Target Seam",
                roll.TargetSeamId,
                true,
                seamId => MutateSelectedOperation(
                    "Edit Roll Target Seam",
                    changed =>
                        ((RollOperationDefinition)changed).TargetSeamId =
                            seamId)));
        }

        private void BuildStitchFields(
            VisualElement root,
            StitchOperationDefinition stitch)
        {
            Label title = new Label("Selected Seams");
            title.AddToClassList("section-title");
            root.Add(title);
            if (controller.Source.Seams.Count == 0)
            {
                root.Add(new HelpBox(
                    "Create a Seam before adding it to Stitch.",
                    HelpBoxMessageType.Warning));
                return;
            }

            for (int i = 0; i < controller.Source.Seams.Count; i++)
            {
                SeamDefinition seam = controller.Source.Seams[i];
                string seamId = seam.Id;
                Toggle selected = new Toggle(
                    $"{seam.Id} · {seam.A.PanelId}.{seam.A.BoundaryId} ↔ " +
                    $"{seam.B.PanelId}.{seam.B.BoundaryId}")
                {
                    value = stitch.SeamIds.Contains(seamId)
                };
                selected.RegisterValueChangedCallback(evt =>
                    MutateSelectedOperation(
                        "Edit Stitch Seam Selection",
                        changed =>
                        {
                            List<string> ids =
                                ((StitchOperationDefinition)changed).SeamIds;
                            if (evt.newValue && !ids.Contains(seamId))
                            {
                                ids.Add(seamId);
                            }
                            else if (!evt.newValue)
                            {
                                ids.Remove(seamId);
                            }
                        }));
                root.Add(selected);
            }

            root.Add(new HelpBox(
                "Stitch is terminal for every selected panel in the current " +
                "geometry contract.",
                HelpBoxMessageType.Info));
        }

        private void BuildSolidifyFields(
            VisualElement root,
            SolidifyOperationDefinition solidify)
        {
            FloatField thickness = new FloatField("Thickness (m)")
            {
                value = solidify.Thickness
            };
            thickness.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Solidify Thickness",
                    changed =>
                        ((SolidifyOperationDefinition)changed).Thickness =
                            evt.newValue));
            root.Add(thickness);

            EnumField direction = new EnumField(
                "Direction",
                solidify.Direction);
            direction.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Solidify Direction",
                    changed =>
                        ((SolidifyOperationDefinition)changed).Direction =
                            (SolidifyDirection)evt.newValue));
            root.Add(direction);

            Label panelsTitle = new Label("Selected Panels");
            panelsTitle.AddToClassList("section-title");
            root.Add(panelsTitle);
            for (int i = 0; i < controller.Source.Panels.Count; i++)
            {
                PanelDefinition panel = controller.Source.Panels[i];
                string panelId = panel.Id;
                Toggle selected = new Toggle(panelId)
                {
                    value = solidify.PanelIds.Contains(panelId)
                };
                selected.RegisterValueChangedCallback(evt =>
                    MutateSelectedOperation(
                        "Edit Solidify Panel Selection",
                        changed =>
                        {
                            List<string> ids =
                                ((SolidifyOperationDefinition)changed).PanelIds;
                            if (evt.newValue && !ids.Contains(panelId))
                            {
                                ids.Add(panelId);
                            }
                            else if (!evt.newValue)
                            {
                                ids.Remove(panelId);
                            }
                        }));
                root.Add(selected);
            }
        }

        private void BuildSphericalWrapFields(
            VisualElement root,
            SphericalWrapOperationDefinition sphericalWrap)
        {
            root.Add(CreatePanelPopup(sphericalWrap.PanelId, panelId =>
                MutateSelectedOperation(
                    "Target Spherical Wrap Panel",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed).PanelId =
                            panelId)));

            FloatField radius = new FloatField("Radius")
            {
                value = sphericalWrap.Radius
            };
            radius.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Spherical Radius",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed).Radius =
                            evt.newValue));
            root.Add(radius);

            Vector2Field latitude = new Vector2Field("Latitude Range")
            {
                value = sphericalWrap.LatitudeRange
            };
            latitude.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Spherical Latitude",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed)
                            .LatitudeRange = evt.newValue));
            root.Add(latitude);

            Vector2Field longitude = new Vector2Field("Longitude Range")
            {
                value = sphericalWrap.LongitudeRange
            };
            longitude.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Spherical Longitude",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed)
                            .LongitudeRange = evt.newValue));
            root.Add(longitude);

            EnumField wrapDirection = new EnumField(
                "Wrap Direction",
                sphericalWrap.WrapDirection);
            wrapDirection.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Spherical Direction",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed)
                            .WrapDirection =
                                (SphericalWrapDirection)evt.newValue));
            root.Add(wrapDirection);

            EnumField poleMode = new EnumField(
                "Pole Mode",
                sphericalWrap.PoleMode);
            poleMode.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Spherical Pole Mode",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed).PoleMode =
                            (SphericalPoleMode)evt.newValue));
            root.Add(poleMode);

            EnumField subdivisionMode = new EnumField(
                "Subdivision Mode",
                sphericalWrap.SubdivisionMode);
            subdivisionMode.RegisterValueChangedCallback(evt =>
                MutateSelectedOperation(
                    "Edit Spherical Subdivision",
                    changed =>
                        ((SphericalWrapOperationDefinition)changed)
                            .SubdivisionMode =
                                (SphericalSubdivisionMode)evt.newValue));
            root.Add(subdivisionMode);
        }

        private VisualElement CreatePanelPopup(
            string currentPanelId,
            Action<string> changed)
        {
            List<string> choices = GetPanelIds();
            if (choices.Count == 0)
            {
                TextField empty = new TextField("Panel")
                {
                    value = "<no panels>"
                };
                empty.SetEnabled(false);
                return empty;
            }

            int index = choices.IndexOf(currentPanelId);
            PopupField<string> popup = new PopupField<string>(
                "Panel",
                choices,
                index >= 0 ? index : 0);
            popup.RegisterValueChangedCallback(evt => changed(evt.newValue));
            return popup;
        }

        private VisualElement CreateSeamPopup(
            string label,
            string currentSeamId,
            bool allowNone,
            Action<string> changed)
        {
            List<string> choices = GetSeamIds();
            const string none = "<none>";
            if (allowNone)
            {
                choices.Insert(0, none);
            }

            if (choices.Count == 0)
            {
                TextField empty = new TextField(label)
                {
                    value = "<no seams>"
                };
                empty.SetEnabled(false);
                return empty;
            }

            int index = string.IsNullOrEmpty(currentSeamId) && allowNone
                ? 0
                : choices.IndexOf(currentSeamId);
            PopupField<string> popup = new PopupField<string>(
                label,
                choices,
                index >= 0 ? index : 0);
            popup.RegisterValueChangedCallback(evt =>
                changed(evt.newValue == none ? null : evt.newValue));
            return popup;
        }

        private List<string> GetPanelIds()
        {
            List<string> values = new List<string>();
            if (controller?.Source == null)
            {
                return values;
            }

            for (int i = 0; i < controller.Source.Panels.Count; i++)
            {
                string id = controller.Source.Panels[i]?.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    values.Add(id);
                }
            }

            return values;
        }

        private List<string> GetSeamIds()
        {
            List<string> values = new List<string>();
            if (controller?.Source == null)
            {
                return values;
            }

            for (int i = 0; i < controller.Source.Seams.Count; i++)
            {
                string id = controller.Source.Seams[i]?.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    values.Add(id);
                }
            }

            return values;
        }

        private void MutateSelectedOperation(
            string undoName,
            Action<FoldOperationDefinition> mutation)
        {
            controller.MutateOperation(
                selectedOperationIndex,
                undoName,
                mutation,
                EditorApplication.timeSinceStartup);
        }

        private void MoveSelectedOperation(int delta)
        {
            int destination = selectedOperationIndex + delta;
            if (controller.MoveOperation(
                    selectedOperationIndex,
                    destination,
                    EditorApplication.timeSinceStartup))
            {
                selectedOperationIndex = destination;
            }
        }

        private void RemoveSelectedOperation()
        {
            controller.RemoveOperation(
                selectedOperationIndex,
                EditorApplication.timeSinceStartup);
            selectedOperationIndex = Mathf.Min(
                selectedOperationIndex,
                controller.Source.Operations.Count - 1);
        }
    }
}
