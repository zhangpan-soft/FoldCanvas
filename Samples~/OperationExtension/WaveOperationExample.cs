namespace FoldCanvas.Samples.OperationExtension
{
    public static class WaveOperationExample
    {
        public static bool TryCompile(
            FoldCanvasAsset source,
            out FoldCanvasCompileResult result,
            out FoldCanvasDiagnostic registrationDiagnostic)
        {
            FoldCanvasOperationRegistry registry =
                new FoldCanvasOperationRegistry();
            if (!registry.TryRegister(
                    new WaveOperationExecutor(),
                    out registrationDiagnostic))
            {
                result = null;
                return false;
            }

            result = FoldCanvasCompiler.Compile(source, registry);
            return result.Success;
        }
    }
}
