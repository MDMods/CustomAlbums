using HarmonyLib;
using Il2CppPeroTools2.Log;

namespace CustomAlbums.Patches
{
    internal class SilenceLogPatch
    {
        /// <summary>
        ///     Disables log write-to-file if it is set in MelonPreferences (it is by default).
        /// </summary>
        [HarmonyPatch(typeof(PeroLogConfig), nameof(PeroLogConfig.instance), MethodType.Getter)]
        internal class LoadConfigPatch
        {
            private static void Postfix(ref PeroLogConfig __result)
            {
                if (ModSettings.LoggingToFileEnabled) return;
                __result.m_IsLogToFile = false;
            }
        }

    }
}
