using System.IO;
using AppKit;
using Foundation;
using Launcher.Core;

namespace Launcher.Cocoa
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public static StartupException StartupException;

        public AppDelegate()
        {
            try
            {
                string baseDir;
#if !DEBUG
                baseDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..");
#else
                baseDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "..", "shared");
#endif
                Core.Launcher.Init(baseDir);
            }
            catch (StartupException ex)
            {
                StartupException = ex;
            }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
            Core.Launcher.Exit();
        }

        public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
        {
            return true;
        }
    }
}
