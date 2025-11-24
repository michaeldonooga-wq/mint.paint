using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Windows;

namespace mint.paint
{
    public class GridManager
    {
        private ViewportManager viewportManager;

        public bool ShowGrid { get; set; } = false;
        public bool SnapToGrid { get; set; } = false;
        private float gridSize = 1f;
        public float GridSize 
        { 
            get => gridSize;
            set => gridSize = Math.Max(1f, Math.Min(100f, value));
        }
        public SKColor GridColor { get; set; } = new SKColor(0, 0, 0, 30);
        public float GridLineWidth { get; set; } = 0.5f;

        // Направляющие линии
        public List<float> VerticalGuides { get; } = new List<float>();
        public List<float> HorizontalGuides { get; } = new List<float>();
        public SKColor GuideColor { get; set; } = new SKColor(255, 0, 0, 100);

        public GridManager(ViewportManager viewportManager)
        {
            this.viewportManager = viewportManager;
        }

        public void DrawGrid(SKCanvas canvas, SKSize canvasSize, float canvasWidth = 0, float canvasHeight = 0)
        {
            if (!ShowGrid || viewportManager.ZoomLevel < 6.5) return;

            using var gridPaint = new SKPaint
            {
                Color = GridColor,
                StrokeWidth = 1f / (float)viewportManager.ZoomLevel,
                Style = SKPaintStyle.Stroke,
                IsAntialias = false
            };

            float startX = (float)Math.Floor(0 / GridSize) * GridSize;
            float startY = (float)Math.Floor(0 / GridSize) * GridSize;

            // Вертикальные линии
            for (float x = startX; x <= canvasWidth; x += GridSize)
            {
                if (x >= 0)
                    canvas.DrawLine(x, 0, x, canvasHeight, gridPaint);
            }

            // Горизонтальные линии
            for (float y = startY; y <= canvasHeight; y += GridSize)
            {
                if (y >= 0)
                    canvas.DrawLine(0, y, canvasWidth, y, gridPaint);
            }

            // Направляющие линии
            DrawGuides(canvas, canvasSize);
        }

        private void DrawGuides(SKCanvas canvas, SKSize canvasSize)
        {
            if (VerticalGuides.Count == 0 && HorizontalGuides.Count == 0) return;

            using var guidePaint = new SKPaint
            {
                Color = GuideColor,
                StrokeWidth = 1.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            // Вертикальные направляющие
            foreach (var x in VerticalGuides)
            {
                if (x >= 0 && x <= canvasSize.Width)
                {
                    canvas.DrawLine(x, 0, x, canvasSize.Height, guidePaint);
                }
            }

            // Горизонтальные направляющие
            foreach (var y in HorizontalGuides)
            {
                if (y >= 0 && y <= canvasSize.Height)
                {
                    canvas.DrawLine(0, y, canvasSize.Width, y, guidePaint);
                }
            }
        }

        public Point SnapToGridPoint(Point point)
        {
            if (!SnapToGrid) return point;

            double snappedX = Math.Round(point.X / GridSize) * GridSize;
            double snappedY = Math.Round(point.Y / GridSize) * GridSize;

            return new Point(snappedX, snappedY);
        }

        public void AddVerticalGuide(float x)
        {
            if (!VerticalGuides.Contains(x))
                VerticalGuides.Add(x);
        }

        public void AddHorizontalGuide(float y)
        {
            if (!HorizontalGuides.Contains(y))
                HorizontalGuides.Add(y);
        }

        public void ClearGuides()
        {
            VerticalGuides.Clear();
            HorizontalGuides.Clear();
        }

        public void RemoveVerticalGuide(float x)
        {
            VerticalGuides.Remove(x);
        }

        public void RemoveHorizontalGuide(float y)
        {
            HorizontalGuides.Remove(y);
        }
    }
}