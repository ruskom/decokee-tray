namespace DecokeeTray.UI
{
    /// <summary>
    /// Drawing helpers for rounded rectangles.
    /// </summary>
    internal static class GraphicsExtensions
    {
        /// <summary>
        /// Fills a rounded rectangle.
        /// </summary>
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using var path = RoundedPath(bounds, radius);
            graphics.FillPath(brush, path);
        }

        /// <summary>
        /// Draws a rounded rectangle.
        /// </summary>
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using var path = RoundedPath(bounds, radius);
            graphics.DrawPath(pen, path);
        }

        /// <summary>
        /// Builds a rounded rectangle path.
        /// </summary>
        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
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
    }
}
