namespace DecokeeTray.Windows
{
    /// <summary>
    /// Finds, moves, and focuses windows on the Quake display.
    /// </summary>
    internal sealed class WindowSwitcher
    {
        private IntPtr _lastSelectedHandle;

        /// <summary>
        /// Gets the selected Quake screen bounds.
        /// </summary>
        public Rectangle? TargetScreenBounds()
        {
            return FindQuakeScreen()?.Bounds;
        }

        /// <summary>
        /// Focuses the first window on the Quake display.
        /// </summary>
        public string FocusTop()
        {
            var target = FindQuakeScreen();
            if (target is null) return "No Quake-like display found";

            var selected = EnumerateWindows()
                .FirstOrDefault(window => Intersects(window.Bounds, target.Bounds));

            if (selected is null) return "No windows on Quake display";

            Focus(selected);
            _lastSelectedHandle = selected.Handle;
            return $"Focused {selected.Title}";
        }

        /// <summary>
        /// Moves the active window to the Quake display.
        /// </summary>
        public string MoveForegroundToQuake()
        {
            var target = FindQuakeScreen();
            if (target is null) return "No Quake-like display found";

            var handle = WindowNativeMethods.GetForegroundWindow();
            if (handle == IntPtr.Zero) return "No active window";

            if (WindowNativeMethods.GetWindowProcessId(handle) == Environment.ProcessId) return "Active window is Decokee Tray";

            if (!WindowNativeMethods.IsWindowVisible(handle) || WindowNativeMethods.GetWindowTextLength(handle) == 0)
                return "Active window cannot be moved";

            var title = WindowNativeMethods.GetWindowTitle(handle);
            var className = WindowNativeMethods.GetWindowClassName(handle);
            if (string.IsNullOrWhiteSpace(title) || IsIgnoredWindow(title, className))
                return "Active window cannot be moved";

            WindowNativeMethods.RestoreWindow(handle);
            WindowNativeMethods.MoveAndResizeWindow(handle, target.Bounds);
            WindowNativeMethods.FocusWindow(handle);
            _lastSelectedHandle = handle;
            return $"Moved {title} to Quake";
        }

        /// <summary>
        /// Cycles through windows on the Quake display.
        /// </summary>
        public WindowSwitchResult Cycle(int delta)
        {
            var target = FindQuakeScreen();
            if (target is null) return WindowSwitchResult.StatusOnly("No Quake-like display found");

            var windows = EnumerateWindows()
                .Where(window => Intersects(window.Bounds, target.Bounds))
                .OrderBy(window => window.Bounds.Left)
                .ThenBy(window => window.Bounds.Top)
                .ThenBy(window => window.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (windows.Length == 0) return WindowSwitchResult.StatusOnly("No windows on Quake display");

            var foreground = WindowNativeMethods.GetForegroundWindow();
            var index = Array.FindIndex(windows, window => window.Handle == foreground);
            if (index < 0 && _lastSelectedHandle != IntPtr.Zero)
                index = Array.FindIndex(windows, window => window.Handle == _lastSelectedHandle);

            var next = index < 0
                ? delta > 0 ? 0 : windows.Length - 1
                : Mod(index + delta, windows.Length);

            var selected = windows[next];
            Focus(selected);
            _lastSelectedHandle = selected.Handle;
            return new WindowSwitchResult($"Switched to {selected.Title}", selected.Title, target.Bounds);
        }

        /// <summary>
        /// Formats windows currently on the Quake display.
        /// </summary>
        public string DebugWindowList()
        {
            var target = FindQuakeScreen();
            if (target is null) return "No Quake-like display found.";

            var windows = EnumerateWindows()
                .Where(window => Intersects(window.Bounds, target.Bounds))
                .ToArray();

            if (windows.Length == 0) return $"No windows on Quake display. Bounds: {target.Bounds}";

            return "Target screen: " + target.Bounds + Environment.NewLine + Environment.NewLine +
                   string.Join(Environment.NewLine, windows.Select(window =>
                       $"{window.Title} [{window.ClassName}] {window.Bounds}"));
        }

        /// <summary>
        /// Focuses a window entry.
        /// </summary>
        private static void Focus(WindowInfo window)
        {
            WindowNativeMethods.FocusWindow(window.Handle);
        }

        /// <summary>
        /// Finds the likely Quake display.
        /// </summary>
        private static Screen? FindQuakeScreen()
        {
            return Screen.AllScreens
                .Where(screen => !screen.Primary)
                .OrderByDescending(screen => AspectScore(screen.Bounds))
                .ThenBy(screen => screen.Bounds.Top)
                .ThenBy(screen => screen.Bounds.Left)
                .FirstOrDefault();
        }

        /// <summary>
        /// Scores a screen by Quake-like aspect ratio.
        /// </summary>
        private static double AspectScore(Rectangle bounds)
        {
            var aspect = bounds.Height == 0 ? 0 : (double)bounds.Width / bounds.Height;
            var quakeBonus = bounds.Width >= 1500 && bounds.Height <= 700 ? 10 : 0;
            return aspect + quakeBonus;
        }

        /// <summary>
        /// Enumerates visible candidate windows.
        /// </summary>
        private static IEnumerable<WindowInfo> EnumerateWindows()
        {
            var windows = new List<WindowInfo>();
            WindowNativeMethods.EnumWindows((handle, _) =>
            {
                if (!WindowNativeMethods.IsWindowVisible(handle) || WindowNativeMethods.GetWindowTextLength(handle) == 0) return true;

                if (WindowNativeMethods.GetWindowProcessId(handle) == Environment.ProcessId) return true;

                if (!WindowNativeMethods.GetWindowRect(handle, out var rect)) return true;

                var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                if (bounds.Width <= 0 || bounds.Height <= 0) return true;

                var title = WindowNativeMethods.GetWindowTitle(handle);
                if (string.IsNullOrWhiteSpace(title)) return true;

                var className = WindowNativeMethods.GetWindowClassName(handle);
                if (IsIgnoredWindow(title, className)) return true;

                windows.Add(new WindowInfo(handle, title, className, bounds));
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        /// <summary>
        /// Filters shell and desktop windows.
        /// </summary>
        private static bool IsIgnoredWindow(string title, string className)
        {
            return title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase) ||
                   className is "Progman" or "WorkerW" or "Shell_TrayWnd";
        }

        /// <summary>
        /// Checks whether two rectangles overlap enough.
        /// </summary>
        private static bool Intersects(Rectangle left, Rectangle right)
        {
            var intersection = Rectangle.Intersect(left, right);
            return intersection.Width > 20 && intersection.Height > 20;
        }

        /// <summary>
        /// Returns a positive modulo result.
        /// </summary>
        private static int Mod(int value, int modulo)
        {
            return (value % modulo + modulo) % modulo;
        }

        /// <summary>
        /// Provides window info behavior.
        /// </summary>
        private sealed record WindowInfo(IntPtr Handle, string Title, string ClassName, Rectangle Bounds);
    }
}
