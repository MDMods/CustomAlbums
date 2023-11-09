using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace CustomAlbums.Utilities
{
    internal class Debug
    {
        [HarmonyPatch("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher", "ReportException")]
        internal static class Il2CppDetourMethodPatcherPatch
        {
            private static readonly Logger Logger = new(nameof(Il2CppDetourMethodPatcherPatch));
            private static bool Prefix(Exception ex)
            {
                Logger.Msg("During invoking native->managed trampoline: " +  ex);
                return false;
            }
        }
    }
}
