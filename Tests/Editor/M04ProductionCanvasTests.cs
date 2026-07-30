using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FoldCanvas.Tests
{
    public sealed class M04ProductionCanvasTests
    {
        private static readonly Color32 SeamColor =
            new Color32(222, 239, 247, 255);

        [Test]
        public void ProductionCanvas_HasBleedSafeWeldedBoundaries()
        {
            PackageInfo package =
                PackageInfo.FindForAssembly(
                    typeof(M04ProductionCanvasTests).Assembly);
            Assert.That(package, Is.Not.Null);
            string path = Path.Combine(
                package.resolvedPath,
                "Samples~",
                "BootstrapPanel",
                "M04ProductionCupCanvas.png");
            Assert.That(File.Exists(path), Is.True, path);

            Texture2D texture =
                new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(
                    texture.LoadImage(File.ReadAllBytes(path), false),
                    Is.True);
                Assert.That(texture.width, Is.EqualTo(1024));
                Assert.That(texture.height, Is.EqualTo(1024));

                Color32[] pixels = texture.GetPixels32();
                AssertWallBoundaries(
                    pixels,
                    texture.width,
                    texture.height);
                AssertBottomBleed(
                    pixels,
                    texture.width,
                    texture.height);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void AssertWallBoundaries(
            Color32[] pixels,
            int width,
            int height)
        {
            int xMin = Mathf.RoundToInt(0.06f * width);
            int xMax = Mathf.RoundToInt(0.94f * width);
            int yMin = Mathf.RoundToInt(0.46f * height);
            int yMax = Mathf.RoundToInt(0.90f * height);
            const int bleed = 12;

            for (int x = xMin; x <= xMax; x++)
            {
                Assert.That(
                    Pixel(pixels, width, x, yMin),
                    Is.EqualTo(SeamColor),
                    $"wall.vMin pixel {x}");
                AssertLightPixel(
                    Pixel(pixels, width, x, yMax),
                    $"wall.vMax pixel {x}");
            }

            for (int y = yMin; y <= yMax; y++)
            {
                Assert.That(
                    Pixel(pixels, width, xMin, y),
                    Is.EqualTo(SeamColor),
                    $"wall.uMin pixel {y}");
                Assert.That(
                    Pixel(pixels, width, xMax, y),
                    Is.EqualTo(SeamColor),
                    $"wall.uMax pixel {y}");
            }

            Assert.That(
                Pixel(pixels, width, xMin - bleed, yMin),
                Is.EqualTo(SeamColor));
            Assert.That(
                Pixel(pixels, width, xMax + bleed, yMin),
                Is.EqualTo(SeamColor));
            Assert.That(
                Pixel(pixels, width, xMin, yMin - bleed),
                Is.EqualTo(SeamColor));
        }

        private static void AssertBottomBleed(
            Color32[] pixels,
            int width,
            int height)
        {
            Vector2 center = new Vector2(
                0.5f * width,
                0.2f * height);
            float radius = 0.18f * width;
            for (int sample = 0; sample < 128; sample++)
            {
                float angle =
                    sample * Mathf.PI * 2f / 128f;
                Vector2 direction =
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AssertSample(
                    pixels,
                    width,
                    center + direction * radius,
                    $"bottom perimeter sample {sample}");
                AssertSample(
                    pixels,
                    width,
                    center + direction * (radius + 8f),
                    $"bottom 8px bleed sample {sample}");
            }
        }

        private static void AssertSample(
            Color32[] pixels,
            int width,
            Vector2 position,
            string message)
        {
            Assert.That(
                Pixel(
                    pixels,
                    width,
                    Mathf.RoundToInt(position.x),
                    Mathf.RoundToInt(position.y)),
                Is.EqualTo(SeamColor),
                message);
        }

        private static Color32 Pixel(
            Color32[] pixels,
            int width,
            int x,
            int y)
        {
            return pixels[y * width + x];
        }

        private static void AssertLightPixel(
            Color32 color,
            string message)
        {
            Assert.That(color.r, Is.GreaterThanOrEqualTo(180), message);
            Assert.That(color.g, Is.GreaterThanOrEqualTo(180), message);
            Assert.That(color.b, Is.GreaterThanOrEqualTo(180), message);
            Assert.That(color.a, Is.EqualTo(255), message);
        }
    }
}
