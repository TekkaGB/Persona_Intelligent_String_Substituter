using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PersonaTextReplacer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {        
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            MainWindow mw = new();
            mw.Show();
        }
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occured:\n{e.Exception.Message}\n\nInner Exception:\n{e.Exception.InnerException}" +
                $"\n\nStack Trace:\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
                             MessageBoxImage.Error);

            e.Handled = true;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ((MainWindow)Current.MainWindow).ReplaceButton.IsEnabled = true;
                ((MainWindow)Current.MainWindow).InputButton.IsEnabled = true;
                ((MainWindow)Current.MainWindow).OutputButton.IsEnabled = true;
                ((MainWindow)Current.MainWindow).CaseCheckbox.IsEnabled = true;
                ((MainWindow)Current.MainWindow).WholeWordCheckbox.IsEnabled = true;
                ((MainWindow)Current.MainWindow).GameBox.IsEnabled = true;
            });
        }
    }
}
