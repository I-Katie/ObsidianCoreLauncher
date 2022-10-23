using Launcher.Core;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageLogin.xaml
    /// </summary>
    public partial class PageLogin : Page
    {
        private readonly MainWindow win;

        public PageLogin(MainWindow win)
        {
            InitializeComponent();

            this.win = win;

            titleLabel.Content = Core.Launcher.GameConfig.Name;
            titleLabel.FontSize += 4;
        }

        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                try
                {
                    await Login.LoginAsync(win);
                }
                catch (Exception ex)
                {
                    await win.ShowErrorPageAsync("Error", ex.Message);
                    win.SetPageLogin();
                }
            });
        }
    }
}
