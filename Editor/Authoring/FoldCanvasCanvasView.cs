using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoldCanvas.Editor
{
    internal sealed class FoldCanvasCanvasView : VisualElement
    {
        private const float MinimumZoom = 0.25f;
        private const float MaximumZoom = 8f;
        private const float CanvasPadding = 24f;
        private const float MinimumPanelExtent = 0.005f;
        private const float MinimumVisibleCanvas = 48f;

        private readonly VisualElement canvasContent;
        private readonly Image canvasImage;
        private readonly VisualElement panelLayer;
        private readonly List<BoundaryHighlight> boundaryHighlights =
            new List<BoundaryHighlight>();

        private FoldCanvasAsset source;
        private string selectedPanelId;
        private float zoom = 1f;
        private Vector2 pan;
        private bool panning;
        private int panPointerId = -1;
        private Vector2 previousPointerPosition;

        public FoldCanvasCanvasView()
        {
            name = "canvas-view";
            focusable = true;
            style.flexGrow = 1f;
            style.position = Position.Relative;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);

            canvasContent = new VisualElement
            {
                name = "canvas-content",
                pickingMode = PickingMode.Position
            };
            canvasContent.style.position = Position.Absolute;
            canvasContent.style.backgroundColor =
                new Color(0.09f, 0.11f, 0.15f, 1f);
            canvasContent.style.borderLeftWidth = 1f;
            canvasContent.style.borderRightWidth = 1f;
            canvasContent.style.borderTopWidth = 1f;
            canvasContent.style.borderBottomWidth = 1f;
            Color canvasBorder = new Color(0.25f, 0.31f, 0.4f, 1f);
            canvasContent.style.borderLeftColor = canvasBorder;
            canvasContent.style.borderRightColor = canvasBorder;
            canvasContent.style.borderTopColor = canvasBorder;
            canvasContent.style.borderBottomColor = canvasBorder;
            Add(canvasContent);

            canvasImage = new Image
            {
                name = "appearance-image",
                scaleMode = ScaleMode.StretchToFill,
                pickingMode = PickingMode.Ignore
            };
            canvasImage.style.position = Position.Absolute;
            canvasImage.style.left = 0f;
            canvasImage.style.right = 0f;
            canvasImage.style.top = 0f;
            canvasImage.style.bottom = 0f;
            canvasContent.Add(canvasImage);

            panelLayer = new VisualElement
            {
                name = "panel-overlay-layer",
                pickingMode = PickingMode.Ignore
            };
            panelLayer.style.position = Position.Absolute;
            panelLayer.style.left = 0f;
            panelLayer.style.right = 0f;
            panelLayer.style.top = 0f;
            panelLayer.style.bottom = 0f;
            canvasContent.Add(panelLayer);

            RegisterCallback<GeometryChangedEvent>(_ => UpdateLayout());
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        public event Action<string> PanelSelected;

        public event Action<string> PanelCanvasRectEditStarted;

        public event Action<string, Rect> PanelCanvasRectChanged;

        public event Action<string> PanelCanvasRectEditCompleted;

        public float Zoom => zoom;

        public Vector2 Pan => pan;

        public void SetSource(FoldCanvasAsset asset)
        {
            source = asset;
            canvasImage.image = asset == null ? null : asset.Appearance;
            RebuildPanelOverlays();
            UpdateLayout();
        }

        public void SetSelection(string panelId)
        {
            selectedPanelId = panelId;
            RebuildPanelOverlays();
            UpdateLayout();
        }

        public void SetBoundaryHighlights(SeamDefinition seam)
        {
            boundaryHighlights.Clear();
            if (seam != null)
            {
                boundaryHighlights.Add(new BoundaryHighlight(
                    seam.A.PanelId,
                    seam.A.BoundaryId,
                    new Color(0.2f, 0.86f, 1f, 1f)));
                boundaryHighlights.Add(new BoundaryHighlight(
                    seam.B.PanelId,
                    seam.B.BoundaryId,
                    new Color(1f, 0.32f, 0.7f, 1f)));
            }

            RebuildPanelOverlays();
            UpdateLayout();
        }

        public void SetSingleBoundaryHighlight(
            string panelId,
            string boundaryId)
        {
            boundaryHighlights.Clear();
            if (!string.IsNullOrWhiteSpace(panelId) &&
                !string.IsNullOrWhiteSpace(boundaryId))
            {
                boundaryHighlights.Add(new BoundaryHighlight(
                    panelId,
                    boundaryId,
                    new Color(0.98f, 0.72f, 0.16f, 1f)));
            }

            RebuildPanelOverlays();
            UpdateLayout();
        }

        public void FrameCanvas()
        {
            zoom = 1f;
            pan = Vector2.zero;
            UpdateLayout();
        }

        public void RefreshSourceLayout()
        {
            canvasImage.image = source == null ? null : source.Appearance;
            UpdateLayout();
        }

        private void RebuildPanelOverlays()
        {
            panelLayer.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Panels.Count; i++)
            {
                PanelDefinition panel = source.Panels[i];
                if (panel == null)
                {
                    continue;
                }

                VisualElement overlay = CreatePanelOverlay(panel, i);
                panelLayer.Add(overlay);
            }
        }

        private VisualElement CreatePanelOverlay(
            PanelDefinition panel,
            int panelIndex)
        {
            bool selected = string.Equals(
                panel.Id,
                selectedPanelId,
                StringComparison.Ordinal);
            VisualElement overlay = new VisualElement
            {
                name = "panel-" + panelIndex,
                userData = panel,
                pickingMode = PickingMode.Position
            };
            overlay.style.position = Position.Absolute;
            overlay.style.backgroundColor = selected
                ? new Color(0.12f, 0.72f, 1f, 0.18f)
                : new Color(0.08f, 0.62f, 0.9f, 0.08f);
            Color border = selected
                ? new Color(0.18f, 0.84f, 1f, 1f)
                : new Color(0.12f, 0.55f, 0.82f, 0.9f);
            SetBorder(overlay, border, selected ? 2f : 1f);

            if (panel.Shape == PanelShape.Disk)
            {
                overlay.style.borderTopLeftRadius = 999f;
                overlay.style.borderTopRightRadius = 999f;
                overlay.style.borderBottomLeftRadius = 999f;
                overlay.style.borderBottomRightRadius = 999f;
            }

            Label label = new Label(panel.Id)
            {
                pickingMode = PickingMode.Ignore
            };
            label.style.position = Position.Absolute;
            label.style.left = 4f;
            label.style.top = 2f;
            label.style.color = Color.white;
            label.style.fontSize = 11f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            overlay.Add(label);

            overlay.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                PanelSelected?.Invoke(panel.Id);
                evt.StopPropagation();
            });

            AddBoundaryHighlights(overlay, panel);
            if (selected)
            {
                AddResizeHandle(overlay, panel, ResizeCorner.BottomLeft);
                AddResizeHandle(overlay, panel, ResizeCorner.BottomRight);
                AddResizeHandle(overlay, panel, ResizeCorner.TopLeft);
                AddResizeHandle(overlay, panel, ResizeCorner.TopRight);
            }

            return overlay;
        }

        private void AddBoundaryHighlights(
            VisualElement overlay,
            PanelDefinition panel)
        {
            for (int i = 0; i < boundaryHighlights.Count; i++)
            {
                BoundaryHighlight highlight = boundaryHighlights[i];
                if (!string.Equals(
                        highlight.PanelId,
                        panel.Id,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                VisualElement line = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };
                line.style.position = Position.Absolute;
                line.style.backgroundColor = highlight.Color;
                const float lineWidth = 3f;
                switch (highlight.BoundaryId)
                {
                    case "uMin":
                        line.style.left = -lineWidth * 0.5f;
                        line.style.top = 0f;
                        line.style.bottom = 0f;
                        line.style.width = lineWidth;
                        break;
                    case "uMax":
                        line.style.right = -lineWidth * 0.5f;
                        line.style.top = 0f;
                        line.style.bottom = 0f;
                        line.style.width = lineWidth;
                        break;
                    case "vMin":
                        line.style.left = 0f;
                        line.style.right = 0f;
                        line.style.bottom = -lineWidth * 0.5f;
                        line.style.height = lineWidth;
                        break;
                    case "vMax":
                        line.style.left = 0f;
                        line.style.right = 0f;
                        line.style.top = -lineWidth * 0.5f;
                        line.style.height = lineWidth;
                        break;
                    case "perimeter":
                        line.style.left = 0f;
                        line.style.right = 0f;
                        line.style.top = 0f;
                        line.style.bottom = 0f;
                        line.style.backgroundColor = Color.clear;
                        SetBorder(line, highlight.Color, lineWidth);
                        line.style.borderTopLeftRadius = 999f;
                        line.style.borderTopRightRadius = 999f;
                        line.style.borderBottomLeftRadius = 999f;
                        line.style.borderBottomRightRadius = 999f;
                        break;
                    default:
                        continue;
                }

                overlay.Add(line);
            }
        }

        private void AddResizeHandle(
            VisualElement overlay,
            PanelDefinition panel,
            ResizeCorner corner)
        {
            VisualElement handle = new VisualElement
            {
                name = "resize-" + corner,
                pickingMode = PickingMode.Position
            };
            handle.style.position = Position.Absolute;
            handle.style.width = 10f;
            handle.style.height = 10f;
            handle.style.backgroundColor =
                new Color(0.98f, 0.72f, 0.16f, 1f);
            handle.style.borderTopLeftRadius = 5f;
            handle.style.borderTopRightRadius = 5f;
            handle.style.borderBottomLeftRadius = 5f;
            handle.style.borderBottomRightRadius = 5f;

            bool left = corner == ResizeCorner.BottomLeft ||
                corner == ResizeCorner.TopLeft;
            bool top = corner == ResizeCorner.TopLeft ||
                corner == ResizeCorner.TopRight;
            if (left)
            {
                handle.style.left = -5f;
            }
            else
            {
                handle.style.right = -5f;
            }

            if (top)
            {
                handle.style.top = -5f;
            }
            else
            {
                handle.style.bottom = -5f;
            }

            Vector2 startPointer = default;
            Rect startRect = default;
            int pointerId = -1;
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                pointerId = evt.pointerId;
                startPointer = evt.position;
                startRect = panel.CanvasRect;
                handle.CapturePointer(pointerId);
                PanelCanvasRectEditStarted?.Invoke(panel.Id);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (pointerId < 0 ||
                    evt.pointerId != pointerId ||
                    !handle.HasPointerCapture(pointerId))
                {
                    return;
                }

                float width = Mathf.Max(1f, canvasContent.resolvedStyle.width);
                float height = Mathf.Max(1f, canvasContent.resolvedStyle.height);
                Vector2 delta = (Vector2)evt.position - startPointer;
                float deltaU = delta.x / width;
                float deltaV = -delta.y / height;
                Rect changed = ResizeRect(
                    startRect,
                    corner,
                    deltaU,
                    deltaV);
                PanelCanvasRectChanged?.Invoke(panel.Id, changed);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (pointerId < 0 || evt.pointerId != pointerId)
                {
                    return;
                }

                if (handle.HasPointerCapture(pointerId))
                {
                    handle.ReleasePointer(pointerId);
                }

                pointerId = -1;
                PanelCanvasRectEditCompleted?.Invoke(panel.Id);
                evt.StopPropagation();
            });
            overlay.Add(handle);
        }

        private void UpdateLayout()
        {
            float viewportWidth = resolvedStyle.width;
            float viewportHeight = resolvedStyle.height;
            if (viewportWidth <= 0f || viewportHeight <= 0f)
            {
                return;
            }

            float textureAspect = 2f;
            if (source?.Appearance != null && source.Appearance.height > 0)
            {
                textureAspect =
                    (float)source.Appearance.width / source.Appearance.height;
            }

            float availableWidth = Mathf.Max(1f, viewportWidth - CanvasPadding * 2f);
            float availableHeight = Mathf.Max(1f, viewportHeight - CanvasPadding * 2f);
            float baseWidth = availableWidth;
            float baseHeight = baseWidth / textureAspect;
            if (baseHeight > availableHeight)
            {
                baseHeight = availableHeight;
                baseWidth = baseHeight * textureAspect;
            }

            float width = baseWidth * zoom;
            float height = baseHeight * zoom;
            pan = ClampPanToVisibleCanvas(
                pan,
                new Vector2(viewportWidth, viewportHeight),
                new Vector2(width, height));
            canvasContent.style.width = width;
            canvasContent.style.height = height;
            canvasContent.style.left =
                (viewportWidth - width) * 0.5f + pan.x;
            canvasContent.style.top =
                (viewportHeight - height) * 0.5f + pan.y;

            if (source == null)
            {
                return;
            }

            int overlayIndex = 0;
            for (int panelIndex = 0;
                panelIndex < source.Panels.Count &&
                overlayIndex < panelLayer.childCount;
                panelIndex++)
            {
                PanelDefinition panel = source.Panels[panelIndex];
                if (panel == null)
                {
                    continue;
                }

                VisualElement overlay = panelLayer[overlayIndex++];
                Rect rect = panel.CanvasRect;
                overlay.style.left = rect.xMin * width;
                overlay.style.top = (1f - rect.yMax) * height;
                overlay.style.width = rect.width * width;
                overlay.style.height = rect.height * height;
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            float factor = evt.delta.y > 0f ? 0.9f : 1.1f;
            float oldZoom = zoom;
            zoom = Mathf.Clamp(oldZoom * factor, MinimumZoom, MaximumZoom);
            pan = CalculateCursorCenteredZoomPan(
                pan,
                evt.localMousePosition,
                new Vector2(
                    resolvedStyle.width * 0.5f,
                    resolvedStyle.height * 0.5f),
                oldZoom,
                zoom);
            UpdateLayout();
            evt.StopPropagation();
        }

        internal static Vector2 CalculateCursorCenteredZoomPan(
            Vector2 currentPan,
            Vector2 cursor,
            Vector2 viewportCenter,
            float oldZoom,
            float newZoom)
        {
            if (oldZoom <= 0f || newZoom <= 0f)
            {
                return currentPan;
            }

            Vector2 cursorFromCanvasCenter =
                cursor - viewportCenter - currentPan;
            return cursor - viewportCenter -
                cursorFromCanvasCenter * (newZoom / oldZoom);
        }

        internal static Vector2 ClampPanToVisibleCanvas(
            Vector2 requestedPan,
            Vector2 viewportSize,
            Vector2 canvasSize)
        {
            float maximumX = Mathf.Max(
                0f,
                viewportSize.x * 0.5f - MinimumVisibleCanvas +
                canvasSize.x * 0.5f);
            float maximumY = Mathf.Max(
                0f,
                viewportSize.y * 0.5f - MinimumVisibleCanvas +
                canvasSize.y * 0.5f);
            return new Vector2(
                Mathf.Clamp(requestedPan.x, -maximumX, maximumX),
                Mathf.Clamp(requestedPan.y, -maximumY, maximumY));
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            bool wantsPan = evt.button == 2 ||
                (evt.button == 0 && evt.altKey);
            if (!wantsPan)
            {
                return;
            }

            panning = true;
            panPointerId = evt.pointerId;
            previousPointerPosition = evt.position;
            this.CapturePointer(panPointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!panning ||
                evt.pointerId != panPointerId ||
                !this.HasPointerCapture(panPointerId))
            {
                return;
            }

            Vector2 position = evt.position;
            pan += position - previousPointerPosition;
            previousPointerPosition = position;
            UpdateLayout();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!panning || evt.pointerId != panPointerId)
            {
                return;
            }

            if (this.HasPointerCapture(panPointerId))
            {
                this.ReleasePointer(panPointerId);
            }

            panning = false;
            panPointerId = -1;
            evt.StopPropagation();
        }

        private static Rect ResizeRect(
            Rect original,
            ResizeCorner corner,
            float deltaU,
            float deltaV)
        {
            float xMin = original.xMin;
            float xMax = original.xMax;
            float yMin = original.yMin;
            float yMax = original.yMax;
            bool left = corner == ResizeCorner.BottomLeft ||
                corner == ResizeCorner.TopLeft;
            bool top = corner == ResizeCorner.TopLeft ||
                corner == ResizeCorner.TopRight;

            if (left)
            {
                xMin = Mathf.Clamp(
                    original.xMin + deltaU,
                    0f,
                    xMax - MinimumPanelExtent);
            }
            else
            {
                xMax = Mathf.Clamp(
                    original.xMax + deltaU,
                    xMin + MinimumPanelExtent,
                    1f);
            }

            if (top)
            {
                yMax = Mathf.Clamp(
                    original.yMax + deltaV,
                    yMin + MinimumPanelExtent,
                    1f);
            }
            else
            {
                yMin = Mathf.Clamp(
                    original.yMin + deltaV,
                    0f,
                    yMax - MinimumPanelExtent);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static void SetBorder(
            VisualElement element,
            Color color,
            float width)
        {
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
        }

        private readonly struct BoundaryHighlight
        {
            public BoundaryHighlight(
                string panelId,
                string boundaryId,
                Color color)
            {
                PanelId = panelId;
                BoundaryId = boundaryId;
                Color = color;
            }

            public string PanelId { get; }

            public string BoundaryId { get; }

            public Color Color { get; }
        }

        private enum ResizeCorner
        {
            BottomLeft,
            BottomRight,
            TopLeft,
            TopRight
        }
    }
}
