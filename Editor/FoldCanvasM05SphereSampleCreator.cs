using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FoldCanvas.Editor
{
    public static partial class FoldCanvasSampleCreator
    {
        private const int M05GoreCount = 8;
        private const float M05Radius = 0.5f;
        private const string M05TexturePath =
            SampleFolder + "/M05SphereCanvas.png";
        private const string M05AssetPath =
            SampleFolder + "/M05SphereGoldenFoldCanvas.asset";
        private const string M05PreviewRootObjectName =
            "FoldCanvas Sphere Root";
        private const string M05SourceCanvasObjectName =
            "Source Canvas Preview";
        private const string M05GeneratedSphereObjectName =
            "Generated Sphere";
        private const string M05SolidSphereObjectName =
            "Solid Validation";
        private const string M05WireframeObjectName =
            "Wireframe Debug";
        private const string M05SeamObjectName = "Seam Debug";
        private const string M05PoleObjectName = "Pole Debug";
        private const string M05UvStretchObjectName =
            "UV Stretch Debug";
        private const string M05RadiusErrorObjectName =
            "Radius Error Debug";
        private const string M05ValidationReportObjectName =
            "Validation Report";
        private const string M05PreviewCameraObjectName =
            "Preview Camera";
        private const string M05DebugColorShaderName =
            "FoldCanvas/Debug Vertex Color";
        private const string M05TexturedMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereTextured.mat";
        private const string M05SolidMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereSolid.mat";
        private const string M05WireframeMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereWireframe.mat";
        private const string M05SeamMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereSeams.mat";
        private const string M05PoleMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SpherePoles.mat";
        private const string M05DebugColorMaterialPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereDebugColor.mat";
        private const string M05WireframeMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereWireframe.asset";
        private const string M05SeamMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereSeams.asset";
        private const string M05PoleMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SpherePoles.asset";
        private const string M05UvStretchMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereUvStretch.asset";
        private const string M05RadiusErrorMeshPath =
            FoldCanvasBaker.DefaultOutputFolder +
            "/M05SphereRadiusError.asset";

        [MenuItem("Tools/FoldCanvas/Create Sphere Sample")]
        public static void CreateM05SphereSample()
        {
            EnsureSampleFolder();
            Texture2D appearance =
                CreateOrUpdateM05SphereAppearance();
            FoldCanvasAsset asset =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M05AssetPath);
            bool created = asset == null;
            if (created)
            {
                asset =
                    ScriptableObject.CreateInstance<FoldCanvasAsset>();
                asset.name = "M05SphereGoldenFoldCanvas";
            }

            ConfigureM05SphereAsset(asset, appearance);
            if (created)
            {
                AssetDatabase.CreateAsset(asset, M05AssetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log(
                $"FoldCanvas M05 sphere sample " +
                $"{(created ? "created" : "updated")} at " +
                $"{M05AssetPath}.",
                asset);
        }

        [MenuItem("Tools/FoldCanvas/Create Sphere Proof")]
        public static void CreateM05SphereProof()
        {
            CreateM05SphereSample();
            FoldCanvasAsset source =
                AssetDatabase.LoadAssetAtPath<FoldCanvasAsset>(
                    M05AssetPath);
            if (!FoldCanvasBaker.BakeMesh(
                    source,
                    FoldCanvasBaker.DefaultOutputFolder,
                    out Mesh sphereMesh,
                    out FoldCanvasCompileResult result))
            {
                Debug.LogError(
                    "FoldCanvas M05 sphere proof failed to compile:\n" +
                    JoinDiagnostics(result),
                    source);
                return;
            }

            FoldCanvasSphereReport report = result.SphereReport;
            if (report == null || !report.IsClosedSphere)
            {
                Debug.LogError(
                    "FoldCanvas M05 proof rejected the derived result: " +
                    "the explicit gore panels and seam graph must produce " +
                    "one closed outward sphere.",
                    source);
                return;
            }

            Mesh wireframeMesh =
                CreateOrUpdateM05WireframeMesh(result.CompiledData);
            Mesh seamMesh = CreateOrUpdateM05SeamMesh(
                source,
                result.CompiledData);
            Mesh poleMesh =
                CreateOrUpdateM05PoleMesh(result.CompiledData);
            Mesh uvStretchMesh =
                CreateOrUpdateM05MetricMesh(
                    result.CompiledData,
                    M05UvStretchMeshPath,
                    "M05Sphere_UvStretch",
                    true);
            Mesh radiusErrorMesh =
                CreateOrUpdateM05MetricMesh(
                    result.CompiledData,
                    M05RadiusErrorMeshPath,
                    "M05Sphere_RadiusError",
                    false);

            Material texturedMaterial =
                CreateOrUpdateProofMaterial(
                    M05TexturedMaterialPath,
                    "M05SphereTextured_Material",
                    source.Appearance,
                    false);
            Material solidMaterial =
                CreateOrUpdateM041Material(
                    M05SolidMaterialPath,
                    "M05SphereSolid_Material",
                    M04SolidShaderName,
                    new Color(0.24f, 0.82f, 0.88f, 1f));
            Material wireframeMaterial =
                CreateOrUpdateM041Material(
                    M05WireframeMaterialPath,
                    "M05SphereWireframe_Material",
                    M041WireframeShaderName,
                    new Color(0.22f, 0.92f, 1f, 1f));
            Material seamMaterial =
                CreateOrUpdateM041Material(
                    M05SeamMaterialPath,
                    "M05SphereSeams_Material",
                    M041WireframeShaderName,
                    new Color(1f, 0.18f, 0.72f, 1f));
            seamMaterial.renderQueue = 4001;
            EditorUtility.SetDirty(seamMaterial);
            Material poleMaterial =
                CreateOrUpdateM041Material(
                    M05PoleMaterialPath,
                    "M05SpherePoles_Material",
                    M041WireframeShaderName,
                    new Color(1f, 0.72f, 0.08f, 1f));
            poleMaterial.renderQueue = 4002;
            EditorUtility.SetDirty(poleMaterial);
            Material debugColorMaterial =
                CreateOrUpdateM05DebugColorMaterial();
            AssetDatabase.SaveAssets();

            DeactivateEarlierFoldCanvasPreviews();
            GameObject root = GetOrCreateM05PreviewRoot();
            Vector3 sourcePosition = new Vector3(-1.35f, 0.68f, 0f);
            Vector3 texturedPosition = new Vector3(0f, 0.68f, 0f);
            Vector3 solidPosition = new Vector3(1.25f, 0.68f, 0f);
            Vector3 wirePosition = new Vector3(-1.25f, -0.68f, 0f);
            Vector3 uvPosition = new Vector3(0f, -0.68f, 0f);
            Vector3 radiusPosition = new Vector3(1.25f, -0.68f, 0f);

            GameObject sourceCanvas = GetOrCreateM04MeshObject(
                root,
                M05SourceCanvasObjectName);
            ConfigureM03OwnedMeshObject(
                sourceCanvas,
                CreateOrUpdateM04CanvasMesh(),
                texturedMaterial,
                sourcePosition,
                Quaternion.identity,
                new Vector3(1.05f, 0.525f, 1f));

            GameObject generatedSphere = GetOrCreateM04MeshObject(
                root,
                M05GeneratedSphereObjectName);
            ConfigureM03OwnedMeshObject(
                generatedSphere,
                sphereMesh,
                texturedMaterial,
                texturedPosition,
                Quaternion.identity,
                Vector3.one);

            GameObject solidSphere = GetOrCreateM04MeshObject(
                root,
                M05SolidSphereObjectName);
            ConfigureM03OwnedMeshObject(
                solidSphere,
                sphereMesh,
                solidMaterial,
                solidPosition,
                Quaternion.identity,
                Vector3.one);

            GameObject wireframe = GetOrCreateM04MeshObject(
                root,
                M05WireframeObjectName);
            ConfigureM03OwnedMeshObject(
                wireframe,
                wireframeMesh,
                wireframeMaterial,
                wirePosition,
                Quaternion.identity,
                Vector3.one);

            GameObject seams = GetOrCreateM04MeshObject(
                root,
                M05SeamObjectName);
            ConfigureM03OwnedMeshObject(
                seams,
                seamMesh,
                seamMaterial,
                wirePosition,
                Quaternion.identity,
                Vector3.one);

            GameObject poles = GetOrCreateM04MeshObject(
                root,
                M05PoleObjectName);
            ConfigureM03OwnedMeshObject(
                poles,
                poleMesh,
                poleMaterial,
                wirePosition,
                Quaternion.identity,
                Vector3.one);

            GameObject uvStretch = GetOrCreateM04MeshObject(
                root,
                M05UvStretchObjectName);
            ConfigureM03OwnedMeshObject(
                uvStretch,
                uvStretchMesh,
                debugColorMaterial,
                uvPosition,
                Quaternion.identity,
                Vector3.one);

            GameObject radiusError = GetOrCreateM04MeshObject(
                root,
                M05RadiusErrorObjectName);
            ConfigureM03OwnedMeshObject(
                radiusError,
                radiusErrorMesh,
                debugColorMaterial,
                radiusPosition,
                Quaternion.identity,
                Vector3.one);

            GameObject reportObject = GetOrCreateM05ReportObject(
                root,
                M05ValidationReportObjectName);
            TextMesh reportText =
                reportObject.GetComponent<TextMesh>();
            if (reportText == null)
            {
                reportText = Undo.AddComponent<TextMesh>(reportObject);
            }

            Undo.RecordObject(
                reportText,
                "Update FoldCanvas M05 Validation Report");
            reportText.text = FormatM05ValidationReport(report);
            reportText.anchor = TextAnchor.UpperLeft;
            reportText.alignment = TextAlignment.Left;
            reportText.fontSize = 40;
            reportText.characterSize = 0.018f;
            reportText.color = new Color(0.82f, 0.94f, 1f, 1f);
            Undo.RecordObject(
                reportObject.transform,
                "Place FoldCanvas M05 Validation Report");
            reportObject.transform.localPosition =
                new Vector3(-2.55f, -1.12f, -0.02f);
            reportObject.transform.localRotation = Quaternion.identity;
            reportObject.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(reportText);

            Camera camera = ConfigureM04Camera(
                root,
                M05PreviewCameraObjectName,
                new Vector3(0f, 0f, -7f),
                Vector3.zero,
                true);
            Undo.RecordObject(
                camera,
                "Update FoldCanvas M05 Preview Camera");
            camera.orthographicSize = 1.55f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            camera.backgroundColor =
                new Color(0.018f, 0.026f, 0.05f, 1f);

            Selection.activeGameObject = generatedSphere;
            EditorSceneManager.MarkSceneDirty(root.scene);
            ApplyM05SceneView(camera, Vector3.zero);
            Debug.Log(
                $"FoldCanvas M05 sphere proof: " +
                $"{report.SphericalPanelCount} 2D gore panels, " +
                $"{report.SurfaceRenderVertexCount} render vertices, " +
                $"{report.SurfaceTopologyVertexCount} topology vertices, " +
                $"{report.TriangleCount} triangles, " +
                $"{report.UniqueEdgeCount} logical edges, " +
                $"Euler {report.EulerCharacteristic}, " +
                $"{report.OpenEdgeCount} open, " +
                $"{report.NonManifoldEdgeCount} non-manifold, " +
                $"north/south pole topology " +
                $"{report.NorthPoleTopologyCount}/" +
                $"{report.SouthPoleTopologyCount}, max radius error " +
                $"{report.MaximumRadiusError:R} m. The 2D canvas and " +
                "SphericalWrap/seam rules are authoritative; every visible " +
                "sphere and debug view is derived.",
                generatedSphere);
        }

        [MenuItem("Tools/FoldCanvas/M05 Sphere View/Overview")]
        public static void UseM05SphereOverview()
        {
            SetM05SphereView(
                new Vector3(0f, 0f, -7f),
                Vector3.zero,
                1.55f,
                true);
        }

        [MenuItem("Tools/FoldCanvas/M05 Sphere View/Textured Sphere")]
        public static void UseM05TexturedSphereView()
        {
            SetM05SphereView(
                new Vector3(0f, 0.68f, -4f),
                new Vector3(0f, 0.68f, 0f),
                0.68f,
                false);
        }

        [MenuItem("Tools/FoldCanvas/M05 Sphere View/Wireframe and Seams")]
        public static void UseM05WireframeSphereView()
        {
            SetM05SphereView(
                new Vector3(-1.25f, -0.68f, -4f),
                new Vector3(-1.25f, -0.68f, 0f),
                0.68f,
                false);
        }

        [MenuItem("Tools/FoldCanvas/M05 Sphere View/UV Stretch")]
        public static void UseM05UvStretchView()
        {
            SetM05SphereView(
                new Vector3(0f, -0.68f, -4f),
                new Vector3(0f, -0.68f, 0f),
                0.68f,
                false);
        }

        [MenuItem("Tools/FoldCanvas/M05 Sphere View/Radius Error")]
        public static void UseM05RadiusErrorView()
        {
            SetM05SphereView(
                new Vector3(1.25f, -0.68f, -4f),
                new Vector3(1.25f, -0.68f, 0f),
                0.68f,
                false);
        }

        [MenuItem("Tools/FoldCanvas/Regenerate M05 Sphere Canvas Source")]
        public static void RegenerateM05SphereCanvasSource()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(FoldCanvasSampleCreator).Assembly);
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package path.");
            }

            string normalizedResolvedPath =
                Path.GetFullPath(package.resolvedPath)
                    .Replace('\\', '/');
            if (normalizedResolvedPath.IndexOf(
                    "/Library/PackageCache/",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    "The deterministic M05 source canvas can only be " +
                    "regenerated from a local or embedded development " +
                    "package. Package-cache contents are read-only.");
            }

            string folder = Path.Combine(
                normalizedResolvedPath,
                "Samples~",
                "Sphere");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "sphere-canvas.png");
            Texture2D texture = BuildM05SphereCanvas();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            Debug.Log(
                $"Regenerated the deterministic M05 source canvas at " +
                $"{path}.");
        }

        private static void ConfigureM05SphereAsset(
            FoldCanvasAsset asset,
            Texture2D appearance)
        {
            asset.Appearance = appearance;
            asset.Panels.Clear();
            asset.Seams.Clear();
            asset.Operations.Clear();
            float goreWidth =
                M05Radius * Mathf.PI * 2f / M05GoreCount;
            float goreHeight = M05Radius * Mathf.PI;
            for (int i = 0; i < M05GoreCount; i++)
            {
                string panelId = $"gore-{i:00}";
                asset.Panels.Add(PanelDefinition.CreateRectangle(
                    panelId,
                    new Rect(
                        i / (float)M05GoreCount,
                        0f,
                        1f / M05GoreCount,
                        1f),
                    new Vector2(goreWidth, goreHeight),
                    4,
                    16));
                asset.Operations.Add(
                    new SphericalWrapOperationDefinition
                    {
                        Id = $"wrap-{i:00}",
                        PanelId = panelId,
                        Radius = M05Radius,
                        LatitudeRange =
                            new Vector2(-90f, 90f),
                        LongitudeRange = new Vector2(
                            -180f + i * 45f,
                            -180f + (i + 1) * 45f),
                        WrapDirection =
                            SphericalWrapDirection
                                .LongitudeAlongU,
                        PoleMode = SphericalPoleMode.Merge,
                        SubdivisionMode =
                            SphericalSubdivisionMode.PanelGrid
                    });
            }

            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-sphere"
                };
            for (int i = 0; i < M05GoreCount; i++)
            {
                string seamId = $"seam-{i:00}";
                asset.Seams.Add(new SeamDefinition
                {
                    Id = seamId,
                    A = new BoundaryReference(
                        $"gore-{i:00}",
                        "uMax"),
                    B = new BoundaryReference(
                        $"gore-{(i + 1) % M05GoreCount:00}",
                        "uMin"),
                    Mode = SeamMode.Weld,
                    ReverseB = false,
                    SampleCount = 0
                });
                stitch.SeamIds.Add(seamId);
            }

            asset.Operations.Add(stitch);
        }

        private static Texture2D CreateOrUpdateM05SphereAppearance()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(FoldCanvasSampleCreator).Assembly);
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package path " +
                    "for the M05 sphere canvas.");
            }

            string sourcePath = Path.Combine(
                package.resolvedPath,
                "Samples~",
                "Sphere",
                "sphere-canvas.png");
            if (!File.Exists(sourcePath))
            {
                RegenerateM05SphereCanvasSource();
            }

            File.Copy(
                sourcePath,
                Path.GetFullPath(M05TexturePath),
                true);
            AssetDatabase.ImportAsset(
                M05TexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(M05TexturePath)
                    as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(
                M05TexturePath);
        }

        private static Texture2D BuildM05SphereCanvas()
        {
            const int width = 2048;
            const int height = 1024;
            Texture2D texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "M05SphereCanvas",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32 background =
                new Color32(19, 35, 68, 255);
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            Color32 equator = new Color32(250, 191, 65, 255);
            Color32 north = new Color32(106, 220, 255, 255);
            Color32 south = new Color32(255, 121, 145, 255);
            Color32 ink = new Color32(245, 250, 255, 255);
            int goreWidth = width / M05GoreCount;
            FillM05Rect(
                pixels,
                width,
                height,
                0,
                500,
                width,
                24,
                equator);
            for (int gore = 0; gore < M05GoreCount; gore++)
            {
                int left = gore * goreWidth;
                int center = left + goreWidth / 2;
                FillM05Rect(
                    pixels,
                    width,
                    height,
                    center - 3,
                    190,
                    6,
                    620,
                    gore % 2 == 0 ? north : south);
                DrawM05Text(
                    pixels,
                    width,
                    height,
                    "NORTH",
                    center,
                    840,
                    4,
                    north);
                DrawM05Text(
                    pixels,
                    width,
                    height,
                    "FOLDCANVAS",
                    center,
                    540,
                    3,
                    ink);
                DrawM05Text(
                    pixels,
                    width,
                    height,
                    "SOUTH",
                    center,
                    135,
                    4,
                    south);
                DrawM05Text(
                    pixels,
                    width,
                    height,
                    (gore + 1).ToString("00"),
                    center,
                    62,
                    3,
                    equator);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void FillM05Rect(
            Color32[] pixels,
            int width,
            int height,
            int x,
            int y,
            int rectWidth,
            int rectHeight,
            Color32 color)
        {
            int minX = Mathf.Clamp(x, 0, width);
            int maxX = Mathf.Clamp(x + rectWidth, 0, width);
            int minY = Mathf.Clamp(y, 0, height);
            int maxY = Mathf.Clamp(y + rectHeight, 0, height);
            for (int py = minY; py < maxY; py++)
            {
                int row = py * width;
                for (int px = minX; px < maxX; px++)
                {
                    pixels[row + px] = color;
                }
            }
        }

        private static void DrawM05Text(
            Color32[] pixels,
            int width,
            int height,
            string text,
            int centerX,
            int bottomY,
            int scale,
            Color32 color)
        {
            int characterAdvance = 6 * scale;
            int textWidth =
                text.Length * characterAdvance - scale;
            int originX = centerX - textWidth / 2;
            for (int characterIndex = 0;
                characterIndex < text.Length;
                characterIndex++)
            {
                string[] glyph = GetM05Glyph(text[characterIndex]);
                for (int row = 0; row < glyph.Length; row++)
                {
                    for (int column = 0;
                        column < glyph[row].Length;
                        column++)
                    {
                        if (glyph[row][column] != '1')
                        {
                            continue;
                        }

                        FillM05Rect(
                            pixels,
                            width,
                            height,
                            originX +
                                characterIndex * characterAdvance +
                                column * scale,
                            bottomY +
                                (glyph.Length - 1 - row) * scale,
                            scale,
                            scale,
                            color);
                    }
                }
            }
        }

        private static string[] GetM05Glyph(char character)
        {
            switch (character)
            {
                case 'A':
                    return new[]
                    {
                        "01110", "10001", "10001", "11111",
                        "10001", "10001", "10001"
                    };
                case 'C':
                    return new[]
                    {
                        "01111", "10000", "10000", "10000",
                        "10000", "10000", "01111"
                    };
                case 'D':
                    return new[]
                    {
                        "11110", "10001", "10001", "10001",
                        "10001", "10001", "11110"
                    };
                case 'F':
                    return new[]
                    {
                        "11111", "10000", "10000", "11110",
                        "10000", "10000", "10000"
                    };
                case 'H':
                    return new[]
                    {
                        "10001", "10001", "10001", "11111",
                        "10001", "10001", "10001"
                    };
                case 'L':
                    return new[]
                    {
                        "10000", "10000", "10000", "10000",
                        "10000", "10000", "11111"
                    };
                case 'N':
                    return new[]
                    {
                        "10001", "11001", "10101", "10101",
                        "10011", "10001", "10001"
                    };
                case 'O':
                    return new[]
                    {
                        "01110", "10001", "10001", "10001",
                        "10001", "10001", "01110"
                    };
                case 'R':
                    return new[]
                    {
                        "11110", "10001", "10001", "11110",
                        "10100", "10010", "10001"
                    };
                case 'S':
                    return new[]
                    {
                        "01111", "10000", "10000", "01110",
                        "00001", "00001", "11110"
                    };
                case 'T':
                    return new[]
                    {
                        "11111", "00100", "00100", "00100",
                        "00100", "00100", "00100"
                    };
                case 'U':
                    return new[]
                    {
                        "10001", "10001", "10001", "10001",
                        "10001", "10001", "01110"
                    };
                case 'V':
                    return new[]
                    {
                        "10001", "10001", "10001", "10001",
                        "10001", "01010", "00100"
                    };
                case '0':
                    return new[]
                    {
                        "01110", "10001", "10011", "10101",
                        "11001", "10001", "01110"
                    };
                case '1':
                    return new[]
                    {
                        "00100", "01100", "00100", "00100",
                        "00100", "00100", "01110"
                    };
                case '2':
                    return new[]
                    {
                        "01110", "10001", "00001", "00010",
                        "00100", "01000", "11111"
                    };
                case '3':
                    return new[]
                    {
                        "11110", "00001", "00001", "01110",
                        "00001", "00001", "11110"
                    };
                case '4':
                    return new[]
                    {
                        "00010", "00110", "01010", "10010",
                        "11111", "00010", "00010"
                    };
                case '5':
                    return new[]
                    {
                        "11111", "10000", "10000", "11110",
                        "00001", "00001", "11110"
                    };
                case '6':
                    return new[]
                    {
                        "01110", "10000", "10000", "11110",
                        "10001", "10001", "01110"
                    };
                case '7':
                    return new[]
                    {
                        "11111", "00001", "00010", "00100",
                        "01000", "01000", "01000"
                    };
                case '8':
                    return new[]
                    {
                        "01110", "10001", "10001", "01110",
                        "10001", "10001", "01110"
                    };
                case '9':
                    return new[]
                    {
                        "01110", "10001", "10001", "01111",
                        "00001", "00001", "01110"
                    };
                default:
                    return new[]
                    {
                        "00000", "00000", "00000", "00000",
                        "00000", "00000", "00000"
                    };
            }
        }

        private static Mesh CreateOrUpdateM05WireframeMesh(
            FoldCanvasCompiledData data)
        {
            Dictionary<int, Vector3> positions =
                BuildM041TopologyPositions(data);
            HashSet<ulong> edges = new HashSet<ulong>();
            for (int i = 0;
                i < data.TriangleIndices.Count;
                i += 3)
            {
                int a = data.Vertices[
                    data.TriangleIndices[i]].TopologyVertexId;
                int b = data.Vertices[
                    data.TriangleIndices[i + 1]].TopologyVertexId;
                int c = data.Vertices[
                    data.TriangleIndices[i + 2]].TopologyVertexId;
                edges.Add(M041EdgeKey(a, b));
                edges.Add(M041EdgeKey(b, c));
                edges.Add(M041EdgeKey(c, a));
            }

            List<ulong> orderedEdges = new List<ulong>(edges);
            orderedEdges.Sort();
            List<Vector3> vertices =
                new List<Vector3>(orderedEdges.Count * 2);
            for (int i = 0; i < orderedEdges.Count; i++)
            {
                int a = (int)(orderedEdges[i] >> 32);
                int b = (int)(orderedEdges[i] & uint.MaxValue);
                vertices.Add(positions[a]);
                vertices.Add(positions[b]);
            }

            return CreateOrUpdateM041LineMesh(
                M05WireframeMeshPath,
                "M05Sphere_LogicalWireframe",
                vertices);
        }

        private static Mesh CreateOrUpdateM05SeamMesh(
            FoldCanvasAsset source,
            FoldCanvasCompiledData data)
        {
            List<Vector3> vertices = new List<Vector3>();
            for (int seamIndex = 0;
                seamIndex < source.Seams.Count;
                seamIndex++)
            {
                SeamDefinition seam = source.Seams[seamIndex];
                if (seam == null ||
                    !data.TryGetPanel(
                        seam.A.PanelId,
                        out FoldCanvasCompiledPanel panel) ||
                    !panel.TryGetBoundary(
                        seam.A.BoundaryId,
                        out FoldCanvasCompiledBoundary boundary))
                {
                    continue;
                }

                for (int i = 0;
                    i < boundary.VertexIndices.Count - 1;
                    i++)
                {
                    vertices.Add(
                        data.Vertices[
                            boundary.VertexIndices[i]].Position);
                    vertices.Add(
                        data.Vertices[
                            boundary.VertexIndices[i + 1]].Position);
                }
            }

            return CreateOrUpdateM041LineMesh(
                M05SeamMeshPath,
                "M05Sphere_Seams",
                vertices);
        }

        private static Mesh CreateOrUpdateM05PoleMesh(
            FoldCanvasCompiledData data)
        {
            List<Vector3> vertices = new List<Vector3>();
            if (data.SphericalSurfaces.Count > 0)
            {
                FoldCanvasCompiledSphericalSurface surface =
                    data.SphericalSurfaces[0];
                AddM05PoleCross(
                    vertices,
                    surface.Center +
                        surface.CurrentV * surface.Radius,
                    surface.CurrentU,
                    surface.CurrentNormal);
                AddM05PoleCross(
                    vertices,
                    surface.Center -
                        surface.CurrentV * surface.Radius,
                    surface.CurrentU,
                    surface.CurrentNormal);
            }

            return CreateOrUpdateM041LineMesh(
                M05PoleMeshPath,
                "M05Sphere_Poles",
                vertices);
        }

        private static void AddM05PoleCross(
            List<Vector3> vertices,
            Vector3 center,
            Vector3 firstAxis,
            Vector3 secondAxis)
        {
            const float halfSize = 0.045f;
            vertices.Add(center - firstAxis * halfSize);
            vertices.Add(center + firstAxis * halfSize);
            vertices.Add(center - secondAxis * halfSize);
            vertices.Add(center + secondAxis * halfSize);
            Vector3 diagonal =
                (firstAxis + secondAxis).normalized;
            vertices.Add(center - diagonal * halfSize);
            vertices.Add(center + diagonal * halfSize);
        }

        private static Mesh CreateOrUpdateM05MetricMesh(
            FoldCanvasCompiledData data,
            string path,
            string meshName,
            bool uvStretch)
        {
            Vector3[] vertices = new Vector3[data.Vertices.Count];
            Vector2[] uv = new Vector2[data.Vertices.Count];
            Color[] colors = new Color[data.Vertices.Count];
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                vertices[i] = data.Vertices[i].Position;
                uv[i] = data.Vertices[i].SourceUv;
                colors[i] = Color.gray;
            }

            if (uvStretch)
            {
                ApplyM05UvStretchColors(data, colors);
            }
            else
            {
                ApplyM05RadiusErrorColors(data, colors);
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created)
            {
                mesh = new Mesh { name = meshName };
            }
            else
            {
                mesh.Clear();
                mesh.name = meshName;
            }

            mesh.indexFormat = vertices.Length > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.colors = colors;
            int[] triangles =
                new int[data.TriangleIndices.Count];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = data.TriangleIndices[i];
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (created)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void ApplyM05UvStretchColors(
            FoldCanvasCompiledData data,
            Color[] colors)
        {
            double[] totals = new double[data.Vertices.Count];
            int[] counts = new int[data.Vertices.Count];
            double minimum = double.PositiveInfinity;
            double maximum = double.NegativeInfinity;
            for (int offset = 0;
                offset < data.TriangleIndices.Count;
                offset += 3)
            {
                int aIndex = data.TriangleIndices[offset];
                int bIndex = data.TriangleIndices[offset + 1];
                int cIndex = data.TriangleIndices[offset + 2];
                FoldCanvasCompiledVertex a = data.Vertices[aIndex];
                FoldCanvasCompiledVertex b = data.Vertices[bIndex];
                FoldCanvasCompiledVertex c = data.Vertices[cIndex];
                double sourceArea = Math.Abs(
                    (b.SourcePosition.x - a.SourcePosition.x) *
                    (c.SourcePosition.y - a.SourcePosition.y) -
                    (b.SourcePosition.y - a.SourcePosition.y) *
                    (c.SourcePosition.x - a.SourcePosition.x));
                if (sourceArea <= double.Epsilon)
                {
                    continue;
                }

                double currentArea = Vector3.Cross(
                    b.Position - a.Position,
                    c.Position - a.Position).magnitude;
                double stretch = currentArea / sourceArea;
                int[] triangle = { aIndex, bIndex, cIndex };
                for (int i = 0; i < triangle.Length; i++)
                {
                    totals[triangle[i]] += stretch;
                    counts[triangle[i]]++;
                }
            }

            double[] averages = new double[data.Vertices.Count];
            for (int i = 0; i < averages.Length; i++)
            {
                if (counts[i] == 0)
                {
                    continue;
                }

                averages[i] = totals[i] / counts[i];
                double logarithm = Math.Log(
                    Math.Max(averages[i], 1e-12d));
                minimum = Math.Min(minimum, logarithm);
                maximum = Math.Max(maximum, logarithm);
            }

            if (double.IsInfinity(minimum) ||
                double.IsInfinity(maximum))
            {
                return;
            }

            double range = Math.Max(maximum - minimum, 1e-12d);
            for (int i = 0; i < colors.Length; i++)
            {
                if (counts[i] == 0)
                {
                    continue;
                }

                float t = (float)(
                    (Math.Log(Math.Max(averages[i], 1e-12d)) -
                     minimum) /
                    range);
                colors[i] = M05HeatColor(t);
            }
        }

        private static void ApplyM05RadiusErrorColors(
            FoldCanvasCompiledData data,
            Color[] colors)
        {
            Dictionary<int, FoldCanvasCompiledSphericalSurface>
                surfaces = new Dictionary<int,
                    FoldCanvasCompiledSphericalSurface>();
            for (int i = 0;
                i < data.SphericalSurfaces.Count;
                i++)
            {
                surfaces[data.SphericalSurfaces[i].PanelIndex] =
                    data.SphericalSurfaces[i];
            }

            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex vertex = data.Vertices[i];
                if (!surfaces.TryGetValue(
                        vertex.PanelIndex,
                        out FoldCanvasCompiledSphericalSurface surface))
                {
                    continue;
                }

                double tolerance =
                    FoldCanvasGeometryTolerances
                        .SphericalRadiusAbsoluteTolerance +
                    FoldCanvasGeometryTolerances
                        .SphericalRadiusRelativeTolerance *
                    surface.Radius;
                double error = Math.Abs(
                    Vector3.Distance(
                        vertex.Position,
                        surface.Center) -
                    surface.Radius);
                float t = Mathf.Clamp01(
                    (float)(error / Math.Max(tolerance, 1e-12d)));
                colors[i] = Color.Lerp(
                    new Color(0.18f, 0.95f, 0.45f, 1f),
                    new Color(1f, 0.12f, 0.08f, 1f),
                    t);
            }
        }

        private static Color M05HeatColor(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0.5f)
            {
                return Color.Lerp(
                    new Color(0.08f, 0.32f, 1f, 1f),
                    new Color(0.12f, 1f, 0.45f, 1f),
                    t * 2f);
            }

            return Color.Lerp(
                new Color(0.12f, 1f, 0.45f, 1f),
                new Color(1f, 0.15f, 0.05f, 1f),
                (t - 0.5f) * 2f);
        }

        private static Material CreateOrUpdateM05DebugColorMaterial()
        {
            Shader shader = Shader.Find(M05DebugColorShaderName);
            if (shader == null)
            {
                throw new InvalidDataException(
                    $"Required M05 debug shader " +
                    $"'{M05DebugColorShaderName}' was not imported.");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    M05DebugColorMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M05SphereDebugColor_Material"
                };
                AssetDatabase.CreateAsset(
                    material,
                    M05DebugColorMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_Color", Color.white);
            material.doubleSidedGI = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject GetOrCreateM05ReportObject(
            GameObject root,
            string objectName)
        {
            GameObject report = null;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                GameObject candidate =
                    root.transform.GetChild(i).gameObject;
                if (candidate.name != objectName)
                {
                    continue;
                }

                if (report == null ||
                    candidate.GetInstanceID() < report.GetInstanceID())
                {
                    report = candidate;
                }
            }

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                GameObject duplicate =
                    root.transform.GetChild(i).gameObject;
                if (duplicate != report &&
                    duplicate.name == objectName)
                {
                    Undo.DestroyObjectImmediate(duplicate);
                }
            }

            if (report == null)
            {
                report = new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(
                    report,
                    "Create FoldCanvas M05 Validation Report");
                report.transform.SetParent(root.transform, false);
            }

            Undo.RecordObject(
                report,
                "Update FoldCanvas M05 Validation Report");
            report.name = objectName;
            if (!report.activeSelf)
            {
                report.SetActive(true);
            }

            return report;
        }

        private static string FormatM05ValidationReport(
            FoldCanvasSphereReport report)
        {
            return
                $"CLOSED SPHERE: {(report.IsClosedSphere ? "YES" : "NO")}\n" +
                $"PANELS {report.SphericalPanelCount}  " +
                $"RENDER V {report.SurfaceRenderVertexCount}  " +
                $"TOPOLOGY V {report.SurfaceTopologyVertexCount}\n" +
                $"TRIANGLES {report.TriangleCount}  " +
                $"EDGES {report.UniqueEdgeCount}  " +
                $"EULER {report.EulerCharacteristic}\n" +
                $"OPEN {report.OpenEdgeCount}  " +
                $"NON-MANIFOLD {report.NonManifoldEdgeCount}  " +
                $"POLES {report.NorthPoleTopologyCount}/" +
                $"{report.SouthPoleTopologyCount}\n" +
                $"MAX RADIUS ERROR " +
                report.MaximumRadiusError.ToString(
                    "R",
                    System.Globalization.CultureInfo.InvariantCulture) +
                $"  TOLERANCE " +
                report.RadiusTolerance.ToString(
                    "R",
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        private static GameObject GetOrCreateM05PreviewRoot()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] candidates =
                Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject root = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != scene ||
                    candidate.name != M05PreviewRootObjectName ||
                    !candidate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                if (root == null ||
                    candidate.GetInstanceID() < root.GetInstanceID())
                {
                    root = candidate;
                }
            }

            if (root == null)
            {
                root = new GameObject(M05PreviewRootObjectName);
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create FoldCanvas M05 Sphere Root");
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                GameObject duplicate = candidates[i];
                if (duplicate == null ||
                    duplicate == root ||
                    EditorUtility.IsPersistent(duplicate) ||
                    duplicate.scene != scene ||
                    duplicate.name != M05PreviewRootObjectName ||
                    !duplicate.CompareTag("EditorOnly"))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(duplicate);
            }

            Undo.RecordObject(
                root,
                "Update FoldCanvas M05 Sphere Root");
            root.name = M05PreviewRootObjectName;
            root.tag = "EditorOnly";
            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            Undo.RecordObject(
                root.transform,
                "Reset FoldCanvas M05 Sphere Root");
            root.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static void DeactivateEarlierFoldCanvasPreviews()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate == null ||
                    EditorUtility.IsPersistent(candidate) ||
                    candidate.scene != scene ||
                    !candidate.CompareTag("EditorOnly") ||
                    !candidate.activeSelf)
                {
                    continue;
                }

                bool earlier =
                    candidate.name == M03PreviewRootObjectName ||
                    candidate.name == M04PreviewRootObjectName ||
                    candidate.name == M041PreviewRootObjectName;
                if (!earlier)
                {
                    continue;
                }

                Undo.RecordObject(
                    candidate,
                    "Hide Earlier FoldCanvas Preview");
                candidate.SetActive(false);
            }
        }

        private static void SetM05SphereView(
            Vector3 position,
            Vector3 target,
            float orthographicSize,
            bool showReport)
        {
            GameObject root = FindM05PreviewRoot();
            if (root == null)
            {
                CreateM05SphereProof();
                root = FindM05PreviewRoot();
            }

            if (root == null)
            {
                return;
            }

            if (!root.activeSelf)
            {
                Undo.RecordObject(
                    root,
                    "Show FoldCanvas M05 Sphere Root");
                root.SetActive(true);
            }

            Transform reportTransform =
                root.transform.Find(M05ValidationReportObjectName);
            if (reportTransform != null &&
                reportTransform.gameObject.activeSelf != showReport)
            {
                Undo.RecordObject(
                    reportTransform.gameObject,
                    "Toggle FoldCanvas M05 Validation Report");
                reportTransform.gameObject.SetActive(showReport);
            }

            Transform cameraTransform =
                root.transform.Find(M05PreviewCameraObjectName);
            if (cameraTransform == null)
            {
                CreateM05SphereProof();
                return;
            }

            Camera camera =
                cameraTransform.GetComponent<Camera>();
            Undo.RecordObject(
                cameraTransform,
                "Move FoldCanvas M05 Preview Camera");
            cameraTransform.localPosition = position;
            cameraTransform.localRotation =
                Quaternion.LookRotation(
                    target - position,
                    Vector3.up);
            Undo.RecordObject(
                camera,
                "Frame FoldCanvas M05 Preview Camera");
            camera.enabled = true;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            Selection.activeGameObject = camera.gameObject;
            ApplyM05SceneView(camera, target);
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static GameObject FindM05PreviewRoot()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject candidate = objects[i];
                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.scene == scene &&
                    candidate.name == M05PreviewRootObjectName &&
                    candidate.CompareTag("EditorOnly"))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ApplyM05SceneView(
            Camera camera,
            Vector3 target)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || camera == null)
            {
                return;
            }

            sceneView.pivot = target;
            sceneView.rotation = camera.transform.rotation;
            sceneView.size = camera.orthographicSize * 1.15f;
            sceneView.Repaint();
        }
    }
}
