using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
                Assert.That(firstAsset.Operations.Count, Is.EqualTo(2));
                Assert.That(firstAsset.Seams, Is.Empty);
                Assert.That(firstAsset.Panels[0].Id, Is.EqualTo("wall"));
                Assert.That(firstAsset.Panels[1].Id, Is.EqualTo("bottom"));
                Assert.That(
                    firstAsset.Operations[0],
                    Is.TypeOf<RollOperationDefinition>());

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
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
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
