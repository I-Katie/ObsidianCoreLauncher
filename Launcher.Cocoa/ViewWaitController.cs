using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Launcher.Cocoa
{
    public partial class ViewWaitController : AppKit.NSViewController
    {
        private readonly string title;

        #region Constructors

        // Called when created from unmanaged code
        public ViewWaitController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewWaitController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewWaitController(string title) : base("ViewWait", NSBundle.MainBundle)
        {
            Initialize();

            this.title = title;
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.StringValue = title;
            ProgressIndicator.StartAnimation(null);
        }
    }
}
