namespace LightroomSync
{
    internal record CatalogRow(string Name, string Local, string Network, string BackupText, bool HasBackup);

    // Lists local catalogs next to the last known network version and offers to restore the previous local version.
    internal class CatalogsForm : Form
    {
        private readonly Form1 owner;
        private readonly ListView list = new();
        private readonly Button restoreButton = new();

        public CatalogsForm(Form1 owner)
        {
            this.owner = owner;
            Text = "Catalogs";
            Font = owner.Font;
            Icon = owner.Icon;
            ClientSize = new Size(760, 380);
            MinimumSize = new Size(600, 300);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.MultiSelect = false;
            list.HideSelection = false;
            list.Columns.Add("Catalog", 220);
            list.Columns.Add("Local version", 150);
            list.Columns.Add("Network version", 150);
            list.Columns.Add("Backups", 200);
            list.SelectedIndexChanged += (_, _) =>
                restoreButton.Enabled = list.SelectedItems.Count == 1 && (bool)list.SelectedItems[0].Tag!;

            restoreButton.Text = "Restore previous version";
            restoreButton.AutoSize = true;
            restoreButton.Padding = new Padding(8, 2, 8, 2);
            restoreButton.Enabled = false;
            restoreButton.Click += async (_, _) => await Restore();

            var closeButton = new Button { Text = "Close", AutoSize = true, Padding = new Padding(8, 2, 8, 2), DialogResult = DialogResult.Cancel };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12),
            };
            buttons.Controls.Add(closeButton);
            buttons.Controls.Add(restoreButton);

            Controls.Add(list);
            Controls.Add(buttons);
            CancelButton = closeButton;
            Load += (_, _) => Reload();
        }

        private void Reload()
        {
            list.Items.Clear();
            foreach (CatalogRow row in owner.GetCatalogRows())
            {
                list.Items.Add(new ListViewItem(new[] { row.Name, row.Local, row.Network, row.BackupText }) { Tag = row.HasBackup });
            }
            restoreButton.Enabled = false;
        }

        private async Task Restore()
        {
            string name = list.SelectedItems[0].Text;
            var answer = MessageBox.Show(
                "Replace the current \"" + name + "\" catalog with its previous version?\n\n" +
                "The current catalog is kept as a backup, and the restored version is uploaded to the network share so your other computers follow.",
                "Restore previous version", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            string? error = await owner.RestoreBackup(name);
            if (error != null)
            {
                MessageBox.Show(error, "Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Reload();
        }
    }
}
