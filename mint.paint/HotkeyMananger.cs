using System.Windows.Input;

namespace mint.paint
{
    public static class HotkeyManager
    {
        public static ToolsPanel.ToolType GetToolFromKey(Key key)
        {
            return key switch
            {
                Key.B => ToolsPanel.ToolType.Brush,
                Key.E => ToolsPanel.ToolType.Eraser,
                Key.G => ToolsPanel.ToolType.Fill,
                Key.I => ToolsPanel.ToolType.Eyedropper,
                Key.L => ToolsPanel.ToolType.Line,
                Key.R => ToolsPanel.ToolType.Rectangle,
                Key.C => ToolsPanel.ToolType.Circle,
                Key.S => ToolsPanel.ToolType.Selection, // Новый инструмент - выделение
                _ => ToolsPanel.ToolType.Brush
            };
        }

        public static string GetToolHotkey(ToolsPanel.ToolType tool)
        {
            return tool switch
            {
                ToolsPanel.ToolType.Brush => "B",
                ToolsPanel.ToolType.Eraser => "E",
                ToolsPanel.ToolType.Fill => "G",
                ToolsPanel.ToolType.Eyedropper => "I",
                ToolsPanel.ToolType.Line => "L",
                ToolsPanel.ToolType.Rectangle => "R",
                ToolsPanel.ToolType.Circle => "C",
                ToolsPanel.ToolType.Selection => "S", // Новый инструмент - выделение
                _ => ""
            };
        }
    }
}