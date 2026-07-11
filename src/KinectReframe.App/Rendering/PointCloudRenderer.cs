using System;
using Microsoft.Kinect;

namespace KinectReframe.Rendering
{
    /// <summary>
    /// Projects Kinect camera-space depth points into a rotatable software-rendered point cloud.
    /// This keeps the renderer dependency-free and suitable for the Kinect SDK 1.8 x86 stack.
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
            double zoom,
            int detailLevel,
            int pointSize,
            bool useSurfaceShading)
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
            double focalLength = 285.0 * Clamp(zoom, 0.35, 3.0);

            int clampedDetail = Math.Max(1, Math.Min(4, detailLevel));
            int sampleStep = 5 - clampedDetail;
            if (!bodyOnly)
            {
                sampleStep = Math.Max(2, sampleStep);
            }

            int pointRadius = Math.Max(0, Math.Min(3, pointSize - 1));

            // Rotate around a useful room-scale pivot rather than around the Kinect lens.
            const double pivotDepth = 2.0;

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
                    if (!IsValidPoint(point))
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

                    double surfaceLight = useSurfaceShading
                        ? EstimateSurfaceLight(cameraPoints, sourceX, sourceY, sourceIndex)
                        : 0.96;

                    byte red;
                    byte green;
                    byte blue;
                    SelectColour(depthPixel, rotatedZ, surfaceLight, out red, out green, out blue);
                    DrawPoint(outputX, outputY, (float)rotatedZ, red, green, blue, pointRadius);
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

        private double EstimateSurfaceLight(
            SkeletonPoint[] cameraPoints,
            int sourceX,
            int sourceY,
            int sourceIndex)
        {
            if (sourceX <= 0 || sourceX >= sourceWidth - 1 || sourceY <= 0 || sourceY >= sourceHeight - 1)
            {
                return 0.78;
            }

            SkeletonPoint centre = cameraPoints[sourceIndex];
            SkeletonPoint right = cameraPoints[sourceIndex + 1];
            SkeletonPoint down = cameraPoints[sourceIndex + sourceWidth];

            if (!IsValidPoint(right) || !IsValidPoint(down))
            {
                return 0.78;
            }

            // Do not derive a normal across a depth discontinuity such as a body edge.
            if (Math.Abs(right.Z - centre.Z) > 0.09f || Math.Abs(down.Z - centre.Z) > 0.09f)
            {
                return 0.62;
            }

            double ax = right.X - centre.X;
            double ay = right.Y - centre.Y;
            double az = right.Z - centre.Z;
            double bx = down.X - centre.X;
            double by = down.Y - centre.Y;
            double bz = down.Z - centre.Z;

            double nx = (ay * bz) - (az * by);
            double ny = (az * bx) - (ax * bz);
            double nz = (ax * by) - (ay * bx);
            double length = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));

            if (length < 0.000001)
            {
                return 0.78;
            }

            nx /= length;
            ny /= length;
            nz /= length;

            // A soft camera-left/top light reveals facial and torso depth without flicker.
            const double lightX = -0.30;
            const double lightY = 0.35;
            const double lightZ = -0.89;
            double diffuse = Math.Abs((nx * lightX) + (ny * lightY) + (nz * lightZ));
            return Clamp(0.46 + (diffuse * 0.54), 0.46, 1.0);
        }

        private void SelectColour(
            DepthImagePixel depthPixel,
            double depthMetres,
            double surfaceLight,
            out byte red,
            out byte green,
            out byte blue)
        {
            double depthLight = Clamp(1.22 - (depthMetres * 0.13), 0.58, 1.0);
            double brightness = Clamp(surfaceLight * depthLight, 0.42, 1.0);

            if (depthPixel.PlayerIndex > 0)
            {
                // Surface shading keeps the cyan hologram identity while revealing 3D form.
                red = (byte)(54 * brightness);
                green = (byte)(235 * brightness);
                blue = (byte)(255 * brightness);
                return;
            }

            double normalized = Clamp((depthMetres - 0.5) / 3.8, 0.0, 1.0);
            red = (byte)((38 + (160 * normalized)) * brightness);
            green = (byte)((210 - (110 * normalized)) * brightness);
            blue = (byte)((255 - (70 * normalized)) * brightness);
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

        private static bool IsValidPoint(SkeletonPoint point)
        {
            return point.Z > 0.15f &&
                   !float.IsNaN(point.X) &&
                   !float.IsNaN(point.Y) &&
                   !float.IsNaN(point.Z) &&
                   !float.IsInfinity(point.X) &&
                   !float.IsInfinity(point.Y) &&
                   !float.IsInfinity(point.Z);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
