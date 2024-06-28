using Il2Cpp;
using Il2CppAssets.Scripts.UI.Panels.PnlMusicTag;
using Il2CppSuperScrollView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CustomAlbums.Utilities;
using HarmonyLib;
using MelonLoader.NativeUtils;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using System.Runtime.CompilerServices;
using CustomAlbums.Managers;
using Il2CppInterop.Runtime;
using Il2CppAssets.Scripts.Database;
using static MelonLoader.MelonLogger;

namespace CustomAlbums.Patches
{
    internal class TreeItemPatch
    {
        // TODO: Finish "Album" support
        private static readonly Logger Logger = new(nameof(TreeItemPatch));

        [HarmonyPatch(typeof(PnlMusicTagItem), nameof(PnlMusicTagItem.OnTagClicked))]
        internal class OnMusicTagClickedPatch
        {
            private static void Prefix(int tagIndex, PnlMusicTagItem __instance)
            {
                // STUB
            }
        }

        [HarmonyPatch(typeof(PnlMusicTagItem), nameof(PnlMusicTagItem.Enable))]
        internal class EnablePatch
        {
            private static void Prefix(PnlMusicTagItem __instance)
            {
                // STUB
            }
        }

        [HarmonyPatch(typeof(PnlMusicTagItem), nameof(PnlMusicTagItem.AddDataToMgr))]
        internal class AddDataPatch
        {
           // STUB
        }
    }
}
