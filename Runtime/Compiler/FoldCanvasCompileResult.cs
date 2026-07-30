using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FoldCanvas
{
    public sealed class FoldCanvasCompileResult
    {
        private readonly List<FoldCanvasDiagnostic> diagnostics = new List<FoldCanvasDiagnostic>();
        private readonly List<FoldCanvasClosedVolumeReport>
            solidifyClosedVolumeReports =
                new List<FoldCanvasClosedVolumeReport>();
        private readonly ReadOnlyCollection<FoldCanvasClosedVolumeReport>
            readOnlySolidifyClosedVolumeReports;

        private readonly List<FoldCanvasSphereReport> sphereReports =
            new List<FoldCanvasSphereReport>();

        private readonly ReadOnlyCollection<FoldCanvasSphereReport>
            readOnlySphereReports;

        public FoldCanvasCompileResult()
        {
            readOnlySolidifyClosedVolumeReports =
                solidifyClosedVolumeReports.AsReadOnly();
            readOnlySphereReports = sphereReports.AsReadOnly();
        }

        public Mesh Mesh { get; internal set; }

        public FoldCanvasCompiledData CompiledData { get; internal set; }

        public FoldCanvasClosedVolumeReport ClosedVolumeReport
        {
            get;
            internal set;
        }

        public FoldCanvasSphereReport SphereReport
        {
            get;
            private set;
        }

        public IReadOnlyList<FoldCanvasSphereReport> SphereReports =>
            readOnlySphereReports;

        public IReadOnlyList<FoldCanvasClosedVolumeReport>
            SolidifyClosedVolumeReports =>
                readOnlySolidifyClosedVolumeReports;

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

        internal void AddClosedVolumeReport(
            FoldCanvasClosedVolumeReport report)
        {
            solidifyClosedVolumeReports.Add(report);
        }

        internal void AddSphereReport(FoldCanvasSphereReport report)
        {
            if (report == null)
            {
                throw new System.ArgumentNullException(nameof(report));
            }

            sphereReports.Add(report);
            if (SphereReport == null)
            {
                SphereReport = report;
            }
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
