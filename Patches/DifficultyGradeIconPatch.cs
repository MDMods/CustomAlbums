using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.UI.Panels;

namespace CustomAlbums.Patches
{

    [HarmonyPatch(typeof(PnlStage), nameof(PnlStage.RefreshDiffUI))]
    internal class DifficultyGradeIconPatch
    { 
        private static int GetEvaluate(Dictionary<int, Data.ChartSave> highest, int diff) => highest.GetValueOrDefault(diff)?.Evaluate ?? -1;

        /// <summary>
        ///     Enables the S logos on the difficulties for custom charts.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static void Postfix(MusicInfo musicInfo, PnlStage __instance)
        {
            var uid = musicInfo?.uid;
            var specialSongInstance = Singleton<SpecialSongManager>.instance;

            if (string.IsNullOrEmpty(uid)) return;

            // For custom charts, we need to set the S logos for each difficulty
            if (uid.StartsWith($"{AlbumManager.Uid}-"))
            {
                // Gets the highest data from save data
                var customHighest = SaveManager.SaveData.GetChartSaveDataFromUid(uid).Highest;

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
            var hiddenEval = DataHelper.highest.GetIDataByUid(uid, 4).ToChartSave().Evaluate;
            var masterEval = DataHelper.highest.GetIDataByUid(uid, 3).ToChartSave().Evaluate;

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
}