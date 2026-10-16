namespace LightroomSync
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            rootLayout = new TableLayoutPanel();
            headerPanel = new TableLayoutPanel();
            statusDotLabel = new Label();
            statusLabel = new Label();
            statusDetailLabel = new Label();
            progressBar = new ProgressBar();
            labelLocal = new Label();
            localFolderTextBox = new TextBox();
            buttonSelectLocalFolder = new Button();
            labelNetwork = new Label();
            networkFolderTextBox = new TextBox();
            buttonSelectNetworkFolder = new Button();
            actionsPanel = new FlowLayoutPanel();
            buttonSyncNow = new Button();
            buttonUploadNow = new Button();
            buttonCatalogs = new Button();
            labelEvents = new Label();
            eventsTextBox = new TextBox();
            timer1 = new System.Windows.Forms.Timer(components);
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            launchAtStartupToolStripMenuItem = new ToolStripMenuItem();
            autoCheckForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            minimizeToTrayToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            submitABugToolStripMenuItem = new ToolStripMenuItem();
            gitHubPageToolStripMenuItem = new ToolStripMenuItem();
            checkForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            rootLayout.SuspendLayout();
            headerPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            //
            // rootLayout: stacked rows; the events box takes all remaining space
            //
            rootLayout.ColumnCount = 2;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rootLayout.Controls.Add(headerPanel, 0, 0);
            rootLayout.Controls.Add(labelLocal, 0, 1);
            rootLayout.Controls.Add(localFolderTextBox, 0, 2);
            rootLayout.Controls.Add(buttonSelectLocalFolder, 1, 2);
            rootLayout.Controls.Add(labelNetwork, 0, 3);
            rootLayout.Controls.Add(networkFolderTextBox, 0, 4);
            rootLayout.Controls.Add(buttonSelectNetworkFolder, 1, 4);
            rootLayout.Controls.Add(actionsPanel, 0, 5);
            rootLayout.Controls.Add(labelEvents, 0, 6);
            rootLayout.Controls.Add(eventsTextBox, 0, 7);
            rootLayout.SetColumnSpan(headerPanel, 2);
            rootLayout.SetColumnSpan(labelLocal, 2);
            rootLayout.SetColumnSpan(labelNetwork, 2);
            rootLayout.SetColumnSpan(actionsPanel, 2);
            rootLayout.SetColumnSpan(labelEvents, 2);
            rootLayout.SetColumnSpan(eventsTextBox, 2);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new Padding(20, 12, 20, 20);
            rootLayout.RowCount = 8;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            //
            // headerPanel: status dot, headline and detail line
            //
            headerPanel.AutoSize = true;
            headerPanel.ColumnCount = 2;
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerPanel.Controls.Add(statusDotLabel, 0, 0);
            headerPanel.Controls.Add(statusLabel, 1, 0);
            headerPanel.Controls.Add(statusDetailLabel, 1, 1);
            headerPanel.Controls.Add(progressBar, 1, 2);
            headerPanel.SetRowSpan(statusDotLabel, 2);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Margin = new Padding(0, 0, 0, 16);
            headerPanel.Name = "headerPanel";
            headerPanel.RowCount = 3;
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            //
            // statusDotLabel
            //
            statusDotLabel.Anchor = AnchorStyles.Left;
            statusDotLabel.AutoSize = true;
            statusDotLabel.Font = new Font("Segoe UI", 24F);
            statusDotLabel.ForeColor = Color.Gray;
            statusDotLabel.Margin = new Padding(0, 0, 8, 0);
            statusDotLabel.Name = "statusDotLabel";
            statusDotLabel.Text = "\u25CF";
            //
            // statusLabel
            //
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            statusLabel.Margin = new Padding(0, 4, 0, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Text = "Starting...";
            //
            // statusDetailLabel
            //
            statusDetailLabel.AutoSize = true;
            statusDetailLabel.ForeColor = SystemColors.GrayText;
            statusDetailLabel.Margin = new Padding(0);
            statusDetailLabel.Name = "statusDetailLabel";
            statusDetailLabel.Text = " ";
            //
            // progressBar: hidden unless a transfer is running; marquee when the duration is unknown
            //
            progressBar.Dock = DockStyle.Fill;
            progressBar.Margin = new Padding(0, 8, 0, 0);
            progressBar.Maximum = 1000;
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(100, 8);
            progressBar.Visible = false;
            //
            // labelLocal
            //
            labelLocal.AutoSize = true;
            labelLocal.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelLocal.Margin = new Padding(0, 0, 0, 4);
            labelLocal.Name = "labelLocal";
            labelLocal.Text = "Local catalog folder";
            //
            // localFolderTextBox
            //
            localFolderTextBox.Dock = DockStyle.Fill;
            localFolderTextBox.Margin = new Padding(0, 0, 8, 16);
            localFolderTextBox.Name = "localFolderTextBox";
            localFolderTextBox.TabIndex = 1;
            localFolderTextBox.TextChanged += localFolderTextBox_TextChanged;
            //
            // buttonSelectLocalFolder
            //
            buttonSelectLocalFolder.AutoSize = true;
            buttonSelectLocalFolder.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonSelectLocalFolder.Margin = new Padding(0, 0, 0, 14);
            buttonSelectLocalFolder.MinimumSize = new Size(96, 0);
            buttonSelectLocalFolder.Name = "buttonSelectLocalFolder";
            buttonSelectLocalFolder.Padding = new Padding(8, 2, 8, 2);
            buttonSelectLocalFolder.TabIndex = 2;
            buttonSelectLocalFolder.Text = "Browse...";
            buttonSelectLocalFolder.UseVisualStyleBackColor = true;
            buttonSelectLocalFolder.Click += buttonSelectLocalFolder_Click;
            //
            // labelNetwork
            //
            labelNetwork.AutoSize = true;
            labelNetwork.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelNetwork.Margin = new Padding(0, 0, 0, 4);
            labelNetwork.Name = "labelNetwork";
            labelNetwork.Text = "Network folder (shared by all your computers)";
            //
            // networkFolderTextBox
            //
            networkFolderTextBox.Dock = DockStyle.Fill;
            networkFolderTextBox.Margin = new Padding(0, 0, 8, 16);
            networkFolderTextBox.Name = "networkFolderTextBox";
            networkFolderTextBox.TabIndex = 3;
            networkFolderTextBox.Text = "P:\\Lightroom";
            networkFolderTextBox.TextChanged += networkFolderTextBox_TextChanged;
            //
            // buttonSelectNetworkFolder
            //
            buttonSelectNetworkFolder.AutoSize = true;
            buttonSelectNetworkFolder.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonSelectNetworkFolder.Margin = new Padding(0, 0, 0, 14);
            buttonSelectNetworkFolder.MinimumSize = new Size(96, 0);
            buttonSelectNetworkFolder.Name = "buttonSelectNetworkFolder";
            buttonSelectNetworkFolder.Padding = new Padding(8, 2, 8, 2);
            buttonSelectNetworkFolder.TabIndex = 4;
            buttonSelectNetworkFolder.Text = "Browse...";
            buttonSelectNetworkFolder.UseVisualStyleBackColor = true;
            buttonSelectNetworkFolder.Click += buttonSelectNetworkFolder_Click;
            //
            // actionsPanel
            //
            actionsPanel.AutoSize = true;
            actionsPanel.Controls.Add(buttonSyncNow);
            actionsPanel.Controls.Add(buttonUploadNow);
            actionsPanel.Controls.Add(buttonCatalogs);
            actionsPanel.Dock = DockStyle.Top;
            actionsPanel.Margin = new Padding(0, 0, 0, 12);
            actionsPanel.Name = "actionsPanel";
            //
            // buttonSyncNow
            //
            buttonSyncNow.AutoSize = false;
            buttonSyncNow.Size = new Size(130, 36);
            buttonSyncNow.Margin = new Padding(0, 0, 8, 0);
            buttonSyncNow.Name = "buttonSyncNow";
            buttonSyncNow.Padding = new Padding(8, 2, 8, 2);
            buttonSyncNow.TabIndex = 5;
            buttonSyncNow.Text = "Sync now";
            buttonSyncNow.UseVisualStyleBackColor = true;
            buttonSyncNow.Click += buttonSyncNow_Click;
            //
            // buttonUploadNow
            //
            buttonUploadNow.AutoSize = false;
            buttonUploadNow.Size = new Size(130, 36);
            buttonUploadNow.Margin = new Padding(0);
            buttonUploadNow.Name = "buttonUploadNow";
            buttonUploadNow.Padding = new Padding(8, 2, 8, 2);
            buttonUploadNow.TabIndex = 6;
            buttonUploadNow.Text = "Upload now";
            buttonUploadNow.UseVisualStyleBackColor = true;
            buttonUploadNow.Click += buttonUploadNow_Click;
            //
            // buttonCatalogs
            //
            buttonCatalogs.AutoSize = false;
            buttonCatalogs.Margin = new Padding(8, 0, 0, 0);
            buttonCatalogs.Name = "buttonCatalogs";
            buttonCatalogs.Size = new Size(130, 36);
            buttonCatalogs.TabIndex = 7;
            buttonCatalogs.Text = "Catalogs...";
            buttonCatalogs.UseVisualStyleBackColor = true;
            buttonCatalogs.Click += buttonCatalogs_Click;
            //
            // labelEvents
            //
            labelEvents.AutoSize = true;
            labelEvents.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            labelEvents.Margin = new Padding(0, 0, 0, 4);
            labelEvents.Name = "labelEvents";
            labelEvents.Text = "Events";
            //
            // eventsTextBox
            //
            eventsTextBox.Dock = DockStyle.Fill;
            eventsTextBox.Font = new Font("Consolas", 9.5F);
            eventsTextBox.Margin = new Padding(0);
            eventsTextBox.Multiline = true;
            eventsTextBox.Name = "eventsTextBox";
            eventsTextBox.ReadOnly = true;
            eventsTextBox.ScrollBars = ScrollBars.Vertical;
            eventsTextBox.TabIndex = 8;
            //
            // timer1
            //
            timer1.Enabled = true;
            timer1.Interval = 5000;
            timer1.Tick += timer1_Tick;
            //
            // menuStrip1
            //
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.TabIndex = 10;
            menuStrip1.Text = "menuStrip1";
            //
            // fileToolStripMenuItem
            //
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { launchAtStartupToolStripMenuItem, autoCheckForUpdatesToolStripMenuItem, minimizeToTrayToolStripMenuItem, settingsToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Text = "File";
            //
            // launchAtStartupToolStripMenuItem
            //
            launchAtStartupToolStripMenuItem.Name = "launchAtStartupToolStripMenuItem";
            launchAtStartupToolStripMenuItem.Text = "Launch At Startup";
            launchAtStartupToolStripMenuItem.Click += launchAtStartupToolStripMenuItem_Click;
            //
            // autoCheckForUpdatesToolStripMenuItem
            //
            autoCheckForUpdatesToolStripMenuItem.Name = "autoCheckForUpdatesToolStripMenuItem";
            autoCheckForUpdatesToolStripMenuItem.Text = "Auto Check For Updates";
            autoCheckForUpdatesToolStripMenuItem.Click += autoCheckForUpdatesToolStripMenuItem_Click;
            //
            // minimizeToTrayToolStripMenuItem
            //
            minimizeToTrayToolStripMenuItem.Name = "minimizeToTrayToolStripMenuItem";
            minimizeToTrayToolStripMenuItem.Text = "Minimize To Tray";
            minimizeToTrayToolStripMenuItem.Click += minimizeToTrayToolStripMenuItem_Click;
            //
            // settingsToolStripMenuItem
            //
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Text = "Settings...";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            //
            // exitToolStripMenuItem
            //
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            //
            // helpToolStripMenuItem
            //
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { submitABugToolStripMenuItem, gitHubPageToolStripMenuItem, checkForUpdatesToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Text = "Help";
            //
            // submitABugToolStripMenuItem
            //
            submitABugToolStripMenuItem.Name = "submitABugToolStripMenuItem";
            submitABugToolStripMenuItem.Text = "Submit A Bug";
            submitABugToolStripMenuItem.Click += submitABugToolStripMenuItem_Click;
            //
            // gitHubPageToolStripMenuItem
            //
            gitHubPageToolStripMenuItem.Name = "gitHubPageToolStripMenuItem";
            gitHubPageToolStripMenuItem.Text = "GitHub Page";
            gitHubPageToolStripMenuItem.Click += gitHubPageToolStripMenuItem_Click;
            //
            // checkForUpdatesToolStripMenuItem
            //
            checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            checkForUpdatesToolStripMenuItem.Text = "Check For Updates";
            checkForUpdatesToolStripMenuItem.Click += checkForUpdatesToolStripMenuItem_Click;
            //
            // aboutToolStripMenuItem
            //
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(760, 560);
            Controls.Add(rootLayout);
            Controls.Add(menuStrip1);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(620, 480);
            Name = "Form1";
            Text = "Lightroom Sync";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel rootLayout;
        private TableLayoutPanel headerPanel;
        private Label statusDotLabel;
        private Label statusLabel;
        private Label statusDetailLabel;
        private Label labelLocal;
        private TextBox localFolderTextBox;
        private Button buttonSelectLocalFolder;
        private Label labelNetwork;
        private TextBox networkFolderTextBox;
        private Button buttonSelectNetworkFolder;
        private FlowLayoutPanel actionsPanel;
        private Button buttonSyncNow;
        private Button buttonUploadNow;
        private Button buttonCatalogs;
        private ProgressBar progressBar;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private Label labelEvents;
        private TextBox eventsTextBox;
        private System.Windows.Forms.Timer timer1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem launchAtStartupToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem minimizeToTrayToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem submitABugToolStripMenuItem;
        private ToolStripMenuItem gitHubPageToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem checkForUpdatesToolStripMenuItem;
        private ToolStripMenuItem autoCheckForUpdatesToolStripMenuItem;
    }
}
