using Gtk;
using System;
using System.Threading.Tasks;

namespace Launcher.GTK
{
    internal class PageError : VBox
    {
        private readonly TaskCompletionSource<bool> tcs = new();
        internal Task Task => tcs.Task;

        public PageError(string title, string msg) : base(false, 15)
        {
            var titleLabel = new Label()
            {
                Markup = $"<span font-weight=\"bold\" foreground=\"red\">{GLib.Markup.EscapeText(title)}</span>",
                Halign = Align.Center
            };
            Add(titleLabel);

            var msgLabel = new Label(msg)
            {
                Halign = Align.Center
            };
            // TODO: this stretches the window but then wraps and justifies to the original size
            msgLabel.LineWrap = true;
            msgLabel.Justify = Justification.Center;
            Add(msgLabel);

            var OkButton = new Button("Ok")
            {
                Halign = Align.Center,
                MarginTop = 10
            };
            OkButton.Clicked += OkButton_Clicked;
            Add(OkButton);
        }

        private void OkButton_Clicked(object? sender, EventArgs e)
        {
            if (!tcs.Task.IsCompleted)
                tcs.SetResult(true);
        }
    }
}
