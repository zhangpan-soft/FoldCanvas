using System;

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
        public const string InvalidPanelDimensions = "FC1001";
        public const string InvalidTessellation = "FC1002";
        public const string CanvasRectOutOfRange = "FC1003";
        public const string MissingPanelReference = "FC2001";
        public const string UnsupportedSeam = "FC2002";
        public const string UnsupportedOperation = "FC3001";
        public const string NonFiniteOperationParameter = "FC3002";
        public const string NonFiniteVertex = "FC5001";
        public const string ZeroAreaTriangle = "FC5002";
    }

    public sealed class FoldCanvasDiagnostic
    {
        public FoldCanvasDiagnostic(
            string code,
            FoldCanvasDiagnosticSeverity severity,
            string message,
            string panelId = null,
            string operationId = null,
            string seamId = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            PanelId = panelId;
            OperationId = operationId;
            SeamId = seamId;
        }

        public string Code { get; }

        public FoldCanvasDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public string PanelId { get; }

        public string OperationId { get; }

        public string SeamId { get; }

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
    }
}
