using System;
using UnityEngine;

namespace FoldCanvas
{
    public enum FoldScriptUnit
    {
        Meter = 0,
        Centimeter = 1,
        Millimeter = 2
    }

    [Serializable]
    public sealed class FoldCanvasSourceMetadata
    {
        public const string CurrentSchemaVersion = "0.1";

        [SerializeField]
        private string schemaVersion = CurrentSchemaVersion;

        [SerializeField]
        private string assetId = "foldcanvas-asset";

        [SerializeField]
        private string displayName = "FoldCanvas Asset";

        [SerializeField]
        private FoldScriptUnit units = FoldScriptUnit.Meter;

        [SerializeField]
        private string appearanceReference = string.Empty;

        [SerializeField, Min(1)]
        private int canvasPixelWidth = 1;

        [SerializeField, Min(1)]
        private int canvasPixelHeight = 1;

        [SerializeField, TextArea]
        private string extensionsJson = string.Empty;

        public string SchemaVersion
        {
            get => schemaVersion;
            set => schemaVersion = value;
        }

        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public FoldScriptUnit Units
        {
            get => units;
            set => units = value;
        }

        public string AppearanceReference
        {
            get => appearanceReference;
            set => appearanceReference = value;
        }

        public int CanvasPixelWidth
        {
            get => canvasPixelWidth;
            set => canvasPixelWidth = value;
        }

        public int CanvasPixelHeight
        {
            get => canvasPixelHeight;
            set => canvasPixelHeight = value;
        }

        public string ExtensionsJson
        {
            get => extensionsJson;
            set => extensionsJson = value;
        }
    }
}
