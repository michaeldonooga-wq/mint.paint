using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace mint.paint
{
    public partial class NavigatorWindow : Window
    {
        private DrawingManager drawingManager;
        private ViewportManager viewportManager;
        private System.Windows.Threading.DispatcherTimer updateTimer;
        private bool isDragging = false;

        public NavigatorWindow(DrawingManager drawingManager, ViewportManager viewportManager)
        {
            InitializeComponent();
            this.drawingManager = drawingManager;
            this.viewportManager = viewportManager;

            updateTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            updateTimer.Tick += (s, e) => UpdatePreview();
            updateTimer.Start();

            ViewportCanvas.MouseDown += ViewportCanvas_MouseDown;
            ViewportCanvas.MouseMove += ViewportCanvas_MouseMove;
            ViewportCanvas.MouseUp += ViewportCanvas_MouseUp;

            viewportManager.OnViewportChanged += UpdateViewportRect;
            
            Title = Localization.Strings.Get("NavigatorTitle");
        }

        private void UpdatePreview()
        {
            var layer = drawingManager?.GetLayerManager()?.ActiveLayer;
            if (layer?.Bitmap == null) return;

            using var image = SKImage.FromBitmap(layer.Bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 80);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new System.IO.MemoryStream(data.ToArray());
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            PreviewImage.Source = bitmap;
            UpdateViewportRect();
        }

        private void UpdateViewportRect()
        {
        }

        private void ViewportCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            ViewportCanvas.CaptureMouse();
            MoveViewport(e.GetPosition(ViewportCanvas));
        }

        private void ViewportCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
                MoveViewport(e.GetPosition(ViewportCanvas));
        }

        private void ViewportCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ViewportCanvas.ReleaseMouseCapture();
        }

        private void MoveViewport(Point pos)
        {
            var layer = drawingManager?.GetLayerManager()?.ActiveLayer;
            if (layer?.Bitmap == null) return;

            var scale = Math.Min(PreviewImage.ActualWidth / layer.Bitmap.Width, PreviewImage.ActualHeight / layer.Bitmap.Height);
            var x = -(pos.X / scale);
            var y = -(pos.Y / scale);

            viewportManager.SetOffset(new Point(x, y));
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
