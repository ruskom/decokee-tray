using Timer = System.Windows.Forms.Timer;

namespace DecokeeTray.UI
{
    /// <summary>
    /// Tray popup for Quake settings and behavior selection.
    /// </summary>
    internal sealed class SettingsPopupForm : Form
    {
        private readonly Timer _applyTimer;
        private readonly ComboBox _buttonBehavior;
        private readonly Button _colorButton;
        private readonly CheckBox _keepAliveCheck;
        private readonly ComboBox _knobBehavior;
        private readonly TrackBar _knobBrightness;
        private readonly TrackBar _luminance;
        private readonly ThemePalette _palette;
        private readonly Label _status;
        private Color _color;
        private bool _keepAliveEnabled = true;

        /// <summary>
        /// Creates a settings popup form instance.
        /// </summary>
        public SettingsPopupForm(QuakeSettings settings, ButtonBehavior buttonBehavior, KnobBehavior knobBehavior)
        {
            _palette = ThemePalette.Current();
            Text = "Decokee Quake";
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ClientSize = new Size(390, 390);
            BackColor = _palette.Background;
            ForeColor = _palette.Text;
            Padding = new Padding(14);
            DoubleBuffered = true;

            _color = settings.Color;

            _luminance = CreateTrackBar(settings.Luminance);
            _knobBrightness = CreateTrackBar(settings.KnobBrightness);
            _buttonBehavior = CreateComboBox();
            _buttonBehavior.Items.AddRange(BehaviorLabels.ButtonLabels.Cast<object>().ToArray());
            _buttonBehavior.SelectedItem = BehaviorLabels.Label(buttonBehavior);
            _buttonBehavior.SelectedIndexChanged += (_, _) =>
            {
                if (BehaviorLabels.TryParseButton(_buttonBehavior.Text, out var behavior))
                    ButtonBehaviorChanged?.Invoke(this, behavior);
            };

            _knobBehavior = CreateComboBox();
            _knobBehavior.Items.AddRange(BehaviorLabels.KnobLabels.Cast<object>().ToArray());
            _knobBehavior.SelectedItem = BehaviorLabels.Label(knobBehavior);
            _knobBehavior.SelectedIndexChanged += (_, _) =>
            {
                if (BehaviorLabels.TryParseKnob(_knobBehavior.Text, out var behavior))
                    KnobBehaviorChanged?.Invoke(this, behavior);
            };

            _colorButton = CreateButton("Color");
            _colorButton.Click += (_, _) => PickColor();

            _keepAliveCheck = new CheckBox
            {
                Text = "Keepalive",
                Left = 22,
                Top = 320,
                Width = 160,
                Height = 30,
                Checked = true,
                ForeColor = _palette.Text,
                BackColor = _palette.Background,
                FlatStyle = FlatStyle.Flat
            };
            _keepAliveCheck.CheckedChanged += (_, _) =>
            {
                if (_keepAliveCheck.Checked != _keepAliveEnabled) KeepAliveChanged?.Invoke(this, _keepAliveCheck.Checked);
            };

            _status = new Label
            {
                Text = "Ready",
                AutoEllipsis = true,
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = _palette.MutedText,
                BackColor = _palette.Background
            };

            _applyTimer = new Timer { Interval = 250 };
            _applyTimer.Tick += (_, _) =>
            {
                _applyTimer.Stop();
                SettingsApplied?.Invoke(this, CurrentSettings());
            };

            _luminance.ValueChanged += (_, _) => ScheduleApply();
            _knobBrightness.ValueChanged += (_, _) => ScheduleApply();

            Controls.Add(Header());
            Controls.Add(Row("Screen luminance", _luminance, 64, _palette));
            Controls.Add(Row("Knob brightness", _knobBrightness, 114, _palette));
            Controls.Add(LabelAt("Knob color", 22, 173, _palette));
            _colorButton.Location = new Point(164, 166);
            Controls.Add(_colorButton);
            Controls.Add(LabelAt("Button behavior", 22, 216, _palette));
            _buttonBehavior.Location = new Point(164, 209);
            Controls.Add(_buttonBehavior);
            Controls.Add(LabelAt("Knob behavior", 22, 266, _palette));
            _knobBehavior.Location = new Point(164, 259);
            Controls.Add(_knobBehavior);
            Controls.Add(_keepAliveCheck);
            Controls.Add(_status);
            UpdateColorButton();
        }

        public event EventHandler<QuakeSettings>? SettingsApplied;
        public event EventHandler<ButtonBehavior>? ButtonBehaviorChanged;
        public event EventHandler<KnobBehavior>? KnobBehaviorChanged;
        public event EventHandler<bool>? KeepAliveChanged;

        /// <summary>
        /// Shows the popup near the tray icon.
        /// </summary>
        public void ShowNearTray()
        {
            var cursor = Cursor.Position;
            var screen = Screen.FromPoint(cursor);
            var area = screen.WorkingArea;
            var x = Math.Clamp(cursor.X - Width / 2, area.Left + 8, area.Right - Width - 8);
            var y = area.Bottom - Height - 12;
            Location = new Point(x, Math.Max(area.Top + 8, y));
            Show();
        }

        /// <summary>
        /// Updates the popup status text.
        /// </summary>
        public void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => SetStatus(message));
                return;
            }

            _status.Text = message;
        }

        /// <summary>
        /// Updates keepalive controls in the popup.
        /// </summary>
        public void SetKeepAliveState(bool enabled, bool effectiveEnabled)
        {
            _keepAliveEnabled = enabled;
            if (_keepAliveCheck is not null)
            {
                _keepAliveCheck.Checked = enabled;
                _keepAliveCheck.Text = effectiveEnabled || !enabled ? "Keepalive" : "Keepalive paused";
            }
        }

        /// <summary>
        /// Updates the luminance slider.
        /// </summary>
        public void SetLuminance(int value)
        {
            var clamped = Math.Clamp(value, _luminance.Minimum, _luminance.Maximum);
            if (_luminance.Value != clamped) _luminance.Value = clamped;
        }

        /// <summary>
        /// Builds settings from the current controls.
        /// </summary>
        private QuakeSettings CurrentSettings()
        {
            return new QuakeSettings(_luminance.Value, _knobBrightness.Value, _color);
        }

        /// <summary>
        /// Shows the color picker.
        /// </summary>
        private void PickColor()
        {
            using var dialog = new ColorDialog
            {
                Color = _color,
                FullOpen = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _color = dialog.Color;
                UpdateColorButton();
                ScheduleApply();
            }
        }

        /// <summary>
        /// Schedules delayed settings application.
        /// </summary>
        private void ScheduleApply()
        {
            _applyTimer.Stop();
            _applyTimer.Start();
        }

        /// <summary>
        /// Updates the color button appearance.
        /// </summary>
        private void UpdateColorButton()
        {
            _colorButton.BackColor = _color;
            _colorButton.ForeColor = _color.GetBrightness() < 0.45 ? Color.White : Color.Black;
            _colorButton.Text = $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}";
        }

        /// <summary>
        /// Creates a styled slider.
        /// </summary>
        private static TrackBar CreateTrackBar(int value)
        {
            return new TrackBar
            {
                Minimum = 0,
                Maximum = 255,
                TickFrequency = 25,
                Value = Math.Clamp(value, 0, 255),
                Width = 190,
                Height = 42
            };
        }

        /// <summary>
        /// Builds the popup header.
        /// </summary>
        private Control Header()
        {
            var panel = new Panel { Left = 18, Top = 14, Width = 350, Height = 44, BackColor = _palette.Background };
            panel.Controls.Add(new Label
            {
                Text = "Decokee Quake",
                Left = 0,
                Top = 0,
                Width = 220,
                Height = 24,
                Font = new Font(Font.FontFamily, 11.5f, FontStyle.Bold),
                ForeColor = _palette.Text,
                BackColor = _palette.Background
            });
            panel.Controls.Add(new Label
            {
                Text = "Tray controls",
                Left = 0,
                Top = 24,
                Width = 220,
                Height = 20,
                Font = new Font(Font.FontFamily, 8.5f),
                ForeColor = _palette.MutedText,
                BackColor = _palette.Background
            });
            return panel;
        }

        /// <summary>
        /// Builds a labeled slider row.
        /// </summary>
        private static Control Row(string label, TrackBar trackBar, int top, ThemePalette palette)
        {
            var panel = new Panel { Left = 22, Top = top, Width = 340, Height = 46, BackColor = palette.Background };
            panel.Controls.Add(new Label
            {
                Text = label,
                Left = 0,
                Top = 9,
                Width = 142,
                Height = 22,
                ForeColor = palette.Text,
                BackColor = palette.Background
            });
            trackBar.Left = 142;
            trackBar.Top = 0;
            trackBar.BackColor = palette.Background;
            panel.Controls.Add(trackBar);
            return panel;
        }

        /// <summary>
        /// Creates a positioned label.
        /// </summary>
        private static Label LabelAt(string text, int left, int top, ThemePalette palette)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 130,
                Height = 22,
                ForeColor = palette.Text,
                BackColor = palette.Background
            };
        }

        /// <summary>
        /// Creates a styled button.
        /// </summary>
        private Button CreateButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 126,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = _palette.Control,
                ForeColor = _palette.Text
            };
        }

        /// <summary>
        /// Creates a styled combo box.
        /// </summary>
        private ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                Width = 190,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = _palette.Control,
                ForeColor = _palette.Text
            };
        }

        /// <summary>
        /// Paints the popup chrome.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 14);
            using var fill = new SolidBrush(_palette.Background);
            using var border = new Pen(_palette.Border);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        /// <summary>
        /// Updates the rounded window region.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = RoundedRectangle(new Rectangle(0, 0, Width, Height), 14);
            Region = new Region(path);
        }

        /// <summary>
        /// Hides the popup when focus leaves it.
        /// </summary>
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
        }

        /// <summary>
        /// Builds a rounded rectangle path.
        /// </summary>
        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Disposes popup resources after closing.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _applyTimer.Stop();
            _applyTimer.Dispose();
            base.OnFormClosed(e);
        }
    }
}
