namespace LightroomSync
{
    internal class SettingsForm : Form
    {
        private readonly Config config;
        private readonly NumericUpDown intervalBox = new() { Minimum = 1, Maximum = 600 };
        private readonly NumericUpDown backupsBox = new() { Minimum = 1, Maximum = 20 };
        private readonly CheckBox notificationsBox = new() { Text = "Show notifications", AutoSize = true };

        public SettingsForm(Config config)
        {
            this.config = config;
            Text = "Settings";
            Font = SystemFonts.MessageBoxFont;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Padding = new Padding(16);

            intervalBox.Value = Math.Clamp(config.CheckIntervalSeconds, 1, 600);
            backupsBox.Value = Math.Clamp(config.BackupsToKeep, 1, 20);
            notificationsBox.Checked = config.Notifications;

            var ok = new Button { Text = "OK", AutoSize = true, Padding = new Padding(8, 2, 8, 2), DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", AutoSize = true, Padding = new Padding(8, 2, 8, 2), DialogResult = DialogResult.Cancel };
            ok.Click += (_, _) => Save();

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2 };
            layout.Controls.Add(new Label { Text = "Check for changes every (seconds)", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 16, 8) }, 0, 0);
            layout.Controls.Add(intervalBox, 1, 0);
            layout.Controls.Add(new Label { Text = "Previous versions to keep per catalog", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 16, 8) }, 0, 1);
            layout.Controls.Add(backupsBox, 1, 1);
            layout.Controls.Add(notificationsBox, 0, 2);
            layout.SetColumnSpan(notificationsBox, 2);
            layout.Controls.Add(buttons, 0, 3);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void Save()
        {
            config.CheckIntervalSeconds = (int)intervalBox.Value;
            config.BackupsToKeep = (int)backupsBox.Value;
            config.Notifications = notificationsBox.Checked;
        }
    }
}
