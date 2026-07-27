using System.IO;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasSampleCreator
    {
        private const string SampleFolder = "Assets/FoldCanvasSamples";
        private const string TexturePath = SampleFolder + "/BootstrapCanvas.png";
        private const string AssetPath = SampleFolder + "/BootstrapFoldCanvas.asset";

        [MenuItem("Tools/FoldCanvas/Create Bootstrap Sample")]
        public static void CreateBootstrapSample()
        {
            EnsureSampleFolder();
            Texture2D appearance = CreateOrLoadAppearance();
            FoldCanvasAsset existing = AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(AssetPath);
            if (existing != null)
            {
                bool changed = false;
                if (existing.Appearance != appearance)
                {
                    existing.Appearance = appearance;
                    changed = true;
                }

                if (TryUpgradeLegacyDiskToEllipse(existing))
                {
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }

                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            FoldCanvasAsset asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "BootstrapFoldCanvas";
            asset.Appearance = appearance;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "rectangle",
                new Rect(0f, 0f, 0.5f, 1f),
                new Vector2(1.2f, 1f),
                8,
                6));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "ellipse",
                new Rect(0.5f, 0f, 0.5f, 1f),
                new Vector2(1.1f, 0.7f),
                32,
                4));

            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "move-ellipse",
                PanelId = "ellipse",
                Translation = new Vector3(1.3f, 0f, 0f),
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"FoldCanvas bootstrap sample created at {AssetPath}.", asset);
        }

        private static bool TryUpgradeLegacyDiskToEllipse(FoldCanvasAsset asset)
        {
            if (asset.Panels.Count != 2 ||
                asset.Operations.Count != 1 ||
                asset.Panels[1] == null ||
                asset.Panels[1].Id != "disk" ||
                !(asset.Operations[0] is RigidTransformOperationDefinition transform) ||
                transform.PanelId != "disk")
            {
                return false;
            }

            asset.Panels[1].Id = "ellipse";
            asset.Panels[1].PhysicalSize = new Vector2(1.1f, 0.7f);
            transform.Id = "move-ellipse";
            transform.PanelId = "ellipse";
            transform.Translation = new Vector3(1.3f, 0f, 0f);
            return true;
        }

        private static void EnsureSampleFolder()
        {
            if (!AssetDatabase.IsValidFolder(SampleFolder))
            {
                AssetDatabase.CreateFolder("Assets", "FoldCanvasSamples");
            }
        }

        private static Texture2D CreateOrLoadAppearance()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool left = x < size / 2;
                    bool checker = ((x / 16) + (y / 16)) % 2 == 0;
                    Color primary = left
                        ? new Color(0.12f, 0.68f, 0.95f, 1f)
                        : new Color(0.95f, 0.58f, 0.16f, 1f);
                    Color secondary = left
                        ? new Color(0.05f, 0.18f, 0.35f, 1f)
                        : new Color(0.35f, 0.08f, 0.18f, 1f);
                    texture.SetPixel(x, y, checker ? primary : secondary);
                }
            }

            texture.Apply(false, false);
            string absolutePath = Path.GetFullPath(TexturePath);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
    }
}
