using Gtk;
using Launcher.Core;
using System;
using System.Threading.Tasks;
using Settings = Launcher.Core.Settings;

namespace Launcher.GTK
{
    internal class PageLoggedIn : VBox
    {
        private readonly MainWindow win;
        private readonly LoginData loginData;
        private readonly bool offline;
        private readonly Settings settings;

        public PageLoggedIn(MainWindow win, LoginData loginData, bool offline) : base(false, 5)
        {
            this.win = win;
            this.loginData = loginData;
            this.offline = offline;

            settings = Settings.Load();

            var titleLabel = new Label()
            {
                Markup = $"<span font-weight=\"bold\">{GLib.Markup.EscapeText(Core.Launcher.GameConfig.Name)}</span>",
                Halign = Align.Center
            };
            PackStart(titleLabel, false, false, 15);

            var bottomBox = new VBox
            {
                new Label("Xbox gamertag: " + loginData.GamerTag),
                new Label("Minecraft name: " + loginData.PlayerName)
            };
            bottomBox.MarginBottom = 20;
            Add(bottomBox);

            var buttonLaunch = new Button(offline ? "Play offline" : "Play")
            {
                Halign = Align.Center,
                MarginBottom = 10
            };
            buttonLaunch.Clicked += ButtonLaunch_Clicked;
            Add(buttonLaunch);

            var buttonSettings = new Button("Settings")
            {
                Halign = Align.Center
            };
            buttonSettings.Clicked += ButtonSettings_Clicked;
            Add(buttonSettings);

            var buttonLogout = new Button("Logout")
            {
                Halign = Align.Center
            };
            buttonLogout.Clicked += ButtonLogout_Clicked;
            Add(buttonLogout);

            //to speed up testing:
            /*Application.Invoke(delegate
            {
                buttonLaunch.Click();
            });*/
        }

        private void ButtonLaunch_Clicked(object? sender, EventArgs e)
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

        private void ButtonSettings_Clicked(object? sender, EventArgs e)
        {
            win.SetPage(new PageSettings(win, settings, this));
        }

        private void ButtonLogout_Clicked(object? sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await Login.LogoutAsync(win);
            });
        }
    }
}
