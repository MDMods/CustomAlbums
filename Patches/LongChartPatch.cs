﻿using HarmonyLib;
using Il2CppDYUnityLib;

namespace CustomAlbums.Patches
{
    /// <summary>
    ///     Changes the maximum length of a chart from 4 minutes to about 357913 minutes.
    ///     This does not change the fact that the maximum amount of chart elements is 32767.
    /// </summary>
    [HarmonyPatch(typeof(FixUpdateTimer), "Run")]
    internal class LongChartPatch
    {
        private static void Prefix(FixUpdateTimer __instance)
        {
            if (__instance.totalTick is >= 24000 and < int.MaxValue) __instance.totalTick = int.MaxValue;
        }
    }
}