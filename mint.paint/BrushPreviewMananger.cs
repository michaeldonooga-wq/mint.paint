using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace mint.paint
{
    public class BrushPreviewManager
    {
        private Ellipse brushPreview;
        private Border sizeIndicator;
        private TextBlock sizeIndicatorText;

        private SKColor previewColor = new SKColor(0, 0, 0, 128); // Полупрозрачный черный
        private bool isVisible = false;

        public BrushPreviewManager(Ellipse brushPreviewElement, Border sizeIndicatorElement, TextBlock sizeIndicatorTextElement)
        {
            brushPreview = brushPreviewElement;
            sizeIndicator = sizeIndicatorElement;
            sizeIndicatorText = sizeIndicatorTextElement;

            // Начальные настройки
            brushPreview.Visibility = Visibility.Collapsed;
            sizeIndicator.Visibility = Visibility.Collapsed;
        }

        public void UpdatePreview(Point position, double brushSize, SKColor color, ToolsPanel.ToolType tool)
        {
            if (tool == ToolsPanel.ToolType.Selection || tool == ToolsPanel.ToolType.Eyedropper || tool == ToolsPanel.ToolType.Text)
            {
                HidePreview();
                return;
            }

            brushSize = Math.Max(1, brushSize);

            // Обновляем размер и позицию
            brushPreview.Width = brushSize;
            brushPreview.Height = brushSize;
            brushPreview.StrokeThickness = Math.Max(1, brushSize / 15);

            // Позиционируем по центру курсора
            Canvas.SetLeft(brushPreview, position.X - brushSize / 2);
            Canvas.SetTop(brushPreview, position.Y - brushSize / 2);

            // Обновляем цвет в зависимости от инструмента
            var wpfColor = tool switch
            {
                ToolsPanel.ToolType.Eraser => Colors.White,
                _ => Color.FromArgb(128, color.Red, color.Green, color.Blue)
            };

            brushPreview.Stroke = new SolidColorBrush(wpfColor);
            brushPreview.Fill = tool == ToolsPanel.ToolType.Eraser ?
                new SolidColorBrush(Colors.Transparent) :
                new SolidColorBrush(Color.FromArgb(64, color.Red, color.Green, color.Blue));

            ShowPreview();
        }

        public void ShowPreview()
        {
            if (!isVisible)
            {
                brushPreview.Visibility = Visibility.Visible;
                isVisible = true;
            }
        }

        public void HidePreview()
        {
            if (isVisible)
            {
                brushPreview.Visibility = Visibility.Collapsed;
                isVisible = false;
            }
        }

        public void ShowSizeIndicator(double size)
        {
            sizeIndicatorText.Text = $"{size:0}px";
            sizeIndicator.Visibility = Visibility.Visible;

            // Автоматическое скрытие через 2 секунды
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = System.TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) =>
            {
                sizeIndicator.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        public void HideSizeIndicator()
        {
            sizeIndicator.Visibility = Visibility.Collapsed;
        }

        // Анимированное изменение размера preview
        public void AnimateSizeChange(double oldSize, double newSize)
        {
            if (Math.Abs(newSize - oldSize) > 5)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation(
                    brushPreview.Width,
                    newSize,
                    new System.Windows.Duration(System.TimeSpan.FromMilliseconds(200)));

                brushPreview.BeginAnimation(Ellipse.WidthProperty, animation);
                brushPreview.BeginAnimation(Ellipse.HeightProperty, animation);
            }
        }

        // Показывает информацию о кисти при наведении
        public void ShowBrushInfo(double size, double hardness, SKColor color)
        {
            var toolTip = new ToolTip
            {
                Content = $"Размер: {size:0}px\nЖесткость: {hardness:0}%\nЦвет: RGB({color.Red}, {color.Green}, {color.Blue})",
                Placement = System.Windows.Controls.Primitives.PlacementMode.Relative,
                HorizontalOffset = 20,
                VerticalOffset = 20
            };

            brushPreview.ToolTip = toolTip;
        }
    }
}