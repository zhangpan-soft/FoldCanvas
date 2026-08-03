using System;
using UnityEngine;

namespace FoldCanvas.Tests
{
    internal static class M07AdversarialGeometryFixtures
    {
        public static MeshBuildBuffer CreateBuffer(
            Vector3[] positions,
            params int[] triangleIndices)
        {
            MeshBuildBuffer buffer = new MeshBuildBuffer();
            for (int i = 0; i < positions.Length; i++)
            {
                buffer.AddVertex(
                    positions[i],
                    Vector2.zero,
                    Vector2.zero,
                    -1);
            }

            for (int i = 0; i < triangleIndices.Length; i++)
            {
                buffer.Triangles.AddUnchecked(triangleIndices[i]);
            }

            return buffer;
        }

        public static MeshBuildBuffer InvalidTriangleIndex()
        {
            return CreateBuffer(
                new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up
                },
                0,
                1,
                8);
        }

        public static MeshBuildBuffer IncompleteTriangleIndexBuffer()
        {
            return CreateBuffer(
                new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up
                },
                0,
                1);
        }

        public static MeshBuildBuffer NonFiniteVertex()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(float.NaN, 0f, 0f),
                    Vector3.right,
                    Vector3.up
                },
                0,
                1,
                2);
        }

        public static MeshBuildBuffer ZeroAreaTriangle()
        {
            return CreateBuffer(
                new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.right * 2f
                },
                0,
                1,
                2);
        }

        public static MeshBuildBuffer DuplicateFace()
        {
            return CreateBuffer(
                new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up
                },
                0,
                1,
                2,
                0,
                1,
                2);
        }

        public static MeshBuildBuffer InvertedFace()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(1f, 1f, 0f),
                    new Vector3(0f, 1f, 0f)
                },
                0,
                1,
                2,
                0,
                3,
                2);
        }

        public static MeshBuildBuffer NonManifoldEdge()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, -1f, 0f),
                    new Vector3(0.5f, 0f, 1f)
                },
                0,
                1,
                2,
                1,
                0,
                3,
                0,
                1,
                4);
        }

        public static MeshBuildBuffer BowTieVertex()
        {
            return CreateBuffer(
                new[]
                {
                    Vector3.zero,
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(-1f, 0f, 0f),
                    new Vector3(0f, -1f, 0f)
                },
                0,
                1,
                2,
                0,
                3,
                4);
        }

        public static MeshBuildBuffer OpenSeam()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(1f, 1f, 0f),
                    new Vector3(0f, 1f, 0f)
                },
                0,
                1,
                2,
                0,
                2,
                3);
        }

        public static MeshBuildBuffer ZeroLengthBoundary()
        {
            MeshBuildBuffer buffer = new MeshBuildBuffer();
            PanelDefinition panel = PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                Vector2.one,
                1,
                1);
            PanelBuildRecord record = PanelTessellator.Append(
                panel,
                buffer);
            record.AddBoundary(
                "collapsed-test-boundary",
                new[] { 0, 0 });
            return buffer;
        }

        public static MeshBuildBuffer DisconnectedComponents()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(3f, 0f, 0f),
                    new Vector3(4f, 0f, 0f),
                    new Vector3(3f, 1f, 0f)
                },
                0,
                1,
                2,
                3,
                4,
                5);
        }

        public static MeshBuildBuffer WeldSeamClosureGap()
        {
            MeshBuildBuffer buffer = DisconnectedComponents();
            buffer.AddWeldValidationRecord(
                "stitch-gap",
                "open-seam",
                new[] { 0, 1 },
                new[] { 3, 4 });
            return buffer;
        }

        public static MeshBuildBuffer TopologyPositionConflict()
        {
            MeshBuildBuffer buffer = DisconnectedComponents();
            buffer.UnionTopology(0, 3);
            return buffer;
        }

        public static MeshBuildBuffer InvertedClosedTetrahedron()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, 0f, 1f)
                },
                0,
                1,
                2,
                0,
                3,
                1,
                0,
                2,
                3,
                1,
                3,
                2);
        }

        public static MeshBuildBuffer SelfIntersectingRoll()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(-1f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(0f, 2f, 0f),
                    new Vector3(-1f, 1f, 0f),
                    new Vector3(1f, 1f, 0f),
                    new Vector3(0f, -1f, 0f)
                },
                0,
                1,
                2,
                3,
                4,
                5);
        }

        public static MeshBuildBuffer ThicknessOverlap()
        {
            return CreateBuffer(
                new[]
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(1f, -1f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, -0.5f, -1f),
                    new Vector3(0f, -0.5f, 1f),
                    new Vector3(0f, 1.5f, 0f)
                },
                0,
                1,
                2,
                3,
                4,
                5);
        }

        public static MeshBuildBuffer StrictCandidateBudgetOverflow()
        {
            const int triangleCount = 710;
            Vector3[] positions = new Vector3[triangleCount * 3];
            int[] indices = new int[triangleCount * 3];
            for (int triangle = 0;
                triangle < triangleCount;
                triangle++)
            {
                int offset = triangle * 3;
                positions[offset] = new Vector3(-1f, -1f, 0f);
                positions[offset + 1] = new Vector3(1f, -1f, 0f);
                positions[offset + 2] = new Vector3(0f, 1f, 0f);
                indices[offset] = offset;
                indices[offset + 1] = offset + 1;
                indices[offset + 2] = offset + 2;
            }

            return CreateBuffer(positions, indices);
        }

        public static FoldCanvasAsset CreateProductionCupAsset(
            FoldCanvasValidationLevel validationLevel)
        {
            const int segments = 32;
            const float radius = 0.07f;
            const float height = 0.12f;
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M07ProductionCup";
            asset.CompileSettings.ValidationLevel = validationLevel;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "wall",
                new Rect(0f, 0.5f, 1f, 0.5f),
                new Vector2(2f * Mathf.PI * radius, height),
                segments,
                1));
            asset.Panels.Add(PanelDefinition.CreateDisk(
                "bottom",
                new Rect(0f, 0f, 0.5f, 0.5f),
                Vector2.one * (2f * radius),
                segments,
                10));
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
                SampleCount = segments
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
                Translation = new Vector3(0f, -height * 0.5f, 0f),
                RotationEuler = new Vector3(90f, 0f, 0f),
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

        public static M07GeometryValidationRun Validate(
            MeshBuildBuffer buffer,
            FoldCanvasValidationLevel validationLevel)
        {
            return new M07GeometryValidationRun(
                buffer,
                validationLevel);
        }
    }

    internal sealed class M07GeometryValidationRun : IDisposable
    {
        public M07GeometryValidationRun(
            MeshBuildBuffer buffer,
            FoldCanvasValidationLevel validationLevel)
        {
            Buffer = buffer ?? throw new ArgumentNullException(
                nameof(buffer));
            Asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            Asset.CompileSettings.ValidationLevel = validationLevel;
            Result = new FoldCanvasCompileResult();
            Report = FoldCanvasGeometryValidator.Validate(
                Buffer,
                Asset,
                Result);
            Result.GeometryValidationReport = Report;
        }

        public MeshBuildBuffer Buffer { get; }

        public FoldCanvasAsset Asset { get; }

        public FoldCanvasCompileResult Result { get; }

        public FoldCanvasGeometryValidationReport Report { get; }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Asset);
        }
    }
}
