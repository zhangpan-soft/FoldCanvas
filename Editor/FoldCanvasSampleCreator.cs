using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private const string M03PreviewRootObjectName =
            "FoldCanvas M03 Preview Root";
        private const string M03PreviewCameraObjectName =
            "FoldCanvas M03 Preview Camera";
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
            float maximumBottomGap =
                MeasureM03WallBottomFit(result.CompiledData);
            int openTopologyEdges =
                ValidateM03CupTopology(result.CompiledData);
            if (maximumBottomGap >
                FoldCanvasGeometryTolerances.RollSeamProofTolerance)
            {
                Debug.LogError(
                    "FoldCanvas M03 cup proof was not created because the wall " +
                    $"and bottom differ by {maximumBottomGap:R} m, exceeding " +
                    $"{FoldCanvasGeometryTolerances.RollSeamProofTolerance:R} m.",
                    source);
                return;
            }

            GameObject previewRoot = GetOrCreateM03PreviewRoot();
            GameObject proof = GetOrCreateM03OwnedMeshObject(
                previewRoot,
                M03ProofObjectName,
                mesh,
                material);
            ConfigureM03OwnedMeshObject(
                proof,
                mesh,
                material,
                new Vector3(0.09f, 0f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                Vector3.one);

            Mesh canvasMesh = CreateOrUpdateM03CanvasMesh();
            GameObject canvasProof = GetOrCreateM03OwnedMeshObject(
                previewRoot,
                M03CanvasProofObjectName,
                canvasMesh,
                material);
            ConfigureM03OwnedMeshObject(
                canvasProof,
                canvasMesh,
                material,
                new Vector3(-0.085f, 0f, 0f),
                Quaternion.identity,
                new Vector3(0.14f, 0.14f, 0.14f));
            Camera proofCamera = ConfigureM03ProofCamera(previewRoot);
            Selection.activeGameObject = proof;
            EditorSceneManager.MarkSceneDirty(previewRoot.scene);

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
                $"{mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles; " +
                $"{result.CompiledData.TopologyVertexCount} topology vertices, " +
                $"{openTopologyEdges} open top-rim edges; maximum wall/bottom " +
                $"boundary gap {maximumBottomGap:R} m.",
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
            const int wallSegments = 64;
            const int wallRows = 12;
            const int bottomSegments = 64;
            asset.Appearance = appearance;
            asset.Panels.Clear();
            asset.Operations.Clear();
            asset.Seams.Clear();

            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0.06f, 0.46f, 0.88f, 0.44f),
                new Vector2(2f * Mathf.PI * radius, height),
                wallSegments,
                wallRows));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.32f, 0.02f, 0.36f, 0.36f),
                Vector2.one * (2f * radius),
                bottomSegments,
                8));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-wall",
                A = new BoundaryReference("wall", "uMin"),
                B = new BoundaryReference("wall", "uMax"),
                Mode = SeamMode.Weld,
                ReverseB = false,
                SampleCount = wallRows + 1
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-bottom",
                A = new BoundaryReference("wall", "vMin"),
                B = new BoundaryReference("bottom", "perimeter"),
                Mode = SeamMode.Weld,
                ReverseB = false,
                SampleCount = bottomSegments
            });

            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll-wall",
                PanelId = "wall",
                Direction = RollDirection.U,
                AngleDegrees = 360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                ExplicitRadius = radius,
                StartAngleDegrees = 180f
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(0f, -height * 0.5f, 0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-cup"
                };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            asset.Operations.Add(stitch);
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

        private static GameObject GetOrCreateM03PreviewRoot()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] candidates =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject root = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != activeScene ||
                    candidate.name != M03PreviewRootObjectName ||
                    !candidate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                if (root == null ||
                    candidate.GetInstanceID() < root.GetInstanceID())
                {
                    root = candidate;
                }
            }

            if (root == null)
            {
                root = new GameObject(M03PreviewRootObjectName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create FoldCanvas M03 Preview Root");
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject duplicate = candidates[i];
                if (duplicate == null ||
                    duplicate == root ||
                    EditorUtility.IsPersistent(duplicate) ||
                    duplicate.scene != activeScene ||
                    duplicate.name != M03PreviewRootObjectName ||
                    !duplicate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(duplicate);
            }

            Undo.RecordObject(root, "Update FoldCanvas M03 Preview Root");
            root.name = M03PreviewRootObjectName;
            root.tag = "EditorOnly";
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            Undo.RecordObject(root.transform, "Reset FoldCanvas M03 Preview Root");
            root.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static GameObject GetOrCreateM03OwnedMeshObject(
            GameObject previewRoot,
            string objectName,
            Mesh expectedMesh,
            Material expectedMaterial)
        {
            Transform ownedTransform = previewRoot.transform.Find(objectName);
            GameObject ownedObject = ownedTransform != null
                ? ownedTransform.gameObject
                : FindLegacyM03ProofObject(
                    previewRoot.scene,
                    objectName,
                    expectedMesh,
                    expectedMaterial);
            if (ownedObject == null)
            {
                ownedObject = new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(
                    ownedObject,
                    $"Create {objectName}");
            }

            if (ownedObject.transform.parent != previewRoot.transform)
            {
                Undo.SetTransformParent(
                    ownedObject.transform,
                    previewRoot.transform,
                    $"Own {objectName}");
            }

            Undo.RecordObject(ownedObject, $"Update {objectName}");
            ownedObject.name = objectName;
            if (!ownedObject.activeSelf)
            {
                ownedObject.SetActive(true);
            }

            return ownedObject;
        }

        private static GameObject FindLegacyM03ProofObject(
            Scene scene,
            string objectName,
            Mesh expectedMesh,
            Material expectedMaterial)
        {
            MeshFilter[] filters = Resources.FindObjectsOfTypeAll<MeshFilter>();
            GameObject best = null;
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null ||
                    EditorUtility.IsPersistent(filter) ||
                    filter.gameObject.scene != scene ||
                    filter.gameObject.name != objectName ||
                    filter.sharedMesh != expectedMesh ||
                    IsUnderM03PreviewRoot(filter.transform))
                {
                    continue;
                }

                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null ||
                    renderer.sharedMaterial != expectedMaterial)
                {
                    continue;
                }

                if (best == null ||
                    filter.gameObject.GetInstanceID() < best.GetInstanceID())
                {
                    best = filter.gameObject;
                }
            }

            return best;
        }

        private static bool IsUnderM03PreviewRoot(Transform transform)
        {
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.gameObject.name == M03PreviewRootObjectName &&
                    current.gameObject.CompareTag("EditorOnly"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void ConfigureM03OwnedMeshObject(
            GameObject ownedObject,
            Mesh mesh,
            Material material,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            MeshFilter filter = ownedObject.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = Undo.AddComponent<MeshFilter>(ownedObject);
            }

            MeshRenderer renderer = ownedObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<MeshRenderer>(ownedObject);
            }

            Undo.RecordObject(filter, $"Update {ownedObject.name} Mesh");
            filter.sharedMesh = mesh;
            Undo.RecordObject(renderer, $"Update {ownedObject.name} Material");
            renderer.sharedMaterial = material;
            Undo.RecordObject(
                ownedObject.transform,
                $"Reset {ownedObject.name} Transform");
            ownedObject.transform.localPosition = localPosition;
            ownedObject.transform.localRotation = localRotation;
            ownedObject.transform.localScale = localScale;
        }

        private static Camera ConfigureM03ProofCamera(GameObject previewRoot)
        {
            Camera[] ownedCameras =
                previewRoot.GetComponentsInChildren<Camera>(true);
            Camera camera = null;
            for (int i = 0; i < ownedCameras.Length; i++)
            {
                Camera candidate = ownedCameras[i];
                if (camera == null ||
                    (candidate.gameObject.name ==
                        M03PreviewCameraObjectName &&
                        camera.gameObject.name !=
                            M03PreviewCameraObjectName) ||
                    (candidate.gameObject.name ==
                        camera.gameObject.name &&
                        candidate.GetInstanceID() < camera.GetInstanceID()))
                {
                    camera = candidate;
                }
            }

            if (camera == null)
            {
                GameObject cameraObject = new GameObject(
                    M03PreviewCameraObjectName);
                Undo.RegisterCreatedObjectUndo(
                    cameraObject,
                    "Create FoldCanvas M03 Preview Camera");
                Undo.SetTransformParent(
                    cameraObject.transform,
                    previewRoot.transform,
                    "Own FoldCanvas M03 Preview Camera");
                camera = Undo.AddComponent<Camera>(cameraObject);
            }

            for (int i = 0; i < ownedCameras.Length; i++)
            {
                if (ownedCameras[i] != camera)
                {
                    Undo.DestroyObjectImmediate(ownedCameras[i].gameObject);
                }
            }

            GameObject cameraObjectOwner = camera.gameObject;
            if (cameraObjectOwner.transform.parent != previewRoot.transform)
            {
                Undo.SetTransformParent(
                    cameraObjectOwner.transform,
                    previewRoot.transform,
                    "Own FoldCanvas M03 Preview Camera");
            }

            Undo.RecordObject(
                cameraObjectOwner,
                "Update FoldCanvas M03 Preview Camera");
            cameraObjectOwner.name = M03PreviewCameraObjectName;
            cameraObjectOwner.tag = "Untagged";
            if (!cameraObjectOwner.activeSelf)
            {
                cameraObjectOwner.SetActive(true);
            }

            Vector3 cameraPosition = new Vector3(0f, -0.14f, -0.6f);
            Vector3 cameraTarget = new Vector3(0.015f, -0.015f, 0f);
            Undo.RecordObject(
                camera.transform,
                "Reset FoldCanvas M03 Preview Camera Transform");
            camera.transform.localPosition = cameraPosition;
            camera.transform.localRotation = Quaternion.LookRotation(
                cameraTarget - cameraPosition,
                Vector3.up);
            camera.transform.localScale = Vector3.one;
            Undo.RecordObject(camera, "Update FoldCanvas M03 Preview Camera");
            camera.enabled = true;
            camera.orthographic = true;
            camera.orthographicSize = 0.13f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
            camera.depth = 100f;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            return camera;
        }

        private static float MeasureM03WallBottomFit(
            FoldCanvasCompiledData compiledData)
        {
            if (compiledData == null)
            {
                throw new InvalidDataException(
                    "The M03 cup proof has no compiled data for fit validation.");
            }

            FoldCanvasCompiledPanel wall = compiledData.GetPanel("wall");
            FoldCanvasCompiledPanel bottom = compiledData.GetPanel("bottom");
            if (wall == null || bottom == null)
            {
                throw new InvalidDataException(
                    "The M03 cup proof requires wall and bottom panels.");
            }

            FoldCanvasCompiledBoundary wallBottom = wall.GetBoundary("vMin");
            FoldCanvasCompiledBoundary bottomPerimeter =
                bottom.GetBoundary("perimeter");
            if (wallBottom == null ||
                bottomPerimeter == null ||
                wallBottom.VertexIndices.Count !=
                    bottomPerimeter.VertexIndices.Count + 1)
            {
                throw new InvalidDataException(
                    "The M03 cup proof requires matching wall and bottom perimeter sampling.");
            }

            float maximumGap = 0f;
            for (int wallOffset = 0;
                wallOffset < wallBottom.VertexIndices.Count;
                wallOffset++)
            {
                Vector3 wallPosition =
                    compiledData.Vertices[
                        wallBottom.VertexIndices[wallOffset]].Position;
                float nearestGap = float.PositiveInfinity;
                for (int bottomOffset = 0;
                    bottomOffset < bottomPerimeter.VertexIndices.Count;
                    bottomOffset++)
                {
                    Vector3 bottomPosition =
                        compiledData.Vertices[
                            bottomPerimeter.VertexIndices[bottomOffset]].Position;
                    nearestGap = Mathf.Min(
                        nearestGap,
                        Vector3.Distance(wallPosition, bottomPosition));
                }

                maximumGap = Mathf.Max(maximumGap, nearestGap);
            }

            for (int bottomOffset = 0;
                bottomOffset < bottomPerimeter.VertexIndices.Count;
                bottomOffset++)
            {
                Vector3 bottomPosition =
                    compiledData.Vertices[
                        bottomPerimeter.VertexIndices[bottomOffset]].Position;
                float nearestGap = float.PositiveInfinity;
                for (int wallOffset = 0;
                    wallOffset < wallBottom.VertexIndices.Count;
                    wallOffset++)
                {
                    Vector3 wallPosition =
                        compiledData.Vertices[
                            wallBottom.VertexIndices[wallOffset]].Position;
                    nearestGap = Mathf.Min(
                        nearestGap,
                        Vector3.Distance(bottomPosition, wallPosition));
                }

                maximumGap = Mathf.Max(maximumGap, nearestGap);
            }

            return maximumGap;
        }

        private static int ValidateM03CupTopology(
            FoldCanvasCompiledData compiledData)
        {
            const int expectedTopologyVertices = 1281;
            const int expectedOpenRimEdges = 64;
            if (compiledData.TopologyVertexCount != expectedTopologyVertices)
            {
                throw new InvalidDataException(
                    "The M03 cup proof does not have the expected welded topology vertex count.");
            }

            FoldCanvasCompiledPanel wall = compiledData.GetPanel("wall");
            FoldCanvasCompiledPanel bottom = compiledData.GetPanel("bottom");
            FoldCanvasCompiledBoundary uMin = wall.GetBoundary("uMin");
            FoldCanvasCompiledBoundary uMax = wall.GetBoundary("uMax");
            for (int i = 0; i < uMin.VertexIndices.Count; i++)
            {
                if (TopologyId(compiledData, uMin.VertexIndices[i]) !=
                    TopologyId(compiledData, uMax.VertexIndices[i]))
                {
                    throw new InvalidDataException(
                        "The M03 cup wall side seam is not topologically welded.");
                }
            }

            FoldCanvasCompiledBoundary wallBottom =
                wall.GetBoundary("vMin");
            FoldCanvasCompiledBoundary diskPerimeter =
                bottom.GetBoundary("perimeter");
            if (wallBottom.VertexIndices.Count !=
                    diskPerimeter.VertexIndices.Count + 1 ||
                TopologyId(compiledData, wallBottom.VertexIndices[0]) !=
                    TopologyId(
                        compiledData,
                        wallBottom.VertexIndices[
                            wallBottom.VertexIndices.Count - 1]))
            {
                throw new InvalidDataException(
                    "The M03 cup bottom loop is not closed before disk attachment.");
            }

            for (int i = 0; i < diskPerimeter.VertexIndices.Count; i++)
            {
                if (TopologyId(compiledData, wallBottom.VertexIndices[i]) !=
                    TopologyId(compiledData, diskPerimeter.VertexIndices[i]))
                {
                    throw new InvalidDataException(
                        "The M03 cup bottom is not topologically welded to the wall.");
                }
            }

            Dictionary<ulong, int> edgeIncidence =
                new Dictionary<ulong, int>();
            IReadOnlyList<int> triangles = compiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = TopologyId(compiledData, triangles[i]);
                int b = TopologyId(compiledData, triangles[i + 1]);
                int c = TopologyId(compiledData, triangles[i + 2]);
                AddTopologyEdge(edgeIncidence, a, b);
                AddTopologyEdge(edgeIncidence, b, c);
                AddTopologyEdge(edgeIncidence, c, a);
            }

            HashSet<ulong> expectedOpenRim = new HashSet<ulong>();
            IReadOnlyList<int> top = wall.GetBoundary("vMax").VertexIndices;
            for (int i = 0; i < top.Count - 1; i++)
            {
                expectedOpenRim.Add(TopologyEdgeKey(
                    TopologyId(compiledData, top[i]),
                    TopologyId(compiledData, top[i + 1])));
            }

            int openEdges = 0;
            foreach (KeyValuePair<ulong, int> edge in edgeIncidence)
            {
                if (edge.Value > 2 ||
                    (edge.Value == 1 && !expectedOpenRim.Contains(edge.Key)))
                {
                    throw new InvalidDataException(
                        "The M03 cup contains an unexpected open or non-manifold topology edge.");
                }

                if (edge.Value == 1)
                {
                    openEdges++;
                }
            }

            if (openEdges != expectedOpenRimEdges ||
                openEdges != expectedOpenRim.Count)
            {
                throw new InvalidDataException(
                    "The M03 cup must leave only its 64-edge top rim open.");
            }

            return openEdges;
        }

        private static int TopologyId(
            FoldCanvasCompiledData compiledData,
            int vertexIndex)
        {
            return compiledData.Vertices[vertexIndex].TopologyVertexId;
        }

        private static void AddTopologyEdge(
            Dictionary<ulong, int> edgeIncidence,
            int first,
            int second)
        {
            ulong key = TopologyEdgeKey(first, second);
            edgeIncidence.TryGetValue(key, out int count);
            edgeIncidence[key] = count + 1;
        }

        private static ulong TopologyEdgeKey(int first, int second)
        {
            int minimum = Mathf.Min(first, second);
            int maximum = Mathf.Max(first, second);
            return ((ulong)(uint)minimum << 32) | (uint)maximum;
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
