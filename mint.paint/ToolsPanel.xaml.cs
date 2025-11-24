using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace mint.paint
{
    public partial class ToolsPanel : UserControl
    {
        public enum ToolType { Brush, Eraser, Fill, Eyedropper, Line, Rectangle, Circle, Selection, EllipseSelection, Lasso, SelectionMove, Text, Gradient, MagicWand }

        public ToolType CurrentTool { get; private set; } = ToolType.Brush;
        public double BrushSize { get; private set; } = 5;
        public double BrushHardness { get; private set; } = 100;
        public double BrushInterval { get; private set; } = 1;
        public bool SmoothTrajectory { get; private set; } = true;
        public bool SmoothBrush { get; private set; } = true;
        public bool BrushPreviewEnabled { get; private set; } = true;

        public event Action<ToolType> ToolChanged;
        public event Action ClearRequested;
        public event Action BrushSettingsChanged;
        public event Action<bool> BrushPreviewChanged;
        public event Action PresetsRequested;

        private Button lastSelectedButton;

        public ToolsPanel()
        {
            InitializeComponent();

            // Изначально выбираем кисть
            SelectButton(BrushButton, ToolType.Brush);

            // Включаем перетаскивание для всего UserControl
            this.MouseLeftButtonDown += ToolsPanel_MouseLeftButtonDown;
        }

        private void ToolsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Позволяем перетаскивать окно при клике на любую область панели
            // кроме кнопок (они обрабатывают клики сами)
            if (e.OriginalSource is FrameworkElement element)
            {
                if (element is Button ||
                    element.Parent is Button ||
                    element is TextBlock ||
                    element is Border)
                {
                    return; // Не начинаем перетаскивание при клике на элементы управления
                }
            }

            // Начинаем перетаскивание окна
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.DragMove();
            }
        }

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var tool = button.Tag.ToString() switch
                {
                    "Brush" => ToolType.Brush,
                    "Eraser" => ToolType.Eraser,
                    "Fill" => ToolType.Fill,
                    "Eyedropper" => ToolType.Eyedropper,
                    "Line" => ToolType.Line,
                    "Rectangle" => ToolType.Rectangle,
                    "Circle" => ToolType.Circle,
                    "Selection" => ToolType.Selection,
                    "EllipseSelection" => ToolType.EllipseSelection,
                    "Lasso" => ToolType.Lasso,
                    "SelectionMove" => ToolType.SelectionMove,
                    "Text" => ToolType.Text,
                    "Gradient" => ToolType.Gradient,
                    "MagicWand" => ToolType.MagicWand,
                    _ => ToolType.Brush
                };

                SelectButton(button, tool);
                ToolChanged?.Invoke(tool);
            }

            e.Handled = true;
        }

        private void SelectButton(Button button, ToolType tool)
        {
            // Сбрасываем стиль предыдущей кнопки
            if (lastSelectedButton != null)
            {
                lastSelectedButton.Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                lastSelectedButton.BorderThickness = new Thickness(1);
                lastSelectedButton.BorderBrush = Brushes.LightGray;
            }

            // Устанавливаем стиль для выбранной кнопки
            button.Background = new SolidColorBrush(Color.FromArgb(255, 229, 243, 255));
            button.BorderThickness = new Thickness(2);
            button.BorderBrush = Brushes.DodgerBlue;

            lastSelectedButton = button;
            CurrentTool = tool;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить весь холст?", "Подтверждение",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ClearRequested?.Invoke();
            }

            e.Handled = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Настройки кисти и инструментов будут открыты в отдельном окне",
                          "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);

            e.Handled = true;
        }

        private void PresetsButton_Click(object sender, RoutedEventArgs e)
        {
            PresetsRequested?.Invoke();
            e.Handled = true;
        }

        public void SetTool(ToolType tool)
        {
            Button button = tool switch
            {
                ToolType.Brush => BrushButton,
                ToolType.Eraser => EraserButton,
                ToolType.Fill => FillButton,
                ToolType.Eyedropper => EyedropperButton,
                ToolType.Line => ShapesButton,
                ToolType.Rectangle => ShapesButton,
                ToolType.Circle => ShapesButton,
                ToolType.Selection => SelectionButton,
                ToolType.EllipseSelection => EllipseSelectionButton,
                ToolType.Lasso => LassoButton,
                ToolType.SelectionMove => SelectionMoveButton,
                ToolType.Text => TextButton,
                ToolType.Gradient => GradientButton,
                ToolType.MagicWand => MagicWandButton,
                _ => BrushButton
            };

            SelectButton(button, tool);
            ToolChanged?.Invoke(tool);
        }

        // Методы для обновления настроек
        public void UpdateBrushSize(double size)
        {
            BrushSize = size;
            BrushSettingsChanged?.Invoke();
        }

        public void UpdateBrushHardness(double hardness)
        {
            BrushHardness = hardness;
            BrushSettingsChanged?.Invoke();
        }

        public void UpdateBrushInterval(double interval)
        {
            BrushInterval = interval;
            BrushSettingsChanged?.Invoke();
        }
    }
}