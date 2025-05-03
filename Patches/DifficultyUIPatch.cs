using HarmonyLib;
using Il2CppAssets.Scripts.UI.Panels;
using CustomAlbums.Utilities;
using Il2CppAssets.Scripts.Database;
using CustomAlbums.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2Cpp;

namespace CustomAlbums.Patches
{
    internal class DifficultyUIPatch 
    {

        /// <summary>
        ///     Enables the S logos on the difficulties for custom charts.
        /// </summary>
        [HarmonyPatch(typeof(PnlStage), nameof(PnlStage.RefreshDiffUI))]
        internal class DiffUIFix
        {
            private static int GetEvaluate(Dictionary<int, Data.CustomChartSave> highest, int diff) => highest.GetValueOrDefault(diff)?.Evaluate ?? -1;
            
            private static void Postfix(MusicInfo musicInfo, PnlStage __instance) 
            {

                // Return if chart is not custom
                var uid = musicInfo?.uid;
                if (uid is null || !uid.StartsWith($"{AlbumManager.Uid}-")) return;

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
        }
    }
}
