using Timer = System.Windows.Forms.Timer;

namespace DecokeeTray.UI
{
    /// <summary>
    /// Displays the window-switching overlay on the Quake screen.
    /// </summary>
    internal sealed class OverlayController : IDisposable
    {
        private static readonly Size OverlaySize = new(900, 150);
        private readonly OverlayAnchorForm _anchor = new();
        private readonly Timer _showTimer = new() { Interval = 150 };
        private readonly ToolTip _toolTip = new();
        private string _text = string.Empty;

        /// <summary>
        /// Creates a overlay controller instance.
        /// </summary>
        public OverlayController()
        {
            _toolTip.OwnerDraw = true;
            _toolTip.ShowAlways = true;
            _toolTip.Popup += (_, e) => { e.ToolTipSize = OverlaySize; };
            _toolTip.Draw += (_, e) =>
            {
                using var background = new SolidBrush(Color.FromArgb(18, 24, 34));
                using var font = new Font("Segoe UI", 24, FontStyle.Bold);
                e.Graphics.FillRectangle(background, e.Bounds);
                var textBounds = Rectangle.Inflate(e.Bounds, -36, -18);
                TextRenderer.DrawText(
                    e.Graphics,
                    _text,
                    font,
                    textBounds,
                    Color.White,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.WordBreak |
                    TextFormatFlags.EndEllipsis);
            };
            _showTimer.Tick += (_, _) =>
            {
                _showTimer.Stop();
                _toolTip.Show(_text, _anchor, 0, 0, 1_500);
            };
            _ = _anchor.Handle;
        }

        /// <summary>
        /// Releases resources owned by this instance.
        /// </summary>
        public void Dispose()
        {
            _showTimer.Stop();
            _showTimer.Dispose();
            _toolTip.Dispose();
            _anchor.Dispose();
        }

        /// <summary>
        /// Shows the overlay text on the target screen.
        /// </summary>
        public void Show(string text, Rectangle? screenBounds)
        {
            if (screenBounds is null) return;

            _text = text;
            var bounds = OverlayBounds(screenBounds.Value);
            _anchor.Bounds = bounds;
            OverlayNativeMethods.ShowTopmostNoActivate(_anchor.Handle, _anchor.Bounds);
            _toolTip.Hide(_anchor);
            _showTimer.Stop();
            _showTimer.Start();
        }

        /// <summary>
        /// Calculates centered overlay bounds.
        /// </summary>
        private static Rectangle OverlayBounds(Rectangle screen)
        {
            return new Rectangle(
                screen.Left + (screen.Width - OverlaySize.Width) / 2,
                screen.Top + (screen.Height - OverlaySize.Height) / 2,
                OverlaySize.Width,
                OverlaySize.Height);
        }
    }
}
