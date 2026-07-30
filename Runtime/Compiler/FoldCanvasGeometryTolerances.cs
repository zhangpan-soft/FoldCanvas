namespace FoldCanvas
{
    internal static class FoldCanvasGeometryTolerances
    {
        public const float MinimumTriangleDoubleAreaSquared = 1e-18f;
        public const float NormalizedFoldLineTolerance = 1e-6f;
        public const float SourceTriangleBarycentricTolerance = 1e-5f;
        public const float MinimumCurrentHingeLength = 1e-7f;
        public const float MinimumStitchBoundaryLength = 1e-7f;
        public const float CurrentHingeAbsoluteTolerance = 1e-6f;
        public const float CurrentHingeRelativeTolerance = 1e-5f;
        public const float MinimumRollAngleDegrees = 1e-4f;
        public const float RollFrameAbsoluteTolerance = 1e-6f;
        public const float RollFrameRelativeTolerance = 1e-5f;
        public const float RollFrameOrthogonalityTolerance = 1e-5f;
        public const float RollSeamProofTolerance = 1e-5f;
        public const double MaximumCircularRollAngleDegrees = 360d;
        public const double FullRollAngleToleranceDegrees = 1e-4d;
        public const double MinimumRollDistortionRatio = 0.5d;
        public const double MaximumRollDistortionRatio = 2d;
        public const float SolidifyHardCornerMaximumNormalDot = 0.95f;
        public const float ClosedVolumeTopologyPositionTolerance = 1e-6f;
        public const double MinimumClosedVolume = 1e-18d;
        public const double RelativeClosedVolumeTolerance = 1e-12d;
        public const float MinimumSphericalRadius = 1e-6f;
        public const float MinimumSphericalRangeDegrees = 1e-4f;
        public const double MaximumSphericalLongitudeSpanDegrees = 360d;
        public const double SphericalAngleToleranceDegrees = 1e-4d;
        public const float SphericalRadiusAbsoluteTolerance = 1e-6f;
        public const float SphericalRadiusRelativeTolerance = 1e-5f;
    }
}
