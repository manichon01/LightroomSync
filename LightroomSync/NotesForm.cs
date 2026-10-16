using System.Reflection;

namespace LightroomSync
{
    // The important setup warnings: shown on first run, and from Help > Important Notes.
    // The same text is shown by the installer (InfoBeforeFile in InnoSetup.iss).
    internal class NotesForm : Form
    {
        public NotesForm(Font baseFont, Icon? icon)
        {
            Text = "Important notes";
            Font = baseFont;
            Icon = icon;
            ClientSize = new Size(720, 560);
            MinimumSize = new Size(480, 360);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            var text = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Text = Load().Replace("\r\n", "\n").Replace("\n", "\r\n"),
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
                TabStop = false,
            };
            var ok = new Button { Text = "I understand", AutoSize = true, Padding = new Padding(12, 4, 12, 4), DialogResult = DialogResult.OK };
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
            bottom.Controls.Add(ok);

            // Fill first, then the docked button bar.
            Controls.Add(text);
            Controls.Add(bottom);
            AcceptButton = ok;
            Shown += (_, _) => text.SelectionStart = 0;
        }

        private static string Load()
        {
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LightroomSync.ImportantNotes.txt");
            if (stream == null)
            {
                return "The notes are missing from this build. See Help > User Guide.";
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
