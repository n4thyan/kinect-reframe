using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;

namespace KinectReframe.Services
{
    /// <summary>
    /// Encodes frozen WPF bitmap frames on a worker thread and writes them to MJPEG AVI.
    /// The bounded queue protects the Kinect/UI thread; excess frames are counted as dropped.
    /// </summary>
    public sealed class VideoRecorder : IDisposable
    {
        private readonly object syncRoot = new object();
        private BlockingCollection<BitmapSource> frames;
        private Thread worker;
        private MjpegAviWriter writer;
        private Exception workerException;
        private int framesWritten;
        private int droppedFrames;
        private bool disposed;

        public bool IsRecording { get; private set; }
        public string OutputPath { get; private set; }
        public int FramesWritten { get { return Volatile.Read(ref framesWritten); } }
        public int DroppedFrames { get { return Volatile.Read(ref droppedFrames); } }

        public void Start(string path, int width, int height, int framesPerSecond, int jpegQuality)
        {
            lock (syncRoot)
            {
                ThrowIfDisposed();

                if (IsRecording)
                {
                    throw new InvalidOperationException("A video recording is already active.");
                }

                if (jpegQuality < 1 || jpegQuality > 100)
                {
                    throw new ArgumentOutOfRangeException("jpegQuality");
                }

                OutputPath = path;
                FramesPerSecond = framesPerSecond;
                JpegQuality = jpegQuality;
                framesWritten = 0;
                droppedFrames = 0;
                workerException = null;
                frames = new BlockingCollection<BitmapSource>(4);
                writer = new MjpegAviWriter(path, width, height, framesPerSecond);
                IsRecording = true;

                worker = new Thread(EncodeLoop);
                worker.IsBackground = true;
                worker.Name = "Kinect Reframe video encoder";
                worker.SetApartmentState(ApartmentState.STA);
                worker.Start();
            }
        }

        public int FramesPerSecond { get; private set; }
        public int JpegQuality { get; private set; }

        public bool TryAddFrame(BitmapSource frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame");
            }

            if (!IsRecording || frames == null || frames.IsAddingCompleted)
            {
                return false;
            }

            if (frame.CanFreeze && !frame.IsFrozen)
            {
                frame.Freeze();
            }

            if (!frames.TryAdd(frame))
            {
                Interlocked.Increment(ref droppedFrames);
                return false;
            }

            return true;
        }

        public VideoRecordingResult Stop()
        {
            BlockingCollection<BitmapSource> localFrames;
            Thread localWorker;

            lock (syncRoot)
            {
                if (!IsRecording)
                {
                    return new VideoRecordingResult(OutputPath, FramesWritten, DroppedFrames, workerException);
                }

                IsRecording = false;
                localFrames = frames;
                localWorker = worker;
                localFrames.CompleteAdding();
            }

            if (localWorker != null && localWorker.IsAlive)
            {
                localWorker.Join();
            }

            Exception error = workerException;
            VideoRecordingResult result = new VideoRecordingResult(OutputPath, FramesWritten, DroppedFrames, error);

            lock (syncRoot)
            {
                if (frames != null)
                {
                    frames.Dispose();
                }

                frames = null;
                worker = null;
                writer = null;
            }

            return result;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (IsRecording)
            {
                Stop();
            }

            disposed = true;
        }

        private void EncodeLoop()
        {
            try
            {
                foreach (BitmapSource frame in frames.GetConsumingEnumerable())
                {
                    byte[] jpeg = EncodeJpeg(frame, JpegQuality);
                    writer.AddJpegFrame(jpeg);
                    Interlocked.Increment(ref framesWritten);
                }
            }
            catch (Exception exception)
            {
                workerException = exception;
            }
            finally
            {
                try
                {
                    if (writer != null)
                    {
                        writer.Dispose();
                    }
                }
                catch (Exception exception)
                {
                    if (workerException == null)
                    {
                        workerException = exception;
                    }
                }
            }
        }

        private static byte[] EncodeJpeg(BitmapSource frame, int quality)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = quality;
            encoder.Frames.Add(BitmapFrame.Create(frame));

            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("VideoRecorder");
            }
        }
    }

    public sealed class VideoRecordingResult
    {
        public VideoRecordingResult(string path, int framesWritten, int droppedFrames, Exception error)
        {
            Path = path;
            FramesWritten = framesWritten;
            DroppedFrames = droppedFrames;
            Error = error;
        }

        public string Path { get; private set; }
        public int FramesWritten { get; private set; }
        public int DroppedFrames { get; private set; }
        public Exception Error { get; private set; }
        public bool Succeeded { get { return Error == null && FramesWritten > 0; } }
    }
}
