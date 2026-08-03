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
        public const string InvalidValidationLevel = "FC0006";
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
        public const string StitchSampleCountOutOfRange = "FC2014";
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
        public const string SolidifyClosedVolumeValidationFailed = "FC4007";
        public const string NonFiniteVertex = "FC5001";
        public const string ZeroAreaTriangle = "FC5002";
        public const string NonManifoldTopology = "FC5003";
        public const string InvalidWeldEpsilon = "FC5004";
        public const string GeneratedVertexLimitExceeded = "FC5005";
        public const string GeneratedTriangleLimitExceeded = "FC5006";
        public const string GeometryBudgetOverflow = "FC5007";
        public const string InvalidTriangleIndex = "FC5008";
        public const string IncompleteTriangleIndexBuffer = "FC5009";
        public const string DuplicateTriangle = "FC5010";
        public const string OpenTopologyBoundary = "FC5011";
        public const string InconsistentWinding = "FC5012";
        public const string DisconnectedGeometry = "FC5013";
        public const string SeamClosureMismatch = "FC5014";
        public const string BowTieTopologyVertex = "FC5015";
        public const string TopologyPositionConflict = "FC5016";
        public const string InvertedClosedComponent = "FC5017";
        public const string SelfIntersection = "FC5018";
        public const string StrictValidationBudgetExceeded = "FC5019";
        public const string DegenerateCompiledBoundary = "FC5020";
        public const string SphericalWrapTargetMissing = "FC6001";
        public const string UnsupportedSphericalWrapPanelShape = "FC6002";
        public const string NonFiniteSphericalWrapParameter = "FC6003";
        public const string InvalidSphericalRadius = "FC6004";
        public const string CollapsedSphericalRange = "FC6005";
        public const string UnsupportedSphericalMultiTurn = "FC6006";
        public const string InvalidSphericalWrapDirection = "FC6007";
        public const string InvalidSphericalPoleMode = "FC6008";
        public const string UnsupportedSphericalSubdivisionMode = "FC6009";
        public const string UnsupportedSphericalEmbedding = "FC6010";
        public const string InsufficientSphericalPoleTessellation = "FC6011";
        public const string SphericalRadiusError = "FC6012";
        public const string InvalidSphericalPoleTopology = "FC6013";
        public const string SphereValidationFailed = "FC6014";
        public const string DuplicateSphericalWrapTarget = "FC6015";
        public const string SphereValidationRequiredBeforeSolidify = "FC6016";
        public const string MalformedFoldScript = "FC7001";
        public const string UnsupportedFoldScriptVersion = "FC7002";
        public const string InvalidFoldScriptStructure = "FC7003";
        public const string UnknownFoldScriptOperation = "FC7004";
        public const string NonFiniteFoldScriptNumber = "FC7005";
        public const string UnsafeAppearancePath = "FC7006";
        public const string FoldScriptInputLimitExceeded = "FC7007";
        public const string DuplicateFoldScriptIdentifier = "FC7008";
        public const string FoldScriptAppearanceResolutionFailed = "FC7009";
        public const string InvalidFoldScriptReference = "FC7010";
        public const string InvalidRepairResponse = "FC7011";
        public const string DuplicateFoldScriptProperty = "FC7012";
        public const string InvalidBoundarySpan = "FC8001";
        public const string ToroidalWrapTargetMissing = "FC8002";
        public const string UnsupportedToroidalWrapPanelShape = "FC8003";
        public const string NonFiniteToroidalWrapParameter = "FC8004";
        public const string InvalidToroidalRadius = "FC8005";
        public const string CollapsedToroidalRange = "FC8006";
        public const string UnsupportedToroidalMultiTurn = "FC8007";
        public const string UnsupportedToroidalEmbedding = "FC8008";
        public const string InsufficientToroidalTessellation = "FC8009";
        public const string ToroidalWindingFailed = "FC8010";
        public const string InvalidToroidalWrapDirection = "FC8011";
        public const string UnregisteredExtensionOperation = "FC9001";
        public const string InvalidExtensionRegistration = "FC9002";
        public const string DuplicateExtensionRegistration = "FC9003";
        public const string ExtensionOperationTargetMissing = "FC9004";
        public const string ExtensionOperationValidationFailed = "FC9005";
        public const string ExtensionOperationExecutionFailed = "FC9006";
        public const string ExtensionOperationInvalidMutation = "FC9007";
        public const string ExtensionOperationException = "FC9008";
        public const string MalformedGalleryManifest = "FC9101";
        public const string UnsupportedGalleryVersion = "FC9102";
        public const string InvalidGalleryEntry = "FC9103";
        public const string DuplicateGalleryEntryId = "FC9104";
        public const string ExportInputMissing = "FC9201";
        public const string InvalidExportOptions = "FC9202";
        public const string InvalidExportGeometry = "FC9203";
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

    public sealed class FoldCanvasDiagnosticGeometryContext
    {
        public FoldCanvasDiagnosticGeometryContext(
            int vertexIndex = -1,
            int topologyVertexId = -1,
            int componentIndex = -1,
            int triangleIndex = -1,
            int relatedTriangleIndex = -1,
            int edgeTopologyVertexA = -1,
            int edgeTopologyVertexB = -1)
        {
            ValidateOptionalIndex(vertexIndex, nameof(vertexIndex));
            ValidateOptionalIndex(
                topologyVertexId,
                nameof(topologyVertexId));
            ValidateOptionalIndex(componentIndex, nameof(componentIndex));
            ValidateOptionalIndex(triangleIndex, nameof(triangleIndex));
            ValidateOptionalIndex(
                relatedTriangleIndex,
                nameof(relatedTriangleIndex));
            ValidateOptionalIndex(
                edgeTopologyVertexA,
                nameof(edgeTopologyVertexA));
            ValidateOptionalIndex(
                edgeTopologyVertexB,
                nameof(edgeTopologyVertexB));

            VertexIndex = vertexIndex;
            TopologyVertexId = topologyVertexId;
            ComponentIndex = componentIndex;
            TriangleIndex = triangleIndex;
            RelatedTriangleIndex = relatedTriangleIndex;
            EdgeTopologyVertexA = edgeTopologyVertexA;
            EdgeTopologyVertexB = edgeTopologyVertexB;
        }

        public int VertexIndex { get; }

        public int TopologyVertexId { get; }

        public int ComponentIndex { get; }

        public int TriangleIndex { get; }

        public int RelatedTriangleIndex { get; }

        public int EdgeTopologyVertexA { get; }

        public int EdgeTopologyVertexB { get; }

        public bool HasValue =>
            VertexIndex >= 0 ||
            TopologyVertexId >= 0 ||
            ComponentIndex >= 0 ||
            TriangleIndex >= 0 ||
            RelatedTriangleIndex >= 0 ||
            EdgeTopologyVertexA >= 0 ||
            EdgeTopologyVertexB >= 0;

        private static void ValidateOptionalIndex(
            int value,
            string parameterName)
        {
            if (value < -1)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Diagnostic geometry indices must be -1 or non-negative.");
            }
        }
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
            IEnumerable<string> repairSuggestions = null,
            FoldCanvasDiagnosticGeometryContext geometryContext = null,
            string boundaryId = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            PanelId = panelId;
            OperationId = operationId;
            SeamId = seamId;
            BoundaryId = boundaryId;
            Values = CopyValues(values);
            RepairSuggestions = CopyRepairSuggestions(repairSuggestions);
            GeometryContext = geometryContext;
        }

        public string Code { get; }

        public FoldCanvasDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public string PanelId { get; }

        public string OperationId { get; }

        public string SeamId { get; }

        public string BoundaryId { get; }

        public IReadOnlyList<FoldCanvasDiagnosticValue> Values { get; }

        public IReadOnlyList<string> RepairSuggestions { get; }

        public FoldCanvasDiagnosticGeometryContext GeometryContext { get; }

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

            if (!string.IsNullOrEmpty(BoundaryId))
            {
                context += $" boundary={BoundaryId}";
            }

            if (GeometryContext != null && GeometryContext.HasValue)
            {
                if (GeometryContext.VertexIndex >= 0)
                {
                    context += $" vertex={GeometryContext.VertexIndex}";
                }

                if (GeometryContext.TopologyVertexId >= 0)
                {
                    context +=
                        $" topologyVertex={GeometryContext.TopologyVertexId}";
                }

                if (GeometryContext.ComponentIndex >= 0)
                {
                    context +=
                        $" component={GeometryContext.ComponentIndex}";
                }

                if (GeometryContext.TriangleIndex >= 0)
                {
                    context +=
                        $" triangle={GeometryContext.TriangleIndex}";
                }

                if (GeometryContext.RelatedTriangleIndex >= 0)
                {
                    context +=
                        $" relatedTriangle={GeometryContext.RelatedTriangleIndex}";
                }

                if (GeometryContext.EdgeTopologyVertexA >= 0 &&
                    GeometryContext.EdgeTopologyVertexB >= 0)
                {
                    context +=
                        $" edge=({GeometryContext.EdgeTopologyVertexA}," +
                        $"{GeometryContext.EdgeTopologyVertexB})";
                }
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
