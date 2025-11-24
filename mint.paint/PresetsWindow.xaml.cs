using mint.paint;
using mint.paint.Localization;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace mint.paint
{
    public partial class PresetsWindow : Window
    {
        public event Action<BrushPreset> BrushPresetSelected;
        public event Action<TextPreset> TextPresetSelected;

        private List<BrushPreset> customBrushes = new List<BrushPreset>();
        private TextPreset currentTextPreset = new TextPreset();

        public PresetsWindow()
        {
            InitializeComponent();
            InitializeFonts();
            InitializePresets();
            UpdateTextPreview();
            LocalizeUI();
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            Title = S("PresetsTitle");
            BrushesTab.Header = S("Brushes");
            FontsTab.Header = S("Fonts");
            BrushPresetsLabel.Text = S("BrushPresets");
            BrushesGroup.Header = S("Brushes");
            CustomBrushesGroup.Header = S("CustomBrushes");
            CreatePresetButton.Content = S("CreatePreset");
            TextSettingsLabel.Text = S("TextSettings");
            FontGroup.Header = S("FontFamily");
            LoadFontButton.Content = S("LoadFont");
            BoldCheckBox.Content = S("Bold");
            ItalicCheckBox.Content = S("Italic");
            TextSizeGroup.Header = S("TextSize");
            AlignmentGroup.Header = S("Alignment");
            AlignLeftRadio.Content = S("AlignLeft");
            AlignCenterRadio.Content = S("AlignCenter");
            AlignRightRadio.Content = S("AlignRight");
            ApplyButton.Content = S("Apply");
            CancelButton.Content = S("Cancel");
            TextPreview.Text = S("TextPreview");
            WatercolorButton.Content = S("Watercolor");
            NormalButton.Content = S("Normal");
            PencilButton.Content = S("Pencil");
            PixelButton.Content = S("Pixel");
            DropsButton.Content = S("Drops");
        }

        public static List<string> CustomFonts { get; } = new List<string>();
        public static Dictionary<string, FontFamily> CustomFontFamilies { get; } = new Dictionary<string, FontFamily>();

        private void InitializeFonts()
        {
            // Заполняем список шрифтов
            var fonts = new List<string>
            {
                "Arial", "Times New Roman", "Courier New", "Verdana",
                "Tahoma", "Georgia", "Comic Sans MS", "Impact",
                "Trebuchet MS", "Palatino Linotype", "Lucida Console"
            };

            fonts.AddRange(CustomFonts);
            FontFamilyComboBox.ItemsSource = fonts;
            FontFamilyComboBox.SelectedItem = "Arial";
        }

        private void InitializePresets()
        {
            var S = Localization.Strings.Get;
            // Предустановленные кисти
            var defaultBrushes = new List<BrushPreset>
            {
                new BrushPreset { Name = S("SoftRound"), Size = 10, Hardness = 30, Type = BrushType.SoftRound },
                new BrushPreset { Name = S("HardRound"), Size = 8, Hardness = 100, Type = BrushType.HardRound },
                new BrushPreset { Name = S("Pencil").Replace("✏️ ", ""), Size = 1, Hardness = 100, Type = BrushType.Pencil },
                new BrushPreset { Name = S("Watercolor").Replace("💧 ", ""), Size = 15, Hardness = 20, Type = BrushType.Watercolor },
                new BrushPreset { Name = S("Oil"), Size = 12, Hardness = 80, Type = BrushType.Oil }
            };

            CustomBrushesListBox.ItemsSource = defaultBrushes.Concat(customBrushes);
        }

        private void BrushPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string presetTag)
            {
                var preset = CreateBrushPresetFromTag(presetTag);
                BrushPresetSelected?.Invoke(preset);
                this.Close();
            }
        }

        private BrushPreset CreateBrushPresetFromTag(string tag)
        {
            return tag switch
            {
                "soft_round" => new BrushPreset { Name = "Мягкая круглая", Size = 10, Hardness = 30, Type = BrushType.SoftRound },
                "hard_round" => new BrushPreset { Name = "Жесткая круглая", Size = 8, Hardness = 100, Type = BrushType.HardRound },
                "pencil" => new BrushPreset { Name = "Карандаш", Size = 1, Hardness = 100, Type = BrushType.Pencil },
                "watercolor" => new BrushPreset { Name = "Акварель", Size = 15, Hardness = 20, Type = BrushType.Watercolor },
                "oil" => new BrushPreset { Name = "Масло", Size = 12, Hardness = 80, Type = BrushType.Oil },
                "grass" => new BrushPreset { Name = "Трава", Size = 20, Hardness = 50, Type = BrushType.Textured },
                "stars" => new BrushPreset { Name = "Звезды", Size = 25, Hardness = 100, Type = BrushType.Textured },
                "snow" => new BrushPreset { Name = "Снег", Size = 18, Hardness = 30, Type = BrushType.Textured },
                "spray" => new BrushPreset { Name = "Брызги", Size = 22, Hardness = 40, Type = BrushType.Textured },
                "pixel" => new BrushPreset { Name = "Пиксели", Size = 1, Hardness = 100, Type = BrushType.Pixel },
                "drops" => new BrushPreset { Name = "Капли", Size = 15, Hardness = 50, Type = BrushType.Drops },
                "normal" => new BrushPreset { Name = "Обычная", Size = 5, Hardness = 100, Type = BrushType.SoftRound },
                _ => new BrushPreset { Name = "Кисть по умолчанию", Size = 5, Hardness = 100, Type = BrushType.SoftRound }
            };
        }

        private void CreatePreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreatePresetDialog();
            if (dialog.ShowDialog() == true)
            {
                customBrushes.Add(dialog.NewPreset);
                InitializePresets(); // Обновляем список
            }
        }

        private void FontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontFamilyComboBox.SelectedItem is string fontFamily)
            {
                currentTextPreset.FontFamily = fontFamily;
                UpdateTextPreview();
            }
        }

        private void FontSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FontSizeText == null) return;
            
            currentTextPreset.FontSize = (float)e.NewValue;
            FontSizeText.Text = $"{e.NewValue:0}px";
            UpdateTextPreview();
        }

        private void FontStyle_Changed(object sender, RoutedEventArgs e)
        {
            if (BoldCheckBox == null || ItalicCheckBox == null) return;
            
            currentTextPreset.IsBold = BoldCheckBox.IsChecked == true;
            currentTextPreset.IsItalic = ItalicCheckBox.IsChecked == true;
            UpdateTextPreview();
        }

        private void TextAlign_Changed(object sender, RoutedEventArgs e)
        {
            if (AlignLeftRadio.IsChecked == true)
                currentTextPreset.TextAlign = TextAlign.Left;
            else if (AlignCenterRadio.IsChecked == true)
                currentTextPreset.TextAlign = TextAlign.Center;
            else if (AlignRightRadio.IsChecked == true)
                currentTextPreset.TextAlign = TextAlign.Right;

            UpdateTextPreview();
        }

        private void UpdateTextPreview()
        {
            if (TextPreview == null) return;
            
            TextPreview.FontFamily = CustomFontFamilies.ContainsKey(currentTextPreset.FontFamily)
                ? CustomFontFamilies[currentTextPreset.FontFamily]
                : new FontFamily(currentTextPreset.FontFamily);
            TextPreview.FontSize = currentTextPreset.FontSize;
            TextPreview.FontWeight = currentTextPreset.IsBold ? FontWeights.Bold : FontWeights.Normal;
            TextPreview.FontStyle = currentTextPreset.IsItalic ? FontStyles.Italic : FontStyles.Normal;

            TextPreview.HorizontalAlignment = currentTextPreset.TextAlign switch
            {
                TextAlign.Left => HorizontalAlignment.Left,
                TextAlign.Center => HorizontalAlignment.Center,
                TextAlign.Right => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left
            };
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            TextPresetSelected?.Invoke(currentTextPreset);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadFont_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Шрифты (*.ttf;*.otf)|*.ttf;*.otf",
                Title = "Загрузить шрифт"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var fontUri = new Uri(dialog.FileName);
                    var glyphTypeface = new GlyphTypeface(fontUri);
                    var fontName = glyphTypeface.Win32FamilyNames.ContainsKey(System.Globalization.CultureInfo.CurrentUICulture)
                        ? glyphTypeface.Win32FamilyNames[System.Globalization.CultureInfo.CurrentUICulture]
                        : glyphTypeface.Win32FamilyNames.Values.FirstOrDefault() ?? System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    
                    if (!CustomFonts.Contains(fontName))
                    {
                        var baseUri = new Uri(System.IO.Path.GetDirectoryName(dialog.FileName) + "\\");
                        var fontFamily = new FontFamily(baseUri, "./#" + fontName);
                        CustomFonts.Add(fontName);
                        CustomFontFamilies[fontName] = fontFamily;
                        InitializeFonts();
                        FontFamilyComboBox.SelectedItem = fontName;
                        var S = Localization.Strings.Get;
                        MessageBox.Show(string.Format(S("FontLoaded"), fontName), S("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    var S = Localization.Strings.Get;
                    MessageBox.Show(string.Format(S("FontLoadError"), ex.Message), S("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class BrushPreset
    {
        public string Name { get; set; } = "Новая кисть";
        public double Size { get; set; } = 5;
        public double Hardness { get; set; } = 100;
        public BrushType Type { get; set; } = BrushType.SoftRound;
        public SKColor Color { get; set; } = SKColors.Black;
    }

    public class TextPreset
    {
        public string FontFamily { get; set; } = "Arial";
        public float FontSize { get; set; } = 24f;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public TextAlign TextAlign { get; set; } = TextAlign.Left;
        public SKColor TextColor { get; set; } = SKColors.Black;
    }

    public enum BrushType
    {
        SoftRound,
        HardRound,
        Pencil,
        Watercolor,
        Oil,
        Textured,
        Pixel,
        Drops
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right
    }
}

// Диалог создания пресета
public class CreatePresetDialog : Window
{
    public BrushPreset NewPreset { get; private set; } = new BrushPreset();

    public CreatePresetDialog()
    {
        var S = mint.paint.Localization.Strings.Get;
        Width = 300;
        Height = 250;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Title = S("CreateBrushPreset");

        var stackPanel = new StackPanel { Margin = new Thickness(20) };

        // Название
        stackPanel.Children.Add(new TextBlock { Text = S("Name"), Margin = new Thickness(0, 0, 0, 5) });
        var nameBox = new TextBox { Text = S("NewBrush"), Margin = new Thickness(0, 0, 0, 10) };
        stackPanel.Children.Add(nameBox);

        // Размер
        stackPanel.Children.Add(new TextBlock { Text = S("Size"), Margin = new Thickness(0, 0, 0, 5) });
        var sizeSlider = new Slider { Minimum = 1, Maximum = 100, Value = 5, Margin = new Thickness(0, 0, 0, 10) };
        stackPanel.Children.Add(sizeSlider);

        // Жесткость
        stackPanel.Children.Add(new TextBlock { Text = S("Hardness"), Margin = new Thickness(0, 0, 0, 5) });
        var hardnessSlider = new Slider { Minimum = 0, Maximum = 100, Value = 100, Margin = new Thickness(0, 0, 0, 20) };
        stackPanel.Children.Add(hardnessSlider);

        // Кнопки
        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var okButton = new Button { Content = S("Create"), Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        okButton.Click += (s, e) =>
        {
            NewPreset.Name = nameBox.Text;
            NewPreset.Size = sizeSlider.Value;
            NewPreset.Hardness = hardnessSlider.Value;
            DialogResult = true;
            Close();
        };

        var cancelButton = new Button { Content = S("Cancel"), Width = 80 };
        cancelButton.Click += (s, e) =>
        {
            DialogResult = false;
            Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        stackPanel.Children.Add(buttonPanel);

        Content = stackPanel;
    }
}