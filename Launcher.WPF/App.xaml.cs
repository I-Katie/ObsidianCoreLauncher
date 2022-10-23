using Launcher.Core;
using System.Windows;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static StartupException? StartupException;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                Core.Launcher.Init();
            }
            catch (StartupException ex)
            {
                StartupException = ex;
            }

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Core.Launcher.Exit();
        }
    }
}
