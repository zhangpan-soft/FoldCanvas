using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasM24SampleCreator
    {
        private const string SampleFolder = "Assets/FoldCanvasSamples";
        private const string AssetPath =
            SampleFolder + "/M24OffGridFoldCanvas.asset";
        private const string TexturePath =
            SampleFolder + "/M24OffGridFoldCanvas.png";
        private const string PreviewRootName =
            "FoldCanvas M24 Crease Preview Root";
        private const string FoldedObjectName = "Folded Result";
        private const string FlatObjectName = "Flat Source Topology";
        private const string FlatWireframeObjectName =
            "Flat Source Wireframe";

        [MenuItem("Tools/FoldCanvas/Create M24 Off-Grid Fold Proof")]
        public static void CreateProof()
        {
            EnsureSampleFolder();
            Texture2D appearance = CreateOrUpdateAppearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M24OffGridFoldCanvas";
            }

            Configure(asset);
            asset.Appearance = appearance;
            if (created)
            {
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            FoldCanvasCompileResult folded =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasAsset flatSource =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            CopyPanel(asset.Panels[0], flatSource);
            AddRefinementProofOperations(flatSource);
            FoldCanvasCompileResult flat =
                FoldCanvasCompiler.Compile(flatSource);
            try
            {
                if (!folded.Success || !flat.Success)
                {
                    Debug.LogError(
                        "FoldCanvas M24 proof failed to compile. " +
                        FirstDiagnostic(folded, flat),
                        asset);
                    return;
                }

                GameObject root = GetOrCreateOwnedObject(
                    PreviewRootName,
                    null);
                Undo.RecordObject(root, "Update FoldCanvas M24 Proof");
                root.tag = "EditorOnly";
                if (!root.activeSelf)
                {
                    root.SetActive(true);
                }
                GameObject foldedObject = GetOrCreateOwnedObject(
                    FoldedObjectName,
                    root.transform);
                GameObject flatObject = GetOrCreateOwnedObject(
                    FlatObjectName,
                    root.transform);
                GameObject flatWireframeObject = GetOrCreateOwnedObject(
                    FlatWireframeObjectName,
                    root.transform);
                ConfigureMeshObject(
                    foldedObject,
                    folded.Mesh,
                    new Vector3(0.8f, 0f, 0f),
                    Color.white,
                    appearance);
                ConfigureMeshObject(
                    flatObject,
                    flat.Mesh,
                    new Vector3(-0.8f, 0f, 0f),
                    Color.white,
                    appearance);
                ConfigureWireframeObject(
                    flatWireframeObject,
                    flat.Mesh,
                    new Vector3(-0.8f, 0f, -0.002f));

                Selection.activeGameObject = foldedObject;
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.pivot = Vector3.zero;
                    sceneView.rotation = Quaternion.Euler(24f, -28f, 0f);
                    sceneView.size = 1.7f;
                    sceneView.Repaint();
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(root.scene);
                Debug.Log(
                    "FoldCanvas M24 proof compiled from one coarse 2D rectangle and off-grid FoldScript crease: " +
                    folded.CompiledData.Vertices.Count + " vertices, " +
                    folded.CompiledData.TriangleIndices.Count / 3 +
                    " triangles. Both surfaces sample the same decorated 2D canvas; the flat source also carries the derived wireframe.",
                    foldedObject);
            }
            finally
            {
                Object.DestroyImmediate(flatSource);
            }
        }

        private static void Configure(FoldCanvasAsset asset)
        {
            asset.Panels.Clear();
            asset.Seams.Clear();
            asset.Operations.Clear();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "off-grid-crease",
                PanelId = "panel",
                LineStart = new Vector2(0.3f, 0f),
                LineEnd = new Vector2(0.3f, 1f),
                Side = FoldSide.Positive,
                AngleDegrees = 90f,
                Falloff = 0f
            });
        }

        private static void CopyPanel(
            PanelDefinition source,
            FoldCanvasAsset target)
        {
            target.Panels.Add(PanelDefinition.CreateRectangle(
                source.Id,
                source.CanvasRect,
                source.PhysicalSize,
                source.USegments,
                source.VSegments));
        }

        private static void AddRefinementProofOperations(
            FoldCanvasAsset asset)
        {
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "refine-crease",
                PanelId = "panel",
                LineStart = new Vector2(0.3f, 0f),
                LineEnd = new Vector2(0.3f, 1f),
                Side = FoldSide.Positive,
                AngleDegrees = 90f,
                Falloff = 0f
            });
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "unfold-proof",
                PanelId = "panel",
                LineStart = new Vector2(0.3f, 0f),
                LineEnd = new Vector2(0.3f, 1f),
                Side = FoldSide.Positive,
                AngleDegrees = -90f,
                Falloff = 0f
            });
        }

        private static void ConfigureMeshObject(
            GameObject target,
            Mesh mesh,
            Vector3 position,
            Color color,
            Texture texture = null)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = Undo.AddComponent<MeshFilter>(target);
            }

                MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<MeshRenderer>(target);
            }

            Undo.RecordObject(filter, "Update FoldCanvas M24 Mesh");
            DestroyOwnedMesh(filter.sharedMesh, mesh);
            mesh.hideFlags = HideFlags.HideAndDontSave;
            filter.sharedMesh = mesh;
            Undo.RecordObject(renderer, "Update FoldCanvas M24 Material");
            DestroyOwnedMaterial(renderer.sharedMaterial);
            Material material = new Material(ResolveShader(texture != null))
            {
                name = target.name + " Material",
                color = color,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat(
                    "_Cull",
                    (float)UnityEngine.Rendering.CullMode.Back);
            }

            renderer.sharedMaterial = material;
            material.doubleSidedGI = false;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            Undo.RecordObject(target.transform, "Update FoldCanvas M24 Proof");
            target.transform.localPosition = position;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            if (!target.activeSelf)
            {
                target.SetActive(true);
            }
        }

        private static void ConfigureWireframeObject(
            GameObject target,
            Mesh source,
            Vector3 position)
        {
            Mesh wireframe = BuildWireframe(source);
            ConfigureMeshObject(
                target,
                wireframe,
                position,
                new Color(0.08f, 0.09f, 0.12f, 1f));
        }

        private static Texture2D CreateOrUpdateAppearance()
        {
            const int width = 512;
            const int height = 512;
            Texture2D generated = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "M24OffGridFoldCanvas",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    bool checker = ((x / 64 + y / 64) & 1) == 0;
                    byte checkerLift = checker ? (byte)22 : (byte)0;
                    byte red = (byte)Mathf.Clamp(
                        55f + 135f * u + checkerLift,
                        0f,
                        255f);
                    byte green = (byte)Mathf.Clamp(
                        90f + 115f * v + checkerLift,
                        0f,
                        255f);
                    byte blue = (byte)Mathf.Clamp(
                        225f - 85f * u + checkerLift,
                        0f,
                        255f);
                    if (Mathf.Abs(u - 0.3f) <= 0.008f)
                    {
                        red = 255;
                        green = 226;
                        blue = 74;
                    }

                    pixels[y * width + x] =
                        new Color32(red, green, blue, 255);
                }
            }

            generated.SetPixels32(pixels);
            generated.Apply(false, false);
            File.WriteAllBytes(
                Path.GetFullPath(TexturePath),
                generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(
                TexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(TexturePath) as TextureImporter;
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

            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static Mesh BuildWireframe(Mesh source)
        {
            int[] triangles = source.triangles;
            HashSet<ulong> seen = new HashSet<ulong>();
            List<int> lineIndices = new List<int>(triangles.Length * 2);
            for (int offset = 0; offset < triangles.Length; offset += 3)
            {
                AddWireEdge(
                    triangles[offset],
                    triangles[offset + 1],
                    seen,
                    lineIndices);
                AddWireEdge(
                    triangles[offset + 1],
                    triangles[offset + 2],
                    seen,
                    lineIndices);
                AddWireEdge(
                    triangles[offset + 2],
                    triangles[offset],
                    seen,
                    lineIndices);
            }

            Mesh wireframe = new Mesh
            {
                name = "M24 Flat Source Wireframe",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = source.vertices
            };
            wireframe.SetIndices(
                lineIndices.ToArray(),
                MeshTopology.Lines,
                0,
                true);
            return wireframe;
        }

        private static void AddWireEdge(
            int first,
            int second,
            ISet<ulong> seen,
            ICollection<int> indices)
        {
            uint minimum = (uint)Mathf.Min(first, second);
            uint maximum = (uint)Mathf.Max(first, second);
            ulong key = ((ulong)minimum << 32) | maximum;
            if (!seen.Add(key))
            {
                return;
            }

            indices.Add(first);
            indices.Add(second);
        }

        private static void DestroyOwnedMesh(
            Mesh current,
            Mesh replacement)
        {
            if (current != null &&
                current != replacement &&
                !EditorUtility.IsPersistent(current) &&
                (current.hideFlags & HideFlags.DontSave) != 0)
            {
                Object.DestroyImmediate(current);
            }
        }

        private static void DestroyOwnedMaterial(Material material)
        {
            if (material != null &&
                !EditorUtility.IsPersistent(material) &&
                (material.hideFlags & HideFlags.DontSave) != 0)
            {
                Object.DestroyImmediate(material);
            }
        }

        private static Shader ResolveShader(bool textured)
        {
            Shader shader = textured
                ? Shader.Find("Unlit/Texture")
                : Shader.Find("Unlit/Color");
            if (shader == null && textured)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            return shader ?? Shader.Find("Standard");
        }

        private static GameObject GetOrCreateOwnedObject(
            string objectName,
            Transform parent)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                GameObject candidate = all[i];
                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.scene == scene &&
                    candidate.name == objectName &&
                    candidate.transform.parent == parent &&
                    (parent != null || candidate.CompareTag("EditorOnly")))
                {
                    Undo.RecordObject(
                        candidate,
                        "Update FoldCanvas M24 Proof");
                    return candidate;
                }
            }

            GameObject created = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(
                created,
                "Create FoldCanvas M24 Proof");
            created.transform.SetParent(parent, false);
            return created;
        }

        private static string FirstDiagnostic(
            FoldCanvasCompileResult first,
            FoldCanvasCompileResult second)
        {
            if (first.Diagnostics.Count > 0)
            {
                return first.Diagnostics[0].ToString();
            }

            return second.Diagnostics.Count > 0
                ? second.Diagnostics[0].ToString()
                : "No diagnostic was returned.";
        }

        private static void EnsureSampleFolder()
        {
            if (!AssetDatabase.IsValidFolder(SampleFolder))
            {
                AssetDatabase.CreateFolder("Assets", "FoldCanvasSamples");
            }
        }
    }
}
