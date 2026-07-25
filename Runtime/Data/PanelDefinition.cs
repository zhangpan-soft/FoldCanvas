using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum PanelShape
    {
        Rectangle = 0,
        Disk = 1
    }

    [Serializable]
    public sealed class PanelDefinition
    {
        [SerializeField]
        private string id = "panel";

        [SerializeField]
        private PanelShape shape = PanelShape.Rectangle;

        [SerializeField]
        private Rect canvasRect = new Rect(0f, 0f, 1f, 1f);

        [SerializeField]
        private Vector2 physicalSize = Vector2.one;

        [SerializeField, Min(1)]
        private int uSegments = 1;

        [SerializeField, Min(1)]
        private int vSegments = 1;

        [SerializeField, Min(3)]
        private int radialSegments = 32;

        [SerializeField, Min(1)]
        private int radialRings = 4;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public PanelShape Shape
        {
            get => shape;
            set => shape = value;
        }

        public Rect CanvasRect
        {
            get => canvasRect;
            set => canvasRect = value;
        }

        public Vector2 PhysicalSize
        {
            get => physicalSize;
            set => physicalSize = value;
        }

        public int USegments
        {
            get => uSegments;
            set => uSegments = value;
        }

        public int VSegments
        {
            get => vSegments;
            set => vSegments = value;
        }

        public int RadialSegments
        {
            get => radialSegments;
            set => radialSegments = value;
        }

        public int RadialRings
        {
            get => radialRings;
            set => radialRings = value;
        }

        public static PanelDefinition CreateRectangle(
            string panelId,
            Rect sourceCanvasRect,
            Vector2 sizeMeters,
            int segmentsU,
            int segmentsV)
        {
            return new PanelDefinition
            {
                id = panelId,
                shape = PanelShape.Rectangle,
                canvasRect = sourceCanvasRect,
                physicalSize = sizeMeters,
                uSegments = segmentsU,
                vSegments = segmentsV
            };
        }

        public static PanelDefinition CreateDisk(
            string panelId,
            Rect sourceCanvasRect,
            Vector2 diameterMeters,
            int segments,
            int rings)
        {
            return new PanelDefinition
            {
                id = panelId,
                shape = PanelShape.Disk,
                canvasRect = sourceCanvasRect,
                physicalSize = diameterMeters,
                radialSegments = segments,
                radialRings = rings
            };
        }
    }
}
