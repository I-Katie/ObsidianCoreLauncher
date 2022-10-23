using Launcher.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IPageControl
    {
        public MainWindow()
        {
            InitializeComponent();

            if (App.StartupException == null)
                Title = Core.Launcher.GameConfig.Name;
            else
                Title = App.StartupException.Title;

            labelName.Content = Core.Launcher.Title;
        }

        internal void SetPage(Page page)
        {
            frame.Navigate(page);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetPage(new PageWait("Loading..."));

            if (App.StartupException == null)
            {
                // start normally
                await Task.Run(async () =>
                {
                    try
                    {
                        await Login.AutoLoginAsync(this);
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorPageAsync("Error", ex.Message);
                        SetPageLogin();
                    }
                });
            }
            else
            {
                await ShowErrorPageAsync("Startup error", App.StartupException.Message);
                App.Current.Shutdown();
            }
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            frame.NavigationService.RemoveBackEntry();
        }

        // IWindow

        public void SetPageWait(string title)
        {
            Dispatcher.Invoke(() => SetPage(new PageWait(title)));
        }

        public void SetPageWaitWithCancel(string title, CancellationTokenSource cts)
        {
            Dispatcher.Invoke(() => SetPage(new PageWaitWithCancel(title, cts)));
        }

        public void SetPageLogin()
        {
            Dispatcher.Invoke(() => SetPage(new PageLogin(this)));
        }

        public void SetPageLoggedIn(LoginData loginData, bool offline)
        {
            Dispatcher.Invoke(() => SetPage(new PageLoggedIn(this, loginData, offline)));
        }

        public Task ShowErrorPageAsync(string title, string msg)
        {
            return Dispatcher.Invoke(() =>
            {
                var pageError = new PageError(title, msg);
                SetPage(pageError);
                return pageError.Task;
            });
        }

        public IDownloadProgressPage SetPageDownloadProgress(string title, int maximum)
        {
            return Dispatcher.Invoke(() =>
            {
                var pdp = new PageDownloadProgress(title, maximum);
                SetPage(pdp);
                return pdp;
            });
        }
    }
}
