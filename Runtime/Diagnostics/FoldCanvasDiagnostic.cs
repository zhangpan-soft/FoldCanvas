using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FoldCanvas
{
    public enum FoldCanvasDiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public static class FoldCanvasDiagnosticCodes
    {
        public const string NullAsset = "FC0001";
        public const string DuplicatePanelId = "FC0002";
        public const string DuplicateOperationId = "FC0003";
        public const string EmptyPanelId = "FC0004";
        public const string InvalidCompileLimits = "FC0005";
        public const string InvalidPanelDimensions = "FC1001";
        public const string InvalidTessellation = "FC1002";
        public const string CanvasRectOutOfRange = "FC1003";
        public const string UnsupportedPanelShape = "FC1004";
        public const string NonFinitePanelSize = "FC1005";
        public const string NonPositivePanelSize = "FC1006";
        public const string ExcessiveTessellation = "FC1007";
        public const string MissingPanelReference = "FC2001";
        public const string UnsupportedSeam = "FC2002";
        public const string StitchSeamMissing = "FC2003";
        public const string StitchBoundaryMissing = "FC2004";
        public const string StitchSampleCountMismatch = "FC2005";
        public const string StitchPositionMismatch = "FC2006";
        public const string UnsupportedStitchSeamMode = "FC2007";
        public const string DuplicateSeamId = "FC2008";
        public const string EmptyStitchSeamList = "FC2009";
        public const string StitchMustBeTerminalForSelectedPanels = "FC2010";
        public const string ZeroLengthStitchBoundary = "FC2011";
        public const string StitchBoundaryClosureMismatch = "FC2012";
        public const string StitchBoundarySubdivisionFailed = "FC2013";
        public const string UnsupportedOperation = "FC3001";
        public const string NonFiniteOperationParameter = "FC3002";
        public const string FoldTargetMissing = "FC3003";
        public const string NonFiniteFoldLine = "FC3004";
        public const string FoldLineOutOfRange = "FC3005";
        public const string DegenerateFoldLine = "FC3006";
        public const string AmbiguousFoldHinge = "FC3007";
        public const string NonFiniteFoldAngle = "FC3008";
        public const string UnsupportedFoldFalloff = "FC3009";
        public const string InvalidFoldSide = "FC3010";
        public const string FoldCreaseRequiresTopologySplit = "FC3011";
        public const string RollTargetMissing = "FC3012";
        public const string NonFiniteRollParameter = "FC3013";
        public const string NearZeroRollAngle = "FC3014";
        public const string InvalidExplicitRollRadius = "FC3015";
        public const string UnsupportedFitTargetBoundary = "FC3016";
        public const string UnsupportedRollPanelShape = "FC3017";
        public const string RollStretchReport = "FC3018";
        public const string InvalidRollDirection = "FC3019";
        public const string InvalidRollRadiusMode = "FC3020";
        public const string UnsupportedRollEmbedding = "FC3021";
        public const string InsufficientRollTessellation = "FC3022";
        public const string UnsupportedMultiTurnRoll = "FC3023";
        public const string InvalidSolidifyThickness = "FC4001";
        public const string SolidifyTargetMissing = "FC4002";
        public const string IncompleteSolidifyTopologySelection = "FC4003";
        public const string UnsupportedSolidifyCorner = "FC4004";
        public const string NonManifoldSolidifyInput = "FC4005";
        public const string InvalidSolidifyDirection = "FC4006";
        public const string NonFiniteVertex = "FC5001";
        public const string ZeroAreaTriangle = "FC5002";
        public const string NonManifoldTopology = "FC5003";
        public const string InvalidWeldEpsilon = "FC5004";
    }

    public sealed class FoldCanvasDiagnosticValue
    {
        public FoldCanvasDiagnosticValue(
            string key,
            double value,
            string unit = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Diagnostic value keys must be non-empty.",
                    nameof(key));
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Diagnostic values must be finite.");
            }

            Key = key;
            Value = value;
            Unit = unit;
        }

        public string Key { get; }

        public double Value { get; }

        public string Unit { get; }
    }

    public sealed class FoldCanvasDiagnostic
    {
        public FoldCanvasDiagnostic(
            string code,
            FoldCanvasDiagnosticSeverity severity,
            string message,
            string panelId = null,
            string operationId = null,
            string seamId = null,
            IEnumerable<FoldCanvasDiagnosticValue> values = null,
            IEnumerable<string> repairSuggestions = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            PanelId = panelId;
            OperationId = operationId;
            SeamId = seamId;
            Values = CopyValues(values);
            RepairSuggestions = CopyRepairSuggestions(repairSuggestions);
        }

        public string Code { get; }

        public FoldCanvasDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public string PanelId { get; }

        public string OperationId { get; }

        public string SeamId { get; }

        public IReadOnlyList<FoldCanvasDiagnosticValue> Values { get; }

        public IReadOnlyList<string> RepairSuggestions { get; }

        public override string ToString()
        {
            string context = string.Empty;
            if (!string.IsNullOrEmpty(PanelId))
            {
                context += $" panel={PanelId}";
            }

            if (!string.IsNullOrEmpty(OperationId))
            {
                context += $" operation={OperationId}";
            }

            if (!string.IsNullOrEmpty(SeamId))
            {
                context += $" seam={SeamId}";
            }

            return $"[{Severity}] {Code}{context}: {Message}";
        }

        private static IReadOnlyList<FoldCanvasDiagnosticValue> CopyValues(
            IEnumerable<FoldCanvasDiagnosticValue> values)
        {
            if (values == null)
            {
                return Array.AsReadOnly(
                    Array.Empty<FoldCanvasDiagnosticValue>());
            }

            List<FoldCanvasDiagnosticValue> copy =
                new List<FoldCanvasDiagnosticValue>();
            foreach (FoldCanvasDiagnosticValue value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "Diagnostic values cannot contain null entries.",
                        nameof(values));
                }

                copy.Add(value);
            }

            return new ReadOnlyCollection<FoldCanvasDiagnosticValue>(copy);
        }

        private static IReadOnlyList<string> CopyRepairSuggestions(
            IEnumerable<string> repairSuggestions)
        {
            if (repairSuggestions == null)
            {
                return Array.AsReadOnly(Array.Empty<string>());
            }

            List<string> copy = new List<string>();
            foreach (string repairSuggestion in repairSuggestions)
            {
                if (repairSuggestion == null)
                {
                    throw new ArgumentException(
                        "Repair suggestions cannot contain null entries.",
                        nameof(repairSuggestions));
                }

                copy.Add(repairSuggestion);
            }

            return new ReadOnlyCollection<string>(copy);
        }
    }
}
