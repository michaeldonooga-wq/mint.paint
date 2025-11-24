using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MintPaint.Installer
{
    public partial class MainWindow : Window
    {
        private string defaultInstallPath;

        public MainWindow()
        {
            InitializeComponent();
            defaultInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mint Paint");
            InstallPathTextBox.Text = defaultInstallPath;
            SetLanguage();
        }

        private void SetLanguage()
        {
            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (lang == "ru")
            {
                Title = "Установка Mint Paint";
                SubtitleText.Text = "Графический редактор";
                WelcomeText.Text = "Добро пожаловать в установку Mint Paint!";
                PathLabel.Text = "Папка установки:";
                BrowseButton.Content = "Обзор...";
                DesktopShortcutCheckBox.Content = "Создать ярлык на рабочем столе";
                StartMenuShortcutCheckBox.Content = "Создать ярлык в меню Пуск";
                InstallButton.Content = "Установить";
                CancelButton.Content = "Отмена";
            }
            else
            {
                Title = "Mint Paint Setup";
                SubtitleText.Text = "Graphics Editor";
                WelcomeText.Text = "Welcome to Mint Paint Setup!";
                PathLabel.Text = "Installation folder:";
                BrowseButton.Content = "Browse...";
                DesktopShortcutCheckBox.Content = "Create desktop shortcut";
                StartMenuShortcutCheckBox.Content = "Create Start Menu shortcut";
                InstallButton.Content = "Install";
                CancelButton.Content = "Cancel";
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для установки",
                SelectedPath = InstallPathTextBox.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InstallPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var installPath = InstallPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(installPath))
            {
                MessageBox.Show("Укажите папку установки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InstallButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            InstallProgress.Visibility = Visibility.Visible;
            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            StatusText.Text = lang == "ru" ? "Установка..." : "Installing...";

            try
            {
                await Task.Run(() => InstallApplication(installPath));

                StatusText.Text = lang == "ru" ? "Установка завершена успешно!" : "Installation completed successfully!";
                MessageBox.Show(
                    lang == "ru" ? "Mint Paint успешно установлен!" : "Mint Paint installed successfully!",
                    lang == "ru" ? "Установка завершена" : "Installation Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (MessageBox.Show(
                    lang == "ru" ? "Запустить Mint Paint?" : "Launch Mint Paint?",
                    lang == "ru" ? "Установка завершена" : "Installation Complete",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(installPath, "mint.paint.exe"),
                        UseShellExecute = true
                    });
                }

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка установки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                InstallButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
                InstallProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void InstallApplication(string installPath)
        {
            // Сохраняем значения чекбоксов до входа в фоновый поток
            bool createDesktopShortcut = false;
            bool createStartMenuShortcut = false;
            
            Dispatcher.Invoke(() =>
            {
                createDesktopShortcut = DesktopShortcutCheckBox.IsChecked == true;
                createStartMenuShortcut = StartMenuShortcutCheckBox.IsChecked == true;
            });

            // Создаем папку установки
            Directory.CreateDirectory(installPath);

            // Копируем файлы из папки с установщиком
            var sourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
            
            if (!Directory.Exists(sourceDir))
            {
                // Пытаемся найти в родительской папке (для запуска из Visual Studio)
                sourceDir = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Files");
                if (!Directory.Exists(sourceDir))
                {
                    throw new Exception("Файлы для установки не найдены! Убедитесь, что папка Files находится рядом с установщиком.");
                }
            }

            CopyDirectory(sourceDir, installPath);

            // Создаем ярлыки
            if (createDesktopShortcut)
            {
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Mint Paint.lnk"),
                    Path.Combine(installPath, "mint.paint.exe"),
                    "Графический редактор Mint Paint"
                );
            }

            if (createStartMenuShortcut)
            {
                var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Mint Paint");
                Directory.CreateDirectory(startMenuPath);
                CreateShortcut(
                    Path.Combine(startMenuPath, "Mint Paint.lnk"),
                    Path.Combine(installPath, "mint.paint.exe"),
                    "Графический редактор Mint Paint"
                );
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                System.IO.File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string description)
        {
            try
            {
                var content = $"[InternetShortcut]\nURL=file:///{targetPath.Replace("\\", "/")}";
                System.IO.File.WriteAllText(shortcutPath.Replace(".lnk", ".url"), content);
            }
            catch { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
