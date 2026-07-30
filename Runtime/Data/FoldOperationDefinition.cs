using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    public enum FoldOperationType
    {
        RigidTransform = 0,
        Fold = 1,
        Roll = 2,
        Stitch = 3,
        Solidify = 4,
        SphericalWrap = 5
    }

    [Serializable]
    public abstract class FoldOperationDefinition
    {
        [SerializeField]
        private string id = "operation";

        [SerializeField]
        private bool enabled = true;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public abstract FoldOperationType Type { get; }
    }

    [Serializable]
    public sealed class RigidTransformOperationDefinition : FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField]
        private Vector3 translation;

        [SerializeField]
        private Vector3 rotationEuler;

        [SerializeField]
        private Vector3 scale = Vector3.one;

        public override FoldOperationType Type => FoldOperationType.RigidTransform;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public Vector3 Translation
        {
            get => translation;
            set => translation = value;
        }

        public Vector3 RotationEuler
        {
            get => rotationEuler;
            set => rotationEuler = value;
        }

        public Vector3 Scale
        {
            get => scale;
            set => scale = value;
        }
    }

    public enum FoldSide
    {
        Positive = 0,
        Negative = 1
    }

    [Serializable]
    public sealed class FoldLineOperationDefinition : FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField]
        private Vector2 lineStart = new Vector2(0f, 0.5f);

        [SerializeField]
        private Vector2 lineEnd = new Vector2(1f, 0.5f);

        [SerializeField]
        private FoldSide side = FoldSide.Positive;

        [SerializeField]
        private float angleDegrees = 90f;

        [SerializeField, Range(0f, 1f)]
        private float falloff;

        public override FoldOperationType Type => FoldOperationType.Fold;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public Vector2 LineStart
        {
            get => lineStart;
            set => lineStart = value;
        }

        public Vector2 LineEnd
        {
            get => lineEnd;
            set => lineEnd = value;
        }

        public FoldSide Side
        {
            get => side;
            set => side = value;
        }

        public float AngleDegrees
        {
            get => angleDegrees;
            set => angleDegrees = value;
        }

        public float Falloff
        {
            get => falloff;
            set => falloff = value;
        }
    }

    public enum RollDirection
    {
        U = 0,
        V = 1
    }

    public enum RollRadiusMode
    {
        PreserveArcLength = 0,
        Explicit = 1,
        FitTargetBoundary = 2
    }

    [Serializable]
    public sealed class RollOperationDefinition : FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "panel";

        [SerializeField]
        private RollDirection direction = RollDirection.U;

        [SerializeField]
        private float angleDegrees = 360f;

        [SerializeField]
        private RollRadiusMode radiusMode = RollRadiusMode.PreserveArcLength;

        [SerializeField, Min(0.000001f)]
        private float explicitRadius = 0.05f;

        [SerializeField]
        private string targetSeamId;

        [SerializeField]
        private float startAngleDegrees;

        public override FoldOperationType Type => FoldOperationType.Roll;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public RollDirection Direction
        {
            get => direction;
            set => direction = value;
        }

        public float AngleDegrees
        {
            get => angleDegrees;
            set => angleDegrees = value;
        }

        public RollRadiusMode RadiusMode
        {
            get => radiusMode;
            set => radiusMode = value;
        }

        public float ExplicitRadius
        {
            get => explicitRadius;
            set => explicitRadius = value;
        }

        public string TargetSeamId
        {
            get => targetSeamId;
            set => targetSeamId = value;
        }

        public float StartAngleDegrees
        {
            get => startAngleDegrees;
            set => startAngleDegrees = value;
        }
    }

    [Serializable]
    public sealed class StitchOperationDefinition : FoldOperationDefinition
    {
        [SerializeField]
        private List<string> seamIds = new List<string>();

        public override FoldOperationType Type => FoldOperationType.Stitch;

        public List<string> SeamIds => seamIds;
    }

    public enum SolidifyDirection
    {
        Inward = 0,
        Outward = 1,
        Centered = 2
    }

    [Serializable]
    public sealed class SolidifyOperationDefinition : FoldOperationDefinition
    {
        [SerializeField]
        private List<string> panelIds = new List<string>();

        [SerializeField, Min(0.000001f)]
        private float thickness = 0.002f;

        [SerializeField]
        private SolidifyDirection direction = SolidifyDirection.Inward;

        public override FoldOperationType Type => FoldOperationType.Solidify;

        public List<string> PanelIds => panelIds;

        public float Thickness
        {
            get => thickness;
            set => thickness = value;
        }

        public SolidifyDirection Direction
        {
            get => direction;
            set => direction = value;
        }
    }
}
