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
        private const string M04TexturePath =
            SampleFolder + "/M04ProductionCupCanvas.png";
        private const string M04AssetPath =
            SampleFolder + "/M04ProductionCupFoldCanvas.asset";
        private const string M04SolidMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M04CupSolidDiagnostic_Material.mat";
        private const string M04TexturedMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M04CupProduction_Material.mat";
        private const string M04CanvasMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M04ProductionCanvas_Quad.asset";
        private const string M04PreviewRootObjectName =
            "FoldCanvas M04 Preview Root";
        private const string M04SolidProofObjectName =
            "FoldCanvas M04 Solid Proof";
        private const string M04TexturedProofObjectName =
            "FoldCanvas M04 Production Proof";
        private const string M04CanvasProofObjectName =
            "FoldCanvas M04 Production Canvas";
        private const string M04ExteriorCameraObjectName =
            "FoldCanvas M04 Exterior Camera";
        private const string M04SideCameraObjectName =
            "FoldCanvas M04 Exact Side Camera";
        private const string M04InteriorCameraObjectName =
            "FoldCanvas M04 Interior Camera";
        private const string M04UndersideCameraObjectName =
            "FoldCanvas M04 Underside Camera";
        private const string M04SolidShaderName =
            "FoldCanvas/One-Sided Solid Color";
        private const float M04Thickness = 0.004f;

        [MenuItem("Tools/FoldCanvas/Create M04 Production Cup Sample")]
        public static void CreateM04ProductionCupSample()
        {
            EnsureSampleFolder();
            Texture2D appearance =
                CreateOrUpdateM04ProductionAppearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M04AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset =
                    ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M04ProductionCupFoldCanvas";
            }

            ConfigureM03CupAsset(asset, appearance);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-cup",
                    Thickness = M04Thickness,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("wall");
            solidify.PanelIds.Add("bottom");
            asset.Operations.Add(solidify);

            if (created)
            {
                AssetDatabase.CreateAsset(asset, M04AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log(
                $"FoldCanvas M04 production cup sample " +
                $"{(created ? "created" : "updated")} at " +
                $"{M04AssetPath}.",
                asset);
        }

        [MenuItem("Tools/FoldCanvas/Create M04 Production Cup Proof")]
        public static void CreateM04ProductionCupProof()
        {
            CreateM04ProductionCupSample();
            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M04AssetPath);
            if (!FoldCanvasBaker.BakeMesh(
                    source,
                    FoldCanvasBaker.DefaultOutputFolder,
                    out Mesh mesh,
                    out FoldCanvasCompileResult result))
            {
                Debug.LogError(
                    "FoldCanvas M04 production cup proof failed to " +
                    "compile:\n" + JoinDiagnostics(result),
                    source);
                return;
            }

            float maximumBottomGap =
                MeasureM03WallBottomFit(result.CompiledData);
            CountTopologyEdges(
                result.CompiledData,
                out int openEdges,
                out int nonManifoldEdges);
            float measuredThickness =
                MeasureM04BottomCenterThickness(result.CompiledData);
            if (maximumBottomGap >
                    FoldCanvasGeometryTolerances
                        .RollSeamProofTolerance ||
                openEdges != 0 ||
                nonManifoldEdges != 0)
            {
                Debug.LogError(
                    "FoldCanvas M04 proof rejected its generated " +
                    $"geometry: wall/bottom gap={maximumBottomGap:R} m, " +
                    $"open edges={openEdges}, non-manifold edges=" +
                    $"{nonManifoldEdges}.",
                    source);
                return;
            }

            Material solidMaterial =
                CreateOrUpdateM04SolidMaterial();
            Material texturedMaterial =
                CreateOrUpdateProofMaterial(
                    M04TexturedMaterialPath,
                    "M04CupProduction_Material",
                    source.Appearance,
                    false);
            DeactivateM03PreviewForM04();
            GameObject previewRoot = GetOrCreateM04PreviewRoot();
            GameObject solidProof = GetOrCreateM04MeshObject(
                previewRoot,
                M04SolidProofObjectName);
            ConfigureM03OwnedMeshObject(
                solidProof,
                mesh,
                solidMaterial,
                new Vector3(-0.035f, 0f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                Vector3.one);
            GameObject texturedProof = GetOrCreateM04MeshObject(
                previewRoot,
                M04TexturedProofObjectName);
            ConfigureM03OwnedMeshObject(
                texturedProof,
                mesh,
                texturedMaterial,
                new Vector3(0.085f, 0f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
                Vector3.one);

            Mesh canvasMesh = CreateOrUpdateM04CanvasMesh();
            GameObject canvasProof = GetOrCreateM04MeshObject(
                previewRoot,
                M04CanvasProofObjectName);
            ConfigureM03OwnedMeshObject(
                canvasProof,
                canvasMesh,
                texturedMaterial,
                new Vector3(-0.19f, 0f, 0f),
                Quaternion.identity,
                new Vector3(0.14f, 0.14f, 0.14f));

            Camera exterior = ConfigureM04Camera(
                previewRoot,
                M04ExteriorCameraObjectName,
                new Vector3(0.22f, 0.1f, -0.48f),
                new Vector3(0f, 0f, 0f),
                true);
            ConfigureM04Camera(
                previewRoot,
                M04SideCameraObjectName,
                new Vector3(0.025f, 0f, -0.5f),
                new Vector3(0.025f, 0f, 0f),
                false);
            ConfigureM04Camera(
                previewRoot,
                M04InteriorCameraObjectName,
                new Vector3(0.025f, 0.3f, -0.28f),
                new Vector3(0.025f, 0f, 0f),
                false);
            ConfigureM04Camera(
                previewRoot,
                M04UndersideCameraObjectName,
                new Vector3(0.025f, -0.3f, -0.28f),
                new Vector3(0.025f, -0.025f, 0f),
                false);

            Selection.activeGameObject = texturedProof;
            EditorSceneManager.MarkSceneDirty(previewRoot.scene);
            ApplyM04SceneView(exterior);
            Debug.Log(
                $"FoldCanvas M04 production proof compiled from " +
                $"{M04AssetPath}: {mesh.vertexCount} render vertices, " +
                $"{result.CompiledData.TopologyVertexCount} topology " +
                $"vertices, {mesh.triangles.Length / 3} triangles, " +
                $"wall/bottom gap {maximumBottomGap:R} m, requested " +
                $"thickness {M04Thickness:R} m, measured bottom-center " +
                $"thickness {measuredThickness:R} m, open edges " +
                $"{openEdges}, non-manifold edges {nonManifoldEdges}. " +
                "The exterior camera is the default; exact-side, " +
                "interior, and underside cameras remain owned " +
                "validation views.",
                texturedProof);
        }

        [MenuItem("Tools/FoldCanvas/M04 View/Exterior")]
        public static void UseM04ExteriorView()
        {
            ActivateM04View(M04ExteriorCameraObjectName);
        }

        [MenuItem("Tools/FoldCanvas/M04 View/Exact Side")]
        public static void UseM04ExactSideView()
        {
            ActivateM04View(M04SideCameraObjectName);
        }

        [MenuItem("Tools/FoldCanvas/M04 View/Interior")]
        public static void UseM04InteriorView()
        {
            ActivateM04View(M04InteriorCameraObjectName);
        }

        [MenuItem("Tools/FoldCanvas/M04 View/Underside")]
        public static void UseM04UndersideView()
        {
            ActivateM04View(M04UndersideCameraObjectName);
        }

        private static Texture2D CreateOrUpdateM04ProductionAppearance()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo
                    .FindForAssembly(
                        typeof(FoldCanvasSampleCreator).Assembly);
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package path " +
                        "for the M04 production canvas.");
            }

            string sourcePath = Path.Combine(
                package.resolvedPath,
                "Samples~",
                "BootstrapPanel",
                "M04ProductionCupCanvas.png");
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "The packaged M04 production canvas is missing.",
                    sourcePath);
            }

            File.Copy(
                sourcePath,
                Path.GetFullPath(M04TexturePath),
                true);
            AssetDatabase.ImportAsset(
                M04TexturePath,
                ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer =
                AssetImporter.GetAtPath(
                    M04TexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType =
                    TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.npotScale =
                    TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                M04TexturePath);
        }

        private static Material CreateOrUpdateM04SolidMaterial()
        {
            Shader shader = Shader.Find(M04SolidShaderName);
            if (shader == null)
            {
                throw new InvalidDataException(
                    $"Required M04 proof shader " +
                    $"'{M04SolidShaderName}' was not imported.");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    M04SolidMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M04CupSolidDiagnostic_Material"
                };
                AssetDatabase.CreateAsset(
                    material,
                    M04SolidMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor(
                "_Color",
                new Color(0.28f, 0.78f, 0.86f, 1f));
            material.doubleSidedGI = false;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static GameObject GetOrCreateM04PreviewRoot()
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
                    candidate.name !=
                        M04PreviewRootObjectName ||
                    !candidate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                if (root == null ||
                    candidate.GetInstanceID() <
                        root.GetInstanceID())
                {
                    root = candidate;
                }
            }

            if (root == null)
            {
                root = new GameObject(
                    M04PreviewRootObjectName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create FoldCanvas M04 Preview Root");
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject duplicate = candidates[i];
                if (duplicate == null ||
                    duplicate == root ||
                    EditorUtility.IsPersistent(duplicate) ||
                    duplicate.scene != activeScene ||
                    duplicate.name !=
                        M04PreviewRootObjectName ||
                    !duplicate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(duplicate);
            }

            Undo.RecordObject(
                root,
                "Update FoldCanvas M04 Preview Root");
            root.name = M04PreviewRootObjectName;
            root.tag = "EditorOnly";
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            Undo.RecordObject(
                root.transform,
                "Reset FoldCanvas M04 Preview Root");
            root.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static GameObject GetOrCreateM04MeshObject(
            GameObject previewRoot,
            string objectName)
        {
            Transform child =
                previewRoot.transform.Find(objectName);
            GameObject ownedObject = child != null
                ? child.gameObject
                : new GameObject(objectName);
            if (child == null)
            {
                Undo.RegisterCreatedObjectUndo(
                    ownedObject,
                    $"Create {objectName}");
                Undo.SetTransformParent(
                    ownedObject.transform,
                    previewRoot.transform,
                    $"Own {objectName}");
            }

            Undo.RecordObject(
                ownedObject,
                $"Update {objectName}");
            ownedObject.name = objectName;
            if (!ownedObject.activeSelf)
            {
                ownedObject.SetActive(true);
            }

            return ownedObject;
        }

        private static Camera ConfigureM04Camera(
            GameObject previewRoot,
            string objectName,
            Vector3 position,
            Vector3 target,
            bool enabled)
        {
            Transform child =
                previewRoot.transform.Find(objectName);
            GameObject cameraObject = child != null
                ? child.gameObject
                : new GameObject(objectName);
            if (child == null)
            {
                Undo.RegisterCreatedObjectUndo(
                    cameraObject,
                    $"Create {objectName}");
                Undo.SetTransformParent(
                    cameraObject.transform,
                    previewRoot.transform,
                    $"Own {objectName}");
            }

            Camera camera =
                cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = Undo.AddComponent<Camera>(
                    cameraObject);
            }

            Undo.RecordObject(
                cameraObject,
                $"Update {objectName}");
            cameraObject.name = objectName;
            cameraObject.tag = "Untagged";
            if (!cameraObject.activeSelf)
            {
                cameraObject.SetActive(true);
            }

            Undo.RecordObject(
                cameraObject.transform,
                $"Reset {objectName} Transform");
            cameraObject.transform.localPosition = position;
            cameraObject.transform.localRotation =
                Quaternion.LookRotation(
                    target - position,
                    Vector3.up);
            cameraObject.transform.localScale = Vector3.one;
            Undo.RecordObject(
                camera,
                $"Update {objectName}");
            camera.enabled = enabled;
            camera.orthographic = true;
            camera.orthographicSize = 0.145f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2f;
            camera.clearFlags =
                CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.025f, 0.03f, 0.045f, 1f);
            camera.depth = 200f;
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            return camera;
        }

        private static void DeactivateM03PreviewForM04()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] candidates =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != activeScene ||
                    candidate.name !=
                        M03PreviewRootObjectName ||
                    !candidate.CompareTag("EditorOnly") ||
                    !candidate.activeSelf)
                {
                    continue;
                }

                Undo.RecordObject(
                    candidate,
                    "Hide FoldCanvas M03 Presentation Preview");
                candidate.SetActive(false);
            }
        }

        private static void ActivateM04View(
            string cameraObjectName)
        {
            GameObject previewRoot =
                FindM04PreviewRootInActiveScene();
            if (previewRoot == null)
            {
                CreateM04ProductionCupProof();
                previewRoot =
                    FindM04PreviewRootInActiveScene();
            }

            if (previewRoot == null)
            {
                return;
            }

            if (!previewRoot.activeSelf)
            {
                Undo.RecordObject(
                    previewRoot,
                    "Show FoldCanvas M04 Preview");
                previewRoot.SetActive(true);
            }

            Camera selected = null;
            Camera[] cameras =
                previewRoot.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                bool enable =
                    cameras[i].gameObject.name ==
                    cameraObjectName;
                Undo.RecordObject(
                    cameras[i],
                    "Switch FoldCanvas M04 Validation View");
                cameras[i].enabled = enable;
                if (enable)
                {
                    selected = cameras[i];
                }
            }

            if (selected != null)
            {
                Selection.activeGameObject =
                    selected.gameObject;
                ApplyM04SceneView(selected);
                EditorSceneManager.MarkSceneDirty(
                    previewRoot.scene);
            }
        }

        private static GameObject FindM04PreviewRootInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] candidates =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.scene == activeScene &&
                    candidate.name ==
                        M04PreviewRootObjectName &&
                    candidate.CompareTag("EditorOnly"))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ApplyM04SceneView(Camera camera)
        {
            SceneView sceneView =
                SceneView.lastActiveSceneView;
            if (sceneView == null || camera == null)
            {
                return;
            }

            sceneView.pivot =
                new Vector3(0.015f, 0f, 0f);
            sceneView.rotation =
                camera.transform.rotation;
            sceneView.size = 0.19f;
            sceneView.Repaint();
        }

        private static Mesh CreateOrUpdateM04CanvasMesh()
        {
            Mesh mesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    M04CanvasMeshPath);
            bool created = mesh == null;
            if (created)
            {
                mesh = new Mesh
                {
                    name = "M04ProductionCanvas_Quad"
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
                0, 2, 1,
                0, 3, 2
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (created)
            {
                AssetDatabase.CreateAsset(
                    mesh,
                    M04CanvasMeshPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void CountTopologyEdges(
            FoldCanvasCompiledData data,
            out int openEdges,
            out int nonManifoldEdges)
        {
            Dictionary<ulong, int> incidence =
                new Dictionary<ulong, int>();
            for (int i = 0;
                i < data.TriangleIndices.Count;
                i += 3)
            {
                int a = TopologyId(
                    data,
                    data.TriangleIndices[i]);
                int b = TopologyId(
                    data,
                    data.TriangleIndices[i + 1]);
                int c = TopologyId(
                    data,
                    data.TriangleIndices[i + 2]);
                AddTopologyEdge(incidence, a, b);
                AddTopologyEdge(incidence, b, c);
                AddTopologyEdge(incidence, c, a);
            }

            openEdges = 0;
            nonManifoldEdges = 0;
            foreach (KeyValuePair<ulong, int> edge in incidence)
            {
                if (edge.Value == 1)
                {
                    openEdges++;
                }
                else if (edge.Value > 2)
                {
                    nonManifoldEdges++;
                }
            }
        }

        private static float MeasureM04BottomCenterThickness(
            FoldCanvasCompiledData data)
        {
            int provenance =
                data.GetPanel("bottom").VertexStart;
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    data.Vertices[i];
                if (vertex.ProvenanceId == provenance)
                {
                    positions.Add(vertex.Position);
                }
            }

            float maximum = 0f;
            for (int first = 0;
                first < positions.Count - 1;
                first++)
            {
                for (int second = first + 1;
                    second < positions.Count;
                    second++)
                {
                    maximum = Mathf.Max(
                        maximum,
                        Vector3.Distance(
                            positions[first],
                            positions[second]));
                }
            }

            return maximum;
        }
    }
}
