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
	[Register ("ViewErrorController")]
	partial class ViewErrorController
	{
		[Outlet]
		AppKit.NSTextField MessageLabel { get; set; }

		[Outlet]
		AppKit.NSTextField TitleLabel { get; set; }

		[Action ("Button_Clicked:")]
		partial void Button_Clicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}

			if (MessageLabel != null) {
				MessageLabel.Dispose ();
				MessageLabel = null;
			}
		}
	}
}
