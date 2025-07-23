using Lively.Common;
using Lively.Common.Helpers;
using Lively.Core.Display;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace Lively.Views
{
    public partial class DiagnosticMenu : Window
    {
        private readonly IDisplayManager displayManager;
        private readonly List<WindowCoverageDebugOverlay> gridOverlays = [];
        private bool isGridOverlayVisible;
        private DebugLog debugLogWindow;

        public DiagnosticMenu()
        {
            InitializeComponent();
            this.displayManager = App.Services.GetRequiredService<IDisplayManager>();
        }

        private void Generate_Report_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                DefaultExt = ".zip",
                Filter = "Compressed archive (.zip)|*.zip",
                FileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    LogUtil.ExtractLogFiles(saveDlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate log report:\n{ex.Message}", "Lively Wallpaper");
                }
            }
        }

        private void Open_Debug_View_Click(object sender, RoutedEventArgs e)
        {
            if (debugLogWindow != null)
                return;

            debugLogWindow = new DebugLog();
            debugLogWindow.Closed += (s, e) => debugLogWindow = null;
            debugLogWindow.Show();
        }

        private void Get_Help_Click(object sender, RoutedEventArgs e)
        {
            LinkUtil.OpenBrowser("https://github.com/rocksdanister/lively/wiki/Common-Problems");
        }

        private void Grid_Overlay_Click(object sender, RoutedEventArgs e)
        {
            if (!isGridOverlayVisible)
            {
                GridOverlyButton.Content = "Grid Overlay [ON]";
                ShowGridOverlay();
            }
            else
            {
                GridOverlyButton.Content = "Grid Overlay [OFF]";
                CloseGridOverlay();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseGridOverlay();
        }

        private void ShowGridOverlay()
        {
            if (isGridOverlayVisible) 
                return;

            isGridOverlayVisible = true;
            foreach (var display in displayManager.DisplayMonitors)
            {
                var gridOverlay = new WindowCoverageDebugOverlay(display);
                gridOverlay.Show();
                gridOverlays.Add(gridOverlay);
            }
        }

        private void CloseGridOverlay()
        {
            if (!isGridOverlayVisible) 
                return;

            isGridOverlayVisible = false;
            foreach (var gridOverlay in gridOverlays)
                gridOverlay.Close();

            gridOverlays.Clear();
        }
    }
}
