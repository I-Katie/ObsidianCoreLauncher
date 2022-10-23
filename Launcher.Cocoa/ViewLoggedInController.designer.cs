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
	[Register ("ViewLoggedInController")]
	partial class ViewLoggedInController
	{
		[Outlet]
		AppKit.NSButton LaunchButton { get; set; }

		[Outlet]
		AppKit.NSTextField MinecraftLabel { get; set; }

		[Outlet]
		AppKit.NSTextField TitleLabel { get; set; }

		[Outlet]
		AppKit.NSTextField XboxLabel { get; set; }

		[Action ("Launch_Clicked:")]
		partial void Launch_Clicked (Foundation.NSObject sender);

		[Action ("Logout_Clicked:")]
		partial void Logout_Clicked (Foundation.NSObject sender);

		[Action ("Settings_Clicked:")]
		partial void Settings_Clicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (MinecraftLabel != null) {
				MinecraftLabel.Dispose ();
				MinecraftLabel = null;
			}

			if (LaunchButton != null) {
				LaunchButton.Dispose ();
				LaunchButton = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}

			if (XboxLabel != null) {
				XboxLabel.Dispose ();
				XboxLabel = null;
			}
		}
	}
}
