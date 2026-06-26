namespace DecokeeTray.Windows
{
    /// <summary>
    /// Result of a Quake window-switch operation.
    /// </summary>
    internal sealed record WindowSwitchResult(string Status, string? OverlayText, Rectangle? ScreenBounds = null)
    {
        /// <summary>
        /// Creates a result without overlay text.
        /// </summary>
        public static WindowSwitchResult StatusOnly(string status)
        {
            return new WindowSwitchResult(status, null);
        }
    }
}
