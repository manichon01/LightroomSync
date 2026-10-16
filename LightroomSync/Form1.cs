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
        public string currentVersion = "1.0.0"; // <----- Make sure you always update latestVersion.txt as well!
                                                // Yes, I'm too lazy to pipe this in.

        private Config config = new Config();
        private Status status = new Status();

        private bool hasDealtWithLightroomOpen = false;

        private bool timerBeingHandled = false; //Used to handle async events in the timer potentially firing multiple times
        private bool networkWasDown = false;
        private readonly HashSet<string> failedCatalogs = new(); // zips that failed to sync; not retried until "Sync now"

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

        private void RefreshIdleStatus()
        {
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

                    // Zip folders
                    foreach (string folderPath in foldersToZip)
                    {
                        if (Directory.Exists(folderPath))
                        {
                            string folderName = Path.GetFileName(folderPath);

                            // Zip files inside the folder
                            string[] files = Directory.GetFiles(folderPath);
                            foreach (string filePath in files)
                            {
                                string entryName = Path.Combine(folderName, Path.GetFileName(filePath));
                                zipArchive.CreateEntryFromFile(filePath, entryName);
                            }
                        }
                        else
                        {
                            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
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
                    return JsonConvert.DeserializeObject<Status>(jsonContent);
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

        private async Task UploadCatalogs()
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
            Status? loadedStatus = getNetworkStatus();
            if (loadedStatus != null && loadedStatus.isSafeToOverride)
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
                try
                {
                    await ZipFilesAndFolders(customFormat, filesToZip, foldersToZip);
                    status.MostRecentVersions.Add(customFormat);
                    Log("Files and folders have been zipped successfully.");
                }
                catch (Exception ex)
                {
                    Log("ERROR: zipping files and folders: " + ex.Message);
                    return;
                }

                Log("Moving " + customFormat + " to " + config.NetworkFolder);
                try
                {
                    await Task.Run(() => { File.Move(customFormat, config.NetworkFolder + "\\" + customFormat, true); });
                    Log(customFormat + " moved successfully.");
                }
                catch (IOException ex)
                {
                    Log("Error moving: " + ex.Message);
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
        }

        public Form1(bool startMinimized)
        {
            InitializeComponent();

            // Create the NotifyIcon instance
            trayIcon = new NotifyIcon();
            trayIcon.Text = "LightroomSync";
            trayIcon.Icon = new Icon(GetType(), "camera.ico");

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
            trayIcon.Visible = false;
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

        private async void buttonUploadNow_Click(object sender, EventArgs e)
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
                SetStatus("Uploading catalogs...", StatusBlue);
                await UploadCatalogs();
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
                    }
                    RefreshIdleStatus();
                    return;
                }
                if (networkWasDown)
                {
                    networkWasDown = false;
                    Log("Network folder is back. Resuming sync.");
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
            SetStatus("Downloading newer catalog...", StatusBlue, catalog);
            string catName = catalog.Substring(0, catalog.Length - 23); //23 is len(" - yyyy-MM-dd HH-mm.zip")
            string workDir = Utils.GetWorkingDir();
            string zipPath = Path.Combine(workDir, catalog);
            string extractDir = Path.Combine(config.LocalFolder, ".lrsync-tmp-" + catName);
            string backupDir = Path.Combine(config.LocalFolder, "LightroomSync Backup", catName);
            string[] names = { catName + ".lrcat", catName + ".lrcat-data", catName + " Helper.lrdata" };
            var movedToBackup = new List<string>();

            Log("Newer catalog version detected! Copying " + catalog + " to local storage.");
            try
            {
                Directory.CreateDirectory(workDir);
                await Task.Run(() => File.Copy(Path.Combine(config.NetworkFolder, catalog), zipPath, true));
                Log(catalog + " copied successfully. Extracting.");

                await Task.Run(() =>
                {
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                    ZipFile.ExtractToDirectory(zipPath, extractDir);
                });

                string extractedCat = Path.Combine(extractDir, names[0]);
                if (!File.Exists(extractedCat) || new FileInfo(extractedCat).Length == 0)
                {
                    throw new InvalidDataException("Zip does not contain a valid " + names[0]);
                }

                // Point of no return: park the old catalog in the backup folder (same volume, so it is a rename).
                if (Directory.Exists(backupDir))
                {
                    Directory.Delete(backupDir, true);
                }
                Directory.CreateDirectory(backupDir);
                try
                {
                    foreach (string name in names)
                    {
                        string current = Path.Combine(config.LocalFolder, name);
                        if (File.Exists(current))
                        {
                            File.Move(current, Path.Combine(backupDir, name));
                            movedToBackup.Add(name);
                        }
                        else if (Directory.Exists(current))
                        {
                            Directory.Move(current, Path.Combine(backupDir, name));
                            movedToBackup.Add(name);
                        }
                    }

                    foreach (string name in names)
                    {
                        string extracted = Path.Combine(extractDir, name);
                        string target = Path.Combine(config.LocalFolder, name);
                        if (File.Exists(extracted))
                        {
                            File.Move(extracted, target);
                        }
                        else if (Directory.Exists(extracted))
                        {
                            Directory.Move(extracted, target);
                        }
                    }
                }
                catch
                {
                    // Put the old catalog back so the user is never left without one.
                    foreach (string name in movedToBackup)
                    {
                        string target = Path.Combine(config.LocalFolder, name);
                        string saved = Path.Combine(backupDir, name);
                        try
                        {
                            if (File.Exists(target)) File.Delete(target);
                            if (Directory.Exists(target)) Directory.Delete(target, true);
                            if (File.Exists(saved)) File.Move(saved, target);
                            else if (Directory.Exists(saved)) Directory.Move(saved, target);
                        }
                        catch (Exception restoreEx)
                        {
                            Log("Could not restore " + name + ": " + restoreEx.Message + ". Your old catalog is in " + backupDir);
                        }
                    }
                    throw;
                }

                Log("Catalog " + catName + " is up to date. Previous local version kept in " + backupDir);
                succeeded = true;
            }
            catch (Exception ex)
            {
                Log("ERROR syncing " + catName + ": " + ex.Message + ". Your local catalog was not replaced. The zip on the network share is intact.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                    if (File.Exists(zipPath)) File.Delete(zipPath);
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
                    version = await client.GetStringAsync("https://github.com/software-2/LightroomSync/raw/master/latestVersion.txt");
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
                        Utils.OpenURL("https://github.com/software-2/LightroomSync/releases");
                    }
                    return;
                }
            }

            var parsed = Version.Parse(version);

            if (parsed.CompareTo(Version.Parse(currentVersion)) != 0)
            {
                var dialog = "There is a new version! Want to go get it?" + Environment.NewLine + Environment.NewLine + "New Version: " + parsed.ToString() + Environment.NewLine + "Your Version: " + currentVersion.ToString();
                var result = MessageBox.Show(dialog, "New Version!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    Utils.OpenURL("https://github.com/software-2/LightroomSync/releases");
                }
            }
            else if (!silently)
            {
                MessageBox.Show("You expected an update, but it was me! Dio!", "Up To Date!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void submitABugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.OpenURL("https://github.com/software-2/LightroomSync/issues");
        }

        private void gitHubPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Utils.OpenURL("https://github.com/software-2/LightroomSync");
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