using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore;

namespace CustomAlbums.Patches
{
    internal class ChartDialogPatch
    {
        [HarmonyPatch(typeof(DialogMasterControl), nameof(DialogMasterControl.Init))]
        internal class InitPatch
        {
            private static readonly Logger Logger = new(nameof(InitPatch));
            private static void Prefix(DialogMasterControl __instance)
            {
                Logger.Msg("Setting talk file values.");
                var currentStageUid = GlobalDataBase.dbBattleStage.musicUid;
                PlayDialogAnimPatch.CurrentStageInfo = GlobalDataBase.dbStageInfo.m_StageInfo;
                if (currentStageUid.StartsWith($"{AlbumManager.UID}-"))
                {
                    var currentAlbum = AlbumManager.GetByUid(currentStageUid);
                    if (currentAlbum.Sheets.TryGetValue(PlayDialogAnimPatch.CurrentStageInfo.difficulty - 1, out var sheet) && sheet.TalkFileVersion2)
                    {
                        PlayDialogAnimPatch.HasVersion2 = true;
                    }
                    else
                    {
                        PlayDialogAnimPatch.HasVersion2 = false;
                    }
                    PlayDialogAnimPatch.IsCustom = true;
                }
                else
                {
                    PlayDialogAnimPatch.IsCustom = false;
                    PlayDialogAnimPatch.HasVersion2 = false;
                }
                PlayDialogAnimPatch.Index = 0;
                PlayDialogAnimPatch.CurrentLanguage = DataHelper.userLanguage;
            }
        }

        [HarmonyPatch(typeof(DialogSubControl), nameof(DialogSubControl.PlayDialogAnim))]
        internal class PlayDialogAnimPatch
        {
            private static readonly Logger Logger = new(nameof(PlayDialogAnimPatch));
            internal static int Index { get; set; }
            internal static StageInfo CurrentStageInfo { get; set; }
            internal static bool IsCustom { get; set; }
            internal static bool HasVersion2 { get; set; }
            internal static string CurrentLanguage { get; set; } = string.Empty;
            private static void Prefix(DialogSubControl __instance)
            {
                if (!IsCustom || !HasVersion2) return;
                
                if (CurrentStageInfo.dialogEvents.ContainsKey(CurrentLanguage))
                {
                    var dialogEvents = CurrentStageInfo.dialogEvents[CurrentLanguage];
                    __instance.m_BgImg.color = dialogEvents[Index++].bgColor;
                } 
                else
                {
                    var dialogEvents = CurrentStageInfo.dialogEvents["English"];
                    __instance.m_BgImg.color = dialogEvents[Index++].bgColor;
                }
            }
        } 
    }
}
