using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using Launcher.Core;
using System.Threading.Tasks;

namespace Launcher.Cocoa
{
    public partial class ViewLoggedInController : AppKit.NSViewController
    {
        private readonly MainViewController win;
        private readonly LoginData loginData;
        private readonly bool offline;
        private readonly Settings settings;

        #region Constructors

        // Called when created from unmanaged code
        public ViewLoggedInController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewLoggedInController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewLoggedInController(MainViewController win, LoginData loginData, bool offline) : base("ViewLoggedIn", NSBundle.MainBundle)
        {
            Initialize();

            this.win = win;
            this.loginData = loginData;
            this.offline = offline;

            settings = Settings.Load();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.StringValue = Core.Launcher.GameConfig.Name;
            XboxLabel.StringValue = "Xbox gamertag: " + loginData.GamerTag;
            MinecraftLabel.StringValue = "Minecraft name: " + loginData.PlayerName;

            LaunchButton.Title = offline ? "Play offline" : "Play";
        }

        partial void Launch_Clicked(NSObject sender)
        {
            win.SetPage(new ViewWaitController("Launching game..."));
            Task.Run(async () =>
            {
                try
                {
                    await GameLauncher.LaunchAsync(win, settings, loginData, offline);
                    InvokeOnMainThread(() => win.View.Window.Close());
                }
                catch (LaunchException ex)
                {
                    await win.ShowErrorPageAsync(ex.Title, ex.Message);
                    InvokeOnMainThread(() => win.SetPage(this));
                }
            });
        }

        partial void Settings_Clicked(NSObject sender)
        {
            win.SetPage(new ViewSettingsController(win, settings, this));
        }

        partial void Logout_Clicked(NSObject sender)
        {
            Task.Run(async () =>
            {
                await Login.LogoutAsync(win);
            });
        }
    }
}
