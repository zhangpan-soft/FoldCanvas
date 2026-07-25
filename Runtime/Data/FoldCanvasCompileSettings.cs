using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum FoldCanvasValidationLevel
    {
        Basic = 0,
        Standard = 1,
        Strict = 2
    }

    [Serializable]
    public sealed class FoldCanvasCompileSettings
    {
        [SerializeField, Min(0.0000001f)]
        private float weldEpsilon = 0.00001f;

        [SerializeField]
        private bool recalculateNormals = true;

        [SerializeField]
        private FoldCanvasValidationLevel validationLevel = FoldCanvasValidationLevel.Basic;

        public float WeldEpsilon
        {
            get => weldEpsilon;
            set => weldEpsilon = value;
        }

        public bool RecalculateNormals
        {
            get => recalculateNormals;
            set => recalculateNormals = value;
        }

        public FoldCanvasValidationLevel ValidationLevel
        {
            get => validationLevel;
            set => validationLevel = value;
        }
    }
}
