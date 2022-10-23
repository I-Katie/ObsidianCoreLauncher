using Gtk;
using Launcher.Core;

namespace Launcher.GTK
{
    internal class PageDownloadProgress : VBox, IDownloadProgressPage
    {
        private readonly Label currentFileLabel;
        private readonly ProgressBar progressBar;
        private readonly float maximum;
        private int value;

        public PageDownloadProgress(string title, int maximum)
        {
            var titleLabel = new Label()
            {
                Markup = $"<span font-weight=\"bold\">{GLib.Markup.EscapeText(title)}</span>",
                Halign = Align.Center,
                MarginBottom = 20
            };
            Add(titleLabel);

            currentFileLabel = new Label("name");
            Add(currentFileLabel);

            progressBar = new ProgressBar()
            {
                MarginTop = 10,
                MarginStart = 50,
                MarginEnd = 50
            };
            Add(progressBar);

            value = 0;
            this.maximum = maximum;
        }

        public int Value
        {
            get => Dispatcher.Invoke(() => value);
            set => Dispatcher.Invoke(() =>
            {
                this.value = value;
                progressBar.Fraction = this.value / maximum;
            });
        }

        public string CurrentFileName
        {
            set => Dispatcher.Invoke(() => currentFileLabel.Text = value);
        }
    }
}
