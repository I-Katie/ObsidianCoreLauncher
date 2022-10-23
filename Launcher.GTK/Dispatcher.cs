using Gtk;
using System;
using System.Threading.Tasks;
using Action = System.Action;

namespace Launcher.GTK
{
    // Runs stuff on the GTK gui thread.

    internal static class Dispatcher
    {
        internal static void Invoke(Action func)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Application.Invoke(delegate
            {
                try
                {
                    func.Invoke();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            tcs.Task.GetAwaiter().GetResult();
        }

        internal static T Invoke<T>(Func<T> func)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            Application.Invoke(delegate
            {
                try
                {
                    tcs.SetResult(func.Invoke());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task.GetAwaiter().GetResult();
        }
    }
}
