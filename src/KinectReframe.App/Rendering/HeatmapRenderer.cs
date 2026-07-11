using System;
using Microsoft.Kinect;

namespace KinectReframe.Rendering
{
    public sealed class HeatmapRenderer
    {
        private readonly int width;
        private readonly int height;
        private readonly int[] previousDepth;
        private readonly float[] motionEnergy;
        private readonly byte[] motionPixels;
        private readonly byte[] depthPixels;

        public HeatmapRenderer(int width, int height)
        {
            this.width = width;
            this.height = height;
            previousDepth = new int[width * height];
            motionEnergy = new float[width * height];
            motionPixels = new byte[width * height * 4];
            depthPixels = new byte[width * height * 4];
        }

        public int ActiveMotionPixels { get; private set; }

        public byte[] RenderMotion(
            DepthImagePixel[] pixels,
            bool bodyOnly,
            int thresholdMillimetres,
            double persistence)
        {
            if (pixels == null || pixels.Length != width * height)
            {
                throw new ArgumentException("Unexpected depth frame size.", "pixels");
            }

            float fade = (float)Math.Max(0.72, Math.Min(0.995, persistence));
            int threshold = Math.Max(5, thresholdMillimetres);
            ActiveMotionPixels = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                int depth = pixels[i].Depth;
                int oldDepth = previousDepth[i];
                bool valid = depth > 0;
                bool included = !bodyOnly || pixels[i].PlayerIndex > 0;

                motionEnergy[i] *= fade;

                if (valid && oldDepth > 0 && included)
                {
                    int difference = Math.Abs(depth - oldDepth);

                    // Very large jumps are usually invalid-depth edges rather than useful motion.
                    if (difference >= threshold && difference <= 1000)
                    {
                        float addition = Math.Min(1.0f, difference / 260.0f);
                        motionEnergy[i] = Math.Min(1.0f, motionEnergy[i] + (addition * 0.55f));
                    }
                }

                previousDepth[i] = depth;

                if (!included)
                {
                    motionEnergy[i] *= 0.88f;
                }

                if (motionEnergy[i] > 0.08f)
                {
                    ActiveMotionPixels++;
                }

                WriteHeatColour(motionPixels, i * 4, motionEnergy[i], true);
            }

            return motionPixels;
        }

        public byte[] RenderDepth(DepthImagePixel[] pixels, bool bodyOnly)
        {
            if (pixels == null || pixels.Length != width * height)
            {
                throw new ArgumentException("Unexpected depth frame size.", "pixels");
            }

            const double nearest = 450.0;
            const double farthest = 4500.0;

            for (int i = 0; i < pixels.Length; i++)
            {
                int offset = i * 4;
                int depth = pixels[i].Depth;
                bool included = !bodyOnly || pixels[i].PlayerIndex > 0;

                if (depth <= 0 || !included)
                {
                    depthPixels[offset] = 0;
                    depthPixels[offset + 1] = 0;
                    depthPixels[offset + 2] = 0;
                    depthPixels[offset + 3] = 0;
                    continue;
                }

                double normalized = 1.0 - ((depth - nearest) / (farthest - nearest));
                float value = (float)Math.Max(0.0, Math.Min(1.0, normalized));
                WriteHeatColour(depthPixels, offset, value, false);
            }

            return depthPixels;
        }

        public void ClearMotion()
        {
            Array.Clear(previousDepth, 0, previousDepth.Length);
            Array.Clear(motionEnergy, 0, motionEnergy.Length);
            Array.Clear(motionPixels, 0, motionPixels.Length);
            ActiveMotionPixels = 0;
        }

        private static void WriteHeatColour(byte[] target, int offset, float value, bool transparentWhenCold)
        {
            value = Math.Max(0.0f, Math.Min(1.0f, value));

            byte red;
            byte green;
            byte blue;

            if (value < 0.25f)
            {
                float local = value / 0.25f;
                red = 0;
                green = (byte)(local * 170);
                blue = (byte)(95 + (local * 160));
            }
            else if (value < 0.5f)
            {
                float local = (value - 0.25f) / 0.25f;
                red = (byte)(local * 255);
                green = (byte)(170 + (local * 85));
                blue = (byte)(255 - (local * 220));
            }
            else if (value < 0.75f)
            {
                float local = (value - 0.5f) / 0.25f;
                red = 255;
                green = (byte)(255 - (local * 205));
                blue = (byte)(35 - (local * 35));
            }
            else
            {
                float local = (value - 0.75f) / 0.25f;
                red = 255;
                green = (byte)(50 + (local * 205));
                blue = (byte)(local * 255);
            }

            target[offset] = blue;
            target[offset + 1] = green;
            target[offset + 2] = red;
            target[offset + 3] = transparentWhenCold
                ? (byte)(value < 0.02f ? 0 : Math.Min(220, 40 + (value * 215)))
                : (byte)190;
        }
    }
}
