using MelonLoader;

namespace CustomAlbums
{
    internal static class ModSettings
    {
        private static MelonPreferences_Entry<bool> _verboseLogging;
        private static MelonPreferences_Entry<bool> _savingEnabled;
        private static MelonPreferences_Entry<bool> _enableLogWriteToFile;
        public static bool VerboseLogging => _verboseLogging.Value;
        public static bool SavingEnabled => _savingEnabled.Value;
        public static bool LoggingToFileEnabled => _enableLogWriteToFile.Value;


        internal static void Register()
        {
            var category = MelonPreferences.CreateCategory("CustomAlbums", "Custom Albums");

            _verboseLogging = category.CreateEntry("VerboseLogging", false, "Verbose Logging");
            _savingEnabled = category.CreateEntry("SavingEnabled", true, "Enable Saving");
            _enableLogWriteToFile = category.CreateEntry("LogWriteToFile", false, "Enable Log Write to File");
        }
    }
}