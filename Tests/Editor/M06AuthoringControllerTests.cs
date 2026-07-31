using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoldCanvas.Tests
{
    public sealed class M06AuthoringControllerTests
    {
        private const string BakeTestFolder =
            "Assets/FoldCanvasM06AuthoringTests";

        private FoldCanvasAsset asset;
        private FoldCanvas.Editor.FoldCanvasAuthoringController controller;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
            asset = ScriptableObject.CreateInstance<FoldCanvasAsset>();
            asset.name = "M06AuthoringTest";
            controller =
                new FoldCanvas.Editor.FoldCanvasAuthoringController();
            controller.SetSource(asset, 0d);
        }

        [TearDown]
        public void TearDown()
        {
            controller?.Dispose();
            if (asset != null)
            {
                Object.DestroyImmediate(asset);
            }

            Undo.ClearAll();
            AssetDatabase.DeleteAsset(BakeTestFolder);
        }

        [Test]
        public void CreateRectanglePanel_RecordsUndoAndStableId()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);

            Assert.That(panel.Id, Is.EqualTo("rectangle-1"));
            Assert.That(asset.Panels.Count, Is.EqualTo(1));
            Assert.That(panel.Shape, Is.EqualTo(PanelShape.Rectangle));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(asset.Panels, Is.Empty);
        }

        [Test]
        public void CreateDiskPanel_RecordsUndoAndStableId()
        {
            PanelDefinition first = controller.AddPanel(
                PanelShape.Disk,
                0.01d);
            PanelDefinition second = controller.AddPanel(
                PanelShape.Disk,
                0.02d);

            Assert.That(first.Id, Is.EqualTo("disk-1"));
            Assert.That(second.Id, Is.EqualTo("disk-2"));
            Assert.That(first.Shape, Is.EqualTo(PanelShape.Disk));
            Assert.That(first.RadialSegments, Is.EqualTo(32));
        }

        [Test]
        public void EditCanvasRect_UndoRedoRestoresExactValues()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            Rect original = panel.CanvasRect;
            Rect changed = new Rect(0.11f, 0.17f, 0.42f, 0.31f);

            controller.MutatePanel(
                panel.Id,
                "Edit Panel Canvas Rect",
                value => value.CanvasRect = changed,
                0.02d);
            Assert.That(asset.Panels[0].CanvasRect, Is.EqualTo(changed));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(asset.Panels[0].CanvasRect, Is.EqualTo(original));

            Undo.PerformRedo();
            Assert.That(asset.Panels[0].CanvasRect, Is.EqualTo(changed));
        }

        [Test]
        public void CanvasZoom_KeepsSourcePointUnderCursor()
        {
            Vector2 pan = new Vector2(18f, -11f);
            Vector2 cursor = new Vector2(330f, 90f);
            Vector2 center = new Vector2(240f, 160f);
            const float oldZoom = 1.25f;
            const float newZoom = 2.5f;

            Vector2 sourceBefore =
                (cursor - center - pan) / oldZoom;
            Vector2 zoomedPan = FoldCanvas.Editor.FoldCanvasCanvasView
                .CalculateCursorCenteredZoomPan(
                    pan,
                    cursor,
                    center,
                    oldZoom,
                    newZoom);
            Vector2 sourceAfter =
                (cursor - center - zoomedPan) / newZoom;

            Assert.That(sourceAfter.x, Is.EqualTo(sourceBefore.x).Within(1e-5f));
            Assert.That(sourceAfter.y, Is.EqualTo(sourceBefore.y).Within(1e-5f));
        }

        [Test]
        public void CanvasPan_IsBoundedToKeepCanvasVisible()
        {
            Vector2 clamped = FoldCanvas.Editor.FoldCanvasCanvasView
                .ClampPanToVisibleCanvas(
                    new Vector2(999f, -999f),
                    new Vector2(400f, 300f),
                    new Vector2(200f, 100f));

            Assert.That(clamped, Is.EqualTo(new Vector2(252f, -152f)));
        }

        [Test]
        public void RenamePanel_UndoRedoRestoresReferences()
        {
            PanelDefinition wall = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            PanelDefinition bottom = controller.AddPanel(
                PanelShape.Disk,
                0.02d);
            SeamDefinition seam = controller.AddSeam(
                wall.Id,
                "vMin",
                bottom.Id,
                "perimeter",
                SeamMode.Weld,
                0.03d);
            FoldOperationDefinition place = controller.AddOperation(
                FoldOperationType.RigidTransform,
                wall.Id,
                seam.Id,
                0.04d);
            FoldOperationDefinition roll = controller.AddOperation(
                FoldOperationType.Roll,
                wall.Id,
                seam.Id,
                0.05d);
            SolidifyOperationDefinition solidify =
                (SolidifyOperationDefinition)controller.AddOperation(
                    FoldOperationType.Solidify,
                    wall.Id,
                    seam.Id,
                    0.06d);
            SphericalWrapOperationDefinition sphericalWrap =
                (SphericalWrapOperationDefinition)controller.AddOperation(
                    FoldOperationType.SphericalWrap,
                    wall.Id,
                    seam.Id,
                    0.07d);

            Assert.That(
                controller.RenamePanel(wall.Id, "cup-wall", 0.08d),
                Is.True);

            Assert.That(asset.Panels[0].Id, Is.EqualTo("cup-wall"));
            Assert.That(seam.A.PanelId, Is.EqualTo("cup-wall"));
            Assert.That(
                ((RigidTransformOperationDefinition)place).PanelId,
                Is.EqualTo("cup-wall"));
            Assert.That(
                ((RollOperationDefinition)roll).PanelId,
                Is.EqualTo("cup-wall"));
            Assert.That(solidify.PanelIds, Does.Contain("cup-wall"));
            Assert.That(sphericalWrap.PanelId, Is.EqualTo("cup-wall"));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(asset.Panels[0].Id, Is.EqualTo("rectangle-1"));
            Assert.That(asset.Seams[0].A.PanelId, Is.EqualTo("rectangle-1"));

            Undo.PerformRedo();

            Assert.That(asset.Panels[0].Id, Is.EqualTo("cup-wall"));
            Assert.That(asset.Seams[0].A.PanelId, Is.EqualTo("cup-wall"));
        }

        [Test]
        public void ReorderOperation_IsDeterministicAndUndoable()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            controller.AddOperation(
                FoldOperationType.Roll,
                panel.Id,
                null,
                0.02d);
            controller.AddOperation(
                FoldOperationType.RigidTransform,
                panel.Id,
                null,
                0.03d);

            Assert.That(controller.MoveOperation(1, 0, 0.04d), Is.True);
            Assert.That(
                asset.Operations[0].Type,
                Is.EqualTo(FoldOperationType.RigidTransform));
            Assert.That(
                asset.Operations[1].Type,
                Is.EqualTo(FoldOperationType.Roll));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(
                asset.Operations[0].Type,
                Is.EqualTo(FoldOperationType.Roll));
            Assert.That(
                asset.Operations[1].Type,
                Is.EqualTo(FoldOperationType.RigidTransform));
        }

        [Test]
        public void SourceChange_DebouncesCompileInsteadOfRepainting()
        {
            controller.AddPanel(PanelShape.Rectangle, 0.01d);

            Assert.That(controller.TryCompile(0.10d), Is.False);
            Assert.That(controller.TryCompile(0.20d), Is.False);
            Assert.That(controller.CompileCount, Is.Zero);
            Assert.That(controller.TryCompile(0.21d), Is.True);
            Assert.That(controller.CompileCount, Is.EqualTo(1));
            Assert.That(controller.TryCompile(1d), Is.False);
            Assert.That(controller.CompileCount, Is.EqualTo(1));
        }

        [Test]
        public void NewerRevision_DiscardsStalePreviewResult()
        {
            controller.Dispose();
            FoldCanvas.Editor.FoldCanvasAuthoringController reentrant = null;
            int calls = 0;
            reentrant = new FoldCanvas.Editor.FoldCanvasAuthoringController(
                source =>
                {
                    FoldCanvasCompileResult result =
                        FoldCanvasCompiler.Compile(source);
                    if (calls++ == 0)
                    {
                        reentrant.RequestCompile(0.2d);
                    }

                    return result;
                });
            controller = reentrant;
            controller.SetSource(asset, 0d);
            controller.AddPanel(PanelShape.Rectangle, 0.01d);

            Assert.That(controller.TryCompile(0.21d), Is.False);
            Assert.That(controller.LastCompileResult, Is.Null);
            Assert.That(controller.HasPendingCompile, Is.True);
            Assert.That(controller.TryCompile(0.42d), Is.True);
            Assert.That(controller.LastCompileResult, Is.Not.Null);
            Assert.That(controller.LastCompileResult.Success, Is.True);
            Assert.That(controller.CompileCount, Is.EqualTo(2));
        }

        [Test]
        public void CreateSeam_UsesNamedBoundaryReferencesAndUndo()
        {
            PanelDefinition wall = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            PanelDefinition bottom = controller.AddPanel(
                PanelShape.Disk,
                0.02d);

            SeamDefinition seam = controller.AddSeam(
                wall.Id,
                "vMin",
                bottom.Id,
                "perimeter",
                SeamMode.Weld,
                0.03d);

            Assert.That(seam.A.PanelId, Is.EqualTo(wall.Id));
            Assert.That(seam.A.BoundaryId, Is.EqualTo("vMin"));
            Assert.That(seam.B.PanelId, Is.EqualTo(bottom.Id));
            Assert.That(seam.B.BoundaryId, Is.EqualTo("perimeter"));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();

            Assert.That(asset.Seams, Is.Empty);
        }

        [Test]
        public void InvalidSource_ShowsStableOrderedDiagnostics()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            controller.MutatePanel(
                panel.Id,
                "Make Invalid Panel",
                changed =>
                {
                    changed.PhysicalSize = Vector2.zero;
                    changed.CanvasRect = new Rect(-0.1f, 0f, 0.4f, 0.4f);
                    changed.USegments = 0;
                },
                0.02d);

            controller.CompileImmediately(0.3d);
            FoldCanvasCompileResult first = controller.LastCompileResult;
            string firstCodes = JoinDiagnosticCodes(first);
            controller.CompileImmediately(0.4d);
            FoldCanvasCompileResult second = controller.LastCompileResult;

            Assert.That(first.Success, Is.False);
            Assert.That(first.Mesh, Is.Null);
            Assert.That(
                firstCodes,
                Is.EqualTo(
                    FoldCanvasDiagnosticCodes.NonPositivePanelSize + "," +
                    FoldCanvasDiagnosticCodes.CanvasRectOutOfRange + "," +
                    FoldCanvasDiagnosticCodes.InvalidTessellation));
            Assert.That(JoinDiagnosticCodes(second), Is.EqualTo(firstCodes));
        }

        [Test]
        public void OpenWorkspace_TwiceReusesOneWindow()
        {
            FoldCanvas.Editor.FoldCanvasWindow first = null;
            try
            {
                first = FoldCanvas.Editor.FoldCanvasWindow
                    .OpenAuthoringWorkspace();
                FoldCanvas.Editor.FoldCanvasWindow second =
                    FoldCanvas.Editor.FoldCanvasWindow
                        .OpenAuthoringWorkspace();

                Assert.That(second, Is.SameAs(first));
                Assert.That(
                    first.rootVisualElement.Q<VisualElement>("canvas-host"),
                    Is.Not.Null);
                Assert.That(
                    first.rootVisualElement.Q<VisualElement>("preview-host"),
                    Is.Not.Null);
            }
            finally
            {
                if (first != null)
                {
                    if (Application.isBatchMode)
                    {
                        Object.DestroyImmediate(first);
                    }
                    else
                    {
                        first.Close();
                    }
                }
            }
        }

        [Test]
        public void DiagnosticFocus_SelectsPanelOrOperationContext()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            FoldOperationDefinition operation = controller.AddOperation(
                FoldOperationType.Roll,
                panel.Id,
                null,
                0.02d);
            FoldCanvas.Editor.FoldCanvasWindow window = null;
            try
            {
                window = FoldCanvas.Editor.FoldCanvasWindow
                    .OpenAuthoringWorkspace();
                window.SetSourceForTests(asset);
                window.FocusDiagnosticForTests(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.RollTargetMissing,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Focus test.",
                    operationId: operation.Id));

                Assert.That(window.SelectedOperationIndexForTests, Is.Zero);

                window.FocusDiagnosticForTests(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.NonPositivePanelSize,
                    FoldCanvasDiagnosticSeverity.Error,
                    "Focus test.",
                    panelId: panel.Id));

                Assert.That(
                    window.SelectedPanelIdForTests,
                    Is.EqualTo(panel.Id));
            }
            finally
            {
                DestroyWorkspaceWindow(window);
            }
        }

        [Test]
        public void WorkspacePreview_IsEditorOnlyAndOwned()
        {
            using (FoldCanvas.Editor.FoldCanvasPreviewRenderer preview =
                new FoldCanvas.Editor.FoldCanvasPreviewRenderer())
            {
                GameObject root = preview.PreviewRoot;

                Assert.That(root, Is.Not.Null);
                Assert.That(root.name, Does.Contain("M06 Workspace Preview"));
                Assert.That(root.CompareTag("EditorOnly"), Is.True);
                Assert.That(
                    root.hideFlags,
                    Is.EqualTo(HideFlags.HideAndDontSave));
                Assert.That(root.transform.childCount, Is.EqualTo(5));
            }
        }

        [Test]
        public void WorkspaceSolidView_UsesOneSidedDiagnosticMaterial()
        {
            controller.AddPanel(PanelShape.Rectangle, 0.01d);
            controller.CompileImmediately(0.3d);
            Texture2D appearance = new Texture2D(2, 2);
            try
            {
                using (FoldCanvas.Editor.FoldCanvasPreviewRenderer preview =
                    new FoldCanvas.Editor.FoldCanvasPreviewRenderer())
                {
                    preview.SetResult(
                        controller.LastCompileResult,
                        appearance,
                        null);
                    Assert.That(
                        preview.SurfaceMaterialForTests.name,
                        Is.EqualTo("M06 Preview Textured"));

                    preview.SetSolid(true);

                    Assert.That(
                        preview.SurfaceMaterialForTests.name,
                        Is.EqualTo("M06 Preview Solid"));
                    Assert.That(
                        preview.SurfaceMaterialForTests.shader.name,
                        Is.EqualTo("FoldCanvas/One-Sided Solid Color"));
                }
            }
            finally
            {
                Object.DestroyImmediate(appearance);
            }
        }

        [Test]
        public void Workspace_DoesNotModifyMainCamera()
        {
            GameObject cameraObject = new GameObject("User Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.fieldOfView = 47f;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.transform.position = new Vector3(3f, 4f, 5f);
            camera.transform.rotation = Quaternion.Euler(7f, 13f, 19f);
            try
            {
                using (FoldCanvas.Editor.FoldCanvasPreviewRenderer preview =
                    new FoldCanvas.Editor.FoldCanvasPreviewRenderer())
                {
                    Assert.That(preview.PreviewRoot, Is.Not.Null);
                }

                Assert.That(camera.fieldOfView, Is.EqualTo(47f));
                Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.Depth));
                Assert.That(
                    camera.transform.position,
                    Is.EqualTo(new Vector3(3f, 4f, 5f)));
                Assert.That(
                    camera.transform.rotation.eulerAngles,
                    Is.EqualTo(Quaternion.Euler(7f, 13f, 19f).eulerAngles));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void WorkspaceRecompile_ReplacesAndDestroysDerivedMesh()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            controller.CompileImmediately(0.21d);
            Mesh first = controller.LastCompileResult.Mesh;
            Assert.That(first, Is.Not.Null);

            controller.MutatePanel(
                panel.Id,
                "Resize Panel",
                changed => changed.PhysicalSize = new Vector2(2f, 1f),
                0.3d);
            controller.CompileImmediately(0.51d);
            Mesh second = controller.LastCompileResult.Mesh;

            Assert.That(first == null, Is.True);
            Assert.That(second, Is.Not.Null);
            Assert.That(object.ReferenceEquals(second, first), Is.False);
        }

        [Test]
        public void WorkspaceClose_CleansPreviewResources()
        {
            FoldCanvas.Editor.FoldCanvasPreviewRenderer preview =
                new FoldCanvas.Editor.FoldCanvasPreviewRenderer();
            GameObject root = preview.PreviewRoot;

            preview.Dispose();

            Assert.That(root == null, Is.True);
        }

        [Test]
        public void FailedCompile_PreservesNoPreviewMesh()
        {
            PanelDefinition panel = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            controller.CompileImmediately(0.3d);
            using (FoldCanvas.Editor.FoldCanvasPreviewRenderer preview =
                new FoldCanvas.Editor.FoldCanvasPreviewRenderer())
            {
                preview.SetResult(
                    controller.LastCompileResult,
                    null,
                    null);
                Assert.That(preview.SurfaceMeshForTests, Is.Not.Null);

                controller.MutatePanel(
                    panel.Id,
                    "Invalidate Panel",
                    changed => changed.PhysicalSize = Vector2.zero,
                    0.4d);
                controller.CompileImmediately(0.7d);
                preview.SetResult(
                    controller.LastCompileResult,
                    null,
                    null);

                Assert.That(controller.LastCompileResult.Success, Is.False);
                Assert.That(preview.SurfaceMeshForTests, Is.Null);
            }
        }

        [Test]
        public void BlankAsset_ToCupSource_CompilesWithoutCodeEdits()
        {
            const float radius = 0.05f;
            const float height = 0.12f;
            PanelDefinition wall = controller.AddPanel(
                PanelShape.Rectangle,
                0.01d);
            controller.RenamePanel(wall.Id, "wall", 0.02d);
            controller.MutatePanel(
                "wall",
                "Configure Wall",
                changed =>
                {
                    changed.CanvasRect =
                        new Rect(0.06f, 0.46f, 0.88f, 0.44f);
                    changed.PhysicalSize = new Vector2(
                        2f * Mathf.PI * radius,
                        height);
                    changed.USegments = 64;
                    changed.VSegments = 12;
                },
                0.03d);

            PanelDefinition bottom = controller.AddPanel(
                PanelShape.Disk,
                0.04d);
            controller.RenamePanel(bottom.Id, "bottom", 0.05d);
            controller.MutatePanel(
                "bottom",
                "Configure Bottom",
                changed =>
                {
                    changed.CanvasRect =
                        new Rect(0.32f, 0.02f, 0.36f, 0.36f);
                    changed.PhysicalSize = Vector2.one * (2f * radius);
                    changed.RadialSegments = 64;
                    changed.RadialRings = 8;
                },
                0.06d);

            SeamDefinition closeWall = controller.AddSeam(
                "wall",
                "uMin",
                "wall",
                "uMax",
                SeamMode.Weld,
                0.07d);
            controller.MutateSeam(
                closeWall.Id,
                "Configure Wall Seam",
                changed => changed.SampleCount = 13,
                0.08d);
            SeamDefinition attachBottom = controller.AddSeam(
                "wall",
                "vMin",
                "bottom",
                "perimeter",
                SeamMode.Weld,
                0.09d);
            controller.MutateSeam(
                attachBottom.Id,
                "Configure Bottom Seam",
                changed => changed.SampleCount = 64,
                0.10d);

            RollOperationDefinition roll =
                (RollOperationDefinition)controller.AddOperation(
                    FoldOperationType.Roll,
                    "wall",
                    null,
                    0.11d);
            controller.MutateOperation(
                0,
                "Configure Wall Roll",
                changed =>
                {
                    RollOperationDefinition operation =
                        (RollOperationDefinition)changed;
                    operation.Id = "roll-wall";
                    operation.Direction = RollDirection.U;
                    operation.AngleDegrees = 360f;
                    operation.RadiusMode = RollRadiusMode.PreserveArcLength;
                    operation.ExplicitRadius = radius;
                    operation.StartAngleDegrees = 180f;
                },
                0.12d);
            Assert.That(roll.PanelId, Is.EqualTo("wall"));

            controller.AddOperation(
                FoldOperationType.RigidTransform,
                "bottom",
                null,
                0.13d);
            controller.MutateOperation(
                1,
                "Configure Bottom Placement",
                changed =>
                {
                    RigidTransformOperationDefinition operation =
                        (RigidTransformOperationDefinition)changed;
                    operation.Id = "place-bottom";
                    operation.Translation =
                        new Vector3(0f, -height * 0.5f, 0f);
                    operation.RotationEuler = new Vector3(90f, 0f, 0f);
                    operation.Scale = Vector3.one;
                },
                0.14d);

            StitchOperationDefinition stitch =
                (StitchOperationDefinition)controller.AddOperation(
                    FoldOperationType.Stitch,
                    "wall",
                    closeWall.Id,
                    0.15d);
            controller.MutateOperation(
                2,
                "Configure Cup Stitch",
                changed =>
                {
                    StitchOperationDefinition operation =
                        (StitchOperationDefinition)changed;
                    operation.Id = "stitch-cup";
                    operation.SeamIds.Add(attachBottom.Id);
                },
                0.16d);
            Assert.That(stitch.SeamIds.Count, Is.EqualTo(2));

            SolidifyOperationDefinition solidify =
                (SolidifyOperationDefinition)controller.AddOperation(
                    FoldOperationType.Solidify,
                    "wall",
                    null,
                    0.17d);
            controller.MutateOperation(
                3,
                "Configure Cup Thickness",
                changed =>
                {
                    SolidifyOperationDefinition operation =
                        (SolidifyOperationDefinition)changed;
                    operation.Id = "solidify-cup";
                    operation.Thickness = 0.004f;
                    operation.Direction = SolidifyDirection.Inward;
                    operation.PanelIds.Add("bottom");
                },
                0.18d);
            Assert.That(solidify.PanelIds.Count, Is.EqualTo(2));

            controller.CompileImmediately(0.4d);
            FoldCanvasCompileResult result = controller.LastCompileResult;

            Assert.That(result.Success, Is.True, JoinDiagnostics(result));
            Assert.That(result.Mesh, Is.Not.Null);
            Assert.That(result.ClosedVolumeReport, Is.Not.Null);
            Assert.That(result.ClosedVolumeReport.IsSingleClosedVolume, Is.True);
            Assert.That(result.ClosedVolumeReport.OpenEdgeCount, Is.Zero);
            Assert.That(result.ClosedVolumeReport.NonManifoldEdgeCount, Is.Zero);
        }

        [Test]
        public void WorkspaceCup_HasClosedVolumeEvidence()
        {
            BlankAsset_ToCupSource_CompilesWithoutCodeEdits();

            FoldCanvasClosedVolumeReport report =
                controller.LastCompileResult.ClosedVolumeReport;
            Assert.That(report.ComponentCount, Is.EqualTo(1));
            Assert.That(report.OrientationConflictEdgeCount, Is.Zero);
            Assert.That(report.TotalAbsoluteVolume, Is.GreaterThan(0d));
        }

        [Test]
        public void BakeValidCup_CreatesDerivedMeshWithoutChangingSource()
        {
            BlankAsset_ToCupSource_CompilesWithoutCodeEdits();
            asset.name = "M06ValidCup";
            int panelCount = asset.Panels.Count;
            int operationCount = asset.Operations.Count;

            bool baked = FoldCanvas.Editor.FoldCanvasBaker.BakeMesh(
                asset,
                BakeTestFolder,
                out Mesh savedMesh,
                out FoldCanvasCompileResult bakeResult);

            Assert.That(baked, Is.True, JoinDiagnostics(bakeResult));
            Assert.That(savedMesh, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(savedMesh),
                Is.EqualTo(
                    BakeTestFolder + "/M06ValidCup_Generated.asset"));
            Assert.That(asset.Panels.Count, Is.EqualTo(panelCount));
            Assert.That(asset.Operations.Count, Is.EqualTo(operationCount));
            Assert.That(asset.hideFlags, Is.EqualTo(HideFlags.None));
        }

        [Test]
        public void BakeInvalidCup_DoesNotOverwriteExistingBake()
        {
            BlankAsset_ToCupSource_CompilesWithoutCodeEdits();
            asset.name = "M06ProtectedCup";
            Assert.That(
                FoldCanvas.Editor.FoldCanvasBaker.BakeMesh(
                    asset,
                    BakeTestFolder,
                    out Mesh original,
                    out FoldCanvasCompileResult validResult),
                Is.True,
                JoinDiagnostics(validResult));
            string path = AssetDatabase.GetAssetPath(original);
            string guid = AssetDatabase.AssetPathToGUID(path);
            int vertexCount = original.vertexCount;
            int indexCount = original.triangles.Length;

            controller.MutatePanel(
                "wall",
                "Invalidate Cup",
                changed => changed.PhysicalSize = Vector2.zero,
                1d);
            bool baked = FoldCanvas.Editor.FoldCanvasBaker.BakeMesh(
                asset,
                BakeTestFolder,
                out Mesh rejected,
                out FoldCanvasCompileResult invalidResult);
            Mesh preserved = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            Assert.That(baked, Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(preserved, Is.Not.Null);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            Assert.That(preserved.vertexCount, Is.EqualTo(vertexCount));
            Assert.That(preserved.triangles.Length, Is.EqualTo(indexCount));
        }

        private static void DestroyWorkspaceWindow(
            FoldCanvas.Editor.FoldCanvasWindow window)
        {
            if (window == null)
            {
                return;
            }

            if (Application.isBatchMode)
            {
                Object.DestroyImmediate(window);
            }
            else
            {
                window.Close();
            }
        }

        private static string JoinDiagnosticCodes(
            FoldCanvasCompileResult result)
        {
            string codes = string.Empty;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (i > 0)
                {
                    codes += ",";
                }

                codes += result.Diagnostics[i].Code;
            }

            return codes;
        }

        private static string JoinDiagnostics(FoldCanvasCompileResult result)
        {
            if (result == null)
            {
                return "No compile result.";
            }

            string message = string.Empty;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                if (i > 0)
                {
                    message += "\n";
                }

                message += result.Diagnostics[i].ToString();
            }

            return message;
        }
    }
}
