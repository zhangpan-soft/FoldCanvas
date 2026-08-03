using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M09HandleCupTopologyTests
    {
        private const int CupSegments = 24;
        private const float CupRadius = 0.6f;
        private const float CupHeight = 0.9f;
        private const float Thickness = 0.025f;
        private const float Tolerance = 0.00002f;

        [Test]
        public void HandleCup_AttachmentSpansHaveZeroPositionGap()
        {
            FoldCanvasAsset asset = CreateHandleCupAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            AssertAttachment(result, false);
            AssertAttachment(result, true);
            Destroy(result, asset);
        }

        [Test]
        public void HandleCup_AttachmentEdgesHaveTwoOppositeUses()
        {
            FoldCanvasAsset asset = CreateHandleCupAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Dictionary<Edge, List<DirectedEdge>> incidence =
                BuildTopologyEdgeIncidence(result);
            AssertAttachmentEdgeUses(result, incidence, false);
            AssertAttachmentEdgeUses(result, incidence, true);
            Destroy(result, asset);
        }

        [Test]
        public void HandleCup_SolidifyProducesOneClosedVolume()
        {
            FoldCanvasAsset asset = CreateHandleCupAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                result.ClosedVolumeReport.IsSingleClosedVolume,
                Is.True);
            Assert.That(result.ClosedVolumeReport.ComponentCount, Is.EqualTo(1));
            Assert.That(result.ClosedVolumeReport.OpenEdgeCount, Is.Zero);
            Assert.That(
                result.ClosedVolumeReport.NonManifoldEdgeCount,
                Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void HandleCup_HasNoOpenOrNonManifoldTopologyEdges()
        {
            FoldCanvasAsset asset = CreateHandleCupAsset();

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
                result.GeometryValidationReport.TopologyPositionConflictCount,
                Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void HandleCup_HasNonZeroOrientedVolume()
        {
            FoldCanvasAsset asset = CreateHandleCupAsset();

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                result.ClosedVolumeReport.TotalAbsoluteVolume,
                Is.GreaterThan(0.001d));
            Assert.That(
                result.ClosedVolumeReport.ZeroVolumeComponentCount,
                Is.Zero);
            Destroy(result, asset);
        }

        [Test]
        public void HandleCup_RepeatedCompilesAreDeterministic()
        {
            FoldCanvasAsset asset = CreateHandleCupAsset();

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
                second.ClosedVolumeReport.TopologyVertexCount,
                Is.EqualTo(
                    first.ClosedVolumeReport.TopologyVertexCount));
            Destroy(first, asset, false);
            Destroy(second, asset);
        }

        internal static FoldCanvasAsset CreateHandleCupAsset()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M09HandleCup";
            float delta = 2f * Mathf.PI / CupSegments;
            float handleWidth =
                2f * CupRadius * Mathf.Sin(delta * 0.5f);
            float legLength =
                2f * CupRadius * Mathf.Cos(delta * 0.5f);

            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0f, 0.6f, 1f, 0.35f),
                new Vector2(
                    2f * Mathf.PI * CupRadius,
                    CupHeight),
                CupSegments,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.05f, 0.05f, 0.35f, 0.35f),
                Vector2.one * (2f * CupRadius),
                CupSegments,
                4));
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "handle",
                new Rect(0.55f, 0.05f, 0.12f, 0.45f),
                new Vector2(handleWidth, 3f * legLength),
                1,
                3));

            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-wall",
                A = new BoundaryReference("wall", "uMin"),
                B = new BoundaryReference("wall", "uMax"),
                Mode = SeamMode.Weld
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-bottom",
                A = new BoundaryReference("wall", "vMin"),
                B = new BoundaryReference("bottom", "perimeter"),
                Mode = SeamMode.Weld
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-handle-a",
                A = new BoundaryReference("handle", "vMin"),
                B = new BoundaryReference(
                    "wall",
                    "vMax",
                    new Vector2(0f, 1f / CupSegments)),
                Mode = SeamMode.Weld,
                ReverseB = true
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-handle-b",
                A = new BoundaryReference("handle", "vMax"),
                B = new BoundaryReference(
                    "wall",
                    "vMax",
                    new Vector2(
                        0.5f,
                        0.5f + 1f / CupSegments)),
                Mode = SeamMode.Weld
            });

            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll-wall",
                PanelId = "wall",
                Direction = RollDirection.U,
                AngleDegrees = 360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                StartAngleDegrees = 180f
            });
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-bottom",
                PanelId = "bottom",
                Translation = new Vector3(
                    0f,
                    -CupHeight * 0.5f,
                    0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
                Scale = Vector3.one
            });

            Vector3 a0 = new Vector3(
                CupRadius,
                CupHeight * 0.5f,
                0f);
            Vector3 a1 = new Vector3(
                CupRadius * Mathf.Cos(delta),
                CupHeight * 0.5f,
                CupRadius * Mathf.Sin(delta));
            Vector3 currentU = (a0 - a1).normalized;
            Vector3 currentNormal = Vector3.Cross(
                currentU,
                Vector3.up).normalized;
            Vector3 firstMidpoint = (a0 + a1) * 0.5f;
            Quaternion handleRotation = Quaternion.LookRotation(
                currentNormal,
                Vector3.up);
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place-handle",
                PanelId = "handle",
                Translation = firstMidpoint +
                    Vector3.up * (1.5f * legLength),
                RotationEuler = handleRotation.eulerAngles,
                Scale = Vector3.one
            });
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "fold-handle-a",
                PanelId = "handle",
                LineStart = new Vector2(0f, 1f / 3f),
                LineEnd = new Vector2(1f, 1f / 3f),
                Side = FoldSide.Positive,
                AngleDegrees = -90f
            });
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "fold-handle-b",
                PanelId = "handle",
                LineStart = new Vector2(0f, 2f / 3f),
                LineEnd = new Vector2(1f, 2f / 3f),
                Side = FoldSide.Positive,
                AngleDegrees = -90f
            });

            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-cup-handle"
                };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            stitch.SeamIds.Add("attach-handle-a");
            stitch.SeamIds.Add("attach-handle-b");
            asset.Operations.Add(stitch);

            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-cup-handle",
                    Thickness = Thickness,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("wall");
            solidify.PanelIds.Add("bottom");
            solidify.PanelIds.Add("handle");
            asset.Operations.Add(solidify);
            return asset;
        }

        private static void AssertAttachment(
            FoldCanvasCompileResult result,
            bool second)
        {
            IReadOnlyList<int> handle = result.CompiledData
                .GetPanel("handle")
                .GetBoundary(second ? "vMax" : "vMin")
                .VertexIndices;
            IReadOnlyList<int> rim = result.CompiledData
                .GetPanel("wall")
                .GetBoundary("vMax")
                .VertexIndices;
            int wallStart = second ? CupSegments / 2 : 0;
            int firstWall = rim[wallStart];
            int secondWall = rim[wallStart + 1];
            if (!second)
            {
                int swap = firstWall;
                firstWall = secondWall;
                secondWall = swap;
            }

            AssertPair(result, handle[0], firstWall);
            AssertPair(result, handle[handle.Count - 1], secondWall);
        }

        private static void AssertPair(
            FoldCanvasCompileResult result,
            int first,
            int second)
        {
            FoldCanvasCompiledVertex a =
                result.CompiledData.Vertices[first];
            FoldCanvasCompiledVertex b =
                result.CompiledData.Vertices[second];
            Assert.That(
                Vector3.Distance(a.Position, b.Position),
                Is.LessThan(Tolerance));
            Assert.That(a.TopologyVertexId, Is.EqualTo(b.TopologyVertexId));
        }

        private static void AssertAttachmentEdgeUses(
            FoldCanvasCompileResult result,
            Dictionary<Edge, List<DirectedEdge>> incidence,
            bool second)
        {
            IReadOnlyList<int> handle = result.CompiledData
                .GetPanel("handle")
                .GetBoundary(second ? "vMax" : "vMin")
                .VertexIndices;
            Edge edge = new Edge(
                Topology(result, handle[0]),
                Topology(result, handle[handle.Count - 1]));
            Assert.That(incidence.ContainsKey(edge), Is.True);
            Assert.That(incidence[edge].Count, Is.EqualTo(2));
            Assert.That(
                incidence[edge][0].From,
                Is.EqualTo(incidence[edge][1].To));
            Assert.That(
                incidence[edge][0].To,
                Is.EqualTo(incidence[edge][1].From));
        }

        private static Dictionary<Edge, List<DirectedEdge>>
            BuildTopologyEdgeIncidence(FoldCanvasCompileResult result)
        {
            Dictionary<Edge, List<DirectedEdge>> incidence =
                new Dictionary<Edge, List<DirectedEdge>>();
            IReadOnlyList<int> triangles =
                result.CompiledData.TriangleIndices;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = Topology(result, triangles[i]);
                int b = Topology(result, triangles[i + 1]);
                int c = Topology(result, triangles[i + 2]);
                AddEdge(incidence, a, b);
                AddEdge(incidence, b, c);
                AddEdge(incidence, c, a);
            }

            return incidence;
        }

        private static void AddEdge(
            Dictionary<Edge, List<DirectedEdge>> incidence,
            int from,
            int to)
        {
            Edge edge = new Edge(from, to);
            if (!incidence.TryGetValue(
                    edge,
                    out List<DirectedEdge> uses))
            {
                uses = new List<DirectedEdge>();
                incidence.Add(edge, uses);
            }

            uses.Add(new DirectedEdge(from, to));
        }

        private static int Topology(
            FoldCanvasCompileResult result,
            int vertexIndex)
        {
            return result.CompiledData.Vertices[vertexIndex]
                .TopologyVertexId;
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

        private readonly struct DirectedEdge
        {
            public DirectedEdge(int from, int to)
            {
                From = from;
                To = to;
            }

            public int From { get; }

            public int To { get; }
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
