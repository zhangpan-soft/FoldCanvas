using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal static class FoldCanvasRobustnessGenerator
    {
        public const string GeneratorVersion = "2";

        public const string PlanarValidSuite = "planar-valid";
        public const string RollValidSuite = "roll-valid";
        public const string RollInsufficientSuite =
            "roll-insufficient-invalid";
        public const string FoldOffGridSuite = "fold-off-grid-valid";

        public static readonly string[] SmokeSuiteIds =
        {
            PlanarValidSuite,
            RollValidSuite,
            RollInsufficientSuite,
            FoldOffGridSuite
        };

        private static readonly float[] SizeBuckets =
        {
            0.25f,
            0.5f,
            0.75f,
            1f,
            1.5f,
            2f,
            3f,
            4f
        };

        private static readonly float[] SignedAngleBuckets =
        {
            -360f,
            -180f,
            -90f,
            90f,
            180f,
            360f
        };

        private static readonly float[] StartAngleBuckets =
        {
            -180f,
            -90f,
            0f,
            45f,
            90f,
            180f
        };

        private static readonly float[] OffGridBuckets =
        {
            0.2f,
            0.3f,
            0.4f,
            0.6f,
            0.7f,
            0.8f
        };

        public static FoldCanvasRobustnessGeneratedCase Generate(
            string suiteId,
            ulong seed,
            int ordinal)
        {
            if (ordinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            if (!IsKnownSuite(suiteId))
            {
                throw new ArgumentException(
                    $"Unknown M13 robustness suite '{suiteId}'.",
                    nameof(suiteId));
            }

            string caseId = BuildCaseId(suiteId, seed, ordinal);
            SplitMix64 random = new SplitMix64(
                InitialState(suiteId, seed, ordinal));
            FoldCanvasAsset asset = CreateBaseAsset(caseId);
            bool expectedSuccess;
            string expectedDiagnostic;
            try
            {
                if (string.Equals(
                        suiteId,
                        PlanarValidSuite,
                        StringComparison.Ordinal))
                {
                    BuildPlanarValid(asset, ref random);
                    expectedSuccess = true;
                    expectedDiagnostic = string.Empty;
                }
                else if (string.Equals(
                    suiteId,
                    RollValidSuite,
                    StringComparison.Ordinal))
                {
                    BuildRollValid(asset, ref random);
                    expectedSuccess = true;
                    expectedDiagnostic = string.Empty;
                }
                else if (string.Equals(
                    suiteId,
                    RollInsufficientSuite,
                    StringComparison.Ordinal))
                {
                    BuildRollInsufficient(asset, ref random);
                    expectedSuccess = false;
                    expectedDiagnostic =
                        FoldCanvasDiagnosticCodes
                            .InsufficientRollTessellation;
                }
                else
                {
                    BuildFoldOffGrid(asset, ref random);
                    expectedSuccess = true;
                    expectedDiagnostic = string.Empty;
                }

                string canonicalSource = CanonicalSource(asset);
                return new FoldCanvasRobustnessGeneratedCase(
                    GeneratorVersion,
                    suiteId,
                    seed,
                    ordinal,
                    caseId,
                    asset,
                    canonicalSource,
                    expectedSuccess,
                    expectedDiagnostic);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(asset);
                throw;
            }
        }

        public static string CanonicalSource(FoldCanvasAsset asset)
        {
            FoldScriptDocument document =
                FoldScriptAssetConverter.ToDocument(
                    asset,
                    "appearance.png");
            FoldScriptWriteResult write =
                FoldScriptSerializer.Write(document);
            if (!write.Success)
            {
                throw new InvalidDataException(
                    "The M13 generator produced non-canonical FoldScript: " +
                    JoinDiagnostics(write));
            }

            return write.Json;
        }

        public static string BuildCaseId(
            string suiteId,
            ulong seed,
            int ordinal)
        {
            if (string.IsNullOrWhiteSpace(suiteId))
            {
                throw new ArgumentException(
                    "Suite IDs must be non-empty.",
                    nameof(suiteId));
            }

            if (ordinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return "m13-" + suiteId + "-" +
                seed.ToString("x16", CultureInfo.InvariantCulture) + "-" +
                ordinal.ToString("D6", CultureInfo.InvariantCulture);
        }

        private static FoldCanvasAsset CreateBaseAsset(string caseId)
        {
            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = caseId;
            asset.SourceMetadata.SchemaVersion =
                FoldCanvasSourceMetadata.CurrentSchemaVersion;
            asset.SourceMetadata.AssetId = caseId;
            asset.SourceMetadata.DisplayName = caseId;
            asset.SourceMetadata.Units = FoldScriptUnit.Meter;
            asset.SourceMetadata.AppearanceReference = "appearance.png";
            asset.SourceMetadata.CanvasPixelWidth = 256;
            asset.SourceMetadata.CanvasPixelHeight = 256;
            asset.CompileSettings.ValidationLevel =
                FoldCanvasValidationLevel.Basic;
            return asset;
        }

        private static void BuildPlanarValid(
            FoldCanvasAsset asset,
            ref SplitMix64 random)
        {
            int uSegments = random.NextInt(1, 9);
            int vSegments = random.NextInt(1, 9);
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(
                    random.Choose(SizeBuckets),
                    random.Choose(SizeBuckets)),
                uSegments,
                vSegments));

            if (random.NextBoolean())
            {
                asset.Operations.Add(new RigidTransformOperationDefinition
                {
                    Id = "place",
                    PanelId = "panel",
                    Translation = new Vector3(
                        random.NextInt(-4, 5) * 0.25f,
                        random.NextInt(-4, 5) * 0.25f,
                        random.NextInt(-4, 5) * 0.25f),
                    RotationEuler = new Vector3(
                        random.NextInt(-2, 3) * 45f,
                        random.NextInt(-2, 3) * 45f,
                        random.NextInt(-2, 3) * 45f),
                    Scale = random.NextBoolean()
                        ? Vector3.one
                        : new Vector3(-1f, 1f, 1f)
                });
            }
        }

        private static void BuildRollValid(
            FoldCanvasAsset asset,
            ref SplitMix64 random)
        {
            RollDirection direction = random.NextBoolean()
                ? RollDirection.U
                : RollDirection.V;
            float angle = random.Choose(SignedAngleBuckets);
            int rollSegments = random.NextInt(3, 13);
            int crossSegments = random.NextInt(1, 5);
            int uSegments = direction == RollDirection.U
                ? rollSegments
                : crossSegments;
            int vSegments = direction == RollDirection.V
                ? rollSegments
                : crossSegments;
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(
                    random.Choose(SizeBuckets),
                    random.Choose(SizeBuckets)),
                uSegments,
                vSegments));
            asset.Operations.Add(new RigidTransformOperationDefinition
            {
                Id = "place",
                PanelId = "panel",
                Translation = new Vector3(
                    random.NextInt(-2, 3) * 0.5f,
                    random.NextInt(-2, 3) * 0.5f,
                    random.NextInt(-2, 3) * 0.5f),
                RotationEuler = new Vector3(
                    0f,
                    random.NextInt(-2, 3) * 45f,
                    random.NextInt(-2, 3) * 45f),
                Scale = random.NextBoolean()
                    ? Vector3.one
                    : new Vector3(-1f, 1f, 1f)
            });
            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll",
                PanelId = "panel",
                Direction = direction,
                AngleDegrees = angle,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                StartAngleDegrees = random.Choose(StartAngleBuckets)
            });
        }

        private static void BuildRollInsufficient(
            FoldCanvasAsset asset,
            ref SplitMix64 random)
        {
            RollDirection direction = random.NextBoolean()
                ? RollDirection.U
                : RollDirection.V;
            int rollSegments = random.NextInt(1, 3);
            int crossSegments = random.NextInt(1, 4);
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(
                    random.Choose(SizeBuckets),
                    random.Choose(SizeBuckets)),
                direction == RollDirection.U
                    ? rollSegments
                    : crossSegments,
                direction == RollDirection.V
                    ? rollSegments
                    : crossSegments));
            asset.Operations.Add(new RollOperationDefinition
            {
                Id = "roll",
                PanelId = "panel",
                Direction = direction,
                AngleDegrees = random.NextBoolean() ? 360f : -360f,
                RadiusMode = RollRadiusMode.PreserveArcLength,
                StartAngleDegrees = random.Choose(StartAngleBuckets)
            });
        }

        private static void BuildFoldOffGrid(
            FoldCanvasAsset asset,
            ref SplitMix64 random)
        {
            asset.Panels.Add(PanelDefinition.CreateRectangle(
                "panel",
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(
                    random.Choose(SizeBuckets),
                    random.Choose(SizeBuckets)),
                1,
                1));
            bool vertical = random.NextBoolean();
            float coordinate = random.Choose(OffGridBuckets);
            asset.Operations.Add(new FoldLineOperationDefinition
            {
                Id = "fold",
                PanelId = "panel",
                LineStart = vertical
                    ? new Vector2(coordinate, 0f)
                    : new Vector2(0f, coordinate),
                LineEnd = vertical
                    ? new Vector2(coordinate, 1f)
                    : new Vector2(1f, coordinate),
                Side = random.NextBoolean()
                    ? FoldSide.Positive
                    : FoldSide.Negative,
                AngleDegrees = random.NextBoolean() ? 90f : -90f,
                Falloff = 0f
            });
        }

        private static bool IsKnownSuite(string suiteId)
        {
            if (suiteId == null)
            {
                return false;
            }

            for (int i = 0; i < SmokeSuiteIds.Length; i++)
            {
                if (string.Equals(
                        suiteId,
                        SmokeSuiteIds[i],
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static ulong InitialState(
            string suiteId,
            ulong seed,
            int ordinal)
        {
            unchecked
            {
                ulong state = seed ^ Fnv1A64(suiteId);
                state ^= (ulong)ordinal * 0xD6E8FEB86659FD93UL;
                state ^= 0xA0761D6478BD642FUL;
                return state;
            }
        }

        private static ulong Fnv1A64(string value)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                for (int i = 0; i < value.Length; i++)
                {
                    char character = value[i];
                    if (character > 0x7f)
                    {
                        throw new ArgumentException(
                            "M13 robustness suite IDs must be ASCII.",
                            nameof(value));
                    }

                    hash ^= (byte)character;
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }

        private static string JoinDiagnostics(FoldScriptWriteResult result)
        {
            if (result == null || result.Diagnostics.Count == 0)
            {
                return "no diagnostics";
            }

            string[] values = new string[result.Diagnostics.Count];
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                values[i] = result.Diagnostics[i].ToString();
            }

            return string.Join(" | ", values);
        }

        private struct SplitMix64
        {
            private ulong state;

            public SplitMix64(ulong initialState)
            {
                state = initialState;
            }

            public ulong NextUInt64()
            {
                unchecked
                {
                    state += 0x9E3779B97F4A7C15UL;
                    ulong value = state;
                    value = (value ^ (value >> 30)) *
                        0xBF58476D1CE4E5B9UL;
                    value = (value ^ (value >> 27)) *
                        0x94D049BB133111EBUL;
                    return value ^ (value >> 31);
                }
            }

            public int NextInt(int minimumInclusive, int maximumExclusive)
            {
                if (maximumExclusive <= minimumInclusive)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(maximumExclusive));
                }

                uint range = checked((uint)(
                    maximumExclusive - minimumInclusive));
                return minimumInclusive +
                    (int)(NextUInt64() % range);
            }

            public bool NextBoolean()
            {
                return (NextUInt64() & 1UL) != 0UL;
            }

            public float Choose(float[] values)
            {
                if (values == null || values.Length == 0)
                {
                    throw new ArgumentException(
                        "Value buckets must be non-empty.",
                        nameof(values));
                }

                return values[NextInt(0, values.Length)];
            }
        }
    }
}
