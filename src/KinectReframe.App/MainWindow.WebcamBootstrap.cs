using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private bool layoutPolishApplied;

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // The webcam/camera inspector is assembled after the complete WPF tree exists.
            // Calling it here avoids relying solely on a Loaded subscription created from
            // another partial class, which did not run consistently on the Kinect PC.
            WebcamWindow_Loaded(this, null);
            ApplyStudioLayoutPolish();
        }

        private void ApplyStudioLayoutPolish()
        {
            if (layoutPolishApplied)
            {
                return;
            }

            Grid root = Content as Grid;
            if (root == null || root.RowDefinitions.Count < 5)
            {
                return;
            }

            layoutPolishApplied = true;

            // Reduce dead chrome and return more vertical space to the live preview.
            root.RowDefinitions[0].Height = new GridLength(26);
            root.RowDefinitions[1].Height = new GridLength(46);
            root.RowDefinitions[3].Height = new GridLength(150);
            root.RowDefinitions[4].Height = new GridLength(24);

            Grid workspace = root.Children
                .OfType<Grid>()
                .FirstOrDefault(item => Grid.GetRow(item) == 2 && item.ColumnDefinitions.Count == 5);

            if (workspace != null)
            {
                workspace.Margin = new Thickness(6);
                workspace.ColumnDefinitions[0].Width = new GridLength(216);
                workspace.ColumnDefinitions[1].Width = new GridLength(4);
                workspace.ColumnDefinitions[3].Width = new GridLength(4);
                workspace.ColumnDefinitions[4].Width = new GridLength(350);
            }

            Grid lowerDock = root.Children
                .OfType<Grid>()
                .FirstOrDefault(item => Grid.GetRow(item) == 3 && item.ColumnDefinitions.Count == 5);

            if (lowerDock != null)
            {
                lowerDock.Margin = new Thickness(6, 0, 6, 6);
            }
        }
    }
}
