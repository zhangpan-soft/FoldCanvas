using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Editor
{
    public static partial class FoldCanvasSampleCreator
    {
        private const string M041AssetPath =
            SampleFolder + "/M041ClosedVolumeCupFoldCanvas.asset";
        private const string M041PreviewRootObjectName =
            "FoldCanvas M04.1 Closed Volume Preview Root";
        private const string M041SourceCanvasObjectName =
            "FoldCanvas M04.1 2D Source Canvas";
        private const string M041SolidObjectName =
            "FoldCanvas M04.1 Cup ClosedVolume Solid";
        private const string M041WireframeObjectName =
            "FoldCanvas M04.1 Cup Logical Wireframe";
        private const string M041SectionObjectName =
            "FoldCanvas M04.1 Cup Section";
        private const string M041SectionLinesObjectName =
            "FoldCanvas M04.1 Section Lines";
        private const string M041OuterCornerObjectName =
            "FoldCanvas M04.1 OuterCorner";
        private const string M041InnerCornerObjectName =
            "FoldCanvas M04.1 InnerCorner";
        private const string M041OverviewCameraObjectName =
            "FoldCanvas M04.1 Overview Camera";
        private const string M041WireframeCameraObjectName =
            "FoldCanvas M04.1 Wireframe Camera";
        private const string M041SectionCameraObjectName =
            "FoldCanvas M04.1 Section Camera";
        private const string M041WireframeShaderName =
            "FoldCanvas/Topology Wireframe";
        private const string M041SectionShaderName =
            "FoldCanvas/One-Sided Section Solid";
        private const string M041WireframeMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeWireframe.asset";
        private const string M041SectionLinesMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeSectionLines.asset";
        private const string M041OuterCornerMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeOuterCorner.asset";
        private const string M041InnerCornerMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeInnerCorner.asset";
        private const string M041WireframeMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeWireframe.mat";
        private const string M041SectionMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeSection.mat";
        private const string M041SectionLinesMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeSectionLines.mat";
        private const string M041OuterCornerMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeOuterCorner.mat";
        private const string M041InnerCornerMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M041ClosedVolumeInnerCorner.mat";
        private const float M041SectionPlaneX = 0.003f;
        private const float M041SectionIntersectionTolerance = 1e-6f;

        [MenuItem("Tools/FoldCanvas/Create M04.1 Closed Volume Cup Sample")]
        public static void CreateM041ClosedVolumeCupSample()
        {
            EnsureSampleFolder();
            Texture2D appearance =
                CreateOrUpdateM04ProductionAppearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M041AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset =
                    ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M041ClosedVolumeCupFoldCanvas";
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
                AssetDatabase.CreateAsset(asset, M041AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log(
                $"FoldCanvas M04.1 Cup ClosedVolume sample " +
                $"{(created ? "created" : "updated")} at " +
                $"{M041AssetPath}.",
                asset);
        }

        [MenuItem("Tools/FoldCanvas/Create M04.1 Closed Volume Cup Proof")]
        public static void CreateM041ClosedVolumeCupProof()
        {
            CreateM041ClosedVolumeCupSample();
            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M041AssetPath);
            if (!FoldCanvasBaker.BakeMesh(
                    source,
                    FoldCanvasBaker.DefaultOutputFolder,
                    out Mesh cupMesh,
                    out FoldCanvasCompileResult result))
            {
                Debug.LogError(
                    "FoldCanvas M04.1 ClosedVolume proof failed to " +
                    "compile:\n" + JoinDiagnostics(result),
                    source);
                return;
            }

            FoldCanvasClosedVolumeReport volume =
                result.ClosedVolumeReport;
            if (volume == null ||
                !volume.IsSingleClosedVolume ||
                result.CompiledData.CornerSegments.Count == 0)
            {
                Debug.LogError(
                    "FoldCanvas M04.1 ClosedVolume proof rejected its " +
                    "generated geometry: the result must contain one " +
                    "closed non-zero volume and generated hard-corner " +
                    "segments.",
                    source);
                return;
            }

            Mesh wireframeMesh =
                CreateOrUpdateM041WireframeMesh(
                    result.CompiledData);
            Mesh sectionLinesMesh =
                CreateOrUpdateM041SectionLinesMesh(
                    result.CompiledData);
            Mesh outerCornerMesh =
                CreateOrUpdateM041CornerMesh(
                    result.CompiledData,
                    true);
            Mesh innerCornerMesh =
                CreateOrUpdateM041CornerMesh(
                    result.CompiledData,
                    false);
            Material solidMaterial =
                CreateOrUpdateM04SolidMaterial();
            Material wireframeMaterial =
                CreateOrUpdateM041Material(
                    M041WireframeMaterialPath,
                    "M041ClosedVolumeWireframe_Material",
                    M041WireframeShaderName,
                    new Color(0.28f, 0.93f, 1f, 1f));
            Material sectionMaterial =
                CreateOrUpdateM041Material(
                    M041SectionMaterialPath,
                    "M041ClosedVolumeSection_Material",
                    M041SectionShaderName,
                    new Color(0.94f, 0.66f, 0.24f, 1f));
            sectionMaterial.SetFloat(
                "_SectionPlaneX",
                M041SectionPlaneX);
            sectionMaterial.SetFloat(
                "_SectionBandWidth",
                0.0006f);
            EditorUtility.SetDirty(sectionMaterial);
            Material sectionLinesMaterial =
                CreateOrUpdateM041Material(
                    M041SectionLinesMaterialPath,
                    "M041ClosedVolumeSectionLines_Material",
                    M041WireframeShaderName,
                    Color.white);
            Material outerCornerMaterial =
                CreateOrUpdateM041Material(
                    M041OuterCornerMaterialPath,
                    "M041ClosedVolumeOuterCorner_Material",
                    M041WireframeShaderName,
                    new Color(1f, 0.36f, 0.08f, 1f));
            outerCornerMaterial.renderQueue = 4001;
            EditorUtility.SetDirty(outerCornerMaterial);
            Material innerCornerMaterial =
                CreateOrUpdateM041Material(
                    M041InnerCornerMaterialPath,
                    "M041ClosedVolumeInnerCorner_Material",
                    M041WireframeShaderName,
                    new Color(1f, 0.18f, 0.72f, 1f));
            innerCornerMaterial.renderQueue = 4002;
            EditorUtility.SetDirty(innerCornerMaterial);
            Material sourceCanvasMaterial =
                CreateOrUpdateProofMaterial(
                    M04TexturedMaterialPath,
                    "M04CupProduction_Material",
                    source.Appearance,
                    false);
            AssetDatabase.SaveAssets();

            DeactivateM03PreviewForM04();
            DeactivateM04PreviewForM041();
            GameObject previewRoot =
                GetOrCreateM041PreviewRoot();
            Quaternion cupRotation =
                Quaternion.Euler(0f, -90f, 0f);

            GameObject sourceCanvas = GetOrCreateM04MeshObject(
                previewRoot,
                M041SourceCanvasObjectName);
            ConfigureM03OwnedMeshObject(
                sourceCanvas,
                CreateOrUpdateM04CanvasMesh(),
                sourceCanvasMaterial,
                new Vector3(-0.285f, 0f, 0.04f),
                Quaternion.identity,
                new Vector3(0.14f, 0.14f, 0.14f));

            GameObject solidProof = GetOrCreateM04MeshObject(
                previewRoot,
                M041SolidObjectName);
            ConfigureM03OwnedMeshObject(
                solidProof,
                cupMesh,
                solidMaterial,
                new Vector3(-0.105f, 0f, 0f),
                cupRotation,
                Vector3.one);

            GameObject wireframeProof = GetOrCreateM04MeshObject(
                previewRoot,
                M041WireframeObjectName);
            ConfigureM03OwnedMeshObject(
                wireframeProof,
                wireframeMesh,
                wireframeMaterial,
                new Vector3(0.055f, 0f, 0f),
                cupRotation,
                Vector3.one);

            GameObject outerCorner = GetOrCreateM04MeshObject(
                previewRoot,
                M041OuterCornerObjectName);
            ConfigureM03OwnedMeshObject(
                outerCorner,
                outerCornerMesh,
                outerCornerMaterial,
                wireframeProof.transform.localPosition,
                cupRotation,
                Vector3.one);

            GameObject innerCorner = GetOrCreateM04MeshObject(
                previewRoot,
                M041InnerCornerObjectName);
            ConfigureM03OwnedMeshObject(
                innerCorner,
                innerCornerMesh,
                innerCornerMaterial,
                wireframeProof.transform.localPosition,
                cupRotation,
                Vector3.one);

            GameObject sectionProof = GetOrCreateM04MeshObject(
                previewRoot,
                M041SectionObjectName);
            ConfigureM03OwnedMeshObject(
                sectionProof,
                cupMesh,
                sectionMaterial,
                new Vector3(0.215f, 0f, 0f),
                cupRotation,
                Vector3.one);

            GameObject sectionLines = GetOrCreateM04MeshObject(
                previewRoot,
                M041SectionLinesObjectName);
            ConfigureM03OwnedMeshObject(
                sectionLines,
                sectionLinesMesh,
                sectionLinesMaterial,
                sectionProof.transform.localPosition,
                cupRotation,
                Vector3.one);

            Camera overview = ConfigureM041Camera(
                previewRoot,
                M041OverviewCameraObjectName,
                new Vector3(0.1f, 0.11f, -0.62f),
                new Vector3(-0.035f, 0f, 0f),
                0.18f,
                true);
            ConfigureM041Camera(
                previewRoot,
                M041WireframeCameraObjectName,
                new Vector3(0.055f, 0.07f, -0.5f),
                new Vector3(0.055f, 0f, 0f),
                0.08f,
                false);
            ConfigureM041Camera(
                previewRoot,
                M041SectionCameraObjectName,
                new Vector3(0.215f, 0f, -0.5f),
                new Vector3(0.215f, 0f, 0f),
                0.08f,
                false);

            Selection.activeGameObject = solidProof;
            EditorSceneManager.MarkSceneDirty(previewRoot.scene);
            ApplyM041SceneView(overview);
            Debug.Log(
                $"FoldCanvas M04.1 Cup ClosedVolume proof: " +
                $"{volume.ComponentCount} component, " +
                $"{volume.UniqueEdgeCount} unique logical edges, " +
                $"{volume.OpenEdgeCount} open, " +
                $"{volume.NonManifoldEdgeCount} non-manifold, " +
                $"{volume.OrientationConflictEdgeCount} orientation " +
                $"conflicts, |volume|={volume.TotalAbsoluteVolume:R} " +
                $"m^3, {result.CompiledData.CornerSegments.Count} " +
                $"paired hard-corner segments, " +
                $"{wireframeMesh.GetIndexCount(0) / 2} wire edges, " +
                $"{sectionLinesMesh.GetIndexCount(0) / 2} section " +
                "segments. Result materials are texture-free; the " +
                "visible canvas remains the authoritative 2D source.",
                solidProof);
        }

        [MenuItem("Tools/FoldCanvas/M04.1 View/Overview")]
        public static void UseM041OverviewView()
        {
            ActivateM041View(M041OverviewCameraObjectName);
        }

        [MenuItem("Tools/FoldCanvas/M04.1 View/Wireframe")]
        public static void UseM041WireframeView()
        {
            ActivateM041View(M041WireframeCameraObjectName);
        }

        [MenuItem("Tools/FoldCanvas/M04.1 View/Section")]
        public static void UseM041SectionView()
        {
            ActivateM041View(M041SectionCameraObjectName);
        }

        private static Material CreateOrUpdateM041Material(
            string path,
            string materialName,
            string shaderName,
            Color color)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new System.IO.InvalidDataException(
                    $"Required M04.1 proof shader '{shaderName}' " +
                    "was not imported.");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_Color", color);
            material.doubleSidedGI = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateOrUpdateM041WireframeMesh(
            FoldCanvasCompiledData data)
        {
            Dictionary<int, Vector3> positions =
                BuildM041TopologyPositions(data);
            HashSet<ulong> uniqueEdges = new HashSet<ulong>();
            for (int i = 0;
                i < data.TriangleIndices.Count;
                i += 3)
            {
                int a = data.Vertices[
                    data.TriangleIndices[i]].TopologyVertexId;
                int b = data.Vertices[
                    data.TriangleIndices[i + 1]].TopologyVertexId;
                int c = data.Vertices[
                    data.TriangleIndices[i + 2]].TopologyVertexId;
                uniqueEdges.Add(M041EdgeKey(a, b));
                uniqueEdges.Add(M041EdgeKey(b, c));
                uniqueEdges.Add(M041EdgeKey(c, a));
            }

            List<ulong> orderedEdges =
                new List<ulong>(uniqueEdges);
            orderedEdges.Sort();
            List<Vector3> vertices =
                new List<Vector3>(orderedEdges.Count * 2);
            for (int i = 0; i < orderedEdges.Count; i++)
            {
                int from = (int)(orderedEdges[i] >> 32);
                int to = (int)(orderedEdges[i] & uint.MaxValue);
                vertices.Add(positions[from]);
                vertices.Add(positions[to]);
            }

            return CreateOrUpdateM041LineMesh(
                M041WireframeMeshPath,
                "M041ClosedVolume_LogicalWireframe",
                vertices);
        }

        private static Mesh CreateOrUpdateM041CornerMesh(
            FoldCanvasCompiledData data,
            bool outer)
        {
            List<Vector3> vertices =
                new List<Vector3>(
                    data.CornerSegments.Count * 2);
            for (int i = 0; i < data.CornerSegments.Count; i++)
            {
                FoldCanvasCompiledCornerSegment segment =
                    data.CornerSegments[i];
                int from = outer
                    ? segment.OuterFromVertexIndex
                    : segment.InnerFromVertexIndex;
                int to = outer
                    ? segment.OuterToVertexIndex
                    : segment.InnerToVertexIndex;
                vertices.Add(data.Vertices[from].Position);
                vertices.Add(data.Vertices[to].Position);
            }

            return CreateOrUpdateM041LineMesh(
                outer
                    ? M041OuterCornerMeshPath
                    : M041InnerCornerMeshPath,
                outer
                    ? "M041ClosedVolume_OuterCorner"
                    : "M041ClosedVolume_InnerCorner",
                vertices);
        }

        private static Mesh CreateOrUpdateM041SectionLinesMesh(
            FoldCanvasCompiledData data)
        {
            List<Vector3> segments = new List<Vector3>();
            for (int i = 0;
                i < data.TriangleIndices.Count;
                i += 3)
            {
                Vector3 a = data.Vertices[
                    data.TriangleIndices[i]].Position;
                Vector3 b = data.Vertices[
                    data.TriangleIndices[i + 1]].Position;
                Vector3 c = data.Vertices[
                    data.TriangleIndices[i + 2]].Position;
                AddM041TriangleSection(a, b, c, segments);
            }

            return CreateOrUpdateM041LineMesh(
                M041SectionLinesMeshPath,
                "M041ClosedVolume_SectionLines",
                segments);
        }

        private static void AddM041TriangleSection(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            List<Vector3> segments)
        {
            float da = a.x - M041SectionPlaneX;
            float db = b.x - M041SectionPlaneX;
            float dc = c.x - M041SectionPlaneX;
            if (Mathf.Abs(da) <= M041SectionIntersectionTolerance &&
                Mathf.Abs(db) <= M041SectionIntersectionTolerance &&
                Mathf.Abs(dc) <= M041SectionIntersectionTolerance)
            {
                return;
            }

            List<Vector3> intersections = new List<Vector3>(3);
            AddM041EdgeSection(a, b, da, db, intersections);
            AddM041EdgeSection(b, c, db, dc, intersections);
            AddM041EdgeSection(c, a, dc, da, intersections);
            if (intersections.Count < 2)
            {
                return;
            }

            int bestFirst = 0;
            int bestSecond = 1;
            float bestDistanceSquared =
                (intersections[0] - intersections[1])
                .sqrMagnitude;
            for (int first = 0;
                first < intersections.Count - 1;
                first++)
            {
                for (int second = first + 1;
                    second < intersections.Count;
                    second++)
                {
                    float distanceSquared =
                        (intersections[first] -
                         intersections[second]).sqrMagnitude;
                    if (distanceSquared > bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestFirst = first;
                        bestSecond = second;
                    }
                }
            }

            if (bestDistanceSquared >
                M041SectionIntersectionTolerance *
                M041SectionIntersectionTolerance)
            {
                segments.Add(intersections[bestFirst]);
                segments.Add(intersections[bestSecond]);
            }
        }

        private static void AddM041EdgeSection(
            Vector3 from,
            Vector3 to,
            float fromDistance,
            float toDistance,
            List<Vector3> intersections)
        {
            bool fromOnPlane =
                Mathf.Abs(fromDistance) <=
                M041SectionIntersectionTolerance;
            bool toOnPlane =
                Mathf.Abs(toDistance) <=
                M041SectionIntersectionTolerance;
            if (fromOnPlane)
            {
                from.x = M041SectionPlaneX;
                AddM041UniqueIntersection(
                    intersections,
                    from);
            }

            if (toOnPlane)
            {
                to.x = M041SectionPlaneX;
                AddM041UniqueIntersection(
                    intersections,
                    to);
            }

            if (!fromOnPlane &&
                !toOnPlane &&
                ((fromDistance < 0f && toDistance > 0f) ||
                 (fromDistance > 0f && toDistance < 0f)))
            {
                float t =
                    fromDistance /
                    (fromDistance - toDistance);
                Vector3 intersection =
                    Vector3.LerpUnclamped(from, to, t);
                intersection.x = M041SectionPlaneX;
                AddM041UniqueIntersection(
                    intersections,
                    intersection);
            }
        }

        private static void AddM041UniqueIntersection(
            List<Vector3> intersections,
            Vector3 candidate)
        {
            float toleranceSquared =
                M041SectionIntersectionTolerance *
                M041SectionIntersectionTolerance;
            for (int i = 0; i < intersections.Count; i++)
            {
                if ((intersections[i] - candidate).sqrMagnitude <=
                    toleranceSquared)
                {
                    return;
                }
            }

            intersections.Add(candidate);
        }

        private static Mesh CreateOrUpdateM041LineMesh(
            string path,
            string meshName,
            List<Vector3> vertices)
        {
            Mesh mesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created)
            {
                mesh = new Mesh
                {
                    name = meshName
                };
            }
            else
            {
                mesh.Clear();
                mesh.name = meshName;
            }

            mesh.indexFormat = vertices.Count > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            int[] indices = new int[vertices.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            mesh.SetIndices(
                indices,
                MeshTopology.Lines,
                0,
                true);
            mesh.RecalculateBounds();
            if (created)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Dictionary<int, Vector3>
            BuildM041TopologyPositions(
                FoldCanvasCompiledData data)
        {
            Dictionary<int, Vector3> positions =
                new Dictionary<int, Vector3>();
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex =
                    data.Vertices[i];
                if (!positions.ContainsKey(vertex.TopologyVertexId))
                {
                    positions.Add(
                        vertex.TopologyVertexId,
                        vertex.Position);
                }
            }

            return positions;
        }

        private static ulong M041EdgeKey(int first, int second)
        {
            int minimum = Mathf.Min(first, second);
            int maximum = Mathf.Max(first, second);
            return ((ulong)(uint)minimum << 32) |
                (uint)maximum;
        }

        private static GameObject GetOrCreateM041PreviewRoot()
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
                    candidate.name != M041PreviewRootObjectName ||
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
                    M041PreviewRootObjectName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create FoldCanvas M04.1 Preview Root");
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject duplicate = candidates[i];
                if (duplicate == null ||
                    duplicate == root ||
                    EditorUtility.IsPersistent(duplicate) ||
                    duplicate.scene != activeScene ||
                    duplicate.name != M041PreviewRootObjectName ||
                    !duplicate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(duplicate);
            }

            Undo.RecordObject(
                root,
                "Update FoldCanvas M04.1 Preview Root");
            root.name = M041PreviewRootObjectName;
            root.tag = "EditorOnly";
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            Undo.RecordObject(
                root.transform,
                "Reset FoldCanvas M04.1 Preview Root");
            root.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static void DeactivateM04PreviewForM041()
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != activeScene ||
                    candidate.name != M04PreviewRootObjectName ||
                    !candidate.CompareTag("EditorOnly") ||
                    !candidate.activeSelf)
                {
                    continue;
                }

                Undo.RecordObject(
                    candidate,
                    "Hide FoldCanvas M04 Preview");
                candidate.SetActive(false);
            }
        }

        private static Camera ConfigureM041Camera(
            GameObject previewRoot,
            string objectName,
            Vector3 position,
            Vector3 target,
            float orthographicSize,
            bool enabled)
        {
            Camera camera = ConfigureM04Camera(
                previewRoot,
                objectName,
                position,
                target,
                enabled);
            Undo.RecordObject(
                camera,
                $"Update {objectName} Framing");
            camera.orthographicSize = orthographicSize;
            return camera;
        }

        private static void ActivateM041View(
            string cameraObjectName)
        {
            GameObject previewRoot =
                FindM041PreviewRootInActiveScene();
            if (previewRoot == null)
            {
                CreateM041ClosedVolumeCupProof();
                previewRoot =
                    FindM041PreviewRootInActiveScene();
            }

            if (previewRoot == null)
            {
                return;
            }

            if (!previewRoot.activeSelf)
            {
                Undo.RecordObject(
                    previewRoot,
                    "Show FoldCanvas M04.1 Preview");
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
                    "Switch FoldCanvas M04.1 Validation View");
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
                ApplyM041SceneView(selected);
                EditorSceneManager.MarkSceneDirty(
                    previewRoot.scene);
            }
        }

        private static GameObject
            FindM041PreviewRootInActiveScene()
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
                        M041PreviewRootObjectName &&
                    candidate.CompareTag("EditorOnly"))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ApplyM041SceneView(Camera camera)
        {
            SceneView sceneView =
                SceneView.lastActiveSceneView;
            if (sceneView == null || camera == null)
            {
                return;
            }

            sceneView.pivot = new Vector3(-0.035f, 0f, 0f);
            sceneView.rotation = camera.transform.rotation;
            sceneView.size = camera.orthographicSize * 1.15f;
            sceneView.Repaint();
        }
    }
}
