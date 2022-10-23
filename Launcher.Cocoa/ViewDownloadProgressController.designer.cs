// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Launcher.Cocoa
{
	[Register ("ViewDownloadProgressController")]
	partial class ViewDownloadProgressController
	{
		[Outlet]
		AppKit.NSTextField FileNameLabel { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator ProgressIndicator { get; set; }

		[Outlet]
		AppKit.NSTextField TitleLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}

			if (FileNameLabel != null) {
				FileNameLabel.Dispose ();
				FileNameLabel = null;
			}

			if (ProgressIndicator != null) {
				ProgressIndicator.Dispose ();
				ProgressIndicator = null;
			}
		}
	}
}
