namespace DecokeeTray.State
{
    /// <summary>
    /// Maps behavior enum values to UI labels.
    /// </summary>
    internal static class BehaviorLabels
    {
        public static readonly string[] ButtonLabels =
        [
            Label(ButtonBehavior.FocusCurrentQuakeWindow),
            Label(ButtonBehavior.MoveWindowToQuake)
        ];

        public static readonly string[] KnobLabels =
        [
            Label(KnobBehavior.SwitchQuakeWindows),
            Label(KnobBehavior.QuakeLuminance),
            Label(KnobBehavior.Volume)
        ];

        /// <summary>
        /// Returns the UI label for a behavior.
        /// </summary>
        public static string Label(ButtonBehavior behavior)
        {
            return behavior switch
            {
                ButtonBehavior.MoveWindowToQuake => "Move Window to Quake",
                _ => "Focus Current Quake Window"
            };
        }

        /// <summary>
        /// Returns the UI label for a behavior.
        /// </summary>
        public static string Label(KnobBehavior behavior)
        {
            return behavior switch
            {
                KnobBehavior.QuakeLuminance => "Quake Luminance",
                KnobBehavior.Volume => "Volume",
                _ => "Switch Quake Windows"
            };
        }

        /// <summary>
        /// Parses a button behavior label.
        /// </summary>
        public static bool TryParseButton(string text, out ButtonBehavior behavior)
        {
            behavior = text switch
            {
                "Move Window to Quake" => ButtonBehavior.MoveWindowToQuake,
                _ => ButtonBehavior.FocusCurrentQuakeWindow
            };
            return true;
        }

        /// <summary>
        /// Parses a knob behavior label.
        /// </summary>
        public static bool TryParseKnob(string text, out KnobBehavior behavior)
        {
            behavior = text switch
            {
                "Quake Luminance" or "Quake luminance" => KnobBehavior.QuakeLuminance,
                "Volume" => KnobBehavior.Volume,
                _ => KnobBehavior.SwitchQuakeWindows
            };
            return true;
        }
    }
}
