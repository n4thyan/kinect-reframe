using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KinectReframe.Services
{
    /// <summary>
    /// Minimal single-stream MJPEG AVI writer. Frames must already be JPEG encoded.
    /// The implementation intentionally avoids external codecs so recordings work on
    /// the same legacy x86/.NET Framework stack as Kinect SDK 1.8.
    /// </summary>
    public sealed class MjpegAviWriter : IDisposable
    {
        private const uint AviHasIndex = 0x10;
        private const uint KeyFrameFlag = 0x10;

        private readonly FileStream stream;
        private readonly BinaryWriter writer;
        private readonly List<IndexEntry> index = new List<IndexEntry>();
        private readonly int framesPerSecond;
        private readonly long riffSizePosition;
        private readonly long totalFramesPosition;
        private readonly long mainSuggestedBufferPosition;
        private readonly long streamLengthPosition;
        private readonly long streamSuggestedBufferPosition;
        private readonly long maxBytesPerSecondPosition;
        private readonly long moviSizePosition;
        private readonly long moviDataStart;

        private bool disposed;
        private int maximumFrameSize;

        public MjpegAviWriter(string path, int width, int height, int framesPerSecond)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A video output path is required.", "path");
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("Video dimensions must be positive.");
            }

            if (framesPerSecond <= 0 || framesPerSecond > 60)
            {
                throw new ArgumentOutOfRangeException("framesPerSecond");
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Width = width;
            Height = height;
            this.framesPerSecond = framesPerSecond;

            stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            writer = new BinaryWriter(stream, Encoding.ASCII);

            WriteFourCc("RIFF");
            riffSizePosition = stream.Position;
            writer.Write(0);
            WriteFourCc("AVI ");

            WriteFourCc("LIST");
            long headerListSizePosition = stream.Position;
            writer.Write(0);
            long headerListStart = stream.Position;
            WriteFourCc("hdrl");

            WriteFourCc("avih");
            writer.Write(56);
            writer.Write(1000000 / framesPerSecond);
            maxBytesPerSecondPosition = stream.Position;
            writer.Write(0);
            writer.Write(0);
            writer.Write(AviHasIndex);
            totalFramesPosition = stream.Position;
            writer.Write(0);
            writer.Write(0);
            writer.Write(1);
            mainSuggestedBufferPosition = stream.Position;
            writer.Write(0);
            writer.Write(width);
            writer.Write(height);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            WriteFourCc("LIST");
            long streamListSizePosition = stream.Position;
            writer.Write(0);
            long streamListStart = stream.Position;
            WriteFourCc("strl");

            WriteFourCc("strh");
            writer.Write(56);
            WriteFourCc("vids");
            WriteFourCc("MJPG");
            writer.Write(0);
            writer.Write((short)0);
            writer.Write((short)0);
            writer.Write(0);
            writer.Write(1);
            writer.Write(framesPerSecond);
            writer.Write(0);
            streamLengthPosition = stream.Position;
            writer.Write(0);
            streamSuggestedBufferPosition = stream.Position;
            writer.Write(0);
            writer.Write(-1);
            writer.Write(0);
            writer.Write((short)0);
            writer.Write((short)0);
            writer.Write((short)Math.Min(short.MaxValue, width));
            writer.Write((short)Math.Min(short.MaxValue, height));

            WriteFourCc("strf");
            writer.Write(40);
            writer.Write(40);
            writer.Write(width);
            writer.Write(height);
            writer.Write((short)1);
            writer.Write((short)24);
            WriteFourCc("MJPG");
            writer.Write(width * height * 3);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            PatchInt32(streamListSizePosition, checked((int)(stream.Position - streamListStart)));
            PatchInt32(headerListSizePosition, checked((int)(stream.Position - headerListStart)));

            WriteFourCc("LIST");
            moviSizePosition = stream.Position;
            writer.Write(0);
            WriteFourCc("movi");
            moviDataStart = stream.Position;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FrameCount { get { return index.Count; } }

        public void AddJpegFrame(byte[] jpegBytes)
        {
            ThrowIfDisposed();

            if (jpegBytes == null || jpegBytes.Length == 0)
            {
                throw new ArgumentException("A JPEG frame is required.", "jpegBytes");
            }

            long chunkStart = stream.Position;
            WriteFourCc("00dc");
            writer.Write(jpegBytes.Length);
            writer.Write(jpegBytes);

            if ((jpegBytes.Length & 1) != 0)
            {
                writer.Write((byte)0);
            }

            index.Add(new IndexEntry
            {
                Offset = checked((int)(chunkStart - moviDataStart)),
                Size = jpegBytes.Length
            });

            if (jpegBytes.Length > maximumFrameSize)
            {
                maximumFrameSize = jpegBytes.Length;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            long moviEnd = stream.Position;
            PatchInt32(moviSizePosition, checked((int)(moviEnd - (moviSizePosition + 4))));

            WriteFourCc("idx1");
            writer.Write(index.Count * 16);
            foreach (IndexEntry entry in index)
            {
                WriteFourCc("00dc");
                writer.Write(KeyFrameFlag);
                writer.Write(entry.Offset);
                writer.Write(entry.Size);
            }

            long fileEnd = stream.Position;
            PatchInt32(totalFramesPosition, index.Count);
            PatchInt32(streamLengthPosition, index.Count);
            PatchInt32(mainSuggestedBufferPosition, maximumFrameSize);
            PatchInt32(streamSuggestedBufferPosition, maximumFrameSize);
            PatchInt32(maxBytesPerSecondPosition, maximumFrameSize * framesPerSecond);
            PatchInt32(riffSizePosition, checked((int)(fileEnd - 8)));

            writer.Flush();
            writer.Dispose();
            stream.Dispose();
        }

        private void PatchInt32(long position, int value)
        {
            long returnPosition = stream.Position;
            stream.Position = position;
            writer.Write(value);
            stream.Position = returnPosition;
        }

        private void WriteFourCc(string value)
        {
            if (value == null || value.Length != 4)
            {
                throw new ArgumentException("A FOURCC value must contain exactly four characters.", "value");
            }

            writer.Write(Encoding.ASCII.GetBytes(value));
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MjpegAviWriter");
            }
        }

        private sealed class IndexEntry
        {
            public int Offset { get; set; }
            public int Size { get; set; }
        }
    }
}
