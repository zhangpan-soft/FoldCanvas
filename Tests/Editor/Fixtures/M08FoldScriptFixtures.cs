using System.IO;
using System.Text;
using UnityEditor.PackageManager;

namespace FoldCanvas.Tests
{
    internal static class M08FoldScriptFixtures
    {
        public static string LoadPackageText(string relativePath)
        {
            PackageInfo package = PackageInfo.FindForAssetPath(
                "Packages/com.foldcanvas.core");
            return File.ReadAllText(Path.Combine(package.resolvedPath, relativePath));
        }

        public static string SimpleRectangle(
            int uSegments = 1,
            int vSegments = 1,
            string operations = "[]",
            string extensions = null,
            string units = "meter",
            string physicalWidth = "1",
            string physicalHeight = "1")
        {
            string extensionProperty = extensions == null
                ? string.Empty
                : ",\"extensions\":" + extensions;
            return "{" +
                "\"schemaVersion\":\"0.1\"," +
                "\"assetId\":\"test-asset\"," +
                "\"displayName\":\"Test Asset\"," +
                "\"units\":\"" + units + "\"," +
                "\"canvas\":{" +
                    "\"appearance\":\"canvas.png\"," +
                    "\"width\":64,\"height\":64}," +
                "\"panels\":[{" +
                    "\"id\":\"panel\",\"shape\":\"rectangle\"," +
                    "\"canvasRect\":[0,0,1,1]," +
                    "\"physicalSize\":[" + physicalWidth + "," +
                        physicalHeight + "]," +
                    "\"tessellation\":{" +
                        "\"uSegments\":" + uSegments + "," +
                        "\"vSegments\":" + vSegments + "}}]," +
                "\"seams\":[]," +
                "\"operations\":" + operations + "," +
                "\"compile\":{" +
                    "\"weldEpsilon\":0.00001," +
                    "\"recalculateNormals\":true," +
                    "\"validationLevel\":\"basic\"," +
                    "\"maxGeneratedVertices\":1000000," +
                    "\"maxGeneratedTriangles\":2000000}" +
                extensionProperty + "}";
        }

        public static string FullRoll(int uSegments)
        {
            string operations = "[{" +
                "\"id\":\"roll\",\"type\":\"roll\"," +
                "\"panel\":\"panel\",\"direction\":\"u\"," +
                "\"angleDegrees\":360," +
                "\"radiusMode\":\"preserveArcLength\"," +
                "\"startAngleDegrees\":0}]";
            return SimpleRectangle(uSegments, 1, operations);
        }

        public static string ExcessivePanels()
        {
            StringBuilder panels = new StringBuilder();
            panels.Append('[');
            for (int i = 0; i <= FoldCanvasLimits.MaximumFoldScriptPanels; i++)
            {
                if (i > 0) panels.Append(',');
                panels.Append("{\"id\":\"p");
                panels.Append(i);
                panels.Append("\",\"shape\":\"rectangle\"," +
                    "\"canvasRect\":[0,0,1,1]," +
                    "\"physicalSize\":[1,1]," +
                    "\"tessellation\":{" +
                    "\"uSegments\":1,\"vSegments\":1}}");
            }

            panels.Append(']');
            return Root(panels.ToString(), "[]");
        }

        public static string ExcessiveOperations()
        {
            StringBuilder operations = new StringBuilder();
            operations.Append('[');
            for (int i = 0; i <= FoldCanvasLimits.MaximumFoldScriptOperations; i++)
            {
                if (i > 0) operations.Append(',');
                operations.Append("{\"id\":\"o");
                operations.Append(i);
                operations.Append("\",\"type\":\"stitch\",\"seams\":[\"s\"]}");
            }

            operations.Append(']');
            return Root(
                "[{\"id\":\"panel\",\"shape\":\"rectangle\"," +
                "\"canvasRect\":[0,0,1,1],\"physicalSize\":[1,1]," +
                "\"tessellation\":{\"uSegments\":1,\"vSegments\":1}}]",
                operations.ToString());
        }

        private static string Root(string panels, string operations)
        {
            return "{" +
                "\"schemaVersion\":\"0.1\"," +
                "\"assetId\":\"test-asset\"," +
                "\"displayName\":\"Test Asset\"," +
                "\"units\":\"meter\"," +
                "\"canvas\":{\"appearance\":\"canvas.png\",\"width\":64,\"height\":64}," +
                "\"panels\":" + panels + "," +
                "\"seams\":[]," +
                "\"operations\":" + operations + "," +
                "\"compile\":{\"weldEpsilon\":0.00001," +
                "\"recalculateNormals\":true,\"validationLevel\":\"basic\"}}";
        }
    }
}
