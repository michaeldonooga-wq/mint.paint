using SkiaSharp;
using System.Collections.Generic;
using System.Linq; // Добавляем эту директиву

namespace mint.paint
{
    public class BrushManager
    {
        public double Size { get; set; } = 30;
        public double Hardness { get; set; } = 0;
        public SKColor PrimaryColor { get; set; } = SKColors.Blue;
        public SKColor SecondaryColor { get; set; } = SKColors.White;
        public BrushType CurrentBrushType { get; set; } = BrushType.SoftRound;
        public double DropSize { get; set; } = 0.3;
        public int DropCount { get; set; } = 2;

        // Для истории цветов
        private LimitedStack<SKColor> colorHistory;
        private System.Random random = new System.Random();

        public BrushManager()
        {
            colorHistory = new LimitedStack<SKColor>(10);
            colorHistory.Push(PrimaryColor);
        }

        public SKColor Color
        {
            get => PrimaryColor;
            set
            {
                PrimaryColor = value;
                colorHistory.Push(value);
            }
        }

        public SKColor GetCurrentColor()
        {
            return PrimaryColor;
        }

        public void SwapColors()
        {
            var temp = PrimaryColor;
            PrimaryColor = SecondaryColor;
            SecondaryColor = temp;
        }

        public IEnumerable<SKColor> GetColorHistory()
        {
            return colorHistory.GetItems();
        }

        public void AddColorToHistory(SKColor color)
        {
            colorHistory.Push(color);
        }

        public void DrawBrushStroke(SKCanvas canvas, SKPoint point, SKPoint? lastPoint = null)
        {
            var paint = new SKPaint
            {
                Color = PrimaryColor,
                IsAntialias = true
            };

            switch (CurrentBrushType)
            {
                case BrushType.Watercolor:
                    using (var shader = SKShader.CreateRadialGradient(
                        point, (float)Size,
                        new[] { new SKColor(PrimaryColor.Red, PrimaryColor.Green, PrimaryColor.Blue, (byte)(PrimaryColor.Alpha * Hardness / 100 * 0.6)), 
                                new SKColor(PrimaryColor.Red, PrimaryColor.Green, PrimaryColor.Blue, 0) },
                        new[] { 0f, 1f },
                        SKShaderTileMode.Clamp))
                    {
                        paint.Shader = shader;
                        canvas.DrawCircle(point.X, point.Y, (float)Size * 1.2f, paint);
                    }
                    break;

                case BrushType.Oil:
                    paint.Style = SKPaintStyle.Stroke;
                    paint.IsAntialias = true;
                    paint.StrokeCap = SKStrokeCap.Round;
                    var angle = 0.0;
                    if (lastPoint.HasValue)
                    {
                        var dx = point.X - lastPoint.Value.X;
                        var dy = point.Y - lastPoint.Value.Y;
                        var distance = System.Math.Sqrt(dx * dx + dy * dy);
                        if (distance > 1)
                            angle = System.Math.Atan2(dy, dx);
                    }
                    var perpAngle = angle + System.Math.PI / 2;
                    var brushCount = 5;
                    for (int i = 0; i < brushCount; i++)
                    {
                        var offset = (i - brushCount / 2f) * (float)Size / (brushCount - 1);
                        var alpha = (byte)(PrimaryColor.Alpha * (1f - System.Math.Abs(offset) / (float)Size * 0.5));
                        paint.Color = new SKColor(PrimaryColor.Red, PrimaryColor.Green, PrimaryColor.Blue, alpha);
                        paint.StrokeWidth = (float)Size / 3f;
                        var offsetX = (float)(System.Math.Cos(perpAngle) * offset);
                        var offsetY = (float)(System.Math.Sin(perpAngle) * offset);
                        canvas.DrawPoint(new SKPoint(point.X + offsetX, point.Y + offsetY), paint);
                    }
                    break;

                case BrushType.Textured:
                    for (int i = 0; i < 10; i++)
                    {
                        var offset = (float)(random.NextDouble() * Size);
                        var textureAngle = random.NextDouble() * System.Math.PI * 2;
                        var x = point.X + (float)(System.Math.Cos(textureAngle) * offset);
                        var y = point.Y + (float)(System.Math.Sin(textureAngle) * offset);
                        canvas.DrawCircle(x, y, (float)(random.NextDouble() * 3 + 1), paint);
                    }
                    break;

                case BrushType.Pixel:
                    paint.IsAntialias = false;
                    paint.Style = SKPaintStyle.Fill;
                    canvas.DrawRect(SKRect.Create((int)point.X, (int)point.Y, (int)Size, (int)Size), paint);
                    break;

                case BrushType.Drops:
                    for (int i = 0; i < DropCount; i++)
                    {
                        var offset = (float)(random.NextDouble() * Size * 0.8);
                        var dropAngle = random.NextDouble() * System.Math.PI * 2;
                        var x = point.X + (float)(System.Math.Cos(dropAngle) * offset);
                        var y = point.Y + (float)(System.Math.Sin(dropAngle) * offset);
                        var dropSize = (float)(random.NextDouble() * Size * DropSize + Size * 0.1);
                        var alpha = (byte)(PrimaryColor.Alpha * (0.5 + random.NextDouble() * 0.5));
                        paint.Color = new SKColor(PrimaryColor.Red, PrimaryColor.Green, PrimaryColor.Blue, alpha);
                        canvas.DrawCircle(x, y, dropSize, paint);
                    }
                    break;

                default:
                    var hardnessRatio = (float)(Hardness / 100.0);
                    using (var shader = SKShader.CreateRadialGradient(
                        point, (float)Size / 2,
                        new[] { PrimaryColor, new SKColor(PrimaryColor.Red, PrimaryColor.Green, PrimaryColor.Blue, 0) },
                        new[] { hardnessRatio, 1f },
                        SKShaderTileMode.Clamp))
                    {
                        paint.Shader = shader;
                        paint.Style = SKPaintStyle.Fill;
                        canvas.DrawCircle(point.X, point.Y, (float)Size / 2, paint);
                    }
                    break;
            }

            paint.Dispose();
        }

        public void UpdatePaint(SKPaint paint, ToolsPanel.ToolType tool)
        {
            if (paint == null) return;

            switch (tool)
            {
                case ToolsPanel.ToolType.Brush:
                    paint.Color = PrimaryColor;
                    paint.StrokeWidth = (float)Size;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.IsAntialias = true;
                    paint.StrokeCap = SKStrokeCap.Round;
                    paint.StrokeJoin = SKStrokeJoin.Round;
                    break;

                case ToolsPanel.ToolType.Eraser:
                    paint.Color = SKColors.Transparent;
                    paint.StrokeWidth = (float)Size;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.IsAntialias = true;
                    paint.BlendMode = SKBlendMode.Clear;
                    break;

                case ToolsPanel.ToolType.Line:
                case ToolsPanel.ToolType.Rectangle:
                case ToolsPanel.ToolType.Circle:
                    paint.Color = PrimaryColor;
                    paint.StrokeWidth = 2f;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.IsAntialias = true;
                    break;

                case ToolsPanel.ToolType.Selection:
                    paint.Color = new SKColor(0, 120, 215, 100);
                    paint.StrokeWidth = 2f;
                    paint.Style = SKPaintStyle.Stroke;
                    paint.IsAntialias = true;
                    break;
            }
        }
    }

    // Класс для ограниченной истории
    public class LimitedStack<T>
    {
        private Stack<T> stack;
        private int capacity;

        public LimitedStack(int capacity)
        {
            this.capacity = capacity;
            stack = new Stack<T>(capacity);
        }

        public void Push(T item)
        {
            if (stack.Count >= capacity)
            {
                var items = stack.ToArray();
                stack.Clear();
                for (int i = items.Length - 1; i > 0; i--)
                    stack.Push(items[i]);
            }
            stack.Push(item);
        }

        public T Pop()
        {
            return stack.Pop();
        }

        public IEnumerable<T> GetItems()
        {
            // Исправлено: используем LINQ для реверса
            return stack.ToArray().Reverse();
        }

        public int Count => stack.Count;
    }
}