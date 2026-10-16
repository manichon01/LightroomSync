using LightroomSync.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Principal;
using static LightroomSync.Alert;
using static System.Net.Mime.MediaTypeNames;

namespace LightroomSync
{
    public partial class Form1 : Form
    {
        private const string RepoUrl = "https://github.com/manichon01/LightroomSync";
        public string currentVersion = "1.1.0"; // <----- Make sure you always update latestVersion.txt as well!
                                                // Yes, I'm too lazy to pipe this in.

        private Config config = new Config();
        private Status status = new Status();

        private bool hasDealtWithLightroomOpen = false;

        private bool timerBeingHandled = false; //Used to handle async events in the timer potentially firing multiple times
        private bool networkWasDown = false;
        private readonly HashSet<string> failedCatalogs = new(); // zips that failed to sync; not retried until "Sync now"

        private List<string> lastKnownNetworkVersions = new(); // cached so the Catalogs dialog never touches the share

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;


        // Struct representing FLASHWINFO
        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        // Import the FlashWindowEx function from user32.dll
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);



        private void StopFlashing()
        {
            FLASHWINFO flashInfo = new FLASHWINFO
            {
                cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FLASHWINFO))),
                hwnd = Process.GetCurrentProcess().MainWindowHandle,
                dwFlags = 0, // Stop flashing
                uCount = 0,
                dwTimeout = 0
            };
            FlashWindowEx(ref flashInfo);
        }

        private void Log(string message)
        {
            AppLog.Write(message);
            if (eventsTextBox.InvokeRequired)
            {
                eventsTextBox.Invoke(new Action<string>(Log), message);
                return;
            }

            // Oldest first, newest at the bottom; keep the box from growing without bound.
            if (eventsTextBox.TextLength > 200_000)
            {
                eventsTextBox.Text = eventsTextBox.Text.Substring(100_000);
            }
            eventsTextBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
        }

        private static readonly Color StatusGreen = Color.FromArgb(46, 160, 67);
        private static readonly Color StatusAmber = Color.FromArgb(219, 148, 0);
        private static readonly Color StatusBlue = Color.FromArgb(47, 129, 247);
        private static readonly Color StatusRed = Color.FromArgb(218, 54, 51);

        private void SetStatus(string headline, Color color, string detail = " ")
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, Color, string>(SetStatus), headline, color, detail);
                return;
            }

            statusLabel.Text = headline;
            statusDotLabel.ForeColor = color;
            statusDetailLabel.Text = detail;
            trayIcon.Text = "LightroomSync - " + headline; // NotifyIcon.Text max is 127 chars
        }

        private void Notify(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string, ToolTipIcon>(Notify), title, text, icon);
                return;
            }
            if (config.Notifications)
            {
                trayIcon.ShowBalloonTip(5000, title, text, icon);
            }
        }

        // fraction == null shows a marquee bar for steps without measurable progress (zip, extract).
        private void SetProgress(double? fraction, string detail)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double?, string>(SetProgress), fraction, detail);
                return;
            }
            progressBar.Visible = true;
            progressBar.Style = fraction == null ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            if (fraction != null)
            {
                progressBar.Value = (int)Math.Clamp(fraction.Value * 1000, 0, 1000);
            }
            statusDetailLabel.Text = detail;
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return size.ToString(unit == 0 ? "0" : "0.0") + " " + units[unit];
        }

        private Task CopyFileAsync(string source, string destination, string label)
        {
            return Task.Run(() =>
            {
                using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20);
                using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
                long total = input.Length, done = 0;
                var buffer = new byte[1 << 20];
                int read, lastPercent = -1;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    done += read;
                    int percent = total > 0 ? (int)(done * 100 / total) : 0;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        SetProgress(total > 0 ? done / (double)total : 0, label + ": " + FormatSize(done) + " of " + FormatSize(total));
                    }
                }
            });
        }

        private static string[] CatalogNames(string catName) =>
            new[] { catName + ".lrcat", catName + ".lrcat-data", catName + " Helper.lrdata" };

        private string BackupRoot(string catName) => Path.Combine(config.LocalFolder, "LightroomSync Backup", catName);

        private string NewBackupDir(string catName) => Path.Combine(BackupRoot(catName), DateTime.Now.ToString("yyyyMMdd-HHmmss"));

        private string[] BackupDirs(string catName)
        {
            string root = BackupRoot(catName);
            return Directory.Exists(root) ? Directory.GetDirectories(root).OrderBy(d => Path.GetFileName(d)).ToArray() : Array.Empty<string>();
        }

        // Moves whichever catalog files exist in "from" into "to" (same volume, so these are renames).
        private static void MoveCatalogFiles(string from, string to, string catName, List<string>? moved)
        {
            foreach (string name in CatalogNames(catName))
            {
                string source = Path.Combine(from, name);
                if (File.Exists(source))
                {
                    File.Move(source, Path.Combine(to, name));
                }
                else if (Directory.Exists(source))
                {
                    Directory.Move(source, Path.Combine(to, name));
                }
                else
                {
                    continue;
                }
                moved?.Add(name);
            }
        }

        // Undo a half-finished swap: put the files that were parked in "savedDir" back into the local folder.
        private void PutBack(List<string> moved, string savedDir)
        {
            foreach (string name in moved)
            {
                string target = Path.Combine(config.LocalFolder, name);
                string saved = Path.Combine(savedDir, name);
                try
                {
                    if (File.Exists(target)) File.Delete(target);
                    if (Directory.Exists(target)) Directory.Delete(target, true);
                    if (File.Exists(saved)) File.Move(saved, target);
                    else if (Directory.Exists(saved)) Directory.Move(saved, target);
                }
                catch (Exception ex)
                {
                    Log("Could not restore " + name + ": " + ex.Message + ". Your old catalog is in " + savedDir);
                }
            }
        }

        private void PruneBackups(string catName)
        {
            foreach (string old in BackupDirs(catName).Reverse().Skip(Math.Max(1, config.BackupsToKeep)))
            {
                try
                {
                    Directory.Delete(old, true);
                }
                catch (Exception ex)
                {
                    Log("Could not delete old backup " + old + ": " + ex.Message);
                }
            }
        }

        private void RefreshIdleStatus()
        {
            progressBar.Visible = false;
            if (networkWasDown)
            {
                SetStatus("Network folder unavailable", StatusRed, "Sync is paused and resumes automatically when the share is back.");
            }
            else if (failedCatalogs.Count > 0)
            {
                SetStatus("Sync error", StatusRed, "See the events log. Press \"Sync now\" to retry.");
            }
            else if (Status.LightroomIsOpen())
            {
                SetStatus("Lightroom is open", StatusAmber, "Catalogs upload when you close Lightroom.");
            }
            else
            {
                SetStatus("Up to date", StatusGreen, "Last checked " + DateTime.Now.ToString("HH:mm:ss"));
            }
        }

        private async Task UpdateStatusFileOnNetwork()
        {
            await Task.Run(() =>
            {
                try
                {
                    string filePath = config.NetworkFolder + "\\status.txt";
                    File.WriteAllText(filePath, status.ToJson());
                    Log("Updated status file");
                }
                catch (Exception ex)
                {
                    throw new Exception("ERROR: Failed writing status file: " + ex.Message);
                }
            });
        }

        static async Task ZipFilesAndFolders(string zipFilename, string[] filesToZip, string[] foldersToZip)
        {
            await Task.Run(() =>
            {
                using (ZipArchive zipArchive = ZipFile.Open(zipFilename, ZipArchiveMode.Create))
                {
                    // Zip individual files
                    foreach (string filePath in filesToZip)
                    {
                        if (File.Exists(filePath))
                        {
                            string entryName = Path.GetFileName(filePath);
                            zipArchive.CreateEntryFromFile(filePath, entryName);
                        }
                        else
                        {
                            throw new FileNotFoundException($"File not found: {filePath}");
                        }
                    }

                    // Zip folders (optional ones, like Helper.lrdata, may not exist) including subfolders
                    foreach (string folderPath in foldersToZip)
                    {
                        if (!Directory.Exists(folderPath))
                        {
                            continue;
                        }

                        string folderName = Path.GetFileName(folderPath);
                        foreach (string filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                        {
                            string entryName = Path.Combine(folderName, Path.GetRelativePath(folderPath, filePath));
                            zipArchive.CreateEntryFromFile(filePath, entryName);
                        }
                    }
                }
            });
        }

        // Directory.Exists on a dead share can block for a long time, so probe on a worker thread with a timeout.
        private async Task<bool> IsNetworkFolderAvailable()
        {
            string folder = config.NetworkFolder;
            var probe = Task.Run(() => Directory.Exists(folder));
            try
            {
                return await Task.WhenAny(probe, Task.Delay(TimeSpan.FromSeconds(5))) == probe && await probe;
            }
            catch
            {
                return false;
            }
        }

        private Status? getNetworkStatus()
        {
            string networkStatusFile = config.NetworkFolder + "\\status.txt";
            if (File.Exists(networkStatusFile))
            {

                string jsonContent = File.ReadAllText(networkStatusFile);
                try
                {
                    var parsedStatus = JsonConvert.DeserializeObject<Status>(jsonContent);
                    if (parsedStatus != null)
                    {
                        lastKnownNetworkVersions = parsedStatus.MostRecentVersions.ToList();
                    }
                    return parsedStatus;
                }
                catch (JsonException ex)
                {
                    Log("JSON parsing error: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Log("Unexpected error: " + ex.Message);
                }
            }
            return null;
        }

        // confirmSuspicious: asked when a catalog looks damaged compared with its last upload. null (automatic uploads) means "do not upload".
        private async Task UploadCatalogs(Func<string, bool>? confirmSuspicious = null)
        {
            SetStatus("Uploading catalogs...", StatusBlue);
            if (!await IsNetworkFolderAvailable())
            {
                Log("ERROR: Network folder is not available, cannot save catalogs");
                return;
            }

            if (Status.LightroomIsOpen())
            {
                Log("ERROR: Lightroom is open, cannot save to network drive");
                return;
            }

            //Verify status file says it's safe to work (if it exists, otherwise, we can assume this is the first time)
            bool firstUpload = !File.Exists(Path.Combine(config.NetworkFolder, "status.txt"));
            Status? loadedStatus = getNetworkStatus();
            if (firstUpload)
            {
                //No status file yet: nothing has been uploaded to this folder, so there is nothing to overwrite.
            }
            else if (loadedStatus != null && loadedStatus.isSafeToOverride)
            {
                //Safe to proceed
            }
            else if (loadedStatus != null && loadedStatus.LastUser == Environment.MachineName)
            {
                //Safe to proceed since we're the ones who said it wasn't safe.
            }
            else
            {
                Log("Network status file says it's not safe to proceed! Something has gone wrong, or another catalog sync is happening!");
                return;
            }

            // Health-check every catalog before touching the share, so a damaged catalog can never replace a good copy.
            var health = new Dictionary<string, CatalogHealth>();
            foreach (string localCatalog in Directory.GetFiles(config.LocalFolder, "*.lrcat"))
            {
                string name = Path.GetFileNameWithoutExtension(localCatalog);
                SetProgress(null, "Checking " + name);
                CatalogHealth result = await Task.Run(() => CatalogCheck.Inspect(localCatalog));
                if (!result.Ok)
                {
                    Log("ERROR: " + name + " failed the integrity check (" + result.Detail + "). Nothing was uploaded.");
                    Notify("Upload blocked", name + " failed the integrity check. Nothing was uploaded.", ToolTipIcon.Error);
                    return;
                }
                health[name] = result;
                Log(name + ": " + result.Summary());

                string? concern = CatalogCheck.Concern(result, loadedStatus?.Catalogs.GetValueOrDefault(name));
                if (concern != null && (confirmSuspicious == null || !confirmSuspicious(name + ": " + concern)))
                {
                    Log("Upload blocked for " + name + ": " + concern);
                    Notify("Upload blocked", name + ": " + concern + " Check the catalog, then use Upload now.", ToolTipIcon.Warning);
                    return;
                }
            }
            if (loadedStatus != null)
            {
                status.Catalogs = loadedStatus.Catalogs;
            }

            status.isSafeToOverride = false;
            try
            {
                await UpdateStatusFileOnNetwork();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return;
            }


            string[] files = Directory.GetFiles(config.LocalFolder, "*.lrcat");

            status.MostRecentVersions = new List<string>();

            foreach (string file in files)
            {
                string catName = Path.GetFileNameWithoutExtension(file);


                string[] filesToZip = { config.LocalFolder + "\\" + catName + ".lrcat" };
                string[] foldersToZip = { config.LocalFolder + "\\" + catName + ".lrcat-data", config.LocalFolder + "\\" + catName + " Helper.lrdata" };

                DateTime lastModified = File.GetLastWriteTime(file);
                string customFormat = catName + " - " + lastModified.ToString("yyyy-MM-dd HH-mm") + ".zip";

                Log("Zipping " + catName);
                SetProgress(null, "Zipping " + catName);
                try
                {
                    // A zip left behind by an earlier failed run would make ZipFile.Open fail forever.
                    File.Delete(customFormat);
                    await ZipFilesAndFolders(customFormat, filesToZip, foldersToZip);
                    Log("Files and folders have been zipped successfully.");
                }
                catch (Exception ex)
                {
                    Log("ERROR: zipping files and folders: " + ex.Message);
                    Notify("Upload failed", "Could not zip " + catName + ": " + ex.Message, ToolTipIcon.Error);
                    return;
                }

                // Copy to a .part name and rename, so other machines never see a half-written zip.
                string finalPath = Path.Combine(config.NetworkFolder, customFormat);
                string partPath = finalPath + ".part";
                try
                {
                    await CopyFileAsync(customFormat, partPath, "Uploading " + catName);
                    await Task.Run(() => { File.Move(partPath, finalPath, true); File.Delete(customFormat); });
                    status.MostRecentVersions.Add(customFormat);
                    if (health.TryGetValue(catName, out CatalogHealth? uploaded))
                    {
                        status.Catalogs[catName] = new CatalogInfo { Images = uploaded.Images, MissingDevelop = uploaded.MissingDevelop };
                    }
                    Log(customFormat + " uploaded successfully.");
                }
                catch (Exception ex)
                {
                    Log("ERROR uploading " + customFormat + ": " + ex.Message);
                    Notify("Upload failed", "Could not upload " + catName + ": " + ex.Message, ToolTipIcon.Error);
                    return;
                }
            }

            status.isSafeToOverride = true;
            try
            {
                await UpdateStatusFileOnNetwork();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return;
            }
            Log("Sucessfully updated all " + files.Length.ToString() + " catalog(s) to network share.");
            Notify("Catalogs uploaded", files.Length + " catalog(s) saved to the network share.");
        }

        public Form1(bool startMinimized)
        {
            InitializeComponent();

            // Create the NotifyIcon instance
            trayIcon = new NotifyIcon();
            trayIcon.Text = "LightroomSync";
            trayIcon.Icon = new Icon(GetType(), "camera.ico");
            trayIcon.Visible = true; // always shown so notifications can appear

            // Create a context menu for the tray icon
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Restore", null, OnRestore);
            trayMenu.Items.Add("Exit", null, OnExit);

            // Assign the context menu to the tray icon
            trayIcon.ContextMenuStrip = trayMenu;

            // Handle the form's Resize event
            this.Resize += OnResize;
            trayIcon.Click += OnRestore;

            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                this.ShowInTaskbar = false;
                trayIcon.Visible = true;
            }
        }

        private void OnRestore(object sender, EventArgs e)
        {
            // Restore the form from the system tray
            this.Show();
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
        }

        private void OnExit(object sender, EventArgs e)
        {
            // Clean up resources and close the application
            trayIcon.Dispose();
            System.Windows.Forms.Application.Exit();
        }

        private void OnResize(object sender, EventArgs e)
        {
            // Minimize the form to the system tray when it's minimized
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.Hide();
                trayIcon.Visible = true;
            }
        }

        private void buttonSyncNow_Click(object sender, EventArgs e)
        {
            failedCatalogs.Clear();
            if (timerBeingHandled)
            {
                Log("A sync is already running.");
                return;
            }
            HandleTimerEvent();
        }

        // Runs work with the timer paused so it cannot overlap an automatic sync.
        private async Task RunExclusive(Func<Task> work)
        {
            if (timerBeingHandled)
            {
                Log("A sync is already running.");
                return;
            }

            timerBeingHandled = true;
            timer1.Enabled = false;
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
            finally
            {
                timerBeingHandled = false;
                timer1.Enabled = true;
                RefreshIdleStatus();
            }
        }

        private async void buttonUploadNow_Click(object sender, EventArgs e)
        {
            await RunExclusive(() => UploadCatalogs(AskUploadAnyway));
        }

        private bool AskUploadAnyway(string concern)
        {
            return MessageBox.Show(concern + "\n\nUploading will replace the good copy on the network folder (older zips stay there).\n\nUpload anyway?",
                "Catalog looks damaged", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        // Syncs first, so Lightroom never opens an out-of-date catalog, then opens the most recently used catalog.
        private async void buttonOpenLightroom_Click(object sender, EventArgs e)
        {
            if (Status.LightroomIsOpen())
            {
                Log("Lightroom is already open.");
                return;
            }
            if (timerBeingHandled)
            {
                Log("A sync is in progress. Try again in a moment.");
                return;
            }

            string? catalog = Directory.Exists(config.LocalFolder)
                ? Directory.GetFiles(config.LocalFolder, "*.lrcat").OrderByDescending(File.GetLastWriteTime).FirstOrDefault()
                : null;
            if (catalog == null)
            {
                MessageBox.Show("There is no catalog in the local catalog folder.", "Open Lightroom", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!await IsNetworkFolderAvailable())
            {
                var answer = MessageBox.Show("The network folder is unreachable, so this computer cannot check for a newer catalog.\n\nOpen Lightroom anyway?",
                    "Open Lightroom", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }
            else
            {
                failedCatalogs.Clear();
                await RunExclusive(HandleTimerEventCore);
                if (failedCatalogs.Count > 0)
                {
                    MessageBox.Show("A newer catalog could not be installed, so Lightroom was not opened. See the Events log.",
                        "Open Lightroom", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo(catalog) { UseShellExecute = true });
                Log("Opening " + Path.GetFileName(catalog) + " in Lightroom.");
            }
            catch (Exception ex)
            {
                Log("ERROR opening Lightroom: " + ex.Message);
                MessageBox.Show("Could not open the catalog: " + ex.Message, "Open Lightroom", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void openLogFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory(AppLog.Folder);
            Utils.OpenURL(AppLog.Folder);
        }

        private void buttonCatalogs_Click(object sender, EventArgs e)
        {
            using var dialog = new CatalogsForm(this);
            dialog.ShowDialog(this);
        }

        private HelpForm? helpForm;

        private void userGuideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (helpForm == null || helpForm.IsDisposed)
            {
                helpForm = new HelpForm(Font, Icon);
                helpForm.Show(this);
            }
            else
            {
                helpForm.Activate();
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new SettingsForm(config);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                timer1.Interval = config.CheckIntervalSeconds * 1000;
            }
        }

        internal List<CatalogRow> GetCatalogRows()
        {
            var rows = new List<CatalogRow>();
            if (!Directory.Exists(config.LocalFolder))
            {
                return rows;
            }

            foreach (string file in Directory.GetFiles(config.LocalFolder, "*.lrcat"))
            {
                string catName = Path.GetFileNameWithoutExtension(file);
                // Zip names end in " - yyyy-MM-dd HH-mm.zip"
                string? network = lastKnownNetworkVersions.FirstOrDefault(v => v.StartsWith(catName + " - "));
                string[] backups = BackupDirs(catName);
                rows.Add(new CatalogRow(
                    catName,
                    File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm"),
                    network == null ? "-" : network.Substring(network.Length - 20, 16),
                    backups.Length == 0 ? "none" : backups.Length + " (newest " + Path.GetFileName(backups[^1]) + ")",
                    backups.Length > 0));
            }
            return rows;
        }

        // Swaps in the newest backup (keeping the current catalog as a backup) and uploads it so other machines follow.
        internal async Task<string?> RestoreBackup(string catName)
        {
            string? error = "A sync is already running; try again in a moment.";
            await RunExclusive(async () =>
            {
                error = null;
                if (Status.LightroomIsOpen())
                {
                    error = "Close Lightroom first.";
                    return;
                }
                string? latest = BackupDirs(catName).LastOrDefault();
                if (latest == null)
                {
                    error = "There is no backup for this catalog.";
                    return;
                }

                string swapDir = NewBackupDir(catName);
                if (swapDir == latest)
                {
                    swapDir += "-restore";
                }
                Directory.CreateDirectory(swapDir);
                var moved = new List<string>();
                try
                {
                    MoveCatalogFiles(config.LocalFolder, swapDir, catName, moved);
                    MoveCatalogFiles(latest, config.LocalFolder, catName, null);
                    Directory.Delete(latest, true);
                }
                catch (Exception ex)
                {
                    PutBack(moved, swapDir);
                    error = "Restore failed, your catalog was left as it was: " + ex.Message;
                    return;
                }

                // Make the restored catalog the newest version so the next sync uploads it instead of overwriting it.
                File.SetLastWriteTime(Path.Combine(config.LocalFolder, catName + ".lrcat"), DateTime.Now);
                Log("Restored the previous version of " + catName + ". The version it replaced is kept in " + swapDir);
                await UploadCatalogs(AskUploadAnyway);
            });
            return error;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.txt"))
            {

                string jsonContent = File.ReadAllText("config.txt");
                try
                {
                    Config? loadedConfig = JsonConvert.DeserializeObject<Config>(jsonContent);

                    if (loadedConfig != null)
                    {
                        config = loadedConfig;
                    }
                    else
                    {
                        Log("JSON deserialization failed");
                    }
                }
                catch (JsonException ex)
                {
                    Log("JSON parsing error: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Log("Unexpected error: " + ex.Message);
                }
            }

            localFolderTextBox.Text = config.LocalFolder;
            networkFolderTextBox.Text = config.NetworkFolder;

            status.LastUser = System.Environment.MachineName;
            AppLog.Write("LightroomSync " + currentVersion + " started on " + Environment.MachineName);
            timer1.Interval = Math.Max(1, config.CheckIntervalSeconds) * 1000;

            if (Utils.ShortcutExistsInStartupFolder())
            {
                launchAtStartupToolStripMenuItem.Image = Resources.checkmark;
            }

            if (config.AutoCheckForUpdates)
            {
                autoCheckForUpdatesToolStripMenuItem.Image = Resources.checkmark;
                CheckForUpdates(true);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {



        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            trayIcon.Dispose(); // otherwise a ghost icon stays until hovered
            string filePath = "config.txt";
            File.WriteAllText(filePath, config.ToJson());
        }

        private void localFolderTextBox_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(localFolderTextBox.Text))
            {
                localFolderTextBox.ForeColor = SystemColors.WindowText;
                config.LocalFolder = localFolderTextBox.Text;
            }
            else
            {
                localFolderTextBox.ForeColor = StatusRed;
            }
        }

        private void networkFolderTextBox_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(networkFolderTextBox.Text))
            {
                networkFolderTextBox.ForeColor = SystemColors.WindowText;
                config.NetworkFolder = networkFolderTextBox.Text;
            }
            else
            {
                networkFolderTextBox.ForeColor = StatusRed;
            }
        }

        private void buttonSelectLocalFolder_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                // Show the folder browser dialog
                DialogResult result = folderBrowserDialog.ShowDialog();

                // Check if the user selected a folder
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    // Update the text box with the selected folder path
                    localFolderTextBox.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void buttonSelectNetworkFolder_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                // Show the folder browser dialog
                DialogResult result = folderBrowserDialog.ShowDialog();

                // Check if the user selected a folder
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    // Update the text box with the selected folder path
                    networkFolderTextBox.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timerBeingHandled == true)
            {
                //return;
            }

            HandleTimerEvent();
        }

        private async void HandleTimerEvent()
        {
            if (timerBeingHandled)
            {
                return;
            }
            timerBeingHandled = true;
            timer1.Enabled = false;

            try
            {
                // Pause all syncing while the share is unreachable; resume automatically when it returns.
                if (!await IsNetworkFolderAvailable())
                {
                    if (!networkWasDown)
                    {
                        networkWasDown = true;
                        Log("Network folder unavailable. Sync paused until it comes back.");
                        Notify("Network folder unavailable", "Sync is paused until the share is back.", ToolTipIcon.Warning);
                    }
                    RefreshIdleStatus();
                    return;
                }
                if (networkWasDown)
                {
                    networkWasDown = false;
                    Log("Network folder is back. Resuming sync.");
                    Notify("Network folder is back", "Sync resumed.");
                }

                await HandleTimerEventCore();
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
            finally
            {
                timerBeingHandled = false;
                timer1.Enabled = true;
                RefreshIdleStatus();
            }
        }

        private async Task HandleTimerEventCore()
        {

            if (Status.LightroomIsOpen() && hasDealtWithLightroomOpen == false)
            {
                timer1.Enabled = false;

                Status? loadedStatus = getNetworkStatus();
                if (loadedStatus != null && loadedStatus.isSafeToOverride == false && loadedStatus.LastUser != Environment.MachineName)
                {
                    Alert alert = new(loadedStatus.LastUser);
                    DialogResult result = alert.ShowDialog();
                    if (result == DialogResult.Yes)
                    {
                        Process[] processes = Process.GetProcessesByName("Lightroom");
                        if (processes.Length > 0)
                        {
                            processes[0].Kill();
                            Log("Lightroom process killed. Please close Lightroom on " + loadedStatus.LastUser + " before trying again.");
                            StopFlashing();
                            timer1.Enabled = true;
                            timerBeingHandled = false;
                            return;
                        }
                        else
                        {
                            Log("ERROR: Couldn't find Lightroom process to kill! Either this is a bug or you closed it manually. Please restart this application (and consider filing a bug report on GitHub).");
                            return;
                        }
                    }

                    // Continue if user selected DialogResult.No, since that means they want to wipe the existing status
                    Log("The status file on your network will be overwritten by this machine.");
                    StopFlashing();
                }

                Log("Detected Lightroom is open. Updating the status file to alert other machines.");
                if (loadedStatus != null)
                {
                    // Keep what the share already knows (current versions, photo counts) instead of blanking it.
                    status.MostRecentVersions = loadedStatus.MostRecentVersions;
                    status.Catalogs = loadedStatus.Catalogs;
                }
                hasDealtWithLightroomOpen = true;
                status.isSafeToOverride = false;
                status.LastUser = Environment.MachineName;
                await UpdateStatusFileOnNetwork();
                timer1.Enabled = true;
                timerBeingHandled = false;
            }
            else if (Status.LightroomIsOpen() == false && hasDealtWithLightroomOpen == true)
            {
                //This means Lightroom WAS open, but isn't anymore. This is where we upload the catalogs.
                timer1.Enabled = false;
                await UploadCatalogs();
                hasDealtWithLightroomOpen = false;
                timer1.Enabled = true;
                timerBeingHandled = false;
            }
            else if (Status.LightroomIsOpen() == false && hasDealtWithLightroomOpen == false)
            {
                // Lightroom has not been open, so there's a possibility the network has newer catalogs.
                timer1.Enabled = false;
                Status? loadedStatus = getNetworkStatus();
                if (loadedStatus == null)
                {
                    timer1.Enabled = true;
                    timerBeingHandled = false;
                    return;
                }

                string[] files = Directory.GetFiles(config.LocalFolder, "*.lrcat");
                string[] timestamped = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    string catName = Path.GetFileNameWithoutExtension(files[i]);
                    DateTime lastModified = File.GetLastWriteTime(files[i]);
                    timestamped[i] = catName + " - " + lastModified.ToString("yyyy-MM-dd HH-mm") + ".zip";
                }

                // Another machine is mid-upload (or has Lightroom open): its zips may be incomplete or stale.
                if (!loadedStatus.isSafeToOverride && loadedStatus.LastUser != Environment.MachineName)
                {
                    timer1.Enabled = true;
                    timerBeingHandled = false;
                    return;
                }

                foreach (string catalog in loadedStatus.MostRecentVersions)
                {
                    if (!timestamped.Contains(catalog) && !failedCatalogs.Contains(catalog))
                    {
                        if (!await SyncCatalogFromNetwork(catalog))
                        {
                            // Don't hammer the share every tick; a new upload (new zip name) or "Sync now" retries.
                            failedCatalogs.Add(catalog);
                            Log("Will not retry " + catalog + " automatically. Press \"Sync now\" to try again.");
                        }
                    }
                }

                timer1.Enabled = true;
                timerBeingHandled = false;
            }
        }

        // Replaces the local catalog with the network zip without ever leaving the user without a catalog:
        // download -> extract to temp -> verify -> move old catalog to a backup folder -> move new one in.
        private async Task<bool> SyncCatalogFromNetwork(string catalog)
        {
            bool succeeded = false;
            string catName = catalog.Substring(0, catalog.Length - 23); //23 is len(" - yyyy-MM-dd HH-mm.zip")
            SetStatus("Downloading newer catalog...", StatusBlue, catalog);
            string workDir = Utils.GetWorkingDir();
            string zipPath = Path.Combine(workDir, catalog);
            string extractDir = Path.Combine(config.LocalFolder, ".lrsync-tmp-" + catName);
            string backupDir = NewBackupDir(catName);
            var movedToBackup = new List<string>();

            Log("Newer catalog version detected! Copying " + catalog + " to local storage.");
            try
            {
                Directory.CreateDirectory(workDir);
                await CopyFileAsync(Path.Combine(config.NetworkFolder, catalog), zipPath, "Downloading " + catName);
                Log(catalog + " copied successfully. Extracting.");

                SetProgress(null, "Extracting " + catName);
                await Task.Run(() =>
                {
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                    ZipFile.ExtractToDirectory(zipPath, extractDir);
                });

                string extractedCat = Path.Combine(extractDir, catName + ".lrcat");
                if (!File.Exists(extractedCat) || new FileInfo(extractedCat).Length == 0)
                {
                    throw new InvalidDataException("Zip does not contain a valid " + catName + ".lrcat");
                }

                SetProgress(null, "Checking " + catName);
                CatalogHealth downloaded = await Task.Run(() => CatalogCheck.Inspect(extractedCat));
                if (!downloaded.Ok)
                {
                    throw new InvalidDataException("The downloaded catalog failed the integrity check (" + downloaded.Detail + ")");
                }
                Log(catName + " (downloaded): " + downloaded.Summary());

                // Point of no return: park the old catalog in the backup folder, then move the new one in.
                Directory.CreateDirectory(backupDir);
                try
                {
                    MoveCatalogFiles(config.LocalFolder, backupDir, catName, movedToBackup);
                    MoveCatalogFiles(extractDir, config.LocalFolder, catName, null);

                    // The zip name is the sync marker: make the local file's time produce exactly that name, or every tick would re-download.
                    if (DateTime.TryParseExact(catalog.Substring(catName.Length + 3, 16), "yyyy-MM-dd HH-mm",
                            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime stamp))
                    {
                        File.SetLastWriteTime(Path.Combine(config.LocalFolder, catName + ".lrcat"), stamp);
                    }
                }
                catch
                {
                    PutBack(movedToBackup, backupDir); // never leave the user without a catalog
                    throw;
                }

                PruneBackups(catName);
                Log("Catalog " + catName + " is up to date. Previous local version kept in " + backupDir);
                Notify("Catalog updated", catName + " was updated from the network share.");
                succeeded = true;
            }
            catch (Exception ex)
            {
                Log("ERROR syncing " + catName + ": " + ex.Message + ". Your local catalog was not replaced. The zip on the network share is intact.");
                Notify("Sync failed", "Could not update " + catName + ". Your local catalog was not changed.", ToolTipIcon.Error);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    if (Directory.Exists(backupDir) && !Directory.EnumerateFileSystemEntries(backupDir).Any()) Directory.Delete(backupDir);
                }
                catch (Exception ex)
                {
                    Log("Cleanup warning: " + ex.Message);
                }
            }
            return succeeded;
        }

        private async void CheckForUpdates(bool silently)
        {
            string version = "ERR NOT SET";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    version = await client.GetStringAsync(RepoUrl + "/raw/master/latestVersion.txt");
                }
                catch (Exception ex)
                {
                    Log($"Error checking for new version: {ex.Message}");
                    if (silently)
                    {
                        return;
                    }
                    var result = MessageBox.Show("Sorry, I couldn't find the version number. Do you want to go to the website to check?", "Error!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Yes)
                    {
                        Utils.OpenURL(RepoUrl + "/releases");
                    }
                    return;
                }
            }

            var parsed = Version.Parse(version.Trim());

            if (parsed.CompareTo(Version.Parse(currentVersion)) > 0)
            {
                var dialog = "There is a new version! Want to go get it?" + Environment.NewLine + Environment.NewLine + "New Version: " + parsed.ToString() + Environment.NewLine + "Your Version: " + currentVersion.ToString();
                var result = MessageBox.Show(dialog, "New Version!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    Utils.OpenURL(RepoUrl + "/releases");
                }
            }
            else if (!silently)
            {
                MessageBox.Show("You expected an update, but it was me! Dio!", "Up To Date!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void submitABugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.OpenURL(RepoUrl + "/issues");
        }

        private void gitHubPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.OpenURL(RepoUrl);
        }

        private void minimizeToTrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void launchAtStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Either remove or add from startup as a toggle.
            if (Utils.ShortcutExistsInStartupFolder())
            {
                Utils.DeleteShortcutFromStartupFolder();
                launchAtStartupToolStripMenuItem.Image = null;
            }
            else
            {
                string assemblyLocation = Assembly.GetEntryAssembly().Location;
                string executablePath = Path.GetDirectoryName(assemblyLocation);
                string appPath = executablePath + "\\LightroomSync.exe";
                launchAtStartupToolStripMenuItem.Image = Resources.checkmark;

                Utils.CreateShortcutInStartupFolder(appPath);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("LightroomSync" + Environment.NewLine + "Copyright 2023 Anthony Bryan" + Environment.NewLine + Environment.NewLine + "Version " + currentVersion);
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CheckForUpdates(false);
        }

        private void autoCheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            config.AutoCheckForUpdates = !config.AutoCheckForUpdates;
            if (config.AutoCheckForUpdates)
            {
                autoCheckForUpdatesToolStripMenuItem.Image = Resources.checkmark;
            } 
            else
            {
                autoCheckForUpdatesToolStripMenuItem.Image = null;
            }
        }
    }
}