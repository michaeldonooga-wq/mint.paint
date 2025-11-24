using System;
using System.Windows;

namespace mint.paint
{
    public class ZoomManager
    {
        private double _zoomLevel = 1.0;
        private Point _zoomCenter = new Point(0.5, 0.5);

        public double ZoomLevel
        {
            get => _zoomLevel;
            set => _zoomLevel = Math.Max(0.1, Math.Min(10.0, value));
        }

        public Point ZoomCenter => _zoomCenter;

        public void ZoomIn(Point center)
        {
            _zoomCenter = center;
            ZoomLevel *= 1.2;
        }

        public void ZoomOut(Point center)
        {
            _zoomCenter = center;
            ZoomLevel /= 1.2;
        }

        public void Reset()
        {
            ZoomLevel = 1.0;
            _zoomCenter = new Point(0.5, 0.5);
        }

        public void FitToScreen(double imageWidth, double imageHeight, double viewportWidth, double viewportHeight)
        {
            var scaleX = viewportWidth / imageWidth;
            var scaleY = viewportHeight / imageHeight;
            ZoomLevel = Math.Min(scaleX, scaleY) * 0.95; // 95% чтобы были отступы
        }
    }
}