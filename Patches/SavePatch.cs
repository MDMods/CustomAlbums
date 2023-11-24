using CustomAlbums.Managers;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using HarmonyLib;
using CustomAlbums.Utilities;
using System.Text.Json.Nodes;
using Il2CppRewired.UI.ControlMapper;
using Il2CppAccount;
using Il2CppFormulaBase;
using Il2CppAssets.Scripts.PeroTools.Platforms.Steam;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.UI.Controls;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;

namespace CustomAlbums.Patches
{
    internal static class SavePatch
    {
        internal static readonly Logger Logger = new(nameof(SavePatch));

        // A mapping of Evaluate->letter grade 
        internal static readonly string[] EvalToGrade = new string[]
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
        /// Sets the PnlRecord (score, combo, accuracy) to empty.
        /// This is what will normally appear if you have not completed a chart.
        /// </summary>
        /// <param name="panel">The panel instance to set</param>
        private static void SetPanelToEmpty(PnlRecord panel)
        {
            panel.imgIconFc.SetActive(false);
            panel.txtAccuracy.text = "-";
            panel.txtClear.text = "-";
            panel.txtCombo.text = "-";
            panel.txtCombo.color = panel.gradeColor[0];
            panel.txtGrade.text = "-";
            panel.txtGrade.color = panel.gradeColor[0];
            panel.txtScore.text = "-";
            panel.txtScore.color = panel.gradeColor[6];
        }

        /// <summary>
        /// Sets the PnlRecord (score, combo, accuracy) to the custom chart data.
        /// </summary>
        /// <param name="panel">The panel instance to set.</param>
        /// <param name="data">The custom chart data.</param>
        /// <param name="isFullCombo">If the selected chart has been FCed.</param>
        private static void SetPanelWithData(PnlRecord panel, JsonNode data, bool isFullCombo)
        {
            // Enables the FC icon if chart has been FCed, otherwise disable
            // Also sets the combo text to a gold color if it has been FCed, gray otherwise
            if (isFullCombo)
            {
                panel.imgIconFc.SetActive(true);
                panel.txtCombo.color = panel.gradeColor[6];
            }
            else
            {
                panel.imgIconFc.SetActive(false);
                panel.txtCombo.color = panel.gradeColor[0];
            }

            // Sets all of the PnlRecord data to custom chart data.
            var evaluate = data["Evaluate"].GetValue<int>();
            panel.txtAccuracy.text = data["AccuracyStr"].GetValue<string>();
            panel.txtClear.text = data["Clear"].GetValue<float>().ToString();
            panel.txtCombo.text = data["Combo"].GetValue<int>().ToString();
            panel.txtGrade.text = EvalToGrade[evaluate];
            panel.txtGrade.color = panel.gradeColor[evaluate];
            panel.txtScore.text = data["Score"].GetValue<int>().ToString();
        }

        /// <summary>
        /// Grabs the custom chart data and injects the PnlRecord with the 
        /// </summary>
        /// <param name="__instance">The PnlPreparation instance</param>
        /// <param name="forceReload">Whether or not the PnlPreparation should force reload the leaderboards</param>
        /// <returns></returns>
        private static bool InjectPnlPreparation(PnlPreparation __instance, bool forceReload)
        {
            var currentMusicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();

            // If the chart is not custom, run the original method
            if (currentMusicInfo.albumJsonIndex != AlbumManager.UID + 1) return true;

            var currentChartData = SaveManager.SaveData.GetChartSaveDataFromUid(currentMusicInfo.uid);
            var recordPanel = __instance.pnlRecord;

            var highestExists = currentChartData.TryGetPropertyValue("Highest", out var currentChartHighest);
            
            // If no highest data exists
            if (!highestExists || currentChartHighest is null)
            {
                SetPanelToEmpty(recordPanel);
                foreach (var panel in __instance.pnlRanks)
                {
                    panel.Refresh(forceReload);
                }
                return false;
            }

            var difficulty = GlobalDataBase.s_DbMusicTag.selectedDiffTglIndex;

            var fullComboExists = currentChartData.TryGetPropertyValue("FullCombo", out var currentChartFullCombo);

            // If no full combo data exists
            if (!fullComboExists || currentChartFullCombo is null)
            {
                recordPanel.imgIconFc.SetActive(false);
                SetPanelToEmpty(recordPanel);
                
                foreach (var panel in __instance.pnlRanks)
                {
                    panel.Refresh(forceReload);
                }

                return false;
            }

            // LINQ query to see if difficulty is in the full combo list
            bool IsFullCombo = currentChartFullCombo.AsArray().Any(x => (int)x == difficulty);
            currentChartHighest = currentChartHighest[difficulty.ToString()];

            // If the current chart has no data for the selected difficulty
            if (currentChartHighest is null)
            {
                recordPanel.imgIconFc.SetActive(false);
                SetPanelToEmpty(recordPanel);

                foreach (var panel in __instance.pnlRanks)
                {
                    panel.Refresh(forceReload);
                }

                return false;
            }

            // Set the panel with the found data and refresh leaderboards if needed
            SetPanelWithData(recordPanel, currentChartHighest, IsFullCombo);
            foreach (var panel in __instance.pnlRanks)
            {
                panel.Refresh(forceReload);
            }
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
                if (musicInfo.uid.StartsWith($"{AlbumManager.UID}-"))
                {
                    var fileName = $"album_{Path.GetFileNameWithoutExtension(AlbumManager.GetByUid(musicInfo.uid)?.Path) ?? string.Empty}";
                    SaveManager.SaveData.Hides.Add(fileName);
                    // TODO: Attempt to deal with this without touching vanilla save
                    ShowText.ShowInfo(DBConfigTip.GetTip("hideSuccess"));
                    return false;
                }
                return true;
            }
        }

        // TODO: Remove this stuff, just don't want saves to actually save yet :)

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
