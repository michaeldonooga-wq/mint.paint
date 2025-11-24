using SkiaSharp;
using System;

namespace mint.paint
{
    public class Layer : IDisposable
    {
        public SKBitmap Bitmap { get; private set; }
        public string Name { get; set; }
        public bool IsVisible { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
        public bool IsLocked { get; set; } = false;
        public LayerEffects Effects { get; set; } = new LayerEffects();

        public Layer(int width, int height, string name = "Layer")
        {
            Bitmap = new SKBitmap(width, height);
            Name = name;
            Clear();
        }

        public Layer(SKBitmap bitmap, string name = "Layer")
        {
            Bitmap = bitmap.Copy();
            Name = name;
        }

        public void Clear()
        {
            using var canvas = new SKCanvas(Bitmap);
            canvas.Clear(SKColors.Transparent);
        }

        public void Draw(SKCanvas canvas, SKPaint paint = null, bool forceVisible = false, float inactiveOpacity = 0.3f)
        {
            if (!IsVisible && !forceVisible) return;
            if (Opacity <= 0) return;

            float opacity = IsVisible ? Opacity : Opacity * inactiveOpacity;
            using var layerPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, (byte)(opacity * 255))
            };

            if (Effects != null && (Effects.DropShadowEnabled || Effects.OuterGlowEnabled || Effects.BevelEnabled))
                Effects.Apply(canvas, Bitmap);

            canvas.DrawBitmap(Bitmap, 0, 0, layerPaint);
        }

        public void Dispose()
        {
            Bitmap?.Dispose();
        }

        public void Resize(int newWidth, int newHeight)
        {
            var oldBitmap = Bitmap;
            Bitmap = new SKBitmap(newWidth, newHeight);
            using var canvas = new SKCanvas(Bitmap);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(oldBitmap, 0, 0);
            oldBitmap.Dispose();
        }

        public Layer Clone()
        {
            var clone = new Layer(Bitmap, Name + " Copy")
            {
                IsVisible = IsVisible,
                Opacity = Opacity,
                BlendMode = BlendMode,
                IsLocked = IsLocked
            };
            clone.Effects = Effects.Clone();
            return clone;
        }
    }

    public enum BlendMode
    {
        Normal,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        ColorDodge,
        ColorBurn,
        HardLight,
        SoftLight,
        Difference,
        Exclusion
    }
}