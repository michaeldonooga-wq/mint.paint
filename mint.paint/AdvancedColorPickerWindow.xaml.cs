using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace mint.paint
{
    public partial class AdvancedColorPickerWindow : Window
    {
        private DrawingManager drawingManager;
        private SKColor selectedColor;
        private SKColor previousColor;
        private bool isUpdating = false;
        private bool isEditingPrimary = true;

        public event Action<SKColor> ColorSelected;

        public AdvancedColorPickerWindow(DrawingManager drawingManager)
        {
            InitializeComponent();
            if (Application.Current.Resources.Contains("DefaultFontFamily"))
                this.FontFamily = (FontFamily)Application.Current.Resources["DefaultFontFamily"];
            this.drawingManager = drawingManager;
            selectedColor = drawingManager.GetCurrentBrushColor();
            previousColor = selectedColor;
            
            this.Loaded += (s, e) =>
            {
                DrawColorWheel();
                InitializeColorSchemes();
                InitializeRecentColors();
                InitializeCustomPalettes();
                previousColor = drawingManager.GetSecondaryColor();
                PreviousColorBrush.Color = Color.FromRgb(previousColor.Red, previousColor.Green, previousColor.Blue);
                UpdateColorPreview();
                UpdateAllSliders();
            };
        }

        private void DrawColorWheel()
        {
            int size = 250;
            var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(size, size, 96, 96, 
                System.Windows.Media.PixelFormats.Bgr32, null);

            bitmap.Lock();
            unsafe
            {
                byte* ptr = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;
                int centerX = size / 2;
                int centerY = size / 2;
                int radius = size / 2 - 10;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int dx = x - centerX;
                        int dy = y - centerY;
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        int offset = y * stride + x * 4;

                        if (distance <= radius)
                        {
                            double angle = Math.Atan2(dy, dx);
                            double hue = (angle + Math.PI) / (2 * Math.PI) * 360;
                            double saturation = distance / radius;

                            var color = HSVToRGB(hue, saturation, 1.0);
                            ptr[offset] = color.Blue;
                            ptr[offset + 1] = color.Green;
                            ptr[offset + 2] = color.Red;
                            ptr[offset + 3] = 255;
                        }
                        else
                        {
                            ptr[offset] = 255;
                            ptr[offset + 1] = 255;
                            ptr[offset + 2] = 255;
                            ptr[offset + 3] = 255;
                        }
                    }
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, size, size));
            bitmap.Unlock();

            ColorWheelImage.Source = bitmap;
        }

        private SKColor HSVToRGB(double h, double s, double v)
        {
            int hi = (int)(h / 60) % 6;
            double f = h / 60 - Math.Floor(h / 60);
            byte vByte = (byte)(v * 255);
            byte p = (byte)(v * (1 - s) * 255);
            byte q = (byte)(v * (1 - f * s) * 255);
            byte t = (byte)(v * (1 - (1 - f) * s) * 255);

            return hi switch
            {
                0 => new SKColor(vByte, t, p),
                1 => new SKColor(q, vByte, p),
                2 => new SKColor(p, vByte, t),
                3 => new SKColor(p, q, vByte),
                4 => new SKColor(t, p, vByte),
                _ => new SKColor(vByte, p, q)
            };
        }

        private bool isDraggingWheel = false;

        private void ColorWheel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isDraggingWheel = true;
            ColorWheelImage.CaptureMouse();
            PickColorFromWheel(e.GetPosition(ColorWheelImage));
        }

        private void ColorWheel_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDraggingWheel && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                PickColorFromWheel(e.GetPosition(ColorWheelImage));
            }
            else if (isDraggingWheel)
            {
                isDraggingWheel = false;
                ColorWheelImage.ReleaseMouseCapture();
            }
        }

        private void PickColorFromWheel(Point point)
        {
            int centerX = 125;
            int centerY = 125;
            double dx = point.X - centerX;
            double dy = point.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double radius = 115;

            if (distance > radius)
            {
                double angle = Math.Atan2(dy, dx);
                dx = radius * Math.Cos(angle);
                dy = radius * Math.Sin(angle);
                distance = radius;
            }

            double angle2 = Math.Atan2(dy, dx);
            double hue = (angle2 + Math.PI) / (2 * Math.PI) * 360;
            double saturation = Math.Min(distance / radius, 1.0);

            var color = HSVToRGB(hue, saturation, 1.0);
            byte alpha = AlphaSlider != null ? (byte)AlphaSlider.Value : selectedColor.Alpha;
            selectedColor = new SKColor(color.Red, color.Green, color.Blue, alpha);
            UpdateColorPreview();
            UpdateAllSliders();
        }



        private void ToggleRGB_Click(object sender, RoutedEventArgs e)
        {
            if (RGBPanel.Visibility == Visibility.Collapsed)
            {
                RGBPanel.Visibility = Visibility.Visible;
                ToggleRGBButton.Content = "▲ RGB";
                AnimateHeight(this.Height, this.Height + 90);
            }
            else
            {
                ToggleRGBButton.Content = "▼ RGB";
                AnimateHeight(this.Height, this.Height - 90, () => RGBPanel.Visibility = Visibility.Collapsed);
            }
        }

        private void ToggleHSV_Click(object sender, RoutedEventArgs e)
        {
            if (HSVPanel.Visibility == Visibility.Collapsed)
            {
                HSVPanel.Visibility = Visibility.Visible;
                ToggleHSVButton.Content = "▲ HSV";
                AnimateHeight(this.Height, this.Height + 90);
            }
            else
            {
                ToggleHSVButton.Content = "▼ HSV";
                AnimateHeight(this.Height, this.Height - 90, () => HSVPanel.Visibility = Visibility.Collapsed);
            }
        }

        private void AnimateHeight(double from, double to, Action onComplete = null)
        {
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(0.3),
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };
            if (onComplete != null)
                animation.Completed += (s, e) => onComplete();
            this.BeginAnimation(Window.HeightProperty, animation);
        }

        private void RGB_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            selectedColor = new SKColor((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value, (byte)AlphaSlider.Value);
            UpdateColorPreview();
            UpdateRGBText();
            UpdateHSVSliders();
            UpdateHexText();
            UpdateCursorPosition();
        }

        private void Alpha_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating || AlphaText == null) return;

            selectedColor = new SKColor(selectedColor.Red, selectedColor.Green, selectedColor.Blue, (byte)AlphaSlider.Value);
            AlphaText.Text = ((int)AlphaSlider.Value).ToString();
            UpdateColorPreview();
        }

        private void HSV_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isUpdating) return;

            var color = HSVToRGB(HueSlider.Value, SatSlider.Value / 100.0, ValSlider.Value / 100.0);
            selectedColor = new SKColor(color.Red, color.Green, color.Blue, (byte)AlphaSlider.Value);
            UpdateColorPreview();
            UpdateRGBSliders();
            UpdateHSVText();
            UpdateHexText();
            UpdateCursorPosition();
        }

        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdating || AlphaSlider == null || drawingManager == null) return;

            string hex = HexTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(hex)) return;
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int colorValue))
            {
                byte r = (byte)((colorValue >> 16) & 0xFF);
                byte g = (byte)((colorValue >> 8) & 0xFF);
                byte b = (byte)(colorValue & 0xFF);
                selectedColor = new SKColor(r, g, b, (byte)AlphaSlider.Value);
                UpdateColorPreview();
                UpdateAllSliders();
            }
        }

        private void UpdateAllSliders()
        {
            if (RedSlider == null) return;
            isUpdating = true;
            UpdateRGBSliders();
            UpdateHSVSliders();
            UpdateHexText();
            UpdateCursorPosition();
            isUpdating = false;
        }

        private void UpdateRGBSliders()
        {
            if (RedSlider == null) return;
            RedSlider.Value = selectedColor.Red;
            GreenSlider.Value = selectedColor.Green;
            BlueSlider.Value = selectedColor.Blue;
            AlphaSlider.Value = selectedColor.Alpha;
            UpdateRGBText();
            AlphaText.Text = selectedColor.Alpha.ToString();
        }

        private void UpdateHSVSliders()
        {
            if (HueSlider == null) return;
            var (h, s, v) = RGBToHSV(selectedColor.Red, selectedColor.Green, selectedColor.Blue);
            HueSlider.Value = h;
            SatSlider.Value = s * 100;
            ValSlider.Value = v * 100;
            UpdateHSVText();
        }

        private void UpdateHexText()
        {
            if (HexTextBox == null) return;
            HexTextBox.Text = $"#{selectedColor.Red:X2}{selectedColor.Green:X2}{selectedColor.Blue:X2}";
        }

        private void UpdateHSVText()
        {
            if (HueText == null) return;
            HueText.Text = ((int)HueSlider.Value).ToString();
            SatText.Text = ((int)SatSlider.Value).ToString();
            ValText.Text = ((int)ValSlider.Value).ToString();
        }

        private void UpdateCursorPosition()
        {
            if (ColorCursor == null) return;
            
            var (h, s, v) = RGBToHSV(selectedColor.Red, selectedColor.Green, selectedColor.Blue);
            
            double centerX = 125;
            double centerY = 125;
            double radius = 115;
            
            double angleRadians = (h / 360.0 * 2 * Math.PI) - Math.PI;
            double distance = s * radius;
            
            double x = centerX + distance * Math.Cos(angleRadians);
            double y = centerY + distance * Math.Sin(angleRadians);
            
            Canvas.SetLeft(ColorCursor, x - 5);
            Canvas.SetTop(ColorCursor, y - 5);
        }

        private (double h, double s, double v) RGBToHSV(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;
            
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;
            
            double h = 0;
            if (delta != 0)
            {
                if (max == rd)
                    h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd)
                    h = 60 * (((bd - rd) / delta) + 2);
                else
                    h = 60 * (((rd - gd) / delta) + 4);
            }
            if (h < 0) h += 360;
            
            double s = max == 0 ? 0 : delta / max;
            double v = max;
            
            return (h, s, v);
        }

        private void UpdateRGBText()
        {
            if (RedText == null) return;
            RedText.Text = ((int)RedSlider.Value).ToString();
            GreenText.Text = ((int)GreenSlider.Value).ToString();
            BlueText.Text = ((int)BlueSlider.Value).ToString();
        }

        private void InitializeColorSchemes()
        {
            if (SchemeTypeComboBox == null) return;
            SchemeTypeComboBox.SelectedIndex = 0;
        }

        private void InitializeRecentColors()
        {
            if (RecentColorsControl == null || drawingManager == null) return;
            var recentColors = drawingManager.GetRecentColors()
                .Select(c => new ColorWrapper(c))
                .ToList();
            RecentColorsControl.ItemsSource = recentColors;
        }

        private void InitializeCustomPalettes()
        {
            if (PaletteComboBox == null || drawingManager == null) return;
            var palettes = drawingManager.GetCustomPalettes();
            PaletteComboBox.Items.Clear();

            foreach (var paletteName in palettes.Keys)
            {
                PaletteComboBox.Items.Add(paletteName);
            }

            if (PaletteComboBox.Items.Count > 0)
            {
                PaletteComboBox.SelectedIndex = 0;
            }
        }

        private void SchemeTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SchemeColorsControl == null || drawingManager == null || SchemeTypeComboBox.SelectedIndex < 0) return;
            var currentColor = isEditingPrimary ? selectedColor : drawingManager.GetSecondaryColor();
            var schemeType = (ColorSchemeType)SchemeTypeComboBox.SelectedIndex;
            var colors = drawingManager.GetColorPicker().GetColorScheme(currentColor, schemeType)
                .Select(c => new ColorWrapper(c))
                .ToList();
            SchemeColorsControl.ItemsSource = colors;
        }

        private void PaletteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaletteComboBox.SelectedItem is string paletteName)
            {
                var palettes = drawingManager.GetCustomPalettes();
                if (palettes.ContainsKey(paletteName))
                {
                    var colors = palettes[paletteName]
                        .Select(c => new ColorWrapper(c))
                        .ToList();
                    PaletteColorsControl.ItemsSource = colors;
                }
            }
        }

        private void ColorItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ColorWrapper colorWrapper)
            {
                selectedColor = colorWrapper.SKColor;
                UpdateColorPreview();
                UpdateAllSliders();
            }
        }

        private void UpdateColorPreview()
        {
            if (CurrentColorBrush == null) return;
            
            if (isEditingPrimary)
            {
                CurrentColorBrush.Color = Color.FromArgb(selectedColor.Alpha, selectedColor.Red, selectedColor.Green, selectedColor.Blue);
                drawingManager.SetBrushColor(selectedColor);
            }
            else
            {
                PreviousColorBrush.Color = Color.FromArgb(selectedColor.Alpha, selectedColor.Red, selectedColor.Green, selectedColor.Blue);
                drawingManager.SetSecondaryColor(selectedColor);
            }
            
            ColorSelected?.Invoke(selectedColor);
            
            if (SchemeTypeComboBox?.SelectedIndex >= 0)
            {
                SchemeTypeComboBox_SelectionChanged(null, null);
            }
        }

        private void PrimaryColor_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isEditingPrimary = true;
            PrimaryColorBorder.BorderThickness = new Thickness(3);
            SecondaryColorBorder.BorderThickness = new Thickness(1);
            selectedColor = drawingManager.GetCurrentBrushColor();
            UpdateAllSliders();
        }

        private void SecondaryColor_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isEditingPrimary = false;
            PrimaryColorBorder.BorderThickness = new Thickness(1);
            SecondaryColorBorder.BorderThickness = new Thickness(3);
            selectedColor = drawingManager.GetSecondaryColor();
            UpdateAllSliders();
        }

        private void CreatePaletteButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Window
            {
                Title = "Новая палитра",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "OK", Width = 70, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 70, Margin = new Thickness(5) };

            okButton.Click += (s, args) => { dialog.DialogResult = true; dialog.Close(); };
            cancelButton.Click += (s, args) => { dialog.DialogResult = false; dialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(new TextBlock { Text = "Введите название палитры:", Margin = new Thickness(0, 0, 0, 5) });
            stack.Children.Add(textBox);
            stack.Children.Add(buttonPanel);
            dialog.Content = stack;

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string paletteName = textBox.Text.Trim();
                drawingManager.CreateCustomPalette(paletteName);
                InitializeCustomPalettes();
                PaletteComboBox.SelectedItem = paletteName;
            }
        }

        private void AddColorToPaletteButton_Click(object sender, RoutedEventArgs e)
        {
            if (PaletteComboBox.SelectedItem is string paletteName)
            {
                drawingManager.AddColorToPalette(paletteName, selectedColor);
                PaletteComboBox_SelectionChanged(null, null);
            }
        }
    }

    // Класс-обертка для привязки SKColor к WPF
    public class ColorWrapper
    {
        public SKColor SKColor { get; }
        public Color Color => System.Windows.Media.Color.FromRgb(SKColor.Red, SKColor.Green, SKColor.Blue);

        public ColorWrapper(SKColor skColor)
        {
            SKColor = skColor;
        }
    }
}