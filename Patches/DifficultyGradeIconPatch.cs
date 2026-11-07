using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.UI.Panels;

namespace CustomAlbums.Patches;

[HarmonyPatch(typeof(PnlStage), nameof(PnlStage.RefreshDiffUI))]
internal class DifficultyGradeIconPatch
{
    private static int GetEvaluate(Dictionary<int, ChartSave> highest, int diff)
    {
        return highest.GetValueOrDefault(diff)?.Evaluate ?? -1;
    }

    private static int GetVanillaEvaluate(string uid, int diff)
    {
        return DataHelper.highest.GetIDataByUid(uid, diff)?.ToChartSave().Evaluate ?? -1;
    }

    /// <summary>
    ///     Enables the S logos on the difficulties for custom charts.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static void Postfix(MusicInfo musicInfo, PnlStage __instance)
    {
        var uid = musicInfo?.uid;
        var specialSongInstance = Singleton<SpecialSongManager>.instance;

        if (string.IsNullOrEmpty(uid)) return;

        var isCustom = uid.StartsWith($"{AlbumManager.Uid}-");
        var customHighest = isCustom ? SaveManager.SaveData.GetChartSaveDataFromUid(uid)?.Highest : null;

        // For custom charts, we need to set the S logos for each difficulty
        if (isCustom)
        {
            if (customHighest == null) return;

            // Get the Evaluate value from each, set diff3 to diff4 if hidden is invoked
            var diff1 = GetEvaluate(customHighest, 1);
            var diff2 = GetEvaluate(customHighest, 2);
            var diff3 = Singleton<SpecialSongManager>.instance.IsInvokeHideBms(uid)
                ? GetEvaluate(customHighest, 4)
                : GetEvaluate(customHighest, 3);

            // Set the S logos for each difficulty
            __instance.m_Diff1Item.ChangeSchemeByEvalue(diff1, __instance.globalData);
            __instance.m_Diff2Item.ChangeSchemeByEvalue(diff2, __instance.globalData);
            __instance.m_Diff3Item.ChangeSchemeByEvalue(diff3, __instance.globalData);
        }

        // For vanilla and custom charts, this fixes the hidden difficulty icon for non-master charts
        if (!specialSongInstance.IsInvokeHideBms(uid)) return;
        if (!specialSongInstance.m_HideBmsInfos.TryGetValue(uid, out var info)) return;

        // Get the difficulty evaluates
        var hiddenEval = isCustom ? GetEvaluate(customHighest, 4) : GetVanillaEvaluate(uid, 4);
        var masterEval = isCustom ? GetEvaluate(customHighest, 3) : GetVanillaEvaluate(uid, 3);

        // The game handles case 3 already, don't need to reinvent the wheel here
        switch (info.triggerDiff)
        {
            case 1:
                __instance.m_Diff3Item.ChangeSchemeByEvalue(masterEval, __instance.globalData);
                __instance.m_Diff1Item.ChangeSchemeByEvalue(hiddenEval, __instance.globalData);
                break;
            case 2:
                __instance.m_Diff3Item.ChangeSchemeByEvalue(masterEval, __instance.globalData);
                __instance.m_Diff2Item.ChangeSchemeByEvalue(hiddenEval, __instance.globalData);
                break;
        }
    }
}