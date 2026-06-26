namespace DecokeeTray.State
{
    /// <summary>
    /// Loads and saves tray state from the user profile.
    /// </summary>
    internal static class TrayStateStore
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        /// <summary>
        /// Loads persisted tray state.
        /// </summary>
        public static TrayState Load()
        {
            try
            {
                var path = StatePath();
                if (!File.Exists(path)) return TrayState.Default;

                var persisted = JsonSerializer.Deserialize<PersistedTrayState>(File.ReadAllText(path), Options);
                if (persisted is null) return TrayState.Default;

                return new TrayState(
                    new QuakeSettings(
                        Math.Clamp(persisted.Luminance, 0, 255),
                        Math.Clamp(persisted.KnobBrightness, 0, 255),
                        Color.FromArgb(persisted.ColorArgb)),
                    ParseEnum(persisted.ButtonBehavior, ButtonBehavior.FocusCurrentQuakeWindow),
                    ParseEnum(persisted.KnobBehavior, KnobBehavior.SwitchQuakeWindows));
            }
            catch
            {
                return TrayState.Default;
            }
        }

        /// <summary>
        /// Saves tray state.
        /// </summary>
        public static void Save(TrayState state)
        {
            var path = StatePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var persisted = new PersistedTrayState(
                Math.Clamp(state.Settings.Luminance, 0, 255),
                Math.Clamp(state.Settings.KnobBrightness, 0, 255),
                state.Settings.Color.ToArgb(),
                state.ButtonBehavior.ToString(),
                state.KnobBehavior.ToString());
            File.WriteAllText(path, JsonSerializer.Serialize(persisted, Options));
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
            where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, true, out var parsed)
                ? parsed
                : fallback;
        }

        /// <summary>
        /// Builds the tray state file path.
        /// </summary>
        private static string StatePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Decokee",
                "QuakeTray",
                "state.json");
        }

        /// <summary>
        /// Provides persisted tray state behavior.
        /// </summary>
        private sealed record PersistedTrayState(
            int Luminance,
            int KnobBrightness,
            int ColorArgb,
            string? ButtonBehavior = null,
            string? KnobBehavior = null);
    }
}
