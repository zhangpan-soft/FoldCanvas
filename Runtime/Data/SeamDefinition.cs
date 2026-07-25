using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum SeamMode
    {
        Weld = 0,
        Hinge = 1,
        KeepOpen = 2
    }

    [Serializable]
    public struct BoundaryReference
    {
        [SerializeField]
        private string panelId;

        [SerializeField]
        private string boundaryId;

        public BoundaryReference(string panel, string boundary)
        {
            panelId = panel;
            boundaryId = boundary;
        }

        public string PanelId => panelId;

        public string BoundaryId => boundaryId;
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
