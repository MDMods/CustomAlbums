using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.UI.Panels;

namespace CustomAlbums.Patches
{
    internal class DifficultyGradeIconPatch
    {
        /// <summary>
        ///     Fixes vanilla bug where the difficulty grade icon on hidden charts does not update properly when triggerDiff is not 3.
        /// </summary>
        [HarmonyPatch(typeof(PnlStage), nameof(PnlStage.RefreshDiffUI))]
        internal class DiffUIFix
        {
            private static void Postfix(MusicInfo musicInfo, PnlStage __instance)
            {
                var uid = musicInfo?.uid;
                var specialSongInstance = Singleton<SpecialSongManager>.instance;
                
                if (string.IsNullOrEmpty(uid) || !specialSongInstance.IsInvokeHideBms(uid)) return;
                if (!specialSongInstance.m_HideBmsInfos.TryGetValue(uid, out var info)) return;

                // Get the hidden difficulty evaluate
                var hiddenEval = DataHelper.highest.GetIDataByUid(uid, 4).ToChartSave().Evaluate;

                // The game handles case 3 already, don't need to reinvent the wheel here
                switch (info.triggerDiff)
                {
                    case 1:
                        __instance.m_Diff1Item.ChangeSchemeByEvalue(hiddenEval, __instance.globalData);
                        break;
                    case 2:
                        __instance.m_Diff2Item.ChangeSchemeByEvalue(hiddenEval, __instance.globalData);
                        break;
                }
            }
        }
    }
}
