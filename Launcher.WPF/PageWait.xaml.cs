using System.Windows.Controls;

namespace Launcher.WPF
{
    /// <summary>
    /// Interaction logic for PageWait.xaml
    /// </summary>
    public partial class PageWait : Page
    {
        public PageWait(string title)
        {
            InitializeComponent();

            titleLabel.Content = title;
            titleLabel.FontSize += 2;
        }
    }
}
