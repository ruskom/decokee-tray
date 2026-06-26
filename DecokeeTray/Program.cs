namespace DecokeeTray
{
    using DecokeeTray.Services;

    /// <summary>
    /// Application entry point.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        /// <summary>
        /// Starts the application.
        /// </summary>
        private static void Main()
        {
            AppLog.Initialize();
            Application.ThreadException += (_, e) => AppLog.Error("WinForms thread exception", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                    AppLog.Error("Unhandled exception", exception);
                else
                    AppLog.Write("Unhandled exception object: " + e.ExceptionObject);
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                AppLog.Error("Unobserved task exception", e.Exception);
                e.SetObserved();
            };

            using var singleInstance = SingleInstanceGuard.Acquire();
            if (!singleInstance.HasHandle)
            {
                AppLog.Write("Another Decokee Tray instance is already running; exiting.");
                return;
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            for (var attempt = 1; attempt <= 3; attempt++)
                try
                {
                    AppLog.Write($"Starting Decokee Tray, attempt {attempt}.");
                    using var context = new TrayContext();
                    Application.Run(context);
                    AppLog.Write("Decokee Tray exited normally.");
                    return;
                }
                catch (Exception ex) when (attempt < 3)
                {
                    AppLog.Error($"Startup failed on attempt {attempt}; retrying.", ex);
                    Thread.Sleep(TimeSpan.FromSeconds(attempt));
                }
                catch (Exception ex)
                {
                    AppLog.Error("Fatal startup failure.", ex);
                    throw;
                }
        }
    }
}
