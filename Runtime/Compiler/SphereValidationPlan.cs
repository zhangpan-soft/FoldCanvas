using System;
using System.Collections.Generic;

namespace FoldCanvas
{
    internal sealed class SphereValidationPlan
    {
        private readonly List<SphereComponentPlan> components;

        private SphereValidationPlan(
            List<SphereComponentPlan> orderedComponents)
        {
            components = orderedComponents;
        }

        public static SphereValidationPlan Build(
            FoldCanvasAsset asset)
        {
            Dictionary<string, WrapSource> wrapsByPanel =
                new Dictionary<string, WrapSource>(
                    StringComparer.Ordinal);
            for (int operationIndex = 0;
                operationIndex < asset.Operations.Count;
                operationIndex++)
            {
                if (!(asset.Operations[operationIndex] is
                        SphericalWrapOperationDefinition wrap) ||
                    !wrap.Enabled ||
                    string.IsNullOrWhiteSpace(wrap.PanelId) ||
                    wrapsByPanel.ContainsKey(wrap.PanelId))
                {
                    continue;
                }

                wrapsByPanel.Add(
                    wrap.PanelId,
                    new WrapSource(
                        wrap.PanelId,
                        wrap.Id,
                        operationIndex));
            }

            if (wrapsByPanel.Count == 0)
            {
                return new SphereValidationPlan(
                    new List<SphereComponentPlan>());
            }

            List<WrapSource> orderedWraps =
                OrderWrapsByPanelSource(asset, wrapsByPanel);
            Dictionary<string, int> wrapIndexByPanel =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            for (int i = 0; i < orderedWraps.Count; i++)
            {
                wrapIndexByPanel.Add(
                    orderedWraps[i].PanelId,
                    i);
            }

            SeamLookup seamLookup = BuildSeamLookup(asset);
            int[] parents = new int[orderedWraps.Count];
            bool[] hasRelevantStitch =
                new bool[orderedWraps.Count];
            List<RelevantStitch> relevantStitches =
                new List<RelevantStitch>();
            for (int i = 0; i < parents.Length; i++)
            {
                parents[i] = i;
            }

            for (int operationIndex = 0;
                operationIndex < asset.Operations.Count;
                operationIndex++)
            {
                if (!(asset.Operations[operationIndex] is
                        StitchOperationDefinition stitch) ||
                    !stitch.Enabled ||
                    stitch.SeamIds == null)
                {
                    continue;
                }

                for (int seamIndex = 0;
                    seamIndex < stitch.SeamIds.Count;
                    seamIndex++)
                {
                    string seamId = stitch.SeamIds[seamIndex];
                    if (!seamLookup.TryGetUnique(
                            seamId,
                            out SeamDefinition seam) ||
                        !wrapIndexByPanel.TryGetValue(
                            seam.A.PanelId,
                            out int first) ||
                        !wrapIndexByPanel.TryGetValue(
                            seam.B.PanelId,
                            out int second))
                    {
                        continue;
                    }

                    Union(parents, first, second);
                    hasRelevantStitch[first] = true;
                    hasRelevantStitch[second] = true;
                    relevantStitches.Add(new RelevantStitch(
                        operationIndex,
                        stitch.Id,
                        first,
                        second));
                }
            }

            Dictionary<int, List<int>> membersByRoot =
                new Dictionary<int, List<int>>();
            for (int i = 0; i < orderedWraps.Count; i++)
            {
                if (!hasRelevantStitch[i])
                {
                    continue;
                }

                int root = Find(parents, i);
                if (!membersByRoot.TryGetValue(
                        root,
                        out List<int> members))
                {
                    members = new List<int>();
                    membersByRoot.Add(root, members);
                }

                members.Add(i);
            }

            List<List<int>> orderedGroups =
                new List<List<int>>(membersByRoot.Values);
            orderedGroups.Sort(
                (first, second) =>
                    first[0].CompareTo(second[0]));
            List<SphereComponentPlan> componentPlans =
                new List<SphereComponentPlan>();
            for (int componentIndex = 0;
                componentIndex < orderedGroups.Count;
                componentIndex++)
            {
                List<int> members = orderedGroups[componentIndex];
                HashSet<int> memberSet = new HashSet<int>(members);
                int lastStitchIndex = -1;
                string lastStitchId = null;
                for (int stitchIndex = 0;
                    stitchIndex < relevantStitches.Count;
                    stitchIndex++)
                {
                    RelevantStitch relevant =
                        relevantStitches[stitchIndex];
                    if (!memberSet.Contains(relevant.FirstWrapIndex) ||
                        !memberSet.Contains(relevant.SecondWrapIndex))
                    {
                        continue;
                    }

                    if (relevant.OperationIndex >= lastStitchIndex)
                    {
                        lastStitchIndex =
                            relevant.OperationIndex;
                        lastStitchId = relevant.OperationId;
                    }
                }

                List<string> panelIds =
                    new List<string>(members.Count);
                List<string> operationIds =
                    new List<string>(members.Count);
                for (int memberIndex = 0;
                    memberIndex < members.Count;
                    memberIndex++)
                {
                    WrapSource source =
                        orderedWraps[members[memberIndex]];
                    panelIds.Add(source.PanelId);
                    operationIds.Add(source.OperationId);
                }

                componentPlans.Add(new SphereComponentPlan(
                    $"sphere-component-{componentIndex:000}",
                    panelIds,
                    operationIds,
                    lastStitchIndex,
                    lastStitchId));
            }

            return new SphereValidationPlan(componentPlans);
        }

        public bool TryValidateAfterOperation(
            int operationIndex,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            List<SphereComponentPlan> due =
                new List<SphereComponentPlan>();
            for (int i = 0; i < components.Count; i++)
            {
                SphereComponentPlan component = components[i];
                if (!component.Validated &&
                    component.LastRelevantStitchOperationIndex ==
                        operationIndex)
                {
                    due.Add(component);
                }
            }

            return TryValidateComponents(due, buffer, result);
        }

        public bool TryValidateBeforeSolidify(
            int operationIndex,
            SolidifyOperationDefinition solidify,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            List<SphereComponentPlan> affected =
                new List<SphereComponentPlan>();
            for (int i = 0; i < components.Count; i++)
            {
                SphereComponentPlan component = components[i];
                if (component.Validated ||
                    !component.Intersects(solidify.PanelIds))
                {
                    continue;
                }

                if (operationIndex <=
                    component.LastRelevantStitchOperationIndex)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .SphereValidationRequiredBeforeSolidify,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Spherical component '{component.ComponentId}' must complete its relevant Stitch operations and closed-sphere validation before Solidify.",
                        component.PanelIds[0],
                        solidify.Id,
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "solidifyOperationIndex",
                                operationIndex),
                            new FoldCanvasDiagnosticValue(
                                "requiredStitchOperationIndex",
                                component
                                    .LastRelevantStitchOperationIndex)
                        }));
                    return false;
                }

                affected.Add(component);
            }

            return TryValidateComponents(
                affected,
                buffer,
                result);
        }

        public bool TryValidateRemaining(
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            List<SphereComponentPlan> remaining =
                new List<SphereComponentPlan>();
            for (int i = 0; i < components.Count; i++)
            {
                if (!components[i].Validated)
                {
                    remaining.Add(components[i]);
                }
            }

            return TryValidateComponents(
                remaining,
                buffer,
                result);
        }

        private static bool TryValidateComponents(
            IReadOnlyList<SphereComponentPlan> due,
            MeshBuildBuffer buffer,
            FoldCanvasCompileResult result)
        {
            if (due.Count == 0)
            {
                return true;
            }

            FoldCanvasCompiledData snapshot = buffer.Freeze();
            bool allValid = true;
            for (int i = 0; i < due.Count; i++)
            {
                SphereComponentPlan component = due[i];
                FoldCanvasSphereReport report =
                    FoldCanvasSphereValidator.Analyze(
                        snapshot,
                        component.ComponentId,
                        component.PanelIds,
                        component.SphericalWrapOperationIds,
                        SphereValidationStage.PreSolidify,
                        component.LastRelevantStitchOperationId,
                        component
                            .LastRelevantStitchOperationIndex);
                result.AddSphereReport(report);
                component.Validated = true;
                if (report.IsClosedSphere)
                {
                    continue;
                }

                FoldCanvasCompiler.AddSphereValidationDiagnostic(
                    result,
                    report);
                allValid = false;
            }

            return allValid;
        }

        private static List<WrapSource> OrderWrapsByPanelSource(
            FoldCanvasAsset asset,
            Dictionary<string, WrapSource> wrapsByPanel)
        {
            List<WrapSource> ordered = new List<WrapSource>();
            for (int panelIndex = 0;
                panelIndex < asset.Panels.Count;
                panelIndex++)
            {
                PanelDefinition panel = asset.Panels[panelIndex];
                if (panel != null &&
                    wrapsByPanel.TryGetValue(
                        panel.Id,
                        out WrapSource source))
                {
                    ordered.Add(source);
                }
            }

            return ordered;
        }

        private static SeamLookup BuildSeamLookup(
            FoldCanvasAsset asset)
        {
            SeamLookup lookup = new SeamLookup();
            for (int i = 0; i < asset.Seams.Count; i++)
            {
                lookup.Add(asset.Seams[i]);
            }

            return lookup;
        }

        private static int Find(int[] parents, int index)
        {
            int root = index;
            while (parents[root] != root)
            {
                root = parents[root];
            }

            while (parents[index] != index)
            {
                int next = parents[index];
                parents[index] = root;
                index = next;
            }

            return root;
        }

        private static void Union(
            int[] parents,
            int first,
            int second)
        {
            int firstRoot = Find(parents, first);
            int secondRoot = Find(parents, second);
            if (firstRoot == secondRoot)
            {
                return;
            }

            int representative = Math.Min(
                firstRoot,
                secondRoot);
            int merged = Math.Max(firstRoot, secondRoot);
            parents[merged] = representative;
        }

        private readonly struct WrapSource
        {
            public WrapSource(
                string panelId,
                string operationId,
                int operationIndex)
            {
                PanelId = panelId;
                OperationId = operationId;
                OperationIndex = operationIndex;
            }

            public string PanelId { get; }

            public string OperationId { get; }

            public int OperationIndex { get; }
        }

        private readonly struct RelevantStitch
        {
            public RelevantStitch(
                int operationIndex,
                string operationId,
                int firstWrapIndex,
                int secondWrapIndex)
            {
                OperationIndex = operationIndex;
                OperationId = operationId;
                FirstWrapIndex = firstWrapIndex;
                SecondWrapIndex = secondWrapIndex;
            }

            public int OperationIndex { get; }

            public string OperationId { get; }

            public int FirstWrapIndex { get; }

            public int SecondWrapIndex { get; }
        }

        private sealed class SphereComponentPlan
        {
            public SphereComponentPlan(
                string componentId,
                List<string> panelIds,
                List<string> sphericalWrapOperationIds,
                int lastRelevantStitchOperationIndex,
                string lastRelevantStitchOperationId)
            {
                ComponentId = componentId;
                PanelIds = panelIds;
                SphericalWrapOperationIds =
                    sphericalWrapOperationIds;
                LastRelevantStitchOperationIndex =
                    lastRelevantStitchOperationIndex;
                LastRelevantStitchOperationId =
                    lastRelevantStitchOperationId;
            }

            public string ComponentId { get; }

            public List<string> PanelIds { get; }

            public List<string> SphericalWrapOperationIds { get; }

            public int LastRelevantStitchOperationIndex { get; }

            public string LastRelevantStitchOperationId { get; }

            public bool Validated { get; set; }

            public bool Intersects(IReadOnlyList<string> panelIds)
            {
                if (panelIds == null)
                {
                    return false;
                }

                for (int targetIndex = 0;
                    targetIndex < panelIds.Count;
                    targetIndex++)
                {
                    string target = panelIds[targetIndex];
                    for (int componentIndex = 0;
                        componentIndex < PanelIds.Count;
                        componentIndex++)
                    {
                        if (string.Equals(
                            target,
                            PanelIds[componentIndex],
                            StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private sealed class SeamLookup
        {
            private readonly Dictionary<string, SeamDefinition> seams =
                new Dictionary<string, SeamDefinition>(
                    StringComparer.Ordinal);

            private readonly HashSet<string> duplicateIds =
                new HashSet<string>(StringComparer.Ordinal);

            public void Add(SeamDefinition seam)
            {
                if (seam == null ||
                    string.IsNullOrWhiteSpace(seam.Id))
                {
                    return;
                }

                if (!seams.ContainsKey(seam.Id))
                {
                    seams.Add(seam.Id, seam);
                    return;
                }

                duplicateIds.Add(seam.Id);
            }

            public bool TryGetUnique(
                string seamId,
                out SeamDefinition seam)
            {
                seam = null;
                return !string.IsNullOrWhiteSpace(seamId) &&
                    !duplicateIds.Contains(seamId) &&
                    seams.TryGetValue(seamId, out seam);
            }
        }
    }
}
