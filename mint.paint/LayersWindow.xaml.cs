using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace mint.paint
{
    public partial class LayersWindow : Window
    {
        private DrawingManager drawingManager;
        private bool showInactiveLayers = false;
        private bool autoLayerOnText = false;

        public LayersWindow(DrawingManager drawingManager)
        {
            InitializeComponent();
            this.drawingManager = drawingManager;
            UpdateLayersList();

            // Подписываемся на изменения слоев
            drawingManager.LayersChanged += UpdateLayersList;
            
            LocalizeUI();
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            Title = S("LayersTitle");
            AddLayerButton.ToolTip = S("AddLayer");
            RemoveLayerButton.ToolTip = S("RemoveLayer");
            MergeLayersButton.ToolTip = S("MergeLayers");
            LayerMenuButton.ToolTip = S("Options");
            ShowInactiveLayersMenu.Header = S("ShowInactiveLayers");
            InactiveOpacityMenu.Header = S("InactiveOpacity");
            AutoLayerTextMenu.Header = S("AutoLayerText");
            MergeSameColorsMenu.Header = S("MergeSameColors");
            OpacityLabel.Text = S("Opacity");
            MoveUpButton.Content = S("MoveUp");
            MoveDownButton.Content = S("MoveDown");
            LayerEffectsButton.Content = S("LayerEffects");
        }

        private void UpdateLayersList()
        {
            var layerManager = drawingManager.GetLayerManager();
            if (layerManager == null) return;

            LayersListBox.ItemsSource = null;

            var layersList = layerManager.Layers.ToList();
            layersList.Reverse();
            LayersListBox.ItemsSource = layersList;

            if (layerManager.ActiveLayer != null)
            {
                LayersListBox.SelectedItem = layerManager.ActiveLayer;
            }
        }

        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.CreateNewLayer();
        }

        private void RemoveLayerButton_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.RemoveActiveLayer();
        }

        private void MergeLayersButton_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.MergeVisibleLayers();
        }

        private void LayersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayersListBox.SelectedItem is Layer selectedLayer)
            {
                var layerManager = drawingManager.GetLayerManager();

                // Используем ToList() для поиска индекса
                var layersList = layerManager.Layers.ToList();
                int index = layersList.IndexOf(selectedLayer);
                drawingManager.SetActiveLayer(index);

                // Обновляем слайдер непрозрачности
                OpacitySlider.Value = selectedLayer.Opacity * 100;
                
                // Анимация прокрутки к выбранному элементу
                LayersListBox.ScrollIntoView(selectedLayer);
            }
        }

        private void VisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (drawingManager != null)
            {
                drawingManager.InvalidateCanvas();
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LayersListBox.SelectedItem is Layer selectedLayer)
            {
                selectedLayer.Opacity = (float)(e.NewValue / 100.0);
                if (drawingManager != null)
                {
                    drawingManager.InvalidateCanvas();
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (LayersListBox.SelectedItem is Layer selectedLayer)
            {
                var layerManager = drawingManager.GetLayerManager();

                // Используем ToList() для поиска индекса
                var layersList = layerManager.Layers.ToList();
                int index = layersList.IndexOf(selectedLayer);
                drawingManager.MoveLayerUp(index);
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (LayersListBox.SelectedItem is Layer selectedLayer)
            {
                var layerManager = drawingManager.GetLayerManager();

                // Используем ToList() для поиска индекса
                var layersList = layerManager.Layers.ToList();
                int index = layersList.IndexOf(selectedLayer);
                drawingManager.MoveLayerDown(index);
            }
        }

        private void LayerItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Обработка двойного клика на весь элемент
            if (e.ClickCount == 2 && sender is Border border && border.DataContext is Layer layer)
            {
                RenameLayer(layer);
            }
        }

        private void LayerName_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Обработка двойного клика на название слоя
            if (e.ClickCount == 2 && sender is TextBlock textBlock && textBlock.DataContext is Layer layer)
            {
                RenameLayer(layer);
            }
        }

        private void RenameLayer(Layer layer)
        {
            // Простой диалог для переименования
            var dialog = new RenameDialog(layer.Name);
            if (dialog.ShowDialog() == true)
            {
                layer.Name = dialog.NewName;
                UpdateLayersList();
            }
        }

        private void LayerMenuButton_Click(object sender, RoutedEventArgs e)
        {
            LayerMenuButton.ContextMenu.IsOpen = true;
        }
        
        private void ShowInactiveLayers_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                showInactiveLayers = menuItem.IsChecked;
                var layerManager = drawingManager.GetLayerManager();
                if (layerManager != null)
                {
                    layerManager.ShowInactiveLayers = showInactiveLayers;
                    drawingManager.InvalidateCanvas();
                }
                UpdateLayersList();
            }
        }
        
        private void InactiveOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var layerManager = drawingManager?.GetLayerManager();
            if (layerManager != null)
            {
                layerManager.InactiveLayerOpacity = (float)(e.NewValue / 100.0);
                drawingManager.InvalidateCanvas();
            }
        }
        
        private void AutoLayerOnText_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                autoLayerOnText = menuItem.IsChecked;
            }
        }
        
        private void MergeSameColors_Click(object sender, RoutedEventArgs e)
        {
            var layerManager = drawingManager.GetLayerManager();
            if (layerManager == null || layerManager.Layers.Count < 2) return;
            
            var colorGroups = new Dictionary<uint, List<Layer>>();
            
            foreach (var layer in layerManager.Layers.ToList())
            {
                if (!layer.IsVisible) continue;
                
                var bitmap = layer.Bitmap;
                uint dominantColor = 0;
                var colorCounts = new Dictionary<uint, int>();
                
                for (int y = 0; y < bitmap.Height; y += 10)
                {
                    for (int x = 0; x < bitmap.Width; x += 10)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        if (pixel.Alpha < 128) continue;
                        
                        uint color = ((uint)pixel.Red << 16) | ((uint)pixel.Green << 8) | pixel.Blue;
                        colorCounts[color] = colorCounts.GetValueOrDefault(color) + 1;
                    }
                }
                
                if (colorCounts.Count > 0)
                {
                    dominantColor = colorCounts.OrderByDescending(c => c.Value).First().Key;
                    if (!colorGroups.ContainsKey(dominantColor))
                        colorGroups[dominantColor] = new List<Layer>();
                    colorGroups[dominantColor].Add(layer);
                }
            }
            
            int merged = 0;
            foreach (var group in colorGroups.Values.Where(g => g.Count > 1))
            {
                var firstLayer = group[0];
                using var canvas = new SkiaSharp.SKCanvas(firstLayer.Bitmap);
                
                for (int i = 1; i < group.Count; i++)
                {
                    canvas.DrawBitmap(group[i].Bitmap, 0, 0);
                    var index = layerManager.Layers.ToList().IndexOf(group[i]);
                    layerManager.RemoveLayer(index);
                    merged++;
                }
            }
            
            if (merged > 0)
            {
                drawingManager.InvalidateCanvas();
                var S = Localization.Strings.Get;
                MessageBox.Show(string.Format(S("MergedLayers"), merged), S("Done"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var S = Localization.Strings.Get;
                MessageBox.Show(S("NoSameColors"), S("Info"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        public bool GetAutoLayerOnText()
        {
            return autoLayerOnText;
        }

        private void LayerEffectsButton_Click(object sender, RoutedEventArgs e)
        {
            if (LayersListBox.SelectedItem is Layer selectedLayer)
            {
                var effectsWindow = new LayerEffectsWindow(selectedLayer.Effects) { Owner = this };
                if (effectsWindow.ShowDialog() == true)
                {
                    drawingManager.InvalidateCanvas();
                }
            }
        }
        
        protected override void OnClosed(System.EventArgs e)
        {
            if (drawingManager != null)
            {
                drawingManager.LayersChanged -= UpdateLayersList;
            }
            base.OnClosed(e);
        }
    }

    // Простой диалог для переименования
    public class RenameDialog : Window
    {
        public string NewName { get; private set; }

        public RenameDialog(string currentName)
        {
            var S = Localization.Strings.Get;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = S("RenameLayer");

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            stackPanel.Children.Add(new TextBlock { Text = S("NewName"), Margin = new Thickness(0, 0, 0, 10) });

            var textBox = new TextBox { Text = currentName, Margin = new Thickness(0, 0, 0, 20) };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var okButton = new Button { Content = S("OK"), Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            okButton.Click += (s, e) =>
            {
                NewName = textBox.Text;
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

            // Устанавливаем фокус на текстовое поле
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}