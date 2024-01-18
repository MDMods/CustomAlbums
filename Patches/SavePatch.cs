using CustomAlbums.Managers;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using HarmonyLib;
using CustomAlbums.Utilities;
using System.Text.Json.Nodes;
using Il2CppAccount;
using System.Globalization;
using Il2CppAssets.Scripts.GameCore.HostComponent;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using Il2CppAssets.Scripts.PeroTools.Platforms.Steam;
using Logger = CustomAlbums.Utilities.Logger;
using Il2CppPeroPeroGames.DataStatistics;
using System.Reflection;
using static CustomAlbums.Managers.SaveManager;

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
            "B",
            "A",
            "S",
            "S",
            "S"
        };

        //
        // PANEL INJECTION SECTION
        //

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
            Singleton<SpecialSongManager>.instance.m_HideBmsInfos.TryGetValuePossibleNullKey(musicInfo.uid, out var hideBms);
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
            if (currentMusicInfo.albumJsonIndex != AlbumManager.Uid + 1) return true;
            if (!ModSettings.SavingEnabled) return false;

            // Reset the panel to its default
            ClearAndRefreshPanels(__instance, forceReload);

            var recordPanel = __instance.pnlRecord;
            var currentChartData = SaveData.GetChartSaveDataFromUid(currentMusicInfo.uid);
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

        //
        // ACCOUNT SECTION
        //

        // TODO: Figure all of this out without using vanilla save
        [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.AddHide))]
        internal class AddHidePatch
        {
            private static bool Prefix(MusicInfo musicInfo)
            {
                if (!musicInfo.uid.StartsWith($"{AlbumManager.Uid}-")) return true;
                if (!ModSettings.SavingEnabled) return false;
                
                SaveData.Hides.Add(AlbumManager.GetAlbumNameFromUid(musicInfo.uid));
                return true;
            }
        }

        [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.RemoveHide))]
        internal class RemoveHidePatch
        {
            private static bool Prefix(MusicInfo musicInfo)
            {
                if (!musicInfo.uid.StartsWith($"{AlbumManager.Uid}-")) return true;
                if (!ModSettings.SavingEnabled) return false;

                SaveData.Hides.Remove(AlbumManager.GetAlbumNameFromUid(musicInfo.uid));
                return true;
            }
        }

        [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.AddCollection))]
        internal class AddCollectionPatch
        {
            private static bool Prefix(MusicInfo musicInfo)
            {
                if (!musicInfo.uid.StartsWith($"{AlbumManager.Uid}-")) return true;
                if (!ModSettings.SavingEnabled) return false;

                SaveData.Collections.Add(AlbumManager.GetAlbumNameFromUid(musicInfo.uid));
                return true;
            }
        }

        [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.RemoveCollection))]
        internal class RemoveCollectionPatch
        {
            private static bool Prefix(MusicInfo musicInfo)
            {
                if (!musicInfo.uid.StartsWith($"{AlbumManager.Uid}-")) return true;
                if (!ModSettings.SavingEnabled) return false;

                SaveData.Collections.Remove(AlbumManager.GetAlbumNameFromUid(musicInfo.uid));
                return true;
            }
        }

        [HarmonyPatch(typeof(DBMusicTag), nameof(DBMusicTag.AddHistory))]
        internal class AddHistoryPatch
        {
            private static bool Prefix(string musicUid)
            {
                if (!musicUid.StartsWith($"{AlbumManager.Uid}-")) return true;
                if (!ModSettings.SavingEnabled) return false;

                if (SaveData.History.Count == 10)
                    SaveData.History.Dequeue();

                SaveData.History.Enqueue(AlbumManager.GetAlbumNameFromUid(musicUid));
                return true;
            }
        }

        //
        // SCORE SECTION
        //

        /// <summary>
        /// Gets the score data of the custom chart and sends it to SaveManager for processing.
        /// </summary>
        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.UploadScore))]
        internal class UploadScorePatch
        {
            private static void Postfix(string musicUid, int musicDifficulty, string characterUid, string elfinUid, int hp, int score, float acc, int maximumCombo, string evaluate, int miss)
            {
                if (!musicUid.StartsWith($"{AlbumManager.Uid}-")) return;
                SaveScore(musicUid, musicDifficulty, score, acc, maximumCombo, evaluate, miss);
            }
        }

        /// <summary>
        /// Stops the game from sending analytics of custom charts.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataBattleHelper))]
        internal class SendMDPatch
        {
            private static MethodInfo[] TargetMethods()
            {
                return typeof(ThinkingDataBattleHelper).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => method.Name.StartsWith("Send")).ToArray();
            }
            private static bool Prefix()
            {
                return !BattleHelper.MusicInfo().uid.StartsWith($"{AlbumManager.Uid}-");
            }
        }

        /// <summary>
        /// Stops the game from saving custom chart score data.
        /// </summary>
        
        // TODO: Make sure this actually stops the game from saving high score data to vanilla save
        [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.RecordBattleArgs))]
        internal class StopSavingPatch
        {
            private static bool Prefix()
            {
                if (GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}-")) Logger.Msg($"Custom chart {GlobalDataBase.dbBattleStage}, not saving");
                return !GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}-");
            }
        }

        /// <summary>
        /// Enables the PnlVictory screen logic when the chart is custom.
        /// </summary>
        [HarmonyPatch(typeof(PnlVictory), nameof(PnlVictory.SetScore))]
        internal class SetScorePatch
        {
            private static void Postfix(PnlVictory __instance)
            {
                if (!ModSettings.SavingEnabled) return;

                var albumName = AlbumManager.GetAlbumNameFromUid(GlobalDataBase.dbBattleStage.musicUid);
                if (!SaveData.Highest.TryGetValue(albumName, out var highest))
                    return;

                // If the chart has been played then enable the "HI-SCORE" UI element
                var difficulty = GlobalDataBase.s_DbBattleStage.m_MapDifficulty;
                __instance.m_CurControls.highScoreTitle.enabled = PreviousScore != "-";
                __instance.m_CurControls.highScoreTxt.enabled = PreviousScore != "-";

                // Gets the current run score
                var score = Singleton<TaskStageTarget>.instance.GetScore();

                // Enable "New Best!!" UI element if the chart hasn't been played, or the new play is a higher score
                if (!highest.ContainsKey(difficulty))
                    __instance.m_CurControls.newBest.SetActive(true);
                if (PreviousScore != "-")
                    __instance.m_CurControls.newBest.SetActive(score > int.Parse(PreviousScore));

                __instance.m_CurControls.highScoreTxt.text = PreviousScore;
            }
        }

        //
        // HACK SECTION
        //

        // TODO: Find a way to inject hidden and favorite charts without using vanilla save -- below are workarounds for quick release.
        private static void CleanCustomData()
        {
            if (!ModSettings.SavingEnabled) return;

            DataHelper.hides.RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            DataHelper.history.RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            DataHelper.collections.RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            DataHelper.tasks.RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            
            if (DataHelper.selectedAlbumUid == "music_package_999") 
                DataHelper.selectedAlbumUid = "music_package_0";
            
            if (DataHelper.selectedAlbumTagIndex == AlbumManager.Uid) 
                DataHelper.selectedAlbumTagIndex = 0;

            if (DataHelper.selectedMusicUidFromInfoList.StartsWith($"{AlbumManager.Uid}-"))
            {
                DataHelper.selectedMusicUidFromInfoList = "0-0";
                SaveData.SelectedAlbum = AlbumManager.GetAlbumNameFromUid(DataHelper.selectedMusicUidFromInfoList);
            }
            else
            {
                SaveData.SelectedAlbum = string.Empty;
            }

            foreach (var task in DataHelper.tasks)
            {
                Singleton<DataManager>.instance["Task"][task].GetResult<Il2CppSystem.Collections.Generic.List<IData>>().RemoveAll((Il2CppSystem.Predicate<IData>)(data => data["uid"].GetResult<string>().StartsWith($"{AlbumManager.Uid}")));
            }
        }

        private static void InjectCustomData()
        {
            if (!ModSettings.SavingEnabled) return;

            DataHelper.hides.AddManagedRange(SaveData.Hides.GetAlbumUidsFromNames());
            DataHelper.history.AddManagedRange(SaveData.History.GetAlbumUidsFromNames());
            DataHelper.collections.AddManagedRange(SaveData.Collections.GetAlbumUidsFromNames());

            if (!SaveData.SelectedAlbum.StartsWith($"{AlbumManager.Uid}-")) return;
            
            DataHelper.selectedAlbumUid = "music_package_999";
            DataHelper.selectedAlbumTagIndex = 999;
            DataHelper.selectedMusicUidFromInfoList = SaveData.SelectedAlbum ?? "0-0";
        }


        [HarmonyPatch(typeof(SteamSync), nameof(SteamSync.SaveLocal))]
        internal class SaveLocalPatch
        {
            private static bool BackedUp { get; set; }
            private static void Prefix()
            {
                if (!ModSettings.SavingEnabled) return;

                if (DataHelper.selectedMusicUidFromInfoList.StartsWith($"{AlbumManager.Uid}-"))
                    SaveData.SelectedAlbum = $"{AlbumManager.GetAlbumNameFromUid(DataHelper.selectedMusicUidFromInfoList)}";

                SaveSaveFile();
                CleanCustomData();
            }

            private static void Postfix()
            {
                if (!ModSettings.SavingEnabled) return;
                if (!BackedUp) Backup.InitBackups();
                BackedUp = true;
                InjectCustomData();
            }
        }

        [HarmonyPatch(typeof(SteamSync), nameof(SteamSync.LoadLocal))]
        internal class LoadLocalPatch
        {
            private static void Postfix()
            {
                if (!ModSettings.SavingEnabled) return;
                InjectCustomData();
            }
        }

        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.RefreshDatas))]
        internal class RefreshDatasPatch
        {
            private static void Prefix()
            {
                if (!ModSettings.SavingEnabled) return;
                Backup.InitBackups();
                SaveSaveFile();
                CleanCustomData();
                InjectCustomData();
            }
        }

        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.OnSaveSelectCallback))]
        internal class OnSaveSelectPatch
        {
            private static void Prefix(ref bool isLocal)
            {
                if (!ModSettings.SavingEnabled || !isLocal) return;
                
                SaveSaveFile();
                CleanCustomData();
            }

            private static void Postfix(ref bool isLocal)
            {
                if (!ModSettings.SavingEnabled || !isLocal) return;
                InjectCustomData();
            }
        }
    }
}
