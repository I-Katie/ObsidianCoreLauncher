using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using Launcher.Core;

namespace Launcher.Cocoa
{
    public partial class ViewDownloadProgressController : AppKit.NSViewController, IDownloadProgressPage
    {
        private readonly string title;
        private readonly int maximum;
        private int value;

        #region Constructors

        // Called when created from unmanaged code
        public ViewDownloadProgressController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewDownloadProgressController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewDownloadProgressController(string title, int maximum) : base("ViewDownloadProgress", NSBundle.MainBundle)
        {
            Initialize();

            this.title = title;
            this.maximum = maximum;
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
            FileNameLabel.StringValue = "";

            value = 0;
            ProgressIndicator.MinValue = 0;
            ProgressIndicator.DoubleValue = value;
            ProgressIndicator.MaxValue = maximum;
        }

        public int Value
        {
            get
            {
                int val = default;
                InvokeOnMainThread(() => val = value);
                return val;
            }

            set
            {
                InvokeOnMainThread(() =>
                {
                    this.value = value;
                    ProgressIndicator.DoubleValue = this.value;
                });
            }
        }

        public string CurrentFileName
        {
            set => InvokeOnMainThread(() => FileNameLabel.StringValue = value);
        }
    }
}
