using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore;
using Il2CppAssets.Scripts.Structs;

namespace CustomAlbums.Patches
{
    internal class ChartDialogPatch
    {
        /// <summary>
        ///     This method runs once when the chart loads, so init basic values to avoid computing again
        /// </summary>
        [HarmonyPatch(typeof(DialogMasterControl), nameof(DialogMasterControl.Init))]
        internal class InitPatch
        {
            private static readonly Logger Logger = new(nameof(InitPatch));

            private static void Prefix(DialogMasterControl __instance)
            {
                Logger.Msg("Setting talk file values.");
                var currentStageUid = GlobalDataBase.dbBattleStage.musicUid;
                PlayDialogAnimPatch.CurrentStageInfo = GlobalDataBase.dbStageInfo.m_StageInfo;

                if (!currentStageUid.StartsWith($"{AlbumManager.Uid}-"))
                {
                    PlayDialogAnimPatch.HasVersion2 = false;
                    return;
                }

                var currentAlbum = AlbumManager.GetByUid(currentStageUid);

                // Reset values that may have changed
                PlayDialogAnimPatch.HasVersion2 =
                    (currentAlbum?.Sheets.TryGetValue(PlayDialogAnimPatch.CurrentStageInfo.difficulty, out var sheet) ??
                     false) && sheet.TalkFileVersion2;
                PlayDialogAnimPatch.Index = 0;
                PlayDialogAnimPatch.CurrentLanguage = DataHelper.userLanguage;

                if (PlayDialogAnimPatch.CurrentStageInfo.dialogEvents == null) return;
                // Set this here to avoid repeatedly checking a dictionary
                PlayDialogAnimPatch.DialogEvents =
                    PlayDialogAnimPatch.CurrentStageInfo.dialogEvents.ContainsKey(PlayDialogAnimPatch.CurrentLanguage)
                        ? PlayDialogAnimPatch.CurrentStageInfo.dialogEvents[PlayDialogAnimPatch.CurrentLanguage]
                        : PlayDialogAnimPatch.CurrentStageInfo.dialogEvents["English"];
            }
        }


        /// <summary>
        ///     This patch allows support for setting the transparency of dialog boxes. Eventually, this patch will allow stronger
        ///     control of dialog boxes using the "version": 2 property.
        /// </summary>
        [HarmonyPatch(typeof(DialogSubControl), nameof(DialogSubControl.PlayDialogAnim))]
        internal class PlayDialogAnimPatch
        {
            private static readonly Logger Logger = new(nameof(PlayDialogAnimPatch));
            internal static int Index { get; set; }
            internal static StageInfo CurrentStageInfo { get; set; }
            internal static bool HasVersion2 { get; set; }
            internal static string CurrentLanguage { get; set; } = string.Empty;
            internal static Il2CppSystem.Collections.Generic.List<GameDialogArgs> DialogEvents { get; set; }

            private static void Prefix(DialogSubControl __instance)
            {
                if (!HasVersion2 || DialogEvents == null) return;

                __instance.m_BgImg.color = DialogEvents[Index++].bgColor;
            }
        }
    }
}