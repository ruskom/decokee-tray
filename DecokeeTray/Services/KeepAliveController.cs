using Timer = System.Windows.Forms.Timer;

namespace DecokeeTray.Services
{
    /// <summary>
    /// Coordinates Quake keepalive timing and display power state.
    /// </summary>
    internal sealed class KeepAliveController : IDisposable
    {
        private readonly QuakeDevice _device;
        private readonly DisplayPowerWatcher _displayPowerWatcher;
        private readonly Action<bool, bool> _stateChanged;
        private readonly Action<string> _statusChanged;
        private readonly Timer _timer;
        private bool _displayAllowsKeepAlive = true;
        private bool _userEnabled = true;

        /// <summary>
        /// Creates a keep alive controller instance.
        /// </summary>
        public KeepAliveController(
            QuakeDevice device,
            Action<string> statusChanged,
            Action<bool, bool> stateChanged)
        {
            _device = device;
            _statusChanged = statusChanged;
            _stateChanged = stateChanged;

            _timer = new Timer { Interval = 15_000 };
            _timer.Tick += (_, _) => PingNow();

            _displayPowerWatcher = new DisplayPowerWatcher(OnDisplayStateChanged);
            UpdateTimer(false);
        }

        public bool EffectiveEnabled => _userEnabled && _displayAllowsKeepAlive;

        public bool UserEnabled => _userEnabled;

        /// <summary>
        /// Releases resources owned by this instance.
        /// </summary>
        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            _displayPowerWatcher.Dispose();
        }

        /// <summary>
        /// Runs one keepalive ping if enabled.
        /// </summary>
        public void PingNow()
        {
            if (!EffectiveEnabled) return;

            try
            {
                _device.Ping();
                _statusChanged("Keepalive OK " + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                _statusChanged("Keepalive failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Sets the user keepalive preference.
        /// </summary>
        public void SetUserEnabled(bool enabled)
        {
            if (_userEnabled == enabled) return;

            _userEnabled = enabled;
            UpdateTimer(enabled);
            _statusChanged(enabled ? "Keepalive enabled" : "Keepalive disabled");
            NotifyStateChanged();
        }

        /// <summary>
        /// Notifies listeners about keepalive state.
        /// </summary>
        private void NotifyStateChanged()
        {
            _stateChanged(_userEnabled, EffectiveEnabled);
        }

        /// <summary>
        /// Handles display power state changes.
        /// </summary>
        private void OnDisplayStateChanged(DisplayPowerState state)
        {
            _displayAllowsKeepAlive = state is DisplayPowerState.On or DisplayPowerState.Unknown;
            UpdateTimer(_displayAllowsKeepAlive);

            var message = state switch
            {
                DisplayPowerState.Off => "Primary display off; keepalive paused",
                DisplayPowerState.Dimmed => "Primary display dimmed; keepalive paused",
                DisplayPowerState.On => "Primary display on; keepalive resumed",
                _ => "Display state unknown"
            };
            _statusChanged(message);
            NotifyStateChanged();
        }

        /// <summary>
        /// Starts or stops the keepalive timer.
        /// </summary>
        private void UpdateTimer(bool runImmediately)
        {
            if (EffectiveEnabled)
            {
                _timer.Start();
                if (runImmediately) PingNow();
            }
            else
            {
                _timer.Stop();
            }
        }
    }
}
