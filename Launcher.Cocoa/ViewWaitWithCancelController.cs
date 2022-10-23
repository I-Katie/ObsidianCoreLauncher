using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using System.Threading;

namespace Launcher.Cocoa
{
    public partial class ViewWaitWithCancelController : AppKit.NSViewController
    {
        private readonly string title;
        private readonly CancellationTokenSource tokenSource;

        #region Constructors

        // Called when created from unmanaged code
        public ViewWaitWithCancelController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewWaitWithCancelController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewWaitWithCancelController(string title, CancellationTokenSource tokenSource) : base("ViewWaitWithCancel", NSBundle.MainBundle)
        {
            Initialize();

            this.title = title;
            this.tokenSource = tokenSource;
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

        partial void Button_Pressed(NSObject sender)
        {
            tokenSource.Cancel();
        }
    }
}
