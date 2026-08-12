using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FoldCanvas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoldCanvas.M21Proof
{
    public static class FoldCanvasProofGalleryGenerator
    {
        private const int SourceWidth = 960;
        private const int SourceHeight = 540;
        private const int ResultSize = 720;
        private const string RequiredUnityVersion = "6000.3.20f1";
        private const string CupJson = "Assets/Source/cup.foldcanvas.json";
        private const string CupPng = "Assets/Source/cup-canvas.png";
        private const string SphereJson =
            "Assets/Source/sphere.foldcanvas.json";
        private const string SpherePng =
            "Assets/Source/sphere-canvas.png";
        private static readonly Color Background =
            new Color(0.018f, 0.026f, 0.05f, 1f);
        private static readonly Color Cyan =
            new Color(0.22f, 0.92f, 1f, 1f);
        private static readonly Color Magenta =
            new Color(1f, 0.18f, 0.72f, 1f);
        private static readonly Color Orange =
            new Color(1f, 0.72f, 0.08f, 1f);

        public static void GenerateBatch()
        {
            try
            {
                Generate();
                Debug.Log("FoldCanvas M21 proof gallery generation passed.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void GenerateForTest()
        {
            Generate();
            Debug.Log("FoldCanvas M21 proof gallery generation passed.");
        }

        private static void Generate()
        {
            if (Application.unityVersion != RequiredUnityVersion)
            {
                throw new InvalidOperationException(
                    "M21 requires Unity " + RequiredUnityVersion + ".");
            }

            string outputRoot = Environment.GetEnvironmentVariable(
                "FOLDCANVAS_PROOF_OUTPUT");
            string sourceRevision = Environment.GetEnvironmentVariable(
                "FOLDCANVAS_PROOF_SOURCE_REVISION");
            string generatorHash = RequiredHashEnvironment(
                "FOLDCANVAS_PROOF_GENERATOR_SHA256");
            string runnerHash = RequiredHashEnvironment(
                "FOLDCANVAS_PROOF_RUNNER_SHA256");
            string testHash = RequiredHashEnvironment(
                "FOLDCANVAS_PROOF_TEST_SHA256");
            string projectBuilderHash = RequiredHashEnvironment(
                "FOLDCANVAS_PROOF_PROJECT_BUILDER_SHA256");
            if (string.IsNullOrWhiteSpace(outputRoot) ||
                !Path.IsPathRooted(outputRoot))
            {
                throw new InvalidDataException(
                    "FOLDCANVAS_PROOF_OUTPUT must be an absolute path.");
            }
            if (!IsRevision(sourceRevision))
            {
                throw new InvalidDataException(
                    "FOLDCANVAS_PROOF_SOURCE_REVISION must be a lowercase " +
                    "40-character commit SHA.");
            }

            outputRoot = Path.GetFullPath(outputRoot);
            Directory.CreateDirectory(outputRoot);
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Shader textureShader = Shader.Find(
                "FoldCanvas/One-Sided Unlit Texture");
            Shader lineShader = Shader.Find(
                "FoldCanvas/Topology Wireframe");
            if (textureShader == null || lineShader == null)
            {
                throw new InvalidDataException(
                    "Required proof shaders are not imported.");
            }

            Material line = ColorMaterial(lineShader, Cyan, "Topology");
            Material seam = ColorMaterial(lineShader, Magenta, "Seams");
            Material pole = ColorMaterial(lineShader, Orange, "Poles");
            line.renderQueue = 4000;
            seam.renderQueue = 4001;
            pole.renderQueue = 4002;
            List<Artifact> artifacts = new List<Artifact>();
            Evidence cup = Cup(
                outputRoot,
                textureShader,
                line,
                artifacts);
            Evidence sphere = Sphere(
                outputRoot,
                textureShader,
                line,
                seam,
                pole,
                artifacts);
            artifacts.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            Manifest(
                outputRoot,
                sourceRevision,
                generatorHash,
                runnerHash,
                testHash,
                projectBuilderHash,
                artifacts,
                cup,
                sphere);
            UnityEngine.Object.DestroyImmediate(line);
            UnityEngine.Object.DestroyImmediate(seam);
            UnityEngine.Object.DestroyImmediate(pole);
        }

        private static Evidence Cup(
            string outputRoot,
            Shader textureShader,
            Material lineMaterial,
            List<Artifact> artifacts)
        {
            Source source = Load(CupJson, CupPng);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(source.Asset);
            try
            {
                FoldCanvasClosedVolumeReport report =
                    result.ClosedVolumeReport;
                if (!result.Success || report == null ||
                    !report.IsSingleClosedVolume ||
                    report.ComponentCount != 1 ||
                    report.OpenEdgeCount != 0 ||
                    report.NonManifoldEdgeCount != 0 ||
                    report.OrientationConflictEdgeCount != 0 ||
                    report.TotalAbsoluteVolume <= 0d)
                {
                    throw new InvalidDataException(
                        "Cup is not one valid closed volume: " +
                        Diagnostics(result) + ".");
                }

                SourceImage(
                    source.Texture,
                    "cup-source.png",
                    outputRoot,
                    artifacts);
                Material textured = TextureMaterial(
                    textureShader,
                    source.Texture,
                    "Cup Texture");
                GameObject cup = MeshObject(
                    "Cup",
                    result.Mesh,
                    textured,
                    Quaternion.Euler(0f, -90f, 0f));
                Render(
                    "cup-textured.png",
                    ResultSize,
                    ResultSize,
                    new Vector3(0.22f, 0.1f, -0.48f),
                    Vector3.zero,
                    0.09f,
                    outputRoot,
                    artifacts);

                cup.SetActive(false);
                Mesh topology = Topology(result.CompiledData);
                GameObject wire = MeshObject(
                    "Cup Topology",
                    topology,
                    lineMaterial,
                    Quaternion.Euler(0f, -90f, 0f));
                Render(
                    "cup-topology.png",
                    ResultSize,
                    ResultSize,
                    new Vector3(0.18f, 0.07f, -0.48f),
                    Vector3.zero,
                    0.09f,
                    outputRoot,
                    artifacts);
                UnityEngine.Object.DestroyImmediate(cup);
                UnityEngine.Object.DestroyImmediate(wire);
                UnityEngine.Object.DestroyImmediate(textured);
                UnityEngine.Object.DestroyImmediate(topology);

                return new Evidence(
                    "cup",
                    source.JsonHash,
                    source.PngHash,
                    GeometryHash(result.CompiledData),
                    new SortedDictionary<string, string>(
                        StringComparer.Ordinal)
                    {
                        ["componentCount"] = "1",
                        ["isSingleClosedVolume"] = "true",
                        ["nonManifoldEdgeCount"] = "0",
                        ["openEdgeCount"] = "0",
                        ["orientationConflictEdgeCount"] = "0",
                        ["totalAbsoluteVolume"] =
                            report.TotalAbsoluteVolume.ToString(
                                "R",
                                CultureInfo.InvariantCulture)
                    });
            }
            finally
            {
                Destroy(result, source);
            }
        }

        private static Evidence Sphere(
            string outputRoot,
            Shader textureShader,
            Material lineMaterial,
            Material seamMaterial,
            Material poleMaterial,
            List<Artifact> artifacts)
        {
            Source source = Load(SphereJson, SpherePng);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(source.Asset);
            try
            {
                FoldCanvasSphereReport report = result.SphereReport;
                if (!result.Success || report == null ||
                    !report.IsClosedSphere ||
                    report.SphericalPanelCount != 8 ||
                    report.EulerCharacteristic != 2 ||
                    report.OpenEdgeCount != 0 ||
                    report.NonManifoldEdgeCount != 0 ||
                    report.OrientationConflictEdgeCount != 0 ||
                    report.InwardTriangleCount != 0 ||
                    report.NorthPoleTopologyCount != 1 ||
                    report.SouthPoleTopologyCount != 1)
                {
                    throw new InvalidDataException(
                        "Sphere is not the expected closed outward " +
                        "eight-gore surface: " + Diagnostics(result) + ".");
                }

                SourceImage(
                    source.Texture,
                    "sphere-source.png",
                    outputRoot,
                    artifacts);
                Material textured = TextureMaterial(
                    textureShader,
                    source.Texture,
                    "Sphere Texture");
                Quaternion rotation = Quaternion.Euler(12f, -22f, 0f);
                GameObject sphere = MeshObject(
                    "Sphere",
                    result.Mesh,
                    textured,
                    rotation);
                Render(
                    "sphere-textured.png",
                    ResultSize,
                    ResultSize,
                    new Vector3(1.08f, 0.52f, -3.5f),
                    Vector3.zero,
                    0.68f,
                    outputRoot,
                    artifacts);

                sphere.SetActive(false);
                Mesh topology = Topology(result.CompiledData);
                Mesh seams = Boundaries(result.CompiledData);
                Mesh poles = Poles(result.CompiledData);
                GameObject wire = MeshObject(
                    "Sphere Topology",
                    topology,
                    lineMaterial,
                    rotation);
                GameObject seam = MeshObject(
                    "Sphere Seams",
                    seams,
                    seamMaterial,
                    rotation);
                GameObject pole = MeshObject(
                    "Sphere Poles",
                    poles,
                    poleMaterial,
                    rotation);
                Render(
                    "sphere-topology.png",
                    ResultSize,
                    ResultSize,
                    new Vector3(1.08f, 0.52f, -3.5f),
                    Vector3.zero,
                    0.68f,
                    outputRoot,
                    artifacts);
                UnityEngine.Object.DestroyImmediate(sphere);
                UnityEngine.Object.DestroyImmediate(wire);
                UnityEngine.Object.DestroyImmediate(seam);
                UnityEngine.Object.DestroyImmediate(pole);
                UnityEngine.Object.DestroyImmediate(textured);
                UnityEngine.Object.DestroyImmediate(topology);
                UnityEngine.Object.DestroyImmediate(seams);
                UnityEngine.Object.DestroyImmediate(poles);

                return new Evidence(
                    "sphere",
                    source.JsonHash,
                    source.PngHash,
                    GeometryHash(result.CompiledData),
                    new SortedDictionary<string, string>(
                        StringComparer.Ordinal)
                    {
                        ["eulerCharacteristic"] = "2",
                        ["inwardTriangleCount"] = "0",
                        ["isClosedSphere"] = "true",
                        ["nonManifoldEdgeCount"] = "0",
                        ["northPoleTopologyCount"] = "1",
                        ["openEdgeCount"] = "0",
                        ["orientationConflictEdgeCount"] = "0",
                        ["southPoleTopologyCount"] = "1",
                        ["sphericalPanelCount"] = "8"
                    });
            }
            finally
            {
                Destroy(result, source);
            }
        }

        private static Source Load(string jsonPath, string pngPath)
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (json == null || texture == null)
            {
                throw new FileNotFoundException(
                    "Proof source import is incomplete.");
            }

            FoldScriptImportResult imported = FoldScriptImporter.Import(
                json.text,
                new FoldScriptImportOptions
                {
                    SourceProjectPath = jsonPath,
                    AppearanceResolver = new Resolver(texture),
                    RequireAppearance = true
                });
            if (!imported.Success)
            {
                throw new InvalidDataException(
                    "FoldScript import failed: " +
                    Diagnostics(imported.Diagnostics) + ".");
            }
            return new Source(
                imported.Asset,
                texture,
                Sha256(File.ReadAllBytes(jsonPath)),
                Sha256(File.ReadAllBytes(pngPath)));
        }

        private static void SourceImage(
            Texture2D texture,
            string fileName,
            string outputRoot,
            List<Artifact> artifacts)
        {
            Shader shader = Shader.Find(
                "FoldCanvas/One-Sided Unlit Texture");
            Material material = TextureMaterial(
                shader,
                texture,
                "Source Canvas");
            float aspect = (float)texture.width / texture.height;
            Mesh quad = new Mesh { name = "Source Quad" };
            quad.vertices = new[]
            {
                new Vector3(-aspect, -1f, 0f),
                new Vector3(aspect, -1f, 0f),
                new Vector3(aspect, 1f, 0f),
                new Vector3(-aspect, 1f, 0f)
            };
            quad.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateNormals();
            GameObject canvas = MeshObject(
                "Source Canvas",
                quad,
                material,
                Quaternion.identity);
            Render(
                fileName,
                SourceWidth,
                SourceHeight,
                new Vector3(0f, 0f, -4f),
                Vector3.zero,
                1.14f,
                outputRoot,
                artifacts);
            UnityEngine.Object.DestroyImmediate(canvas);
            UnityEngine.Object.DestroyImmediate(quad);
            UnityEngine.Object.DestroyImmediate(material);
        }

        private static void Render(
            string fileName,
            int width,
            int height,
            Vector3 cameraPosition,
            Vector3 target,
            float orthographicSize,
            string outputRoot,
            List<Artifact> artifacts)
        {
            GameObject cameraObject = new GameObject("M21 Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            RenderTexture renderTexture = null;
            Texture2D pixels = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                cameraObject.transform.position = cameraPosition;
                cameraObject.transform.rotation = Quaternion.LookRotation(
                    target - cameraPosition,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Background;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.useOcclusionCulling = false;
                renderTexture = new RenderTexture(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Point,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                pixels = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false,
                    true);
                pixels.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0,
                    false);
                pixels.Apply(false, false);
                byte[] png = pixels.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(outputRoot, fileName), png);
                artifacts.Add(new Artifact(
                    fileName,
                    width,
                    height,
                    Sha256(png)));
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
                if (pixels != null)
                {
                    UnityEngine.Object.DestroyImmediate(pixels);
                }
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static GameObject MeshObject(
            string name,
            Mesh mesh,
            Material material,
            Quaternion rotation)
        {
            GameObject result = new GameObject(name);
            result.AddComponent<MeshFilter>().sharedMesh = mesh;
            result.AddComponent<MeshRenderer>().sharedMaterial = material;
            result.transform.rotation = rotation;
            return result;
        }

        private static Material TextureMaterial(
            Shader shader,
            Texture2D texture,
            string name)
        {
            Material material = new Material(shader) { name = name };
            material.mainTexture = texture;
            material.color = Color.white;
            return material;
        }

        private static Material ColorMaterial(
            Shader shader,
            Color color,
            string name)
        {
            Material material = new Material(shader) { name = name };
            material.SetColor("_Color", color);
            return material;
        }

        private static Mesh Topology(FoldCanvasCompiledData data)
        {
            Dictionary<int, Vector3> positions = Positions(data);
            HashSet<ulong> edgeSet = new HashSet<ulong>();
            for (int i = 0; i < data.TriangleIndices.Count; i += 3)
            {
                int a = data.Vertices[
                    data.TriangleIndices[i]].TopologyVertexId;
                int b = data.Vertices[
                    data.TriangleIndices[i + 1]].TopologyVertexId;
                int c = data.Vertices[
                    data.TriangleIndices[i + 2]].TopologyVertexId;
                edgeSet.Add(Edge(a, b));
                edgeSet.Add(Edge(b, c));
                edgeSet.Add(Edge(c, a));
            }
            List<ulong> edges = new List<ulong>(edgeSet);
            edges.Sort();
            List<Vector3> vertices = new List<Vector3>(edges.Count * 2);
            for (int i = 0; i < edges.Count; i++)
            {
                vertices.Add(positions[(int)(edges[i] >> 32)]);
                vertices.Add(positions[(int)(edges[i] & uint.MaxValue)]);
            }
            return Lines("Topology", vertices);
        }

        private static Mesh Boundaries(FoldCanvasCompiledData data)
        {
            List<Vector3> vertices = new List<Vector3>();
            for (int panelIndex = 0;
                panelIndex < data.Panels.Count;
                panelIndex++)
            {
                FoldCanvasCompiledPanel panel = data.Panels[panelIndex];
                for (int boundaryIndex = 0;
                    boundaryIndex < panel.Boundaries.Count;
                    boundaryIndex++)
                {
                    FoldCanvasCompiledBoundary boundary =
                        panel.Boundaries[boundaryIndex];
                    if (boundary.Id != "uMin" && boundary.Id != "uMax")
                    {
                        continue;
                    }
                    for (int i = 0;
                        i + 1 < boundary.VertexIndices.Count;
                        i++)
                    {
                        vertices.Add(data.Vertices[
                            boundary.VertexIndices[i]].Position);
                        vertices.Add(data.Vertices[
                            boundary.VertexIndices[i + 1]].Position);
                    }
                }
            }
            return Lines("Seams", vertices);
        }

        private static Mesh Poles(FoldCanvasCompiledData data)
        {
            Dictionary<int, Vector3> positions = Positions(data);
            float high = float.NegativeInfinity;
            float low = float.PositiveInfinity;
            foreach (Vector3 position in positions.Values)
            {
                high = Mathf.Max(high, position.y);
                low = Mathf.Min(low, position.y);
            }
            List<Vector3> vertices = new List<Vector3>();
            foreach (Vector3 position in positions.Values)
            {
                if (Mathf.Abs(position.y - high) > 1e-5f &&
                    Mathf.Abs(position.y - low) > 1e-5f)
                {
                    continue;
                }
                vertices.Add(position + Vector3.left * 0.025f);
                vertices.Add(position + Vector3.right * 0.025f);
                vertices.Add(position + Vector3.back * 0.025f);
                vertices.Add(position + Vector3.forward * 0.025f);
            }
            return Lines("Poles", vertices);
        }

        private static Mesh Lines(string name, List<Vector3> vertices)
        {
            Mesh mesh = new Mesh { name = name };
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.SetVertices(vertices);
            int[] indices = new int[vertices.Count];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            mesh.SetIndices(indices, MeshTopology.Lines, 0, true);
            return mesh;
        }

        private static Dictionary<int, Vector3> Positions(
            FoldCanvasCompiledData data)
        {
            Dictionary<int, Vector3> positions =
                new Dictionary<int, Vector3>();
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                if (!positions.ContainsKey(vertex.TopologyVertexId))
                {
                    positions.Add(
                        vertex.TopologyVertexId,
                        vertex.Position);
                }
            }
            return positions;
        }

        private static ulong Edge(int first, int second)
        {
            uint minimum = (uint)Mathf.Min(first, second);
            uint maximum = (uint)Mathf.Max(first, second);
            return ((ulong)minimum << 32) | maximum;
        }

        private static string GeometryHash(FoldCanvasCompiledData data)
        {
            StringBuilder text = new StringBuilder();
            text.Append("vertices=");
            text.Append(data.Vertices.Count.ToString(
                CultureInfo.InvariantCulture));
            text.Append('\n');
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                text.Append(i.ToString(CultureInfo.InvariantCulture));
                text.Append('|');
                Float(text, vertex.Position.x);
                text.Append(',');
                Float(text, vertex.Position.y);
                text.Append(',');
                Float(text, vertex.Position.z);
                text.Append('|');
                Float(text, vertex.SourceUv.x);
                text.Append(',');
                Float(text, vertex.SourceUv.y);
                text.Append('|');
                text.Append(vertex.TopologyVertexId.ToString(
                    CultureInfo.InvariantCulture));
                text.Append('|');
                text.Append(vertex.PanelIndex.ToString(
                    CultureInfo.InvariantCulture));
                text.Append('\n');
            }
            text.Append("triangles=");
            text.Append(data.TriangleIndices.Count.ToString(
                CultureInfo.InvariantCulture));
            text.Append('\n');
            for (int i = 0; i < data.TriangleIndices.Count; i++)
            {
                text.Append(data.TriangleIndices[i].ToString(
                    CultureInfo.InvariantCulture));
                text.Append(i % 3 == 2 ? '\n' : ',');
            }
            return Sha256(Encoding.UTF8.GetBytes(text.ToString()));
        }

        private static void Float(StringBuilder text, float value)
        {
            text.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void Manifest(
            string outputRoot,
            string sourceRevision,
            string generatorHash,
            string runnerHash,
            string testHash,
            string projectBuilderHash,
            List<Artifact> artifacts,
            Evidence cup,
            Evidence sphere)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\n");
            JsonString(json, 1, "format", "foldcanvas-proof-gallery", true);
            JsonString(json, 1, "version", "1", true);
            JsonString(json, 1, "sourceRevision", sourceRevision, true);
            JsonString(json, 1, "unityVersion", RequiredUnityVersion, true);
            JsonString(
                json,
                1,
                "packageVersion",
                FoldCanvasVersion.Package,
                true);
            JsonString(
                json,
                1,
                "foldScriptVersion",
                FoldCanvasSourceMetadata.CurrentSchemaVersion,
                true);
            JsonString(
                json,
                1,
                "generator",
                "FoldCanvas.M21Proof.FoldCanvasProofGalleryGenerator." +
                "GenerateBatch",
                true);
            JsonString(
                json,
                1,
                "generatorSha256",
                generatorHash,
                true);
            JsonString(
                json,
                1,
                "runnerSha256",
                runnerHash,
                true);
            JsonString(
                json,
                1,
                "testSha256",
                testHash,
                true);
            JsonString(
                json,
                1,
                "projectBuilderSha256",
                projectBuilderHash,
                true);
            JsonString(
                json,
                1,
                "command",
                "python3 Scripts/generate_proof_gallery.py --unity " +
                "/path/to/Unity",
                true);
            json.Append("  \"sources\": [\n");
            JsonSource(
                json,
                cup,
                "Samples~/BootstrapPanel/" +
                "m12-production-cup.foldcanvas.json",
                "Samples~/BootstrapPanel/M04ProductionCupCanvas.png",
                true);
            JsonSource(
                json,
                sphere,
                "Samples~/Sphere/sphere-golden.foldcanvas.json",
                "Samples~/Sphere/sphere-canvas.png",
                false);
            json.Append("  ],\n  \"artifacts\": [\n");
            for (int i = 0; i < artifacts.Count; i++)
            {
                Artifact artifact = artifacts[i];
                json.Append("    {\n");
                JsonString(json, 3, "path", artifact.Path, true);
                JsonNumber(json, 3, "width", artifact.Width, true);
                JsonNumber(json, 3, "height", artifact.Height, true);
                JsonString(json, 3, "sha256", artifact.Hash, false);
                json.Append(i + 1 < artifacts.Count
                    ? "    },\n"
                    : "    }\n");
            }
            json.Append("  ],\n  \"geometry\": [\n");
            JsonGeometry(json, cup, true);
            JsonGeometry(json, sphere, false);
            json.Append("  ]\n}\n");
            File.WriteAllText(
                Path.Combine(outputRoot, "manifest.json"),
                json.ToString(),
                new UTF8Encoding(false));
        }

        private static void JsonSource(
            StringBuilder json,
            Evidence evidence,
            string jsonPath,
            string pngPath,
            bool comma)
        {
            json.Append("    {\n");
            JsonString(json, 3, "id", evidence.Id, true);
            JsonString(json, 3, "foldScriptPath", jsonPath, true);
            JsonString(
                json,
                3,
                "foldScriptSha256",
                evidence.JsonHash,
                true);
            JsonString(json, 3, "sourceCanvasPath", pngPath, true);
            JsonString(
                json,
                3,
                "sourceCanvasSha256",
                evidence.PngHash,
                false);
            json.Append(comma ? "    },\n" : "    }\n");
        }

        private static void JsonGeometry(
            StringBuilder json,
            Evidence evidence,
            bool comma)
        {
            json.Append("    {\n");
            JsonString(json, 3, "id", evidence.Id, true);
            JsonString(
                json,
                3,
                "geometrySha256",
                evidence.GeometryHash,
                true);
            json.Append("      \"values\": {\n");
            int index = 0;
            foreach (KeyValuePair<string, string> pair in evidence.Values)
            {
                JsonString(
                    json,
                    4,
                    pair.Key,
                    pair.Value,
                    index + 1 < evidence.Values.Count);
                index++;
            }
            json.Append("      }\n");
            json.Append(comma ? "    },\n" : "    }\n");
        }

        private static void JsonString(
            StringBuilder json,
            int indent,
            string name,
            string value,
            bool comma)
        {
            json.Append(' ', indent * 2);
            Quote(json, name);
            json.Append(": ");
            Quote(json, value);
            json.Append(comma ? ",\n" : "\n");
        }

        private static void JsonNumber(
            StringBuilder json,
            int indent,
            string name,
            int value,
            bool comma)
        {
            json.Append(' ', indent * 2);
            Quote(json, name);
            json.Append(": ");
            json.Append(value.ToString(CultureInfo.InvariantCulture));
            json.Append(comma ? ",\n" : "\n");
        }

        private static void Quote(StringBuilder json, string value)
        {
            json.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"') json.Append("\\\"");
                else if (c == '\\') json.Append("\\\\");
                else if (c == '\n') json.Append("\\n");
                else if (c == '\r') json.Append("\\r");
                else if (c == '\t') json.Append("\\t");
                else if (c < 0x20)
                {
                    json.Append("\\u");
                    json.Append(((int)c).ToString("x4"));
                }
                else json.Append(c);
            }
            json.Append('"');
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            Source source)
        {
            if (result != null && result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }
            if (source != null && source.Asset != null)
            {
                UnityEngine.Object.DestroyImmediate(source.Asset);
            }
        }

        private static string Diagnostics(FoldCanvasCompileResult result)
        {
            return result == null
                ? "null result"
                : Diagnostics(result.Diagnostics);
        }

        private static string Diagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < diagnostics.Count; i++)
            {
                if (i > 0) text.Append(" | ");
                text.Append(diagnostics[i].Code);
                text.Append(' ');
                text.Append(diagnostics[i].Message);
            }
            return text.ToString();
        }

        private static bool IsRevision(string value)
        {
            if (value == null || value.Length != 40) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f'))) return false;
            }
            return true;
        }

        private static string RequiredHashEnvironment(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (value == null || value.Length != 64)
            {
                throw new InvalidDataException(
                    name + " must be a lowercase SHA-256 value.");
            }
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f')))
                {
                    throw new InvalidDataException(
                        name + " must be a lowercase SHA-256 value.");
                }
            }
            return value;
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(bytes);
                StringBuilder text = new StringBuilder(64);
                for (int i = 0; i < digest.Length; i++)
                {
                    text.Append(digest[i].ToString("x2"));
                }
                return text.ToString();
            }
        }

        private sealed class Resolver : IFoldScriptAppearanceResolver
        {
            private readonly Texture2D texture;
            public Resolver(Texture2D texture) { this.texture = texture; }
            public bool TryResolve(string path, out Texture2D appearance)
            {
                appearance = texture;
                return appearance != null;
            }
        }

        private sealed class Source
        {
            public Source(
                FoldCanvasAsset asset,
                Texture2D texture,
                string jsonHash,
                string pngHash)
            {
                Asset = asset;
                Texture = texture;
                JsonHash = jsonHash;
                PngHash = pngHash;
            }
            public FoldCanvasAsset Asset { get; }
            public Texture2D Texture { get; }
            public string JsonHash { get; }
            public string PngHash { get; }
        }

        private sealed class Artifact
        {
            public Artifact(
                string path,
                int width,
                int height,
                string hash)
            {
                Path = path;
                Width = width;
                Height = height;
                Hash = hash;
            }
            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
            public string Hash { get; }
        }

        private sealed class Evidence
        {
            public Evidence(
                string id,
                string jsonHash,
                string pngHash,
                string geometryHash,
                SortedDictionary<string, string> values)
            {
                Id = id;
                JsonHash = jsonHash;
                PngHash = pngHash;
                GeometryHash = geometryHash;
                Values = values;
            }
            public string Id { get; }
            public string JsonHash { get; }
            public string PngHash { get; }
            public string GeometryHash { get; }
            public SortedDictionary<string, string> Values { get; }
        }
    }
}
