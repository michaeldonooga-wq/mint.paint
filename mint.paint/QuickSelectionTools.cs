using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace mint.paint
{
    public static class QuickSelectionTools
    {
        // Волшебная палочка - выделение похожих цветов
        public static List<SKPoint> MagicWand(SKBitmap bitmap, Point point, float tolerance)
        {
            if (bitmap == null) return new List<SKPoint>();

            int x = (int)point.X;
            int y = (int)point.Y;

            if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height)
                return new List<SKPoint>();

            var targetColor = bitmap.GetPixel(x, y);
            var visited = new bool[bitmap.Width, bitmap.Height];
            var pixelsToCheck = new Queue<Point>();
            var selectedPixels = new List<SKPoint>();

            pixelsToCheck.Enqueue(point);

            while (pixelsToCheck.Count > 0 && selectedPixels.Count < 5000) // Ограничение для производительности
            {
                var current = pixelsToCheck.Dequeue();
                int cx = (int)current.X, cy = (int)current.Y;

                if (cx < 0 || cx >= bitmap.Width || cy < 0 || cy >= bitmap.Height)
                    continue;

                if (visited[cx, cy])
                    continue;

                visited[cx, cy] = true;

                var currentColor = bitmap.GetPixel(cx, cy);
                if (ColorDistance(targetColor, currentColor) <= tolerance)
                {
                    selectedPixels.Add(new SKPoint(cx, cy));

                    // Добавляем соседние пиксели (8-связность)
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            if (kx == 0 && ky == 0) continue;
                            pixelsToCheck.Enqueue(new Point(cx + kx, cy + ky));
                        }
                    }
                }
            }

            return selectedPixels;
        }

        // Полигональное лассо
        public static List<SKPoint> PolygonalLasso(List<Point> points)
        {
            return points.Select(p => new SKPoint((float)p.X, (float)p.Y)).ToList();
        }

        // Магнитное лассо (привязывается к краям)
        public static List<SKPoint> MagneticLasso(SKBitmap bitmap, List<Point> points, float edgeThreshold)
        {
            var result = new List<SKPoint>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var start = points[i];
                var end = points[i + 1];

                // Ищем ближайший край вдоль линии
                var edgePoints = FindEdgePointsAlongLine(bitmap, start, end, edgeThreshold);
                result.AddRange(edgePoints);
            }

            return result;
        }

        private static List<SKPoint> FindEdgePointsAlongLine(SKBitmap bitmap, Point start, Point end, float threshold)
        {
            var edgePoints = new List<SKPoint>();
            var steps = (int)Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                var x = (int)(start.X + t * (end.X - start.X));
                var y = (int)(start.Y + t * (end.Y - start.Y));

                if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height)
                    continue;

                // Проверяем градиент (край)
                var gradient = CalculateGradient(bitmap, x, y);
                if (gradient > threshold)
                {
                    edgePoints.Add(new SKPoint(x, y));
                }
            }

            return edgePoints;
        }

        private static float CalculateGradient(SKBitmap bitmap, int x, int y)
        {
            // Простой оператор Собеля для обнаружения краев
            float gx = 0, gy = 0;

            for (int ky = -1; ky <= 1; ky++)
            {
                for (int kx = -1; kx <= 1; kx++)
                {
                    int px = x + kx;
                    int py = y + ky;

                    if (px < 0 || px >= bitmap.Width || py < 0 || py >= bitmap.Height)
                        continue;

                    var pixel = bitmap.GetPixel(px, py);
                    var intensity = (pixel.Red + pixel.Green + pixel.Blue) / 3.0f / 255.0f;

                    // Ядра Собеля
                    gx += intensity * kx;
                    gy += intensity * ky;
                }
            }

            return (float)Math.Sqrt(gx * gx + gy * gy);
        }

        // Прямоугольное выделение
        public static SKRect RectangleSelection(Point start, Point end)
        {
            return new SKRect(
                (float)Math.Min(start.X, end.X),
                (float)Math.Min(start.Y, end.Y),
                (float)Math.Max(start.X, end.X),
                (float)Math.Max(start.Y, end.Y)
            );
        }

        // Эллиптическое выделение
        public static SKRect EllipticalSelection(Point start, Point end)
        {
            return new SKRect(
                (float)Math.Min(start.X, end.X),
                (float)Math.Min(start.Y, end.Y),
                (float)Math.Max(start.X, end.X),
                (float)Math.Max(start.Y, end.Y)
            );
        }

        private static float ColorDistance(SKColor c1, SKColor c2)
        {
            // Взвешенная евклидова дистанция (человеческое восприятие)
            var r = c1.Red - c2.Red;
            var g = c1.Green - c2.Green;
            var b = c1.Blue - c2.Blue;

            return (float)Math.Sqrt(r * r * 0.299 + g * g * 0.587 + b * b * 0.114);
        }

        // Создает маску из списка точек
        public static SKBitmap CreateMaskFromPoints(List<SKPoint> points, int width, int height)
        {
            var mask = new SKBitmap(width, height);
            using var canvas = new SKCanvas(mask);

            // Заливаем белым
            canvas.Clear(SKColors.White);

            if (points.Count > 2)
            {
                using var path = new SKPath();
                path.MoveTo(points[0]);

                for (int i = 1; i < points.Count; i++)
                {
                    path.LineTo(points[i]);
                }

                path.Close();

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                canvas.DrawPath(path, paint);
            }

            return mask;
        }
    }
}