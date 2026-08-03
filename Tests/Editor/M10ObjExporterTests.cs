using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M10ObjExporterTests
    {
        [Test]
        public void ObjExporter_RepeatedExportsAreByteIdentical()
        {
            FoldCanvasAsset asset = CreateRectangle();
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(asset);

            FoldCanvasTextExportResult first =
                FoldCanvasObjExporter.Export(compile.CompiledData);
            FoldCanvasTextExportResult second =
                FoldCanvasObjExporter.Export(compile.CompiledData);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            Assert.That(second.Content, Is.EqualTo(first.Content));
            Destroy(compile, asset);
        }

        [Test]
        public void ObjExporter_IsInvariantAcrossCurrentCulture()
        {
            FoldCanvasAsset asset = CreateRectangle();
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(asset);
            CultureInfo original = CultureInfo.CurrentCulture;
            string english;
            string french;
            try
            {
                CultureInfo.CurrentCulture =
                    CultureInfo.GetCultureInfo("en-US");
                english = FoldCanvasObjExporter.Export(
                    compile.CompiledData).Content;
                CultureInfo.CurrentCulture =
                    CultureInfo.GetCultureInfo("fr-FR");
                french = FoldCanvasObjExporter.Export(
                    compile.CompiledData).Content;
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            Assert.That(french, Is.EqualTo(english));
            Assert.That(english, Does.Contain("v -1 -0.5 0\n"));
            Assert.That(english, Does.Not.Contain("-1,-"));
            Destroy(compile, asset);
        }

        [Test]
        public void ObjExporter_EmitsOrderedPositionsUvsAndFaces()
        {
            FoldCanvasAsset asset = CreateRectangle(1, 1);
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(asset);

            FoldCanvasTextExportResult export =
                FoldCanvasObjExporter.Export(
                    compile.CompiledData,
                    new FoldCanvasObjExportOptions
                    {
                        ObjectName = "Proof Surface"
                    });

            Assert.That(export.Success, Is.True, Diagnostics(export));
            string[] lines = export.Content.Split('\n');
            Assert.That(export.Content, Does.Contain("o Proof_Surface\n"));
            Assert.That(CountPrefix(lines, "v "), Is.EqualTo(4));
            Assert.That(CountPrefix(lines, "vt "), Is.EqualTo(4));
            Assert.That(CountPrefix(lines, "f "), Is.EqualTo(2));
            Assert.That(export.Content, Does.Contain("vt 0 0\n"));
            Assert.That(export.Content, Does.Contain("vt 1 1\n"));
            Assert.That(export.Content, Does.Contain("f 1/1 2/2 4/4\n"));
            Assert.That(export.Content, Does.Contain("f 1/1 4/4 3/3\n"));
            Destroy(compile, asset);
        }

        [Test]
        public void ObjExporter_PreservesDistinctUvSeamVertices()
        {
            FoldCanvasAsset asset = CreateStitchedPanels();
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompiledBoundary left = compile.CompiledData
                .GetPanel("left").GetBoundary("uMax");
            FoldCanvasCompiledBoundary right = compile.CompiledData
                .GetPanel("right").GetBoundary("uMin");

            Assert.That(
                compile.CompiledData.Vertices[left.VertexIndices[0]]
                    .TopologyVertexId,
                Is.EqualTo(
                    compile.CompiledData.Vertices[right.VertexIndices[0]]
                        .TopologyVertexId));
            Assert.That(
                compile.CompiledData.Vertices[left.VertexIndices[0]]
                    .SourceUv,
                Is.Not.EqualTo(
                    compile.CompiledData.Vertices[right.VertexIndices[0]]
                        .SourceUv));

            FoldCanvasTextExportResult export =
                FoldCanvasObjExporter.Export(compile.CompiledData);
            string[] lines = export.Content.Split('\n');

            Assert.That(CountPrefix(lines, "v "),
                Is.EqualTo(compile.CompiledData.Vertices.Count));
            Assert.That(CountPrefix(lines, "vt "),
                Is.EqualTo(compile.CompiledData.Vertices.Count));
            Assert.That(
                lines[FindTextureLineOffset(lines) + left.VertexIndices[0]],
                Is.Not.EqualTo(
                    lines[FindTextureLineOffset(lines) +
                        right.VertexIndices[0]]));
            Destroy(compile, asset);
        }

        [Test]
        public void ObjExporter_DoesNotMutateCompiledDataOrMesh()
        {
            FoldCanvasAsset asset = CreateRectangle();
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(asset);
            Vector3[] meshVertices = compile.Mesh.vertices;
            int[] meshTriangles = compile.Mesh.triangles;
            Vector2[] meshUv = compile.Mesh.uv;
            FoldCanvasCompiledVertex[] compiledVertices =
                CopyVertices(compile.CompiledData.Vertices);

            FoldCanvasObjExporter.Export(
                compile.CompiledData,
                new FoldCanvasObjExportOptions
                {
                    FlipTextureV = true,
                    IncludeMetadataComments = false
                });

            CollectionAssert.AreEqual(meshVertices, compile.Mesh.vertices);
            CollectionAssert.AreEqual(meshTriangles, compile.Mesh.triangles);
            CollectionAssert.AreEqual(meshUv, compile.Mesh.uv);
            CollectionAssert.AreEqual(
                compiledVertices,
                compile.CompiledData.Vertices);
            Destroy(compile, asset);
        }

        [Test]
        public void ObjExporter_InvalidOptionsReturnStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateRectangle();
            FoldCanvasCompileResult compile =
                FoldCanvasCompiler.Compile(asset);

            FoldCanvasTextExportResult export =
                FoldCanvasObjExporter.Export(
                    compile.CompiledData,
                    new FoldCanvasObjExportOptions
                    {
                        ObjectName = new string('x', 257)
                    });

            Assert.That(export.Success, Is.False);
            Assert.That(export.Content, Is.Null);
            Assert.That(export.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                export.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.InvalidExportOptions));
            Destroy(compile, asset);
        }

        [Test]
        public void ObjExporter_NullInputReturnsStableDiagnostic()
        {
            FoldCanvasTextExportResult export =
                FoldCanvasObjExporter.Export(null);

            Assert.That(export.Success, Is.False);
            Assert.That(export.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                export.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.ExportInputMissing));
        }

        private static FoldCanvasAsset CreateRectangle(
            int uSegments = 2,
            int vSegments = 1)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M10ObjTest";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(2f, 1f),
                uSegments,
                vSegments));
            return asset;
        }

        private static FoldCanvasAsset CreateStitchedPanels()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "left",
                new Rect(0f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                1));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "right",
                new Rect(0.6f, 0f, 0.4f, 1f),
                Vector2.one,
                1,
                1));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "join",
                A = new BoundaryReference("left", "uMax"),
                B = new BoundaryReference("right", "uMin"),
                Mode = SeamMode.Weld,
                SampleCount = 2
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-right",
                PanelId = "right",
                Translation = Vector3.right,
                Scale = Vector3.one
            });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition { Id = "stitch" };
            stitch.SeamIds.Add("join");
            asset.Operations.Add(stitch);
            return asset;
        }

        private static int CountPrefix(
            string[] lines,
            string prefix)
        {
            int count = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(
                        prefix,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int FindTextureLineOffset(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("vt ", StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static FoldCanvasCompiledVertex[] CopyVertices(
            IReadOnlyList<FoldCanvasCompiledVertex> source)
        {
            FoldCanvasCompiledVertex[] copy =
                new FoldCanvasCompiledVertex[source.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static string Diagnostics(
            FoldCanvasTextExportResult result)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static void Destroy(
            FoldCanvasCompileResult compile,
            FoldCanvasAsset asset)
        {
            if (compile.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(compile.Mesh);
            }

            UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
