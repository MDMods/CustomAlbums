using MelonLoader;

namespace CustomAlbums
{
    internal static class ModSettings
    {
        private static MelonPreferences_Entry<bool> _verboseLogging;
        private static MelonPreferences_Entry<bool> _savingEnabled;
        public static bool VerboseLogging => _verboseLogging.Value;
        public static bool SavingEnabled => _savingEnabled.Value;


        internal static void Register()
        {
            var category = MelonPreferences.CreateCategory("CustomAlbums", "Custom Albums");

            _verboseLogging = category.CreateEntry("VerboseLogging", false, "Verbose Logging");
            _savingEnabled = category.CreateEntry("SavingEnabled", true, "Enable Saving");
        }
    }
}