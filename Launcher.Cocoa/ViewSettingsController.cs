using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using Launcher.Core;

namespace Launcher.Cocoa
{
    public partial class ViewSettingsController : AppKit.NSViewController
    {
        private MainViewController win;
        private Settings settings;
        private ViewLoggedInController returnTo;

        #region Constructors

        // Called when created from unmanaged code
        public ViewSettingsController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewSettingsController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewSettingsController(MainViewController win, Settings settings, ViewLoggedInController returnTo) : base("ViewSettings", NSBundle.MainBundle)
        {
            Initialize();

            this.win = win;
            this.settings = settings;
            this.returnTo = returnTo;
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            JavaBinTextField.StringValue = settings.JVMBinary;
            JREArgsTextField.StringValue = settings.JREArguments;
            CloseOnExitCheckBox.State = settings.CloseOnExit ? NSCellStateValue.On : NSCellStateValue.Off;
        }

        partial void Browse_Clicked(NSObject sender)
        {
            NSOpenPanel openPanel = new NSOpenPanel();
            openPanel.CanChooseDirectories = false;
            openPanel.CanChooseFiles = true;
            openPanel.AllowsMultipleSelection = false;
            if (openPanel.RunModal() == 1)
            {
                JavaBinTextField.StringValue = openPanel.Filename;
            }

        }

        partial void Reset_Clicked(NSObject sender)
        {
            JREArgsTextField.StringValue = GameLauncher.VMDefaults;
        }

        partial void Ok_Clicked(NSObject sender)
        {
            settings.JVMBinary = JavaBinTextField.StringValue.Trim();
            settings.JREArguments = JREArgsTextField.StringValue.Trim();
            settings.CloseOnExit = CloseOnExitCheckBox.State == NSCellStateValue.On;
            settings.Save();

            win.SetPage(returnTo);
        }

        partial void Cancel_Clicked(NSObject sender)
        {
            win.SetPage(returnTo);
        }
    }
}
