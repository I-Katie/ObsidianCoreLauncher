using Gtk;
using System;
using System.Threading;

namespace Launcher.GTK
{
    internal class PageWaitWithCancel : VBox
    {
        private readonly CancellationTokenSource tokenSource;

        public PageWaitWithCancel(string title, CancellationTokenSource tokenSource) : base(false, 5)
        {
            this.tokenSource = tokenSource;

            var label = new Label()
            {
                Markup = $"<span font-weight=\"bold\">{GLib.Markup.EscapeText(title)}</span>",
                Halign = Align.Center
            };
            Add(label);

            var spinner = new Spinner()
            {
                Active = true,
                Halign = Align.Center
            };
            spinner.SetSizeRequest(50, 50);
            Add(spinner);

            var cancelButton = new Button("Cancel")
            {
                Halign = Align.Center,
            };
            cancelButton.Clicked += CancelButton_Clicked;
            Add(cancelButton);
        }

        private void CancelButton_Clicked(object? sender, EventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}
