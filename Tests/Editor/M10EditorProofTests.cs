using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Tests
{
    public sealed class M10EditorProofTests
    {
        private const string TestOutputFolder =
            "Assets/FoldCanvasM10ProofTests";
        private const string SampleAssetPath =
            "Assets/FoldCanvasSamples/M10ExtensionFoldCanvas.asset";
        private const string SampleTexturePath =
            "Assets/FoldCanvasSamples/M10ExtensionCanvas.png";
        private const string ObjPath =
            "Assets/FoldCanvasGenerated/M10ExtensionProof.obj";
        private const string PreviewRootName =
            "FoldCanvas M10 Ecosystem Root";

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
        public void CreateM10EcosystemProof_UsesRegisteredOperationAndExportsObj()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM10EcosystemProof();

                GameObject root = FindPreviewRoot(scene);
                Assert.That(root.CompareTag("EditorOnly"), Is.True);
                Assert.That(
                    root.transform.Find("Registered Wave Textured"),
                    Is.Not.Null);
                Assert.That(
                    root.transform.Find("Registered Wave Solid"),
                    Is.Not.Null);
                Transform reportTransform =
                    root.transform.Find("Ecosystem Report");
                Assert.That(reportTransform, Is.Not.Null);
                Assert.That(
                    reportTransform.GetComponent<TextMesh>().text,
                    Does.Contain("dev.foldcanvas.samples.wave"));

                Camera[] cameras =
                    root.GetComponentsInChildren<Camera>(true);
                Assert.That(cameras.Length, Is.EqualTo(1));
                Assert.That(cameras[0].CompareTag("MainCamera"), Is.False);

                FoldCanvasAsset source =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                        SampleAssetPath);
                Texture2D texture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        SampleTexturePath);
                Assert.That(source, Is.Not.Null);
                Assert.That(texture, Is.Not.Null);

                FoldCanvasCompileResult unregistered =
                    FoldCanvasCompiler.Compile(source);
                Assert.That(unregistered.Success, Is.False);
                Assert.That(unregistered.Mesh, Is.Null);
                Assert.That(unregistered.Diagnostics.Count, Is.EqualTo(1));
                Assert.That(
                    unregistered.Diagnostics[0].Code,
                    Is.EqualTo(
                        FoldCanvasDiagnosticCodes
                            .UnregisteredExtensionOperation));

                FoldCanvasCompileResult registered =
                    FoldCanvasCompiler.Compile(
                        source,
                        FoldCanvas.Editor.FoldCanvasSampleCreator
                            .CreateM10Registry());
                try
                {
                    Assert.That(
                        registered.Success,
                        Is.True,
                        JoinDiagnostics(registered));
                    Assert.That(
                        registered.CompiledData.Vertices.Count,
                        Is.EqualTo(1225));
                    Assert.That(
                        registered.CompiledData.TriangleIndices.Count / 3,
                        Is.EqualTo(2304));
                }
                finally
                {
                    if (registered.Mesh != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            registered.Mesh);
                    }
                }

                Assert.That(File.Exists(Path.GetFullPath(ObjPath)), Is.True);
                string obj = File.ReadAllText(Path.GetFullPath(ObjPath));
                Assert.That(
                    obj,
                    Does.StartWith("# FoldCanvas deterministic OBJ\n"));
                Assert.That(obj, Does.Contain("\no M10ExtensionProof\n"));
                Assert.That(obj, Does.Not.Contain("\r"));
            });
        }

        [Test]
        public void CreateM10EcosystemProof_TwiceReusesInactiveOwnedHierarchy()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM10EcosystemProof();
                GameObject firstRoot = FindPreviewRoot(scene);
                int rootId = firstRoot.GetInstanceID();
                int objectCount = CountSceneObjects(scene);
                string sourceGuid =
                    AssetDatabase.AssetPathToGUID(SampleAssetPath);
                string textureGuid =
                    AssetDatabase.AssetPathToGUID(SampleTexturePath);
                string objBefore =
                    File.ReadAllText(Path.GetFullPath(ObjPath));
                Dictionary<string, int> childIds =
                    new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < firstRoot.transform.childCount; i++)
                {
                    Transform child = firstRoot.transform.GetChild(i);
                    childIds.Add(
                        child.name,
                        child.gameObject.GetInstanceID());
                    child.gameObject.SetActive(false);
                }

                firstRoot.SetActive(false);
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM10EcosystemProof();

                GameObject secondRoot = FindPreviewRoot(scene);
                Assert.That(secondRoot.GetInstanceID(), Is.EqualTo(rootId));
                Assert.That(secondRoot.activeSelf, Is.True);
                Assert.That(
                    CountSceneObjects(scene),
                    Is.EqualTo(objectCount));
                Assert.That(
                    secondRoot.transform.childCount,
                    Is.EqualTo(childIds.Count));
                foreach (KeyValuePair<string, int> childId in childIds)
                {
                    Transform child =
                        secondRoot.transform.Find(childId.Key);
                    Assert.That(child, Is.Not.Null, childId.Key);
                    Assert.That(
                        child.gameObject.GetInstanceID(),
                        Is.EqualTo(childId.Value),
                        childId.Key);
                    Assert.That(
                        child.gameObject.activeSelf,
                        Is.True,
                        childId.Key);
                }

                Assert.That(
                    secondRoot.GetComponentsInChildren<Camera>(true).Length,
                    Is.EqualTo(1));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(SampleAssetPath),
                    Is.EqualTo(sourceGuid));
                Assert.That(
                    AssetDatabase.AssetPathToGUID(SampleTexturePath),
                    Is.EqualTo(textureGuid));
                Assert.That(
                    File.ReadAllText(Path.GetFullPath(ObjPath)),
                    Is.EqualTo(objBefore));
            });
        }

        [Test]
        public void CreateM10EcosystemProof_OneSidedSurfacesFaceOwnedCamera()
        {
            InTemporaryScene(scene =>
            {
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM10EcosystemProof();
                GameObject root = FindPreviewRoot(scene);
                Camera camera =
                    root.GetComponentInChildren<Camera>(true);
                Assert.That(camera, Is.Not.Null);
                Assert.That(
                    camera.transform.localPosition.y,
                    Is.GreaterThan(0f));
                Assert.That(
                    camera.transform.localPosition.z,
                    Is.GreaterThan(0f));
                Assert.That(camera.orthographic, Is.False);

                AssertFacesCamera(
                    root.transform.Find("Registered Wave Textured"),
                    camera);
                AssertFacesCamera(
                    root.transform.Find("Registered Wave Solid"),
                    camera);

                Transform report =
                    root.transform.Find("Ecosystem Report");
                Vector3 reportToCamera =
                    (camera.transform.position - report.position)
                    .normalized;
                Assert.That(
                    Vector3.Dot(-report.forward, reportToCamera),
                    Is.GreaterThan(0.5f));
            });
        }

        [Test]
        public void CreateM10EcosystemProof_DoesNotModifyExistingMainCamera()
        {
            InTemporaryScene(scene =>
            {
                GameObject cameraObject =
                    new GameObject("User Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(7f, 8f, 9f),
                    Quaternion.Euler(12f, 34f, 56f));
                cameraObject.transform.localScale =
                    new Vector3(1.2f, 1.3f, 1.4f);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = false;
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
                    .CreateM10EcosystemProof();
                FoldCanvas.Editor.FoldCanvasSampleCreator
                    .CreateM10EcosystemProof();

                Assert.That(
                    EditorJsonUtility.ToJson(camera),
                    Is.EqualTo(serializedCamera));
                Assert.That(
                    EditorJsonUtility.ToJson(cameraObject.transform),
                    Is.EqualTo(serializedTransform));
                Assert.That(cameraObject.CompareTag("MainCamera"), Is.True);
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
                        "M10 proof ownership tests will not replace an unsaved scene with content.");
                }

                if (!AssetDatabase.IsValidFolder(TestOutputFolder))
                {
                    AssetDatabase.CreateFolder(
                        "Assets",
                        "FoldCanvasM10ProofTests");
                }

                Assert.That(
                    EditorSceneManager.SaveScene(
                        previousScene,
                        TestOutputFolder + "/M10ProofRunnerHost.unity"),
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
                    Assert.That(
                        SceneManager.SetActiveScene(testScene),
                        Is.True);
                }

                Assert.That(
                    SceneManager.GetActiveScene(),
                    Is.EqualTo(testScene));
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

        private static GameObject FindPreviewRoot(Scene scene)
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
                    objects[i].name == PreviewRootName &&
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
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
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

        private static void AssertFacesCamera(
            Transform proof,
            Camera camera)
        {
            Assert.That(proof, Is.Not.Null);
            Mesh mesh = proof.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh, Is.Not.Null);
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Assert.That(triangles.Length, Is.GreaterThanOrEqualTo(3));
            Vector3 first = proof.TransformPoint(
                vertices[triangles[0]]);
            Vector3 second = proof.TransformPoint(
                vertices[triangles[1]]);
            Vector3 third = proof.TransformPoint(
                vertices[triangles[2]]);
            Vector3 normal = Vector3.Cross(
                second - first,
                third - first).normalized;
            Vector3 center = (first + second + third) / 3f;
            Vector3 towardCamera =
                (camera.transform.position - center).normalized;
            Assert.That(
                Vector3.Dot(normal, towardCamera),
                Is.GreaterThan(0.05f),
                proof.name);
        }

        private static string JoinDiagnostics(
            FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }
    }
}
