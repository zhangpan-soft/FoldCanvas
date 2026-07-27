using System;
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
