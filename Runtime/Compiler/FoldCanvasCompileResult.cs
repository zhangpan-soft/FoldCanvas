using System.Collections.Generic;
using UnityEngine;

namespace FoldCanvas
{
    public sealed class FoldCanvasCompileResult
    {
        private readonly List<FoldCanvasDiagnostic> diagnostics = new List<FoldCanvasDiagnostic>();

        public Mesh Mesh { get; internal set; }

        public IReadOnlyList<FoldCanvasDiagnostic> Diagnostics => diagnostics;

        public bool Success
        {
            get
            {
                if (Mesh == null)
                {
                    return false;
                }

                for (int i = 0; i < diagnostics.Count; i++)
                {
                    if (diagnostics[i].Severity == FoldCanvasDiagnosticSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal void Add(FoldCanvasDiagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
        }

        internal bool HasErrors()
        {
            for (int i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Severity == FoldCanvasDiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
