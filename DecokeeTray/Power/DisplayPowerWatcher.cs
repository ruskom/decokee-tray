namespace DecokeeTray.Power
{
    /// <summary>
    /// Hidden window that receives display power notifications.
    /// </summary>
    internal sealed class DisplayPowerWatcher : NativeWindow, IDisposable
    {
        private const int WmPowerBroadcast = 0x0218;
        private const int PbtPowerSettingChange = 0x8013;
        private const int DeviceNotifyWindowHandle = 0x00000000;
        private static readonly Guid ConsoleDisplayState = new("6FE69556-704A-47A0-8F24-C28D936FDA47");

        private readonly Action<DisplayPowerState> _onChanged;
        private bool _disposed;
        private IntPtr _notificationHandle;

        /// <summary>
        /// Creates a display power watcher instance.
        /// </summary>
        public DisplayPowerWatcher(Action<DisplayPowerState> onChanged)
        {
            _onChanged = onChanged;
            CreateHandle(new CreateParams());
            var guid = ConsoleDisplayState;
            _notificationHandle = RegisterPowerSettingNotification(Handle, ref guid, DeviceNotifyWindowHandle);
        }

        /// <summary>
        /// Releases resources owned by this instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            if (_notificationHandle != IntPtr.Zero)
            {
                UnregisterPowerSettingNotification(_notificationHandle);
                _notificationHandle = IntPtr.Zero;
            }

            DestroyHandle();
        }

        /// <summary>
        /// Executes the wnd proc operation.
        /// </summary>
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmPowerBroadcast && message.WParam.ToInt32() == PbtPowerSettingChange)
            {
                var state = TryReadDisplayState(message.LParam);
                if (state is not null) _onChanged(state.Value);
            }

            base.WndProc(ref message);
        }

        /// <summary>
        /// Reads the display power state from a native message.
        /// </summary>
        private static DisplayPowerState? TryReadDisplayState(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return null;

            var guid = Marshal.PtrToStructure<Guid>(lParam);
            if (guid != ConsoleDisplayState) return null;

            var dataLength = Marshal.ReadInt32(lParam, 16);
            if (dataLength < sizeof(int)) return DisplayPowerState.Unknown;

            return Marshal.ReadInt32(lParam, 20) switch
            {
                0 => DisplayPowerState.Off,
                1 => DisplayPowerState.On,
                2 => DisplayPowerState.Dimmed,
                _ => DisplayPowerState.Unknown
            };
        }

        [DllImport("user32.dll", SetLastError = true)]
        /// <summary>
        /// Executes the register power setting notification operation.
        /// </summary>
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr recipient, ref Guid powerSettingGuid,
            int flags);

        [DllImport("user32.dll", SetLastError = true)]
        /// <summary>
        /// Executes the unregister power setting notification operation.
        /// </summary>
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);
    }
}
