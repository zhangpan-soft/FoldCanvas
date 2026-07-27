namespace FoldCanvas
{
    internal static class FoldCanvasGeometryTolerances
    {
        public const float MinimumTriangleDoubleAreaSquared = 1e-18f;
        public const float NormalizedFoldLineTolerance = 1e-6f;
        public const float SourceTriangleBarycentricTolerance = 1e-5f;
        public const float MinimumCurrentHingeLength = 1e-7f;
        public const float CurrentHingeAbsoluteTolerance = 1e-6f;
        public const float CurrentHingeRelativeTolerance = 1e-5f;
    }
}
