using System;
using UnityEngine;

namespace FoldCanvas.Samples.OperationExtension
{
    [Serializable]
    public sealed class WaveOperationDefinition :
        FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField]
        private float amplitude = 0.1f;

        [SerializeField]
        private float cycles = 2f;

        public override FoldOperationType Type =>
            FoldOperationType.Custom;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public float Amplitude
        {
            get => amplitude;
            set => amplitude = value;
        }

        public float Cycles
        {
            get => cycles;
            set => cycles = value;
        }
    }
}
