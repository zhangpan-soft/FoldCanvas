using NUnit.Framework;

namespace FoldCanvas.M21Proof
{
    public sealed class FoldCanvasProofGalleryTests
    {
        [Test]
        public void RegenerateTrackedSource_ProducesValidatedEvidence()
        {
            FoldCanvasProofGalleryGenerator.GenerateForTest();
        }
    }
}
