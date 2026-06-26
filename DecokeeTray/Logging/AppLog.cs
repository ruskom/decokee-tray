namespace DecokeeTray.Logging
{
    /// <summary>
    /// Best-effort console and file logger for the tray app.
    /// </summary>
    internal static class AppLog
    {
        private const int AttachParentProcess = -1;
        private static readonly Lock Gate = new();
        private static string? _path;

        /// <summary>
        /// Initializes logging.
        /// </summary>
        public static void Initialize()
        {
            TryAttachParentConsole();
            try
            {
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
            catch
            {
                // WinExe may not have a console; file logging still works.
            }

            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Decokee",
                "QuakeTray");
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "decokee-tray.log");
            Write("Logging initialized. Log file: " + _path);
        }

        /// <summary>
        /// Writes a log message.
        /// </summary>
        public static void Write(string message)
        {
            var line = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture) + " " + message;
            lock (Gate)
            {
                try
                {
                    Console.Error.WriteLine(line);
                }
                catch
                {
                    // Ignore console failures; file logging is best effort too.
                }

                if (_path is null) return;

                try
                {
                    File.AppendAllText(_path, line + Environment.NewLine);
                }
                catch
                {
                    // Last-resort logger must never crash the app.
                }
            }
        }

        /// <summary>
        /// Writes exception details to the log.
        /// </summary>
        public static void Error(string message, Exception exception)
        {
            Write(message + Environment.NewLine + exception);
        }

        [DllImport("kernel32.dll")]
        /// <summary>
        /// Executes the attach console operation.
        /// </summary>
        private static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// Attempts to attach to the parent console.
        /// </summary>
        private static void TryAttachParentConsole()
        {
            try
            {
                AttachConsole(AttachParentProcess);
            }
            catch
            {
                // No parent console or attach denied.
            }
        }
    }
}
