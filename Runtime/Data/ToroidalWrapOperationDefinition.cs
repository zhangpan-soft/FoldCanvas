using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum ToroidalWrapDirection
    {
        MajorAlongU = 0,
        MajorAlongV = 1
    }

    /// <summary>
    /// Maps an existing rectangle parameter domain onto a toroidal surface.
    /// Boundary cycles remain distinct until explicit Stitch-selected seams
    /// weld them.
    /// </summary>
    [Serializable]
    public sealed class ToroidalWrapOperationDefinition :
        FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField, Min(0.000001f)]
        private float majorRadius = 0.5f;

        [SerializeField, Min(0.000001f)]
        private float minorRadius = 0.15f;

        [SerializeField]
        private Vector2 majorAngleRange = new Vector2(0f, 360f);

        [SerializeField]
        private Vector2 minorAngleRange = new Vector2(0f, 360f);

        [SerializeField]
        private ToroidalWrapDirection wrapDirection =
            ToroidalWrapDirection.MajorAlongU;

        public override FoldOperationType Type =>
            FoldOperationType.ToroidalWrap;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public float MajorRadius
        {
            get => majorRadius;
            set => majorRadius = value;
        }

        public float MinorRadius
        {
            get => minorRadius;
            set => minorRadius = value;
        }

        public Vector2 MajorAngleRange
        {
            get => majorAngleRange;
            set => majorAngleRange = value;
        }

        public Vector2 MinorAngleRange
        {
            get => minorAngleRange;
            set => minorAngleRange = value;
        }

        public ToroidalWrapDirection WrapDirection
        {
            get => wrapDirection;
            set => wrapDirection = value;
        }
    }
}
