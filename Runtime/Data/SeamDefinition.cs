using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum SeamMode
    {
        Weld = 0,
        Hinge = 1,
        KeepOpen = 2,
        Bridge = 3
    }

    [Serializable]
    public struct BoundaryReference
    {
        [SerializeField]
        private string panelId;

        [SerializeField]
        private string boundaryId;

        [SerializeField]
        private bool useSpan;

        [SerializeField]
        private Vector2 span;

        public BoundaryReference(string panel, string boundary)
        {
            panelId = panel;
            boundaryId = boundary;
            useSpan = false;
            span = new Vector2(0f, 1f);
        }

        public BoundaryReference(
            string panel,
            string boundary,
            Vector2 normalizedSpan)
        {
            panelId = panel;
            boundaryId = boundary;
            useSpan = true;
            span = normalizedSpan;
        }

        public string PanelId => panelId;

        public string BoundaryId => boundaryId;

        /// <summary>
        /// When true, Stitch selects only <see cref="Span"/> from the
        /// boundary's authored direction. Omission preserves the complete
        /// boundary contract.
        /// </summary>
        public bool UseSpan => useSpan;

        /// <summary>
        /// Normalized non-wrapping arc-length interval in authored boundary
        /// order. The value is meaningful only when <see cref="UseSpan"/> is
        /// true.
        /// </summary>
        public Vector2 Span => useSpan
            ? span
            : new Vector2(0f, 1f);
    }

    [Serializable]
    public sealed class SeamDefinition
    {
        [SerializeField]
        private string id = "seam";

        [SerializeField]
        private BoundaryReference a;

        [SerializeField]
        private BoundaryReference b;

        [SerializeField]
        private SeamMode mode = SeamMode.Weld;

        [SerializeField]
        private bool reverseB;

        [SerializeField, Min(0)]
        private int sampleCount;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public BoundaryReference A
        {
            get => a;
            set => a = value;
        }

        public BoundaryReference B
        {
            get => b;
            set => b = value;
        }

        public SeamMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public bool ReverseB
        {
            get => reverseB;
            set => reverseB = value;
        }

        public int SampleCount
        {
            get => sampleCount;
            set => sampleCount = value;
        }
    }
}
