namespace DecokeeTray.State
{
    /// <summary>
    /// Persisted tray application state.
    /// </summary>
    internal sealed record TrayState(QuakeSettings Settings, ButtonBehavior ButtonBehavior, KnobBehavior KnobBehavior)
    {
        public static TrayState Default { get; } = new(
            QuakeSettings.Default,
            ButtonBehavior.FocusCurrentQuakeWindow,
            KnobBehavior.SwitchQuakeWindows);
    }
}
