using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasSampleCreator
    {
        private const string SampleFolder = "Assets/FoldCanvasSamples";
        private const string TexturePath = SampleFolder + "/BootstrapCanvas.png";
        private const string AssetPath = SampleFolder + "/BootstrapFoldCanvas.asset";
        private const string M02TexturePath = SampleFolder + "/M02BoxCanvas.png";
        private const string M02AssetPath = SampleFolder + "/M02BoxFoldCanvas.asset";
        private const string M02MaterialPath =
            FoldCanvasBaker.DefaultOutputFolder + "/M02BoxProof_Material.mat";
        private const string M02ProofObjectName = "FoldCanvas M02 Box Proof";

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

        [MenuItem("Tools/FoldCanvas/Create M02 Box Sample")]
        public static void CreateM02BoxSample()
        {
            EnsureSampleFolder();
            Texture2D appearance = CreateM02BoxAppearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M02AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M02BoxFoldCanvas";
            }

            ConfigureM02BoxAsset(asset, appearance);
            if (created)
            {
                AssetDatabase.CreateAsset(asset, M02AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log(
                $"FoldCanvas M02 box sample {(created ? "created" : "updated")} at {M02AssetPath}.",
                asset);
        }

        [MenuItem("Tools/FoldCanvas/Create M02 Box Proof")]
        public static void CreateM02BoxProof()
        {
            CreateM02BoxSample();
            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M02AssetPath);
            if (!FoldCanvasBaker.BakeMesh(
                    source,
                    FoldCanvasBaker.DefaultOutputFolder,
                    out Mesh mesh,
                    out FoldCanvasCompileResult result))
            {
                Debug.LogError(
                    "FoldCanvas M02 box proof failed to compile:\n" +
                    JoinDiagnostics(result),
                    source);
                return;
            }

            Material material = CreateOrUpdateM02Material(source.Appearance);
            GameObject proof = GameObject.Find(M02ProofObjectName);
            if (proof == null)
            {
                proof = new GameObject(M02ProofObjectName);
                Undo.RegisterCreatedObjectUndo(proof, "Create FoldCanvas M02 Box Proof");
            }

            MeshFilter filter = proof.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = Undo.AddComponent<MeshFilter>(proof);
            }

            MeshRenderer renderer = proof.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<MeshRenderer>(proof);
            }

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            proof.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            proof.transform.localScale = Vector3.one;
            Selection.activeGameObject = proof;
            EditorSceneManager.MarkSceneDirty(proof.scene);

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.pivot = new Vector3(0f, 0f, -0.5f);
                sceneView.rotation = Quaternion.Euler(25f, -35f, 0f);
                sceneView.size = 1.8f;
                sceneView.Repaint();
            }

            Debug.Log(
                $"FoldCanvas M02 box proof compiled from {M02AssetPath}: " +
                $"{mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles.",
                proof);
        }

        private static void ConfigureM02BoxAsset(
            FoldCanvasAsset asset,
            Texture2D appearance)
        {
            const float third = 1f / 3f;
            asset.Appearance = appearance;
            asset.Panels.Clear();
            asset.Operations.Clear();
            asset.Seams.Clear();

            asset.Panels.Add(CreateBoxFace(
                "top",
                new Rect(0f, 0.5f, third, 0.5f)));
            asset.Panels.Add(CreateBoxFace(
                "bottom",
                new Rect(third, 0.5f, third, 0.5f)));
            asset.Panels.Add(CreateBoxFace(
                "front",
                new Rect(third * 2f, 0.5f, third, 0.5f)));
            asset.Panels.Add(CreateBoxFace(
                "back",
                new Rect(0f, 0f, third, 0.5f)));
            asset.Panels.Add(CreateBoxFace(
                "left",
                new Rect(third, 0f, third, 0.5f)));
            asset.Panels.Add(CreateBoxFace(
                "right",
                new Rect(third * 2f, 0f, third, 0.5f)));

            AddLayout(asset, "front", new Vector3(0f, -1f, 0f));
            AddLayout(asset, "back", new Vector3(0f, 1f, 0f));
            AddLayout(asset, "left", new Vector3(-1f, 0f, 0f));
            AddLayout(asset, "right", new Vector3(1f, 0f, 0f));

            AddFold(
                asset,
                "fold-front",
                "front",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                FoldSide.Negative,
                90f);
            AddFold(
                asset,
                "fold-back",
                "back",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                FoldSide.Positive,
                -90f);
            AddFold(
                asset,
                "fold-left",
                "left",
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                FoldSide.Positive,
                -90f);
            AddFold(
                asset,
                "fold-right",
                "right",
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                FoldSide.Negative,
                90f);
            AddFold(
                asset,
                "fold-bottom-down",
                "bottom",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                FoldSide.Positive,
                -90f);
            AddFold(
                asset,
                "fold-bottom-close",
                "bottom",
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                FoldSide.Negative,
                -90f);
        }

        private static PanelDefinition CreateBoxFace(string id, Rect canvasRect)
        {
            return PanelDefinition.CreateRectangle(
                id,
                canvasRect,
                Vector2.one,
                1,
                1);
        }

        private static void AddLayout(
            FoldCanvasAsset asset,
            string panelId,
            Vector3 translation)
        {
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "layout-" + panelId,
                PanelId = panelId,
                Translation = translation,
                RotationEuler = Vector3.zero,
                Scale = Vector3.one
            });
        }

        private static void AddFold(
            FoldCanvasAsset asset,
            string id,
            string panelId,
            Vector2 lineStart,
            Vector2 lineEnd,
            FoldSide side,
            float angle)
        {
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = id,
                PanelId = panelId,
                LineStart = lineStart,
                LineEnd = lineEnd,
                Side = side,
                AngleDegrees = angle,
                Falloff = 0f
            });
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
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static Texture2D CreateM02BoxAppearance()
        {
            const int tileSize = 128;
            const int width = tileSize * 3;
            const int height = tileSize * 2;
            Texture2D texture =
                new Texture2D(width, height, TextureFormat.RGBA32, false, false);

            DrawFaceTile(
                texture,
                0,
                1,
                new Color(0.12f, 0.72f, 0.95f, 1f),
                new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" });
            DrawFaceTile(
                texture,
                1,
                1,
                new Color(0.95f, 0.37f, 0.22f, 1f),
                new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" });
            DrawFaceTile(
                texture,
                2,
                1,
                new Color(0.98f, 0.72f, 0.16f, 1f),
                new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" });
            DrawFaceTile(
                texture,
                0,
                0,
                new Color(0.53f, 0.31f, 0.91f, 1f),
                new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" });
            DrawFaceTile(
                texture,
                1,
                0,
                new Color(0.25f, 0.82f, 0.43f, 1f),
                new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" });
            DrawFaceTile(
                texture,
                2,
                0,
                new Color(0.96f, 0.34f, 0.68f, 1f),
                new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" });

            texture.Apply(false, false);
            string absolutePath = Path.GetFullPath(M02TexturePath);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(
                M02TexturePath,
                ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer =
                AssetImporter.GetAtPath(M02TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(M02TexturePath);
        }

        private static void DrawFaceTile(
            Texture2D texture,
            int tileX,
            int tileY,
            Color baseColor,
            string[] glyph)
        {
            const int tileSize = 128;
            int originX = tileX * tileSize;
            int originY = tileY * tileSize;
            for (int y = 0; y < tileSize; y++)
            {
                float brightness = Mathf.Lerp(0.65f, 1f, y / 127f);
                for (int x = 0; x < tileSize; x++)
                {
                    bool border = x < 5 || x >= 123 || y < 5 || y >= 123;
                    Color pixel = border
                        ? new Color(0.025f, 0.025f, 0.035f, 1f)
                        : baseColor * brightness;
                    pixel.a = 1f;
                    texture.SetPixel(originX + x, originY + y, pixel);
                }
            }

            Color ink = Color.white;
            for (int y = 82; y <= 108; y++)
            {
                for (int x = 61; x <= 66; x++)
                {
                    texture.SetPixel(originX + x, originY + y, ink);
                }
            }

            for (int offset = 0; offset <= 16; offset++)
            {
                int arrowY = 108 - offset;
                texture.SetPixel(originX + 63 - offset, originY + arrowY, ink);
                texture.SetPixel(originX + 64 + offset, originY + arrowY, ink);
            }

            const int glyphScale = 8;
            int glyphOriginX = originX + 44;
            int glyphOriginY = originY + 18;
            for (int row = 0; row < glyph.Length; row++)
            {
                for (int column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] != '1')
                    {
                        continue;
                    }

                    int pixelY = glyphOriginY + (glyph.Length - 1 - row) * glyphScale;
                    int pixelX = glyphOriginX + column * glyphScale;
                    for (int dy = 0; dy < glyphScale - 1; dy++)
                    {
                        for (int dx = 0; dx < glyphScale - 1; dx++)
                        {
                            texture.SetPixel(pixelX + dx, pixelY + dy, ink);
                        }
                    }
                }
            }
        }

        private static Material CreateOrUpdateM02Material(Texture2D appearance)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(M02MaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M02BoxProof_Material"
                };
                AssetDatabase.CreateAsset(material, M02MaterialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", appearance);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", appearance);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static string JoinDiagnostics(FoldCanvasCompileResult result)
        {
            if (result == null)
            {
                return "No compile result was returned.";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }
    }
}
