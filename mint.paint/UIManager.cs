using SkiaSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace mint.paint
{
    public class UIManager
    {
        private Window mainWindow;
        private DrawingManager drawingManager;

        // UI элементы
        private TextBlock statusText;
        private TextBlock coordinatesText;
        private TextBlock toolText;
        private TextBlock brushInfoText;

        // Окна
        private ColorPickerWindow colorPickerWindow;
        private ToolsWindow toolsWindow;
        private ToolsPanel toolsPanelControl;
        private LayersWindow layersWindow;
        private AdvancedColorPickerWindow advancedColorPickerWindow;
        private PresetsWindow presetsWindow;

        public UIManager(Window mainWindow, DrawingManager drawingManager,
                        TextBlock coordinatesText,
                        TextBlock toolText, TextBlock brushInfoText)
        {
            this.mainWindow = mainWindow;
            this.drawingManager = drawingManager;
            this.coordinatesText = coordinatesText;
            this.toolText = toolText;
            this.brushInfoText = brushInfoText;

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            drawingManager.StatusChanged += OnStatusChanged;
            drawingManager.CoordinatesChanged += OnCoordinatesChanged;
            drawingManager.ToolChanged += OnToolChanged;
        }

        public void InitializeToolsPanel()
        {
            toolsPanelControl = new ToolsPanel();
            toolsWindow = new ToolsWindow
            {
                Owner = mainWindow,
                Width = 70,
                Height = 550,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = false
            };

            toolsWindow.Content = toolsPanelControl;

            // Подписка на события
            toolsPanelControl.ToolChanged += drawingManager.SetTool;
            toolsPanelControl.ClearRequested += OnClearRequested;
            toolsPanelControl.PresetsRequested += () => ShowPresetsWindow();

            toolsWindow.Left = mainWindow.Left + 10;
            toolsWindow.Top = mainWindow.Top + 100;
            toolsWindow.Show();

        }

        private void OnStatusChanged(string status)
        {
        }

        private void OnCoordinatesChanged(string coordinates)
        {
            coordinatesText.Text = coordinates;
        }

        private void OnToolChanged(ToolsPanel.ToolType tool)
        {
            toolText.Text = tool switch
            {
                ToolsPanel.ToolType.Brush => Localization.Strings.Get("Tool_Brush"),
                ToolsPanel.ToolType.Eraser => Localization.Strings.Get("Tool_Eraser"),
                ToolsPanel.ToolType.Fill => Localization.Strings.Get("Tool_Fill"),
                ToolsPanel.ToolType.Eyedropper => Localization.Strings.Get("Tool_Eyedropper"),
                ToolsPanel.ToolType.Line => Localization.Strings.Get("Tool_Line"),
                ToolsPanel.ToolType.Rectangle => Localization.Strings.Get("Tool_Rectangle"),
                ToolsPanel.ToolType.Circle => Localization.Strings.Get("Tool_Circle"),
                ToolsPanel.ToolType.Selection => Localization.Strings.Get("Tool_Selection"),
                ToolsPanel.ToolType.Text => Localization.Strings.Get("Tool_Text"),
                _ => Localization.Strings.Get("Tool_Brush")
            };
        }

        private void OnClearRequested()
        {
            drawingManager.NewCanvas();
        }

        public void ShowColorPicker()
        {
            try
            {
                colorPickerWindow?.Close();
                colorPickerWindow = new ColorPickerWindow(mainWindow);

                colorPickerWindow.ColorChanged += (color) =>
                {
                    // Цвет устанавливается через DrawingManager
                    var skColor = new SKColor(color.R, color.G, color.B);
                    drawingManager.SetBrushColor(skColor);
                    UpdateBrushInfo();
                };

                // Синхронизируем текущий цвет кисти с палитрой
                var currentColor = drawingManager.GetCurrentBrushColor();
                colorPickerWindow.SetInitialColor(Color.FromRgb(currentColor.Red, currentColor.Green, currentColor.Blue));

                colorPickerWindow.Closed += (s, args) => colorPickerWindow = null;
                colorPickerWindow.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка открытия палитры: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowAdvancedColorPicker()
        {
            try
            {
                advancedColorPickerWindow?.Close();
                advancedColorPickerWindow = new AdvancedColorPickerWindow(drawingManager);
                advancedColorPickerWindow.Owner = mainWindow;
                advancedColorPickerWindow.ColorSelected += (color) =>
                {
                    UpdateBrushInfo();
                    if (mainWindow is MainWindow mw)
                    {
                        mw.UpdateColorPreviewsPublic();
                    }
                };
                advancedColorPickerWindow.Closed += (s, args) =>
                {
                    advancedColorPickerWindow = null;
                    mainWindow.Focus();
                };
                advancedColorPickerWindow.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка открытия расширенной палитры: {ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowLayersWindow()
        {
            try
            {
                layersWindow?.Close();
                layersWindow = new LayersWindow(drawingManager);
                layersWindow.Owner = mainWindow;
                layersWindow.Closed += (s, args) =>
                {
                    layersWindow = null;
                    mainWindow.Focus();
                };
                layersWindow.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна слоев: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ToggleToolsPanel()
        {
            if (toolsWindow == null)
            {
                InitializeToolsPanel();
            }
            else if (toolsWindow.IsVisible)
            {
                toolsWindow.Hide();
            }
            else
            {
                toolsWindow.Show();
            }
        }

        public void UpdateBrushInfo()
        {
            brushInfoText.Text = drawingManager.GetBrushInfo();
        }

        // Методы для работы с сеткой
        public void ToggleGrid()
        {
            drawingManager.ToggleGrid();
        }

        public void ToggleSnapToGrid()
        {
            drawingManager.ToggleSnapToGrid();
        }

        public void SetGridSize(float size)
        {
            drawingManager.SetGridSize(size);
        }

        // Методы для работы со слоями
        public void CreateNewLayer()
        {
            drawingManager.CreateNewLayer();
        }

        public void RemoveActiveLayer()
        {
            drawingManager.RemoveActiveLayer();
        }

        public void MergeVisibleLayers()
        {
            drawingManager.MergeVisibleLayers();
        }

        // Методы для работы с выделением
        public void CutSelection()
        {
            drawingManager.CutSelection();
        }

        public void CopySelection()
        {
            drawingManager.CopySelection();
        }

        public void PasteSelection()
        {
            drawingManager.PasteSelection();
        }

        public void DeleteSelection()
        {
            drawingManager.DeleteSelection();
        }

        public void ClearSelection()
        {
            drawingManager.ClearSelection();
        }

        // Методы для работы с цветами
        public void SwapColors()
        {
            drawingManager.SwapColors();
        }

        public void ShowSaveMessage()
        {
        }

        public void ShowOpenMessage()
        {
        }

        public void ShowPresetsWindow()
        {
            try
            {
                presetsWindow?.Close();
                presetsWindow = new PresetsWindow();
                presetsWindow.Owner = mainWindow;
                
                presetsWindow.BrushPresetSelected += (preset) =>
                {
                    drawingManager.SetBrushSize(preset.Size);
                    drawingManager.SetBrushHardness(preset.Hardness);
                    drawingManager.SetBrushType(preset.Type);
                    UpdateBrushInfo();
                    if (mainWindow is MainWindow mw)
                    {
                        mw.UpdateDropsPanel(preset.Type == BrushType.Drops);
                        mw.UpdateBrushSizeEnabled(preset.Type != BrushType.Pixel);
                    }
                };
                
                presetsWindow.Closed += (s, args) =>
                {
                    presetsWindow = null;
                    mainWindow.Focus();
                };
                presetsWindow.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна пресетов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool GetAutoLayerOnText()
        {
            return layersWindow?.GetAutoLayerOnText() ?? false;
        }
        
        public void Cleanup()
        {
            colorPickerWindow?.Close();
            toolsWindow?.Close();
            layersWindow?.Close();
            advancedColorPickerWindow?.Close();
            presetsWindow?.Close();
        }
    }
}