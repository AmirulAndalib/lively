using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Services;
using Lively.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Media = System.Windows.Media;
using Shapes = System.Windows.Shapes;

namespace Lively.Views
{
    public partial class WindowCoverageDebugOverlay : Window
    {
        private readonly DisplayMonitor targetDisplay;
        private readonly DispatcherTimer dispatcherTimer;
        private readonly int tileSize;
        private float scaleFactor = 1f;

        public WindowCoverageDebugOverlay(DisplayMonitor targetDisplay)
        {
            InitializeComponent();
            var userSettings = App.Services.GetRequiredService<IUserSettingsService>();

            dispatcherTimer = new();
            dispatcherTimer.Tick += (s, e) => UpdateGrid();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, userSettings.Settings.ProcessTimerInterval);

            this.targetDisplay = targetDisplay;
            this.tileSize = userSettings.Settings.ProcessMonitorGridTileSize;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            this.Left = targetDisplay.WorkingArea.Left;
            this.Top = targetDisplay.WorkingArea.Top;
            this.Width = targetDisplay.WorkingArea.Width;
            this.Height = targetDisplay.WorkingArea.Height;

            var dpi = NativeMethods.GetDpiForWindow(hwnd);
            scaleFactor = 1f / (dpi / 96f);

            // Make window click through.
            WindowUtil.SetWindowExStyle(hwnd, NativeMethods.WindowStyles.WS_EX_TRANSPARENT | NativeMethods.WindowStyles.WS_EX_TOOLWINDOW);
            // Start drawing.
            dispatcherTimer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dispatcherTimer.Stop();
        }

        private void UpdateGrid()
        {
            TileCanvas.Children.Clear();

            var screenBounds = targetDisplay.Bounds;
            int cols = (int)(screenBounds.Width * scaleFactor/ (double)tileSize);
            int rows = (int)(screenBounds.Height * scaleFactor/ (double)tileSize);

            bool[,] covered = new bool[rows, cols];

            foreach (var hwnd in WindowUtil.GetVisibleTopLevelWindows())
            {
                if (NativeMethods.GetWindowRect(hwnd, out var rect) == 0)
                    continue;

                if (!RectsIntersect(rect, screenBounds))
                    continue;

                rect = new NativeMethods.RECT
                {
                    Left = (int)(rect.Left * scaleFactor),
                    Right = (int)(rect.Right * scaleFactor),
                    Top = (int)(rect.Top * scaleFactor),
                    Bottom = (int)(rect.Bottom * scaleFactor)
                };

                // Find overlapping tile indices
                int xStart = Math.Max(0, (rect.Left - screenBounds.Left) / tileSize);
                int xEnd = Math.Min(cols - 1, (rect.Right - screenBounds.Left - 1) / tileSize);
                int yStart = Math.Max(0, (rect.Top - screenBounds.Top) / tileSize);
                int yEnd = Math.Min(rows - 1, (rect.Bottom - screenBounds.Top - 1) / tileSize);

                for (int y = yStart; y <= yEnd; y++)
                    for (int x = xStart; x <= xEnd; x++)
                        covered[y, x] = true;
            }

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var tile = new Shapes.Rectangle
                    {
                        Width = tileSize,
                        Height = tileSize,
                        Stroke = new SolidColorBrush(Media.Color.FromArgb(100, 128, 128, 128)),
                        StrokeThickness = 1,
                        Fill = covered[y, x] ? 
                            new SolidColorBrush(Media.Color.FromArgb(50, 255, 0, 0)) : Media.Brushes.Transparent
                    };

                    Canvas.SetLeft(tile, x * tileSize);
                    Canvas.SetTop(tile, y * tileSize);
                    TileCanvas.Children.Add(tile);
                }
            }
        }

        private static bool RectsIntersect(NativeMethods.RECT a, Rectangle b)
        {
            return a.Right > b.Left && a.Left < b.Right &&
                   a.Bottom > b.Top && a.Top < b.Bottom;
        }
    }
}
