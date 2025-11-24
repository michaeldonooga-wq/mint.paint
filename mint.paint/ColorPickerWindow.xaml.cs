using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace mint.paint
{
    public partial class ColorPickerWindow : Window
    {
        public Color SelectedColor { get; private set; } = Colors.Blue;
        public event Action<Color> ColorChanged;

        private bool updatingFromSliders = false;
        private bool isAdvancedMode = false;
        private WriteableBitmap colorWheelBitmap;
        private bool isDragging = false;
        private Window ownerWindow;

        public ColorPickerWindow(Window owner)
        {
            ownerWindow = owner;
            InitializeComponent();
            GenerateColorWheel();
            InitializeSliders();
            PositionNearOwner();

            this.Owner = owner;
            this.Topmost = true;

            owner.LocationChanged += (s, e) => PositionNearOwner();
            owner.SizeChanged += (s, e) => PositionNearOwner();
            owner.Closed += (s, e) => this.Close();
            
            LocalizeUI();
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            Title = S("ColorPickerTitle");
            ToggleModeButton.Content = S("MoreSettings");
            HSLGroup.Header = S("HSLSettings");
            RGBGroup.Header = S("RGBSettings");
            ColorAppliedLabel.Text = S("ColorApplied");
            PaletteLabel.Content = S("Palette");
            UpdateHSLTextBlocks();
            UpdateRGBTextBlocks();
        }

        // Инициализация слайдеров начальными значениями
        private void InitializeSliders()
        {
            // Устанавливаем начальные значения без вызова событий
            updatingFromSliders = true;

            RedSlider.Value = SelectedColor.R;
            GreenSlider.Value = SelectedColor.G;
            BlueSlider.Value = SelectedColor.B;

            RgbToHsl(SelectedColor, out double h, out double s, out double l);
            HueSlider.Value = Math.Round(h);
            SaturationSlider.Value = Math.Round(s);
            LightnessSlider.Value = Math.Round(l);

            UpdateHSLTextBlocks();
            UpdateRGBTextBlocks();
            UpdateColorInfo();
            UpdateCursorFromColor();

            updatingFromSliders = false;
        }

        public void SetInitialColor(Color color)
        {
            SelectedColor = color;
            updatingFromSliders = true;

            // Обновляем все слайдеры
            RedSlider.Value = SelectedColor.R;
            GreenSlider.Value = SelectedColor.G;
            BlueSlider.Value = SelectedColor.B;

            RgbToHsl(SelectedColor, out double h, out double s, out double l);
            HueSlider.Value = Math.Round(h);
            SaturationSlider.Value = Math.Round(s);
            LightnessSlider.Value = Math.Round(l);

            UpdateHSLTextBlocks();
            UpdateRGBTextBlocks();
            UpdateColorInfo();
            UpdateCursorFromColor();

            updatingFromSliders = false;
        }

        private void PositionNearOwner()
        {
            if (ownerWindow != null && ownerWindow.IsLoaded)
            {
                this.Left = ownerWindow.Left + ownerWindow.Width + 5;
                this.Top = ownerWindow.Top;

                var screenWidth = SystemParameters.WorkArea.Right;
                if (this.Left + this.Width > screenWidth)
                {
                    this.Left = ownerWindow.Left - this.Width - 5;
                }

                var screenHeight = SystemParameters.WorkArea.Bottom;
                if (this.Top + this.Height > screenHeight)
                {
                    this.Top = screenHeight - this.Height - 10;
                }

                this.Topmost = true;
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void GenerateColorWheel()
        {
            int size = 250;
            colorWheelBitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);

            int center = size / 2;
            int radius = center - 10;

            byte[] pixels = new byte[size * size * 4];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = (y * size + x) * 4;
                    double dx = x - center;
                    double dy = y - center;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius)
                    {
                        double angle = Math.Atan2(dy, dx);
                        if (angle < 0) angle += 2 * Math.PI;

                        double hue = angle * 180 / Math.PI;
                        double saturation = distance / radius;
                        double lightness = 1.0 - (distance / radius) * 0.5;

                        Color color = HslToRgb(hue, saturation * 100, lightness * 100);
                        pixels[index] = color.B;
                        pixels[index + 1] = color.G;
                        pixels[index + 2] = color.R;
                        pixels[index + 3] = 255;
                    }
                    else
                    {
                        pixels[index] = 255;
                        pixels[index + 1] = 255;
                        pixels[index + 2] = 255;
                        pixels[index + 3] = 255;
                    }
                }
            }

            colorWheelBitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
            ColorWheelImage.Source = colorWheelBitmap;
        }

        private Color GetColorFromWheelPosition(Point position)
        {
            int center = 125;
            double dx = position.X - center;
            double dy = position.Y - center;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            int radius = 115;

            if (distance > radius)
            {
                double angle = Math.Atan2(dy, dx);
                distance = radius;
            }

            double angleFinal = Math.Atan2(dy, dx);
            if (angleFinal < 0) angleFinal += 2 * Math.PI;

            double hue = angleFinal * 180 / Math.PI;
            double saturation = Math.Min(distance / radius, 1.0);
            double lightness = 1.0 - (distance / radius) * 0.5;

            return HslToRgb(hue, saturation * 100, lightness * 100);
        }

        private Point GetPositionFromColor(Color color)
        {
            RgbToHsl(color, out double h, out double s, out double l);

            double angle = h * Math.PI / 180;
            double distance = (s / 100) * 115;

            int center = 125;
            double x = center + distance * Math.Cos(angle);
            double y = center + distance * Math.Sin(angle);

            return new Point(x, y);
        }

        // Обработчики событий цветового круга
        private void ColorWheel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            var position = e.GetPosition(ColorWheelImage);
            var constrainedPosition = ConstrainPositionToCircle(position);
            UpdateColorFromWheel(constrainedPosition);
            UpdateCursorPosition(constrainedPosition);
            ColorWheelImage.CaptureMouse();
        }

        private void ColorWheel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(ColorWheelImage);
                var constrainedPosition = ConstrainPositionToCircle(position);
                UpdateColorFromWheel(constrainedPosition);
                UpdateCursorPosition(constrainedPosition);
            }
            else if (isDragging && e.LeftButton == MouseButtonState.Released)
            {
                isDragging = false;
                ColorWheelImage.ReleaseMouseCapture();
            }
        }

        private void ColorWheel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                ColorWheelImage.ReleaseMouseCapture();
            }
        }

        private void ColorWheel_LostMouseCapture(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private Point ConstrainPositionToCircle(Point position)
        {
            int center = 125;
            int radius = 115;

            double dx = position.X - center;
            double dy = position.Y - center;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > radius)
            {
                double angle = Math.Atan2(dy, dx);
                double x = center + radius * Math.Cos(angle);
                double y = center + radius * Math.Sin(angle);
                return new Point(x, y);
            }

            return position;
        }

        private void UpdateCursorPosition(Point position)
        {
            double x = Math.Max(6, Math.Min(244, position.X));
            double y = Math.Max(6, Math.Min(244, position.Y));

            ColorCursor.Margin = new Thickness(x - 6, y - 6, 0, 0);
        }

        // Обновление курсора из текущего цвета
        private void UpdateCursorFromColor()
        {
            var position = GetPositionFromColor(SelectedColor);
            UpdateCursorPosition(position);
        }

        private void UpdateColorFromWheel(Point position)
        {
            var newColor = GetColorFromWheelPosition(position);

            if (newColor != SelectedColor)
            {
                SelectedColor = newColor;
                updatingFromSliders = true;

                // Обновляем все слайдеры
                RedSlider.Value = SelectedColor.R;
                GreenSlider.Value = SelectedColor.G;
                BlueSlider.Value = SelectedColor.B;

                RgbToHsl(SelectedColor, out double h, out double s, out double l);
                HueSlider.Value = Math.Round(h);
                SaturationSlider.Value = Math.Round(s);
                LightnessSlider.Value = Math.Round(l);

                UpdateHSLTextBlocks();
                UpdateRGBTextBlocks();
                UpdateColorInfo();

                updatingFromSliders = false;
            }
        }

        // Плавное переключение режима с анимацией
        private void ToggleModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isAdvancedMode)
            {
                CollapseWindow();
            }
            else
            {
                ExpandWindow();
            }
        }

        private void ExpandWindow()
        {
            isAdvancedMode = true;
            AdvancedSettingsScroll.Visibility = Visibility.Visible;
            ToggleModeButton.Content = Localization.Strings.Get("LessSettings");

            DoubleAnimation animation = new DoubleAnimation
            {
                From = 420,
                To = 650,
                Duration = TimeSpan.FromSeconds(0.3),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };

            this.BeginAnimation(Window.HeightProperty, animation);
        }

        private void CollapseWindow()
        {
            isAdvancedMode = false;
            ToggleModeButton.Content = Localization.Strings.Get("MoreSettings");

            DoubleAnimation animation = new DoubleAnimation
            {
                From = 650,
                To = 420,
                Duration = TimeSpan.FromSeconds(0.3),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };

            animation.Completed += (s, e) =>
            {
                AdvancedSettingsScroll.Visibility = Visibility.Collapsed;
            };

            this.BeginAnimation(Window.HeightProperty, animation);
        }

        // HSL to RGB преобразование
        private Color HslToRgb(double h, double s, double l)
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

            return Color.FromRgb(
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

        // RGB to HSL преобразование
        private void RgbToHsl(Color color, out double h, out double s, out double l)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

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

        // Обновление из HSL слайдеров
        private void UpdateColorFromHSL()
        {
            if (updatingFromSliders) return;

            updatingFromSliders = true;

            double h = HueSlider.Value;
            double s = SaturationSlider.Value;
            double l = LightnessSlider.Value;

            SelectedColor = HslToRgb(h, s, l);

            // Обновляем RGB слайдеры
            RedSlider.Value = SelectedColor.R;
            GreenSlider.Value = SelectedColor.G;
            BlueSlider.Value = SelectedColor.B;

            UpdateCursorFromColor();
            UpdateRGBTextBlocks();
            UpdateColorInfo();

            updatingFromSliders = false;
        }

        // Обновление из RGB слайдеров
        private void UpdateColorFromRGB()
        {
            if (updatingFromSliders) return;

            updatingFromSliders = true;

            SelectedColor = Color.FromRgb(
                (byte)RedSlider.Value,
                (byte)GreenSlider.Value,
                (byte)BlueSlider.Value
            );

            // Обновляем HSL слайдеры
            RgbToHsl(SelectedColor, out double h, out double s, out double l);
            HueSlider.Value = Math.Round(h);
            SaturationSlider.Value = Math.Round(s);
            LightnessSlider.Value = Math.Round(l);

            UpdateCursorFromColor();
            UpdateHSLTextBlocks();
            UpdateColorInfo();

            updatingFromSliders = false;
        }

        private void UpdateHSLTextBlocks()
        {
            var S = Localization.Strings.Get;
            if (HueText != null) HueText.Text = $"{S("Hue")} {(int)HueSlider.Value}°";
            if (SaturationText != null) SaturationText.Text = $"{S("Saturation")} {(int)SaturationSlider.Value}%";
            if (LightnessText != null) LightnessText.Text = $"{S("Lightness")} {(int)LightnessSlider.Value}%";
        }

        private void UpdateRGBTextBlocks()
        {
            var S = Localization.Strings.Get;
            if (RedText != null) RedText.Text = $"{S("Red")} {(int)RedSlider.Value}";
            if (GreenText != null) GreenText.Text = $"{S("Green")} {(int)GreenSlider.Value}";
            if (BlueText != null) BlueText.Text = $"{S("Blue")} {(int)BlueSlider.Value}";
        }

        private void UpdateColorInfo()
        {
            ColorPreview.Background = new SolidColorBrush(SelectedColor);

            double brightness = (SelectedColor.R * 0.299 + SelectedColor.G * 0.587 + SelectedColor.B * 0.114);
            Color textColor = brightness > 186 ? Colors.Black : Colors.White;

            ColorInfoText.Foreground = new SolidColorBrush(textColor);
            ColorInfoText.Text = $"RGB({SelectedColor.R}, {SelectedColor.G}, {SelectedColor.B})";
            HexColorTextBox.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

            ApplyColor();
        }

        private void ApplyColor()
        {
            ColorChanged?.Invoke(SelectedColor);
        }

        // Обработчики событий слайдеров
        private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded && !updatingFromSliders)
            {
                UpdateColorFromHSL();
                UpdateHSLTextBlocks();
            }
        }

        private void SaturationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded && !updatingFromSliders)
            {
                UpdateColorFromHSL();
                UpdateHSLTextBlocks();
            }
        }

        private void LightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded && !updatingFromSliders)
            {
                UpdateColorFromHSL();
                UpdateHSLTextBlocks();
            }
        }

        private void RGBSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded && !updatingFromSliders)
            {
                UpdateColorFromRGB();
                UpdateRGBTextBlocks();
            }
        }

        private void HexColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (updatingFromSliders) return;

            string hex = HexColorTextBox.Text.Trim().ToUpper();
            if (hex.Length == 7 && hex[0] == '#' && Regex.IsMatch(hex.Substring(1), "^[0-9A-F]{6}$"))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    SelectedColor = color;

                    updatingFromSliders = true;

                    // Обновляем все слайдеры
                    RedSlider.Value = SelectedColor.R;
                    GreenSlider.Value = SelectedColor.G;
                    BlueSlider.Value = SelectedColor.B;

                    RgbToHsl(SelectedColor, out double h, out double s, out double l);
                    HueSlider.Value = Math.Round(h);
                    SaturationSlider.Value = Math.Round(s);
                    LightnessSlider.Value = Math.Round(l);

                    UpdateHSLTextBlocks();
                    UpdateRGBTextBlocks();
                    UpdateCursorFromColor();
                    UpdateColorInfo();

                    updatingFromSliders = false;
                }
                catch
                {
                    // Неверный формат цвета
                }
            }
        }

        private void HexColorTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9A-Fa-f#]$");
        }

        protected override void OnClosed(EventArgs e)
        {
            if (isDragging)
            {
                ColorWheelImage.ReleaseMouseCapture();
                isDragging = false;
            }
            base.OnClosed(e);
        }
    }
}