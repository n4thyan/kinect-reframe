using System;
using Microsoft.Kinect;

namespace KinectReframe.Rendering
{
    /// <summary>
    /// Projects Kinect camera-space depth points into a rotatable software-rendered point cloud.
    /// This keeps the first 3D prototype dependency-free and suitable for the old x86 SDK stack.
    /// </summary>
    public sealed class PointCloudRenderer
    {
        private readonly int sourceWidth;
        private readonly int sourceHeight;
        private readonly int outputWidth;
        private readonly int outputHeight;
        private readonly byte[] pixels;
        private readonly float[] zBuffer;

        public PointCloudRenderer(int sourceWidth, int sourceHeight, int outputWidth, int outputHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || outputWidth <= 0 || outputHeight <= 0)
            {
                throw new ArgumentOutOfRangeException("Point-cloud dimensions must be positive.");
            }

            this.sourceWidth = sourceWidth;
            this.sourceHeight = sourceHeight;
            this.outputWidth = outputWidth;
            this.outputHeight = outputHeight;
            pixels = new byte[outputWidth * outputHeight * 4];
            zBuffer = new float[outputWidth * outputHeight];
        }

        public int PointCount { get; private set; }

        public byte[] Render(
            DepthImagePixel[] depthPixels,
            SkeletonPoint[] cameraPoints,
            bool bodyOnly,
            double yawDegrees,
            double pitchDegrees,
            double zoom)
        {
            if (depthPixels == null)
            {
                throw new ArgumentNullException("depthPixels");
            }

            if (cameraPoints == null)
            {
                throw new ArgumentNullException("cameraPoints");
            }

            int expectedLength = sourceWidth * sourceHeight;
            if (depthPixels.Length < expectedLength || cameraPoints.Length < expectedLength)
            {
                throw new ArgumentException("Depth and camera-space buffers do not match the configured source size.");
            }

            ClearBuffers();
            PointCount = 0;

            double yaw = yawDegrees * Math.PI / 180.0;
            double pitch = pitchDegrees * Math.PI / 180.0;
            double cosYaw = Math.Cos(yaw);
            double sinYaw = Math.Sin(yaw);
            double cosPitch = Math.Cos(pitch);
            double sinPitch = Math.Sin(pitch);
            double focalLength = 285.0 * Math.Max(0.35, Math.Min(3.0, zoom));

            // Rotate around a useful room-scale pivot rather than around the Kinect lens.
            const double pivotDepth = 2.0;
            const int sampleStep = 2;

            for (int sourceY = 0; sourceY < sourceHeight; sourceY += sampleStep)
            {
                int rowOffset = sourceY * sourceWidth;
                for (int sourceX = 0; sourceX < sourceWidth; sourceX += sampleStep)
                {
                    int sourceIndex = rowOffset + sourceX;
                    DepthImagePixel depthPixel = depthPixels[sourceIndex];

                    if (depthPixel.Depth <= 0 || (bodyOnly && depthPixel.PlayerIndex <= 0))
                    {
                        continue;
                    }

                    SkeletonPoint point = cameraPoints[sourceIndex];
                    if (point.Z <= 0.15f || float.IsNaN(point.X) || float.IsNaN(point.Y) || float.IsNaN(point.Z))
                    {
                        continue;
                    }

                    double centredZ = point.Z - pivotDepth;
                    double rotatedX = (cosYaw * point.X) + (sinYaw * centredZ);
                    double yawZ = (-sinYaw * point.X) + (cosYaw * centredZ);
                    double rotatedY = (cosPitch * point.Y) - (sinPitch * yawZ);
                    double rotatedZ = (sinPitch * point.Y) + (cosPitch * yawZ) + pivotDepth;

                    if (rotatedZ <= 0.15)
                    {
                        continue;
                    }

                    int outputX = (int)Math.Round((outputWidth * 0.5) + ((rotatedX * focalLength) / rotatedZ));
                    int outputY = (int)Math.Round((outputHeight * 0.52) - ((rotatedY * focalLength) / rotatedZ));

                    if (outputX < 0 || outputX >= outputWidth || outputY < 0 || outputY >= outputHeight)
                    {
                        continue;
                    }

                    byte red;
                    byte green;
                    byte blue;
                    SelectColour(depthPixel, rotatedZ, out red, out green, out blue);
                    DrawPoint(outputX, outputY, (float)rotatedZ, red, green, blue, bodyOnly ? 2 : 1);
                    PointCount++;
                }
            }

            return pixels;
        }

        private void ClearBuffers()
        {
            Array.Clear(pixels, 0, pixels.Length);
            for (int index = 0; index < zBuffer.Length; index++)
            {
                zBuffer[index] = float.MaxValue;
            }
        }

        private void SelectColour(
            DepthImagePixel depthPixel,
            double depthMetres,
            out byte red,
            out byte green,
            out byte blue)
        {
            if (depthPixel.PlayerIndex > 0)
            {
                // Cyan-white body points stand out from the full-scene depth palette.
                double brightness = Math.Max(0.45, Math.Min(1.0, 1.25 - (depthMetres * 0.16)));
                red = (byte)(55 * brightness);
                green = (byte)(235 * brightness);
                blue = (byte)(255 * brightness);
                return;
            }

            double normalized = Math.Max(0.0, Math.Min(1.0, (depthMetres - 0.5) / 3.8));
            red = (byte)(38 + (160 * normalized));
            green = (byte)(210 - (110 * normalized));
            blue = (byte)(255 - (70 * normalized));
        }

        private void DrawPoint(int centerX, int centerY, float depth, byte red, byte green, byte blue, int radius)
        {
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                int y = centerY + offsetY;
                if (y < 0 || y >= outputHeight)
                {
                    continue;
                }

                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    int x = centerX + offsetX;
                    if (x < 0 || x >= outputWidth)
                    {
                        continue;
                    }

                    int pixelIndex = (y * outputWidth) + x;
                    if (depth >= zBuffer[pixelIndex])
                    {
                        continue;
                    }

                    zBuffer[pixelIndex] = depth;
                    int byteIndex = pixelIndex * 4;
                    pixels[byteIndex] = blue;
                    pixels[byteIndex + 1] = green;
                    pixels[byteIndex + 2] = red;
                    pixels[byteIndex + 3] = 255;
                }
            }
        }
    }
}
