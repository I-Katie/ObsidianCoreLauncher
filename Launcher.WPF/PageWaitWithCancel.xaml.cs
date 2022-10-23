using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageWaitWithCancel.xaml
    /// </summary>
    public partial class PageWaitWithCancel : Page
    {
        private readonly CancellationTokenSource tokenSource;

        public PageWaitWithCancel(string title, CancellationTokenSource tokenSource)
        {
            InitializeComponent();

            this.tokenSource = tokenSource;

            titleLabel.Content = title;
            titleLabel.FontSize += 2;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}
