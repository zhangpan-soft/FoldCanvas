namespace FoldCanvas
{
    /// <summary>
    /// Shared native and serialized input limits. Schema validation and native
    /// ScriptableObject compilation must enforce the same values.
    /// </summary>
    public static class FoldCanvasLimits
    {
        public const int MaximumFoldScriptCharacters = 4 * 1024 * 1024;

        public const int MaximumFoldScriptJsonDepth = 64;

        public const int MaximumFoldScriptJsonNodes = 131072;

        public const int MaximumFoldScriptStringLength = 65536;

        public const int MaximumFoldScriptIdentifierLength = 64;

        public const int MaximumFoldScriptDisplayNameLength = 128;

        public const int MaximumFoldScriptAppearancePathLength = 512;

        public const int MaximumFoldScriptPanels = 4096;

        public const int MaximumFoldScriptSeams = 8192;

        public const int MaximumFoldScriptOperations = 8192;

        public const int MaximumFoldScriptCanvasDimension = 32768;

        public const int MinimumStitchSampleCount = 0;

        public const int MaximumStitchSampleCount = 8192;

        public const int MaximumStrictValidationCandidatePairs = 250000;
    }
}
