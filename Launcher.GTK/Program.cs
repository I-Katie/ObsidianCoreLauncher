using Gtk;
using Launcher.Core;
using System;

namespace Launcher.GTK
{
    //using GTK3

    internal class Program
    {
        internal static StartupException? StartupException;

        [STAThread]
        static void Main(string[] args)
        {
            Application.Init();

            try
            {
                Core.Launcher.Init();
            }
            catch (StartupException ex)
            {
                StartupException = ex;
            }

            try
            {
                MainWindow win = new MainWindow();
                win.ShowAll();

                Application.Invoke(delegate
                {
                    win.Started();
                });

                Application.Run();
            }
            finally
            {
                Core.Launcher.Exit();
            }
        }
    }
}
