using System;
using System.IO;
using System.Xml.Serialization;

namespace KinectReframe.Services
{
    [Serializable]
    public sealed class AppSettings
    {
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public bool WindowMaximized { get; set; }
        public int SceneIndex { get; set; }
        public string PreviewMode { get; set; }
        public bool Mirror { get; set; }
        public bool SeatedTracking { get; set; }
        public bool BodyOnly { get; set; }
        public double Brightness { get; set; }
        public double Contrast { get; set; }
        public double Smoothing { get; set; }
        public double MotionThreshold { get; set; }
        public double MotionPersistence { get; set; }
        public double PointCloudDetail { get; set; }
        public double PointCloudPointSize { get; set; }
        public bool PointCloudShading { get; set; }
        public double OutputScale { get; set; }
        public double VideoFps { get; set; }
        public double VideoQuality { get; set; }

        public static AppSettings CreateDefaults()
        {
            return new AppSettings
            {
                WindowWidth = 1540,
                WindowHeight = 940,
                SceneIndex = 0,
                PreviewMode = "Camera",
                Mirror = true,
                SeatedTracking = true,
                BodyOnly = true,
                Contrast = 1.0,
                Smoothing = 0.35,
                MotionThreshold = 35,
                MotionPersistence = 0.94,
                PointCloudDetail = 4,
                PointCloudPointSize = 2,
                PointCloudShading = true,
                OutputScale = 1.0,
                VideoFps = 15,
                VideoQuality = 82
            };
        }
    }

    public static class AppSettingsStore
    {
        public static string SettingsDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KinectReframe");
            }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(SettingsDirectory, "settings.xml"); }
        }

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return AppSettings.CreateDefaults();
                }

                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (FileStream stream = File.OpenRead(SettingsPath))
                {
                    AppSettings settings = serializer.Deserialize(stream) as AppSettings;
                    return settings ?? AppSettings.CreateDefaults();
                }
            }
            catch
            {
                return AppSettings.CreateDefaults();
            }
        }

        public static void Save(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            Directory.CreateDirectory(SettingsDirectory);
            string temporaryPath = SettingsPath + ".tmp";
            XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));

            using (FileStream stream = File.Create(temporaryPath))
            {
                serializer.Serialize(stream, settings);
            }

            if (File.Exists(SettingsPath))
            {
                File.Delete(SettingsPath);
            }

            File.Move(temporaryPath, SettingsPath);
        }
    }
}
