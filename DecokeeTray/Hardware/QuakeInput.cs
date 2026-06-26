namespace DecokeeTray.Hardware
{
    /// <summary>
    /// Decoded Quake input event.
    /// </summary>
    internal sealed record QuakeInput(QuakeControl Control, KnobDirection Direction);
}
