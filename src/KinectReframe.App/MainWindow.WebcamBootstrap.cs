using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KinectReframe
{
    public partial class MainWindow
    {
        private bool layoutPolishApplied;
        private bool webcamBootstrapQueued;

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // OnActivated can occur before Window.Loaded. Rebuilding the inspector at that
            // point marks it ready before ScrollViewer/Expander visuals exist, leaving the
            // original prototype controls on screen. Queue one pass at ContextIdle so the
            // XAML Loaded handler, sensor startup and complete visual tree all exist first.
            if (!webcamBootstrapQueued && !webcamFeaturesReady)
            {
                webcamBootstrapQueued = true;
                Dispatcher.BeginInvoke(new Action(BootstrapWebcamWorkspace), DispatcherPriority.ContextIdle);
            }

            ApplyStudioLayoutPolish();
        }

        private void BootstrapWebcamWorkspace()
        {
            webcamBootstrapQueued = false;

            if (!IsLoaded)
            {
                return;
            }

            // An earlier Loaded/Activated callback may have run before the inspector's
            // visuals were generated. Force the definitive post-load rebuild once here.
            webcamFeaturesReady = false;
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
