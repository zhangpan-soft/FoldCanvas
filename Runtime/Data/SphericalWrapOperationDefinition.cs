using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum SphericalWrapDirection
    {
        LongitudeAlongU = 0,
        LongitudeAlongV = 1
    }

    public enum SphericalPoleMode
    {
        Merge = 0,
        KeepFan = 1
    }

    public enum SphericalSubdivisionMode
    {
        PanelGrid = 0
    }

    [Serializable]
    public sealed class SphericalWrapOperationDefinition :
        FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField, Min(0.000001f)]
        private float radius = 0.5f;

        [SerializeField]
        private Vector2 latitudeRange = new Vector2(-90f, 90f);

        [SerializeField]
        private Vector2 longitudeRange = new Vector2(0f, 45f);

        [SerializeField]
        private SphericalWrapDirection wrapDirection =
            SphericalWrapDirection.LongitudeAlongU;

        [SerializeField]
        private SphericalPoleMode poleMode = SphericalPoleMode.Merge;

        [SerializeField]
        private SphericalSubdivisionMode subdivisionMode =
            SphericalSubdivisionMode.PanelGrid;

        public override FoldOperationType Type =>
            FoldOperationType.SphericalWrap;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public float Radius
        {
            get => radius;
            set => radius = value;
        }

        public Vector2 LatitudeRange
        {
            get => latitudeRange;
            set => latitudeRange = value;
        }

        public Vector2 LongitudeRange
        {
            get => longitudeRange;
            set => longitudeRange = value;
        }

        public SphericalWrapDirection WrapDirection
        {
            get => wrapDirection;
            set => wrapDirection = value;
        }

        public SphericalPoleMode PoleMode
        {
            get => poleMode;
            set => poleMode = value;
        }

        public SphericalSubdivisionMode SubdivisionMode
        {
            get => subdivisionMode;
            set => subdivisionMode = value;
        }
    }
}
