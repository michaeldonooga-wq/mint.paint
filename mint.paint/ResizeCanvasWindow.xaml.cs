using System.Windows;

namespace mint.paint
{
    public partial class ResizeCanvasWindow : Window
    {
        public int NewWidth { get; private set; }
        public int NewHeight { get; private set; }

        public ResizeCanvasWindow(int currentWidth, int currentHeight)
        {
            InitializeComponent();
            WidthTextBox.Text = currentWidth.ToString();
            HeightTextBox.Text = currentHeight.ToString();
            
            var S = Localization.Strings.Get;
            Title = S("ResizeCanvasTitle");
            WidthLabel.Text = S("Width");
            HeightLabel.Text = S("Height");
            OKButton.Content = S("OK");
            CancelButton.Content = S("Cancel");
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(WidthTextBox.Text, out int width) && width > 0 &&
                int.TryParse(HeightTextBox.Text, out int height) && height > 0)
            {
                NewWidth = width;
                NewHeight = height;
                DialogResult = true;
                Close();
            }
            else
            {
                var S = Localization.Strings.Get;
                MessageBox.Show(S("EnterValidSize"), S("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
