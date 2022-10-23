using Gtk;
using Launcher.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.GTK
{
    internal class MainWindow : Window, IPageControl
    {
        private readonly Box pane;
        private readonly Label topLabel;
        private readonly Label bottomLabel;

        private static readonly Gdk.Size ClientSize = new Gdk.Size(620, 400);

        public MainWindow() : base("")
        {
            Resizable = false;

            if (Program.StartupException == null)
                Title = Core.Launcher.GameConfig.Name;
            else
                Title = Program.StartupException.Title;

            Fixed fixedPane = new Fixed();
            fixedPane.SetSizeRequest(ClientSize.Width, ClientSize.Height);

            pane = new VBox();
            pane.SetSizeRequest(ClientSize.Width, ClientSize.Height);
            fixedPane.Put(pane, 0, 0);

            topLabel = new Label
            {
                Markup = $"<span foreground=\"gray\">{GLib.Markup.EscapeText(Core.Launcher.Title)}</span>"
            };
            fixedPane.Put(topLabel, 10, 10);

            bottomLabel = new Label
            {
                Markup = $"<span foreground=\"gray\">{GLib.Markup.EscapeText("This launcher is unofficial and unaffiliated with Mojang or Microsoft.")}</span>"
            };
            bottomLabel.SizeAllocated += delegate (object o, SizeAllocatedArgs args)
            {
                var size = args.Allocation.Size;
                Application.Invoke(delegate
                {
                    fixedPane.Move(bottomLabel, fixedPane.AllocatedWidth - size.Width - 10, fixedPane.AllocatedHeight - size.Height - 10);
                });
            };
            fixedPane.Put(bottomLabel, 0, 0);

            Add(fixedPane);

            SetPosition(WindowPosition.Center);
        }

        protected override bool OnDeleteEvent(Gdk.Event evnt)
        {
            Application.Quit();
            return true;
        }

        internal void SetPage(Container page)
        {
            Application.Invoke(delegate
            {
                foreach (var child in pane.Children)
                    pane.Remove(child);
                pane.CenterWidget = page;
                page.ShowAll();
            });
        }

        internal void Started()
        {
            SetPage(new PageWait("Loading..."));

            if (Program.StartupException == null)
            {
                // start normally
                Task.Run(async () =>
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
                Task.Run(async () =>
                {
                    await ShowErrorPageAsync("Startup error", Program.StartupException.Message);
                    Dispatcher.Invoke(() => Application.Quit());
                });
            }
        }

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
            PageError? pageError = null;
            Dispatcher.Invoke(() =>
            {
                pageError = new PageError(title, msg);
                SetPage(pageError);
            });
            return pageError!.Task;
        }

        public IDownloadProgressPage SetPageDownloadProgress(string title, int maximum)
        {
            PageDownloadProgress? pageProgress = null;
            Dispatcher.Invoke(() =>
            {
                pageProgress = new PageDownloadProgress(title, maximum);
                SetPage(pageProgress);
            });
            return pageProgress!;
        }
    }
}
