namespace DecokeeTray.Interop
{
    /// <summary>
    /// Windows native keyboard events for audio control.
    /// </summary>
    internal static class AudioNativeMethods
    {
        private const byte VkVolumeDown = 0xAE;
        private const byte VkVolumeUp = 0xAF;
        private const uint KeyEventFKeyUp = 0x0002;

        /// <summary>
        /// Adjusts the system volume by one step.
        /// </summary>
        public static void AdjustVolume(int delta)
        {
            var key = delta >= 0 ? VkVolumeUp : VkVolumeDown;
            keybd_event(key, 0, 0, UIntPtr.Zero);
            keybd_event(key, 0, KeyEventFKeyUp, UIntPtr.Zero);
        }

        /// <summary>
        /// Executes the keybd event operation.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
    }
}
