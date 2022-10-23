using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageError.xaml
    /// </summary>
    public partial class PageError : Page
    {
        private readonly TaskCompletionSource<bool> tcs = new();
        internal Task Task => tcs.Task;

        public PageError(string title, string msg)
        {
            InitializeComponent();

            titleLabel.Content = title;
            titleLabel.FontSize += 2;
            titleLabel.FontWeight = FontWeights.Bold;

            messageTextBlock.Text = msg;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(true);
        }
    }
}
