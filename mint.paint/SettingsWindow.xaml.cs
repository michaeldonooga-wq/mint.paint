using SkiaSharp;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System;

namespace mint.paint
{
    public partial class SettingsWindow : Window
    {
        private DrawingManager drawingManager;

        public SettingsWindow(DrawingManager drawingManager)
        {
            InitializeComponent();
            this.drawingManager = drawingManager;
            LoadCustomPalettes();
            
            UndoStepsSlider.Value = drawingManager.GetUndoRedoManager().MaxUndoSteps;
            UndoStepsText.Text = UndoStepsSlider.Value.ToString();
            UndoStepsSlider.ValueChanged += (s, e) => UndoStepsText.Text = ((int)e.NewValue).ToString();
            
            var currentLang = Properties.Settings.Default.Language ?? "ru";
            LanguageComboBox.SelectedIndex = currentLang == "en" ? 1 : 0;
            
            AutoUpdateCheckBox.IsChecked = Properties.Settings.Default.AutoUpdate;
            
            LocalizeUI();
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            Title = S("SettingsTitle");
            AppSettingsLabel.Text = S("AppSettings");
            PaletteTab.Header = S("Palette");
            SpecialTab.Header = S("Special");
            CustomPalettesLabel.Text = S("CustomPalettes");
            AddButton.Content = S("Add");
            RemoveButton.Content = S("Remove");
            LanguageLabel.Text = S("Language");
            UndoStepsLabel.Text = S("UndoSteps");
            AutoUpdateCheckBox.Content = S("AutoUpdate");
            ApplyButton.Content = S("Apply");
            CloseButton.Content = S("Close");
        }

        private void LoadCustomPalettes()
        {
            CustomPalettesList.Items.Clear();
            var palettes = drawingManager.GetCustomPalettes();
            foreach (var name in palettes.Keys)
            {
                CustomPalettesList.Items.Add(name);
            }
        }



        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.GetUndoRedoManager().MaxUndoSteps = (int)UndoStepsSlider.Value;
            Properties.Settings.Default.AutoUpdate = AutoUpdateCheckBox.IsChecked == true;
            
            var langItem = LanguageComboBox.SelectedItem as ComboBoxItem;
            var lang = langItem?.Tag?.ToString() ?? "ru";
            var oldLang = mint.paint.Properties.Settings.Default.Language ?? "ru";
            
            if (lang != oldLang)
            {
                mint.paint.Properties.Settings.Default.Language = lang;
                mint.paint.Properties.Settings.Default.Save();
                var S = Localization.Strings.Get;
                MessageBox.Show(S("LanguageChanged"), S("Restart"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var S = Localization.Strings.Get;
                MessageBox.Show(S("SettingsApplied"), S("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            mint.paint.Properties.Settings.Default.MaxUndoSteps = (int)UndoStepsSlider.Value;
            mint.paint.Properties.Settings.Default.AutoUpdate = AutoUpdateCheckBox.IsChecked == true;
            mint.paint.Properties.Settings.Default.Save();
            this.Close();
        }

        private void AddPalette_Click(object sender, RoutedEventArgs e)
        {
            var editor = new PaletteEditorWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                drawingManager.SaveCustomPalette(editor.PaletteName, editor.Colors);
                LoadCustomPalettes();
            }
        }

        private void RemovePalette_Click(object sender, RoutedEventArgs e)
        {
            if (CustomPalettesList.SelectedItem is string name)
            {
                var palettes = drawingManager.GetCustomPalettes();
                if (palettes.ContainsKey(name))
                {
                    drawingManager.GetColorPicker().RemoveCustomPalette(name);
                    LoadCustomPalettes();
                }
            }
        }
    }
}
