using Launcher.Core;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageLoggedIn.xaml
    /// </summary>
    public partial class PageLoggedIn : Page
    {
        private readonly MainWindow win;
        private readonly LoginData loginData;
        private readonly bool offline;
        private readonly Settings settings;

        public PageLoggedIn(MainWindow win, LoginData loginData, bool offline)
        {
            InitializeComponent();

            this.win = win;
            this.loginData = loginData;
            this.offline = offline;

            titleLabel.Content = Core.Launcher.GameConfig.Name;
            titleLabel.FontSize += 6;

            gamertagLabel.Content += loginData.GamerTag;
            profileNameLabel.Content += loginData.PlayerName;

            settings = Settings.Load();

            if (offline) launchButton.Content = "Play offline";
        }

        private async void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => Login.LogoutAsync(win));
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            win.SetPage(new PageSettings(win, settings, this));
        }

        private void ButtonLaunch_Click(object sender, RoutedEventArgs e)
        {
            win.SetPage(new PageWait("Launching game..."));
            Task.Run(async () =>
            {
                try
                {
                    await GameLauncher.LaunchAsync(win, settings, loginData, offline);
                    Dispatcher.Invoke(() => win.Close());
                }
                catch (LaunchException ex)
                {
                    await win.ShowErrorPageAsync(ex.Title, ex.Message);
                    Dispatcher.Invoke(() => win.SetPage(this));
                }
            });
        }
    }
}
