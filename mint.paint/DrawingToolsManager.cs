using SkiaSharp;

namespace mint.paint
{
    public class DrawingToolsManager
    {
        public void DrawLine(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            canvas.DrawLine(start, end, paint);
        }

        public void DrawRectangle(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            var rect = SKRect.Create(
                System.Math.Min(start.X, end.X),
                System.Math.Min(start.Y, end.Y),
                System.Math.Abs(end.X - start.X),
                System.Math.Abs(end.Y - start.Y)
            );
            canvas.DrawRect(rect, paint);
        }

        public void DrawCircle(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
        {
            float radius = SKPoint.Distance(start, end) / 2;
            var center = new SKPoint(
                (start.X + end.X) / 2,
                (start.Y + end.Y) / 2
            );
            canvas.DrawCircle(center, radius, paint);
        }

        public void ApplyFill(SKBitmap bitmap, SKPoint point, SKColor fillColor)
        {
            int startX = (int)point.X;
            int startY = (int)point.Y;

            if (startX < 0 || startX >= bitmap.Width || startY < 0 || startY >= bitmap.Height) return;

            SKColor targetColor = bitmap.GetPixel(startX, startY);
            if (targetColor == fillColor) return;

            var pixels = bitmap.Pixels;
            int width = bitmap.Width;
            int height = bitmap.Height;
            uint targetColorValue = (uint)targetColor;
            uint fillColorValue = (uint)fillColor;

            var stack = new System.Collections.Generic.Stack<(int x, int y)>();
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                if (y < 0 || y >= height) continue;

                int left = x;
                while (left > 0 && (uint)pixels[y * width + left - 1] == targetColorValue)
                    left--;

                int right = x;
                while (right < width && (uint)pixels[y * width + right] == targetColorValue)
                    right++;

                for (int i = left; i < right; i++)
                    pixels[y * width + i] = fillColor;

                for (int i = left; i < right; i++)
                {
                    if (y > 0 && (uint)pixels[(y - 1) * width + i] == targetColorValue)
                        stack.Push((i, y - 1));
                    if (y < height - 1 && (uint)pixels[(y + 1) * width + i] == targetColorValue)
                        stack.Push((i, y + 1));
                }
            }

            bitmap.Pixels = pixels;
        }

        public SKRect? MagicWandSelect(SKBitmap bitmap, SKPoint point, int tolerance = 32)
        {
            int startX = (int)point.X;
            int startY = (int)point.Y;

            if (startX < 0 || startX >= bitmap.Width || startY < 0 || startY >= bitmap.Height)
                return null;

            SKColor targetColor = bitmap.GetPixel(startX, startY);
            var pixels = bitmap.Pixels;
            int width = bitmap.Width;
            int height = bitmap.Height;

            var visited = new bool[width * height];
            var stack = new System.Collections.Generic.Stack<(int x, int y)>();
            stack.Push((startX, startY));

            int minX = startX, maxX = startX, minY = startY, maxY = startY;

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                if (y < 0 || y >= height) continue;

                int left = x;
                while (left > 0 && !visited[y * width + left - 1] && ColorMatch(pixels[y * width + left - 1], targetColor, tolerance))
                    left--;

                int right = x;
                while (right < width && !visited[y * width + right] && ColorMatch(pixels[y * width + right], targetColor, tolerance))
                    right++;

                for (int i = left; i < right; i++)
                    visited[y * width + i] = true;

                minX = System.Math.Min(minX, left);
                maxX = System.Math.Max(maxX, right - 1);
                minY = System.Math.Min(minY, y);
                maxY = System.Math.Max(maxY, y);

                for (int i = left; i < right; i++)
                {
                    if (y > 0 && !visited[(y - 1) * width + i] && ColorMatch(pixels[(y - 1) * width + i], targetColor, tolerance))
                        stack.Push((i, y - 1));
                    if (y < height - 1 && !visited[(y + 1) * width + i] && ColorMatch(pixels[(y + 1) * width + i], targetColor, tolerance))
                        stack.Push((i, y + 1));
                }
            }

            return new SKRect(minX, minY, maxX + 1, maxY + 1);
        }

        private bool ColorMatch(SKColor c1, SKColor c2, int tolerance)
        {
            return System.Math.Abs(c1.Red - c2.Red) <= tolerance &&
                   System.Math.Abs(c1.Green - c2.Green) <= tolerance &&
                   System.Math.Abs(c1.Blue - c2.Blue) <= tolerance &&
                   System.Math.Abs(c1.Alpha - c2.Alpha) <= tolerance;
        }
    }
}