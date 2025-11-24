using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace mint.paint
{
    public class InputManager
    {
        private DrawingManager drawingManager;
        private ViewportManager viewportManager;
        private MainWindow mainWindow;

        private bool isSpacePressed = false;
        private bool isPanning = false;
        private Point panStartPoint;

        public InputManager(DrawingManager drawingManager, ViewportManager viewportManager, MainWindow mainWindow)
        {
            this.drawingManager = drawingManager;
            this.viewportManager = viewportManager;
            this.mainWindow = mainWindow;
        }

        public void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var position = e.GetPosition(element);

            // Панорамирование (Space + левая кнопка или СКМ)
            if ((isSpacePressed && e.LeftButton == MouseButtonState.Pressed) || e.MiddleButton == MouseButtonState.Pressed)
            {
                isPanning = true;
                panStartPoint = position;
                if (element != null)
                {
                    element.Cursor = Cursors.SizeAll;
                }
                e.Handled = true;
                return;
            }



            bool isRightButton = e.RightButton == MouseButtonState.Pressed;
            drawingManager.HandleMouseDown(position, isSpacePressed, isRightButton);
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            var position = e.GetPosition(element);

            // Обновляем предпросмотр кисти
            mainWindow.UpdateBrushPreview(position, drawingManager.GetBrushSize());

            // Панорамирование
            if (isPanning)
            {
                var currentPos = position;
                var delta = new Point(currentPos.X - panStartPoint.X, currentPos.Y - panStartPoint.Y);
                viewportManager.Pan(delta);
                panStartPoint = currentPos;
                return;
            }

            // Курсор руки при наведении на кнопку перемещения
            if (drawingManager.GetCurrentTool() == ToolsPanel.ToolType.Selection && drawingManager.IsOverMoveButton(position))
            {
                element.Cursor = Cursors.Hand;
            }
            else if (!isPanning)
            {
                element.Cursor = Cursors.Arrow;
            }

            drawingManager.HandleMouseMove(position);
        }

        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (isPanning)
            {
                isPanning = false;
                if (element != null)
                {
                    element.Cursor = Cursors.Arrow;
                }
                return;
            }

            var position = e.GetPosition(element);
            drawingManager.HandleMouseUp(position);
        }

        public void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var element = sender as FrameworkElement;
            var position = e.GetPosition(element);

            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.None)
            {
                // Зум колесиком мыши (с Ctrl или без)
                if (e.Delta > 0)
                {
                    viewportManager.ZoomIn(position);
                }
                else
                {
                    viewportManager.ZoomOut(position);
                }
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                // Изменение размера кисти колесиком с Alt
                var currentSize = drawingManager.GetBrushSize();
                var newSize = currentSize + (e.Delta > 0 ? 2 : -2);
                newSize = Math.Max(1, Math.Min(100, newSize));
                drawingManager.SetBrushSize(newSize);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                // Изменение жесткости кисти колесиком с Shift
                var currentHardness = drawingManager.GetBrushHardness();
                var newHardness = currentHardness + (e.Delta > 0 ? 5 : -5);
                newHardness = Math.Max(0, Math.Min(100, newHardness));
                drawingManager.SetBrushHardness(newHardness);
                e.Handled = true;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Пропускаем обработку если фокус на TextBox или ComboBox
            if (e.OriginalSource is System.Windows.Controls.TextBox || 
                e.OriginalSource is System.Windows.Controls.ComboBox)
            {
                return;
            }

            // Space для панорамирования
            if (e.Key == Key.Space && !isSpacePressed)
            {
                isSpacePressed = true;
                e.Handled = true;
                return;
            }

            // Горячие клавиши инструментов
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.V) { drawingManager.SetTool(ToolsPanel.ToolType.SelectionMove); e.Handled = true; return; }
                var tool = HotkeyManager.GetToolFromKey(e.Key);
                if (tool != ToolsPanel.ToolType.Brush)
                {
                    drawingManager.SetTool(tool);
                    e.Handled = true;
                    return;
                }
            }

            // Undo/Redo и другие горячие клавиши
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Z) { drawingManager.Undo(); e.Handled = true; return; }
                else if (e.Key == Key.Y) { drawingManager.Redo(); e.Handled = true; return; }
                else if (e.Key == Key.D) { drawingManager.ResetBrushSettings(); e.Handled = true; return; }
                else if (e.Key == Key.X) { drawingManager.SwapColors(); e.Handled = true; return; }
                else if (e.Key == Key.R) { drawingManager.ResetBrushSettings(); e.Handled = true; return; }
                else if (e.Key == Key.W) { drawingManager.SetTool(ToolsPanel.ToolType.MagicWand); e.Handled = true; return; }
                else if (e.Key == Key.F7)
                {
                    mainWindow.PublicShowLayersWindow();
                    e.Handled = true; return;
                }
                // Зум горячими клавишами
                else if (e.Key == Key.Add || e.Key == Key.OemPlus)
                {
                    viewportManager.ZoomIn(new Point(0, 0));
                    e.Handled = true; return;
                }
                else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
                {
                    viewportManager.ZoomOut(new Point(0, 0));
                    e.Handled = true; return;
                }
                else if (e.Key == Key.D0) { viewportManager.Reset(); e.Handled = true; return; }
            }

            // Горячие клавиши для новых функций (Ctrl+Shift)
            if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.N)
                {
                    mainWindow.PublicCreateNewLayer();
                    e.Handled = true; return;
                }
                else if (e.Key == Key.D)
                {
                    mainWindow.PublicDeleteLayer();
                    e.Handled = true; return;
                }
                else if (e.Key == Key.M)
                {
                    mainWindow.PublicMergeLayers();
                    e.Handled = true; return;
                }
                else if (e.Key == Key.P)
                {
                    mainWindow.PublicShowAdvancedColorPicker();
                    e.Handled = true; return;
                }
                else if (e.Key == Key.C)
                {
                    mainWindow.PublicShowColorPicker();
                    e.Handled = true; return;
                }
                else if (e.Key == Key.W)
                {
                    drawingManager.SetTool(ToolsPanel.ToolType.MagicWand);
                    e.Handled = true; return;
                }
            }

            // Быстрое изменение размера кисти
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.OemPlus) { drawingManager.ChangeBrushSize(1); e.Handled = true; return; }
                else if (e.Key == Key.OemMinus) { drawingManager.ChangeBrushSize(-1); e.Handled = true; return; }
                else if (e.Key == Key.X) { drawingManager.SwapColors(); e.Handled = true; return; }
            }

            // Функциональные клавиши
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.F7)
                {
                    mainWindow.PublicShowLayersWindow();
                    e.Handled = true; return;
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            // Отпускание Space
            if (e.Key == Key.Space)
            {
                isSpacePressed = false;
                if (isPanning)
                {
                    isPanning = false;
                }
                e.Handled = true;
            }
        }
    }
}