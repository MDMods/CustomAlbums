using Il2Cpp;
using CustomAlbums.Utilities;
using HarmonyLib;

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
