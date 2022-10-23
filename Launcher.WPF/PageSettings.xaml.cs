using Launcher.Core;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        private readonly MainWindow win;
        private readonly Settings settings;
        private readonly Page returnTo;

        public PageSettings(MainWindow win, Settings settings, Page returnTo)
        {
            InitializeComponent();

            this.win = win;
            this.settings = settings;
            this.returnTo = returnTo;

            titleLabel.FontSize += 2;

            javaBinTextBox.Text = settings.JVMBinary;
            jreArgsTextBox.Text = settings.JREArguments;
            cbCloseOnExit.IsChecked = settings.CloseOnExit;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            settings.JVMBinary = javaBinTextBox.Text.Trim();
            settings.JREArguments = jreArgsTextBox.Text.Trim();
            settings.CloseOnExit = cbCloseOnExit.IsChecked == true;
            settings.Save();

            win.SetPage(returnTo);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            win.SetPage(returnTo);
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Choose the java executable";

            bool? result = dialog.ShowDialog(win);
            if (result == true)
            {
                javaBinTextBox.Text = dialog.FileName;
            }
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            jreArgsTextBox.Text = GameLauncher.VMDefaults;
        }
    }
}
