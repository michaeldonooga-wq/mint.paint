using SkiaSharp;

namespace mint.paint
{
    public class LayerEffects
    {
        public bool DropShadowEnabled { get; set; }
        public SKColor DropShadowColor { get; set; } = new SKColor(0, 0, 0, 128);
        public float DropShadowOffsetX { get; set; } = 5;
        public float DropShadowOffsetY { get; set; } = 5;
        public float DropShadowBlur { get; set; } = 5;

        public bool OuterGlowEnabled { get; set; }
        public SKColor OuterGlowColor { get; set; } = new SKColor(255, 255, 0, 128);
        public float OuterGlowSize { get; set; } = 10;

        public bool BevelEnabled { get; set; }
        public BevelType BevelType { get; set; } = BevelType.Emboss;
        public float BevelDepth { get; set; } = 5;

        public LayerEffects Clone()
        {
            return new LayerEffects
            {
                DropShadowEnabled = DropShadowEnabled,
                DropShadowColor = DropShadowColor,
                DropShadowOffsetX = DropShadowOffsetX,
                DropShadowOffsetY = DropShadowOffsetY,
                DropShadowBlur = DropShadowBlur,
                OuterGlowEnabled = OuterGlowEnabled,
                OuterGlowColor = OuterGlowColor,
                OuterGlowSize = OuterGlowSize,
                BevelEnabled = BevelEnabled,
                BevelType = BevelType,
                BevelDepth = BevelDepth
            };
        }

        public void Apply(SKCanvas canvas, SKBitmap bitmap)
        {
            if (DropShadowEnabled)
            {
                using var shadowPaint = new SKPaint
                {
                    Color = DropShadowColor,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, DropShadowBlur)
                };
                canvas.DrawBitmap(bitmap, DropShadowOffsetX, DropShadowOffsetY, shadowPaint);
            }

            if (OuterGlowEnabled)
            {
                using var glowPaint = new SKPaint
                {
                    Color = OuterGlowColor,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, OuterGlowSize)
                };
                canvas.DrawBitmap(bitmap, 0, 0, glowPaint);
            }

            if (BevelEnabled)
            {
                ApplyBevel(canvas, bitmap);
            }
        }

        private void ApplyBevel(SKCanvas canvas, SKBitmap bitmap)
        {
            var depth = (int)BevelDepth;
            var isEmboss = BevelType == BevelType.Emboss;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.Alpha < 10) continue;

                    bool hasTopLeft = x > 0 && y > 0 && bitmap.GetPixel(x - 1, y - 1).Alpha > 10;
                    bool hasBottomRight = x < bitmap.Width - 1 && y < bitmap.Height - 1 && bitmap.GetPixel(x + 1, y + 1).Alpha > 10;

                    if (!hasTopLeft || !hasBottomRight)
                    {
                        using var paint = new SKPaint { IsAntialias = true };

                        if (!hasTopLeft)
                        {
                            paint.Color = isEmboss ? new SKColor(255, 255, 255, 180) : new SKColor(0, 0, 0, 180);
                            canvas.DrawRect(x, y, 1, 1, paint);
                        }

                        if (!hasBottomRight)
                        {
                            paint.Color = isEmboss ? new SKColor(0, 0, 0, 180) : new SKColor(255, 255, 255, 180);
                            canvas.DrawRect(x, y, 1, 1, paint);
                        }
                    }
                }
            }
        }
    }

    public enum BevelType
    {
        Emboss,
        Engrave
    }
}
