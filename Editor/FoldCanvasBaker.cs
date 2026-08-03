using System;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    public static class FoldCanvasBaker
    {
        public const string DefaultOutputFolder = "Assets/FoldCanvasGenerated";

        public static bool BakeMesh(
            FoldCanvasAsset source,
            string outputFolder,
            out Mesh savedMesh,
            out FoldCanvasCompileResult compileResult)
        {
            return BakeMesh(
                source,
                outputFolder,
                null,
                out savedMesh,
                out compileResult);
        }

        public static bool BakeMesh(
            FoldCanvasAsset source,
            string outputFolder,
            FoldCanvasOperationRegistry operationRegistry,
            out Mesh savedMesh,
            out FoldCanvasCompileResult compileResult)
        {
            savedMesh = null;
            compileResult = FoldCanvasCompiler.Compile(
                source,
                operationRegistry);
            if (!compileResult.Success)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(outputFolder) ||
                !outputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new ArgumentException("Output folder must be inside Assets.", nameof(outputFolder));
            }

            EnsureFolder(outputFolder);
            string safeName = MakeSafeFileName(string.IsNullOrWhiteSpace(source.name)
                ? "FoldCanvasAsset"
                : source.name);
            string path = $"{outputFolder}/{safeName}_Generated.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(compileResult.Mesh, path);
                savedMesh = compileResult.Mesh;
            }
            else
            {
                EditorUtility.CopySerialized(compileResult.Mesh, existing);
                UnityEngine.Object.DestroyImmediate(compileResult.Mesh);
                compileResult.Mesh = existing;
                EditorUtility.SetDirty(existing);
                savedMesh = existing;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = savedMesh;
            EditorGUIUtility.PingObject(savedMesh);
            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.Ordinal))
            {
                throw new ArgumentException("Folder must begin with Assets.", nameof(folderPath));
            }

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    continue;
                }

                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string MakeSafeFileName(string value)
        {
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                value = value.Replace(invalid[i], '_');
            }

            return value;
        }
    }
}
