using Gtk;
using Launcher.Core;
using System;
using Settings = Launcher.Core.Settings;

namespace Launcher.GTK
{
    internal class PageSettings : VBox
    {
        private readonly MainWindow win;
        private readonly Settings settings;
        private readonly Container returnTo;

        private readonly CheckButton closeOnExitCheckBox;
        private readonly Entry javaBinEntry;
        private readonly Entry jreArgsEntry;

        public PageSettings(MainWindow win, Settings settings, Container returnTo) : base(false, 10)
        {
            this.win = win;
            this.settings = settings;
            this.returnTo = returnTo;

            var titleLabel = new Label()
            {
                Markup = "<span font-weight=\"bold\">Settings</span>",
                Halign = Align.Center
            };
            PackStart(titleLabel, false, false, 10);

            // jvm bin

            PackStart(new Label("Java executable:"), false, false, 0);

            var javaBinBox = new HBox()
            {
                MarginStart = 50,
                MarginEnd = 50
            };
            javaBinEntry = new Entry(settings.JVMBinary);
            javaBinBox.PackStart(javaBinEntry, true, true, 0);

            var browseButton = new Button("Browse");
            browseButton.MarginStart = 5;
            browseButton.Clicked += BrowseButton_Clicked;
            javaBinBox.PackStart(browseButton, false, false, 0);

            PackStart(javaBinBox, false, false, 0);

            // jvm arguments

            PackStart(new Label("JVM arguments:"), false, false, 0);

            var vmArgsBox = new HBox()
            {
                MarginStart = 50,
                MarginEnd = 50
            };
            jreArgsEntry = new Entry(settings.JREArguments);
            vmArgsBox.PackStart(jreArgsEntry, true, true, 0);

            var resetButton = new Button("Reset");
            resetButton.MarginStart = 5;
            resetButton.Clicked += ResetButton_Clicked;
            vmArgsBox.PackStart(resetButton, false, false, 0);

            PackStart(vmArgsBox, false, false, 0);

            // close on exit

            closeOnExitCheckBox = new CheckButton("Close console window on game exit")
            {
                Halign = Align.Center,
                Active = settings.CloseOnExit,
            };
            PackStart(closeOnExitCheckBox, false, false, 5);

            // bottom box

            HBox boxBottom = new HBox();

            boxBottom.PackStart(new Label(), true, false, 0); //empty space

            var buttonOk = new Button("Ok");
            buttonOk.Clicked += ButtonOk_Clicked;
            boxBottom.PackStart(buttonOk, false, false, 5);

            var buttonCancel = new Button("Cancel");
            buttonCancel.Clicked += ButtonCancel_Clicked;
            boxBottom.PackStart(buttonCancel, false, false, 5);

            boxBottom.PackStart(new Label(), true, false, 0);  //empty space

            PackStart(boxBottom, false, false, 5);
        }

        private void ButtonOk_Clicked(object? sender, EventArgs e)
        {
            settings.JVMBinary = javaBinEntry.Text.Trim();
            settings.JREArguments = jreArgsEntry.Text.Trim();
            settings.CloseOnExit = closeOnExitCheckBox.Active;
            settings.Save();

            win.SetPage(returnTo);
        }

        private void ButtonCancel_Clicked(object? sender, EventArgs e)
        {
            win.SetPage(returnTo);
        }

        private void BrowseButton_Clicked(object? sender, EventArgs e)
        {
            using (var dialog = new FileChooserDialog("Choose the java executable", win, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept))
            {
                if (dialog.Run() == (int)ResponseType.Accept)
                {
                    javaBinEntry.Text = dialog.Filename;
                }
            }
        }

        private void ResetButton_Clicked(object? sender, EventArgs e)
        {
            jreArgsEntry.Text = GameLauncher.VMDefaults;
        }
    }
}
