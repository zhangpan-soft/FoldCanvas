using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoldCanvas.Editor
{
    internal sealed class FoldCanvasPreviewRenderer : IDisposable
    {
        private const string PreviewRootName =
            "FoldCanvas M06 Workspace Preview";
        private static readonly Color[] PanelPalette =
        {
            new Color(0.18f, 0.78f, 0.94f, 1f),
            new Color(1f, 0.42f, 0.58f, 1f),
            new Color(0.98f, 0.74f, 0.2f, 1f),
            new Color(0.38f, 0.88f, 0.48f, 1f),
            new Color(0.65f, 0.45f, 0.96f, 1f),
            new Color(1f, 0.57f, 0.24f, 1f),
            new Color(0.28f, 0.86f, 0.78f, 1f),
            new Color(0.88f, 0.38f, 0.88f, 1f)
        };

        private PreviewRenderUtility previewUtility;
        private GameObject previewRoot;
        private MeshFilter surfaceFilter;
        private MeshRenderer surfaceRenderer;
        private MeshFilter wireframeFilter;
        private MeshRenderer wireframeRenderer;
        private MeshFilter seamFilter;
        private MeshRenderer seamRenderer;
        private MeshFilter normalFilter;
        private MeshRenderer normalRenderer;
        private MeshFilter thicknessFilter;
        private MeshRenderer thicknessRenderer;
        private Material texturedMaterial;
        private Material solidMaterial;
        private Material panelColorMaterial;
        private Material lineMaterial;
        private Mesh wireframeMesh;
        private Mesh seamMesh;
        private Mesh normalMesh;
        private Mesh thicknessMesh;
        private FoldCanvasCompileResult compileResult;
        private SeamDefinition highlightedSeam;
        private Vector2 orbit = new Vector2(-25f, 20f);
        private Vector3 target;
        private float distance = 3f;
        private bool showSolid;
        private bool showWireframe;
        private bool showPanelColors;
        private bool showSeams;
        private bool showNormals;
        private bool showThickness;

        public GameObject PreviewRoot
        {
            get
            {
                EnsureInitialized();
                return previewRoot;
            }
        }

        internal Mesh SurfaceMeshForTests => surfaceFilter?.sharedMesh;

        internal Material SurfaceMaterialForTests =>
            surfaceRenderer?.sharedMaterial;

        public void SetResult(
            FoldCanvasCompileResult result,
            Texture2D appearance,
            SeamDefinition seam)
        {
            EnsureInitialized();
            compileResult = result;
            highlightedSeam = seam;
            surfaceFilter.sharedMesh = result?.Mesh;
            texturedMaterial.mainTexture = appearance;

            if (result?.Mesh == null || result.CompiledData == null)
            {
                DestroyDebugMeshes();
                wireframeFilter.sharedMesh = null;
                seamFilter.sharedMesh = null;
                normalFilter.sharedMesh = null;
                thicknessFilter.sharedMesh = null;
                UpdateVisibility();
                return;
            }

            ApplyPanelColors(result);
            RebuildDebugMeshes();
            FrameResult();
            UpdateVisibility();
        }

        public void SetHighlightedSeam(SeamDefinition seam)
        {
            highlightedSeam = seam;
            if (compileResult?.CompiledData == null)
            {
                return;
            }

            DestroyMesh(ref seamMesh);
            seamMesh = CreateSeamMesh(
                compileResult.CompiledData,
                highlightedSeam);
            seamFilter.sharedMesh = seamMesh;
            UpdateVisibility();
        }

        public void SetWireframe(bool value)
        {
            showWireframe = value;
            UpdateVisibility();
        }

        public void SetSolid(bool value)
        {
            showSolid = value;
            UpdateVisibility();
        }

        public void SetPanelColors(bool value)
        {
            showPanelColors = value;
            UpdateVisibility();
        }

        public void SetSeams(bool value)
        {
            showSeams = value;
            UpdateVisibility();
        }

        public void SetNormals(bool value)
        {
            showNormals = value;
            UpdateVisibility();
        }

        public void SetThickness(bool value)
        {
            showThickness = value;
            UpdateVisibility();
        }

        public void FrameResult()
        {
            Mesh mesh = surfaceFilter?.sharedMesh;
            if (mesh == null)
            {
                target = Vector3.zero;
                distance = 3f;
                return;
            }

            Bounds bounds = mesh.bounds;
            target = bounds.center;
            float radius = Mathf.Max(0.01f, bounds.extents.magnitude);
            float halfFieldOfView =
                Mathf.Max(1f, previewUtility.cameraFieldOfView * 0.5f) *
                Mathf.Deg2Rad;
            distance = Mathf.Max(
                0.1f,
                radius / Mathf.Tan(halfFieldOfView) * 1.2f);
        }

        public void OnPreviewGUI()
        {
            EnsureInitialized();
            Rect rect = GUILayoutUtility.GetRect(
                64f,
                10000f,
                64f,
                10000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            HandleInput(rect);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(orbit.y, orbit.x, 0f);
            previewUtility.camera.transform.position =
                target + rotation * (Vector3.back * distance);
            previewUtility.camera.transform.rotation = rotation;
            previewUtility.camera.nearClipPlane =
                Mathf.Max(0.001f, distance * 0.01f);
            previewUtility.camera.farClipPlane =
                Mathf.Max(100f, distance * 20f);

            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.camera.Render();
            Texture previewTexture = previewUtility.EndPreview();
            GUI.DrawTexture(
                rect,
                previewTexture,
                ScaleMode.StretchToFill,
                false);

            if (surfaceFilter.sharedMesh == null)
            {
                GUI.Label(
                    new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 40f),
                    "Select a valid FoldCanvas source to preview its derived Mesh.",
                    EditorStyles.wordWrappedLabel);
            }
        }

        public void Dispose()
        {
            DestroyDebugMeshes();
            DestroyMaterial(ref texturedMaterial);
            DestroyMaterial(ref solidMaterial);
            DestroyMaterial(ref panelColorMaterial);
            DestroyMaterial(ref lineMaterial);
            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }

            previewRoot = null;
            surfaceFilter = null;
            surfaceRenderer = null;
            wireframeFilter = null;
            wireframeRenderer = null;
            seamFilter = null;
            seamRenderer = null;
            normalFilter = null;
            normalRenderer = null;
            thicknessFilter = null;
            thicknessRenderer = null;
            compileResult = null;
            highlightedSeam = null;
        }

        private void EnsureInitialized()
        {
            if (previewUtility != null)
            {
                return;
            }

            previewUtility = new PreviewRenderUtility();
            previewUtility.cameraFieldOfView = 30f;
            previewUtility.camera.clearFlags = CameraClearFlags.Color;
            previewUtility.camera.backgroundColor =
                new Color(0.025f, 0.032f, 0.05f, 1f);
            previewUtility.ambientColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            previewUtility.lights[0].intensity = 1.2f;
            previewUtility.lights[0].transform.rotation =
                Quaternion.Euler(40f, 40f, 0f);
            previewUtility.lights[1].intensity = 0.65f;
            previewUtility.lights[1].transform.rotation =
                Quaternion.Euler(340f, 218f, 177f);

            previewRoot = new GameObject(PreviewRootName)
            {
                hideFlags = HideFlags.HideAndDontSave,
                tag = "EditorOnly"
            };
            CreateRendererObject(
                "Surface",
                out surfaceFilter,
                out surfaceRenderer);
            CreateRendererObject(
                "Wireframe",
                out wireframeFilter,
                out wireframeRenderer);
            CreateRendererObject(
                "Selected Seam",
                out seamFilter,
                out seamRenderer);
            CreateRendererObject(
                "Normals",
                out normalFilter,
                out normalRenderer);
            CreateRendererObject(
                "Thickness",
                out thicknessFilter,
                out thicknessRenderer);

            texturedMaterial = CreateMaterial(
                "FoldCanvas/Two-Sided Unlit Texture",
                "Unlit/Texture",
                "M06 Preview Textured");
            solidMaterial = CreateMaterial(
                "FoldCanvas/One-Sided Solid Color",
                "Unlit/Color",
                "M06 Preview Solid");
            if (solidMaterial.HasProperty("_Color"))
            {
                solidMaterial.SetColor(
                    "_Color",
                    new Color(0.25f, 0.78f, 0.86f, 1f));
            }

            panelColorMaterial = CreateMaterial(
                "FoldCanvas/Debug Vertex Color",
                "Sprites/Default",
                "M06 Preview Panel Colors");
            lineMaterial = CreateMaterial(
                "FoldCanvas/Debug Vertex Color",
                "Sprites/Default",
                "M06 Preview Lines");

            surfaceRenderer.sharedMaterial = texturedMaterial;
            wireframeRenderer.sharedMaterial = lineMaterial;
            seamRenderer.sharedMaterial = lineMaterial;
            normalRenderer.sharedMaterial = lineMaterial;
            thicknessRenderer.sharedMaterial = lineMaterial;
            previewUtility.AddSingleGO(previewRoot);
            UpdateVisibility();
        }

        private void CreateRendererObject(
            string objectName,
            out MeshFilter filter,
            out MeshRenderer renderer)
        {
            GameObject child = new GameObject(objectName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            child.transform.SetParent(previewRoot.transform, false);
            filter = child.AddComponent<MeshFilter>();
            renderer = child.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void UpdateVisibility()
        {
            if (surfaceRenderer == null)
            {
                return;
            }

            bool hasMesh = surfaceFilter.sharedMesh != null;
            surfaceRenderer.enabled = hasMesh;
            surfaceRenderer.sharedMaterial = showPanelColors
                ? panelColorMaterial
                : (!showSolid && texturedMaterial.mainTexture != null
                    ? texturedMaterial
                    : solidMaterial);
            wireframeRenderer.enabled = hasMesh && showWireframe &&
                wireframeFilter.sharedMesh != null;
            seamRenderer.enabled = hasMesh && showSeams &&
                seamFilter.sharedMesh != null;
            normalRenderer.enabled = hasMesh && showNormals &&
                normalFilter.sharedMesh != null;
            thicknessRenderer.enabled = hasMesh && showThickness &&
                thicknessFilter.sharedMesh != null;
        }

        private void RebuildDebugMeshes()
        {
            DestroyDebugMeshes();
            FoldCanvasCompiledData data = compileResult.CompiledData;
            wireframeMesh = CreateWireframeMesh(data);
            seamMesh = CreateSeamMesh(data, highlightedSeam);
            normalMesh = CreateNormalMesh(compileResult.Mesh);
            thicknessMesh = CreateThicknessMesh(data);
            wireframeFilter.sharedMesh = wireframeMesh;
            seamFilter.sharedMesh = seamMesh;
            normalFilter.sharedMesh = normalMesh;
            thicknessFilter.sharedMesh = thicknessMesh;
        }

        private static void ApplyPanelColors(FoldCanvasCompileResult result)
        {
            Color[] colors = new Color[result.CompiledData.Vertices.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                int panelIndex = result.CompiledData.Vertices[i].PanelIndex;
                colors[i] = PanelPalette[
                    Mathf.Abs(panelIndex) % PanelPalette.Length];
            }

            result.Mesh.colors = colors;
        }

        private static Mesh CreateWireframeMesh(FoldCanvasCompiledData data)
        {
            List<Vector3> positions = new List<Vector3>();
            List<Color> colors = new List<Color>();
            HashSet<ulong> seen = new HashSet<ulong>();
            IReadOnlyList<int> triangles = data.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                AddLogicalEdge(
                    data,
                    triangles[i],
                    triangles[i + 1],
                    seen,
                    positions,
                    colors,
                    new Color(0.15f, 0.9f, 1f, 1f));
                AddLogicalEdge(
                    data,
                    triangles[i + 1],
                    triangles[i + 2],
                    seen,
                    positions,
                    colors,
                    new Color(0.15f, 0.9f, 1f, 1f));
                AddLogicalEdge(
                    data,
                    triangles[i + 2],
                    triangles[i],
                    seen,
                    positions,
                    colors,
                    new Color(0.15f, 0.9f, 1f, 1f));
            }

            return CreateLineMesh("M06 Logical Wireframe", positions, colors);
        }

        private static void AddLogicalEdge(
            FoldCanvasCompiledData data,
            int a,
            int b,
            HashSet<ulong> seen,
            List<Vector3> positions,
            List<Color> colors,
            Color color)
        {
            int topologyA = data.Vertices[a].TopologyVertexId;
            int topologyB = data.Vertices[b].TopologyVertexId;
            uint minimum = (uint)Mathf.Min(topologyA, topologyB);
            uint maximum = (uint)Mathf.Max(topologyA, topologyB);
            ulong key = ((ulong)minimum << 32) | maximum;
            if (!seen.Add(key))
            {
                return;
            }

            positions.Add(data.Vertices[a].Position);
            positions.Add(data.Vertices[b].Position);
            colors.Add(color);
            colors.Add(color);
        }

        private static Mesh CreateSeamMesh(
            FoldCanvasCompiledData data,
            SeamDefinition seam)
        {
            if (seam == null)
            {
                return null;
            }

            List<Vector3> positions = new List<Vector3>();
            List<Color> colors = new List<Color>();
            AddBoundary(
                data,
                seam.A,
                new Color(0.2f, 0.86f, 1f, 1f),
                positions,
                colors);
            AddBoundary(
                data,
                seam.B,
                new Color(1f, 0.3f, 0.68f, 1f),
                positions,
                colors);
            return CreateLineMesh("M06 Selected Seam", positions, colors);
        }

        private static void AddBoundary(
            FoldCanvasCompiledData data,
            BoundaryReference reference,
            Color color,
            List<Vector3> positions,
            List<Color> colors)
        {
            if (!data.TryGetPanel(
                    reference.PanelId,
                    out FoldCanvasCompiledPanel panel) ||
                !panel.TryGetBoundary(
                    reference.BoundaryId,
                    out FoldCanvasCompiledBoundary boundary))
            {
                return;
            }

            for (int i = 0; i + 1 < boundary.VertexIndices.Count; i++)
            {
                positions.Add(
                    data.Vertices[boundary.VertexIndices[i]].Position);
                positions.Add(
                    data.Vertices[boundary.VertexIndices[i + 1]].Position);
                colors.Add(color);
                colors.Add(color);
            }
        }

        private static Mesh CreateNormalMesh(Mesh surface)
        {
            if (surface == null)
            {
                return null;
            }

            Vector3[] vertices = surface.vertices;
            Vector3[] normals = surface.normals;
            if (vertices.Length == 0 || normals.Length != vertices.Length)
            {
                return null;
            }

            int stride = Mathf.Max(1, vertices.Length / 160);
            float length = Mathf.Max(
                0.0025f,
                surface.bounds.extents.magnitude * 0.05f);
            List<Vector3> positions = new List<Vector3>();
            List<Color> colors = new List<Color>();
            Color color = new Color(0.98f, 0.82f, 0.2f, 1f);
            for (int i = 0; i < vertices.Length; i += stride)
            {
                positions.Add(vertices[i]);
                positions.Add(vertices[i] + normals[i] * length);
                colors.Add(color);
                colors.Add(color);
            }

            return CreateLineMesh("M06 Normals", positions, colors);
        }

        private static Mesh CreateThicknessMesh(FoldCanvasCompiledData data)
        {
            if (data.CornerSegments.Count == 0)
            {
                return null;
            }

            List<Vector3> positions = new List<Vector3>();
            List<Color> colors = new List<Color>();
            Color outerColor = new Color(1f, 0.55f, 0.16f, 1f);
            Color innerColor = new Color(0.56f, 0.36f, 1f, 1f);
            for (int i = 0; i < data.CornerSegments.Count; i++)
            {
                FoldCanvasCompiledCornerSegment segment =
                    data.CornerSegments[i];
                AddSegment(
                    data,
                    segment.OuterFromVertexIndex,
                    segment.OuterToVertexIndex,
                    outerColor,
                    positions,
                    colors);
                AddSegment(
                    data,
                    segment.InnerFromVertexIndex,
                    segment.InnerToVertexIndex,
                    innerColor,
                    positions,
                    colors);
            }

            return CreateLineMesh("M06 Thickness", positions, colors);
        }

        private static void AddSegment(
            FoldCanvasCompiledData data,
            int from,
            int to,
            Color color,
            List<Vector3> positions,
            List<Color> colors)
        {
            if (from < 0 ||
                from >= data.Vertices.Count ||
                to < 0 ||
                to >= data.Vertices.Count)
            {
                return;
            }

            positions.Add(data.Vertices[from].Position);
            positions.Add(data.Vertices[to].Position);
            colors.Add(color);
            colors.Add(color);
        }

        private static Mesh CreateLineMesh(
            string meshName,
            List<Vector3> positions,
            List<Color> colors)
        {
            if (positions.Count == 0)
            {
                return null;
            }

            Mesh mesh = new Mesh
            {
                name = meshName,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (positions.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            int[] indices = new int[positions.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            mesh.SetVertices(positions);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, MeshTopology.Lines, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void HandleInput(Rect rect)
        {
            Event current = Event.current;
            if (!rect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                orbit.x += current.delta.x * 0.7f;
                orbit.y = Mathf.Clamp(
                    orbit.y - current.delta.y * 0.7f,
                    -89f,
                    89f);
                current.Use();
            }
            else if (current.type == EventType.ScrollWheel)
            {
                distance = Mathf.Clamp(
                    distance * (1f + current.delta.y * 0.05f),
                    0.02f,
                    1000f);
                current.Use();
            }
        }

        private void DestroyDebugMeshes()
        {
            if (wireframeFilter != null)
            {
                wireframeFilter.sharedMesh = null;
            }

            if (seamFilter != null)
            {
                seamFilter.sharedMesh = null;
            }

            if (normalFilter != null)
            {
                normalFilter.sharedMesh = null;
            }

            if (thicknessFilter != null)
            {
                thicknessFilter.sharedMesh = null;
            }

            DestroyMesh(ref wireframeMesh);
            DestroyMesh(ref seamMesh);
            DestroyMesh(ref normalMesh);
            DestroyMesh(ref thicknessMesh);
        }

        private static Material CreateMaterial(
            string primaryShaderName,
            string fallbackShaderName,
            string materialName)
        {
            Shader shader = Shader.Find(primaryShaderName) ??
                Shader.Find(fallbackShaderName) ??
                Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"No preview shader is available for '{materialName}'.");
            }

            return new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void DestroyMesh(ref Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = null;
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(material);
            material = null;
        }
    }
}
