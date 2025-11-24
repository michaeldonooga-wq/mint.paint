using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mint.paint
{
    public class AdvancedColorPicker
    {
        private LimitedStack<SKColor> recentColors;
        private Dictionary<string, List<SKColor>> customPalettes;

        public AdvancedColorPicker()
        {
            recentColors = new LimitedStack<SKColor>(12);
            customPalettes = new Dictionary<string, List<SKColor>>();

            // Инициализация стандартных палитр
            InitializeDefaultPalettes();
        }

        private void InitializeDefaultPalettes()
        {
            // Основные цвета
            var basicColors = new List<SKColor>
            {
                SKColors.Black, SKColors.White, SKColors.Red, SKColors.Green,
                SKColors.Blue, SKColors.Yellow, SKColors.Cyan, SKColors.Magenta,
                SKColors.Gray, SKColors.Orange, SKColors.Purple, SKColors.Brown
            };
            customPalettes["Basic"] = basicColors;

            // Пастельные цвета
            var pastelColors = new List<SKColor>
            {
                new SKColor(255, 209, 220), new SKColor(255, 245, 209),
                new SKColor(209, 255, 209), new SKColor(209, 245, 255),
                new SKColor(245, 209, 255), new SKColor(255, 255, 209)
            };
            customPalettes["Pastel"] = pastelColors;

            // Земляные тона
            var earthColors = new List<SKColor>
            {
                new SKColor(139, 69, 19),   new SKColor(160, 82, 45),
                new SKColor(205, 133, 63),  new SKColor(222, 184, 135),
                new SKColor(245, 222, 179), new SKColor(244, 164, 96)
            };
            customPalettes["Earth Tones"] = earthColors;
        }

        public void AddRecentColor(SKColor color)
        {
            recentColors.Push(color);
        }

        public IEnumerable<SKColor> GetRecentColors()
        {
            return recentColors.GetItems();
        }

        public List<SKColor> GetColorScheme(SKColor baseColor, ColorSchemeType schemeType)
        {
            return schemeType switch
            {
                ColorSchemeType.Monochromatic => GetMonochromaticScheme(baseColor),
                ColorSchemeType.Analogous => GetAnalogousScheme(baseColor),
                ColorSchemeType.Complementary => GetComplementaryScheme(baseColor),
                ColorSchemeType.Triadic => GetTriadicScheme(baseColor),
                ColorSchemeType.Tetradic => GetTetradicScheme(baseColor),
                _ => new List<SKColor> { baseColor }
            };
        }

        private List<SKColor> GetMonochromaticScheme(SKColor baseColor)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);

            return new List<SKColor>
            {
                HslToRgb(h, s, Math.Max(0, l - 0.3)), // Темнее
                baseColor,
                HslToRgb(h, s, Math.Min(1, l + 0.3)), // Светлее
                HslToRgb(h, Math.Max(0, s - 0.3), l), // Менее насыщенный
                HslToRgb(h, Math.Min(1, s + 0.3), l)  // Более насыщенный
            };
        }

        private List<SKColor> GetAnalogousScheme(SKColor baseColor)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);

            return new List<SKColor>
            {
                HslToRgb((h - 30 + 360) % 360, s, l),
                HslToRgb((h - 15 + 360) % 360, s, l),
                baseColor,
                HslToRgb((h + 15) % 360, s, l),
                HslToRgb((h + 30) % 360, s, l)
            };
        }

        private List<SKColor> GetComplementaryScheme(SKColor baseColor)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);

            return new List<SKColor>
            {
                baseColor,
                HslToRgb((h + 180) % 360, s, l) // Дополнительный цвет
            };
        }

        private List<SKColor> GetTriadicScheme(SKColor baseColor)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);

            return new List<SKColor>
            {
                baseColor,
                HslToRgb((h + 120) % 360, s, l),
                HslToRgb((h + 240) % 360, s, l)
            };
        }

        private List<SKColor> GetTetradicScheme(SKColor baseColor)
        {
            RgbToHsl(baseColor, out double h, out double s, out double l);

            return new List<SKColor>
            {
                baseColor,
                HslToRgb((h + 90) % 360, s, l),
                HslToRgb((h + 180) % 360, s, l),
                HslToRgb((h + 270) % 360, s, l)
            };
        }

        // HSL преобразования (такие же как в ColorPickerWindow)
        private SKColor HslToRgb(double h, double s, double l)
        {
            h = Math.Max(0, Math.Min(360, h));
            s = Math.Max(0, Math.Min(100, s)) / 100.0;
            l = Math.Max(0, Math.Min(100, l)) / 100.0;

            double r, g, b;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;

                double hk = h / 360.0;
                r = HueToRgb(p, q, hk + 1.0 / 3);
                g = HueToRgb(p, q, hk);
                b = HueToRgb(p, q, hk - 1.0 / 3);
            }

            return new SKColor(
                (byte)Math.Round(r * 255),
                (byte)Math.Round(g * 255),
                (byte)Math.Round(b * 255)
            );
        }

        private double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;

            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        private void RgbToHsl(SKColor color, out double h, out double s, out double l)
        {
            double r = color.Red / 255.0;
            double g = color.Green / 255.0;
            double b = color.Blue / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            l = (max + min) / 2.0;

            if (max == min)
            {
                h = s = 0;
            }
            else
            {
                double delta = max - min;
                s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);

                if (max == r)
                    h = (g - b) / delta + (g < b ? 6 : 0);
                else if (max == g)
                    h = (b - r) / delta + 2;
                else
                    h = (r - g) / delta + 4;

                h *= 60;
            }

            s *= 100;
            l *= 100;
        }

        public Dictionary<string, List<SKColor>> GetCustomPalettes()
        {
            return customPalettes;
        }

        public void SaveCustomPalette(string name, List<SKColor> colors)
        {
            customPalettes[name] = new List<SKColor>(colors);
        }

        public void RemoveCustomPalette(string name)
        {
            customPalettes.Remove(name);
        }

        public void CreateCustomPalette(string name)
        {
            if (!customPalettes.ContainsKey(name))
            {
                customPalettes[name] = new List<SKColor>();
            }
        }

        public void AddColorToPalette(string paletteName, SKColor color)
        {
            if (customPalettes.ContainsKey(paletteName))
            {
                customPalettes[paletteName].Add(color);
            }
        }
    }

    public enum ColorSchemeType
    {
        Monochromatic,
        Analogous,
        Complementary,
        Triadic,
        Tetradic
    }
}