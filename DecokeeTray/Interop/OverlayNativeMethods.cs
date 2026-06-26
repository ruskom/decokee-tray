namespace DecokeeTray.Interop
{
    /// <summary>
    /// Windows native calls for no-activate overlay placement.
    /// </summary>
    internal static class OverlayNativeMethods
    {
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private static readonly IntPtr HwndTopmost = new(-1);

        /// <summary>
        /// Shows a window as topmost without activation.
        /// </summary>
        public static void ShowTopmostNoActivate(IntPtr handle, Rectangle bounds)
        {
            SetWindowPos(
                handle,
                HwndTopmost,
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                SwpNoActivate | SwpShowWindow);
        }

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
