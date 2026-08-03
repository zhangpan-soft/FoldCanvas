using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M09ToroidalTopologyTests
    {
        private const float PositionTolerance = 0.00002f;
        private const float MajorRadius = 1.25f;
        private const float MinorRadius = 0.35f;

        [Test]
        public void ToroidalWrap_RectangleMapsToStableMajorAndMinorRadii()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                Vector3 position = result.CompiledData.Vertices[i].Position;
                float radial = Mathf.Sqrt(
                    position.x * position.x +
                    position.z * position.z);
                float tubeRadius = Mathf.Sqrt(
                    (radial - MajorRadius) *
                    (radial - MajorRadius) +
                    position.y * position.y);
                Assert.That(
                    tubeRadius,
                    Is.EqualTo(MinorRadius).Within(PositionTolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_AfterRigidTransformPreservesCurrentFrame()
        {
            FoldCanvasAsset baselineAsset = CreateTorusAsset(false);
            FoldCanvasAsset asset = CreateTorusAsset(false);
            Vector3 translation = new Vector3(2f, -1f, 0.75f);
            Quaternion rotation = Quaternion.Euler(17f, -31f, 22f);
            asset.Operations.Insert(0, new RigidTransformOperationDefinition
            {
                Id = "place-torus",
                PanelId = "torus",
                Translation = translation,
                RotationEuler = rotation.eulerAngles,
                Scale = Vector3.one
            });

            FoldCanvasCompileResult baseline =
                FoldCanvasCompiler.Compile(baselineAsset);
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(baseline.Success, Is.True, Diagnostics(baseline));
            Assert.That(result.Success, Is.True, Diagnostics(result));
            for (int i = 0; i < result.CompiledData.Vertices.Count; i++)
            {
                Vector3 expected = translation + rotation *
                    baseline.CompiledData.Vertices[i].Position;
                Assert.That(
                    Vector3.Distance(
                        result.CompiledData.Vertices[i].Position,
                        expected),
                    Is.LessThan(PositionTolerance));
            }

            Destroy(baseline, baselineAsset);
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_FullCyclesRequireThreeSegmentsPerAxis()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false, 2, 12);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .InsufficientToroidalTessellation));
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_MultiTurnReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false);
            GetWrap(asset).MajorAngleRange = new Vector2(0f, 720f);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                first.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .UnsupportedToroidalMultiTurn));
            Assert.That(
                second.Diagnostics[0].Code,
                Is.EqualTo(first.Diagnostics[0].Code));
            CollectionAssert.AreEqual(
                DiagnosticValues(first.Diagnostics[0]),
                DiagnosticValues(second.Diagnostics[0]));
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void ToroidalWrap_InvalidRadiiReturnStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false);
            GetWrap(asset).MajorRadius = MinorRadius;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.InvalidToroidalRadius));
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_InvalidDirectionReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false);
            GetWrap(asset).WrapDirection =
                (ToroidalWrapDirection)int.MaxValue;

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Mesh, Is.Null);
            Assert.That(first.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(
                first.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .InvalidToroidalWrapDirection));
            Assert.That(
                second.Diagnostics[0].Code,
                Is.EqualTo(first.Diagnostics[0].Code));
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        [Test]
        public void ToroidalWrap_NonPlanarEmbeddingReturnsStableDiagnostic()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false);
            asset.Operations.Insert(0, new FoldLineOperationDefinition
            {
                Id = "bend-first",
                PanelId = "torus",
                LineStart = new Vector2(0.5f, 0f),
                LineEnd = new Vector2(0.5f, 1f),
                Side = FoldSide.Positive,
                AngleDegrees = 20f
            });

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(
                FindDiagnostic(
                    result,
                    FoldCanvasDiagnosticCodes
                        .UnsupportedToroidalEmbedding),
                Is.Not.Null,
                Diagnostics(result));
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_MajorAlongVClosesBothCycles()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true, 12, 24);
            GetWrap(asset).WrapDirection =
                ToroidalWrapDirection.MajorAlongV;

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(EulerCharacteristic(result), Is.Zero);
            Assert.That(result.GeometryValidationReport.OpenEdgeCount, Is.Zero);
            Assert.That(
                result.GeometryValidationReport.NonManifoldEdgeCount,
                Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_NegativeFullTurnsRemainOutwardAndClosed()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);
            ToroidalWrapOperationDefinition wrap = GetWrap(asset);
            wrap.MajorAngleRange = new Vector2(360f, 0f);
            wrap.MinorAngleRange = new Vector2(360f, 0f);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(EulerCharacteristic(result), Is.Zero);
            Assert.That(
                result.GeometryValidationReport
                    .OrientationConflictEdgeCount,
                Is.Zero);
            Assert.That(
                result.ClosedVolumeReport.SignedVolume,
                Is.GreaterThan(0d));
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_FullTurnsRemainOpenWithoutExplicitStitch()
        {
            FoldCanvasAsset asset = CreateTorusAsset(false);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                result.GeometryValidationReport.OpenEdgeCount,
                Is.GreaterThan(0));
            Assert.That(
                result.CompiledData.TopologyVertexCount,
                Is.EqualTo(result.CompiledData.Vertices.Count));
            Destroy(result, asset);
        }

        [Test]
        public void ToroidalWrap_AfterSelectedStitchReturnsOrderingDiagnostic()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);
            FoldOperationDefinition wrap = asset.Operations[0];
            asset.Operations.RemoveAt(0);
            asset.Operations.Add(wrap);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Mesh, Is.Null);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes
                        .StitchMustBeTerminalForSelectedPanels));
            Destroy(result, asset);
        }

        [Test]
        public void Torus_TwoExplicitWeldsProduceEulerCharacteristicZero()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(EulerCharacteristic(result), Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void Torus_HasNoOpenOrNonManifoldTopologyEdges()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.GeometryValidationReport.OpenEdgeCount, Is.Zero);
            Assert.That(
                result.GeometryValidationReport.NonManifoldEdgeCount,
                Is.Zero);
            Assert.That(
                result.GeometryValidationReport
                    .OrientationConflictEdgeCount,
                Is.Zero);
            Assert.That(
                result.ClosedVolumeReport.IsSingleClosedVolume,
                Is.True);
            Destroy(result, asset);
        }

        [Test]
        public void Torus_HasOutwardWinding()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = Vertex(result, triangles[i]);
                Vector3 b = Vertex(result, triangles[i + 1]);
                Vector3 c = Vertex(result, triangles[i + 2]);
                Vector3 centroid = (a + b + c) / 3f;
                Vector3 radial = new Vector3(
                    centroid.x,
                    0f,
                    centroid.z).normalized;
                Vector3 tubeCenter = MajorRadius * radial;
                Vector3 outward = (centroid - tubeCenter).normalized;
                Assert.That(
                    Vector3.Dot(Vector3.Cross(b - a, c - a), outward),
                    Is.GreaterThan(0f),
                    $"Triangle {i / 3} faces inward.");
            }

            Destroy(result, asset);
        }

        [Test]
        public void Torus_UvSeamsRetainDistinctRenderVerticesAndSharedTopology()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertUvSplitAndTopologyWeld(result, "uMin", "uMax");
            AssertUvSplitAndTopologyWeld(result, "vMin", "vMax");
            Destroy(result, asset);
        }

        [Test]
        public void Torus_RepeatedCompilesAreDeterministic()
        {
            FoldCanvasAsset asset = CreateTorusAsset(true);

            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            CollectionAssert.AreEqual(
                first.CompiledData.Vertices,
                second.CompiledData.Vertices);
            CollectionAssert.AreEqual(
                first.CompiledData.TriangleIndices,
                second.CompiledData.TriangleIndices);
            Assert.That(
                EulerCharacteristic(second),
                Is.EqualTo(EulerCharacteristic(first)));
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        private static FoldCanvasAsset CreateTorusAsset(
            bool closeCycles,
            int uSegments = 24,
            int vSegments = 12)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M09Torus";
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "torus",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(
                    2f * Mathf.PI * MajorRadius,
                    2f * Mathf.PI * MinorRadius),
                uSegments,
                vSegments));
            asset.Operations.Add(new ToroidalWrapOperationDefinition
            {
                Id = "wrap-torus",
                PanelId = "torus",
                MajorRadius = MajorRadius,
                MinorRadius = MinorRadius,
                MajorAngleRange = new Vector2(0f, 360f),
                MinorAngleRange = new Vector2(0f, 360f),
                WrapDirection = ToroidalWrapDirection.MajorAlongU
            });

            if (closeCycles)
            {
                asset.Seams.Add(new SeamDefinition
                {
                    Id = "close-major",
                    A = new BoundaryReference("torus", "uMin"),
                    B = new BoundaryReference("torus", "uMax"),
                    Mode = SeamMode.Weld
                });
                asset.Seams.Add(new SeamDefinition
                {
                    Id = "close-minor",
                    A = new BoundaryReference("torus", "vMin"),
                    B = new BoundaryReference("torus", "vMax"),
                    Mode = SeamMode.Weld
                });
                StitchOperationDefinition stitch =
                    new StitchOperationDefinition
                    {
                        Id = "close-torus"
                    };
                stitch.SeamIds.Add("close-major");
                stitch.SeamIds.Add("close-minor");
                asset.Operations.Add(stitch);
            }

            return asset;
        }

        private static ToroidalWrapOperationDefinition GetWrap(
            FoldCanvasAsset asset)
        {
            for (int i = 0; i < asset.Operations.Count; i++)
            {
                if (asset.Operations[i] is
                    ToroidalWrapOperationDefinition wrap)
                {
                    return wrap;
                }
            }

            throw new InvalidOperationException("Missing ToroidalWrap.");
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

        private static void AssertUvSplitAndTopologyWeld(
            FoldCanvasCompileResult result,
            string firstId,
            string secondId)
        {
            IReadOnlyList<int> first = result.CompiledData.GetPanel("torus")
                .GetBoundary(firstId).VertexIndices;
            IReadOnlyList<int> second = result.CompiledData.GetPanel("torus")
                .GetBoundary(secondId).VertexIndices;
            Assert.That(first.Count, Is.EqualTo(second.Count));
            for (int i = 0; i < first.Count; i++)
            {
                FoldCanvasCompiledVertex a =
                    result.CompiledData.Vertices[first[i]];
                FoldCanvasCompiledVertex b =
                    result.CompiledData.Vertices[second[i]];
                Assert.That(a.TopologyVertexId, Is.EqualTo(b.TopologyVertexId));
                Assert.That(a.SourceUv, Is.Not.EqualTo(b.SourceUv));
                Assert.That(first[i], Is.Not.EqualTo(second[i]));
            }
        }

        private static Vector3 Vertex(
            FoldCanvasCompileResult result,
            int index)
        {
            return result.CompiledData.Vertices[index].Position;
        }

        private static int Topology(
            FoldCanvasCompileResult result,
            int vertexIndex)
        {
            return result.CompiledData.Vertices[vertexIndex]
                .TopologyVertexId;
        }

        private static FoldCanvasDiagnostic FindDiagnostic(
            FoldCanvasCompileResult result,
            string code)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Code == code)
                {
                    return result.Diagnostics[i];
                }
            }

            return null;
        }

        private static string[] DiagnosticValues(
            FoldCanvasDiagnostic diagnostic)
        {
            string[] values = new string[diagnostic.Values.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = diagnostic.Values[i].Key + "=" +
                    diagnostic.Values[i].Value.ToString("R");
            }

            return values;
        }

        private static string Diagnostics(FoldCanvasCompileResult result)
        {
            string message = string.Empty;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                message += result.Diagnostics[i].Code + ": " +
                    result.Diagnostics[i].Message + "\n";
            }

            return message;
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset,
            bool destroyAsset = true)
        {
            if (result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }

            if (destroyAsset)
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
