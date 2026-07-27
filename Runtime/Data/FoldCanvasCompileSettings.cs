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
        public const int DefaultMaxGeneratedVertices = 1000000;

        public const int DefaultMaxGeneratedTriangles = 2000000;

        public const float DefaultWeldEpsilon = 0.00001f;

        [SerializeField, Min(0.0000001f)]
        private float weldEpsilon = DefaultWeldEpsilon;

        [SerializeField]
        private bool recalculateNormals = true;

        [SerializeField]
        private FoldCanvasValidationLevel validationLevel = FoldCanvasValidationLevel.Basic;

        [SerializeField, Min(1)]
        private int maxGeneratedVertices = DefaultMaxGeneratedVertices;

        [SerializeField, Min(1)]
        private int maxGeneratedTriangles = DefaultMaxGeneratedTriangles;

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

        public int MaxGeneratedVertices
        {
            get => maxGeneratedVertices;
            set => maxGeneratedVertices = value;
        }

        public int MaxGeneratedTriangles
        {
            get => maxGeneratedTriangles;
            set => maxGeneratedTriangles = value;
        }
    }
}
