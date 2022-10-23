using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Cocoa
{
    public partial class ViewLoginController : AppKit.NSViewController
    {
        private readonly MainViewController win;

        #region Constructors

        // Called when created from unmanaged code
        public ViewLoginController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewLoginController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewLoginController(MainViewController win) : base("ViewLogin", NSBundle.MainBundle)
        {
            Initialize();
            this.win = win;
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
        }

        partial void Button_Clicked(NSObject sender)
        {
            win.SetPage(new ViewWaitController("Logging in..."));
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
