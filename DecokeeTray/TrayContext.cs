namespace DecokeeTray
{
    /// <summary>
    /// Main WinForms tray application context.
    /// </summary>
    internal sealed class TrayContext : ApplicationContext
    {
        private readonly QuakeDevice _device = new();
        private readonly QuakeInputWatcher _inputWatcher;
        private readonly KeepAliveController _keepAlive;
        private readonly ToolStripMenuItem _keepAliveMenuItem;
        private readonly NotifyIcon _notifyIcon;
        private readonly OverlayController _overlay = new();
        private readonly Icon _trayIcon;
        private readonly SynchronizationContext _uiContext;
        private readonly WindowSwitcher _windowSwitcher = new();
        private ButtonBehavior _buttonBehavior;
        private KnobBehavior _knobBehavior;
        private QuakeSettings _settings;
        private SettingsPopupForm? _settingsForm;

        /// <summary>
        /// Creates a tray context instance.
        /// </summary>
        public TrayContext()
        {
            _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            var state = TrayStateStore.Load();
            _settings = state.Settings;
            _buttonBehavior = state.ButtonBehavior;
            _knobBehavior = state.KnobBehavior;

            _trayIcon = TrayIconFactory.Create();
            _keepAliveMenuItem = new ToolStripMenuItem("Keepalive")
            {
                CheckOnClick = true,
                Checked = true
            };
            _keepAliveMenuItem.CheckedChanged += (_, _) => SetKeepAlive(_keepAliveMenuItem.Checked);

            _notifyIcon = new NotifyIcon
            {
                Icon = _trayIcon,
                Text = "Decokee Quake",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            _notifyIcon.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left) ToggleSettingsPopup();
            };
            _notifyIcon.DoubleClick += (_, _) => ToggleSettingsPopup();

            _keepAlive = new KeepAliveController(_device, SetStatus, (_, _) => SyncKeepAliveState());

            ApplySettings();
            _keepAlive.PingNow();
            _inputWatcher = new QuakeInputWatcher(input => _uiContext.Post(_ => OnQuakeInput(input), null));
        }

        /// <summary>
        /// Builds the tray context menu.
        /// </summary>
        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(_keepAliveMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => ExitThread());
            return menu;
        }

        /// <summary>
        /// Shows or hides the settings popup.
        /// </summary>
        private void ToggleSettingsPopup()
        {
            if (_settingsForm is not null && !_settingsForm.IsDisposed && _settingsForm.Visible)
            {
                _settingsForm.Hide();
                return;
            }

            if (_settingsForm is null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsPopupForm(_settings, _buttonBehavior, _knobBehavior);
                _settingsForm.SettingsApplied += (_, settings) =>
                {
                    _settings = settings;
                    SaveState();
                    ApplySettings();
                };
                _settingsForm.ButtonBehaviorChanged += (_, behavior) =>
                {
                    _buttonBehavior = behavior;
                    SaveState();
                    SetStatus("Button: " + BehaviorLabels.Label(behavior));
                };
                _settingsForm.KnobBehaviorChanged += (_, behavior) =>
                {
                    _knobBehavior = behavior;
                    SaveState();
                    SetStatus("Knob: " + BehaviorLabels.Label(behavior));
                };
                _settingsForm.KeepAliveChanged += (_, enabled) => SetKeepAlive(enabled);
                _settingsForm.FormClosed += (_, _) => _settingsForm = null;
            }

            _settingsForm.SetKeepAliveState(_keepAlive.UserEnabled, _keepAlive.EffectiveEnabled);
            _settingsForm.ShowNearTray();
            _settingsForm.Activate();
        }

        /// <summary>
        /// Applies the keepalive checkbox state.
        /// </summary>
        private void SetKeepAlive(bool enabled)
        {
            if (_keepAlive.UserEnabled == enabled && _keepAliveMenuItem.Checked == enabled) return;

            _keepAlive.SetUserEnabled(enabled);
            SyncKeepAliveState();
        }

        /// <summary>
        /// Synchronizes keepalive UI state.
        /// </summary>
        private void SyncKeepAliveState()
        {
            _keepAliveMenuItem.Checked = _keepAlive.UserEnabled;
            _settingsForm?.SetKeepAliveState(_keepAlive.UserEnabled, _keepAlive.EffectiveEnabled);
        }

        /// <summary>
        /// Applies hardware settings to the device.
        /// </summary>
        private void ApplySettings()
        {
            try
            {
                _device.SetKnobLed(false);
                _device.SetLuminance(_settings.Luminance);
                _device.SetRgbMatrix(_settings.KnobBrightness, 1, _settings.Color);
                SetStatus("Settings applied");
            }
            catch (Exception ex)
            {
                SetStatus("Apply failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Routes decoded Quake input to the selected action.
        /// </summary>
        private void OnQuakeInput(QuakeInput input)
        {
            if (input.Control == QuakeControl.Button)
            {
                SetStatus(_buttonBehavior switch
                {
                    ButtonBehavior.MoveWindowToQuake => _windowSwitcher.MoveForegroundToQuake(),
                    _ => _windowSwitcher.FocusTop()
                });
                return;
            }

            switch (_knobBehavior)
            {
                case KnobBehavior.QuakeLuminance:
                    AdjustLuminance(input.Direction == KnobDirection.Right ? 10 : -10);
                    break;
                case KnobBehavior.Volume:
                    AudioNativeMethods.AdjustVolume(input.Direction == KnobDirection.Right ? 1 : -1);
                    SetStatus(input.Direction == KnobDirection.Right ? "Volume up" : "Volume down");
                    break;
                default:
                    var result = _windowSwitcher.Cycle(input.Direction == KnobDirection.Right ? 1 : -1);
                    SetStatus(result.Status);
                    if (!string.IsNullOrWhiteSpace(result.OverlayText))
                        _overlay.Show(result.OverlayText, result.ScreenBounds);

                    break;
            }
        }

        /// <summary>
        /// Adjusts Quake luminance and persists the value.
        /// </summary>
        private void AdjustLuminance(int delta)
        {
            var value = Math.Clamp(_settings.Luminance + delta, 0, 255);
            _settings = _settings with { Luminance = value };
            SaveState();
            _settingsForm?.SetLuminance(value);
            try
            {
                _device.SetLuminance(value);
                SetStatus("Quake luminance " + value);
            }
            catch (Exception ex)
            {
                SetStatus("Luminance failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Updates the popup status text.
        /// </summary>
        private void SetStatus(string message)
        {
            _notifyIcon.Text = message.Length > 63 ? message[..63] : message;
            _settingsForm?.SetStatus(message);
        }

        /// <summary>
        /// Persists the current tray state.
        /// </summary>
        private void SaveState()
        {
            TrayStateStore.Save(new TrayState(_settings, _buttonBehavior, _knobBehavior));
        }

        /// <summary>
        /// Disposes tray resources during application exit.
        /// </summary>
        protected override void ExitThreadCore()
        {
            _keepAlive.Dispose();
            _inputWatcher.Dispose();
            _overlay.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayIcon.Dispose();
            _settingsForm?.Close();
            base.ExitThreadCore();
        }
    }
}
