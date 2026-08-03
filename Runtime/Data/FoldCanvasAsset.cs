using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    [CreateAssetMenu(fileName = "FoldCanvasAsset", menuName = "FoldCanvas/Fold Canvas Asset")]
    public sealed class FoldCanvasAsset : ScriptableObject
    {
        [SerializeField]
        private Texture2D appearance;

        [SerializeField]
        private FoldCanvasSourceMetadata sourceMetadata =
            new FoldCanvasSourceMetadata();

        [SerializeField]
        private List<PanelDefinition> panels = new List<PanelDefinition>();

        [SerializeReference]
        private List<FoldOperationDefinition> operations = new List<FoldOperationDefinition>();

        [SerializeField]
        private List<SeamDefinition> seams = new List<SeamDefinition>();

        [SerializeField]
        private FoldCanvasCompileSettings compileSettings = new FoldCanvasCompileSettings();

        public Texture2D Appearance
        {
            get => appearance;
            set => appearance = value;
        }

        public FoldCanvasSourceMetadata SourceMetadata
        {
            get
            {
                if (sourceMetadata == null)
                {
                    sourceMetadata = new FoldCanvasSourceMetadata();
                }

                return sourceMetadata;
            }
        }

        public List<PanelDefinition> Panels => panels;

        public List<FoldOperationDefinition> Operations => operations;

        public List<SeamDefinition> Seams => seams;

        public FoldCanvasCompileSettings CompileSettings
        {
            get
            {
                if (compileSettings == null)
                {
                    compileSettings = new FoldCanvasCompileSettings();
                }

                return compileSettings;
            }
        }
    }
}
