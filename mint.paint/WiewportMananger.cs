using SkiaSharp.Views.WPF;
using System;
using System.Windows;

namespace mint.paint
{
    public class ViewportManager
    {
        private SKElement drawingSurface;
        private double zoomLevel = 1.0;
        private Point canvasOffset = new Point(0, 0);

        public double ZoomLevel
        {
            get => zoomLevel;
            set
            {
                zoomLevel = Math.Max(0.1, Math.Min(15.5, value));
                OnViewportChanged?.Invoke();
            }
        }

        public Point CanvasOffset
        {
            get => canvasOffset;
            set
            {
                canvasOffset = value;
                OnViewportChanged?.Invoke();
            }
        }

        public event Action OnViewportChanged;

        public ViewportManager(SKElement drawingSurface)
        {
            this.drawingSurface = drawingSurface;
        }

        public void ZoomIn(Point center)
        {
            double oldZoom = zoomLevel;
            zoomLevel = Math.Max(0.1, Math.Min(15.5, zoomLevel * 1.2));
            
            // Корректируем смещение чтобы точка под курсором осталась на месте
            canvasOffset = new Point(
                center.X - (center.X - canvasOffset.X) * (zoomLevel / oldZoom),
                center.Y - (center.Y - canvasOffset.Y) * (zoomLevel / oldZoom)
            );
            
            OnViewportChanged?.Invoke();
        }

        public void ZoomOut(Point center)
        {
            double oldZoom = zoomLevel;
            zoomLevel = Math.Max(0.1, Math.Min(1840.0, zoomLevel / 1.2));
            
            canvasOffset = new Point(
                center.X - (center.X - canvasOffset.X) * (zoomLevel / oldZoom),
                center.Y - (center.Y - canvasOffset.Y) * (zoomLevel / oldZoom)
            );
            
            OnViewportChanged?.Invoke();
        }

        public void Reset()
        {
            ZoomLevel = 1.0;
            CanvasOffset = new Point(0, 0);
        }

        public void FitToScreen(double imageWidth, double imageHeight)
        {
            var scaleX = drawingSurface.ActualWidth / imageWidth;
            var scaleY = drawingSurface.ActualHeight / imageHeight;
            ZoomLevel = Math.Min(scaleX, scaleY) * 0.95;
            CanvasOffset = new Point(0, 0);
        }

        public void Pan(Point delta)
        {
            CanvasOffset = new Point(CanvasOffset.X + delta.X, CanvasOffset.Y + delta.Y);
        }

        public Point ScreenToCanvas(Point screenPoint)
        {
            return new Point(
                (screenPoint.X - CanvasOffset.X) / ZoomLevel,
                (screenPoint.Y - CanvasOffset.Y) / ZoomLevel
            );
        }

        public Point CanvasToScreen(Point canvasPoint)
        {
            return new Point(
                canvasPoint.X * ZoomLevel + CanvasOffset.X,
                canvasPoint.Y * ZoomLevel + CanvasOffset.Y
            );
        }

        public string GetZoomInfo() => $"Zoom: {ZoomLevel:0%}";

        public void SetOffset(Point offset)
        {
            CanvasOffset = offset;
        }
    }
}