using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class ClosedVolumeValidationTests
    {
        private const int CupSegments = 64;
        private const float CupRadius = 0.07f;
        private const float CupHeight = 0.12f;
        private const float Tolerance = 1e-6f;

        [Test]
        public void ClosedVolumeCup_IsSingleClosedVolume()
        {
            FoldCanvasAsset asset = CreateCupAsset();
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.ClosedVolumeReport, Is.Not.Null);
            Assert.That(
                result.ClosedVolumeReport.IsSingleClosedVolume,
                Is.True);
            Assert.That(
                result.ClosedVolumeReport.ComponentCount,
                Is.EqualTo(1));
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.EqualTo(1));
            Assert.That(
                result.SolidifyClosedVolumeReports[0]
                    .IsSingleClosedVolume,
                Is.True);
            Assert.That(
                result.SolidifyClosedVolumeReports[0].OperationId,
                Is.EqualTo("solidify-cup"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FoldCanvasClosedVolumeReport>)
                    result.SolidifyClosedVolumeReports).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<FoldCanvasClosedVolumeComponentReport>)
                    result.ClosedVolumeReport.Components).Clear());

            Destroy(result, asset);
        }

        [Test]
        public void ClosedVolumeCup_EveryLogicalEdgeHasTwoOppositeUses()
        {
            FoldCanvasAsset asset = CreateCupAsset();
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            FoldCanvasClosedVolumeReport report =
                result.ClosedVolumeReport;
            Assert.That(report.UniqueEdgeCount, Is.GreaterThan(0));
            Assert.That(
                report.TwoIncidentEdgeCount,
                Is.EqualTo(report.UniqueEdgeCount));
            Assert.That(report.OpenEdgeCount, Is.EqualTo(0));
            Assert.That(report.NonManifoldEdgeCount, Is.EqualTo(0));
            Assert.That(
                report.OrientationConflictEdgeCount,
                Is.EqualTo(0));
            Assert.That(
                report.CollapsedTopologyEdgeCount,
                Is.EqualTo(0));
            Assert.That(
                report.TopologyPositionConflictCount,
                Is.EqualTo(0));

            Destroy(result, asset);
        }

        [Test]
        public void ClosedVolumeCup_HasNonZeroOrientedVolume()
        {
            FoldCanvasAsset asset = CreateCupAsset();
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            FoldCanvasClosedVolumeReport report =
                result.ClosedVolumeReport;
            Assert.That(report.ZeroVolumeComponentCount, Is.EqualTo(0));
            Assert.That(report.TotalAbsoluteVolume, Is.GreaterThan(0d));
            Assert.That(Math.Abs(report.SignedVolume), Is.GreaterThan(0d));
            Assert.That(
                report.Components[0].AbsoluteVolume,
                Is.GreaterThan(
                    report.Components[0]
                        .MinimumAcceptedAbsoluteVolume));

            Destroy(result, asset);
        }

        [Test]
        public void ClosedVolumeCup_GeneratesWallBottomInnerAndOuterCorners()
        {
            FoldCanvasAsset asset = CreateCupAsset();
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                result.CompiledData.CornerSegments.Count,
                Is.EqualTo(CupSegments));
            AssertCornerLoop(result, true);
            AssertCornerLoop(result, false);
            for (int i = 0;
                i < result.CompiledData.CornerSegments.Count;
                i++)
            {
                FoldCanvasCompiledCornerSegment segment =
                    result.CompiledData.CornerSegments[i];
                Assert.That(
                    segment.OperationId,
                    Is.EqualTo("solidify-cup"));
                Vector3 outer =
                    result.CompiledData.Vertices[
                        segment.OuterFromVertexIndex].Position;
                Vector3 inner =
                    result.CompiledData.Vertices[
                        segment.InnerFromVertexIndex].Position;
                Assert.That(
                    Vector3.Distance(outer, inner),
                    Is.GreaterThan(Tolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void ClosedVolumeCup_WallBottomOuterAndInnerConnectionsShareTopology()
        {
            FoldCanvasAsset asset = CreateCupAsset();
            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            IReadOnlyList<int> wall =
                result.CompiledData.GetPanel("wall")
                    .GetBoundary("vMin").VertexIndices;
            IReadOnlyList<int> bottom =
                result.CompiledData.GetPanel("bottom")
                    .GetBoundary("perimeter").VertexIndices;
            Assert.That(
                wall.Count,
                Is.EqualTo(bottom.Count + 1),
                "The rectangle boundary repeats its closed endpoint; " +
                "the disk perimeter does not.");
            for (int i = 0; i < bottom.Count; i++)
            {
                FoldCanvasCompiledVertex outerWall =
                    result.CompiledData.Vertices[wall[i]];
                FoldCanvasCompiledVertex outerBottom =
                    result.CompiledData.Vertices[bottom[i]];
                Assert.That(
                    outerBottom.TopologyVertexId,
                    Is.EqualTo(outerWall.TopologyVertexId));
                Assert.That(
                    Vector3.Distance(
                        outerBottom.Position,
                        outerWall.Position),
                    Is.LessThanOrEqualTo(Tolerance));

                FoldCanvasCompiledVertex innerWall =
                    FindOtherLayerVertex(
                        result.CompiledData,
                        wall[i]);
                FoldCanvasCompiledVertex innerBottom =
                    FindOtherLayerVertex(
                        result.CompiledData,
                        bottom[i]);
                Assert.That(
                    innerBottom.TopologyVertexId,
                    Is.EqualTo(innerWall.TopologyVertexId));
                Assert.That(
                    Vector3.Distance(
                        innerBottom.Position,
                        innerWall.Position),
                    Is.LessThanOrEqualTo(Tolerance));
            }

            Destroy(result, asset);
        }

        [Test]
        public void ClosedVolumeReport_OpenPanelIsNotClosed()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1));

            FoldCanvasCompileResult result =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(
                result.ClosedVolumeReport.IsClosedVolume,
                Is.False);
            Assert.That(
                result.ClosedVolumeReport.OpenEdgeCount,
                Is.EqualTo(4));
            Assert.That(
                result.ClosedVolumeReport.ZeroVolumeComponentCount,
                Is.EqualTo(1));
            Assert.That(
                result.SolidifyClosedVolumeReports.Count,
                Is.EqualTo(0));

            Destroy(result, asset);
        }

        [Test]
        public void ClosedVolumeReport_RepeatedCompileIsDeterministic()
        {
            FoldCanvasAsset asset = CreateCupAsset();
            FoldCanvasCompileResult first =
                FoldCanvasCompiler.Compile(asset);
            FoldCanvasCompileResult second =
                FoldCanvasCompiler.Compile(asset);

            Assert.That(first.Success, Is.True, Diagnostics(first));
            Assert.That(second.Success, Is.True, Diagnostics(second));
            AssertReportsEqual(
                first.ClosedVolumeReport,
                second.ClosedVolumeReport);
            Assert.That(
                second.CompiledData.CornerSegments.Count,
                Is.EqualTo(first.CompiledData.CornerSegments.Count));
            for (int i = 0;
                i < first.CompiledData.CornerSegments.Count;
                i++)
            {
                FoldCanvasCompiledCornerSegment a =
                    first.CompiledData.CornerSegments[i];
                FoldCanvasCompiledCornerSegment b =
                    second.CompiledData.CornerSegments[i];
                Assert.That(
                    b.OperationId,
                    Is.EqualTo(a.OperationId));
                Assert.That(
                    b.OuterFromVertexIndex,
                    Is.EqualTo(a.OuterFromVertexIndex));
                Assert.That(
                    b.OuterToVertexIndex,
                    Is.EqualTo(a.OuterToVertexIndex));
                Assert.That(
                    b.InnerFromVertexIndex,
                    Is.EqualTo(a.InnerFromVertexIndex));
                Assert.That(
                    b.InnerToVertexIndex,
                    Is.EqualTo(a.InnerToVertexIndex));
            }

            Destroy(first, null);
            Destroy(second, asset);
        }

        private static void AssertCornerLoop(
            FoldCanvasCompileResult result,
            bool outer)
        {
            Dictionary<int, HashSet<int>> adjacency =
                new Dictionary<int, HashSet<int>>();
            for (int i = 0;
                i < result.CompiledData.CornerSegments.Count;
                i++)
            {
                FoldCanvasCompiledCornerSegment segment =
                    result.CompiledData.CornerSegments[i];
                int fromIndex = outer
                    ? segment.OuterFromVertexIndex
                    : segment.InnerFromVertexIndex;
                int toIndex = outer
                    ? segment.OuterToVertexIndex
                    : segment.InnerToVertexIndex;
                int from = result.CompiledData.Vertices[fromIndex]
                    .TopologyVertexId;
                int to = result.CompiledData.Vertices[toIndex]
                    .TopologyVertexId;
                AddNeighbor(adjacency, from, to);
                AddNeighbor(adjacency, to, from);
            }

            Assert.That(adjacency.Count, Is.EqualTo(CupSegments));
            foreach (KeyValuePair<int, HashSet<int>> pair in adjacency)
            {
                Assert.That(
                    pair.Value.Count,
                    Is.EqualTo(2),
                    $"corner topology vertex {pair.Key}");
            }

            HashSet<int> visited = new HashSet<int>();
            Queue<int> pending = new Queue<int>();
            foreach (int topologyId in adjacency.Keys)
            {
                pending.Enqueue(topologyId);
                break;
            }

            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                if (!visited.Add(current))
                {
                    continue;
                }

                foreach (int neighbor in adjacency[current])
                {
                    pending.Enqueue(neighbor);
                }
            }

            Assert.That(visited.Count, Is.EqualTo(adjacency.Count));
        }

        private static void AddNeighbor(
            Dictionary<int, HashSet<int>> adjacency,
            int from,
            int to)
        {
            if (!adjacency.TryGetValue(
                    from,
                    out HashSet<int> neighbors))
            {
                neighbors = new HashSet<int>();
                adjacency.Add(from, neighbors);
            }

            neighbors.Add(to);
        }

        private static FoldCanvasCompiledVertex FindOtherLayerVertex(
            FoldCanvasCompiledData data,
            int sourceVertexIndex)
        {
            FoldCanvasCompiledVertex source =
                data.Vertices[sourceVertexIndex];
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                FoldCanvasCompiledVertex candidate =
                    data.Vertices[i];
                if (candidate.PanelIndex == source.PanelIndex &&
                    candidate.ProvenanceId == source.ProvenanceId &&
                    candidate.SourcePosition == source.SourcePosition &&
                    candidate.SourceUv == source.SourceUv &&
                    candidate.TopologyVertexId !=
                        source.TopologyVertexId)
                {
                    return candidate;
                }
            }

            Assert.Fail(
                $"No other Solidify layer was found for vertex " +
                $"{sourceVertexIndex}.");
            return default;
        }

        private static void AssertReportsEqual(
            FoldCanvasClosedVolumeReport first,
            FoldCanvasClosedVolumeReport second)
        {
            Assert.That(second.TriangleCount, Is.EqualTo(first.TriangleCount));
            Assert.That(
                second.TopologyVertexCount,
                Is.EqualTo(first.TopologyVertexCount));
            Assert.That(
                second.UniqueEdgeCount,
                Is.EqualTo(first.UniqueEdgeCount));
            Assert.That(
                second.TwoIncidentEdgeCount,
                Is.EqualTo(first.TwoIncidentEdgeCount));
            Assert.That(
                second.OpenEdgeCount,
                Is.EqualTo(first.OpenEdgeCount));
            Assert.That(
                second.NonManifoldEdgeCount,
                Is.EqualTo(first.NonManifoldEdgeCount));
            Assert.That(
                second.OrientationConflictEdgeCount,
                Is.EqualTo(first.OrientationConflictEdgeCount));
            Assert.That(
                second.CollapsedTopologyEdgeCount,
                Is.EqualTo(first.CollapsedTopologyEdgeCount));
            Assert.That(
                second.TopologyPositionConflictCount,
                Is.EqualTo(first.TopologyPositionConflictCount));
            Assert.That(
                second.ComponentCount,
                Is.EqualTo(first.ComponentCount));
            Assert.That(
                second.ZeroVolumeComponentCount,
                Is.EqualTo(first.ZeroVolumeComponentCount));
            Assert.That(
                second.SignedVolume,
                Is.EqualTo(first.SignedVolume));
            Assert.That(
                second.TotalAbsoluteVolume,
                Is.EqualTo(first.TotalAbsoluteVolume));
        }

        private static FoldCanvasAsset CreateCupAsset()
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0.06f, 0.46f, 0.88f, 0.44f),
                new Vector2(
                    2f * Mathf.PI * CupRadius,
                    CupHeight),
                CupSegments,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0.32f, 0.02f, 0.36f, 0.36f),
                Vector2.one * (2f * CupRadius),
                CupSegments,
                20));
            asset.Seams.Add(new SeamDefinition
            {
                Id = "close-wall",
                A = new BoundaryReference("wall", "uMin"),
                B = new BoundaryReference("wall", "uMax"),
                Mode = SeamMode.Weld,
                SampleCount = 2
            });
            asset.Seams.Add(new SeamDefinition
            {
                Id = "attach-bottom",
                A = new BoundaryReference("wall", "vMin"),
                B = new BoundaryReference("bottom", "perimeter"),
                Mode = SeamMode.Weld,
                SampleCount = CupSegments
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
            asset.Operations.Add(
                new RigidTransformOperationDefinition
                {
                    Id = "place-bottom",
                    PanelId = "bottom",
                    Translation = new Vector3(
                        0f,
                        -CupHeight * 0.5f,
                        0f),
                    RotationEuler =
                        new Vector3(90f, 0f, 0f),
                    Scale = Vector3.one
                });
            StitchOperationDefinition stitch =
                new StitchOperationDefinition
                {
                    Id = "stitch-cup"
                };
            stitch.SeamIds.Add("close-wall");
            stitch.SeamIds.Add("attach-bottom");
            asset.Operations.Add(stitch);
            SolidifyOperationDefinition solidify =
                new SolidifyOperationDefinition
                {
                    Id = "solidify-cup",
                    Thickness = 0.004f,
                    Direction = SolidifyDirection.Inward
                };
            solidify.PanelIds.Add("wall");
            solidify.PanelIds.Add("bottom");
            asset.Operations.Add(solidify);
            return asset;
        }

        private static string Diagnostics(
            FoldCanvasCompileResult result)
        {
            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                builder.AppendLine(
                    result.Diagnostics[i].ToString());
            }

            return builder.ToString();
        }

        private static void Destroy(
            FoldCanvasCompileResult result,
            FoldCanvasAsset asset)
        {
            if (result != null && result.Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(result.Mesh);
            }

            if (asset != null)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
