using System;
using Microsoft.Kinect;

namespace KinectReframe.Rendering
{
    public sealed class BodyDepthRenderer
    {
        private readonly int width;
        private readonly int height;
        private readonly byte[] pixels;

        public BodyDepthRenderer(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }

            this.width = width;
            this.height = height;
            pixels = new byte[width * height * 4];
        }

        public int BodyPixelCount { get; private set; }

        public byte[] Render(DepthImagePixel[] depthPixels, bool bodyOnly)
        {
            if (depthPixels == null)
            {
                throw new ArgumentNullException("depthPixels");
            }

            if (depthPixels.Length != width * height)
            {
                throw new ArgumentException("Depth frame size does not match the renderer.", "depthPixels");
            }

            BodyPixelCount = 0;

            for (int index = 0; index < depthPixels.Length; index++)
            {
                DepthImagePixel depthPixel = depthPixels[index];
                int outputIndex = index * 4;
                int y = index / width;
                bool belongsToBody = depthPixel.PlayerIndex > 0;

                if (belongsToBody)
                {
                    BodyPixelCount++;
                }

                if (depthPixel.Depth <= 0 || (bodyOnly && !belongsToBody))
                {
                    WritePixel(outputIndex, 3, 8, 10, 255);
                    continue;
                }

                double normalizedDepth = 1.0 - Clamp((depthPixel.Depth - 800.0) / 3200.0, 0.0, 1.0);
                double scanline = y % 4 == 0 ? 0.66 : 1.0;

                if (belongsToBody)
                {
                    byte blue = (byte)(255 * scanline);
                    byte green = (byte)((95 + (160 * normalizedDepth)) * scanline);
                    byte red = (byte)((18 + (48 * normalizedDepth)) * scanline);
                    WritePixel(outputIndex, blue, green, red, 255);
                }
                else
                {
                    byte shade = (byte)((18 + (48 * normalizedDepth)) * scanline);
                    WritePixel(outputIndex, shade, shade, shade, 255);
                }
            }

            return pixels;
        }

        private void WritePixel(int index, byte blue, byte green, byte red, byte alpha)
        {
            pixels[index] = blue;
            pixels[index + 1] = green;
            pixels[index + 2] = red;
            pixels[index + 3] = alpha;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
