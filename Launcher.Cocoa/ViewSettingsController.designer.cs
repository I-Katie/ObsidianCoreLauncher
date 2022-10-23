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
	[Register ("ViewSettingsController")]
	partial class ViewSettingsController
	{
		[Outlet]
		AppKit.NSButton CloseOnExitCheckBox { get; set; }

		[Outlet]
		AppKit.NSTextField JavaBinTextField { get; set; }

		[Outlet]
		AppKit.NSTextField JREArgsTextField { get; set; }

		[Action ("Browse_Clicked:")]
		partial void Browse_Clicked (Foundation.NSObject sender);

		[Action ("Cancel_Clicked:")]
		partial void Cancel_Clicked (Foundation.NSObject sender);

		[Action ("Ok_Clicked:")]
		partial void Ok_Clicked (Foundation.NSObject sender);

		[Action ("Reset_Clicked:")]
		partial void Reset_Clicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (JavaBinTextField != null) {
				JavaBinTextField.Dispose ();
				JavaBinTextField = null;
			}

			if (JREArgsTextField != null) {
				JREArgsTextField.Dispose ();
				JREArgsTextField = null;
			}

			if (CloseOnExitCheckBox != null) {
				CloseOnExitCheckBox.Dispose ();
				CloseOnExitCheckBox = null;
			}
		}
	}
}
