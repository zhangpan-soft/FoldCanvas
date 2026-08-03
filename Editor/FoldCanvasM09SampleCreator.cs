using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Editor
{
    public static partial class FoldCanvasSampleCreator
    {
        private const int M09CupSegments = 24;
        private const float M09CupRadius = 0.6f;
        private const float M09CupHeight = 0.9f;
        private const float M09HandleThickness = 0.025f;
        private const float M09TorusMajorRadius = 0.62f;
        private const float M09TorusMinorRadius = 0.2f;
        private const string M09TexturePath =
            SampleFolder + "/M09TopologyCanvas.png";
        private const string M09TorusAssetPath =
            SampleFolder + "/M09TorusFoldCanvas.asset";
        private const string M09HandleCupAssetPath =
            SampleFolder + "/M09HandleCupFoldCanvas.asset";
        private const string M09PreviewRootName =
            "FoldCanvas M09 Topology Root";
        private const string M09CameraName = "Preview Camera";
        private const string M09ReportName = "Topology Report";
        private const string M09TexturedMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M09TopologyTextured.mat";
        private const string M09SolidMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M09TopologySolid.mat";
        private const string M09WireframeMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M09TopologyWireframe.mat";
        private const string M09TorusWireframePath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M09TorusWireframe.asset";
        private const string M09HandleWireframePath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M09HandleCupWireframe.asset";

        [MenuItem("Tools/FoldCanvas/Create M09 Topology Samples")]
        public static void CreateM09TopologySamples()
        {
            EnsureSampleFolder();
            Texture2D appearance = CreateOrUpdateM09Appearance();
            CreateOrUpdateM09Asset(
                M09TorusAssetPath,
                "M09TorusFoldCanvas",
                appearance,
                ConfigureM09TorusAsset);
            CreateOrUpdateM09Asset(
                M09HandleCupAssetPath,
                "M09HandleCupFoldCanvas",
                appearance,
                ConfigureM09HandleCupAsset);
            AssetDatabase.SaveAssets();
            FoldCanvasAsset selected =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M09HandleCupAssetPath);
            Selection.activeObject = selected;
            EditorGUIUtility.PingObject(selected);
            Debug.Log(
                "FoldCanvas M09 source assets created. Their 2D canvas, " +
                "panels, boundary spans, seams, and ordered operations " +
                "remain authoritative; meshes are derived.",
                selected);
        }

        [MenuItem("Tools/FoldCanvas/Create M09 Topology Proof")]
        public static void CreateM09TopologyProof()
        {
            CreateM09TopologySamples();
            FoldCanvasAsset torusSource =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M09TorusAssetPath);
            FoldCanvasAsset cupSource =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M09HandleCupAssetPath);
            bool torusCompiled = FoldCanvasBaker.BakeMesh(
                torusSource,
                FoldCanvasBaker.DefaultOutputFolder,
                out Mesh torusMesh,
                out FoldCanvasCompileResult torusResult);
            bool cupCompiled = FoldCanvasBaker.BakeMesh(
                cupSource,
                FoldCanvasBaker.DefaultOutputFolder,
                out Mesh cupMesh,
                out FoldCanvasCompileResult cupResult);
            if (!torusCompiled || !cupCompiled)
            {
                Debug.LogError(
                    "FoldCanvas M09 topology proof failed to compile.\n" +
                    JoinDiagnostics(torusResult) +
                    JoinDiagnostics(cupResult));
                return;
            }

            if (torusResult.GeometryValidationReport.OpenEdgeCount != 0 ||
                torusResult.GeometryValidationReport
                    .NonManifoldEdgeCount != 0 ||
                !cupResult.ClosedVolumeReport.IsSingleClosedVolume)
            {
                Debug.LogError(
                    "FoldCanvas M09 proof rejected derived topology: " +
                    "the torus must close both cycles and the Solidified " +
                    "handle cup must be one closed volume.");
                return;
            }

            Mesh torusWireframe = CreateOrUpdateM09Wireframe(
                torusResult.CompiledData,
                M09TorusWireframePath,
                "M09Torus_LogicalWireframe");
            Mesh cupWireframe = CreateOrUpdateM09Wireframe(
                cupResult.CompiledData,
                M09HandleWireframePath,
                "M09HandleCup_LogicalWireframe");
            Material textured = CreateOrUpdateProofMaterial(
                M09TexturedMaterialPath,
                "M09TopologyTextured_Material",
                torusSource.Appearance,
                false);
            Material solid = CreateOrUpdateM041Material(
                M09SolidMaterialPath,
                "M09TopologySolid_Material",
                M04SolidShaderName,
                new Color(0.22f, 0.82f, 0.72f, 1f));
            Material wireframe = CreateOrUpdateM041Material(
                M09WireframeMaterialPath,
                "M09TopologyWireframe_Material",
                M041WireframeShaderName,
                new Color(0.18f, 0.92f, 1f, 1f));
            wireframe.renderQueue = 4001;
            EditorUtility.SetDirty(wireframe);
            AssetDatabase.SaveAssets();

            DeactivateOtherFoldCanvasPreviewsForM09();
            GameObject root = GetOrCreateM09PreviewRoot();
            ConfigureM09ProofMesh(
                root,
                "Torus Textured",
                torusMesh,
                textured,
                new Vector3(-1.15f, 0.55f, 0f),
                Quaternion.Euler(58f, -25f, 0f));
            ConfigureM09ProofMesh(
                root,
                "Torus Solid",
                torusMesh,
                solid,
                new Vector3(0.65f, 0.55f, 0f),
                Quaternion.Euler(58f, -25f, 0f));
            ConfigureM09ProofMesh(
                root,
                "Torus Wireframe",
                torusWireframe,
                wireframe,
                new Vector3(-0.25f, -0.75f, 0f),
                Quaternion.Euler(58f, -25f, 0f));
            ConfigureM09ProofMesh(
                root,
                "Handle Cup Textured",
                cupMesh,
                textured,
                new Vector3(1.75f, 0.45f, 0f),
                Quaternion.Euler(10f, -28f, 0f));
            ConfigureM09ProofMesh(
                root,
                "Handle Cup Solid",
                cupMesh,
                solid,
                new Vector3(3.3f, 0.45f, 0f),
                Quaternion.Euler(10f, -28f, 0f));
            ConfigureM09ProofMesh(
                root,
                "Handle Cup Wireframe",
                cupWireframe,
                wireframe,
                new Vector3(2.52f, -0.8f, 0f),
                Quaternion.Euler(10f, -28f, 0f));

            GameObject reportObject = GetOrCreateM04MeshObject(
                root,
                M09ReportName);
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

            Undo.RecordObject(report, "Update FoldCanvas M09 Report");
            report.text =
                "M09  2D SOURCE + TOPOLOGY RULES -> 3D\n" +
                $"TORUS  Euler 0  open " +
                $"{torusResult.GeometryValidationReport.OpenEdgeCount}  " +
                $"non-manifold " +
                $"{torusResult.GeometryValidationReport.NonManifoldEdgeCount}\n" +
                $"HANDLE CUP  components " +
                $"{cupResult.ClosedVolumeReport.ComponentCount}  open " +
                $"{cupResult.ClosedVolumeReport.OpenEdgeCount}  " +
                $"non-manifold " +
                $"{cupResult.ClosedVolumeReport.NonManifoldEdgeCount}";
            report.anchor = TextAnchor.UpperLeft;
            report.alignment = TextAlignment.Left;
            report.fontSize = 36;
            report.characterSize = 0.018f;
            report.color = new Color(0.84f, 0.95f, 1f, 1f);
            Undo.RecordObject(
                reportObject.transform,
                "Place FoldCanvas M09 Report");
            reportObject.transform.localPosition =
                new Vector3(-2.05f, -1.5f, 0f);
            reportObject.transform.localRotation = Quaternion.identity;
            reportObject.transform.localScale = Vector3.one;

            Camera camera = ConfigureM04Camera(
                root,
                M09CameraName,
                new Vector3(0.95f, 0.35f, -7f),
                new Vector3(0.95f, 0.15f, 0f),
                true);
            Undo.RecordObject(camera, "Update FoldCanvas M09 Camera");
            camera.orthographic = true;
            camera.orthographicSize = 2.1f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            camera.backgroundColor = new Color(0.018f, 0.025f, 0.045f, 1f);
            Selection.activeGameObject =
                root.transform.Find("Handle Cup Solid").gameObject;
            EditorSceneManager.MarkSceneDirty(root.scene);
            ApplyM05SceneView(camera, new Vector3(0.95f, 0.15f, 0f));
            Debug.Log(
                "FoldCanvas M09 proof compiled from two explicit 2D " +
                "sources. The torus closes U/V with two Weld seams; the " +
                "cup handle is a folded rectangle welded to two rim spans " +
                "and Solidified into one closed volume.",
                root);
        }

        [MenuItem("Tools/FoldCanvas/M09 View/Overview")]
        public static void UseM09OverviewView()
        {
            SetM09View(
                new Vector3(0.95f, 0.35f, -7f),
                new Vector3(0.95f, 0.15f, 0f),
                2.1f,
                M09ProofView.Overview);
        }

        [MenuItem("Tools/FoldCanvas/M09 View/Torus")]
        public static void UseM09TorusView()
        {
            SetM09View(
                new Vector3(-0.25f, 0.35f, -4f),
                new Vector3(-0.25f, 0.05f, 0f),
                1.3f,
                M09ProofView.Torus);
        }

        [MenuItem("Tools/FoldCanvas/M09 View/Handle Cup")]
        public static void UseM09HandleCupView()
        {
            SetM09View(
                new Vector3(2.52f, 0.7f, -4f),
                new Vector3(2.52f, 0.35f, 0f),
                1.85f,
                M09ProofView.HandleCup);
        }

        private static void CreateOrUpdateM09Asset(
            string path,
            string assetName,
            Texture2D appearance,
            Action<FoldCanvasAsset, Texture2D> configure)
        {
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(path);
            bool created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = assetName;
            }

            configure(asset, appearance);
            if (created)
            {
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }
        }

        private static void ConfigureM09TorusAsset(
            FoldCanvasAsset asset,
            Texture2D appearance)
        {
            asset.Appearance = appearance;
            asset.Panels.Clear();
            asset.Seams.Clear();
            asset.Operations.Clear();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "torus",
                new Rect(0f, 0.5f, 1f, 0.5f),
                new Vector2(
                    2f * Mathf.PI * M09TorusMajorRadius,
                    2f * Mathf.PI * M09TorusMinorRadius),
                32,
                16));
            asset.Operations.Add(new ToroidalWrapOperationDefinition
            {
                Id = "wrap-torus",
                PanelId = "torus",
                MajorRadius = M09TorusMajorRadius,
                MinorRadius = M09TorusMinorRadius,
                MajorAngleRange = new Vector2(0f, 360f),
                MinorAngleRange = new Vector2(0f, 360f),
                WrapDirection = ToroidalWrapDirection.MajorAlongU
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-major",
                A = new BoundaryReference("torus", "uMin"),
                B = new BoundaryReference("torus", "uMax"),
                Mode = SeamMode.Weld
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-minor",
                A = new BoundaryReference("torus", "vMin"),
                B = new BoundaryReference("torus", "vMax"),
                Mode = SeamMode.Weld
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "close-torus" };
            stitch.SeamIds.Add("close-major");
            stitch.SeamIds.Add("close-minor");
            asset.Operations.Add(stitch);
        }

        private static void ConfigureM09HandleCupAsset(
            FoldCanvasAsset asset,
            Texture2D appearance)
        {
            asset.Appearance = appearance;
            asset.Panels.Clear();
            asset.Seams.Clear();
            asset.Operations.Clear();
            float delta = 2f * Mathf.PI / M09CupSegments;
            float handleWidth =
                2f * M09CupRadius * Mathf.Sin(delta * 0.5f);
            float legLength =
                2f * M09CupRadius * Mathf.Cos(delta * 0.5f);
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0f, 0.62f, 1f, 0.32f),
                new Vector2(
                    2f * Mathf.PI * M09CupRadius,
                    M09CupHeight),
                M09CupSegments,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.04f, 0.05f, 0.35f, 0.35f),
                Vector2.one * (2f * M09CupRadius),
                M09CupSegments,
                8));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "handle",
                new Rect(0.55f, 0.05f, 0.12f, 0.48f),
                new Vector2(handleWidth, 3f * legLength),
                1,
                3));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-wall",
                A = new BoundaryReference("wall", "uMin"),
                B = new BoundaryReference("wall", "uMax"),
                Mode = SeamMode.Weld
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-bottom",
                A = new BoundaryReference("wall", "vMin"),
                B = new BoundaryReference("bottom", "perimeter"),
                Mode = SeamMode.Weld
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-handle-a",
                A = new BoundaryReference("handle", "vMin"),
                B = new BoundaryReference(
                    "wall",
                    "vMax",
                    new Vector2(0f, 1f / M09CupSegments)),
                Mode = SeamMode.Weld,
                ReverseB = true
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-handle-b",
                A = new BoundaryReference("handle", "vMax"),
                B = new BoundaryReference(
                    "wall",
                    "vMax",
                    new Vector2(
                        0.5f,
                        0.5f + 1f / M09CupSegments)),
                Mode = SeamMode.Weld
            });
            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll-wall",
                PanelId = "wall",
                Direction = RollDirection.U,
                AngleDegrees = 360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                StartAngleDegrees = 180f
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(
                    0f,
                    -M09CupHeight * 0.5f,
                    0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });
            Vector3 a0 = new Vector3(
                M09CupRadius,
                M09CupHeight * 0.5f,
                0f);
            Vector3 a1 = new Vector3(
                M09CupRadius * Mathf.Cos(delta),
                M09CupHeight * 0.5f,
                M09CupRadius * Mathf.Sin(delta));
            Vector3 currentU = (a0 - a1).normalized;
            Vector3 currentNormal = Vector3.Cross(
                currentU,
                Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(
                currentNormal,
                Vector3.up);
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-handle",
                PanelId = "handle",
                Translation = (a0 + a1) * 0.5f +
                    Vector3.up * (1.5f * legLength),
                RotationEuler = rotation.eulerAngles,
                Scale = Vector3.one
            });
            asset.Operations.Add(CreateM09HandleFold(
                "fold-handle-a",
                1f / 3f));
            asset.Operations.Add(CreateM09HandleFold(
                "fold-handle-b",
                2f / 3f));
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-cup-handle"
                };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            stitch.SeamIds.Add("attach-handle-a");
            stitch.SeamIds.Add("attach-handle-b");
            asset.Operations.Add(stitch);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-cup-handle",
                    Thickness = M09HandleThickness,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("wall");
            solidify.PanelIds.Add("bottom");
            solidify.PanelIds.Add("handle");
            asset.Operations.Add(solidify);
        }

        private static FoldLineOperationDefinition CreateM09HandleFold(
            string id,
            float v)
        {
            return new FoldLineOperationDefinition
            {
                Id = id,
                PanelId = "handle",
                LineStart = new Vector2(0f, v),
                LineEnd = new Vector2(1f, v),
                Side = FoldSide.Positive,
                AngleDegrees = -90f
            };
        }

        private static Texture2D CreateOrUpdateM09Appearance()
        {
            const int size = 1024;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "M09TopologyCanvas",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    byte stripe = (byte)(
                        ((x / 32 + y / 32) & 1) == 0
                            ? 34
                            : 0);
                    pixels[y * size + x] = new Color32(
                        (byte)(45 + stripe + 70 * u),
                        (byte)(130 + stripe + 70 * v),
                        (byte)(190 + stripe / 2),
                        255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(
                Path.GetFullPath(M09TexturePath),
                texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(
                M09TexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(M09TexturePath) as TextureImporter;
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

            return AssetDatabase.LoadAssetAtPath<Texture2D>(M09TexturePath);
        }

        private static Mesh CreateOrUpdateM09Wireframe(
            FoldCanvasCompiledData data,
            string path,
            string meshName)
        {
            Dictionary<int, Vector3> positions =
                BuildM041TopologyPositions(data);
            HashSet<ulong> edges = new HashSet<ulong>();
            for (int i = 0; i < data.TriangleIndices.Count; i += 3)
            {
                int a = data.Vertices[
                    data.TriangleIndices[i]].TopologyVertexId;
                int b = data.Vertices[
                    data.TriangleIndices[i + 1]].TopologyVertexId;
                int c = data.Vertices[
                    data.TriangleIndices[i + 2]].TopologyVertexId;
                edges.Add(M041EdgeKey(a, b));
                edges.Add(M041EdgeKey(b, c));
                edges.Add(M041EdgeKey(c, a));
            }

            List<ulong> ordered = new List<ulong>(edges);
            ordered.Sort();
            List<Vector3> vertices =
                new List<Vector3>(ordered.Count * 2);
            for (int i = 0; i < ordered.Count; i++)
            {
                vertices.Add(positions[(int)(ordered[i] >> 32)]);
                vertices.Add(positions[(int)(ordered[i] & uint.MaxValue)]);
            }

            return CreateOrUpdateM041LineMesh(path, meshName, vertices);
        }

        private static void ConfigureM09ProofMesh(
            GameObject root,
            string name,
            Mesh mesh,
            Material material,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject child = GetOrCreateM04MeshObject(root, name);
            ConfigureM03OwnedMeshObject(
                child,
                mesh,
                material,
                position,
                rotation,
                Vector3.one);
        }

        private static GameObject GetOrCreateM09PreviewRoot()
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
                    candidate.name != M09PreviewRootName ||
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
                root = new GameObject(M09PreviewRootName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create FoldCanvas M09 Preview Root");
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject duplicate = candidates[i];
                if (duplicate != null &&
                    duplicate != root &&
                    !EditorUtility.IsPersistent(duplicate) &&
                    duplicate.scene == scene &&
                    duplicate.name == M09PreviewRootName &&
                    duplicate.CompareTag("EditorOnly"))
                {
                    Undo.DestroyObjectImmediate(duplicate);
                }
            }

            Undo.RecordObject(root, "Update FoldCanvas M09 Preview Root");
            root.name = M09PreviewRootName;
            root.tag = "EditorOnly";
            root.SetActive(true);
            Undo.RecordObject(
                root.transform,
                "Reset FoldCanvas M09 Preview Root");
            root.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static void DeactivateOtherFoldCanvasPreviewsForM09()
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
                    candidate.name == M09PreviewRootName ||
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

        private static void SetM09View(
            Vector3 position,
            Vector3 target,
            float orthographicSize,
            M09ProofView view)
        {
            GameObject root = FindM09PreviewRoot();
            if (root == null)
            {
                CreateM09TopologyProof();
                root = FindM09PreviewRoot();
            }

            if (root == null)
            {
                return;
            }

            root.SetActive(true);
            SetM09ProofVisibility(root, view);
            Transform cameraTransform = root.transform.Find(M09CameraName);
            if (cameraTransform == null)
            {
                CreateM09TopologyProof();
                return;
            }

            Camera camera = cameraTransform.GetComponent<Camera>();
            Undo.RecordObject(cameraTransform, "Move FoldCanvas M09 Camera");
            cameraTransform.localPosition = position;
            cameraTransform.localRotation = Quaternion.LookRotation(
                target - position,
                Vector3.up);
            Undo.RecordObject(camera, "Frame FoldCanvas M09 Camera");
            camera.enabled = true;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            Selection.activeGameObject = camera.gameObject;
            ApplyM05SceneView(camera, target);
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static void SetM09ProofVisibility(
            GameObject root,
            M09ProofView view)
        {
            for (int i = 0; i < root.transform.childCount; i++)
            {
                GameObject child = root.transform.GetChild(i).gameObject;
                bool isTorus = child.name.StartsWith(
                    "Torus ",
                    StringComparison.Ordinal);
                bool isHandleCup = child.name.StartsWith(
                    "Handle Cup ",
                    StringComparison.Ordinal);
                bool isReport = child.name == M09ReportName;
                bool visible = !isTorus && !isHandleCup && !isReport ||
                    view == M09ProofView.Overview ||
                    view == M09ProofView.Torus && isTorus ||
                    view == M09ProofView.HandleCup && isHandleCup;
                if (child.activeSelf == visible)
                {
                    continue;
                }

                Undo.RecordObject(child, "Switch FoldCanvas M09 View");
                child.SetActive(visible);
            }
        }

        private static GameObject FindM09PreviewRoot()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.scene == scene &&
                    candidate.name == M09PreviewRootName &&
                    candidate.CompareTag("EditorOnly"))
                {
                    return candidate;
                }
            }

            return null;
        }

        private enum M09ProofView
        {
            Overview,
            Torus,
            HandleCup
        }
    }
}
