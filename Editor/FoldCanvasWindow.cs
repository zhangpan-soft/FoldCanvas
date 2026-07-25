using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoldCanvas.Editor
{
    public sealed class FoldCanvasWindow : EditorWindow
    {
        private ObjectField assetField;
        private Label summaryLabel;
        private ScrollView diagnosticList;
        private Mesh transientMesh;

        [MenuItem("Window/FoldCanvas/FoldCanvas")]
        public static void Open()
        {
            FoldCanvasWindow window = GetWindow<FoldCanvasWindow>();
            window.titleContent = new GUIContent("FoldCanvas");
            window.minSize = new Vector2(440f, 320f);
        }

        private void OnDisable()
        {
            DestroyTransientMesh();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingLeft = 10f;
            root.style.paddingRight = 10f;
            root.style.paddingTop = 10f;
            root.style.paddingBottom = 10f;

            Label title = new Label("FoldCanvas Bootstrap Compiler");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16f;
            title.style.marginBottom = 6f;
            root.Add(title);

            HelpBox help = new HelpBox(
                "This bootstrap window compiles planar panels and rigid transforms. Fold, Roll, Stitch, and Solidify are milestone work and intentionally fail with diagnostics until implemented.",
                HelpBoxMessageType.Info);
            help.style.marginBottom = 8f;
            root.Add(help);

            assetField = new ObjectField("Source Asset")
            {
                objectType = typeof(FoldCanvasAsset),
                allowSceneObjects = false,
                value = Selection.activeObject as FoldCanvasAsset
            };
            root.Add(assetField);

            VisualElement buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 8f;
            buttons.style.marginBottom = 8f;

            Button compileButton = new Button(CompileInMemory)
            {
                text = "Compile In Memory"
            };
            compileButton.style.flexGrow = 1f;
            buttons.Add(compileButton);

            Button bakeButton = new Button(BakeMesh)
            {
                text = "Bake Mesh Asset"
            };
            bakeButton.style.flexGrow = 1f;
            buttons.Add(bakeButton);

            root.Add(buttons);

            summaryLabel = new Label("No compile has run.");
            summaryLabel.style.marginBottom = 6f;
            root.Add(summaryLabel);

            diagnosticList = new ScrollView();
            diagnosticList.style.flexGrow = 1f;
            root.Add(diagnosticList);
        }

        private FoldCanvasAsset GetAsset()
        {
            return assetField?.value as FoldCanvasAsset;
        }

        private void CompileInMemory()
        {
            DestroyTransientMesh();
            FoldCanvasCompileResult result = FoldCanvasCompiler.Compile(GetAsset());
            transientMesh = result.Mesh;
            RenderResult(result, transientMesh == null ? null : transientMesh.name);
        }

        private void BakeMesh()
        {
            FoldCanvasAsset asset = GetAsset();
            if (!FoldCanvasBaker.BakeMesh(
                    asset,
                    FoldCanvasBaker.DefaultOutputFolder,
                    out Mesh savedMesh,
                    out FoldCanvasCompileResult result))
            {
                RenderResult(result, null);
                return;
            }

            RenderResult(result, AssetDatabase.GetAssetPath(savedMesh));
        }

        private void RenderResult(FoldCanvasCompileResult result, string output)
        {
            diagnosticList.Clear();
            if (result == null)
            {
                summaryLabel.text = "Compiler returned no result.";
                return;
            }

            if (result.Mesh != null)
            {
                summaryLabel.text =
                    $"Success={result.Success} | Vertices={result.Mesh.vertexCount} | Triangles={result.Mesh.triangles.Length / 3}" +
                    (string.IsNullOrWhiteSpace(output) ? string.Empty : $" | Output={output}");
            }
            else
            {
                summaryLabel.text = $"Success={result.Success} | No mesh generated.";
            }

            if (result.Diagnostics.Count == 0)
            {
                diagnosticList.Add(new Label("No diagnostics."));
                return;
            }

            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                FoldCanvasDiagnostic diagnostic = result.Diagnostics[i];
                HelpBoxMessageType type;
                switch (diagnostic.Severity)
                {
                    case FoldCanvasDiagnosticSeverity.Error:
                        type = HelpBoxMessageType.Error;
                        break;
                    case FoldCanvasDiagnosticSeverity.Warning:
                        type = HelpBoxMessageType.Warning;
                        break;
                    default:
                        type = HelpBoxMessageType.Info;
                        break;
                }

                diagnosticList.Add(new HelpBox(diagnostic.ToString(), type));
            }
        }

        private void DestroyTransientMesh()
        {
            if (transientMesh == null)
            {
                return;
            }

            DestroyImmediate(transientMesh);
            transientMesh = null;
        }
    }
}
