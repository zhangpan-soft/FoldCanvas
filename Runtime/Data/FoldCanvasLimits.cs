namespace FoldCanvas
{
    /// <summary>
    /// Shared native and serialized input limits. Schema validation and native
    /// ScriptableObject compilation must enforce the same values.
    /// </summary>
    public static class FoldCanvasLimits
    {
        public const int MinimumStitchSampleCount = 0;

        public const int MaximumStitchSampleCount = 8192;
    }
}
