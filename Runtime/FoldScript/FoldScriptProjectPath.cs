using System;
using System.Collections.Generic;

namespace FoldCanvas
{
    public static class FoldScriptProjectPath
    {
        public static bool TryResolveAppearance(
            string sourceProjectPath,
            string appearanceReference,
            out string resolvedProjectPath,
            out FoldCanvasDiagnostic diagnostic)
        {
            resolvedProjectPath = null;
            diagnostic = null;
            if (!TryNormalizeProjectPath(
                    sourceProjectPath,
                    "FoldScript source path",
                    out string source,
                    out diagnostic))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(appearanceReference) ||
                appearanceReference.Length >
                    FoldCanvasLimits.MaximumFoldScriptAppearancePathLength ||
                appearanceReference.IndexOf('\\') >= 0 ||
                appearanceReference.IndexOf('\0') >= 0 ||
                appearanceReference.IndexOf(':') >= 0 ||
                appearanceReference.StartsWith("/", StringComparison.Ordinal))
            {
                diagnostic = Unsafe(
                    "FoldScript appearance must be a bounded project-relative path using forward slashes.");
                return false;
            }

            string candidate;
            if (appearanceReference.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal) ||
                appearanceReference.StartsWith(
                    "Packages/",
                    StringComparison.Ordinal))
            {
                candidate = appearanceReference;
            }
            else
            {
                int slash = source.LastIndexOf('/');
                if (slash < 0)
                {
                    diagnostic = Unsafe(
                        "FoldScript source path must have an approved project directory.");
                    return false;
                }

                candidate = source.Substring(0, slash + 1) +
                    appearanceReference;
            }

            return TryNormalizeProjectPath(
                candidate,
                "FoldScript appearance path",
                out resolvedProjectPath,
                out diagnostic);
        }

        public static bool TryNormalizeProjectPath(
            string path,
            out string normalized,
            out FoldCanvasDiagnostic diagnostic)
        {
            return TryNormalizeProjectPath(
                path,
                "FoldScript project path",
                out normalized,
                out diagnostic);
        }

        private static bool TryNormalizeProjectPath(
            string path,
            string label,
            out string normalized,
            out FoldCanvasDiagnostic diagnostic)
        {
            normalized = null;
            diagnostic = null;
            if (string.IsNullOrWhiteSpace(path) ||
                path.IndexOf('\\') >= 0 ||
                path.IndexOf('\0') >= 0 ||
                path.IndexOf(':') >= 0 ||
                path.StartsWith("/", StringComparison.Ordinal))
            {
                diagnostic = Unsafe(
                    $"{label} must be project-relative and use forward slashes.");
                return false;
            }

            string[] segments = path.Split('/');
            if (segments.Length < 2 ||
                (segments[0] != "Assets" && segments[0] != "Packages"))
            {
                diagnostic = Unsafe(
                    $"{label} must remain under Assets/ or Packages/.");
                return false;
            }

            if (segments[0] == "Packages" && segments.Length < 3)
            {
                diagnostic = Unsafe(
                    $"{label} under Packages/ must include a package name.");
                return false;
            }

            List<string> normalizedSegments =
                new List<string>(segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (segment.Length == 0 || segment == "." || segment == "..")
                {
                    diagnostic = Unsafe(
                        $"{label} contains an empty, dot, or traversal segment.");
                    return false;
                }

                normalizedSegments.Add(segment);
            }

            normalized = string.Join("/", normalizedSegments);
            return true;
        }

        private static FoldCanvasDiagnostic Unsafe(string message)
        {
            return new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.UnsafeAppearancePath,
                FoldCanvasDiagnosticSeverity.Error,
                message);
        }
    }
}
