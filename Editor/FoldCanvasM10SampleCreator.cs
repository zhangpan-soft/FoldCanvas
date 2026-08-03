using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Editor
{
    [Serializable]
    public sealed class M10WaveOperationDefinition :
        FoldOperationDefinition
    {
        [SerializeField]
        private string panelId = "surface";

        [SerializeField]
        private float amplitude = 0.18f;

        [SerializeField]
        private float cycles = 2f;

        public override FoldOperationType Type =>
            FoldOperationType.Custom;

        public string PanelId
        {
            get => panelId;
            set => panelId = value;
        }

        public float Amplitude
        {
            get => amplitude;
            set => amplitude = value;
        }

        public float Cycles
        {
            get => cycles;
            set => cycles = value;
        }
    }

    public sealed class M10WaveOperationExecutor :
        IFoldCanvasOperationExecutor
    {
        public const string StableTypeId =
            "dev.foldcanvas.samples.wave";

        public string OperationTypeId => StableTypeId;

        public string DisplayName => "FoldCanvas Wave Sample";

        public Type OperationDefinitionType =>
            typeof(M10WaveOperationDefinition);

        public FoldCanvasOperationMutationKind MutationKind =>
            FoldCanvasOperationMutationKind.PositionOnly;

        public bool TryValidate(
            FoldOperationDefinition operation,
            FoldCanvasOperationValidationContext context,
            out string targetPanelId)
        {
            if (!(operation is M10WaveOperationDefinition wave))
            {
                targetPanelId = null;
                return false;
            }

            targetPanelId = wave.PanelId;
            bool valid = IsFinite(wave.Amplitude) &&
                IsFinite(wave.Cycles) &&
                wave.Amplitude >= 0f &&
                wave.Amplitude <= 1f &&
                wave.Cycles > 0f &&
                wave.Cycles <= 16f;
            if (!valid)
            {
                context.AddDiagnostic(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes
                        .ExtensionOperationValidationFailed,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The M10 wave proof requires finite amplitude in [0,1] and cycles in (0,16].",
                    wave.PanelId,
                    wave.Id));
            }

            return valid;
        }

        public bool TryExecute(
            FoldOperationDefinition operation,
            FoldCanvasOperationExecutionContext context)
        {
            M10WaveOperationDefinition wave =
                (M10WaveOperationDefinition)operation;
            for (int i = 0; i < context.VertexCount; i++)
            {
                FoldCanvasOperationVertex vertex =
                    context.GetVertex(i);
                float u = vertex.SourcePosition.x /
                    context.PhysicalSize.x + 0.5f;
                float v = vertex.SourcePosition.y /
                    context.PhysicalSize.y + 0.5f;
                float envelope = 0.35f + 0.65f *
                    Mathf.Sin(Mathf.PI * v);
                float height = wave.Amplitude * envelope *
                    Mathf.Sin(2f * Mathf.PI * wave.Cycles * u);
                if (!context.TrySetPosition(
                        i,
                        vertex.Position + Vector3.forward * height))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }

    public static partial class FoldCanvasSampleCreator
    {
        private const string M10TexturePath =
            SampleFolder + "/M10ExtensionCanvas.png";
        private const string M10AssetPath =
            SampleFolder + "/M10ExtensionFoldCanvas.asset";
        private const string M10MaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M10ExtensionProof_Material.mat";
        private const string M10SolidMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M10ExtensionSolid_Material.mat";
        private const string M10ObjPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M10ExtensionProof.obj";
        private const string M10PreviewRootName =
            "FoldCanvas M10 Ecosystem Root";
        private const string M10PreviewCameraName =
            "Preview Camera";
        private const string M10ReportName =
            "Ecosystem Report";

        [MenuItem("Tools/FoldCanvas/Create M10 Ecosystem Sample")]
        public static void CreateM10EcosystemSample()
        {
            EnsureSampleFolder();
            Texture2D texture = CreateOrUpdateM10Appearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M10AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M10ExtensionFoldCanvas";
            }

            asset.Appearance = texture;
            asset.Panels.Clear();
            asset.Seams.Clear();
            asset.Operations.Clear();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "surface",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(2.4f, 1.4f),
                48,
                24));
            asset.Operations.Add(new M10WaveOperationDefinition
            {
                Id = "wave-surface",
                PanelId = "surface",
                Amplitude = 0.28f,
                Cycles = 2.5f
            });
            asset.SourceMetadata.AssetId = "m10-operation-extension";
            asset.SourceMetadata.DisplayName =
                "M10 Operation Extension Proof";
            asset.SourceMetadata.CanvasPixelWidth = 512;
            asset.SourceMetadata.CanvasPixelHeight = 256;

            if (created)
            {
                AssetDatabase.CreateAsset(asset, M10AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("Tools/FoldCanvas/Create M10 Ecosystem Proof")]
        public static void CreateM10EcosystemProof()
        {
            CreateM10EcosystemSample();
            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M10AssetPath);
            FoldCanvasOperationRegistry registry =
                CreateM10Registry();
            bool baked = FoldCanvasBaker.BakeMesh(
                source,
                FoldCanvasBaker.DefaultOutputFolder,
                registry,
                out Mesh mesh,
                out FoldCanvasCompileResult result);
            if (!baked)
            {
                Debug.LogError(
                    "FoldCanvas M10 ecosystem proof failed to compile:\n" +
                    JoinDiagnostics(result),
                    source);
                return;
            }

            if (!FoldCanvasObjEditorExporter.ExportToAssetPath(
                    result.CompiledData,
                    M10ObjPath,
                    new FoldCanvasObjExportOptions
                    {
                        ObjectName = "M10ExtensionProof"
                    },
                    out FoldCanvasTextExportResult export))
            {
                Debug.LogError(
                    "FoldCanvas M10 OBJ proof failed: " +
                    JoinExportDiagnostics(export),
                    source);
                return;
            }

            Material textured = CreateOrUpdateProofMaterial(
                M10MaterialPath,
                "M10ExtensionProof_Material",
                source.Appearance,
                false);
            Material solid = CreateOrUpdateM041Material(
                M10SolidMaterialPath,
                "M10ExtensionSolid_Material",
                M04SolidShaderName,
                new Color(0.25f, 0.82f, 0.76f, 1f));
            DeactivateOtherFoldCanvasPreviewsForM10();
            GameObject root = GetOrCreateM10PreviewRoot();
            GameObject texturedProof = GetOrCreateM04MeshObject(
                root,
                "Registered Wave Textured");
            ConfigureM03OwnedMeshObject(
                texturedProof,
                mesh,
                textured,
                new Vector3(-1.35f, 0f, 0f),
                Quaternion.Euler(-85f, 0f, 0f),
                Vector3.one);
            GameObject solidProof = GetOrCreateM04MeshObject(
                root,
                "Registered Wave Solid");
            ConfigureM03OwnedMeshObject(
                solidProof,
                mesh,
                solid,
                new Vector3(1.35f, 0f, 0f),
                Quaternion.Euler(-85f, 0f, 0f),
                Vector3.one);
            ConfigureM10Report(
                root,
                registry,
                result,
                export.Content);
            Camera camera = ConfigureM10Camera(root);
            Selection.activeGameObject = camera.gameObject;
            ApplyM05SceneView(camera, Vector3.zero);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log(
                $"FoldCanvas M10 proof compiled through explicit registry '{M10WaveOperationExecutor.StableTypeId}', exported deterministic OBJ to {M10ObjPath}, and preserved {mesh.vertexCount} source-UV vertices.",
                root);
        }

        internal static FoldCanvasOperationRegistry CreateM10Registry()
        {
            FoldCanvasOperationRegistry registry =
                new FoldCanvasOperationRegistry();
            if (!registry.TryRegister(
                    new M10WaveOperationExecutor(),
                    out FoldCanvasDiagnostic diagnostic))
            {
                throw new InvalidOperationException(
                    diagnostic.ToString());
            }

            return registry;
        }

        private static Texture2D CreateOrUpdateM10Appearance()
        {
            const int width = 512;
            const int height = 256;
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                false);
            for (int y = 0; y < height; y++)
            {
                float v = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float band = 0.5f + 0.5f *
                        Mathf.Sin(2f * Mathf.PI * 5f * u);
                    Color lower = new Color(0.08f, 0.18f, 0.32f, 1f);
                    Color upper = new Color(0.2f, 0.9f, 0.78f, 1f);
                    Color pixel = Color.Lerp(
                        lower,
                        upper,
                        0.25f + 0.55f * band);
                    pixel *= Mathf.Lerp(0.75f, 1f, v);
                    pixel.a = 1f;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply(false, false);
            File.WriteAllBytes(
                Path.GetFullPath(M10TexturePath),
                texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(
                M10TexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(M10TexturePath) as TextureImporter;
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

            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                M10TexturePath);
        }

        private static GameObject GetOrCreateM10PreviewRoot()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] candidates =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject root = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != scene ||
                    candidate.name != M10PreviewRootName ||
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
                root = new GameObject(M10PreviewRootName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create FoldCanvas M10 Preview Root");
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject duplicate = candidates[i];
                if (duplicate != null &&
                    duplicate != root &&
                    !EditorUtility.IsPersistent(duplicate) &&
                    duplicate.scene == scene &&
                    duplicate.name == M10PreviewRootName &&
                    duplicate.CompareTag("EditorOnly"))
                {
                    Undo.DestroyObjectImmediate(duplicate);
                }
            }

            Undo.RecordObject(root, "Update FoldCanvas M10 Preview Root");
            root.name = M10PreviewRootName;
            root.tag = "EditorOnly";
            root.SetActive(true);
            Undo.RecordObject(root.transform, "Reset FoldCanvas M10 Preview Root");
            root.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static void DeactivateOtherFoldCanvasPreviewsForM10()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != scene ||
                    !candidate.CompareTag("EditorOnly") ||
                    candidate.name == M10PreviewRootName ||
                    candidate.name.IndexOf(
                        "FoldCanvas",
                        StringComparison.Ordinal) < 0 ||
                    !candidate.activeSelf)
                {
                    continue;
                }

                Undo.RecordObject(
                    candidate,
                    "Hide Earlier FoldCanvas Preview");
                candidate.SetActive(false);
            }
        }

        private static Camera ConfigureM10Camera(GameObject root)
        {
            GameObject cameraObject = GetOrCreateM04MeshObject(
                root,
                M10PreviewCameraName);
            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = Undo.AddComponent<Camera>(cameraObject);
            }

            Undo.RecordObject(cameraObject, "Update FoldCanvas M10 Camera");
            cameraObject.tag = "Untagged";
            Undo.RecordObject(cameraObject.transform, "Frame FoldCanvas M10 Camera");
            cameraObject.transform.localPosition =
                new Vector3(0f, 2.7f, 5.5f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(
                Vector3.zero - cameraObject.transform.localPosition,
                Vector3.up);
            cameraObject.transform.localScale = Vector3.one;
            Undo.RecordObject(camera, "Configure FoldCanvas M10 Camera");
            camera.enabled = true;
            camera.orthographic = false;
            camera.fieldOfView = 36f;
            camera.orthographicSize = 2.1f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
            return camera;
        }

        private static void ConfigureM10Report(
            GameObject root,
            FoldCanvasOperationRegistry registry,
            FoldCanvasCompileResult result,
            string obj)
        {
            GameObject reportObject = GetOrCreateM04MeshObject(
                root,
                M10ReportName);
            TextMesh report = reportObject.GetComponent<TextMesh>();
            if (report == null)
            {
                MeshFilter oldFilter =
                    reportObject.GetComponent<MeshFilter>();
                if (oldFilter != null)
                {
                    Undo.DestroyObjectImmediate(oldFilter);
                }

                MeshRenderer oldRenderer =
                    reportObject.GetComponent<MeshRenderer>();
                if (oldRenderer != null)
                {
                    Undo.DestroyObjectImmediate(oldRenderer);
                }

                report = Undo.AddComponent<TextMesh>(reportObject);
            }

            Undo.RecordObject(report, "Update FoldCanvas M10 Report");
            report.text =
                "M10  EXPLICIT EXTENSION REGISTRY\n" +
                $"{registry.Descriptors[0].OperationTypeId}\n" +
                $"vertices {result.CompiledData.Vertices.Count}  " +
                $"triangles {result.CompiledData.TriangleIndices.Count / 3}\n" +
                $"OBJ sha256 {Sha256(obj).Substring(0, 16)}";
            report.fontSize = 42;
            report.characterSize = 0.035f;
            report.anchor = TextAnchor.UpperCenter;
            report.alignment = TextAlignment.Center;
            report.color = Color.white;
            Undo.RecordObject(reportObject.transform, "Position FoldCanvas M10 Report");
            reportObject.transform.localPosition =
                new Vector3(0f, 1.45f, 0f);
            reportObject.transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);
            reportObject.transform.localScale = Vector3.one;
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(
                    Encoding.UTF8.GetBytes(value));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static string JoinExportDiagnostics(
            FoldCanvasTextExportResult result)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }
    }
}
