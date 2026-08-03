using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Tests
{
    public sealed class BootstrapEditorWorkflowTests
    {
        private const string TestOutputFolder = "Assets/FoldCanvasM00Tests";
        private const string SampleFolder = "Assets/FoldCanvasSamples";
        private const string SampleTexturePath = SampleFolder + "/BootstrapCanvas.png";
        private const string SampleAssetPath = SampleFolder + "/BootstrapFoldCanvas.asset";
        private const string M02TexturePath = SampleFolder + "/M02BoxCanvas.png";
        private const string M02AssetPath = SampleFolder + "/M02BoxFoldCanvas.asset";
        private const string M03TexturePath = SampleFolder + "/M03CupCanvas.png";
        private const string M03AssetPath = SampleFolder + "/M03CupFoldCanvas.asset";
        private const string M05TexturePath =
            SampleFolder + "/M05SphereCanvas.png";
        private const string M05AssetPath =
            SampleFolder + "/M05SphereGoldenFoldCanvas.asset";
        private const string M09TexturePath =
            SampleFolder + "/M09TopologyCanvas.png";
        private const string M09TorusAssetPath =
            SampleFolder + "/M09TorusFoldCanvas.asset";
        private const string M09HandleCupAssetPath =
            SampleFolder + "/M09HandleCupFoldCanvas.asset";

        private UnityEngine.Object previousSelection;

        [SetUp]
        public void SetUp()
        {
            previousSelection = Selection.activeObject;
            AssetDatabase.DeleteAsset(TestOutputFolder);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestOutputFolder);
            Selection.activeObject = previousSelection;
        }

        [Test]
        public void CreateBootstrapSample_TwiceKeepsTheSameAssetsAndCreatesNoObjects()
        {
            bool sampleFolderAlreadyExisted = AssetDatabase.IsValidFolder(SampleFolder);
            try
            {
                int objectCountBefore = Resources.FindObjectsOfTypeAll<GameObject>().Length;
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateBootstrapSample();

                string firstAssetGuid = AssetDatabase.AssetPathToGUID(SampleAssetPath);
                string firstTextureGuid = AssetDatabase.AssetPathToGUID(SampleTexturePath);
                string[] firstFolderGuids = FindSortedAssetGuids(SampleFolder);
                FoldCanvasAsset firstAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(SampleAssetPath);

                Assert.That(firstAsset, Is.Not.Null);
                Assert.That(firstAssetGuid, Is.Not.Empty);
                Assert.That(firstTextureGuid, Is.Not.Empty);
                Assert.That(firstAsset.Panels.Count, Is.EqualTo(2));
                Assert.That(firstAsset.Panels[1].Id, Is.EqualTo("ellipse"));
                Assert.That(
                    firstAsset.Panels[1].PhysicalSize,
                    Is.EqualTo(new Vector2(1.1f, 0.7f)));

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateBootstrapSample();

                FoldCanvasAsset secondAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(SampleAssetPath);
                Assert.That(secondAsset, Is.SameAs(firstAsset));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(SampleAssetPath),
                    Is.EqualTo(firstAssetGuid));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(SampleTexturePath),
                    Is.EqualTo(firstTextureGuid));
                CollectionAssert.AreEqual(
                    firstFolderGuids,
                    FindSortedAssetGuids(SampleFolder));
                Assert.That(
                    Resources.FindObjectsOfTypeAll<GameObject>().Length,
                    Is.EqualTo(objectCountBefore));
            }
            finally
            {
                if (!sampleFolderAlreadyExisted)
                {
                    AssetDatabase.DeleteAsset(SampleFolder);
                }
            }
        }

        [Test]
        public void BakeMesh_TwiceUpdatesTheAssetWithoutChangingItsGuid()
        {
            FoldCanvasAsset source = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            source.name = "GuidStableBake";
            source.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));

            try
            {
                Assert.That(
                    FoldCanvas.Editor.FoldCanvasBaker.BakeMesh(
                        source,
                        TestOutputFolder + "/Generated",
                        out Mesh firstMesh,
                        out FoldCanvasCompileResult firstResult),
                    Is.True,
                    JoinDiagnostics(firstResult));

                string path = AssetDatabase.GetAssetPath(firstMesh);
                string guid = AssetDatabase.AssetPathToGUID(path);
                Assert.That(path, Is.EqualTo(
                    TestOutputFolder + "/Generated/GuidStableBake_Generated.asset"));
                Assert.That(guid, Is.Not.Empty);
                Assert.That(firstMesh.vertices[0].x, Is.EqualTo(-0.5f).Within(1e-6f));

                source.Panels[0].PhysicalSize = new Vector2(2f, 1f);
                Assert.That(
                    FoldCanvas.Editor.FoldCanvasBaker.BakeMesh(
                        source,
                        TestOutputFolder + "/Generated",
                        out Mesh secondMesh,
                        out FoldCanvasCompileResult secondResult),
                    Is.True,
                    JoinDiagnostics(secondResult));

                Assert.That(AssetDatabase.GetAssetPath(secondMesh), Is.EqualTo(path));
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
                Assert.That(secondMesh, Is.Not.Null);
                Assert.That(secondMesh.vertices[0].x, Is.EqualTo(-1f).Within(1e-6f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void CreateM02BoxSample_TwiceKeepsGuidsAndCompilesSixFaces()
        {
            bool sampleFolderAlreadyExisted = AssetDatabase.IsValidFolder(SampleFolder);
            try
            {
                int objectCountBefore = Resources.FindObjectsOfTypeAll<GameObject>().Length;
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM02BoxSample();

                string firstAssetGuid = AssetDatabase.AssetPathToGUID(M02AssetPath);
                string firstTextureGuid = AssetDatabase.AssetPathToGUID(M02TexturePath);
                FoldCanvasAsset firstAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M02AssetPath);
                Texture2D firstTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(M02TexturePath);
                Assert.That(firstAsset, Is.Not.Null);
                Assert.That(firstTexture, Is.Not.Null);
                Assert.That(firstAssetGuid, Is.Not.Empty);
                Assert.That(firstTextureGuid, Is.Not.Empty);
                Assert.That(firstAsset.Panels.Count, Is.EqualTo(6));
                Assert.That(firstAsset.Operations.Count, Is.EqualTo(10));
                Assert.That(firstTexture.width, Is.EqualTo(384));
                Assert.That(firstTexture.height, Is.EqualTo(256));

                FoldCanvasCompileResult firstCompile =
                    FoldCanvasCompiler.Compile(firstAsset);
                Assert.That(
                    firstCompile.Success,
                    Is.True,
                    JoinDiagnostics(firstCompile));
                Assert.That(firstCompile.Mesh.vertexCount, Is.EqualTo(24));
                Assert.That(firstCompile.Mesh.triangles.Length / 3, Is.EqualTo(12));
                UnityEngine.Object.DestroyImmediate(firstCompile.Mesh);

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM02BoxSample();

                FoldCanvasAsset secondAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M02AssetPath);
                Assert.That(secondAsset, Is.SameAs(firstAsset));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(M02AssetPath),
                    Is.EqualTo(firstAssetGuid));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(M02TexturePath),
                    Is.EqualTo(firstTextureGuid));
                Assert.That(
                    Resources.FindObjectsOfTypeAll<GameObject>().Length,
                    Is.EqualTo(objectCountBefore));
            }
            finally
            {
                if (!sampleFolderAlreadyExisted)
                {
                    AssetDatabase.DeleteAsset(SampleFolder);
                }
            }
        }

        [Test]
        public void CreateM03CupSample_TwiceKeepsGuidsAndCompilesAlignedSurfaces()
        {
            bool sampleFolderAlreadyExisted = AssetDatabase.IsValidFolder(SampleFolder);
            try
            {
                int objectCountBefore =
                    Resources.FindObjectsOfTypeAll<GameObject>().Length;
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupSample();

                string firstAssetGuid =
                    AssetDatabase.AssetPathToGUID(M03AssetPath);
                string firstTextureGuid =
                    AssetDatabase.AssetPathToGUID(M03TexturePath);
                FoldCanvasAsset firstAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M03AssetPath);
                Texture2D firstTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(M03TexturePath);
                Assert.That(firstAsset, Is.Not.Null);
                Assert.That(firstTexture, Is.Not.Null);
                Assert.That(firstAssetGuid, Is.Not.Empty);
                Assert.That(firstTextureGuid, Is.Not.Empty);
                Assert.That(firstTexture.width, Is.EqualTo(1024));
                Assert.That(firstTexture.height, Is.EqualTo(1024));
                Assert.That(firstAsset.Panels.Count, Is.EqualTo(2));
                Assert.That(firstAsset.Operations.Count, Is.EqualTo(3));
                Assert.That(firstAsset.Seams.Count, Is.EqualTo(2));
                Assert.That(firstAsset.Panels[0].Id, Is.EqualTo("wall"));
                Assert.That(firstAsset.Panels[1].Id, Is.EqualTo("bottom"));
                Assert.That(
                    firstAsset.Operations[0],
                    Is.TypeOf<RollOperationDefinition>());
                Assert.That(
                    ((RollOperationDefinition)firstAsset.Operations[0])
                        .StartAngleDegrees,
                    Is.EqualTo(180f));
                Assert.That(
                    firstAsset.Operations[2],
                    Is.TypeOf<StitchOperationDefinition>());
                Assert.That(firstAsset.Seams[0].Id, Is.EqualTo("close-wall"));
                Assert.That(
                    firstAsset.Seams[1].Id,
                    Is.EqualTo("attach-bottom"));

                FoldCanvasCompileResult firstCompile =
                    FoldCanvasCompiler.Compile(firstAsset);
                Assert.That(
                    firstCompile.Success,
                    Is.True,
                    JoinDiagnostics(firstCompile));
                Assert.That(firstCompile.Mesh.vertexCount, Is.EqualTo(1358));
                Assert.That(
                    firstCompile.Mesh.triangles.Length / 3,
                    Is.EqualTo(2496));
                Assert.That(
                    firstCompile.CompiledData.TopologyVertexCount,
                    Is.EqualTo(1281));
                UnityEngine.Object.DestroyImmediate(firstCompile.Mesh);

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupSample();

                FoldCanvasAsset secondAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(M03AssetPath);
                Assert.That(secondAsset, Is.SameAs(firstAsset));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(M03AssetPath),
                    Is.EqualTo(firstAssetGuid));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(M03TexturePath),
                    Is.EqualTo(firstTextureGuid));
                Assert.That(
                    Resources.FindObjectsOfTypeAll<GameObject>().Length,
                    Is.EqualTo(objectCountBefore));
            }
            finally
            {
                if (!sampleFolderAlreadyExisted)
                {
                    AssetDatabase.DeleteAsset(SampleFolder);
                }
            }
        }

        [Test]
        public void M03InteractivePreviewShader_DefaultsToOpaqueDoubleSidedRendering()
        {
            Shader shader =
                Shader.Find("FoldCanvas/Two-Sided Unlit Texture");

            Assert.That(shader, Is.Not.Null);
            Material material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_MainTex"), Is.True);
                Assert.That(material.HasProperty("_Cull"), Is.True);
                Assert.That(
                    material.GetFloat("_Cull"),
                    Is.EqualTo((float)CullMode.Off));
                Assert.That(
                    material.FindPass("Unlit"),
                    Is.GreaterThanOrEqualTo(0));
                string shaderPath = AssetDatabase.GetAssetPath(shader);
                string shaderSource = File.ReadAllText(shaderPath);
                Assert.That(shaderSource, Does.Contain("VFACE"));
                Assert.That(
                    shaderSource,
                    Does.Contain("1.0 - readableUv.x"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void CreateM03CupProof_TwiceKeepsOneOwnedPreviewCamera()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();
                GameObject firstRoot =
                    FindM03PreviewRoot(scene);
                Camera[] firstCameras =
                    firstRoot.GetComponentsInChildren<Camera>(true);
                Assert.That(firstCameras.Length, Is.EqualTo(1));
                int firstRootId = firstRoot.GetInstanceID();
                int firstCameraId = firstCameras[0].GetInstanceID();

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();

                GameObject secondRoot =
                    FindM03PreviewRoot(scene);
                Camera[] secondCameras =
                    secondRoot.GetComponentsInChildren<Camera>(true);
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(firstRootId));
                Assert.That(secondRoot.tag, Is.EqualTo("EditorOnly"));
                Assert.That(secondCameras.Length, Is.EqualTo(1));
                Assert.That(
                    secondCameras[0].GetInstanceID(),
                    Is.EqualTo(firstCameraId));
                Assert.That(
                    secondCameras[0].gameObject.name,
                    Is.EqualTo("FoldCanvas M03 Preview Camera"));
                Assert.That(
                    secondCameras[0].CompareTag("MainCamera"),
                    Is.False);
            });
        }

        [Test]
        public void CreateM03CupProof_TwiceKeepsStableObjectCount()
        {
            InTemporaryScene(scene =>
            {
                int countBefore = CountSceneObjects(scene);

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();
                int countAfterFirst = CountSceneObjects(scene);
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();
                int countAfterSecond = CountSceneObjects(scene);

                Assert.That(countAfterFirst, Is.EqualTo(countBefore + 4));
                Assert.That(countAfterSecond, Is.EqualTo(countAfterFirst));
                GameObject root =
                    FindM03PreviewRoot(scene);
                Assert.That(
                    root.transform.Find("FoldCanvas M03 Cup Proof"),
                    Is.Not.Null);
                Assert.That(
                    root.transform.Find("FoldCanvas M03 Source Canvas"),
                    Is.Not.Null);
                Assert.That(
                    root.transform.Find("FoldCanvas M03 Preview Camera"),
                    Is.Not.Null);
            });
        }

        [Test]
        public void CreateM03CupProof_DoesNotModifyExistingMainCamera()
        {
            InTemporaryScene(scene =>
            {
                GameObject userCameraObject = new GameObject(
                    "User Main Camera",
                    typeof(Camera));
                userCameraObject.tag = "MainCamera";
                userCameraObject.transform.SetPositionAndRotation(
                    new Vector3(7f, 8f, 9f),
                    Quaternion.Euler(12f, 34f, 56f));
                userCameraObject.transform.localScale =
                    new Vector3(1.2f, 1.3f, 1.4f);
                Camera userCamera = userCameraObject.GetComponent<Camera>();
                userCamera.orthographic = false;
                userCamera.fieldOfView = 47f;
                userCamera.nearClipPlane = 0.33f;
                userCamera.farClipPlane = 777f;
                userCamera.clearFlags = CameraClearFlags.Depth;
                userCamera.backgroundColor =
                    new Color(0.2f, 0.3f, 0.4f, 0.5f);
                userCamera.depth = -7f;
                Vector3 position = userCameraObject.transform.position;
                Quaternion rotation = userCameraObject.transform.rotation;
                Vector3 scale = userCameraObject.transform.localScale;
                bool orthographic = userCamera.orthographic;
                float fieldOfView = userCamera.fieldOfView;
                float near = userCamera.nearClipPlane;
                float far = userCamera.farClipPlane;
                CameraClearFlags clearFlags = userCamera.clearFlags;
                Color background = userCamera.backgroundColor;
                float depth = userCamera.depth;
                string serializedCamera =
                    EditorJsonUtility.ToJson(userCamera);
                string serializedTransform =
                    EditorJsonUtility.ToJson(userCameraObject.transform);

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();

                Assert.That(userCameraObject.transform.position, Is.EqualTo(position));
                Assert.That(userCameraObject.transform.rotation, Is.EqualTo(rotation));
                Assert.That(userCameraObject.transform.localScale, Is.EqualTo(scale));
                Assert.That(userCamera.orthographic, Is.EqualTo(orthographic));
                Assert.That(userCamera.fieldOfView, Is.EqualTo(fieldOfView));
                Assert.That(userCamera.nearClipPlane, Is.EqualTo(near));
                Assert.That(userCamera.farClipPlane, Is.EqualTo(far));
                Assert.That(userCamera.clearFlags, Is.EqualTo(clearFlags));
                Assert.That(userCamera.backgroundColor, Is.EqualTo(background));
                Assert.That(userCamera.depth, Is.EqualTo(depth));
                Assert.That(userCameraObject.CompareTag("MainCamera"), Is.True);
                Assert.That(
                    EditorJsonUtility.ToJson(userCamera),
                    Is.EqualTo(serializedCamera));
                Assert.That(
                    EditorJsonUtility.ToJson(userCameraObject.transform),
                    Is.EqualTo(serializedTransform));
            });
        }

        [Test]
        public void CreateM03CupProof_ReusesInactiveOwnedObjects()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();
                GameObject firstRoot =
                    FindM03PreviewRoot(scene);
                GameObject firstCup =
                    firstRoot.transform.Find("FoldCanvas M03 Cup Proof").gameObject;
                GameObject firstCanvas =
                    firstRoot.transform.Find("FoldCanvas M03 Source Canvas").gameObject;
                GameObject firstCamera =
                    firstRoot.transform.Find("FoldCanvas M03 Preview Camera").gameObject;
                int rootId = firstRoot.GetInstanceID();
                int cupId = firstCup.GetInstanceID();
                int canvasId = firstCanvas.GetInstanceID();
                int cameraId = firstCamera.GetInstanceID();
                firstCup.SetActive(false);
                firstCanvas.SetActive(false);
                firstCamera.SetActive(false);
                firstRoot.SetActive(false);

                FoldCanvas.Editor.FoldCanvasSampleCreator.CreateM03CupProof();

                GameObject secondRoot =
                    FindM03PreviewRoot(scene);
                GameObject secondCup =
                    secondRoot.transform.Find("FoldCanvas M03 Cup Proof").gameObject;
                GameObject secondCanvas =
                    secondRoot.transform.Find("FoldCanvas M03 Source Canvas").gameObject;
                GameObject secondCamera =
                    secondRoot.transform.Find("FoldCanvas M03 Preview Camera").gameObject;
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(rootId));
                Assert.That(secondCup.GetInstanceID(), Is.EqualTo(cupId));
                Assert.That(secondCanvas.GetInstanceID(), Is.EqualTo(canvasId));
                Assert.That(secondCamera.GetInstanceID(), Is.EqualTo(cameraId));
                Assert.That(secondRoot.activeSelf, Is.True);
                Assert.That(secondCup.activeSelf, Is.True);
                Assert.That(secondCanvas.activeSelf, Is.True);
                Assert.That(secondCamera.activeSelf, Is.True);
            });
        }

        [Test]
        public void CreateM04ProductionCupProof_ReusesOwnedFourViewHierarchy()
        {
            InTemporaryScene(scene =>
            {
                int countBefore = CountSceneObjects(scene);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM04ProductionCupProof();
                GameObject firstRoot = FindM04PreviewRoot(scene);
                int countAfterFirst = CountSceneObjects(scene);
                Camera[] firstCameras =
                    firstRoot.GetComponentsInChildren<Camera>(true);
                Assert.That(countAfterFirst, Is.EqualTo(countBefore + 8));
                Assert.That(firstCameras.Length, Is.EqualTo(4));

                int rootId = firstRoot.GetInstanceID();
                int[] cameraIds = new int[firstCameras.Length];
                for (int i = 0; i < firstCameras.Length; i++)
                {
                    cameraIds[i] = firstCameras[i].GetInstanceID();
                }

                firstRoot.SetActive(false);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM04ProductionCupProof();

                GameObject secondRoot = FindM04PreviewRoot(scene);
                Camera[] secondCameras =
                    secondRoot.GetComponentsInChildren<Camera>(true);
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(rootId));
                Assert.That(secondRoot.activeSelf, Is.True);
                Assert.That(secondRoot.tag, Is.EqualTo("EditorOnly"));
                Assert.That(
                    CountSceneObjects(scene),
                    Is.EqualTo(countAfterFirst));
                Assert.That(secondCameras.Length, Is.EqualTo(4));
                for (int i = 0; i < secondCameras.Length; i++)
                {
                    Assert.That(
                        secondCameras[i].GetInstanceID(),
                        Is.EqualTo(cameraIds[i]));
                }

                Camera exterior =
                    secondRoot.transform
                        .Find("FoldCanvas M04 Exterior Camera")
                        .GetComponent<Camera>();
                Assert.That(exterior.enabled, Is.True);
                Assert.That(
                    exterior.transform.localPosition.y,
                    Is.GreaterThan(0f));
                Assert.That(
                    secondRoot.transform
                        .Find("FoldCanvas M04 Exact Side Camera")
                        .GetComponent<Camera>().enabled,
                    Is.False);
                Assert.That(
                    secondRoot.transform
                        .Find("FoldCanvas M04 Interior Camera")
                        .GetComponent<Camera>().enabled,
                    Is.False);
                Assert.That(
                    secondRoot.transform
                        .Find("FoldCanvas M04 Underside Camera")
                        .GetComponent<Camera>().enabled,
                    Is.False);
            });
        }

        [Test]
        public void CreateM04ProductionCupProof_DoesNotModifyMainCamera()
        {
            InTemporaryScene(scene =>
            {
                GameObject cameraObject =
                    new GameObject("User Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(7f, 8f, 9f),
                    Quaternion.Euler(12f, 34f, 56f));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 47f;
                camera.nearClipPlane = 0.33f;
                camera.farClipPlane = 777f;
                camera.clearFlags = CameraClearFlags.Depth;
                camera.backgroundColor =
                    new Color(0.2f, 0.3f, 0.4f, 0.5f);
                camera.depth = -7f;
                string serializedCamera =
                    EditorJsonUtility.ToJson(camera);
                string serializedTransform =
                    EditorJsonUtility.ToJson(cameraObject.transform);

                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM04ProductionCupProof();
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM04ProductionCupProof();

                Assert.That(
                    EditorJsonUtility.ToJson(camera),
                    Is.EqualTo(serializedCamera));
                Assert.That(
                    EditorJsonUtility.ToJson(cameraObject.transform),
                    Is.EqualTo(serializedTransform));
                Assert.That(cameraObject.CompareTag("MainCamera"), Is.True);
            });
        }

        [Test]
        public void ClosedVolumeProof_CreatesSolidWireframeSectionAndCornerObjects()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM041ClosedVolumeCupProof();
                GameObject root =
                    FindM041PreviewRoot(scene);
                Assert.That(root.tag, Is.EqualTo("EditorOnly"));

                Transform sourceCanvas = root.transform.Find(
                    "FoldCanvas M04.1 2D Source Canvas");
                Transform solid = root.transform.Find(
                    "FoldCanvas M04.1 Cup ClosedVolume Solid");
                Transform wireframe = root.transform.Find(
                    "FoldCanvas M04.1 Cup Logical Wireframe");
                Transform section = root.transform.Find(
                    "FoldCanvas M04.1 Cup Section");
                Transform sectionLines = root.transform.Find(
                    "FoldCanvas M04.1 Section Lines");
                Transform outerCorner = root.transform.Find(
                    "FoldCanvas M04.1 OuterCorner");
                Transform innerCorner = root.transform.Find(
                    "FoldCanvas M04.1 InnerCorner");
                Assert.That(sourceCanvas, Is.Not.Null);
                Assert.That(solid, Is.Not.Null);
                Assert.That(wireframe, Is.Not.Null);
                Assert.That(section, Is.Not.Null);
                Assert.That(sectionLines, Is.Not.Null);
                Assert.That(outerCorner, Is.Not.Null);
                Assert.That(innerCorner, Is.Not.Null);

                Assert.That(
                    sourceCanvas.GetComponent<MeshRenderer>()
                        .sharedMaterial.GetTexture("_MainTex"),
                    Is.Not.Null);
                AssertTextureFree(solid);
                AssertTextureFree(wireframe);
                AssertTextureFree(section);
                AssertTextureFree(sectionLines);
                AssertTextureFree(outerCorner);
                AssertTextureFree(innerCorner);
                AssertLineMesh(wireframe);
                AssertLineMesh(sectionLines);
                AssertLineMesh(outerCorner);
                AssertLineMesh(innerCorner);
                Assert.That(
                    section.GetComponent<MeshRenderer>()
                        .sharedMaterial.shader.name,
                    Is.EqualTo(
                        "FoldCanvas/One-Sided Section Solid"));

                Camera[] cameras =
                    root.GetComponentsInChildren<Camera>(true);
                Assert.That(cameras.Length, Is.EqualTo(3));
                int enabledCameras = 0;
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i].enabled)
                    {
                        enabledCameras++;
                        Assert.That(
                            cameras[i].gameObject.name,
                            Is.EqualTo(
                                "FoldCanvas M04.1 Overview Camera"));
                    }
                }

                Assert.That(enabledCameras, Is.EqualTo(1));
            });
        }

        [Test]
        public void ClosedVolumeProof_ReusesOwnedHierarchy()
        {
            InTemporaryScene(scene =>
            {
                int countBefore = CountSceneObjects(scene);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM041ClosedVolumeCupProof();
                GameObject firstRoot =
                    FindM041PreviewRoot(scene);
                int countAfterFirst = CountSceneObjects(scene);
                Assert.That(
                    countAfterFirst,
                    Is.EqualTo(countBefore + 11));
                int rootId = firstRoot.GetInstanceID();
                int childCount = firstRoot.transform.childCount;
                firstRoot.SetActive(false);

                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM041ClosedVolumeCupProof();

                GameObject secondRoot =
                    FindM041PreviewRoot(scene);
                Assert.That(
                    secondRoot.GetInstanceID(),
                    Is.EqualTo(rootId));
                Assert.That(secondRoot.activeSelf, Is.True);
                Assert.That(
                    secondRoot.transform.childCount,
                    Is.EqualTo(childCount));
                Assert.That(
                    CountSceneObjects(scene),
                    Is.EqualTo(countAfterFirst));
                Assert.That(
                    secondRoot
                        .GetComponentsInChildren<Camera>(true)
                        .Length,
                    Is.EqualTo(3));
            });
        }

        [Test]
        public void CreateM05SphereSample_TwiceKeepsGuidsAndCompilesClosedSphere()
        {
            bool sampleFolderAlreadyExisted =
                AssetDatabase.IsValidFolder(SampleFolder);
            try
            {
                int objectCountBefore =
                    Resources.FindObjectsOfTypeAll<GameObject>().Length;
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereSample();

                string firstAssetGuid =
                    AssetDatabase.AssetPathToGUID(M05AssetPath);
                string firstTextureGuid =
                    AssetDatabase.AssetPathToGUID(M05TexturePath);
                FoldCanvasAsset firstAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                        M05AssetPath);
                Texture2D firstTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        M05TexturePath);
                Assert.That(firstAsset, Is.Not.Null);
                Assert.That(firstTexture, Is.Not.Null);
                Assert.That(firstAssetGuid, Is.Not.Empty);
                Assert.That(firstTextureGuid, Is.Not.Empty);
                Assert.That(firstTexture.width, Is.EqualTo(2048));
                Assert.That(firstTexture.height, Is.EqualTo(1024));
                Assert.That(firstAsset.Panels.Count, Is.EqualTo(8));
                Assert.That(firstAsset.Seams.Count, Is.EqualTo(8));
                Assert.That(firstAsset.Operations.Count, Is.EqualTo(9));
                Assert.That(
                    firstAsset.Operations[0],
                    Is.TypeOf<SphericalWrapOperationDefinition>());
                Assert.That(
                    firstAsset.Operations[8],
                    Is.TypeOf<StitchOperationDefinition>());

                FoldCanvasCompileResult firstCompile =
                    FoldCanvasCompiler.Compile(firstAsset);
                Assert.That(
                    firstCompile.Success,
                    Is.True,
                    JoinDiagnostics(firstCompile));
                Assert.That(firstCompile.SphereReport, Is.Not.Null);
                Assert.That(
                    firstCompile.SphereReport.IsClosedSphere,
                    Is.True);
                UnityEngine.Object.DestroyImmediate(firstCompile.Mesh);

                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereSample();

                FoldCanvasAsset secondAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                        M05AssetPath);
                Assert.That(secondAsset, Is.SameAs(firstAsset));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(M05AssetPath),
                    Is.EqualTo(firstAssetGuid));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(M05TexturePath),
                    Is.EqualTo(firstTextureGuid));
                Assert.That(
                    Resources.FindObjectsOfTypeAll<GameObject>().Length,
                    Is.EqualTo(objectCountBefore));
            }
            finally
            {
                if (!sampleFolderAlreadyExisted)
                {
                    AssetDatabase.DeleteAsset(SampleFolder);
                }
            }
        }

        [Test]
        public void CreateM05SphereProof_CreatesOwnedDebugHierarchyAndClosedReport()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();
                GameObject root = FindM05PreviewRoot(scene);
                Assert.That(root.tag, Is.EqualTo("EditorOnly"));

                Transform source =
                    root.transform.Find("Source Canvas Preview");
                Transform generated =
                    root.transform.Find("Generated Sphere");
                Transform solid =
                    root.transform.Find("Solid Validation");
                Transform wireframe =
                    root.transform.Find("Wireframe Debug");
                Transform seams =
                    root.transform.Find("Seam Debug");
                Transform poles =
                    root.transform.Find("Pole Debug");
                Transform uvStretch =
                    root.transform.Find("UV Stretch Debug");
                Transform radiusError =
                    root.transform.Find("Radius Error Debug");
                Transform reportTransform =
                    root.transform.Find("Validation Report");
                Transform cameraTransform =
                    root.transform.Find("Preview Camera");

                Assert.That(source, Is.Not.Null);
                Assert.That(generated, Is.Not.Null);
                Assert.That(solid, Is.Not.Null);
                Assert.That(wireframe, Is.Not.Null);
                Assert.That(seams, Is.Not.Null);
                Assert.That(poles, Is.Not.Null);
                Assert.That(uvStretch, Is.Not.Null);
                Assert.That(radiusError, Is.Not.Null);
                Assert.That(reportTransform, Is.Not.Null);
                Assert.That(cameraTransform, Is.Not.Null);
                AssertTextureFree(solid);
                AssertLineMesh(wireframe);
                AssertLineMesh(seams);
                AssertLineMesh(poles);

                TextMesh reportText =
                    reportTransform.GetComponent<TextMesh>();
                Assert.That(reportText, Is.Not.Null);
                Assert.That(
                    reportText.text,
                    Does.Contain("CLOSED SPHERE: YES"));
                Assert.That(reportText.text, Does.Contain("PANELS 8"));
                Assert.That(reportText.text, Does.Contain("OPEN 0"));
                Assert.That(
                    reportText.text,
                    Does.Contain("NON-MANIFOLD 0"));
                Assert.That(reportText.text, Does.Contain("EULER 2"));
                Assert.That(reportText.text, Does.Contain("POLES 1/1"));

                FoldCanvasAsset proofAsset =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                        M05AssetPath);
                FoldCanvasCompileResult proofCompile =
                    FoldCanvasCompiler.Compile(proofAsset);
                Assert.That(
                    proofCompile.Success,
                    Is.True,
                    JoinDiagnostics(proofCompile));
                Assert.That(
                    proofCompile.SphereReport.IsClosedSphere,
                    Is.True);
                Assert.That(
                    proofCompile.SphereReport.SphericalPanelCount,
                    Is.EqualTo(8));
                Assert.That(
                    proofCompile.SphereReport.OpenEdgeCount,
                    Is.Zero);
                Assert.That(
                    proofCompile.SphereReport.NonManifoldEdgeCount,
                    Is.Zero);
                Assert.That(
                    proofCompile.SphereReport.EulerCharacteristic,
                    Is.EqualTo(2));
                Assert.That(
                    proofCompile.SphereReport.NorthPoleTopologyCount,
                    Is.EqualTo(1));
                Assert.That(
                    proofCompile.SphereReport.SouthPoleTopologyCount,
                    Is.EqualTo(1));
                Assert.That(
                    proofCompile.SphereReport.MaximumRadiusError,
                    Is.LessThanOrEqualTo(
                        proofCompile.SphereReport.RadiusTolerance));
                UnityEngine.Object.DestroyImmediate(proofCompile.Mesh);

                Camera[] cameras =
                    root.GetComponentsInChildren<Camera>(true);
                Assert.That(cameras.Length, Is.EqualTo(1));
                Assert.That(cameras[0].enabled, Is.True);
                Assert.That(cameras[0].CompareTag("MainCamera"), Is.False);
            });
        }

        [Test]
        public void CreateM05SphereProof_TwiceKeepsOwnedObjectCountAndCamera()
        {
            InTemporaryScene(scene =>
            {
                int countBefore = CountSceneObjects(scene);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();
                GameObject firstRoot = FindM05PreviewRoot(scene);
                Camera firstCamera =
                    firstRoot.GetComponentInChildren<Camera>(true);
                int rootId = firstRoot.GetInstanceID();
                int cameraId = firstCamera.GetInstanceID();
                int countAfterFirst = CountSceneObjects(scene);

                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();

                GameObject secondRoot = FindM05PreviewRoot(scene);
                Camera[] secondCameras =
                    secondRoot.GetComponentsInChildren<Camera>(true);
                Assert.That(countAfterFirst, Is.EqualTo(countBefore + 11));
                Assert.That(
                    CountSceneObjects(scene),
                    Is.EqualTo(countAfterFirst));
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(rootId));
                Assert.That(secondCameras.Length, Is.EqualTo(1));
                Assert.That(
                    secondCameras[0].GetInstanceID(),
                    Is.EqualTo(cameraId));
            });
        }

        [Test]
        public void CreateM05SphereProof_ReusesInactiveOwnedObjects()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();
                GameObject firstRoot = FindM05PreviewRoot(scene);
                int rootId = firstRoot.GetInstanceID();
                int childCount = firstRoot.transform.childCount;
                int[] childIds = new int[childCount];
                string[] childNames = new string[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = firstRoot.transform.GetChild(i);
                    childIds[i] = child.gameObject.GetInstanceID();
                    childNames[i] = child.name;
                    child.gameObject.SetActive(false);
                }

                firstRoot.SetActive(false);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();

                GameObject secondRoot = FindM05PreviewRoot(scene);
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(rootId));
                Assert.That(secondRoot.activeSelf, Is.True);
                Assert.That(
                    secondRoot.transform.childCount,
                    Is.EqualTo(childCount));
                for (int i = 0; i < childCount; i++)
                {
                    Transform child =
                        secondRoot.transform.Find(childNames[i]);
                    Assert.That(child, Is.Not.Null, childNames[i]);
                    Assert.That(
                        child.gameObject.GetInstanceID(),
                        Is.EqualTo(childIds[i]),
                        childNames[i]);
                    Assert.That(child.gameObject.activeSelf, Is.True);
                }
            });
        }

        [Test]
        public void CreateM05SphereProof_DoesNotModifyExistingMainCamera()
        {
            InTemporaryScene(scene =>
            {
                GameObject cameraObject =
                    new GameObject("User Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(7f, 8f, 9f),
                    Quaternion.Euler(12f, 34f, 56f));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 47f;
                camera.nearClipPlane = 0.33f;
                camera.farClipPlane = 777f;
                camera.clearFlags = CameraClearFlags.Depth;
                camera.backgroundColor =
                    new Color(0.2f, 0.3f, 0.4f, 0.5f);
                camera.depth = -7f;
                string serializedCamera =
                    EditorJsonUtility.ToJson(camera);
                string serializedTransform =
                    EditorJsonUtility.ToJson(cameraObject.transform);

                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM05SphereProof();

                Assert.That(
                    EditorJsonUtility.ToJson(camera),
                    Is.EqualTo(serializedCamera));
                Assert.That(
                    EditorJsonUtility.ToJson(cameraObject.transform),
                    Is.EqualTo(serializedTransform));
                Assert.That(cameraObject.CompareTag("MainCamera"), Is.True);
            });
        }

        [Test]
        public void CreateM09TopologySamples_TwiceKeepsGuidsAndCompilesProofs()
        {
            FoldCanvas.Editor.FoldCanvasSampleCreator
                .CreateM09TopologySamples();
            string textureGuid =
                AssetDatabase.AssetPathToGUID(M09TexturePath);
            string torusGuid =
                AssetDatabase.AssetPathToGUID(M09TorusAssetPath);
            string cupGuid =
                AssetDatabase.AssetPathToGUID(M09HandleCupAssetPath);
            FoldCanvasAsset torus =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M09TorusAssetPath);
            FoldCanvasAsset cup =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M09HandleCupAssetPath);
            Assert.That(textureGuid, Is.Not.Empty);
            Assert.That(torusGuid, Is.Not.Empty);
            Assert.That(cupGuid, Is.Not.Empty);
            Assert.That(
                torus.Operations[0],
                Is.TypeOf<ToroidalWrapOperationDefinition>());
            Assert.That(cup.Panels.Count, Is.EqualTo(3));
            Assert.That(cup.Seams[2].B.UseSpan, Is.True);

            FoldCanvasCompileResult torusCompile =
                FoldCanvasCompiler.Compile(torus);
            FoldCanvasCompileResult cupCompile =
                FoldCanvasCompiler.Compile(cup);
            Assert.That(
                torusCompile.Success,
                Is.True,
                JoinDiagnostics(torusCompile));
            Assert.That(
                torusCompile.GeometryValidationReport.OpenEdgeCount,
                Is.Zero);
            Assert.That(
                cupCompile.Success,
                Is.True,
                JoinDiagnostics(cupCompile));
            Assert.That(
                cupCompile.ClosedVolumeReport.IsSingleClosedVolume,
                Is.True);
            UnityEngine.Object.DestroyImmediate(torusCompile.Mesh);
            UnityEngine.Object.DestroyImmediate(cupCompile.Mesh);

            FoldCanvas.Editor.FoldCanvasSampleCreator
                .CreateM09TopologySamples();
            Assert.That(
                AssetDatabase.AssetPathToGUID(M09TexturePath),
                Is.EqualTo(textureGuid));
            Assert.That(
                AssetDatabase.AssetPathToGUID(M09TorusAssetPath),
                Is.EqualTo(torusGuid));
            Assert.That(
                AssetDatabase.AssetPathToGUID(M09HandleCupAssetPath),
                Is.EqualTo(cupGuid));
        }

        [Test]
        public void CreateM09TopologyProof_IsOwnedIdempotentAndReusesInactive()
        {
            InTemporaryScene(scene =>
            {
                GameObject userCameraObject =
                    new GameObject("User Main Camera", typeof(Camera));
                userCameraObject.tag = "MainCamera";
                userCameraObject.transform.SetPositionAndRotation(
                    new Vector3(7f, 8f, 9f),
                    Quaternion.Euler(12f, 34f, 56f));
                Camera userCamera =
                    userCameraObject.GetComponent<Camera>();
                userCamera.fieldOfView = 47f;
                string serializedCamera =
                    EditorJsonUtility.ToJson(userCamera);
                string serializedTransform =
                    EditorJsonUtility.ToJson(userCameraObject.transform);

                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM09TopologyProof();
                GameObject firstRoot = FindM09PreviewRoot(scene);
                Assert.That(firstRoot.tag, Is.EqualTo("EditorOnly"));
                Assert.That(
                    firstRoot.transform.Find("Torus Textured"),
                    Is.Not.Null);
                Assert.That(
                    firstRoot.transform.Find("Torus Solid"),
                    Is.Not.Null);
                AssertLineMesh(
                    firstRoot.transform.Find("Torus Wireframe"));
                Assert.That(
                    firstRoot.transform.Find("Handle Cup Textured"),
                    Is.Not.Null);
                Assert.That(
                    firstRoot.transform.Find("Handle Cup Solid"),
                    Is.Not.Null);
                AssertLineMesh(
                    firstRoot.transform.Find("Handle Cup Wireframe"));
                Assert.That(
                    firstRoot.transform.Find("Topology Report")
                        .GetComponent<TextMesh>().text,
                    Does.Contain("TORUS  Euler 0  open 0"));
                FoldCanvas.Editor.FoldCanvasSampleCreator.UseM09TorusView();
                Assert.That(
                    firstRoot.transform.Find("Torus Solid").gameObject
                        .activeSelf,
                    Is.True);
                Assert.That(
                    firstRoot.transform.Find("Handle Cup Solid").gameObject
                        .activeSelf,
                    Is.False);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .UseM09HandleCupView();
                Assert.That(
                    firstRoot.transform.Find("Torus Solid").gameObject
                        .activeSelf,
                    Is.False);
                Assert.That(
                    firstRoot.transform.Find("Handle Cup Solid").gameObject
                        .activeSelf,
                    Is.True);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .UseM09OverviewView();
                Assert.That(
                    firstRoot.transform.Find("Torus Solid").gameObject
                        .activeSelf,
                    Is.True);
                Assert.That(
                    firstRoot.transform.Find("Handle Cup Solid").gameObject
                        .activeSelf,
                    Is.True);
                Assert.That(
                    firstRoot.transform.Find("Topology Report").gameObject
                        .activeSelf,
                    Is.True);
                Camera[] firstCameras =
                    firstRoot.GetComponentsInChildren<Camera>(true);
                Assert.That(firstCameras.Length, Is.EqualTo(1));
                Assert.That(firstCameras[0].CompareTag("MainCamera"), Is.False);
                int rootId = firstRoot.GetInstanceID();
                int cameraId = firstCameras[0].GetInstanceID();
                int childCount = firstRoot.transform.childCount;
                Dictionary<string, int> childIds =
                    new Dictionary<string, int>();
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = firstRoot.transform.GetChild(i);
                    childIds.Add(child.name, child.gameObject.GetInstanceID());
                    child.gameObject.SetActive(false);
                }

                firstRoot.SetActive(false);
                int objectCount = CountSceneObjects(scene);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM09TopologyProof();
                GameObject secondRoot = FindM09PreviewRoot(scene);
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(rootId));
                Assert.That(secondRoot.activeSelf, Is.True);
                Assert.That(
                    CountSceneObjects(scene),
                    Is.EqualTo(objectCount));
                Assert.That(
                    secondRoot.transform.childCount,
                    Is.EqualTo(childCount));
                foreach (KeyValuePair<string, int> pair in childIds)
                {
                    Transform child = secondRoot.transform.Find(pair.Key);
                    Assert.That(child, Is.Not.Null, pair.Key);
                    Assert.That(
                        child.gameObject.GetInstanceID(),
                        Is.EqualTo(pair.Value),
                        pair.Key);
                    Assert.That(child.gameObject.activeSelf, Is.True, pair.Key);
                }

                Assert.That(
                    secondRoot.GetComponentInChildren<Camera>(true)
                        .GetInstanceID(),
                    Is.EqualTo(cameraId));
                Assert.That(
                    EditorJsonUtility.ToJson(userCamera),
                    Is.EqualTo(serializedCamera));
                Assert.That(
                    EditorJsonUtility.ToJson(userCameraObject.transform),
                    Is.EqualTo(serializedTransform));
            });
        }

        private static void InTemporaryScene(Action<Scene> assertion)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            bool createdTemporaryHost = false;
            if (string.IsNullOrEmpty(previousScene.path))
            {
                if (!Application.isBatchMode)
                {
                    Assert.That(
                        previousScene.isDirty ||
                        previousScene.GetRootGameObjects().Length > 0,
                        Is.False,
                        "Preview ownership tests will not replace an unsaved scene with content.");
                }

                if (!AssetDatabase.IsValidFolder(TestOutputFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "FoldCanvasM00Tests");
                }

                Assert.That(
                    EditorSceneManager.SaveScene(
                        previousScene,
                        TestOutputFolder + "/M03PreviewRunnerHost.unity"),
                    Is.True);
                createdTemporaryHost = true;
            }

            Scene testScene = default;
            try
            {
                testScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                if (SceneManager.GetActiveScene() != testScene)
                {
                    Assert.That(SceneManager.SetActiveScene(testScene), Is.True);
                }

                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(testScene));
                assertion(testScene);
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }

                if (testScene.IsValid() && testScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(testScene, true);
                }

                if (createdTemporaryHost)
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        private static GameObject FindM03PreviewRoot(Scene scene)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject match = null;
            int matches = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene &&
                    objects[i].name == "FoldCanvas M03 Preview Root" &&
                    objects[i].CompareTag("EditorOnly"))
                {
                    match = objects[i];
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
            return match;
        }

        private static GameObject FindM04PreviewRoot(Scene scene)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject match = null;
            int matches = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene &&
                    objects[i].name ==
                        "FoldCanvas M04 Preview Root" &&
                    objects[i].CompareTag("EditorOnly"))
                {
                    match = objects[i];
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
            return match;
        }

        private static GameObject FindM041PreviewRoot(Scene scene)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject match = null;
            int matches = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene &&
                    objects[i].name ==
                        "FoldCanvas M04.1 Closed Volume Preview Root" &&
                    objects[i].CompareTag("EditorOnly"))
                {
                    match = objects[i];
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
            return match;
        }

        private static GameObject FindM05PreviewRoot(Scene scene)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject match = null;
            int matches = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene &&
                    objects[i].name == "FoldCanvas Sphere Root" &&
                    objects[i].CompareTag("EditorOnly"))
                {
                    match = objects[i];
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
            return match;
        }

        private static GameObject FindM09PreviewRoot(Scene scene)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject match = null;
            int matches = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene &&
                    objects[i].name == "FoldCanvas M09 Topology Root" &&
                    objects[i].CompareTag("EditorOnly"))
                {
                    match = objects[i];
                    matches++;
                }
            }

            Assert.That(matches, Is.EqualTo(1));
            return match;
        }

        private static void AssertTextureFree(Transform proof)
        {
            Material material =
                proof.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(material, Is.Not.Null);
            if (material.HasProperty("_MainTex"))
            {
                Assert.That(
                    material.GetTexture("_MainTex"),
                    Is.Null);
            }
        }

        private static void AssertLineMesh(Transform proof)
        {
            Mesh mesh =
                proof.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.subMeshCount, Is.EqualTo(1));
            Assert.That(
                mesh.GetTopology(0),
                Is.EqualTo(MeshTopology.Lines));
            Assert.That(mesh.GetIndexCount(0), Is.GreaterThan(0));
            Assert.That(mesh.GetIndexCount(0) % 2, Is.EqualTo(0));
        }

        private static int CountSceneObjects(Scene scene)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene)
                {
                    count++;
                }
            }

            return count;
        }

        private static string[] FindSortedAssetGuids(string folder)
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            Array.Sort(guids, StringComparer.Ordinal);
            return guids;
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
