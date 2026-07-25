using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    [CustomEditor(typeof(FoldCanvasAsset))]
    public sealed class FoldCanvasAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            FoldCanvasAsset asset = (FoldCanvasAsset)target;
            if (GUILayout.Button("Bake Mesh to Assets/FoldCanvasGenerated"))
            {
                if (!FoldCanvasBaker.BakeMesh(
                        asset,
                        FoldCanvasBaker.DefaultOutputFolder,
                        out _,
                        out FoldCanvasCompileResult result))
                {
                    LogDiagnostics(result, asset);
                    return;
                }

                LogDiagnostics(result, asset);
            }
        }

        private static void LogDiagnostics(FoldCanvasCompileResult result, Object context)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                switch (diagnostic.Severity)
                {
                    case FoldCanvasDiagnosticSeverity.Error:
                        Debug.LogError(diagnostic.ToString(), context);
                        break;
                    case FoldCanvasDiagnosticSeverity.Warning:
                        Debug.LogWarning(diagnostic.ToString(), context);
                        break;
                    default:
                        Debug.Log(diagnostic.ToString(), context);
                        break;
                }
            }
        }
    }
}
