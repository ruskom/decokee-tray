namespace DecokeeTray.Interop
{
    /// <summary>
    /// Windows native calls for window enumeration and focus.
    /// </summary>
    internal static class WindowNativeMethods
    {
        /// <summary>
        /// Callback used by EnumWindows.
        /// </summary>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SwRestore = 9;
        private const uint SwpNoZOrder = 0x0004;

        /// <summary>
        /// Focuses a target window using UI Automation first.
        /// </summary>
        public static void FocusWindow(IntPtr handle)
        {
            var currentThread = GetCurrentThreadId();
            var foregroundWindow = GetForegroundWindow();
            var foregroundThread = foregroundWindow == IntPtr.Zero
                ? 0
                : GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
            var targetThread = GetWindowThreadProcessId(handle, IntPtr.Zero);

            var attachedForeground = false;
            var attachedTarget = false;
            try
            {
                if (foregroundThread != 0 && foregroundThread != currentThread)
                    attachedForeground = AttachThreadInput(currentThread, foregroundThread, true);

                if (targetThread != 0 && targetThread != currentThread)
                    attachedTarget = AttachThreadInput(currentThread, targetThread, true);

                ShowWindow(handle, SwRestore);
                if (TryAutomationFocus(handle)) return;

                BringWindowToTop(handle);
                SetForegroundWindow(handle);
                SetFocus(handle);
            }
            finally
            {
                if (attachedTarget) AttachThreadInput(currentThread, targetThread, false);

                if (attachedForeground) AttachThreadInput(currentThread, foregroundThread, false);
            }
        }

        /// <summary>
        /// Gets the window title text.
        /// </summary>
        public static string GetWindowTitle(IntPtr handle)
        {
            var length = GetWindowTextLength(handle);
            if (length <= 0) return string.Empty;

            var buffer = new char[length + 1];
            var read = GetWindowText(handle, buffer, buffer.Length);
            return read <= 0 ? string.Empty : new string(buffer, 0, read);
        }

        /// <summary>
        /// Gets the Win32 window class name.
        /// </summary>
        public static string GetWindowClassName(IntPtr handle)
        {
            var buffer = new char[256];
            var read = GetClassName(handle, buffer, buffer.Length);
            return read <= 0 ? string.Empty : new string(buffer, 0, read);
        }

        /// <summary>
        /// Gets the owning process ID for a window.
        /// </summary>
        public static int GetWindowProcessId(IntPtr handle)
        {
            GetWindowThreadProcessIdWithProcessId(handle, out var processId);
            return unchecked((int)processId);
        }

        /// <summary>
        /// Moves and resizes a window.
        /// </summary>
        public static void MoveAndResizeWindow(IntPtr handle, Rectangle bounds)
        {
            SetWindowPos(
                handle,
                IntPtr.Zero,
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                SwpNoZOrder);
        }

        /// <summary>
        /// Restores a minimized or hidden window.
        /// </summary>
        public static void RestoreWindow(IntPtr handle)
        {
            ShowWindow(handle, SwRestore);
        }

        /// <summary>
        /// Attempts to focus a window with UI Automation.
        /// </summary>
        private static bool TryAutomationFocus(IntPtr handle)
        {
            try
            {
                var automation = new CUIAutomation8();
                var element = automation.ElementFromHandle(handle);
                if (element is null) return false;

                element.SetFocus();
                return GetForegroundWindow() == handle;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Executes the enum windows operation.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr lParam);

        /// <summary>
        /// Executes the is window visible operation.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Gets window text length.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Gets window rect.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

        /// <summary>
        /// Gets foreground window.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Shows window.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Sets foreground window.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Executes the bring window to top operation.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        /// <summary>
        /// Sets focus.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        /// <summary>
        /// Gets window thread process id.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        /// <summary>
        /// Gets window thread process id with process id.
        /// </summary>
        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        private static extern uint GetWindowThreadProcessIdWithProcessId(IntPtr hWnd, out uint processId);

        /// <summary>
        /// Executes the attach thread input operation.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        /// <summary>
        /// Gets current thread id.
        /// </summary>
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        /// <summary>
        /// Gets window text.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, char[] text, int count);

        /// <summary>
        /// Gets class name.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, char[] className, int maxCount);

        /// <summary>
        /// Sets window pos.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);
    }
}
