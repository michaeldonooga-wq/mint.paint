using SkiaSharp;
using System;
using System.Windows.Data;
using System.Windows.Media;

namespace mint.paint
{
    public class SKColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SKColor skColor)
            {
                return new SolidColorBrush(Color.FromRgb(skColor.Red, skColor.Green, skColor.Blue));
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}