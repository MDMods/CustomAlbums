using HarmonyLib;
using Il2CppDYUnityLib;

namespace CustomAlbums.Patches
{
    /// <summary>
    /// Changes the maximum length of a chart from 4 minutes to about 357913 minutes.
    /// </summary>
    [HarmonyPatch(typeof(FixUpdateTimer), "Run")]
    internal class LongChartPatch
    {
        private static void Prefix(FixUpdateTimer __instance)
        {
            if (__instance.totalTick >= 24000 && __instance.totalTick < int.MaxValue)
            {
                __instance.totalTick = int.MaxValue;
            }
        }
    }
}
