using System.Globalization;
using System.Reflection;
using CustomAlbums.Managers;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.UI.Panels;

namespace CustomAlbums.Patches
{
    [HarmonyPatch(typeof(PnlReportCard), nameof(PnlReportCard.RefreshBestRecord))]
    internal static class ReportCardPatch
    {
        // ReSharper disable once InconsistentNaming
        private static bool Prefix(PnlReportCard __instance)
        {
            var musicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();
            if (musicInfo.albumIndex != 999) return true;

            var mapDifficulty = GlobalDataBase.s_DbBattleStage.m_MapDifficulty;
            var curMusicBestRankOrder = Singleton<TempRankDataManager>.instance.GetCurMusicBestRankOrder();

            var album = AlbumManager.GetByUid(musicInfo.uid);
            if (album == null) return false;

            var save = SaveManager.SaveData.Highest[album.AlbumName][mapDifficulty];
            if (save == null) return false;

            __instance.RefreshRecord(musicInfo, mapDifficulty, save.Score, save.Combo, save.AccuracyStr, save.Evaluate, save.Clear.ToString(CultureInfo.InvariantCulture), curMusicBestRankOrder);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class TogglePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return new[] { nameof(PnlPreparation.OnDiffTglChanged), nameof(PnlPreparation.OnEnable) }
                .Select(methodName => typeof(PnlPreparation).GetMethod(methodName))
                .ToArray();
        }
        // ReSharper disable once InconsistentNaming
        private static void Postfix(PnlPreparation __instance)
        {
            var musicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();
            if (musicInfo.albumIndex != 999) return;

            var mapDifficulty = GlobalDataBase.s_DbBattleStage.m_MapDifficulty;
            var gameObject = __instance.btnDownloadReport.gameObject;

            var album = AlbumManager.GetByUid(musicInfo.uid);
            if (album == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!SaveManager.SaveData.Highest.TryGetValue(album.AlbumName, out var chart))
            {
                gameObject.SetActive(false);
                return;
            }

            if (!chart.ContainsKey(mapDifficulty))
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
        }
    }
}

