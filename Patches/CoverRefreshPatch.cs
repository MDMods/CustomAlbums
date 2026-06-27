using HarmonyLib;
using Il2Cpp;

namespace CustomAlbums.Patches;

internal static class CoverRefreshPatch
{
    private static readonly Dictionary<MusicStageCell, string> ActiveCells = new();

    internal static void Refresh(string musicUid)
    {
        // Iterate over a copy to avoid enumeration modification exceptions if a cell destroys itself
        var cells = new Dictionary<MusicStageCell, string>(ActiveCells);
        foreach (var kvp in cells)
            if (kvp.Value == musicUid && kvp.Key != null && kvp.Key.gameObject.activeInHierarchy)
                kvp.Key.ForceRefreshData();
    }

    [HarmonyPatch(typeof(MusicStageCell), nameof(MusicStageCell.SetCoverLogic))]
    internal static class SetCoverLogicPatch
    {
        private static void Postfix(MusicStageCell __instance, MusicStageCell.MusicStageCellInfo cellInfo)
        {
            ActiveCells[__instance] = cellInfo.musicUid;
        }
    }

    [HarmonyPatch(typeof(MusicStageCell), nameof(MusicStageCell.OnDestroy))]
    internal static class OnDestroyPatch
    {
        private static void Postfix(MusicStageCell __instance)
        {
            ActiveCells.Remove(__instance);
        }
    }
}