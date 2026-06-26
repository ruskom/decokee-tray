namespace DecokeeTray.UI
{
    /// <summary>
    /// Invisible anchor form used to position the tooltip overlay.
    /// </summary>
    internal sealed class OverlayAnchorForm : Form
    {
        private const int WsExNoActivate = 0x08000000;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExTopmost = 0x00000008;

        /// <summary>
        /// Creates a overlay anchor form instance.
        /// </summary>
        public OverlayAnchorForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Opacity = 0;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= WsExNoActivate | WsExToolWindow | WsExTopmost;
                return parameters;
            }
        }
    }
}
