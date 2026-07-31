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
            bool[] hasComponentFormingStitch =
                new bool[orderedWraps.Count];
            List<SelectedStitch> selectedStitches =
                new List<SelectedStitch>();
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
                        !TryGetEndpointPanelIds(
                            seam,
                            out string firstPanelId,
                            out string secondPanelId))
                    {
                        continue;
                    }

                    selectedStitches.Add(new SelectedStitch(
                        operationIndex,
                        stitch.Id,
                        firstPanelId,
                        secondPanelId));
                    if (!wrapIndexByPanel.TryGetValue(
                            firstPanelId,
                            out int first) ||
                        !wrapIndexByPanel.TryGetValue(
                            secondPanelId,
                            out int second))
                    {
                        continue;
                    }

                    Union(parents, first, second);
                    hasComponentFormingStitch[first] = true;
                    hasComponentFormingStitch[second] = true;
                }
            }

            Dictionary<int, List<int>> membersByRoot =
                new Dictionary<int, List<int>>();
            for (int i = 0; i < orderedWraps.Count; i++)
            {
                if (!hasComponentFormingStitch[i])
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

                HashSet<string> memberPanelIds =
                    new HashSet<string>(
                        panelIds,
                        StringComparer.Ordinal);
                int lastTouchingStitchIndex = -1;
                string lastTouchingStitchId = null;
                for (int stitchIndex = 0;
                    stitchIndex < selectedStitches.Count;
                    stitchIndex++)
                {
                    SelectedStitch selected =
                        selectedStitches[stitchIndex];
                    if (!selected.Touches(memberPanelIds) ||
                        selected.OperationIndex <
                            lastTouchingStitchIndex)
                    {
                        continue;
                    }

                    lastTouchingStitchIndex =
                        selected.OperationIndex;
                    lastTouchingStitchId =
                        selected.OperationId;
                }

                componentPlans.Add(new SphereComponentPlan(
                    $"sphere-component-{componentIndex:000}",
                    panelIds,
                    operationIds,
                    lastTouchingStitchIndex,
                    lastTouchingStitchId));
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
                    component.LastTouchingStitchOperationIndex ==
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
                    component.LastTouchingStitchOperationIndex)
                {
                    result.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .SphereValidationRequiredBeforeSolidify,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Spherical component '{component.ComponentId}' must complete every Stitch operation that touches the component and closed-sphere validation before Solidify.",
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
                                    .LastTouchingStitchOperationIndex)
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
                        component.LastTouchingStitchOperationId,
                        component
                            .LastTouchingStitchOperationIndex);
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
                    !string.IsNullOrWhiteSpace(panel.Id) &&
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

        private static bool TryGetEndpointPanelIds(
            SeamDefinition seam,
            out string firstPanelId,
            out string secondPanelId)
        {
            firstPanelId = seam?.A.PanelId;
            secondPanelId = seam?.B.PanelId;
            return
                !string.IsNullOrWhiteSpace(firstPanelId) &&
                !string.IsNullOrWhiteSpace(secondPanelId);
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

        private readonly struct SelectedStitch
        {
            public SelectedStitch(
                int operationIndex,
                string operationId,
                string firstPanelId,
                string secondPanelId)
            {
                OperationIndex = operationIndex;
                OperationId = operationId;
                FirstPanelId = firstPanelId;
                SecondPanelId = secondPanelId;
            }

            public int OperationIndex { get; }

            public string OperationId { get; }

            public string FirstPanelId { get; }

            public string SecondPanelId { get; }

            public bool Touches(
                HashSet<string> componentPanelIds)
            {
                return
                    componentPanelIds != null &&
                    (componentPanelIds.Contains(FirstPanelId) ||
                     componentPanelIds.Contains(SecondPanelId));
            }
        }

        private sealed class SphereComponentPlan
        {
            public SphereComponentPlan(
                string componentId,
                List<string> panelIds,
                List<string> sphericalWrapOperationIds,
                int lastTouchingStitchOperationIndex,
                string lastTouchingStitchOperationId)
            {
                ComponentId = componentId;
                PanelIds = panelIds;
                SphericalWrapOperationIds =
                    sphericalWrapOperationIds;
                LastTouchingStitchOperationIndex =
                    lastTouchingStitchOperationIndex;
                LastTouchingStitchOperationId =
                    lastTouchingStitchOperationId;
            }

            public string ComponentId { get; }

            public List<string> PanelIds { get; }

            public List<string> SphericalWrapOperationIds { get; }

            public int LastTouchingStitchOperationIndex { get; }

            public string LastTouchingStitchOperationId { get; }

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
