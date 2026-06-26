namespace DecokeeTray.State
{
    /// <summary>
    /// Persisted hardware settings for the Quake device.
    /// </summary>
    internal sealed record QuakeSettings(int Luminance, int KnobBrightness, Color Color)
    {
        public static QuakeSettings Default { get; } = new(255, 25, Color.FromArgb(0, 72, 255));
    }
}
