using System;

namespace KinectReframe
{
    public partial class MainWindow
    {
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // The webcam/camera inspector is assembled after the complete WPF tree exists.
            // Calling it here avoids relying solely on a Loaded subscription created from
            // another partial class, which did not run consistently on the Kinect PC.
            WebcamWindow_Loaded(this, null);
        }
    }
}
