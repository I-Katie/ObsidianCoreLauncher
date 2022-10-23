using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core
{
    public interface IPageControl
    {
        void SetPageWait(string title);
        void SetPageWaitWithCancel(string title, CancellationTokenSource cts);
        void SetPageLogin();
        void SetPageLoggedIn(LoginData loginData, bool offline);
        Task ShowErrorPageAsync(string title, string msg);
        IDownloadProgressPage SetPageDownloadProgress(string title, int maximum);
    }

    public interface IDownloadProgressPage
    {
        int Value { get; set; }
        string CurrentFileName { set; }
    }
}
