using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoldCanvas.Editor
{
    internal sealed class FoldCanvasAuthoringController : IDisposable
    {
        internal const double DefaultDebounceSeconds = 0.2d;

        private readonly Func<FoldCanvasAsset, FoldCanvasCompileResult>
            compiler;
        private readonly double debounceSeconds;
        private FoldCanvasCompileResult lastCompileResult;
        private bool compilePending;
        private double compileDueTime;
        private int sourceRevision;
        private int compiledRevision = -1;

        public FoldCanvasAuthoringController(
            Func<FoldCanvasAsset, FoldCanvasCompileResult> compile = null,
            double debounce = DefaultDebounceSeconds)
        {
            if (double.IsNaN(debounce) ||
                double.IsInfinity(debounce) ||
                debounce < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(debounce));
            }

            compiler = compile ?? FoldCanvasCompiler.Compile;
            debounceSeconds = debounce;
        }

        public event Action SourceChanged;

        public event Action<FoldCanvasCompileResult> CompileCompleted;

        public FoldCanvasAsset Source { get; private set; }

        public FoldCanvasCompileResult LastCompileResult => lastCompileResult;

        public bool HasPendingCompile => compilePending;

        public int SourceRevision => sourceRevision;

        public int CompiledRevision => compiledRevision;

        public int CompileCount { get; private set; }

        public static FoldCanvasAsset CreateAssetAtPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "A FoldCanvas source path must be an .asset under Assets/.",
                    nameof(path));
            }

            FoldCanvasAsset asset =
                ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public void SetSource(FoldCanvasAsset source, double now)
        {
            if (ReferenceEquals(Source, source))
            {
                return;
            }

            DestroyLastCompileMesh();
            lastCompileResult = null;
            compiledRevision = -1;
            Source = source;
            sourceRevision++;
            compilePending = source != null;
            compileDueTime = now + debounceSeconds;
            SourceChanged?.Invoke();
        }

        public PanelDefinition AddPanel(PanelShape shape, double now)
        {
            RequireSource();
            RecordSource("Add FoldCanvas Panel");

            string prefix = shape == PanelShape.Disk ? "disk" : "rectangle";
            string id = CreateUniquePanelId(prefix);
            PanelDefinition panel;
            if (shape == PanelShape.Disk)
            {
                panel = PanelDefinition.CreateDisk(
                    id,
                    FindAvailableCanvasRect(new Vector2(0.25f, 0.25f)),
                    new Vector2(0.1f, 0.1f),
                    32,
                    4);
            }
            else
            {
                panel = PanelDefinition.CreateRectangle(
                    id,
                    FindAvailableCanvasRect(new Vector2(0.35f, 0.25f)),
                    new Vector2(0.2f, 0.12f),
                    8,
                    4);
            }

            Source.Panels.Add(panel);
            CommitSourceChange(now);
            return panel;
        }

        public void RemovePanel(string panelId, double now)
        {
            RequireSource();
            int index = FindPanelIndex(panelId);
            if (index < 0)
            {
                return;
            }

            RecordSource("Remove FoldCanvas Panel");
            Source.Panels.RemoveAt(index);

            for (int i = Source.Seams.Count - 1; i >= 0; i--)
            {
                SeamDefinition seam = Source.Seams[i];
                if (string.Equals(
                        seam.A.PanelId,
                        panelId,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        seam.B.PanelId,
                        panelId,
                        StringComparison.Ordinal))
                {
                    RemoveSeamReferences(seam.Id);
                    Source.Seams.RemoveAt(i);
                }
            }

            for (int i = Source.Operations.Count - 1; i >= 0; i--)
            {
                if (OperationTargetsPanel(Source.Operations[i], panelId))
                {
                    Source.Operations.RemoveAt(i);
                }
            }

            CommitSourceChange(now);
        }

        public bool RenamePanel(
            string currentId,
            string newId,
            double now)
        {
            RequireSource();
            PanelDefinition panel = FindPanel(currentId);
            if (panel == null)
            {
                return false;
            }

            string normalized = ValidateId(newId, nameof(newId));
            if (!string.Equals(currentId, normalized, StringComparison.Ordinal) &&
                FindPanel(normalized) != null)
            {
                throw new ArgumentException(
                    $"A panel named '{normalized}' already exists.",
                    nameof(newId));
            }

            if (string.Equals(currentId, normalized, StringComparison.Ordinal))
            {
                return true;
            }

            RecordSource("Rename FoldCanvas Panel");
            panel.Id = normalized;
            RewritePanelReferences(currentId, normalized);
            CommitSourceChange(now);
            return true;
        }

        public bool MutatePanel(
            string panelId,
            string undoName,
            Action<PanelDefinition> mutation,
            double now,
            bool recordUndo = true)
        {
            RequireSource();
            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            PanelDefinition panel = FindPanel(panelId);
            if (panel == null)
            {
                return false;
            }

            if (recordUndo)
            {
                RecordSource(undoName);
            }

            mutation(panel);
            CommitSourceChange(now);
            return true;
        }

        public void BeginContinuousEdit(string undoName)
        {
            RequireSource();
            RecordSource(undoName);
        }

        public SeamDefinition AddSeam(
            string panelA,
            string boundaryA,
            string panelB,
            string boundaryB,
            SeamMode mode,
            double now)
        {
            RequireSource();
            ValidateBoundaryReference(panelA, boundaryA);
            ValidateBoundaryReference(panelB, boundaryB);
            RecordSource("Add FoldCanvas Seam");

            SeamDefinition seam = new SeamDefinition
            {
                Id = CreateUniqueSeamId("seam"),
                A = new BoundaryReference(panelA, boundaryA),
                B = new BoundaryReference(panelB, boundaryB),
                Mode = mode
            };
            Source.Seams.Add(seam);
            CommitSourceChange(now);
            return seam;
        }

        public void RemoveSeam(string seamId, double now)
        {
            RequireSource();
            int index = FindSeamIndex(seamId);
            if (index < 0)
            {
                return;
            }

            RecordSource("Remove FoldCanvas Seam");
            RemoveSeamReferences(seamId);
            Source.Seams.RemoveAt(index);
            CommitSourceChange(now);
        }

        public bool RenameSeam(
            string currentId,
            string newId,
            double now)
        {
            RequireSource();
            SeamDefinition seam = FindSeam(currentId);
            if (seam == null)
            {
                return false;
            }

            string normalized = ValidateId(newId, nameof(newId));
            if (!string.Equals(currentId, normalized, StringComparison.Ordinal) &&
                FindSeam(normalized) != null)
            {
                throw new ArgumentException(
                    $"A seam named '{normalized}' already exists.",
                    nameof(newId));
            }

            if (string.Equals(currentId, normalized, StringComparison.Ordinal))
            {
                return true;
            }

            RecordSource("Rename FoldCanvas Seam");
            seam.Id = normalized;
            for (int i = 0; i < Source.Operations.Count; i++)
            {
                if (Source.Operations[i] is StitchOperationDefinition stitch)
                {
                    RewriteListValue(stitch.SeamIds, currentId, normalized);
                }
                else if (Source.Operations[i] is RollOperationDefinition roll &&
                    string.Equals(
                        roll.TargetSeamId,
                        currentId,
                        StringComparison.Ordinal))
                {
                    roll.TargetSeamId = normalized;
                }
            }

            CommitSourceChange(now);
            return true;
        }

        public bool MutateSeam(
            string seamId,
            string undoName,
            Action<SeamDefinition> mutation,
            double now)
        {
            RequireSource();
            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            SeamDefinition seam = FindSeam(seamId);
            if (seam == null)
            {
                return false;
            }

            RecordSource(undoName);
            mutation(seam);
            CommitSourceChange(now);
            return true;
        }

        public FoldOperationDefinition AddOperation(
            FoldOperationType type,
            string selectedPanelId,
            string selectedSeamId,
            double now)
        {
            RequireSource();
            RecordSource("Add FoldCanvas Operation");
            FoldOperationDefinition operation = CreateOperation(
                type,
                selectedPanelId,
                selectedSeamId);
            Source.Operations.Add(operation);
            CommitSourceChange(now);
            return operation;
        }

        public void RemoveOperation(int index, double now)
        {
            RequireSource();
            if (index < 0 || index >= Source.Operations.Count)
            {
                return;
            }

            RecordSource("Remove FoldCanvas Operation");
            Source.Operations.RemoveAt(index);
            CommitSourceChange(now);
        }

        public bool MoveOperation(int index, int destinationIndex, double now)
        {
            RequireSource();
            if (index < 0 ||
                index >= Source.Operations.Count ||
                destinationIndex < 0 ||
                destinationIndex >= Source.Operations.Count ||
                index == destinationIndex)
            {
                return false;
            }

            RecordSource("Reorder FoldCanvas Operation");
            FoldOperationDefinition operation = Source.Operations[index];
            Source.Operations.RemoveAt(index);
            Source.Operations.Insert(destinationIndex, operation);
            CommitSourceChange(now);
            return true;
        }

        public bool RenameOperation(int index, string newId, double now)
        {
            RequireSource();
            if (index < 0 || index >= Source.Operations.Count)
            {
                return false;
            }

            string normalized = ValidateId(newId, nameof(newId));
            for (int i = 0; i < Source.Operations.Count; i++)
            {
                if (i != index &&
                    string.Equals(
                        Source.Operations[i]?.Id,
                        normalized,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"An operation named '{normalized}' already exists.",
                        nameof(newId));
                }
            }

            if (string.Equals(
                    Source.Operations[index].Id,
                    normalized,
                    StringComparison.Ordinal))
            {
                return true;
            }

            RecordSource("Rename FoldCanvas Operation");
            Source.Operations[index].Id = normalized;
            CommitSourceChange(now);
            return true;
        }

        public bool MutateOperation(
            int index,
            string undoName,
            Action<FoldOperationDefinition> mutation,
            double now)
        {
            RequireSource();
            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            if (index < 0 || index >= Source.Operations.Count)
            {
                return false;
            }

            RecordSource(undoName);
            mutation(Source.Operations[index]);
            CommitSourceChange(now);
            return true;
        }

        public void SetAppearance(Texture2D appearance, double now)
        {
            RequireSource();
            if (ReferenceEquals(Source.Appearance, appearance))
            {
                return;
            }

            RecordSource("Set FoldCanvas Appearance");
            Source.Appearance = appearance;
            CommitSourceChange(now);
        }

        public void NotifyExternalSourceChange(double now)
        {
            if (Source == null)
            {
                return;
            }

            sourceRevision++;
            compilePending = true;
            compileDueTime = now + debounceSeconds;
            SourceChanged?.Invoke();
        }

        public void RequestCompile(double now, bool immediate = false)
        {
            sourceRevision++;
            compilePending = true;
            compileDueTime = immediate ? now : now + debounceSeconds;
        }

        public bool TryCompile(double now)
        {
            const double comparisonTolerance = 1e-9d;
            if (!compilePending ||
                now + comparisonTolerance < compileDueTime)
            {
                return false;
            }

            int requestedRevision = sourceRevision;
            compilePending = false;
            FoldCanvasCompileResult result = compiler(Source);
            CompileCount++;

            if (requestedRevision != sourceRevision)
            {
                DestroyCompileMesh(result);
                compilePending = true;
                compileDueTime = now + debounceSeconds;
                return false;
            }

            DestroyLastCompileMesh();
            lastCompileResult = result;
            compiledRevision = requestedRevision;
            CompileCompleted?.Invoke(result);
            return true;
        }

        public void CompileImmediately(double now)
        {
            RequestCompile(now, true);
            TryCompile(now);
        }

        public PanelDefinition FindPanel(string panelId)
        {
            if (Source == null)
            {
                return null;
            }

            for (int i = 0; i < Source.Panels.Count; i++)
            {
                if (string.Equals(
                        Source.Panels[i]?.Id,
                        panelId,
                        StringComparison.Ordinal))
                {
                    return Source.Panels[i];
                }
            }

            return null;
        }

        public SeamDefinition FindSeam(string seamId)
        {
            if (Source == null)
            {
                return null;
            }

            for (int i = 0; i < Source.Seams.Count; i++)
            {
                if (string.Equals(
                        Source.Seams[i]?.Id,
                        seamId,
                        StringComparison.Ordinal))
                {
                    return Source.Seams[i];
                }
            }

            return null;
        }

        public static IReadOnlyList<string> GetBoundaryIds(
            PanelDefinition panel)
        {
            if (panel == null)
            {
                return Array.Empty<string>();
            }

            if (panel.Shape == PanelShape.Disk)
            {
                return new[] { "perimeter" };
            }

            return new[] { "uMin", "uMax", "vMin", "vMax" };
        }

        public void Dispose()
        {
            DestroyLastCompileMesh();
            lastCompileResult = null;
            SourceChanged = null;
            CompileCompleted = null;
        }

        private void CommitSourceChange(double now)
        {
            EditorUtility.SetDirty(Source);
            sourceRevision++;
            compilePending = true;
            compileDueTime = now + debounceSeconds;
            SourceChanged?.Invoke();
        }

        private void RecordSource(string undoName)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(
                string.IsNullOrWhiteSpace(undoName)
                    ? "Edit FoldCanvas Source"
                    : undoName);
            Undo.RecordObject(
                Source,
                string.IsNullOrWhiteSpace(undoName)
                    ? "Edit FoldCanvas Source"
                    : undoName);
        }

        private void RequireSource()
        {
            if (Source == null)
            {
                throw new InvalidOperationException(
                    "Select or create a FoldCanvas source asset first.");
            }
        }

        private int FindPanelIndex(string panelId)
        {
            for (int i = 0; i < Source.Panels.Count; i++)
            {
                if (string.Equals(
                        Source.Panels[i]?.Id,
                        panelId,
                        StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSeamIndex(string seamId)
        {
            for (int i = 0; i < Source.Seams.Count; i++)
            {
                if (string.Equals(
                        Source.Seams[i]?.Id,
                        seamId,
                        StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ValidateBoundaryReference(
            string panelId,
            string boundaryId)
        {
            PanelDefinition panel = FindPanel(panelId);
            if (panel == null)
            {
                throw new ArgumentException(
                    $"Panel '{panelId}' does not exist.",
                    nameof(panelId));
            }

            IReadOnlyList<string> boundaries = GetBoundaryIds(panel);
            for (int i = 0; i < boundaries.Count; i++)
            {
                if (string.Equals(
                        boundaries[i],
                        boundaryId,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new ArgumentException(
                $"Panel '{panelId}' has no boundary '{boundaryId}'.",
                nameof(boundaryId));
        }

        private string CreateUniquePanelId(string prefix)
        {
            return CreateUniqueId(
                prefix,
                candidate => FindPanel(candidate) != null);
        }

        private string CreateUniqueSeamId(string prefix)
        {
            return CreateUniqueId(
                prefix,
                candidate => FindSeam(candidate) != null);
        }

        private string CreateUniqueOperationId(string prefix)
        {
            return CreateUniqueId(
                prefix,
                candidate =>
                {
                    for (int i = 0; i < Source.Operations.Count; i++)
                    {
                        if (string.Equals(
                                Source.Operations[i]?.Id,
                                candidate,
                                StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }

                    return false;
                });
        }

        private static string CreateUniqueId(
            string prefix,
            Func<string, bool> exists)
        {
            for (int index = 1; index < int.MaxValue; index++)
            {
                string candidate = $"{prefix}-{index}";
                if (!exists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"Could not allocate a unique '{prefix}' identifier.");
        }

        private Rect FindAvailableCanvasRect(Vector2 size)
        {
            const float margin = 0.025f;
            float width = Mathf.Clamp(size.x, 0.05f, 1f);
            float height = Mathf.Clamp(size.y, 0.05f, 1f);
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    Rect candidate = new Rect(
                        margin + column * 0.24f,
                        1f - margin - height - row * 0.24f,
                        width,
                        height);
                    if (candidate.xMax > 1f || candidate.yMin < 0f)
                    {
                        continue;
                    }

                    bool overlaps = false;
                    for (int i = 0; i < Source.Panels.Count; i++)
                    {
                        if (Source.Panels[i] != null &&
                            Source.Panels[i].CanvasRect.Overlaps(candidate))
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        return candidate;
                    }
                }
            }

            return new Rect(margin, margin, width, height);
        }

        private FoldOperationDefinition CreateOperation(
            FoldOperationType type,
            string selectedPanelId,
            string selectedSeamId)
        {
            string panelId = FindPanel(selectedPanelId)?.Id;
            if (string.IsNullOrEmpty(panelId) && Source.Panels.Count > 0)
            {
                panelId = Source.Panels[0]?.Id;
            }

            switch (type)
            {
                case FoldOperationType.RigidTransform:
                    return new RigidTransformOperationDefinition
                    {
                        Id = CreateUniqueOperationId("place"),
                        PanelId = panelId
                    };
                case FoldOperationType.Fold:
                    return new FoldLineOperationDefinition
                    {
                        Id = CreateUniqueOperationId("fold"),
                        PanelId = panelId
                    };
                case FoldOperationType.Roll:
                    return new RollOperationDefinition
                    {
                        Id = CreateUniqueOperationId("roll"),
                        PanelId = panelId
                    };
                case FoldOperationType.Stitch:
                    StitchOperationDefinition stitch =
                        new StitchOperationDefinition
                        {
                            Id = CreateUniqueOperationId("stitch")
                        };
                    if (FindSeam(selectedSeamId) != null)
                    {
                        stitch.SeamIds.Add(selectedSeamId);
                    }

                    return stitch;
                case FoldOperationType.Solidify:
                    SolidifyOperationDefinition solidify =
                        new SolidifyOperationDefinition
                        {
                            Id = CreateUniqueOperationId("solidify")
                        };
                    if (!string.IsNullOrEmpty(panelId))
                    {
                        solidify.PanelIds.Add(panelId);
                    }

                    return solidify;
                case FoldOperationType.SphericalWrap:
                    return new SphericalWrapOperationDefinition
                    {
                        Id = CreateUniqueOperationId("spherical-wrap"),
                        PanelId = panelId
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private void RewritePanelReferences(string oldId, string newId)
        {
            for (int i = 0; i < Source.Seams.Count; i++)
            {
                SeamDefinition seam = Source.Seams[i];
                if (string.Equals(
                        seam.A.PanelId,
                        oldId,
                        StringComparison.Ordinal))
                {
                    seam.A = new BoundaryReference(newId, seam.A.BoundaryId);
                }

                if (string.Equals(
                        seam.B.PanelId,
                        oldId,
                        StringComparison.Ordinal))
                {
                    seam.B = new BoundaryReference(newId, seam.B.BoundaryId);
                }
            }

            for (int i = 0; i < Source.Operations.Count; i++)
            {
                FoldOperationDefinition operation = Source.Operations[i];
                switch (operation)
                {
                    case RigidTransformOperationDefinition rigid:
                        rigid.PanelId = RewriteValue(rigid.PanelId, oldId, newId);
                        break;
                    case FoldLineOperationDefinition fold:
                        fold.PanelId = RewriteValue(fold.PanelId, oldId, newId);
                        break;
                    case RollOperationDefinition roll:
                        roll.PanelId = RewriteValue(roll.PanelId, oldId, newId);
                        break;
                    case SolidifyOperationDefinition solidify:
                        RewriteListValue(solidify.PanelIds, oldId, newId);
                        break;
                    case SphericalWrapOperationDefinition sphericalWrap:
                        sphericalWrap.PanelId = RewriteValue(
                            sphericalWrap.PanelId,
                            oldId,
                            newId);
                        break;
                }
            }
        }

        private void RemoveSeamReferences(string seamId)
        {
            for (int i = 0; i < Source.Operations.Count; i++)
            {
                if (Source.Operations[i] is StitchOperationDefinition stitch)
                {
                    for (int seamIndex = stitch.SeamIds.Count - 1;
                        seamIndex >= 0;
                        seamIndex--)
                    {
                        if (string.Equals(
                                stitch.SeamIds[seamIndex],
                                seamId,
                                StringComparison.Ordinal))
                        {
                            stitch.SeamIds.RemoveAt(seamIndex);
                        }
                    }
                }
                else if (Source.Operations[i] is RollOperationDefinition roll &&
                    string.Equals(
                        roll.TargetSeamId,
                        seamId,
                        StringComparison.Ordinal))
                {
                    roll.TargetSeamId = null;
                }
            }
        }

        private static bool OperationTargetsPanel(
            FoldOperationDefinition operation,
            string panelId)
        {
            switch (operation)
            {
                case RigidTransformOperationDefinition rigid:
                    return string.Equals(
                        rigid.PanelId,
                        panelId,
                        StringComparison.Ordinal);
                case FoldLineOperationDefinition fold:
                    return string.Equals(
                        fold.PanelId,
                        panelId,
                        StringComparison.Ordinal);
                case RollOperationDefinition roll:
                    return string.Equals(
                        roll.PanelId,
                        panelId,
                        StringComparison.Ordinal);
                case SolidifyOperationDefinition solidify:
                    for (int i = 0; i < solidify.PanelIds.Count; i++)
                    {
                        if (string.Equals(
                                solidify.PanelIds[i],
                                panelId,
                                StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }

                    return false;
                case SphericalWrapOperationDefinition sphericalWrap:
                    return string.Equals(
                        sphericalWrap.PanelId,
                        panelId,
                        StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private static string RewriteValue(
            string value,
            string oldValue,
            string newValue)
        {
            return string.Equals(value, oldValue, StringComparison.Ordinal)
                ? newValue
                : value;
        }

        private static void RewriteListValue(
            List<string> values,
            string oldValue,
            string newValue)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(
                        values[i],
                        oldValue,
                        StringComparison.Ordinal))
                {
                    values[i] = newValue;
                }
            }
        }

        private static string ValidateId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "FoldCanvas identifiers cannot be empty or whitespace.",
                    parameterName);
            }

            return value.Trim();
        }

        private void DestroyLastCompileMesh()
        {
            DestroyCompileMesh(lastCompileResult);
        }

        private static void DestroyCompileMesh(FoldCanvasCompileResult result)
        {
            if (result?.Mesh == null ||
                AssetDatabase.Contains(result.Mesh))
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(result.Mesh);
            result.Mesh = null;
        }
    }
}
