namespace DecokeeTray.UI
{
    /// <summary>
    /// Color palette derived from the current Windows theme.
    /// </summary>
    internal sealed record ThemePalette(Color Background, Color Control, Color Border, Color Text, Color MutedText)
    {
        /// <summary>
        /// Creates a palette for the current system theme.
        /// </summary>
        public static ThemePalette Current()
        {
            return IsSystemDarkMode()
                ? new ThemePalette(
                    Color.FromArgb(28, 28, 30),
                    Color.FromArgb(44, 44, 48),
                    Color.FromArgb(70, 70, 76),
                    Color.FromArgb(245, 245, 247),
                    Color.FromArgb(160, 160, 168))
                : new ThemePalette(
                    Color.FromArgb(248, 248, 250),
                    Color.White,
                    Color.FromArgb(214, 216, 222),
                    Color.FromArgb(28, 30, 34),
                    Color.FromArgb(96, 100, 108));
        }

        /// <summary>
        /// Detects whether Windows is using dark app mode.
        /// </summary>
        private static bool IsSystemDarkMode()
        {
            try
            {
                var value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme",
                    1);
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
