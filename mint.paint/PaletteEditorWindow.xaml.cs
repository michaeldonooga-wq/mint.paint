using SkiaSharp;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace mint.paint
{
    public partial class PaletteEditorWindow : Window
    {
        private List<SKColor> colors = new List<SKColor>();
        private Border selectedBorder = null;

        public string PaletteName => PaletteNameBox.Text;
        public List<SKColor> Colors => colors;

        public PaletteEditorWindow()
        {
            InitializeComponent();
            LocalizeUI();
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            Title = S("PaletteEditorTitle");
            PaletteNameLabel.Text = S("PaletteName");
            PaletteNameBox.Text = S("MyPalette");
            AddColorButton.Content = S("AddColor");
            RemoveColorButton.Content = S("Remove");
            SaveButton.Content = S("Save");
            CancelButton.Content = S("Cancel");
        }

        private void AddColor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = new SKColor(dialog.Color.R, dialog.Color.G, dialog.Color.B);
                colors.Add(color);
                AddColorToPanel(color);
            }
        }

        private void AddColorToPanel(SKColor color)
        {
            var border = new Border
            {
                Width = 40,
                Height = 40,
                Margin = new Thickness(5, 5, 5, 5),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(2, 2, 2, 2),
                Background = new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue)),
                Cursor = Cursors.Hand,
                Tag = color
            };

            border.MouseDown += ColorBorder_MouseDown;
            ColorsPanel.Children.Add(border);
        }

        private void ColorBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedBorder != null)
            {
                selectedBorder.BorderBrush = Brushes.Gray;
            }

            selectedBorder = sender as Border;
            if (selectedBorder != null)
            {
                selectedBorder.BorderBrush = Brushes.Blue;
            }
        }

        private void RemoveColor_Click(object sender, RoutedEventArgs e)
        {
            if (selectedBorder != null && selectedBorder.Tag is SKColor color)
            {
                colors.Remove(color);
                ColorsPanel.Children.Remove(selectedBorder);
                selectedBorder = null;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PaletteNameBox.Text))
            {
                var S = Localization.Strings.Get;
                MessageBox.Show(S("EnterPaletteName"), S("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (colors.Count == 0)
            {
                var S = Localization.Strings.Get;
                MessageBox.Show(S("AddOneColor"), S("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
