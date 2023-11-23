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

namespace CustomAlbums.Patches
{
    internal static class SavePatch
    {
        internal static readonly Logger Logger = new(nameof(SavePatch));

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

        private static void SetPanelWithData(PnlRecord panel, JsonNode data, bool isFullCombo)
        {
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

            var evaluate = data["Evaluate"].GetValue<int>();
            panel.txtAccuracy.text = data["AccuracyStr"].GetValue<string>();
            panel.txtClear.text = data["Clear"].GetValue<float>().ToString();
            panel.txtCombo.text = data["Combo"].GetValue<int>().ToString();
            panel.txtGrade.text = EvalToGrade[evaluate];
            panel.txtGrade.color = panel.gradeColor[evaluate];
            panel.txtScore.text = data["Score"].GetValue<int>().ToString();
        }

        private static bool InjectPnlPreparation(PnlPreparation __instance, bool forceReload)
        {
            var currentMusicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();

            if (currentMusicInfo.albumJsonIndex != AlbumManager.UID + 1) return true;

            var currentChartData = SaveManager.SaveData.GetChartSaveDataFromUid(currentMusicInfo.uid);

            var recordPanel = __instance.pnlRecord;

            var highestExists = currentChartData.TryGetPropertyValue("Highest", out var currentChartHighest);
            
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

            bool IsFullCombo = currentChartFullCombo.AsArray().Any(x => (int)x == difficulty);
            currentChartHighest = currentChartHighest[difficulty.ToString()];
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
            private static bool Prefix(PnlPreparation __instance)
            {
                return InjectPnlPreparation(__instance, true);
            }
        }

        [HarmonyPatch(typeof(PnlPreparation), nameof(PnlPreparation.OnDiffTglChanged))]
        internal class PnlPreparationDiffTogglePatch
        {
            private static bool Prefix(PnlPreparation __instance)
            {
                return InjectPnlPreparation(__instance, false);
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
