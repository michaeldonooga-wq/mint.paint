using System;
using System.Configuration;
using System.Data;
using System.Windows;
using AutoUpdaterDotNET;

namespace mint.paint
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var lang = mint.paint.Properties.Settings.Default.Language ?? "ru";
            Localization.Strings.SetLanguage(lang);
            System.Diagnostics.Debug.WriteLine($"Language set to: {lang}, Test: {Localization.Strings.Get("New")}");
            
            if (mint.paint.Properties.Settings.Default.AutoUpdate)
            {
                AutoUpdater.Start("https://raw.githubusercontent.com/michaeldonooga-wq/mint.paint/main/update.xml");
            }
        }

        public static void ChangeTheme(string theme)
        {
            var uri = new Uri($"pack://application:,,,/Themes/{theme}.xaml");
            var dict = new ResourceDictionary { Source = uri };
            
            foreach (var key in dict.Keys)
            {
                Current.Resources[key] = dict[key];
            }
        }
    }

}
