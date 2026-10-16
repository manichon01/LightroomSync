# Lightroom Sync User Guide

## What this app does

Lightroom Classic catalogs are meant for one computer. Lightroom will not run a catalog from a network folder. Lightroom Sync works around this by moving your catalog between computers for you.

- When you **close Lightroom**, the app zips your catalog and saves it to a shared network folder.
- When Lightroom is **closed**, the app checks that folder. If it holds a newer catalog, the app downloads it and replaces your local one.
- It **blocks you from opening Lightroom on two computers at once**, so work is never overwritten.

> Lightroom Sync syncs **catalogs only**. It does not copy your photos, previews or Lightroom presets and settings. See "What is and is not synced".

## What you need

- Two or more Windows 10 or 11 (64-bit) computers with **Lightroom Classic**.
- A **network folder** that every computer can reach, such as a folder on a NAS or a shared folder on another PC. Each computer needs read and write access.
- Your **photos stored where every computer can see them**, with the same path on each (see "Your photos").
- The **same Lightroom Classic version** on every computer. A newer Lightroom upgrades the catalog, and an older Lightroom cannot open an upgraded catalog.

Lightroom Sync includes everything it needs. You do not have to install .NET.

## Setup, step by step

Do steps 1 to 3 on **every** computer.

1. **Install and start Lightroom Sync.**
2. **Local catalog folder:** click **Browse...** and choose the folder where your `.lrcat` catalog files are on this computer. This folder can be different on each computer.
3. **Network folder:** click **Browse...** and choose the shared folder. Use the **same shared folder on every computer**. A path that turns red does not exist (or is not reachable) right now.
4. **First computer only, first upload:** close Lightroom, then click **Upload now**. Your catalogs are zipped and saved to the network folder. (Opening and then closing Lightroom does the same.)
5. **Other computers:** with Lightroom closed, click **Sync now**. They download the catalogs from the network folder.
6. **Optional:** turn on **File > Launch At Startup** so the app runs silently in the system tray.

The two folder paths are saved when you close the app. The status line should say **Up to date**.

## Daily use

Just use Lightroom as normal. The app works in the background.

1. Start Lightroom Sync (or let it start with Windows).
2. Before you open Lightroom, wait for **Up to date**. If another computer saved a newer catalog, the app downloads it first.
3. Open Lightroom and work. The app notes that this computer is using the catalogs.
4. **Close Lightroom.** The app zips the catalogs and uploads them. Wait for the notification "Catalogs uploaded" before you shut down or unplug the laptop.

> Always close Lightroom completely and let the upload finish before you move to another computer.

## The status line

The colored dot at the top of the window shows what the app is doing.

- **Green, Up to date:** nothing to do. Shows when it last checked.
- **Amber, Lightroom is open:** catalogs upload when you close Lightroom.
- **Blue, Uploading or Downloading:** a transfer is running. A progress bar shows how much has been copied.
- **Red, Network folder unavailable:** the share cannot be reached. The app **pauses** and resumes by itself when the share is back. Nothing is lost.
- **Red, Sync error:** a catalog failed to update. Read the **Events** log, fix the cause, then click **Sync now**. Your local catalog is not replaced when this happens.

## The buttons

- **Sync now:** checks the network folder right away and downloads newer catalogs. It also retries catalogs that failed before.
- **Upload now:** zips your local catalogs and saves them to the network folder immediately. Close Lightroom first. Use it for the very first upload, or after you restore a version.
- **Catalogs...:** shows every catalog and lets you restore the previous version (see "Backups and restoring").
- **Sync & open Lightroom:** checks the network folder, installs any newer catalog, and only then opens your most recently used catalog in Lightroom. Use it instead of opening Lightroom yourself, so you never start on an out-of-date catalog. If the share cannot be reached, it asks before opening.
- **Browse...:** pick a folder instead of typing its path.

## Safety checks

A Lightroom catalog is a database, and a damaged one can quietly spread to all your computers. So the app checks every catalog:

- **Before uploading:** the catalog is checked for corruption. If it fails, **nothing is uploaded** and the good copy on the network folder stays as it is.
- **Looks different from last time:** if many photos suddenly have no develop settings (their edits), or the photo count drops sharply, an automatic upload is blocked and you get a notification. **Upload now** shows the problem and asks if you want to upload anyway.
- **Before installing a download:** the downloaded catalog is checked. If it is damaged, your local catalog is not replaced.
- The Events log shows the photo count of each catalog, for example `200 photos, 0 without develop settings`.

If an upload is blocked, do not panic: your older versions are safe in the backups and in the zips on the network folder. Open the catalog in Lightroom to see what is wrong, or use **Catalogs...** to restore the previous version.

## Two computers at once

If you open Lightroom on a computer while another computer already has it open, a red **STOP USING LIGHTROOM** window appears. It means the other computer has not finished with the catalog.

- **Kill Lightroom:** closes Lightroom on this computer. This is the safe choice. Close Lightroom properly on the other computer, wait for its upload, then open it here.
- **Override Catalogs:** use only if you are **sure** this computer has the newest catalog and Lightroom is **not** really open elsewhere. When you close Lightroom here, this computer's catalogs become the master copy.

The same window can appear after a crash or power cut that left the "in use" mark on the share. If so, and you know Lightroom is closed everywhere, use Override.

## Backups and restoring

Before the app replaces your local catalog with a newer one, it **keeps your old catalog** as a backup. Nothing is deleted until the new catalog is verified and in place.

- Backups live in the folder `LightroomSync Backup` inside your local catalog folder, in one folder per catalog and one subfolder per date and time.
- The app keeps the newest few versions per catalog (3 by default). Change this in **File > Settings...**.

To go back to the previous version:

1. Close Lightroom.
2. Click **Catalogs...** and select the catalog.
3. Click **Restore previous version** and confirm.

The version you replace is saved as a backup too, so you can undo a restore. The restored catalog is uploaded to the network folder, so your other computers follow.

## What is and is not synced

Synced:

- The catalog file (`.lrcat`), its `.lrcat-data` folder and its `Helper.lrdata` folder, if they exist.
- Edits, ratings, keywords, collections and other data stored in the catalog.

Not synced:

- **Your photos.** They must already be on storage that every computer can reach.
- **Previews and Smart Previews** (`Previews.lrdata`, `Smart Previews.lrdata`). Lightroom rebuilds them, so the first open on a computer may be slower.
- Lightroom **presets, plugins and preferences**.

## Your photos

A catalog stores the path of each photo. For Lightroom to find your photos on every computer, the **path must be identical**.

- Best: use the same drive letter everywhere (for example photos on `P:\Photos` on every PC), or the same network path.
- Mapped drive letters belong to one Windows user. If a drive shows as missing, reconnect it and restart Lightroom.
- If photos show as missing after a sync, use Lightroom's Find Missing Folder to repoint them once.

## Settings

Open **File > Settings...**.

- **Check for changes every (seconds):** how often the app checks the network folder. Default 5.
- **Previous versions to keep per catalog:** how many backups to keep. Default 3.
- **Show notifications:** pop-ups from the tray icon for uploads, downloads, lost network and errors.

In the **File** menu:

- **Launch At Startup:** starts the app with Windows, hidden in the system tray.
- **Auto Check For Updates:** looks for a new version when the app starts.
- **Minimize To Tray:** hides the window. The app keeps running next to the clock. Click the tray icon to show the window again.

> Closing the window with the X button **quits** the app. To keep syncing in the background, use **Minimize To Tray**.

## The network folder

The app writes these into the network folder:

- One `.zip` per catalog, named like `Lightroom Catalog - 2026-10-16 14-30.zip` (the time of the last change).
- `status.txt`, a small file that says which computer is using the catalogs and which zips are current. **Do not edit it** unless the troubleshooting steps below tell you to.

The app **never deletes** the zips on the network folder. They are your safety copies. Old zips pile up over time and you can remove the older ones by hand. Keep at least the newest zip listed for each catalog.

## Where things are stored on your computer

- **Settings:** `%APPDATA%\LightroomSync\config.txt`
- **Working files:** `%APPDATA%\LightroomSync` (zips are kept here only during a transfer)
- **Backups:** `LightroomSync Backup` inside your local catalog folder
- **Log files:** `%APPDATA%\LightroomSync\logs` (open it with **Help > Open Log Folder**). The app keeps everything the Events box shows, plus any crash details, and about 3 MB of history.

Paste `%APPDATA%\LightroomSync` into the Windows Explorer address bar to open the settings folder.

## Troubleshooting

### The status says Network folder unavailable

The computer cannot reach the share. Check that the NAS or other PC is on, that you are on the right network or VPN, and that the mapped drive is connected. The app resumes by itself. If it never recovers, check the folder in Windows Explorer.

### I see a red path in a folder box

The folder does not exist or cannot be reached right now. Fix the path, or reconnect the drive. The app keeps the last good path until you enter a valid one.

### Nothing downloads on a new computer

Check that the network folder is the same as on the first computer and that it holds a `status.txt` and zip files. Then close Lightroom and click **Sync now**. Read the **Events** log for a message.

### "Network status file says it's not safe to proceed"

Another computer marked the catalogs as in use. Close Lightroom there and wait for its upload. If you know Lightroom is closed everywhere, for example after a crash, open the network folder and delete `status.txt`. Then click **Upload now** on the computer with the correct catalog.

### Sync error or a failed upload or download

The Events log gives the reason. Common causes are a full disk, a share that went offline mid-copy, or a file locked by another program. Fix it, then click **Sync now**. A failed download never replaces your local catalog, and the zip on the share stays intact.

### My catalog is missing or wrong

Do not panic. Your data is in at least two places:

1. Click **Catalogs...** and restore the previous version.
2. Open the `LightroomSync Backup` folder in your local catalog folder and copy the catalog back by hand.
3. Take the newest zip from the network folder and extract it into your local catalog folder.

### Lightroom is open but the app says it is not

The app looks for a running program named `Lightroom`. If Lightroom is stuck in the background, the app still treats it as open. End the Lightroom process in Task Manager.

### The app does not start with Windows

Open **File** and check that **Launch At Startup** has a tick. It creates a shortcut in your Windows Startup folder. The shortcut starts the app hidden in the tray.

## Tips

- **Wait for the upload** before putting a laptop to sleep or leaving the house.
- Large catalogs take longer to copy. The progress bar shows the transfer.
- Keep the **clocks** of your computers correct. Versions are named by time.
- Back up the network folder too. It holds your only shared copy.
- To keep the app out of the way, minimize it to the tray and leave it running.

## Command line

Start the app hidden in the system tray:

`LightroomSync.exe tray`

## Updating and uninstalling

- **Help > Check For Updates** tells you when a newer version is available and opens the download page.
- To update, run the new installer. It replaces the old version and keeps your settings.
- To uninstall, use Windows Settings > Apps. Your settings, backups and the zips on the network folder are not removed.

## Getting help

Use **Help > Submit A Bug** and attach the file `lightroomsync.log` from **Help > Open Log Folder**. It shows what the app was doing, including crashes, and usually points to the cause.
