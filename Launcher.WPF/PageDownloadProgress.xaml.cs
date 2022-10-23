using Launcher.Core;
using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageDownloadProgress.xaml
    /// </summary>
    public partial class PageDownloadProgress : Page, IDownloadProgressPage
    {
        public PageDownloadProgress(string title, int maxValue)
        {
            InitializeComponent();

            titleLabel.Content = title;
            nameLabel.Content = "";
            progressBar.Maximum = maxValue;
        }

        public string CurrentFileName
        {
            set => Dispatcher.Invoke(() => nameLabel.Content = value);
        }

        public int Value
        {
            get => Dispatcher.Invoke(() => (int)progressBar.Value);
            set => Dispatcher.Invoke(() => progressBar.Value = value);
        }
    }
}
