using Gtk;
using Launcher.Core;
using System;
using System.Threading.Tasks;

namespace Launcher.GTK
{
    internal class PageLogin : VBox
    {
        private readonly MainWindow win;

        public PageLogin(MainWindow win) : base(false, 20)
        {
            this.win = win;

            var title = new Label()
            {
                Markup = $"<span font-weight=\"bold\">{GLib.Markup.EscapeText(Core.Launcher.GameConfig.Name)}</span>",
                Halign = Align.Center
            };
            Add(title);

            var loginButton = new Button("Login")
            {
                Halign = Align.Center,
            };
            loginButton.Clicked += LoginButton_Clicked;
            Add(loginButton);
        }

        private void LoginButton_Clicked(object? sender, EventArgs e)
        {
            win.SetPage(new PageWait("Logging in..."));
            Task.Run(async () =>
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