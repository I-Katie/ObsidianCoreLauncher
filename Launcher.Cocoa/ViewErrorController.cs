using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using System.Threading.Tasks;

namespace Launcher.Cocoa
{
    public partial class ViewErrorController : AppKit.NSViewController
    {
        private readonly string title;
        private readonly string msg;

        private readonly TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        internal Task Task => tcs.Task;

        #region Constructors

        // Called when created from unmanaged code
        public ViewErrorController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewErrorController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewErrorController(string title, string msg) : base("ViewError", NSBundle.MainBundle)
        {
            Initialize();

            this.title = title;
            this.msg = msg;
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
            MessageLabel.StringValue = msg;
        }

        partial void Button_Clicked(NSObject sender)
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(true);
        }
    }
}
