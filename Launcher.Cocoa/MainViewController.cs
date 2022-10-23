using System;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Launcher.Core;

namespace Launcher.Cocoa
{
    public partial class MainViewController : NSViewController, IPageControl
    {
        public MainViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Do any additional setup after loading the view.

            TitleLabel.StringValue = Core.Launcher.Title;

            ContainerView.TranslatesAutoresizingMaskIntoConstraints = false;
        }

        public override void ViewDidAppear()
        {
            base.ViewDidAppear();

            if (AppDelegate.StartupException == null)
                View.Window.Title = Core.Launcher.GameConfig.Name;
            else
                View.Window.Title = AppDelegate.StartupException.Title;

            View.Window.Center();

            SetPage(new ViewWaitController("Loading..."));

            if (AppDelegate.StartupException == null)
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
                    await ShowErrorPageAsync("Startup error", AppDelegate.StartupException.Message);
                    BeginInvokeOnMainThread(() =>
                    {
                        View.Window.Close();
                    });
                });
            }
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }

        private NSViewController currentController;

        internal void SetPage(NSViewController controller)
        {
            currentController?.View.RemoveFromSuperview();

            currentController = controller;

            controller.View.TranslatesAutoresizingMaskIntoConstraints = false;

            ContainerView.AddSubview(controller.View);
            NSLayoutConstraint.ActivateConstraints(new[] {
                controller.View.CenterXAnchor.ConstraintEqualToAnchor(ContainerView.CenterXAnchor),
                controller.View.CenterYAnchor.ConstraintEqualToAnchor(ContainerView.CenterYAnchor)
            });

            controller.View.ViewDidMoveToSuperview();
        }

        public void SetPageWait(string title)
        {
            InvokeOnMainThread(() => SetPage(new ViewWaitController(title)));
        }

        public void SetPageWaitWithCancel(string title, CancellationTokenSource cts)
        {
            InvokeOnMainThread(() => SetPage(new ViewWaitWithCancelController(title, cts)));
        }

        public void SetPageLogin()
        {
            InvokeOnMainThread(() => SetPage(new ViewLoginController(this)));
        }

        public void SetPageLoggedIn(LoginData loginData, bool offline)
        {
            InvokeOnMainThread(() => SetPage(new ViewLoggedInController(this, loginData, offline)));
        }

        public Task ShowErrorPageAsync(string title, string msg)
        {
            ViewErrorController controllerError = null;
            InvokeOnMainThread(() =>
            {
                controllerError = new ViewErrorController(title, msg);
                SetPage(controllerError);
            });
            return controllerError.Task;
        }

        public IDownloadProgressPage SetPageDownloadProgress(string title, int maximum)
        {
            ViewDownloadProgressController pageProgress = null;
            InvokeOnMainThread(() =>
            {
                pageProgress = new ViewDownloadProgressController(title, maximum);
                SetPage(pageProgress);
            });
            return pageProgress;
        }
    }
}
