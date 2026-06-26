namespace DecokeeTray.UI
{
    /// <summary>
    /// Creates the tray icon bitmap.
    /// </summary>
    internal static class TrayIconFactory
    {
        /// <summary>
        /// Creates the tray icon.
        /// </summary>
        public static Icon Create()
        {
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var shadow = new Pen(Color.FromArgb(190, 0, 0, 0), 4);
            using var frame = new Pen(Color.FromArgb(24, 31, 42), 2);
            using var accent = new SolidBrush(Color.FromArgb(44, 132, 255));
            using var screen = new SolidBrush(Color.FromArgb(246, 248, 252));
            using var stand = new Pen(Color.FromArgb(24, 31, 42), 2);

            var display = new Rectangle(4, 6, 24, 15);
            graphics.DrawRoundedRectangle(shadow, display, 4);
            graphics.FillRoundedRectangle(screen, display, 4);
            graphics.DrawRoundedRectangle(frame, display, 4);
            graphics.FillRoundedRectangle(accent, new Rectangle(8, 11, 16, 4), 2);
            graphics.DrawLine(stand, 16, 21, 16, 25);
            graphics.DrawLine(stand, 10, 26, 22, 26);

            var handle = bitmap.GetHicon();
            try
            {
                return (Icon)Icon.FromHandle(handle).Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        /// <summary>
        /// Executes the destroy icon operation.
        /// </summary>
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
