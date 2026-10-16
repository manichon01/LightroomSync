namespace LightroomSync
{
    // Everything shown in the Events box is also written here, plus crashes, so bug reports have a full history.
    internal static class AppLog
    {
        private static readonly object Gate = new();
        private const long MaxBytes = 1_000_000;

        public static string Folder => Path.Combine(Utils.GetWorkingDir(), "logs");

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Folder);
                    string path = Path.Combine(Folder, "lightroomsync.log");
                    if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                    {
                        // Keep the current log plus two older ones.
                        string older = Path.Combine(Folder, "lightroomsync.2.log");
                        string old = Path.Combine(Folder, "lightroomsync.1.log");
                        if (File.Exists(old))
                        {
                            File.Move(old, older, true);
                        }
                        File.Move(path, old, true);
                    }
                    File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never break the app.
            }
        }
    }
}
