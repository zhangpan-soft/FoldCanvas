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
        private const string M03TexturePath = SampleFolder + "/M03CupCanvas.png";
        private const string M03AssetPath = SampleFolder + "/M03CupFoldCanvas.asset";
        private const string M03MaterialPath =
            FoldCanvasBaker.DefaultOutputFolder + "/M03CupProof_Material.mat";
        private const string M03CanvasMeshPath =
            FoldCanvasBaker.DefaultOutputFolder + "/M03SourceCanvas_Quad.asset";
        private const string M03ProofObjectName = "FoldCanvas M03 Cup Proof";
        private const string M03CanvasProofObjectName =
            "FoldCanvas M03 Source Canvas";
        private const string M03TwoSidedShaderName =
            "FoldCanvas/Two-Sided Unlit Texture";

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

        [MenuItem("Tools/FoldCanvas/Create M03 Cup Sample")]
        public static void CreateM03CupSample()
        {
            EnsureSampleFolder();
            Texture2D appearance = CreateOrUpdateM03CupAppearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M03AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M03CupFoldCanvas";
            }

            ConfigureM03CupAsset(asset, appearance);
            if (created)
            {
                AssetDatabase.CreateAsset(asset, M03AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log(
                $"FoldCanvas M03 cup sample {(created ? "created" : "updated")} at {M03AssetPath}.",
                asset);
        }

        [MenuItem("Tools/FoldCanvas/Create M03 Cup Proof")]
        public static void CreateM03CupProof()
        {
            CreateM03CupSample();
            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M03AssetPath);
            if (!FoldCanvasBaker.BakeMesh(
                    source,
                    FoldCanvasBaker.DefaultOutputFolder,
                    out Mesh mesh,
                    out FoldCanvasCompileResult result))
            {
                Debug.LogError(
                    "FoldCanvas M03 cup proof failed to compile:\n" +
                    JoinDiagnostics(result),
                    source);
                return;
            }

            Material material = CreateOrUpdateProofMaterial(
                M03MaterialPath,
                "M03CupProof_Material",
                source.Appearance,
                true);
            GameObject proof = GameObject.Find(M03ProofObjectName);
            if (proof == null)
            {
                proof = new GameObject(M03ProofObjectName);
                Undo.RegisterCreatedObjectUndo(proof, "Create FoldCanvas M03 Cup Proof");
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
            proof.transform.SetPositionAndRotation(
                new Vector3(0.09f, 0f, 0f),
                Quaternion.Euler(0f, -90f, 0f));
            proof.transform.localScale = Vector3.one;

            GameObject canvasProof = GameObject.Find(M03CanvasProofObjectName);
            if (canvasProof == null)
            {
                canvasProof = new GameObject(M03CanvasProofObjectName);
                Undo.RegisterCreatedObjectUndo(
                    canvasProof,
                    "Create FoldCanvas M03 Source Canvas");
            }

            MeshFilter canvasFilter = canvasProof.GetComponent<MeshFilter>();
            if (canvasFilter == null)
            {
                canvasFilter = Undo.AddComponent<MeshFilter>(canvasProof);
            }

            MeshRenderer canvasRenderer =
                canvasProof.GetComponent<MeshRenderer>();
            if (canvasRenderer == null)
            {
                canvasRenderer = Undo.AddComponent<MeshRenderer>(canvasProof);
            }

            canvasFilter.sharedMesh = CreateOrUpdateM03CanvasMesh();
            canvasRenderer.sharedMaterial = material;
            canvasProof.transform.SetPositionAndRotation(
                new Vector3(-0.085f, 0f, 0f),
                Quaternion.identity);
            canvasProof.transform.localScale =
                new Vector3(0.14f, 0.14f, 0.14f);
            Camera proofCamera = ConfigureM03ProofCamera();
            Selection.activeGameObject = proof;
            EditorSceneManager.MarkSceneDirty(proof.scene);

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.pivot = new Vector3(0.015f, 0f, 0f);
                sceneView.rotation = proofCamera.transform.rotation;
                sceneView.size = 0.18f;
                sceneView.Repaint();
            }

            Debug.Log(
                $"FoldCanvas M03 cup proof compiled from {M03AssetPath}: " +
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

        private static void ConfigureM03CupAsset(
            FoldCanvasAsset asset,
            Texture2D appearance)
        {
            const float radius = 0.05f;
            const float height = 0.12f;
            asset.Appearance = appearance;
            asset.Panels.Clear();
            asset.Operations.Clear();
            asset.Seams.Clear();

            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0.06f, 0.46f, 0.88f, 0.44f),
                new Vector2(2f * Mathf.PI * radius, height),
                64,
                12));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.32f, 0.02f, 0.36f, 0.36f),
                Vector2.one * (2f * radius),
                64,
                8));

            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll-wall",
                PanelId = "wall",
                Direction = RollDirection.U,
                AngleDegrees = 360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                ExplicitRadius = radius,
                StartAngleDegrees = 0f
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(0f, -height * 0.5f, 0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });
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

        private static Texture2D CreateOrUpdateM03CupAppearance()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(FoldCanvasSampleCreator).Assembly);
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package path for the M03 cup canvas.");
            }

            string sourcePath = Path.Combine(
                package.resolvedPath,
                "Samples~",
                "BootstrapPanel",
                "gpt-cup-canvas.png");
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "The packaged M03 cup canvas is missing.",
                    sourcePath);
            }

            string destinationPath = Path.GetFullPath(M03TexturePath);
            File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.ImportAsset(
                M03TexturePath,
                ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer =
                AssetImporter.GetAtPath(M03TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(M03TexturePath);
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
            return CreateOrUpdateProofMaterial(
                M02MaterialPath,
                "M02BoxProof_Material",
                appearance);
        }

        private static Material CreateOrUpdateProofMaterial(
            string materialPath,
            string materialName,
            Texture2D appearance,
            bool twoSided = false)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader;
            if (twoSided)
            {
                shader = Shader.Find(M03TwoSidedShaderName);
                if (shader == null)
                {
                    throw new InvalidDataException(
                        $"Required M03 preview shader '{M03TwoSidedShaderName}' was not imported.");
                }
            }
            else
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Texture");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, materialPath);
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

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat(
                    "_Cull",
                    (float)(twoSided
                        ? UnityEngine.Rendering.CullMode.Off
                        : UnityEngine.Rendering.CullMode.Back));
            }

            material.doubleSidedGI = twoSided;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Camera ConfigureM03ProofCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject(
                    "M03 Proof Camera",
                    typeof(Camera));
                Undo.RegisterCreatedObjectUndo(
                    cameraObject,
                    "Create FoldCanvas M03 Proof Camera");
                camera = cameraObject.GetComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, -0.035f, -0.6f);
            camera.transform.LookAt(new Vector3(0f, -0.005f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 0.13f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
            return camera;
        }

        private static Mesh CreateOrUpdateM03CanvasMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(M03CanvasMeshPath);
            bool created = mesh == null;
            if (created)
            {
                mesh = new Mesh
                {
                    name = "M03SourceCanvas_Quad"
                };
            }
            else
            {
                mesh.Clear();
            }

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[]
            {
                // The reference board faces the proof camera at -Z. The Roll
                // mesh keeps its compiled winding and unit transform.
                0, 2, 1,
                0, 3, 2
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (created)
            {
                AssetDatabase.CreateAsset(mesh, M03CanvasMeshPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
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
