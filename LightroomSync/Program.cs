namespace LightroomSync
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                AppLog.Write("UNHANDLED ERROR: " + e.Exception);
                MessageBox.Show("Something went wrong: " + e.Exception.Message + "\n\nDetails were saved to the log (Help > Open Log Folder). Please include it in a bug report.",
                    "Lightroom Sync", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) => AppLog.Write("FATAL ERROR: " + e.ExceptionObject);
            Application.SetColorMode(SystemColorMode.System); // follow the Windows light/dark setting

            if (!Directory.Exists(Utils.GetWorkingDir()))
            {
                Directory.CreateDirectory(Utils.GetWorkingDir());
            }
            Directory.SetCurrentDirectory(Utils.GetWorkingDir());

            bool startMinimized = false;
            foreach (string argument in args)
            {
                if (argument == "tray")
                {
                    startMinimized = true;
                }
            }
            
            Application.Run(new Form1(startMinimized));
        }
    }
}