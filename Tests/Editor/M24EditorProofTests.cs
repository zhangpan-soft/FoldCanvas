using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Tests
{
    public sealed class M24EditorProofTests
    {
        private const string PreviewRootName =
            "FoldCanvas M24 Crease Preview Root";
        private const string TestOutputFolder =
            "Assets/FoldCanvasM24ProofTests";
        private const string SampleAssetPath =
            "Assets/FoldCanvasSamples/M24OffGridFoldCanvas.asset";

        [Test]
        public void CreateM24OffGridFoldProof_IsOwnedAndIdempotent()
        {
            Scene previous = SceneManager.GetActiveScene();
            bool createdTemporaryHost = false;
            if (string.IsNullOrEmpty(previous.path))
            {
                if (!AssetDatabase.IsValidFolder(TestOutputFolder))
                {
                    AssetDatabase.CreateFolder(
                        "Assets",
                        "FoldCanvasM24ProofTests");
                }

                Assert.That(
                    EditorSceneManager.SaveScene(
                        previous,
                        TestOutputFolder + "/M24ProofRunnerHost.unity"),
                    Is.True);
                createdTemporaryHost = true;
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                FoldCanvas.Editor.FoldCanvasM24SampleCreator.CreateProof();
                GameObject root = FindRoot(scene);
                int objectCount = CountSceneObjects(scene);
                Mesh firstFolded = root.transform
                    .Find("Folded Result")
                    .GetComponent<MeshFilter>()
                    .sharedMesh;
                Mesh firstFlat = root.transform
                    .Find("Flat Source Topology")
                    .GetComponent<MeshFilter>()
                    .sharedMesh;
                Mesh firstWireframe = root.transform
                    .Find("Flat Source Wireframe")
                    .GetComponent<MeshFilter>()
                    .sharedMesh;

                Assert.That(root.CompareTag("EditorOnly"), Is.True);
                Assert.That(firstFolded, Is.Not.Null);
                Assert.That(firstFlat, Is.Not.Null);
                Assert.That(firstFolded.vertexCount, Is.EqualTo(7));
                Assert.That(firstFolded.triangles.Length / 3, Is.EqualTo(6));
                Assert.That(firstFlat.vertexCount, Is.EqualTo(7));
                Assert.That(firstFlat.triangles.Length / 3, Is.EqualTo(6));
                Assert.That(firstWireframe, Is.Not.Null);
                Assert.That(
                    firstWireframe.GetTopology(0),
                    Is.EqualTo(MeshTopology.Lines));
                FoldCanvasAsset source =
                    AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                        SampleAssetPath);
                Assert.That(source, Is.Not.Null);
                Assert.That(source.Appearance, Is.Not.Null);
                Assert.That(
                    ResolveTexture(root.transform.Find("Folded Result")
                        .GetComponent<MeshRenderer>()
                        .sharedMaterial),
                    Is.SameAs(source.Appearance));
                Assert.That(
                    ResolveTexture(root.transform.Find("Flat Source Topology")
                        .GetComponent<MeshRenderer>()
                        .sharedMaterial),
                    Is.SameAs(source.Appearance));

                root.SetActive(false);
                FoldCanvas.Editor.FoldCanvasM24SampleCreator.CreateProof();
                GameObject reused = FindRoot(scene);

                Assert.That(reused, Is.SameAs(root));
                Assert.That(reused.activeSelf, Is.True);
                Assert.That(CountSceneObjects(scene), Is.EqualTo(objectCount));
                Assert.That(firstFolded == null, Is.True);
                Assert.That(firstFlat == null, Is.True);
                Assert.That(firstWireframe == null, Is.True);
                Assert.That(
                    reused.transform.Find("Folded Result")
                        .GetComponent<MeshRenderer>()
                        .sharedMaterial.doubleSidedGI,
                    Is.False);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }

                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (createdTemporaryHost)
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                    AssetDatabase.DeleteAsset(TestOutputFolder);
                }
            }
        }

        private static GameObject FindRoot(Scene scene)
        {
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject match = null;
            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].scene == scene &&
                    objects[i].name == PreviewRootName &&
                    objects[i].CompareTag("EditorOnly"))
                {
                    match = objects[i];
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1));
            return match;
        }

        private static Texture ResolveTexture(Material material)
        {
            if (material.HasProperty("_BaseMap"))
            {
                return material.GetTexture("_BaseMap");
            }

            return material.HasProperty("_MainTex")
                ? material.GetTexture("_MainTex")
                : null;
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
    }
}
