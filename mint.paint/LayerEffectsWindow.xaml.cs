using SkiaSharp;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace mint.paint
{
    public partial class LayerEffectsWindow : Window
    {
        private LayerEffects effects;
        private SKColor shadowColor;
        private SKColor glowColor;

        public LayerEffectsWindow(LayerEffects effects)
        {
            InitializeComponent();
            this.effects = effects;

            DropShadowEnabled.IsChecked = effects.DropShadowEnabled;
            shadowColor = effects.DropShadowColor;
            ShadowColorBox.Background = new SolidColorBrush(Color.FromArgb(shadowColor.Alpha, shadowColor.Red, shadowColor.Green, shadowColor.Blue));
            ShadowOffsetX.Value = effects.DropShadowOffsetX;
            ShadowOffsetY.Value = effects.DropShadowOffsetY;
            ShadowBlur.Value = effects.DropShadowBlur;

            OuterGlowEnabled.IsChecked = effects.OuterGlowEnabled;
            glowColor = effects.OuterGlowColor;
            GlowColorBox.Background = new SolidColorBrush(Color.FromArgb(glowColor.Alpha, glowColor.Red, glowColor.Green, glowColor.Blue));
            GlowSize.Value = effects.OuterGlowSize;

            BevelEnabled.IsChecked = effects.BevelEnabled;
            BevelTypeCombo.SelectedIndex = effects.BevelType == BevelType.Emboss ? 0 : 1;
            BevelDepth.Value = effects.BevelDepth;
            
            LocalizeUI();
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            Title = S("LayerEffectsTitle");
            ShadowGroup.Header = S("Shadow");
            DropShadowEnabled.Content = S("Enable");
            ShadowColorLabel.Text = S("Color");
            ShadowOffsetXLabel.Text = S("OffsetX");
            ShadowOffsetYLabel.Text = S("OffsetY");
            ShadowBlurLabel.Text = S("Blur");
            GlowGroup.Header = S("OuterGlow");
            OuterGlowEnabled.Content = S("Enable");
            GlowColorLabel.Text = S("Color");
            GlowSizeLabel.Text = S("Size");
            BevelGroup.Header = S("Bevel3D");
            BevelEnabled.Content = S("Enable");
            BevelTypeLabel.Text = S("Type");
            EmbossItem.Content = S("Emboss");
            EngraveItem.Content = S("Engrave");
            BevelDepthLabel.Text = S("Depth");
            OKButton.Content = S("OK");
            CancelButton.Content = S("Cancel");
        }

        private void ShadowColorBox_Click(object sender, MouseButtonEventArgs e)
        {
            var picker = new ColorPickerWindow(this);
            picker.SetInitialColor(Color.FromArgb(shadowColor.Alpha, shadowColor.Red, shadowColor.Green, shadowColor.Blue));
            picker.ColorChanged += (color) =>
            {
                shadowColor = new SKColor(color.R, color.G, color.B, color.A);
                ShadowColorBox.Background = new SolidColorBrush(color);
            };
            picker.ShowDialog();
        }

        private void GlowColorBox_Click(object sender, MouseButtonEventArgs e)
        {
            var picker = new ColorPickerWindow(this);
            picker.SetInitialColor(Color.FromArgb(glowColor.Alpha, glowColor.Red, glowColor.Green, glowColor.Blue));
            picker.ColorChanged += (color) =>
            {
                glowColor = new SKColor(color.R, color.G, color.B, color.A);
                GlowColorBox.Background = new SolidColorBrush(color);
            };
            picker.ShowDialog();
        }



        private void OK_Click(object sender, RoutedEventArgs e)
        {
            effects.DropShadowEnabled = DropShadowEnabled.IsChecked == true;
            effects.DropShadowColor = shadowColor;
            effects.DropShadowOffsetX = (float)ShadowOffsetX.Value;
            effects.DropShadowOffsetY = (float)ShadowOffsetY.Value;
            effects.DropShadowBlur = (float)ShadowBlur.Value;

            effects.OuterGlowEnabled = OuterGlowEnabled.IsChecked == true;
            effects.OuterGlowColor = glowColor;
            effects.OuterGlowSize = (float)GlowSize.Value;

            effects.BevelEnabled = BevelEnabled.IsChecked == true;
            effects.BevelType = BevelTypeCombo.SelectedIndex == 0 ? BevelType.Emboss : BevelType.Engrave;
            effects.BevelDepth = (float)BevelDepth.Value;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
