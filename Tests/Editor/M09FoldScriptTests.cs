using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M09FoldScriptTests
    {
        [Test]
        public void FoldScript_RoundTripPreservesBoundarySpanAndToroidalWrap()
        {
            FoldScriptReadResult read = FoldScriptSerializer.Read(
                TorusJson("meter", 1.25f, 0.35f, true));

            FoldScriptWriteResult write =
                FoldScriptSerializer.Write(read.Document);
            FoldScriptReadResult second =
                FoldScriptSerializer.Read(write.Json);

            Assert.That(read.Success, Is.True, Diagnostics(read.Diagnostics));
            Assert.That(write.Success, Is.True, Diagnostics(write.Diagnostics));
            Assert.That(second.Success, Is.True, Diagnostics(second.Diagnostics));
            Assert.That(write.Json, Does.Contain("\"type\": \"toroidalWrap\""));
            Assert.That(write.Json, Does.Contain("\"span\": [0.125,0.375]"));
            Assert.That(second.Document.Seams[0].A.UseSpan, Is.True);
            Assert.That(
                second.Document.Seams[0].A.Span,
                Is.EqualTo(new Vector2(0.125f, 0.375f)));
            Assert.That(
                second.Document.Operations[0],
                Is.TypeOf<FoldScriptToroidalWrapOperation>());
        }

        [Test]
        public void FoldScript_ImportedTorusCompilesClosedEulerZero()
        {
            FoldScriptImportResult import = FoldScriptImporter.Import(
                TorusJson("meter", 1.25f, 0.35f, false));

            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(import.Asset);

            Assert.That(import.Success, Is.True, Diagnostics(import.Diagnostics));
            Assert.That(compile.Success, Is.True, CompileDiagnostics(compile));
            Assert.That(compile.GeometryValidationReport.OpenEdgeCount, Is.Zero);
            Assert.That(
                compile.GeometryValidationReport.NonManifoldEdgeCount,
                Is.Zero);
            Assert.That(EulerCharacteristic(compile), Is.Zero);
            Destroy(compile, import.Asset);
        }

        [Test]
        public void FoldScript_ToroidalRadiiFollowDocumentUnits()
        {
            FoldScriptImportResult import = FoldScriptImporter.Import(
                TorusJson("centimeter", 125f, 35f, false));

            ToroidalWrapOperationDefinition wrap =
                (ToroidalWrapOperationDefinition)
                    import.Asset.Operations[0];
            Assert.That(wrap.MajorRadius, Is.EqualTo(1.25f).Within(1e-6f));
            Assert.That(wrap.MinorRadius, Is.EqualTo(0.35f).Within(1e-6f));
            FoldScriptDocument exported =
                FoldScriptAssetConverter.ToDocument(import.Asset);
            FoldScriptToroidalWrapOperation exportedWrap =
                (FoldScriptToroidalWrapOperation)exported.Operations[0];
            Assert.That(
                exportedWrap.MajorRadius,
                Is.EqualTo(125f).Within(0.0001f));
            Assert.That(
                exportedWrap.MinorRadius,
                Is.EqualTo(35f).Within(0.0001f));
            UnityEngine.Object.DestroyImmediate(import.Asset);
        }

        [Test]
        public void FoldScript_InvalidBoundarySpanReturnsStableDiagnostic()
        {
            string json = TorusJson("meter", 1.25f, 0.35f, true)
                .Replace("[0.125,0.375]", "[0.8,0.2]");

            FoldScriptReadResult first = FoldScriptSerializer.Read(json);
            FoldScriptReadResult second = FoldScriptSerializer.Read(json);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                first.Diagnostics[0].Code,
                Is.EqualTo(FoldCanvasDiagnosticCodes.InvalidBoundarySpan));
            Assert.That(
                second.Diagnostics[0].Code,
                Is.EqualTo(first.Diagnostics[0].Code));
            Assert.That(
                second.Diagnostics[0].Message,
                Is.EqualTo(first.Diagnostics[0].Message));
        }

        private static string TorusJson(
            string units,
            float majorRadius,
            float minorRadius,
            bool includeSpan)
        {
            string a = includeSpan
                ? "{\"panel\":\"torus\",\"boundary\":\"uMin\",\"span\":[0.125,0.375]}"
                : "{\"panel\":\"torus\",\"boundary\":\"uMin\"}";
            string scale = units == "centimeter" ? "100" : "1";
            return "{" +
                "\"schemaVersion\":\"0.1\"," +
                "\"assetId\":\"m09-torus\"," +
                "\"displayName\":\"M09 Torus\"," +
                "\"units\":\"" + units + "\"," +
                "\"canvas\":{\"appearance\":\"m09.png\"," +
                    "\"width\":1024,\"height\":512}," +
                "\"panels\":[{\"id\":\"torus\"," +
                    "\"shape\":\"rectangle\"," +
                    "\"canvasRect\":[0,0,1,1]," +
                    "\"physicalSize\":[" + scale + "," + scale + "]," +
                    "\"tessellation\":{\"uSegments\":24," +
                        "\"vSegments\":12}}]," +
                "\"seams\":[{" +
                    "\"id\":\"close-major\",\"a\":" + a + "," +
                    "\"b\":{\"panel\":\"torus\"," +
                        "\"boundary\":\"uMax\"}," +
                    "\"mode\":\"weld\",\"reverseB\":false," +
                    "\"sampleCount\":0},{" +
                    "\"id\":\"close-minor\"," +
                    "\"a\":{\"panel\":\"torus\"," +
                        "\"boundary\":\"vMin\"}," +
                    "\"b\":{\"panel\":\"torus\"," +
                        "\"boundary\":\"vMax\"}," +
                    "\"mode\":\"weld\",\"reverseB\":false," +
                    "\"sampleCount\":0}]," +
                "\"operations\":[{" +
                    "\"id\":\"wrap\",\"type\":\"toroidalWrap\"," +
                    "\"panel\":\"torus\"," +
                    "\"majorRadius\":" + majorRadius.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "," +
                    "\"minorRadius\":" + minorRadius.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) + "," +
                    "\"majorAngleRange\":[0,360]," +
                    "\"minorAngleRange\":[0,360]," +
                    "\"wrapDirection\":\"majorAlongU\"},{" +
                    "\"id\":\"stitch\",\"type\":\"stitch\"," +
                    "\"seams\":[\"close-major\",\"close-minor\"]}]," +
                "\"compile\":{\"weldEpsilon\":0.00001," +
                    "\"recalculateNormals\":true," +
                    "\"validationLevel\":\"standard\"," +
                    "\"maxGeneratedVertices\":1000000," +
                    "\"maxGeneratedTriangles\":2000000}}";
        }

        private static int EulerCharacteristic(FoldCanvasCompileResult result)
        {
            HashSet<int> vertices = new HashSet<int>();
            HashSet<Edge> edges = new HashSet<Edge>();
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = Topology(result, triangles[i]);
                int b = Topology(result, triangles[i + 1]);
                int c = Topology(result, triangles[i + 2]);
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                edges.Add(new Edge(a, b));
                edges.Add(new Edge(b, c));
                edges.Add(new Edge(c, a));
            }

            return vertices.Count - edges.Count + triangles.Count / 3;
        }

        private static int Topology(
            FoldCanvasCompileResult result,
            int vertexIndex)
        {
            return result.CompiledData.Vertices[vertexIndex]
                .TopologyVertexId;
        }

        private static string Diagnostics(
            IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            string message = string.Empty;
            for (int i = 0; i < diagnostics.Count; i++)
            {
                message += diagnostics[i].Code + ": " +
                    diagnostics[i].Message + "\n";
            }

            return message;
        }

        private static string CompileDiagnostics(
            FoldCanvasCompileResult result)
        {
            return Diagnostics(result.Diagnostics);
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset)
        {
            if (result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }

            if (asset != null)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private readonly struct Edge : IEquatable<Edge>
        {
            public Edge(int first, int second)
            {
                Minimum = Math.Min(first, second);
                Maximum = Math.Max(first, second);
            }

            private int Minimum { get; }

            private int Maximum { get; }

            public bool Equals(Edge other)
            {
                return Minimum == other.Minimum &&
                    Maximum == other.Maximum;
            }

            public override bool Equals(object obj)
            {
                return obj is Edge other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Minimum * 397) ^ Maximum;
                }
            }
        }
    }
}
