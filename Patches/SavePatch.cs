using CustomAlbums.Managers;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using HarmonyLib;
using CustomAlbums.Utilities;
using System.Text.Json.Nodes;
using Il2CppAccount;
using Il2CppAssets.Scripts.PeroTools.Platforms.Steam;
using Il2CppAssets.Scripts.UI.Controls;
using System.Globalization;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Logger = CustomAlbums.Utilities.Logger;
using Il2CppPeroPeroGames.DataStatistics;

namespace CustomAlbums.Patches
{
    internal static class SavePatch
    {
        internal static readonly Logger Logger = new(nameof(SavePatch));

        // A mapping of Evaluate->letter grade
        internal static readonly string[] EvalToGrade =
        {
            "D",
            "C",
            "A",
            "B",
            "S",
            "S",
            "S"
        };

        /// <summary>
        /// Sets the PnlRecord (score, combo, accuracy) to the custom chart data.
        /// </summary>
        /// <param name="panel">The PnlRecord instance to set.</param>
        /// <param name="data">The custom chart data.</param>
        /// <param name="isFullCombo">If the selected chart has been FCed.</param>
        private static void SetPanelWithData(PnlRecord panel, JsonNode data, bool isFullCombo)
        {
            // Enables the FC icon if chart has been FCed
            // Also sets the combo text to a gold color if it has been FCed
            if (isFullCombo)
            {
                panel.imgIconFc.SetActive(true);
                panel.txtCombo.color = panel.gradeColor[6];
            }

            // Sets all the PnlRecord data to custom chart data.
            var evaluate = data["Evaluate"]!.GetValue<int>();
            panel.txtAccuracy.text = data["AccuracyStr"]!.GetValue<string>();
            panel.txtClear.text = data["Clear"]!.GetValue<float>().ToString(CultureInfo.InvariantCulture);
            panel.txtCombo.text = data["Combo"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture);
            panel.txtGrade.text = EvalToGrade[evaluate];
            panel.txtGrade.color = panel.gradeColor[evaluate];
            panel.txtScore.text = data["Score"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Clears the current pnlRecord and refreshes the panel if needed.
        /// <param name="panelPreparation">The PnlPreparation instance.</param>
        /// <param name="reload">Whether panelPreparation should force reload the leaderboards.</param>
        /// </summary>
        private static void ClearAndRefreshPanels(PnlPreparation panelPreparation, bool reload)
        {
            panelPreparation.pnlRecord.Clear();
            foreach (var panel in panelPreparation.pnlRanks)
            {
                panel.Refresh(reload);
            }
        }
        /// <summary>
        /// Gets the correct current difficulty for the selected chart, accounting for hidden activation.
        /// </summary>
        /// <param name="musicInfo">The musicInfo of the chart.</param>
        /// <returns>The current difficulty of the selected chart.</returns>
        private static int GetDifficulty(MusicInfo musicInfo)
        {
            Singleton<SpecialSongManager>.instance.m_HideBmsInfos.TryGetValue(musicInfo.uid, out var hideBms);
            var hiddenDict = Singleton<SpecialSongManager>.instance.m_IsInvokeHideDic;
            var selectedIndex = GlobalDataBase.s_DbMusicTag.selectedDiffTglIndex;
            if (hiddenDict.TryGetValuePossibleNullKey(hideBms?.uid, out var currentlyHidden) && currentlyHidden && selectedIndex == hideBms!.triggerDiff)
            {
                return hideBms!.m_HideDiff;
            }
            return selectedIndex;
        }

        /// <summary>
        /// Grabs the custom chart data and injects the PnlRecord with the chart data.
        /// </summary>
        /// <param name="__instance">The PnlPreparation instance.</param>
        /// <param name="forceReload">Whether the PnlPreparation instance should force reload the leaderboards.</param>
        /// <returns></returns>
        private static bool InjectPnlPreparation(PnlPreparation __instance, bool forceReload)
        {
            var currentMusicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();

            // If the chart is not custom, run the original method, otherwise continue and don't run the original method
            if (currentMusicInfo.albumJsonIndex != AlbumManager.UID + 1) return true;

            // Reset the panel to its default
            ClearAndRefreshPanels(__instance, forceReload);

            var recordPanel = __instance.pnlRecord;
            var currentChartData = SaveManager.SaveData.GetChartSaveDataFromUid(currentMusicInfo.uid);
            var highestExists = currentChartData.TryGetPropertyValue("Highest", out var currentChartHighest);

            // If no highest data exists then early return
            if (!highestExists || currentChartHighest is null)
            {
                Logger.Msg($"No save data found for {currentMusicInfo.uid}, nothing to inject");
                return false;
            }

            var difficulty = GetDifficulty(currentMusicInfo);

            currentChartData.TryGetPropertyValue("FullCombo", out var currentChartFullCombo);

            // LINQ query to see if difficulty is in the full combo list
            // If currentChartFullCombo is null then there is no full combo so isFullCombo is false
            var isFullCombo = currentChartFullCombo?.AsArray().Any(x => (int)x == difficulty) ?? false;

            // Get the highest score for the difficulty that is selected
            currentChartHighest = currentChartHighest[difficulty.ToString()];

            // If the current chart has no data for the selected difficulty then early return
            if (currentChartHighest is null)
            {
                Logger.Msg($"Save data was found for the chart, but not for difficulty {difficulty}");
                return false;
            }

            // Set the panel with the custom score data
            SetPanelWithData(recordPanel, currentChartHighest, isFullCombo);
            Logger.Msg($"Injecting {currentMusicInfo.uid} with difficulty {difficulty}"); ;
            return false;
        }

        [HarmonyPatch(typeof(PnlPreparation), nameof(PnlPreparation.OnEnable))]
        internal class PnlPreparationEnablePatch
        {
            // Inject and force load all leaderboards
            private static bool Prefix(PnlPreparation __instance)
            {
                return InjectPnlPreparation(__instance, true);
            }
        }

        [HarmonyPatch(typeof(PnlPreparation), nameof(PnlPreparation.OnDiffTglChanged))]
        internal class PnlPreparationDiffTogglePatch
        {
            // Inject and load all leaderboards if needed
            private static bool Prefix(PnlPreparation __instance)
            {
                return InjectPnlPreparation(__instance, false);
            }
        }

        [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.AddHide))]
        internal class AddHidePatch
        {
            private static bool Prefix(DBMusicTag __instance, MusicInfo musicInfo)
            {
                if (!musicInfo.uid.StartsWith($"{AlbumManager.UID}-")) return true;

                var fileName = $"album_{Path.GetFileNameWithoutExtension(AlbumManager.GetByUid(musicInfo.uid)?.Path) ?? string.Empty}";
                SaveManager.SaveData.Hides.Add(fileName);
                ShowText.ShowInfo(DBConfigTip.GetTip("hideSuccess"));
                return false;
            }
        }

        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.PrepareUploadScore))]
        internal class UploadScorePatch
        {
            private static void Postfix(string musicUid, int musicDifficulty, string characterUid, string elfinUid, int hp, int score, float acc, int maximumCombo, string evaluate, int miss)
            {
                if (!musicUid.StartsWith($"{AlbumManager.UID}-")) return;
                SaveManager.SaveScore(musicUid, score, acc, maximumCombo, evaluate, miss);
            }
        }

        [HarmonyPatch(typeof(ThinkingDataBattleHelper), nameof(ThinkingDataBattleHelper.SendMDSuccessfulEvent))]
        internal class SendMDPatch
        {
            private static bool Prefix()
            {
                if (!BattleHelper.MusicInfo().uid.StartsWith($"{AlbumManager.UID}-")) return true;

                Logger.Msg("Not sending score!");
                return false;
            }
        }

        // TODO: Remove these, these are safeguards to stop the game from saving :)
        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.OnSaveSelectCallback))]
        internal class GASSavePatch
        {
            private static bool Prefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(SteamSync), nameof(SteamSync.SaveLocal))]
        internal class SteamSyncSavePatch
        {
            private static bool Prefix()
            {
                return false;
            }
        }
    }
}
