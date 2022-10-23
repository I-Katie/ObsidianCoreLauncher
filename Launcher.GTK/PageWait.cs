using Gtk;

namespace Launcher.GTK
{
    internal class PageWait : VBox
    {
        internal PageWait(string title) : base(false, 5)
        {
            var titleLabel = new Label()
            {
                Markup = $"<span font-weight=\"bold\">{GLib.Markup.EscapeText(title)}</span>",
                Halign = Align.Center
            };
            Add(titleLabel);

            var spinner = new Spinner()
            {
                Active = true,
                Halign = Align.Center
            };
            spinner.SetSizeRequest(50, 50);
            Add(spinner);
        }
    }
}
