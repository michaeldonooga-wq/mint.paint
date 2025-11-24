using System;
using System.Windows;
using System.Windows.Media.Animation;
using AutoUpdaterDotNET;

namespace mint.paint
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LocalizeUI();
            AppNameText.Opacity = 0;
            Loaded += (s, e) =>
            {
                var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1.5)) { BeginTime = TimeSpan.FromSeconds(0.5) };
                AppNameText.BeginAnimation(OpacityProperty, anim);
            };
        }

        private void LocalizeUI()
        {
            Title = Localization.Strings.Get("About_Title");
            VersionText.Text = Localization.Strings.Get("About_Version");
            DescriptionText.Text = Localization.Strings.Get("About_Description");
            CopyrightText.Text = Localization.Strings.Get("About_Copyright");
            CheckUpdateButton.Content = Localization.Strings.Get("CheckUpdates");
            OkButton.Content = Localization.Strings.Get("OK");
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = Localization.Strings.Get("CheckUpdates"),
                Width = 300,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240))
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var text = new System.Windows.Controls.TextBlock
            {
                Text = Localization.Strings.Get("CheckingUpdates"),
                FontSize = 14,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            System.Windows.Controls.Grid.SetRow(text, 0);

            var progress = new System.Windows.Controls.ProgressBar
            {
                IsIndeterminate = true,
                Height = 20,
                Margin = new Thickness(0, 15, 0, 0)
            };
            System.Windows.Controls.Grid.SetRow(progress, 2);

            grid.Children.Add(text);
            grid.Children.Add(progress);
            dialog.Content = grid;

            dialog.Show();

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                dialog.Close();
                AutoUpdater.Start("https://raw.githubusercontent.com/michaeldonooga-wq/mint.paint/main/update.xml");
            };
            timer.Start();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
