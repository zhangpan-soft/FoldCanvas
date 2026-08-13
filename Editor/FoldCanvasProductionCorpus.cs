using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Editor
{
    [Serializable]
    internal sealed class FoldCanvasProductionCorpusCase
    {
        public string id;
        public string sourceFormat;
        public string validationLevel;
        public bool success;
        public string errorDiagnosticCode;
        public int diagnosticCount;
        public int renderVertices;
        public int topologyVertices;
        public int triangles;
        public int openEdges;
        public int nonManifoldEdges;
        public int componentCount;
        public bool closedVolume;
        public int sphereEulerCharacteristic;
        public string sourceSha256;
        public string geometrySha256;
        public string objSha256;
        public string diagnosticSha256;
    }

    [Serializable]
    internal sealed class FoldCanvasProductionCorpusDocument
    {
        public string format;
        public string version;
        public string packageVersion;
        public string foldScriptVersion;
        public string unityVersion;
        public string baselineSha256;
        public List<FoldCanvasProductionCorpusCase> cases =
            new List<FoldCanvasProductionCorpusCase>();
    }

    internal sealed class FoldCanvasProductionCorpusDefinition
    {
        public FoldCanvasProductionCorpusDefinition(
            string caseId,
            FoldCanvasValidationLevel level,
            bool shouldSucceed,
            string errorCode,
            string format)
        {
            Id = caseId;
            ValidationLevel = level;
            ExpectedSuccess = shouldSucceed;
            ExpectedErrorCode = errorCode ?? string.Empty;
            SourceFormat = format;
        }

        public string Id { get; }

        public FoldCanvasValidationLevel ValidationLevel { get; }

        public bool ExpectedSuccess { get; }

        public string ExpectedErrorCode { get; }

        public string SourceFormat { get; }
    }

    public static class FoldCanvasProductionCorpusRunner
    {
        internal const string BaselineRelativePath =
            "Documentation~/m24-production-corpus.json";
        internal const string BaselineFormat =
            "foldcanvas-production-corpus";
        internal const string ReportFormat =
            "foldcanvas-production-corpus-report";
        internal const string CorpusVersion = "1";

        private static readonly FoldCanvasProductionCorpusDefinition[]
            Definitions =
            {
                new FoldCanvasProductionCorpusDefinition(
                    "cyclic-torus",
                    FoldCanvasValidationLevel.Strict,
                    true,
                    string.Empty,
                    "FoldScript 0.1"),
                new FoldCanvasProductionCorpusDefinition(
                    "off-grid-fold",
                    FoldCanvasValidationLevel.Basic,
                    true,
                    string.Empty,
                    "FoldScript 0.1"),
                new FoldCanvasProductionCorpusDefinition(
                    "planar-artwork",
                    FoldCanvasValidationLevel.Basic,
                    true,
                    string.Empty,
                    "FoldScript 0.1"),
                new FoldCanvasProductionCorpusDefinition(
                    "production-cup",
                    FoldCanvasValidationLevel.Strict,
                    true,
                    string.Empty,
                    "FoldScript 0.1"),
                new FoldCanvasProductionCorpusDefinition(
                    "registered-wave",
                    FoldCanvasValidationLevel.Standard,
                    true,
                    string.Empty,
                    "native extension descriptor 1"),
                new FoldCanvasProductionCorpusDefinition(
                    "sphere-gores",
                    FoldCanvasValidationLevel.Standard,
                    true,
                    string.Empty,
                    "FoldScript 0.1"),
            };

        [MenuItem("Tools/FoldCanvas/Maintenance/Run M11 Production Corpus")]
        public static void RunFromMenu()
        {
            FoldCanvasProductionCorpusDocument report = RunAndValidate();
            Debug.Log(
                "FoldCanvas M11 production corpus passed " +
                report.cases.Count + " cases. Derived report: " +
                DerivedReportPath());
        }

        [MenuItem(
            "Tools/FoldCanvas/Maintenance/Regenerate M11 Production Corpus Baseline")]
        public static void RegenerateBaseline()
        {
            FoldCanvasProductionCorpusDocument current = RunCurrentCases();
            current.format = BaselineFormat;
            current.baselineSha256 = string.Empty;
            string path = BaselinePath();
            File.WriteAllText(
                path,
                Serialize(current),
                new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log(
                "FoldCanvas M11 production corpus baseline regenerated at " +
                path + ".");
        }

        internal static FoldCanvasProductionCorpusDocument RunAndValidate()
        {
            string baselinePath = BaselinePath();
            string baselineJson = File.ReadAllText(baselinePath);
            FoldCanvasProductionCorpusDocument baseline = Parse(baselineJson);
            ValidateDocumentHeader(baseline, BaselineFormat);

            FoldCanvasProductionCorpusDocument current = RunCurrentCases();
            string difference = DescribeDifference(baseline, current);
            if (!string.IsNullOrEmpty(difference))
            {
                throw new InvalidDataException(
                    "M11 production corpus differs from its reviewed baseline:\n" +
                    difference);
            }

            current.format = ReportFormat;
            current.baselineSha256 = HashText(baselineJson);
            WriteDerivedReport(current);
            return current;
        }

        internal static FoldCanvasProductionCorpusDocument RunCurrentCases()
        {
            FoldCanvasProductionCorpusDocument document =
                new FoldCanvasProductionCorpusDocument
                {
                    format = ReportFormat,
                    version = CorpusVersion,
                    packageVersion = FoldCanvasVersion.Package,
                    foldScriptVersion =
                        FoldCanvasSourceMetadata.CurrentSchemaVersion,
                    unityVersion = Application.unityVersion,
                    baselineSha256 = string.Empty,
                };

            for (int i = 0; i < Definitions.Length; i++)
            {
                document.cases.Add(RunCase(Definitions[i]));
            }

            return document;
        }

        internal static string DescribeDifference(
            FoldCanvasProductionCorpusDocument expected,
            FoldCanvasProductionCorpusDocument actual)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (actual == null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            StringBuilder builder = new StringBuilder();
            CompareValue(
                builder,
                "packageVersion",
                expected.packageVersion,
                actual.packageVersion);
            CompareValue(
                builder,
                "foldScriptVersion",
                expected.foldScriptVersion,
                actual.foldScriptVersion);
            CompareValue(
                builder,
                "unityVersion",
                expected.unityVersion,
                actual.unityVersion);
            if (expected.cases == null || actual.cases == null)
            {
                builder.AppendLine("cases: null corpus case list");
                return builder.ToString().TrimEnd('\r', '\n');
            }

            CompareValue(
                builder,
                "caseCount",
                expected.cases.Count.ToString(CultureInfo.InvariantCulture),
                actual.cases.Count.ToString(CultureInfo.InvariantCulture));
            int shared = Math.Min(expected.cases.Count, actual.cases.Count);
            for (int i = 0; i < shared; i++)
            {
                string expectedJson = JsonUtility.ToJson(expected.cases[i]);
                string actualJson = JsonUtility.ToJson(actual.cases[i]);
                if (!string.Equals(
                        expectedJson,
                        actualJson,
                        StringComparison.Ordinal))
                {
                    builder.Append("case[");
                    builder.Append(i.ToString(CultureInfo.InvariantCulture));
                    builder.Append("] expected ");
                    builder.AppendLine(expectedJson);
                    builder.Append("case[");
                    builder.Append(i.ToString(CultureInfo.InvariantCulture));
                    builder.Append("] actual   ");
                    builder.AppendLine(actualJson);
                }
            }

            return builder.ToString().TrimEnd('\r', '\n');
        }

        internal static FoldCanvasProductionCorpusDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "Corpus JSON must be non-empty.",
                    nameof(json));
            }

            FoldCanvasProductionCorpusDocument document =
                JsonUtility.FromJson<FoldCanvasProductionCorpusDocument>(json);
            if (document == null)
            {
                throw new InvalidDataException(
                    "Production corpus JSON is invalid.");
            }

            return document;
        }

        internal static string Serialize(
            FoldCanvasProductionCorpusDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return JsonUtility.ToJson(document, true) + "\n";
        }

        private static FoldCanvasProductionCorpusCase RunCase(
            FoldCanvasProductionCorpusDefinition definition)
        {
            FoldCanvasOperationRegistry registry;
            string explicitSourceIdentity;
            FoldCanvasAsset asset = FoldCanvasSampleCreator
                .CreateM11CorpusAsset(
                    definition.Id,
                    out registry,
                    out explicitSourceIdentity);
            try
            {
                ConfigureMetadata(asset, definition);
                string sourceText = explicitSourceIdentity ??
                    CanonicalFoldScript(asset);
                FoldCanvasCompileResult first =
                    FoldCanvasCompiler.Compile(asset, registry);
                FoldCanvasCompileResult second =
                    FoldCanvasCompiler.Compile(asset, registry);
                try
                {
                    FoldCanvasProductionCorpusCase firstEvidence =
                        CreateEvidence(definition, sourceText, first);
                    FoldCanvasProductionCorpusCase secondEvidence =
                        CreateEvidence(definition, sourceText, second);
                    string firstJson = JsonUtility.ToJson(firstEvidence);
                    string secondJson = JsonUtility.ToJson(secondEvidence);
                    if (!string.Equals(
                            firstJson,
                            secondJson,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "Corpus case '" + definition.Id +
                            "' changed across identical compiles.\nfirst=" +
                            firstJson + "\nsecond=" + secondJson);
                    }

                    if (firstEvidence.success != definition.ExpectedSuccess)
                    {
                        throw new InvalidDataException(
                            "Corpus case '" + definition.Id +
                            "' returned unexpected success=" +
                            firstEvidence.success + ".");
                    }

                    if (!string.Equals(
                            firstEvidence.errorDiagnosticCode,
                            definition.ExpectedErrorCode,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "Corpus case '" + definition.Id +
                            "' returned error diagnostic '" +
                            firstEvidence.errorDiagnosticCode +
                            "'; expected '" +
                            definition.ExpectedErrorCode + "'.");
                    }

                    return firstEvidence;
                }
                finally
                {
                    DestroyCompileResult(first);
                    DestroyCompileResult(second);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static FoldCanvasProductionCorpusCase CreateEvidence(
            FoldCanvasProductionCorpusDefinition definition,
            string sourceText,
            FoldCanvasCompileResult result)
        {
            FoldCanvasCompiledData data = result.CompiledData;
            FoldCanvasGeometryValidationReport validation =
                result.GeometryValidationReport;
            string objectText = data == null
                ? string.Empty
                : FoldCanvasObjExporter.Export(
                    data,
                    new FoldCanvasObjExportOptions
                    {
                        ObjectName = definition.Id,
                        FlipTextureV = false,
                        IncludeMetadataComments = false,
                    }).Content;
            return new FoldCanvasProductionCorpusCase
            {
                id = definition.Id,
                sourceFormat = definition.SourceFormat,
                validationLevel = definition.ValidationLevel.ToString(),
                success = result.Success,
                errorDiagnosticCode = FirstErrorCode(result),
                diagnosticCount = result.Diagnostics.Count,
                renderVertices = data?.Vertices.Count ?? 0,
                topologyVertices = data?.TopologyVertexCount ?? 0,
                triangles = data?.TriangleIndices.Count / 3 ?? 0,
                openEdges = validation?.OpenEdgeCount ?? 0,
                nonManifoldEdges = validation?.NonManifoldEdgeCount ?? 0,
                componentCount = validation?.ComponentCount ?? 0,
                closedVolume = result.ClosedVolumeReport?.IsClosedVolume ?? false,
                sphereEulerCharacteristic =
                    data != null && data.SphericalSurfaces.Count > 0
                        ? result.SphereReport?.EulerCharacteristic ?? -1
                        : -1,
                sourceSha256 = HashText(sourceText),
                geometrySha256 = HashCompiledData(data),
                objSha256 = HashText(objectText),
                diagnosticSha256 = HashDiagnostics(result.Diagnostics),
            };
        }

        private static void ConfigureMetadata(
            FoldCanvasAsset asset,
            FoldCanvasProductionCorpusDefinition definition)
        {
            asset.name = "M11-" + definition.Id;
            asset.CompileSettings.ValidationLevel =
                definition.ValidationLevel;
            FoldCanvasSourceMetadata metadata = asset.SourceMetadata;
            metadata.SchemaVersion =
                FoldCanvasSourceMetadata.CurrentSchemaVersion;
            metadata.AssetId = "m11-" + definition.Id;
            metadata.DisplayName = "M11 " + definition.Id;
            metadata.Units = FoldScriptUnit.Meter;
            metadata.AppearanceReference =
                "appearance/" + definition.Id + ".png";
            metadata.CanvasPixelWidth = 1024;
            metadata.CanvasPixelHeight = 1024;
            metadata.ExtensionsJson = string.Empty;
        }

        private static string CanonicalFoldScript(FoldCanvasAsset asset)
        {
            FoldScriptDocument document =
                FoldScriptAssetConverter.ToDocument(asset);
            FoldScriptWriteResult write = FoldScriptSerializer.Write(document);
            if (!write.Success)
            {
                throw new InvalidDataException(
                    "Corpus source could not be serialized as canonical " +
                    "FoldScript: " + JoinDiagnostics(write.Diagnostics));
            }

            return write.Json;
        }

        private static string HashCompiledData(FoldCanvasCompiledData data)
        {
            if (data == null)
            {
                return HashBytes(Array.Empty<byte>());
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(
                stream,
                new UTF8Encoding(false),
                true))
            {
                writer.Write(data.Vertices.Count);
                writer.Write(data.TopologyVertexCount);
                for (int i = 0; i < data.Vertices.Count; i++)
                {
                    FoldCanvasCompiledVertex vertex = data.Vertices[i];
                    WriteVector3(writer, vertex.Position);
                    WriteVector2(writer, vertex.SourcePosition);
                    WriteVector2(writer, vertex.SourceUv);
                    writer.Write(vertex.PanelIndex);
                    writer.Write(vertex.ProvenanceId);
                    writer.Write(vertex.TopologyVertexId);
                }

                writer.Write(data.TriangleIndices.Count);
                for (int i = 0; i < data.TriangleIndices.Count; i++)
                {
                    writer.Write(data.TriangleIndices[i]);
                }

                writer.Write(data.Panels.Count);
                for (int panelIndex = 0;
                    panelIndex < data.Panels.Count;
                    panelIndex++)
                {
                    FoldCanvasCompiledPanel panel = data.Panels[panelIndex];
                    writer.Write(panel.Id);
                    writer.Write(panel.PanelIndex);
                    writer.Write((int)panel.Shape);
                    WriteRect(writer, panel.CanvasRect);
                    WriteVector2(writer, panel.PhysicalSize);
                    writer.Write(panel.VertexStart);
                    writer.Write(panel.VertexCount);
                    writer.Write(panel.Boundaries.Count);
                    for (int boundaryIndex = 0;
                        boundaryIndex < panel.Boundaries.Count;
                        boundaryIndex++)
                    {
                        FoldCanvasCompiledBoundary boundary =
                            panel.Boundaries[boundaryIndex];
                        writer.Write(boundary.Id);
                        writer.Write(boundary.VertexIndices.Count);
                        for (int vertexIndex = 0;
                            vertexIndex < boundary.VertexIndices.Count;
                            vertexIndex++)
                        {
                            writer.Write(boundary.VertexIndices[vertexIndex]);
                        }
                    }
                }

                writer.Write(data.CornerSegments.Count);
                for (int i = 0; i < data.CornerSegments.Count; i++)
                {
                    FoldCanvasCompiledCornerSegment corner =
                        data.CornerSegments[i];
                    writer.Write(corner.OperationId);
                    writer.Write(corner.OuterFromVertexIndex);
                    writer.Write(corner.OuterToVertexIndex);
                    writer.Write(corner.InnerFromVertexIndex);
                    writer.Write(corner.InnerToVertexIndex);
                }

                writer.Write(data.SphericalSurfaces.Count);
                for (int i = 0; i < data.SphericalSurfaces.Count; i++)
                {
                    FoldCanvasCompiledSphericalSurface surface =
                        data.SphericalSurfaces[i];
                    writer.Write(surface.OperationId);
                    writer.Write(surface.PanelId);
                    writer.Write(surface.PanelIndex);
                    WriteVector3(writer, surface.Center);
                    WriteVector3(writer, surface.CurrentU);
                    WriteVector3(writer, surface.CurrentV);
                    WriteVector3(writer, surface.CurrentNormal);
                    writer.Write(surface.Radius);
                    WriteVector2(writer, surface.LatitudeRange);
                    WriteVector2(writer, surface.LongitudeRange);
                    writer.Write((int)surface.WrapDirection);
                    writer.Write((int)surface.PoleMode);
                    writer.Write((int)surface.SubdivisionMode);
                    writer.Write(surface.NorthPoleTopologyVertexId);
                    writer.Write(surface.SouthPoleTopologyVertexId);
                    writer.Write(surface.MaximumRadiusError);
                    writer.Write(surface.MinimumAreaStretchRatio);
                    writer.Write(surface.MaximumAreaStretchRatio);
                    writer.Write(surface.AverageAreaStretchRatio);
                    writer.Write(surface.SurfaceVertexIndices.Count);
                    for (int vertexIndex = 0;
                        vertexIndex < surface.SurfaceVertexIndices.Count;
                        vertexIndex++)
                    {
                        writer.Write(surface.SurfaceVertexIndices[vertexIndex]);
                    }
                }

                writer.Flush();
                return HashBytes(stream.ToArray());
            }
        }

        private static string HashDiagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(
                stream,
                new UTF8Encoding(false),
                true))
            {
                writer.Write(diagnostics.Count);
                for (int i = 0; i < diagnostics.Count; i++)
                {
                    FoldCanvasDiagnostic diagnostic = diagnostics[i];
                    writer.Write(diagnostic.Code ?? string.Empty);
                    writer.Write((int)diagnostic.Severity);
                    writer.Write(diagnostic.Message ?? string.Empty);
                    writer.Write(diagnostic.PanelId ?? string.Empty);
                    writer.Write(diagnostic.OperationId ?? string.Empty);
                    writer.Write(diagnostic.SeamId ?? string.Empty);
                    writer.Write(diagnostic.BoundaryId ?? string.Empty);
                    writer.Write(diagnostic.Values.Count);
                    for (int valueIndex = 0;
                        valueIndex < diagnostic.Values.Count;
                        valueIndex++)
                    {
                        FoldCanvasDiagnosticValue value =
                            diagnostic.Values[valueIndex];
                        writer.Write(value.Key);
                        writer.Write(value.Value);
                        writer.Write(value.Unit ?? string.Empty);
                    }

                    writer.Write(diagnostic.RepairSuggestions.Count);
                    for (int suggestionIndex = 0;
                        suggestionIndex < diagnostic.RepairSuggestions.Count;
                        suggestionIndex++)
                    {
                        writer.Write(
                            diagnostic.RepairSuggestions[suggestionIndex]);
                    }

                    FoldCanvasDiagnosticGeometryContext context =
                        diagnostic.GeometryContext;
                    writer.Write(context != null);
                    if (context != null)
                    {
                        writer.Write(context.VertexIndex);
                        writer.Write(context.TopologyVertexId);
                        writer.Write(context.ComponentIndex);
                        writer.Write(context.TriangleIndex);
                        writer.Write(context.RelatedTriangleIndex);
                        writer.Write(context.EdgeTopologyVertexA);
                        writer.Write(context.EdgeTopologyVertexB);
                    }
                }

                writer.Flush();
                return HashBytes(stream.ToArray());
            }
        }

        private static string FirstErrorCode(FoldCanvasCompileResult result)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Severity ==
                    FoldCanvasDiagnosticSeverity.Error)
                {
                    return result.Diagnostics[i].Code;
                }
            }

            return string.Empty;
        }

        private static string BaselinePath()
        {
            PackageManagerInfo package = PackageManagerInfo.FindForAssembly(
                typeof(FoldCanvasCompiler).Assembly);
            if (package == null)
            {
                throw new FileNotFoundException(
                    "Could not resolve the FoldCanvas package root.");
            }

            return Path.Combine(
                package.resolvedPath,
                BaselineRelativePath);
        }

        private static string WriteDerivedReport(
            FoldCanvasProductionCorpusDocument report)
        {
            string path = DerivedReportPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                Serialize(report),
                new UTF8Encoding(false));
            return path;
        }

        private static string DerivedReportPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)
                ?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidDataException(
                    "Could not resolve the Unity project root.");
            }

            return Path.Combine(
                projectRoot,
                "M11Evidence",
                "production-corpus-report.json");
        }

        private static void ValidateDocumentHeader(
            FoldCanvasProductionCorpusDocument document,
            string expectedFormat)
        {
            if (!string.Equals(
                    document.format,
                    expectedFormat,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    document.version,
                    CorpusVersion,
                    StringComparison.Ordinal) ||
                document.cases == null ||
                document.cases.Count == 0)
            {
                throw new InvalidDataException(
                    "The M11 production corpus baseline is invalid.");
            }
        }

        private static void CompareValue(
            StringBuilder builder,
            string field,
            string expected,
            string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                builder.Append(field);
                builder.Append(": expected '");
                builder.Append(expected);
                builder.Append("', actual '");
                builder.Append(actual);
                builder.AppendLine("'");
            }
        }

        private static string JoinDiagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            return string.Join(
                " | ",
                diagnostics.Select(diagnostic => diagnostic.ToString()));
        }

        private static string HashText(string text)
        {
            return HashBytes(Encoding.UTF8.GetBytes(text ?? string.Empty));
        }

        private static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++)
                {
                    builder.Append(digest[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static void WriteVector2(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        private static void WriteRect(BinaryWriter writer, Rect value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.width);
            writer.Write(value.height);
        }

        private static void DestroyCompileResult(
            FoldCanvasCompileResult result)
        {
            if (result?.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }
        }
    }

    public static partial class FoldCanvasSampleCreator
    {
        internal static FoldCanvasAsset CreateM11CorpusAsset(
            string id,
            out FoldCanvasOperationRegistry registry,
            out string explicitSourceIdentity)
        {
            registry = null;
            explicitSourceIdentity = null;
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            switch (id)
            {
                case "planar-artwork":
                    asset.Panels.Add(PanelDefinition.CreateRectangle(
                        "artwork",
                        new Rect(0f, 0f, 1f, 1f),
                        new Vector2(2f, 1f),
                        8,
                        4));
                    return asset;
                case "production-cup":
                    ConfigureM03CupAsset(asset, null);
                    SolidifyOperationDefinition solidify =
                        new SolidifyOperationDefinition
                        {
                            Id = "solidify-cup",
                            Thickness = M04Thickness,
                            Direction = SolidifyDirection.Inward,
                        };
                    solidify.PanelIds.Add("wall");
                    solidify.PanelIds.Add("bottom");
                    asset.Operations.Add(solidify);
                    return asset;
                case "sphere-gores":
                    ConfigureM05SphereAsset(asset, null);
                    return asset;
                case "cyclic-torus":
                    ConfigureM09TorusAsset(asset, null);
                    return asset;
                case "registered-wave":
                    asset.Panels.Add(PanelDefinition.CreateRectangle(
                        "surface",
                        new Rect(0f, 0f, 1f, 1f),
                        new Vector2(2.4f, 1.4f),
                        24,
                        12));
                    asset.Operations.Add(new M10WaveOperationDefinition
                    {
                        Id = "wave-surface",
                        PanelId = "surface",
                        Amplitude = 0.28f,
                        Cycles = 2.5f,
                    });
                    registry = CreateM10Registry();
                    explicitSourceIdentity =
                        "native-extension-descriptor/1\n" +
                        "operationTypeId=dev.foldcanvas.samples.wave\n" +
                        "panel=surface|rectangle|2.4|1.4|24|12\n" +
                        "operation=wave-surface|amplitude=0.28|cycles=2.5\n" +
                        "compile=Standard|weldEpsilon=0.00001|" +
                        "recalculateNormals=true|vertices=1000000|" +
                        "triangles=2000000\n";
                    return asset;
                case "off-grid-fold":
                    asset.Panels.Add(PanelDefinition.CreateRectangle(
                        "panel",
                        new Rect(0f, 0f, 1f, 1f),
                        Vector2.one,
                        1,
                        1));
                    asset.Operations.Add(new FoldLineOperationDefinition
                    {
                        Id = "off-grid-fold",
                        PanelId = "panel",
                        LineStart = new Vector2(0.3f, 0f),
                        LineEnd = new Vector2(0.3f, 1f),
                        Side = FoldSide.Positive,
                        AngleDegrees = 90f,
                    });
                    return asset;
                default:
                    UnityEngine.Object.DestroyImmediate(asset);
                    throw new InvalidDataException(
                        "Unknown M11 corpus case '" + id + "'.");
            }
        }
    }
}
