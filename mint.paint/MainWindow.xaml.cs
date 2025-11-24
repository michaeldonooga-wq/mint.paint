using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace mint.paint
{
    public partial class MainWindow : Window
    {
        private DrawingManager drawingManager;
        private ViewportManager viewportManager;
        private InputManager inputManager;
        private UIManager uiManager;

        private bool isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();

            // Сначала инициализируем менеджеры
            InitializeManagers();

            // Затем подписываемся на события
            InitializeEventHandlers();

            isInitialized = true;
        }

        private void InitializeManagers()
        {
            viewportManager = new ViewportManager(DrawingSurface);
            drawingManager = new DrawingManager(DrawingSurface, viewportManager);
            inputManager = new InputManager(drawingManager, viewportManager, this);
            uiManager = new UIManager(this, drawingManager, CoordinatesText, ToolText, BrushInfoText);
            
            drawingManager.TextAreaSelected += OnTextAreaSelected;
        }

        private void InitializeEventHandlers()
        {
            // Рисование
            DrawingSurface.PaintSurface += drawingManager.OnPaintSurface;
            DrawingSurface.MouseDown += inputManager.OnMouseDown;
            DrawingSurface.MouseMove += inputManager.OnMouseMove;
            DrawingSurface.MouseUp += inputManager.OnMouseUp;
            DrawingSurface.MouseWheel += inputManager.OnMouseWheel;
            
            // Текстовая панель
            InitializeTextPanel();

            // Горячие клавиши
            this.KeyDown += inputManager.OnKeyDown;
            this.KeyUp += inputManager.OnKeyUp;

            // События выделения
            drawingManager.SelectionChanged += OnSelectionChanged;

            // События слоев и сетки
            drawingManager.LayersChanged += UpdateLayerInfo;
            drawingManager.LayersChanged += UpdateGridInfo;
            drawingManager.ToolChanged += ShowShapesPanel;

            // Загрузка окна
            this.Loaded += MainWindow_Loaded;

            // Инициализируем начальные значения
            UpdateLayerInfo();
            UpdateGridInfo();
            
            GradientStyleComboBox.SelectionChanged += (s, e) =>
            {
                if (GradientStyleComboBox.SelectedItem is ComboBoxItem item)
                {
                    drawingManager.SetGradientStyle(item.Tag.ToString());
                }
            };
        }

        // ==================== PUBLIC МЕТОДЫ ДЛЯ INPUT MANAGER ====================

        public void PublicShowLayersWindow()
        {
            ShowLayersWindowMenuItem_Click(null, null);
        }

        public void PublicCreateNewLayer()
        {
            NewLayerMenuItem_Click(null, null);
        }

        public void PublicDeleteLayer()
        {
            DeleteLayerMenuItem_Click(null, null);
        }

        public void PublicMergeLayers()
        {
            MergeLayersMenuItem_Click(null, null);
        }

        public void PublicShowAdvancedColorPicker()
        {
            AdvancedColorPickerButton_Click(null, null);
        }

        public void PublicShowColorPicker()
        {
            AdvancedColorPickerButton_Click(null, null);
        }

        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            uiManager.InitializeToolsPanel();
            UpdateColorPreviews();
            DrawRulers();
            NavigatorMenuItem_Click(null, null);
            
            Dispatcher.BeginInvoke(new Action(() => LocalizeUI()), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        private void LocalizeUI()
        {
            var S = Localization.Strings.Get;
            
            // Menu headers
            FileMenu.Header = S("File");
            EditMenu.Header = S("Edit");
            ViewMenu.Header = S("View");
            ColorMenu.Header = S("Color");
            LayersMenu.Header = S("Layers");
            BrushMenu.Header = S("Brush");
            ShapesMenu.Header = S("Shapes");
            SettingsMenu.Header = S("Menu_Settings");
            
            // File menu
            NewMenuItem.Header = S("New");
            OpenMenuItem.Header = S("Open");
            SaveMenuItem.Header = S("Save");
            ResizeCanvasMenuItem.Header = S("ResizeCanvas");
            
            // Edit menu
            UndoMenuItem.Header = S("Undo");
            RedoMenuItem.Header = S("Redo");
            CutMenuItem.Header = S("Cut");
            CopyMenuItem.Header = S("Copy");
            PasteMenuItem.Header = S("Paste");
            DeleteMenuItem.Header = S("Delete");
            ClearSelectionMenuItem.Header = S("ClearSelection");
            
            // View menu
            ToggleToolsPanelMenuItem.Header = S("HideTools");
            ToggleRulersMenuItem.Header = S("ShowRulers");
            NavigatorMenuItem.Header = S("Navigator");
            ToggleGridMenuItem.Header = S("ShowGrid");
            ToggleSnapToGridMenuItem.Header = S("SnapToGrid");
            ShowLayersWindowMenuItem.Header = S("ShowLayers");
            
            // Color menu
            ColorPaletteMenuItem.Header = S("ColorPalette");
            SwapColorsMenuItem.Header = S("SwapColors");
            
            // Layers menu
            NewLayerMenu.Header = S("NewLayer");
            DeleteLayerMenu.Header = S("DeleteLayer");
            MergeVisibleMenu.Header = S("MergeVisible");
            ShowLayersPanelMenu.Header = S("ShowLayersPanel");
            
            // Brush menu
            BrushPresetsMenuItem.Header = S("BrushPresets");
            IncreaseBrushMenu.Header = S("IncreaseBrush");
            DecreaseBrushMenu.Header = S("DecreaseBrush");
            ResetBrushMenu.Header = S("ResetBrush");
            
            // Shapes menu
            LineMenu.Header = S("Line");
            RectangleMenu.Header = S("Rectangle");
            CircleMenu.Header = S("Circle");
            GradientMenu.Header = S("Gradient");
            
            // Settings menu
            SettingsMenuItem.Header = S("Settings");
            AboutMenuItem.Header = S("About");
            
            // Toolbar
            UndoButton.Content = S("UndoButton");
            RedoButton.Content = S("RedoButton");
            CutButton.Content = S("CutButton");
            CopyButton.Content = S("CopyButton");
            PasteButton.Content = S("PasteButton");
            ApplySelectionButton.Content = S("ApplySelection");
            SizeLabel.Text = S("Size");
            HardnessLabel.Text = S("Hardness");
            
            // Toolbar shapes and gradient
            ShapeLabel.Text = S("Shape");
            LineComboItem.Content = S("Line");
            RectangleComboItem.Content = S("Rectangle");
            CircleComboItem.Content = S("Circle");
            StyleLabel.Text = S("Style");
            LinearComboItem.Content = S("Normal");
            RadialComboItem.Content = S("Radial");
            ReflectedComboItem.Content = S("Reflected");
            AddStopsButton.Content = S("AddStops");
            
            // Text panel
            FontLabel.Text = S("Font");
            TextSizeLabel.Text = S("Size");
            AlignmentLabel.Text = S("Alignment");
            TextOpacityToolbarLabel.Text = S("Opacity");
            
            // Force update status bar
            drawingManager?.SetTool(drawingManager.GetCurrentTool());
            uiManager?.UpdateBrushInfo();
            UpdateLayerInfo();
            UpdateGridInfo();
        }

        private System.Windows.Shapes.Line horizontalRulerLine;
        private System.Windows.Shapes.Line verticalRulerLine;

        private void DrawRulers()
        {
            HorizontalRuler.Children.Clear();
            VerticalRuler.Children.Clear();

            for (int i = 0; i <= 2000; i += 10)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = i,
                    Y1 = i % 100 == 0 ? 0 : (i % 50 == 0 ? 10 : 15),
                    X2 = i,
                    Y2 = 20,
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 1
                };
                HorizontalRuler.Children.Add(line);

                if (i % 100 == 0)
                {
                    var text = new TextBlock
                    {
                        Text = i.ToString(),
                        FontSize = 9,
                        Foreground = System.Windows.Media.Brushes.Black
                    };
                    Canvas.SetLeft(text, i + 2);
                    Canvas.SetTop(text, 2);
                    HorizontalRuler.Children.Add(text);
                }
            }

            for (int i = 0; i <= 2000; i += 10)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = i % 100 == 0 ? 0 : (i % 50 == 0 ? 10 : 15),
                    Y1 = i,
                    X2 = 20,
                    Y2 = i,
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 1
                };
                VerticalRuler.Children.Add(line);

                if (i % 100 == 0)
                {
                    var text = new TextBlock
                    {
                        Text = i.ToString(),
                        FontSize = 9,
                        Foreground = System.Windows.Media.Brushes.Black,
                        RenderTransform = new RotateTransform(-90)
                    };
                    Canvas.SetLeft(text, 2);
                    Canvas.SetTop(text, i - 2);
                    VerticalRuler.Children.Add(text);
                }
            }

            horizontalRulerLine = new System.Windows.Shapes.Line
            {
                Y1 = 0,
                Y2 = 20,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1,
                Visibility = Visibility.Collapsed
            };
            HorizontalRuler.Children.Add(horizontalRulerLine);

            verticalRulerLine = new System.Windows.Shapes.Line
            {
                X1 = 0,
                X2 = 20,
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 1,
                Visibility = Visibility.Collapsed
            };
            VerticalRuler.Children.Add(verticalRulerLine);

            DrawingSurface.MouseMove += UpdateRulerIndicators;
            DrawingSurface.MouseEnter += (s, e) =>
            {
                horizontalRulerLine.Visibility = Visibility.Visible;
                verticalRulerLine.Visibility = Visibility.Visible;
            };
            DrawingSurface.MouseLeave += (s, e) =>
            {
                horizontalRulerLine.Visibility = Visibility.Collapsed;
                verticalRulerLine.Visibility = Visibility.Collapsed;
            };
        }

        private void UpdateRulerIndicators(object sender, MouseEventArgs e)
        {
            if (!this.IsActive) return;

            var pos = e.GetPosition(DrawingSurface);
            if (pos.X < 0 || pos.Y < 0 || pos.X > DrawingSurface.ActualWidth || pos.Y > DrawingSurface.ActualHeight)
            {
                horizontalRulerLine.Visibility = Visibility.Collapsed;
                verticalRulerLine.Visibility = Visibility.Collapsed;
                return;
            }

            horizontalRulerLine.Visibility = Visibility.Visible;
            verticalRulerLine.Visibility = Visibility.Visible;

            var canvasPos = viewportManager?.ScreenToCanvas(pos) ?? pos;
            horizontalRulerLine.X1 = horizontalRulerLine.X2 = canvasPos.X;
            verticalRulerLine.Y1 = verticalRulerLine.Y2 = canvasPos.Y;
        }

        private void OnSelectionChanged(SKRect? selectionBounds)
        {
            if (selectionBounds.HasValue)
            {
                var bounds = selectionBounds.Value;
                // SelectionInfoText.Text = $"Выделение: {bounds.Width:0}x{bounds.Height:0}";

                DeleteMenuItem.IsEnabled = true;
                ClearSelectionMenuItem.IsEnabled = true;
                ApplySelectionButton.Visibility = Visibility.Visible;
            }
            else
            {
                DeleteMenuItem.IsEnabled = false;
                ClearSelectionMenuItem.IsEnabled = false;
                ApplySelectionButton.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ApplySelectionButton_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.ApplySelection();
        }

        // ==================== МЕТОДЫ ПРЕДПРОСМОТРА КИСТИ ====================

        public void UpdateBrushPreview(Point position, double brushSize)
        {
            var tool = drawingManager.GetCurrentTool();
            if (tool == ToolsPanel.ToolType.Selection || tool == ToolsPanel.ToolType.Eyedropper || tool == ToolsPanel.ToolType.Text)
            {
                BrushPreview.Visibility = Visibility.Collapsed;
                PixelBrushPreview.Visibility = Visibility.Collapsed;
                return;
            }

            var isPixelBrush = drawingManager.GetCurrentBrushType() == BrushType.Pixel;
            var color = drawingManager.GetCurrentBrushColor();
            
            if (isPixelBrush)
            {
                BrushPreview.Visibility = Visibility.Collapsed;
                var canvasPos = viewportManager.ScreenToCanvas(position);
                var pixelX = Math.Floor(canvasPos.X);
                var pixelY = Math.Floor(canvasPos.Y);
                var screenPos = viewportManager.CanvasToScreen(new Point(pixelX, pixelY));
                var pixelSize = (int)brushSize;
                var scaledSize = pixelSize * viewportManager.ZoomLevel;
                
                Canvas.SetLeft(PixelBrushPreview, screenPos.X);
                Canvas.SetTop(PixelBrushPreview, screenPos.Y);
                PixelBrushPreview.Width = scaledSize;
                PixelBrushPreview.Height = scaledSize;
                PixelBrushPreview.StrokeThickness = 1;
                PixelBrushPreview.Stroke = new SolidColorBrush(Color.FromArgb(128, color.Red, color.Green, color.Blue));
                PixelBrushPreview.Fill = new SolidColorBrush(Color.FromArgb(32, color.Red, color.Green, color.Blue));
                PixelBrushPreview.Visibility = Visibility.Visible;
            }
            else
            {
                PixelBrushPreview.Visibility = Visibility.Collapsed;
                var scaledSize = brushSize * viewportManager.ZoomLevel;
                Canvas.SetLeft(BrushPreview, position.X - scaledSize / 2);
                Canvas.SetTop(BrushPreview, position.Y - scaledSize / 2);
                BrushPreview.Width = scaledSize;
                BrushPreview.Height = scaledSize;
                BrushPreview.StrokeThickness = Math.Max(1, scaledSize / 15);
                BrushPreview.Stroke = new SolidColorBrush(Color.FromArgb(128, color.Red, color.Green, color.Blue));
                BrushPreview.Fill = new SolidColorBrush(Color.FromArgb(32, color.Red, color.Green, color.Blue));
                BrushPreview.Visibility = Visibility.Visible;
            }
        }

        public void HideBrushPreview()
        {
            BrushPreview.Visibility = Visibility.Collapsed;
            PixelBrushPreview.Visibility = Visibility.Collapsed;
        }

        public void ShowBrushSizeIndicator(double size)
        {
            BrushSizeIndicatorText.Text = $"{size:0}px";
            BrushSizeIndicator.Visibility = Visibility.Visible;

            // Скрываем через 1 секунду
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                BrushSizeIndicator.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void UpdateColorPreviews()
        {
            var primaryColor = drawingManager.GetCurrentBrushColor();
            var secondaryColor = drawingManager.GetSecondaryColor();

            PrimaryColorPreview.Background = new SolidColorBrush(Color.FromRgb(primaryColor.Red, primaryColor.Green, primaryColor.Blue));
            SecondaryColorPreview.Background = new SolidColorBrush(Color.FromRgb(secondaryColor.Red, secondaryColor.Green, secondaryColor.Blue));
        }
        
        public void UpdateColorPreviewsPublic()
        {
            UpdateColorPreviews();
        }

        private void UpdateLayerInfo()
        {
            var layerManager = drawingManager.GetLayerManager();
            if (layerManager?.ActiveLayer != null)
            {
                LayerInfoText.Text = string.Format(Localization.Strings.Get("Layer"), layerManager.ActiveLayer.Name, (int)(layerManager.ActiveLayer.Opacity * 100));
            }
        }

        private void UpdateGridInfo()
        {
            var status = drawingManager.IsGridVisible ? Localization.Strings.Get("On") : Localization.Strings.Get("Off");
            GridInfoText.Text = string.Format(Localization.Strings.Get("Grid"), status);

            // Обновляем состояние чекбоксов в меню
            ToggleGridMenuItem.IsChecked = drawingManager.IsGridVisible;
            ToggleSnapToGridMenuItem.IsChecked = drawingManager.IsSnapToGridEnabled;
        }

        // ==================== ОБРАБОТЧИКИ МЕНЮ ====================

        private void UndoMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.Undo();
        private void RedoMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.Redo();

        // Выделение
        private void CutMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.CutSelection();
        private void CopyMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.CopySelection();
        private void PasteMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.PasteSelection();
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.DeleteSelection();
        private void ClearSelectionMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.ClearSelection();

        // Слои
        private void ShowLayersWindowMenuItem_Click(object sender, RoutedEventArgs e) => uiManager.ShowLayersWindow();
        private void NewLayerMenuItem_Click(object sender, RoutedEventArgs e) => uiManager.CreateNewLayer();
        private void DeleteLayerMenuItem_Click(object sender, RoutedEventArgs e) => uiManager.RemoveActiveLayer();
        private void MergeLayersMenuItem_Click(object sender, RoutedEventArgs e) => uiManager.MergeVisibleLayers();

        // Цвета
        private void AdvancedColorPickerButton_Click(object sender, RoutedEventArgs e) => uiManager.ShowAdvancedColorPicker();
        private void SwapColorsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.SwapColors();
            UpdateColorPreviews();
        }


        // Сетка
        private void ToggleGridMenuItem_Click(object sender, RoutedEventArgs e)
        {
            uiManager.ToggleGrid();
            UpdateGridInfo();
        }

        private void ToggleSnapToGridMenuItem_Click(object sender, RoutedEventArgs e)
        {
            uiManager.ToggleSnapToGrid();
            UpdateGridInfo();
        }

        private void NavigatorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var navigator = new NavigatorWindow(drawingManager, viewportManager) { Owner = this };
            navigator.WindowStartupLocation = WindowStartupLocation.Manual;
            navigator.Left = this.Left + this.ActualWidth - navigator.Width - 20;
            navigator.Top = this.Top + this.ActualHeight - navigator.Height - 60;
            navigator.Show();
        }

        private void ToggleRulersMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var isVisible = ToggleRulersMenuItem.IsChecked;
            
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = isVisible ? 1 : 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            
            if (isVisible)
            {
                RulersRow.Visibility = Visibility.Visible;
                VerticalRuler.Visibility = Visibility.Visible;
            }
            else
            {
                opacityAnim.Completed += (s, args) =>
                {
                    RulersRow.Visibility = Visibility.Collapsed;
                    VerticalRuler.Visibility = Visibility.Collapsed;
                };
            }
            
            RulersRow.BeginAnimation(OpacityProperty, opacityAnim);
            VerticalRuler.BeginAnimation(OpacityProperty, opacityAnim);
        }

        // Кисть
        private void IncreaseBrushSizeMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.ChangeBrushSize(1);
        private void DecreaseBrushSizeMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.ChangeBrushSize(-1);
        private void ResetBrushMenuItem_Click(object sender, RoutedEventArgs e)
        {
            drawingManager.ResetBrushSettings();
            uiManager.UpdateBrushInfo();
        }

        private void ResizeCanvasMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var layer = drawingManager?.GetLayerManager()?.ActiveLayer;
            if (layer == null) return;

            var dialog = new ResizeCanvasWindow(layer.Bitmap.Width, layer.Bitmap.Height)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                drawingManager.ResizeCanvas(dialog.NewWidth, dialog.NewHeight);
            }
        }

        private void LineMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.SetTool(ToolsPanel.ToolType.Line);
        private void RectangleMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.SetTool(ToolsPanel.ToolType.Rectangle);
        private void CircleMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.SetTool(ToolsPanel.ToolType.Circle);
        private void GradientMenuItem_Click(object sender, RoutedEventArgs e) => drawingManager.SetTool(ToolsPanel.ToolType.Gradient);
        
        private void GradientStopsButton_Click(object sender, RoutedEventArgs e)
        {
            var S = Localization.Strings.Get;
            var dialog = new Window
            {
                Title = S("StopsEditor"),
                Width = 300,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };
            
            var stops = drawingManager.GetGradientStops();
            if (stops.Count == 0)
            {
                stops.Add(new DrawingManager.GradientStop { Color = drawingManager.GetCurrentBrushColor(), Position = 0 });
                stops.Add(new DrawingManager.GradientStop { Color = drawingManager.GetSecondaryColor(), Position = 1 });
            }
            int selectedStopIndex = 0;
            
            var mainGrid = new Grid { Margin = new Thickness(10) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var contentStack = new StackPanel();
            
            var previewBorder = new Border { Height = 40, Background = Brushes.White, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 5) };
            var previewCanvas = new Canvas { Cursor = Cursors.Hand };
            previewBorder.Child = previewCanvas;
            contentStack.Children.Add(previewBorder);
            
            var stopsCanvas = new Canvas { Height = 20, Background = Brushes.Transparent, Margin = new Thickness(0, 0, 0, 10) };
            contentStack.Children.Add(stopsCanvas);
            
            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var addButton = new Button { Content = "+", Width = 30, Height = 30, Margin = new Thickness(0, 0, 5, 0), FontSize = 16, FontWeight = FontWeights.Bold };
            var removeButton = new Button { Content = "-", Width = 30, Height = 30, FontSize = 16, FontWeight = FontWeights.Bold };
            buttonsPanel.Children.Add(addButton);
            buttonsPanel.Children.Add(removeButton);
            contentStack.Children.Add(buttonsPanel);
            
            var fileButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var loadButton = new Button { Content = S("Load"), Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 5, 0) };
            var saveButton = new Button { Content = S("Save"), Padding = new Thickness(10, 5, 10, 5) };
            fileButtonsPanel.Children.Add(loadButton);
            fileButtonsPanel.Children.Add(saveButton);
            contentStack.Children.Add(fileButtonsPanel);
            
            var colorLabel = new TextBlock { Text = S("Color"), Margin = new Thickness(0, 0, 0, 5) };
            contentStack.Children.Add(colorLabel);
            
            var colorButton = new Button { Content = S("SelectColor"), Margin = new Thickness(0, 0, 0, 10) };
            contentStack.Children.Add(colorButton);
            
            var posLabel = new TextBlock { Text = S("Position"), Margin = new Thickness(0, 0, 0, 5) };
            contentStack.Children.Add(posLabel);
            var posPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var posSlider = new Slider { Width = 180, Minimum = 0, Maximum = 100, Value = 0 };
            var posBox = new TextBox { Width = 40, Text = "0", Margin = new Thickness(5, 0, 0, 0) };
            posPanel.Children.Add(posSlider);
            posPanel.Children.Add(posBox);
            posPanel.Children.Add(new TextBlock { Text = "%", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            contentStack.Children.Add(posPanel);
            
            var opacityLabel = new TextBlock { Text = S("Opacity"), Margin = new Thickness(0, 0, 0, 5) };
            contentStack.Children.Add(opacityLabel);
            var opacityPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var opacitySlider = new Slider { Width = 180, Minimum = 0, Maximum = 100, Value = 100 };
            var opacityBox = new TextBox { Width = 40, Text = "100", Margin = new Thickness(5, 0, 0, 0) };
            opacityPanel.Children.Add(opacitySlider);
            opacityPanel.Children.Add(opacityBox);
            opacityPanel.Children.Add(new TextBlock { Text = "%", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            contentStack.Children.Add(opacityPanel);
            
            Border draggedMarker = null;
            bool updatingPos = false;
            bool updatingOpacity = false;
            
            Action updatePreview = null;
            updatePreview = () =>
            {
                var gradientStops = new GradientStopCollection();
                foreach (var stop in stops)
                {
                    gradientStops.Add(new GradientStop(Color.FromArgb(stop.Color.Alpha, stop.Color.Red, stop.Color.Green, stop.Color.Blue), stop.Position));
                }
                previewBorder.Background = new LinearGradientBrush(gradientStops, 0);
                
                if (selectedStopIndex >= 0 && selectedStopIndex < stops.Count)
                {
                    posSlider.Value = stops[selectedStopIndex].Position * 100;
                    opacitySlider.Value = stops[selectedStopIndex].Color.Alpha * 100 / 255;
                }
                
                stopsCanvas.Children.Clear();
                for (int i = 0; i < stops.Count; i++)
                {
                    var stop = stops[i];
                    var marker = new Border
                    {
                        Width = 12,
                        Height = 12,
                        Background = new SolidColorBrush(Color.FromArgb(stop.Color.Alpha, stop.Color.Red, stop.Color.Green, stop.Color.Blue)),
                        BorderBrush = i == selectedStopIndex ? Brushes.Blue : Brushes.Black,
                        BorderThickness = new Thickness(2),
                        Cursor = Cursors.Hand,
                        Tag = i
                    };
                    Canvas.SetLeft(marker, stop.Position * (stopsCanvas.ActualWidth > 0 ? stopsCanvas.ActualWidth : 260) - 6);
                    Canvas.SetTop(marker, 4);
                    
                    marker.MouseDown += (s, ev) =>
                    {
                        selectedStopIndex = (int)((Border)s).Tag;
                        if (ev.ClickCount == 2)
                        {
                            var currentColor = stops[selectedStopIndex].Color;
                            var picker = new ColorPickerWindow(dialog);
                            picker.SetInitialColor(Color.FromRgb(currentColor.Red, currentColor.Green, currentColor.Blue));
                            picker.ColorChanged += (color) =>
                            {
                                stops[selectedStopIndex].Color = new SKColor(color.R, color.G, color.B);
                                updatePreview();
                            };
                            picker.ShowDialog();
                        }
                        else
                        {
                            draggedMarker = (Border)s;
                            stopsCanvas.CaptureMouse();
                            updatePreview();
                        }
                    };
                    
                    stopsCanvas.Children.Add(marker);
                }
            };
            
            MouseEventHandler moveHandler = null;
            MouseButtonEventHandler upHandler = null;
            
            moveHandler = (s, ev) =>
            {
                if (draggedMarker != null)
                {
                    var pos = ev.GetPosition(stopsCanvas);
                    var newPos = Math.Max(0, Math.Min(1, pos.X / stopsCanvas.ActualWidth));
                    stops[selectedStopIndex].Position = (float)newPos;
                    updatePreview();
                }
            };
            
            upHandler = (s, ev) =>
            {
                if (draggedMarker != null)
                {
                    stopsCanvas.ReleaseMouseCapture();
                    draggedMarker = null;
                }
            };
            
            stopsCanvas.MouseMove += moveHandler;
            dialog.MouseMove += moveHandler;
            dialog.MouseUp += upHandler;
            
            addButton.Click += (s, ev) =>
            {
                var newStop = new DrawingManager.GradientStop
                {
                    Color = drawingManager.GetCurrentBrushColor(),
                    Position = 0.5f
                };
                stops.Add(newStop);
                stops.Sort((a, b) => a.Position.CompareTo(b.Position));
                selectedStopIndex = stops.IndexOf(newStop);
                updatePreview();
            };
            
            removeButton.Click += (s, ev) =>
            {
                if (selectedStopIndex >= 0 && selectedStopIndex < stops.Count && stops.Count > 2)
                {
                    stops.RemoveAt(selectedStopIndex);
                    selectedStopIndex = Math.Max(0, Math.Min(selectedStopIndex, stops.Count - 1));
                    updatePreview();
                }
            };
            
            loadButton.Click += (s, ev) =>
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = S("Gradients"),
                    Title = S("LoadGradient")
                };
                if (openDialog.ShowDialog() == true)
                {
                    try
                    {
                        var lines = System.IO.File.ReadAllLines(openDialog.FileName);
                        stops.Clear();
                        foreach (var line in lines)
                        {
                            var parts = line.Split(',');
                            if (parts.Length == 5)
                            {
                                stops.Add(new DrawingManager.GradientStop
                                {
                                    Color = new SKColor(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])),
                                    Position = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture)
                                });
                            }
                        }
                        selectedStopIndex = 0;
                        updatePreview();
                    }
                    catch { MessageBox.Show(S("LoadError"), S("Error"), MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            };
            
            saveButton.Click += (s, ev) =>
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = S("Gradients"),
                    Title = S("SaveGradient")
                };
                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        var lines = stops.Select(stop => $"{stop.Color.Red},{stop.Color.Green},{stop.Color.Blue},{stop.Color.Alpha},{stop.Position.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                        System.IO.File.WriteAllLines(saveDialog.FileName, lines);
                    }
                    catch { MessageBox.Show(S("SaveError"), S("Error"), MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            };
            
            colorButton.Click += (s, ev) =>
            {
                if (selectedStopIndex >= 0 && selectedStopIndex < stops.Count)
                {
                    var currentColor = stops[selectedStopIndex].Color;
                    var picker = new ColorPickerWindow(dialog);
                    picker.SetInitialColor(Color.FromRgb(currentColor.Red, currentColor.Green, currentColor.Blue));
                    picker.ColorChanged += (color) =>
                    {
                        stops[selectedStopIndex].Color = new SKColor(color.R, color.G, color.B, stops[selectedStopIndex].Color.Alpha);
                        updatePreview();
                    };
                    picker.ShowDialog();
                }
            };
            
            posSlider.ValueChanged += (s, ev) =>
            {
                if (!updatingPos && selectedStopIndex >= 0 && selectedStopIndex < stops.Count)
                {
                    updatingPos = true;
                    stops[selectedStopIndex].Position = (float)(posSlider.Value / 100);
                    posBox.Text = ((int)posSlider.Value).ToString();
                    updatePreview();
                    updatingPos = false;
                }
            };
            posBox.TextChanged += (s, ev) =>
            {
                if (!updatingPos && int.TryParse(posBox.Text, out int val) && val >= 0 && val <= 100)
                {
                    updatingPos = true;
                    posSlider.Value = val;
                    updatingPos = false;
                }
            };
            
            opacitySlider.ValueChanged += (s, ev) =>
            {
                if (!updatingOpacity && selectedStopIndex >= 0 && selectedStopIndex < stops.Count)
                {
                    updatingOpacity = true;
                    var alpha = (byte)(opacitySlider.Value * 255 / 100);
                    stops[selectedStopIndex].Color = new SKColor(stops[selectedStopIndex].Color.Red, stops[selectedStopIndex].Color.Green, stops[selectedStopIndex].Color.Blue, alpha);
                    opacityBox.Text = ((int)opacitySlider.Value).ToString();
                    updatePreview();
                    updatingOpacity = false;
                }
            };
            opacityBox.TextChanged += (s, ev) =>
            {
                if (!updatingOpacity && int.TryParse(opacityBox.Text, out int val) && val >= 0 && val <= 100)
                {
                    updatingOpacity = true;
                    opacitySlider.Value = val;
                    updatingOpacity = false;
                }
            };
            
            Grid.SetRow(contentStack, 0);
            mainGrid.Children.Add(contentStack);
            
            var okButton = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            okButton.Click += (s, args) =>
            {
                drawingManager.SetGradientStops(stops);
                dialog.DialogResult = true;
                dialog.Close();
            };
            Grid.SetRow(okButton, 1);
            mainGrid.Children.Add(okButton);
            
            dialog.Content = mainGrid;
            stopsCanvas.Loaded += (s, ev) => updatePreview();
            dialog.ShowDialog();
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            App.ChangeTheme("LightTheme");
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            App.ChangeTheme("DarkTheme");
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(drawingManager) { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow { Owner = this };
            aboutWindow.ShowDialog();
        }

        private void ToggleToolsPanelMenuItem_Click(object sender, RoutedEventArgs e) => uiManager.ToggleToolsPanel();
        private bool isModified = false;
        private string currentFilePath = null;
        private string lastTextSize = "24";
        private List<string> fontHistory = new List<string>();
        private List<string> sizeHistory = new List<string>();

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                drawingManager.NewCanvas();
                isModified = false;
                currentFilePath = null;
                Title = "mint.paint 🎨";
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAsFile();
            }
            else
            {
                SaveFile(currentFilePath);
            }
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckUnsavedChanges()) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PNG файлы (*.png)|*.png|Все файлы (*.*)|*.*",
                Title = "Открыть изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = SKBitmap.Decode(dialog.FileName);
                    using var canvas = new SKCanvas(drawingManager.GetLayerManager().ActiveLayer.Bitmap);
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(bitmap, 0, 0);
                    drawingManager.InvalidateCanvas();
                    currentFilePath = dialog.FileName;
                    isModified = false;
                    Title = $"mint.paint 🎨 - {System.IO.Path.GetFileName(currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Localization.Strings.Get("OpenFileError"), ex.Message), Localization.Strings.Get("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAsFile()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG файлы (*.png)|*.png",
                Title = "Сохранить изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                SaveFile(dialog.FileName);
            }
        }

        private void SaveFile(string path)
        {
            try
            {
                using var image = SKImage.FromBitmap(drawingManager.GetLayerManager().ActiveLayer.Bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = System.IO.File.OpenWrite(path);
                data.SaveTo(stream);
                
                currentFilePath = path;
                isModified = false;
                Title = $"mint.paint 🎨 - {System.IO.Path.GetFileName(currentFilePath)}";

            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.Strings.Get("SaveFileError"), ex.Message), Localization.Strings.Get("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckUnsavedChanges()
        {
            if (isModified)
            {
                var S = Localization.Strings.Get;
                var result = MessageBox.Show(S("SaveChanges"), S("UnsavedChanges"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveMenuItem_Click(null, null);
                    return !isModified;
                }
                return result == MessageBoxResult.No;
            }
            return true;
        }

        // ==================== ОБРАБОТЧИКИ КИСТИ ====================

        private void BrushPresetsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            uiManager.ShowPresetsWindow();
        }

        private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialized || drawingManager == null) return;
            drawingManager.SetBrushSize(e.NewValue);
            BrushSizeText.Text = $"{e.NewValue:0}";
            ShowBrushSizeIndicator(e.NewValue);
            isModified = true;
        }

        private void BrushHardnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialized || drawingManager == null) return;
            drawingManager.SetBrushHardness(e.NewValue);
            BrushHardnessText.Text = $"{e.NewValue:0}";
        }

        private void DropSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialized || drawingManager == null) return;
            drawingManager.SetDropSize(e.NewValue);
            DropSizeText.Text = $"{e.NewValue:0.0}";
        }

        private void DropCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialized || drawingManager == null) return;
            drawingManager.SetDropCount((int)e.NewValue);
            DropCountText.Text = $"{(int)e.NewValue}";
        }



        private void PrimaryColorPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            uiManager.ShowAdvancedColorPicker();
        }

        private void SecondaryColorPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            uiManager.ShowAdvancedColorPicker();
        }

        private void ShapesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitialized || ShapesComboBox.SelectedItem == null) return;
            
            var item = (ComboBoxItem)ShapesComboBox.SelectedItem;
            var shape = item.Tag.ToString();
            
            switch (shape)
            {
                case "Line":
                    drawingManager.SetTool(ToolsPanel.ToolType.Line);
                    break;
                case "Rectangle":
                    drawingManager.SetTool(ToolsPanel.ToolType.Rectangle);
                    break;
                case "Circle":
                    drawingManager.SetTool(ToolsPanel.ToolType.Circle);
                    break;
            }
        }

        public void ShowShapesPanel(ToolsPanel.ToolType tool)
        {
            ShapesPanel.Visibility = (tool == ToolsPanel.ToolType.Line || tool == ToolsPanel.ToolType.Rectangle || tool == ToolsPanel.ToolType.Circle)
                ? Visibility.Visible : Visibility.Collapsed;
                
            if (ShapesPanel.Visibility == Visibility.Visible)
            {
                if (tool == ToolsPanel.ToolType.Line) ShapesComboBox.SelectedIndex = 0;
                else if (tool == ToolsPanel.ToolType.Rectangle) ShapesComboBox.SelectedIndex = 1;
                else if (tool == ToolsPanel.ToolType.Circle) ShapesComboBox.SelectedIndex = 2;
            }
            
            GradientPanel.Visibility = (tool == ToolsPanel.ToolType.Gradient) ? Visibility.Visible : Visibility.Collapsed;
            TextPanel.Visibility = (tool == ToolsPanel.ToolType.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateDropsPanel(bool isDropsBrush)
        {
            DropsPanel.Visibility = isDropsBrush ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateBrushSizeEnabled(bool enabled)
        {
            BrushSizeSlider.IsEnabled = enabled;
        }



        private void InitializeTextPanel()
        {
            var fonts = new System.Collections.Generic.List<string>
            {
                "Arial", "Times New Roman", "Courier New", "Verdana", "Tahoma",
                "Georgia", "Comic Sans MS", "Impact", "Calibri", "Consolas"
            };
            fonts.AddRange(PresetsWindow.CustomFonts);
            fonts.Add("Пользовательский...");
            TextFontComboBox.ItemsSource = fonts;
            TextFontComboBox.SelectedIndex = 0;
            
            var sizes = new System.Collections.Generic.List<string> { "8", "10", "12", "14", "16", "18", "20", "24", "28", "32", "36", "48", "72", "Пользовательский..." };
            TextSizeComboBox.ItemsSource = sizes;
            TextSizeComboBox.Text = "24";
            
            TextFontComboBox.SelectionChanged += TextFontComboBox_SelectionChanged;
            TextSizeComboBox.SelectionChanged += TextSizeComboBox_SelectionChanged;
            TextSizeComboBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new System.Windows.Controls.TextChangedEventHandler(TextSizeComboBox_TextChanged));
            TextBoldButton.Checked += (s, e) => UpdateTextParameters();
            TextBoldButton.Unchecked += (s, e) => UpdateTextParameters();
            TextItalicButton.Checked += (s, e) => UpdateTextParameters();
            TextItalicButton.Unchecked += (s, e) => UpdateTextParameters();
            
            var alignments = new System.Collections.Generic.List<string> { "Влево", "Центр", "Вправо" };
            TextAlignmentCombo.ItemsSource = alignments;
            TextAlignmentCombo.SelectedIndex = 0;
            TextAlignmentCombo.SelectionChanged += (s, e) => UpdateTextParameters();
            
            TextOpacitySlider.ValueChanged += (s, e) => {
                TextOpacityLabel.Text = $"{(int)TextOpacitySlider.Value}%";
                UpdateTextParameters();
            };
            
            TextColorBox.Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
        
        private bool isMovingText = false;
        private Point textMoveStartPos;
        
        public bool GetAutoLayerOnText()
        {
            return uiManager?.GetAutoLayerOnText() ?? false;
        }
        
        private void OnTextAreaSelected(Point canvasPosition, double width, double height)
        {
            if (GetAutoLayerOnText())
            {
                drawingManager.CreateNewLayer($"Text {DateTime.Now:HH:mm:ss}");
            }
            
            UpdateTextParameters();
            drawingManager.StartTextInput(canvasPosition);
            
            HiddenTextInput.Text = "";
            HiddenTextInput.Focus();
            HiddenTextInput.TextChanged += HiddenTextInput_TextChanged;
            HiddenTextInput.KeyDown += HiddenTextInput_KeyDown;
            
            UpdateTextMoveButton();
        }
        
        private void UpdateTextMoveButton()
        {
            if (drawingManager.IsTextInputActive())
            {
                var pos = drawingManager.GetTextCursorPosition();
                var screenPos = viewportManager.CanvasToScreen(new Point(pos.X, pos.Y));
                Canvas.SetLeft(TextMoveButton, screenPos.X - 35);
                Canvas.SetTop(TextMoveButton, screenPos.Y - 35);
                TextMoveButton.Visibility = Visibility.Visible;
            }
            else
            {
                TextMoveButton.Visibility = Visibility.Collapsed;
            }
        }
        
        private void TextMoveButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(1.3, TimeSpan.FromMilliseconds(150));
            TextMoveButtonScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            TextMoveButtonScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }
        
        private void TextMoveButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
            TextMoveButtonScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            TextMoveButtonScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }
        
        private void TextMoveButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMovingText = true;
            textMoveStartPos = e.GetPosition(DrawingSurface);
            TextMoveButton.CaptureMouse();
            TextMoveButton.MouseMove += TextMoveButton_MouseMove;
            TextMoveButton.MouseUp += TextMoveButton_MouseUp;
            e.Handled = true;
        }
        
        private void TextMoveButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMovingText)
            {
                var currentPos = e.GetPosition(DrawingSurface);
                var delta = new Point(currentPos.X - textMoveStartPos.X, currentPos.Y - textMoveStartPos.Y);
                textMoveStartPos = currentPos;
                
                var dx = delta.X / viewportManager.ZoomLevel;
                var dy = delta.Y / viewportManager.ZoomLevel;
                drawingManager.MoveTextPosition((float)dx, (float)dy);
                UpdateTextMoveButton();
            }
        }
        
        private void TextMoveButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMovingText = false;
            TextMoveButton.ReleaseMouseCapture();
            TextMoveButton.MouseMove -= TextMoveButton_MouseMove;
            TextMoveButton.MouseUp -= TextMoveButton_MouseUp;
        }
        
        private void HiddenTextInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (drawingManager.IsTextInputActive())
            {
                drawingManager.SetCurrentText(HiddenTextInput.Text);
                UpdateTextMoveButton();
            }
        }
        
        private void HiddenTextInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrEmpty(HiddenTextInput.Text))
                {
                    var font = TextFontComboBox.SelectedItem?.ToString() ?? "Arial";
                    if (!double.TryParse(TextSizeComboBox.Text, out double size) || size < 1)
                    {
                        size = 24;
                    }
                    var bold = TextBoldButton.IsChecked == true;
                    var italic = TextItalicButton.IsChecked == true;
                    var underline = TextUnderlineButton.IsChecked == true;
                    var strikethrough = TextStrikethroughButton.IsChecked == true;
                    
                    drawingManager.ApplyText(font, (float)size, bold, italic, underline, strikethrough);
                }
                drawingManager.StopTextInput();
                HiddenTextInput.TextChanged -= HiddenTextInput_TextChanged;
                HiddenTextInput.KeyDown -= HiddenTextInput_KeyDown;
                TextMoveButton.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                drawingManager.StopTextInput();
                HiddenTextInput.TextChanged -= HiddenTextInput_TextChanged;
                HiddenTextInput.KeyDown -= HiddenTextInput_KeyDown;
                TextMoveButton.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Down || 
                     e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.Right)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    float dx = 0, dy = 0;
                    if (e.Key == System.Windows.Input.Key.Left) dx = -1;
                    else if (e.Key == System.Windows.Input.Key.Right) dx = 1;
                    else if (e.Key == System.Windows.Input.Key.Up) dy = -1;
                    else if (e.Key == System.Windows.Input.Key.Down) dy = 1;
                    drawingManager.MoveTextPosition(dx, dy);
                    e.Handled = true;
                }
            }
        }
        
        private void TextFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TextFontComboBox.SelectedItem is string font && font == "Пользовательский...")
            {
                var dialog = new Window
                {
                    Title = "Пользовательский шрифт",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(10) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock { Text = "Название шрифта:", Margin = new Thickness(0, 0, 0, 5) };
                Grid.SetRow(label, 0);

                var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
                Grid.SetRow(textBox, 1);

                var okButton = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetRow(okButton, 2);
                okButton.Click += (s, args) => { dialog.DialogResult = true; dialog.Close(); };

                grid.Children.Add(label);
                grid.Children.Add(textBox);
                grid.Children.Add(okButton);
                dialog.Content = grid;

                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    AddToFontHistory(textBox.Text);
                    UpdateTextParameters();
                }
                else
                {
                    TextFontComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                UpdateTextParameters();
            }
        }

        private void AddToFontHistory(string font)
        {
            if (fontHistory.Contains(font))
                fontHistory.Remove(font);
            fontHistory.Insert(0, font);
            TextFontComboBox.SelectedItem = font;
        }

        private void AddToSizeHistory(string size)
        {
            if (sizeHistory.Contains(size))
                sizeHistory.Remove(size);
            sizeHistory.Insert(0, size);
        }

        private void TextSizeComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitialized) return;
            if (double.TryParse(TextSizeComboBox.Text, out double _))
            {
                lastTextSize = TextSizeComboBox.Text;
                AddToSizeHistory(TextSizeComboBox.Text);
                UpdateTextParameters();
            }
        }
        
        private void TextSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TextSizeComboBox.SelectedItem is string size && size == "Пользовательский...")
            {
                
                var dialog = new Window
                {
                    Title = "Пользовательский размер",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(10) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock { Text = "Размер (px):", Margin = new Thickness(0, 0, 0, 5) };
                Grid.SetRow(label, 0);

                var textBox = new TextBox { Text = lastTextSize, Margin = new Thickness(0, 0, 0, 10) };
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
                    lastTextSize = customSize.ToString();
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TextSizeComboBox.SelectedIndex = -1;
                        TextSizeComboBox.Text = lastTextSize;
                        UpdateTextParameters();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TextSizeComboBox.SelectedIndex = -1;
                        TextSizeComboBox.Text = lastTextSize;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                return;
            }
            UpdateTextParameters();
        }
        
        private void UpdateTextParameters()
        {
            var font = TextFontComboBox.SelectedItem?.ToString() ?? "Arial";
            if (!double.TryParse(TextSizeComboBox.Text, out double size) || size < 1)
            {
                size = 24;
            }
            var bold = TextBoldButton.IsChecked == true;
            var italic = TextItalicButton.IsChecked == true;
            var underline = TextUnderlineButton.IsChecked == true;
            var strikethrough = TextStrikethroughButton.IsChecked == true;
            var alignment = TextAlignmentCombo.SelectedIndex switch { 1 => TextAlignment.Center, 2 => TextAlignment.Right, _ => TextAlignment.Left };
            var opacity = (byte)(TextOpacitySlider.Value * 255 / 100);
            
            drawingManager.SetTextParameters(font, (float)size, bold, italic, underline, strikethrough);
            drawingManager.SetTextAlignment(alignment);
            drawingManager.SetTextOpacity(opacity);
            
            if (drawingManager.IsTextInputActive())
            {
                HiddenTextInput.Focus();
            }
        }
        


        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {

            base.OnPreviewKeyDown(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!CheckUnsavedChanges())
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }


        
        private void TextColorBox_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            uiManager.ShowAdvancedColorPicker();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            uiManager?.Cleanup();
            drawingManager?.Dispose();
            base.OnClosed(e);
        }
    }
}