using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace mint.paint
{
    public partial class TextOverlay : UserControl
    {
        public event Action<string, string, double, bool, bool, bool, bool, SKColor> TextApplied;
        public event Action TextCancelled;
        public event Action ColorChanged;
        public event Action<double, double> TextMoved;

        private SKColor currentColor = SKColors.Black;
        private bool isMoving = false;

        public TextOverlay()
        {
            InitializeComponent();
            InitializeFonts();
            InitializeSizes();
        }

        private void InitializeFonts()
        {
            var fonts = new List<string>
            {
                "Arial", "Times New Roman", "Courier New", "Verdana", "Tahoma",
                "Georgia", "Comic Sans MS", "Impact", "Calibri", "Consolas"
            };
            fonts.AddRange(PresetsWindow.CustomFonts);
            FontCombo.ItemsSource = fonts;
            FontCombo.SelectedIndex = 0;
        }

        private void InitializeSizes()
        {
            var sizes = new List<string> { "8", "10", "12", "14", "16", "18", "20", "24", "28", "32", "36", "48", "72", "Пользовательский..." };
            SizeCombo.ItemsSource = sizes;
            SizeCombo.Text = "24";
        }

        public void Show(Point position, double width, double height, SKColor color)
        {
            InitializeFonts();
            currentColor = color;
            ColorBox.Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue));
            
            TextBorder.Width = width;
            TextBorder.Height = height;
            TextBorder.Visibility = Visibility.Visible;
            
            TextInput.Text = "";
            TextInput.AcceptsReturn = true;
            TextInput.Focus();
            UpdateTextStyle();
        }

        void TextInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Cancel_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    int caretIndex = TextInput.CaretIndex;
                    TextInput.Text = TextInput.Text.Insert(caretIndex, "\n");
                    TextInput.CaretIndex = caretIndex + 1;
                    e.Handled = true;
                }
                else
                {
                    Apply_Click(null, null);
                    e.Handled = true;
                }
            }
        }

        public void Hide()
        {
            TextBorder.Visibility = Visibility.Collapsed;
        }

        void Font_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateTextStyle();
        }

        void Size_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (SizeCombo.SelectedItem is string size && size == "Пользовательский...")
            {
                var dialog = new Window
                {
                    Title = "Пользовательский размер",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(10) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock { Text = "Размер (px):", Margin = new Thickness(0, 0, 0, 5) };
                Grid.SetRow(label, 0);

                var textBox = new TextBox { Text = SizeCombo.Text, Margin = new Thickness(0, 0, 0, 10) };
                Grid.SetRow(textBox, 1);

                var okButton = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetRow(okButton, 2);
                okButton.Click += (s, args) => { dialog.DialogResult = true; dialog.Close(); };

                grid.Children.Add(label);
                grid.Children.Add(textBox);
                grid.Children.Add(okButton);
                dialog.Content = grid;

                if (dialog.ShowDialog() == true && double.TryParse(textBox.Text, out double customSize) && customSize > 0)
                {
                    SizeCombo.Text = customSize.ToString();
                }
                else
                {
                    SizeCombo.Text = "24";
                }
            }
            UpdateTextStyle();
        }

        void Bold_Click(object sender, RoutedEventArgs e)
        {
            UpdateTextStyle();
        }

        void Italic_Click(object sender, RoutedEventArgs e)
        {
            UpdateTextStyle();
        }

        void Underline_Click(object sender, RoutedEventArgs e)
        {
            UpdateTextStyle();
        }

        void Strikethrough_Click(object sender, RoutedEventArgs e)
        {
            UpdateTextStyle();
        }

        void Color_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        public void UpdateColor(SKColor color)
        {
            currentColor = color;
            ColorBox.Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue));
            UpdateTextStyle();
        }

        void UpdateTextStyle()
        {
            if (TextInput == null || FontCombo.SelectedItem == null) return;

            var fontName = FontCombo.SelectedItem.ToString();
            TextInput.FontFamily = PresetsWindow.CustomFontFamilies.ContainsKey(fontName)
                ? PresetsWindow.CustomFontFamilies[fontName]
                : new FontFamily(fontName);
            
            if (double.TryParse(SizeCombo.Text, out double size))
                TextInput.FontSize = size;

            TextInput.FontWeight = BoldBtn?.IsChecked == true ? FontWeights.Bold : FontWeights.Normal;
            TextInput.FontStyle = ItalicBtn?.IsChecked == true ? FontStyles.Italic : FontStyles.Normal;
            
            var decorations = new TextDecorationCollection();
            if (UnderlineBtn?.IsChecked == true)
                decorations.Add(TextDecorations.Underline[0]);
            if (StrikethroughBtn?.IsChecked == true)
                decorations.Add(TextDecorations.Strikethrough[0]);
            TextInput.TextDecorations = decorations.Count > 0 ? decorations : null;
            TextInput.Foreground = new SolidColorBrush(Color.FromRgb(currentColor.Red, currentColor.Green, currentColor.Blue));
            TextInput.TextAlignment = TextAlignment.Left;
            TextInput.Opacity = 1.0;
            
            var indicator = "";
            if (UnderlineBtn?.IsChecked == true) indicator += "U ";
            if (StrikethroughBtn?.IsChecked == true) indicator += "S";
            StyleIndicator.Text = indicator.Trim();
        }

        void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextInput.Text))
            {
                Hide();
                TextCancelled?.Invoke();
                return;
            }

            var font = FontCombo.SelectedItem?.ToString() ?? "Arial";
            double.TryParse(SizeCombo.Text, out double size);
            if (size < 8) size = 24;

            TextApplied?.Invoke(
                TextInput.Text,
                font,
                size,
                BoldBtn.IsChecked == true,
                ItalicBtn.IsChecked == true,
                UnderlineBtn.IsChecked == true,
                StrikethroughBtn.IsChecked == true,
                currentColor
            );

            Hide();
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            TextCancelled?.Invoke();
        }

        void Move_Click(object sender, RoutedEventArgs e)
        {
            isMoving = !isMoving;
            MoveBtn.Background = isMoving ? System.Windows.Media.Brushes.LightBlue : null;
        }

        public bool IsMoving => isMoving;

        public void MoveBy(double dx, double dy)
        {
            TextMoved?.Invoke(dx, dy);
        }
    }
}
