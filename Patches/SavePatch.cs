using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAccount;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore.HostComponent;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using Il2CppAssets.Scripts.PeroTools.Platforms.Steam;
using Il2CppAssets.Scripts.Structs;
using Il2CppAssets.Scripts.UI.Panels;
using Il2CppInterop.Common;
using MelonLoader;
using MelonLoader.NativeUtils;
using static CustomAlbums.Managers.SaveManager;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using Environment = System.Environment;
using IntPtr = System.IntPtr;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Patches
{
    internal static class SavePatch
    {
        internal static bool? _hqPresent = null;
        internal static bool HQPresent => _hqPresent ??= MelonBase.FindMelon("Headquarters", "AshtonMemer") is not null;           

        private static string OriginalNoNetText;

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

        private static readonly NativeHook<RecordBattleArgsDelegate> Hook = new();

        //
        // PANEL INJECTION SECTION
        //

        /// <summary>
        ///     Sets the <see cref="PnlRecord"/> (score, combo, accuracy) to the custom chart data.
        /// </summary>
        /// <param name="panel">The <see cref="PnlRecord"/> instance to set.</param>
        /// <param name="data">The custom chart data.</param>
        /// <param name="isFullCombo">If the selected chart has been FCed.</param>
        private static void SetPanelWithData(PnlRecord panel, CustomChartSave data, bool isFullCombo)
        {
            // Enables the FC icon if chart has been FCed
            // Also sets the combo text to a gold color if it has been FCed
            if (isFullCombo)
            {
                panel.imgIconFc.SetActive(true);
                panel.txtCombo.color = panel.gradeColor[6];
            }

            // Sets all the PnlRecord data to custom chart data.
            var evaluate = data.Evaluate;
            panel.txtAccuracy.text = data.AccuracyStr;
            panel.txtClear.text = data.Clear.ToStringInvariant();
            panel.txtCombo.text = data.Combo.ToStringInvariant();
            panel.txtGrade.text = EvalToGrade[evaluate];
            panel.txtGrade.color = panel.gradeColor[evaluate];
            panel.txtScore.text = data.Score.ToStringInvariant();
        }

        /// <summary>
        ///     Clears the current pnlRecord and refreshes the panel if needed.
        /// </summary>  
        /// <param name="panelPreparation">The <see cref="PnlPreparation"/> instance.</param>
        /// <param name="reload">Whether panelPreparation should force reload the leaderboards.</param>
        /// 
        private static void ClearAndRefreshPanels(PnlPreparation panelPreparation, bool reload)
        {
            panelPreparation.pnlRecord.Clear();
            panelPreparation.pnlRecord.imgIconFc.SetActive(false);
            foreach (var panel in panelPreparation.pnlRanks) panel.Refresh(reload);
        }

        /// <summary>
        ///     Gets the correct current difficulty for the selected chart, accounting for hidden activation.
        /// </summary>
        /// <param name="uid">The uid of the chart.</param>
        /// <returns>The current difficulty of the selected chart.</returns>
        private static int GetDifficulty(string uid)
        {
            // Get current index and null-check m_HideBmsInfos
            var selectedIndex = GlobalDataBase.s_DbMusicTag.selectedDiffTglIndex;
            var hideBmsInfos = Singleton<SpecialSongManager>.instance.m_HideBmsInfos;
            if (hideBmsInfos == null || !hideBmsInfos.ContainsKey(uid)) return selectedIndex;

            // Null-check the invoked hidden dictionary and make sure that if it's non-null the hideBms exists and is non-null
            var invokedHides = Singleton<SpecialSongManager>.instance.m_IsInvokeHideDic;
            if (invokedHides == null || !invokedHides.ContainsKey(uid) || !hideBmsInfos.TryGetValue(uid, out var hideBms) || hideBms == null) return selectedIndex;

            return invokedHides[uid] && selectedIndex == hideBms.triggerDiff ? hideBms.m_HideDiff : selectedIndex;
        }

        /// <summary>
        ///     Grabs the custom chart data and injects the PnlRecord with the chart data.
        /// </summary>
        /// <param name="__instance">The <see cref="PnlPreparation"/> instance.</param>
        /// <param name="forceReload">Whether the <see cref="PnlPreparation"/> instance should force reload the leaderboards.</param>
        /// <returns></returns>
        private static bool InjectPnlPreparation(PnlPreparation __instance, bool forceReload)
        {
            var currentMusicInfo = GlobalDataBase.s_DbMusicTag.CurMusicInfo();

            // If the chart is not custom, run the original method; otherwise, run our modified one
            if (currentMusicInfo.albumJsonIndex != AlbumManager.Uid + 1) return true;

            // Reset the panel to its default
            ClearAndRefreshPanels(__instance, forceReload);
            __instance.designerLongNameController?.Refresh();

            if (!ModSettings.SavingEnabled) return false;

            var recordPanel = __instance.pnlRecord;
            var currentChartData = SaveData.GetChartSaveDataFromUid(currentMusicInfo.uid);

            // If no highest data exists then early return
            if ((currentChartData.Highest?.Count ?? 0) == 0)
            {
                Logger.Msg($"No save data found for {currentMusicInfo.uid}, nothing to inject");
                return false;
            }

            var difficulty = GetDifficulty(currentMusicInfo.uid);

            var isFullCombo = currentChartData.FullCombo?.Contains(difficulty) ?? false;
            var currentChartHighest = currentChartData.Highest.GetValueOrDefault(difficulty);

            if (currentChartHighest is null)
            {
                Logger.Msg($"Save data was found for the chart, but not for difficulty {difficulty}");
                return false;
            }

            // Set the panel with the custom score data
            SetPanelWithData(recordPanel, currentChartHighest, isFullCombo);
            Logger.Msg($"Injecting {currentMusicInfo.uid} with difficulty {difficulty}");
            
            return false;
        }

        /// <summary>
        /// Adds proper support for different levelDesigners as well as fixes the levelDesigner sticking issue.
        /// </summary>
        [HarmonyPatch(typeof(MusicInfo), nameof(MusicInfo.GetLevelDesignerStringByIndex))]
        internal class LevelDesignerPatch
        {
            private static bool Prefix(MusicInfo __instance, ref string __result, int index)
            {

                __result = index switch
                {
                    1 => __instance.m_MaskValue.TryGetValue("levelDesigner1", out var d1) ? d1.ToString() : __instance.levelDesigner1,
                    2 => __instance.m_MaskValue.TryGetValue("levelDesigner2", out var d2) ? d2.ToString() : __instance.levelDesigner2,
                    3 => __instance.m_MaskValue.TryGetValue("levelDesigner3", out var d3) ? d3.ToString() : __instance.levelDesigner3,
                    4 => __instance.m_MaskValue.TryGetValue("levelDesigner4", out var d4) ? d4.ToString() : __instance.levelDesigner4,
                    5 => __instance.m_MaskValue.TryGetValue("levelDesigner5", out var d5) ? d5.ToString() : __instance.levelDesigner5,
                    _ => throw new ArgumentOutOfRangeException(nameof(index), index, "The difficulty is not a valid index.")
                };

                if (string.IsNullOrEmpty(__result) || __result == "?") __result = __instance.m_MaskValue.TryGetValue("levelDesigner", out var d) ? d.ToString() : __instance.levelDesigner;
                if (!string.IsNullOrEmpty(__result)) return false;

                __result = "?????";
                return false;

            }
        }

        /// <summary>
        /// Stops the game from loading leaderboards on custom charts if Headquarters is not installed.
        /// </summary>
        [HarmonyPatch(typeof(PnlRank), nameof(PnlRank.UIRefresh))]
        internal class PnlRankPatch
        {
            private static bool FirstRun = true;
            private static bool Prefix(string uid, PnlRank __instance)
            {
                var noNetComp = __instance.noNet.GetComponent<UnityEngine.UI.Text>();
                if (FirstRun)
                {
                    OriginalNoNetText = noNetComp.text;
                    FirstRun = false;
                }
                // Check first run case when on a custom and HQ is not present
                if (uid.StartsWith($"{AlbumManager.Uid}-") && _hqPresent == null && !HQPresent)
                {
                    Logger.Warning("Headquarters is not installed! Custom chart leaderboards will not function.");
                }

                // Vanilla chart or HQ present
                if (!uid.StartsWith($"{AlbumManager.Uid}-") || HQPresent)
                {
                    noNetComp.text = OriginalNoNetText;
                    return true;
                }

                // Custom and HQ not present
                noNetComp.text = "Headquarters mod is not loaded! ~(*´Д｀)";
                __instance.noNet.SetActive(true);
                return false;
            }
        }

        //
        // HACK SECTION
        //

        // TODO: Find a way to inject hidden and favorite charts without using vanilla save -- below are workarounds for quick release.
        private static void CleanCustomData()
        {

            DataHelper.hides.RemoveAll((Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            DataHelper.history.RemoveAll(
                (Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            DataHelper.collections.RemoveAll(
                (Il2CppSystem.Predicate<string>)(uid => uid.StartsWith($"{AlbumManager.Uid}-")));
            DataHelper.highest.RemoveAll(
                (Il2CppSystem.Predicate<IData>)(data => data.GetUid().StartsWith($"{AlbumManager.Uid}-")));

            if (DataHelper.selectedAlbumUid == "music_package_999")
                DataHelper.selectedAlbumUid = "music_package_0";

            if (DataHelper.selectedAlbumTagIndex == AlbumManager.Uid)
                DataHelper.selectedAlbumTagIndex = 0;

            if (!DataHelper.selectedMusicUidFromInfoList.StartsWith($"{AlbumManager.Uid}-")) return;
            
            if (ModSettings.SavingEnabled) SaveData.SelectedAlbum = AlbumManager.GetAlbumNameFromUid(DataHelper.selectedMusicUidFromInfoList);
            DataHelper.selectedMusicUidFromInfoList = "0-0";
        }

        private static void InjectCustomData()
        {
            if (!ModSettings.SavingEnabled) return;

            DataHelper.hides.AddManagedRange(SaveData.Hides.GetAlbumUidsFromNames());
            DataHelper.history.AddManagedRange(SaveData.History.GetAlbumUidsFromNames());
            DataHelper.collections.AddManagedRange(SaveData.Collections.GetAlbumUidsFromNames());

            if (!SaveData.SelectedAlbum.StartsWith("album_")) return;
            DataHelper.selectedAlbumUid = "music_package_999";
            DataHelper.selectedAlbumTagIndex = 999;
            DataHelper.selectedMusicUidFromInfoList = AlbumManager.LoadedAlbums.TryGetValue(SaveData.SelectedAlbum, out var album) ? album.Uid : "0-0";
        }

        [HarmonyPatch(typeof(DataHelper), nameof(DataHelper.CheckMusicUnlockMaster), new Type[] { typeof(MusicInfo), typeof(bool) })]
        internal class CheckUnlockMasterPatch
        {
            private static bool Prefix(MusicInfo musicInfo, ref bool __result)
            {
                SaveData.Ability = Math.Max(SaveData.Ability, GlobalDataBase.dbUi.ability);
                var ability = GameAccountSystem.instance.IsLoggedIn() ? SaveData.Ability : 0;
                var uid = musicInfo?.uid;

                // If musicInfo or uid is null, run original
                if (uid is null) return true;

                // Bugged vanilla state, do manual logic
                if (GlobalDataBase.dbUi.ability == 0 && ability != 0)
                {
                    Logger.Msg("Fixing bugged vanilla state for master lock");
                    var vanillaParse = Formatting.TryParseAsInt(musicInfo.difficulty3, out var difficulty);
                    var cond = !vanillaParse || ability >= difficulty;
                    __result = cond || DataHelper.unlockMasters.Contains(uid) || SaveData.UnlockedMasters.Contains(AlbumManager.GetAlbumNameFromUid(uid)) || (!AlbumManager.GetByUid(musicInfo.uid)?.IsPackaged ?? false);
                    return false;
                }

                // Non-bugged vanilla case
                if (!uid.StartsWith($"{AlbumManager.Uid}-")) return true;

                // Non-bugged custom case
                var successParse = Formatting.TryParseAsInt(musicInfo.difficulty3, out var diffNum);
                var abilityConditionOrGimmick = !successParse || ability >= diffNum;

                __result = abilityConditionOrGimmick || SaveData.UnlockedMasters.Contains(AlbumManager.GetAlbumNameFromUid(uid)) || !AlbumManager.GetByUid(musicInfo.uid).IsPackaged;
                return false;
            }
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
        ///     Stops the game from saving custom chart score data.
        /// </summary>
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static IntPtr RecordBattleArgsPatch(IntPtr instance, IntPtr args, IntPtr isSuccess, IntPtr nativeMethodInfo)
        {
            return !GlobalDataBase.s_DbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}-") ? Hook.Trampoline(instance, args, isSuccess, nativeMethodInfo) : IntPtr.Zero;
        }

        /// <summary>
        ///     Gets <c>RecordBattleArgs</c> and detours it using a
        ///     <c>NativeHook&lt;RecordBattleArgsDelegate&gt;</c> to <c>RecordBattleArgsPatch</c>.
        /// </summary>
        internal static unsafe void AttachHook()
        {
            // If you are interested in what's happening here, check out AssetPatch
            var recordBattleArgsMethod = AccessTools.Method(typeof(AchievementManager),
                nameof(AchievementManager.RecordBattleArgs),
                new[] { typeof(GlobalAchievementArgs).MakeByRefType(), typeof(bool) });

            if (recordBattleArgsMethod is null)
            {
                Logger.Error("FATAL ERROR: SavePatch failed.");
                Thread.Sleep(1000);
                Environment.Exit(1);
            }

            var recordBattleArgsPointer = *(IntPtr*)(IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(recordBattleArgsMethod).GetValue(null)!;

            delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> detourPointer = &RecordBattleArgsPatch;

            Hook.Detour = (IntPtr)detourPointer;
            Hook.Target = recordBattleArgsPointer;
            Hook.Attach();
        }

        /// <summary>
        ///     Gets the score data of the custom chart and sends it to SaveManager for processing.
        /// </summary>
        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.UploadScore))]
        internal class UploadScorePatch
        {
            private static void Postfix(SceneUploadResultData resData)
            {
                if (!resData.musicUid.StartsWith($"{AlbumManager.Uid}-")) return;
                SaveScore(resData.musicUid, resData.musicDifficulty, resData.score, resData.accuracy, resData.combo, resData.evaluate, resData.miss);
            }
        }

        /// <summary>
        ///     Enables the PnlVictory screen logic when the chart is custom.
        /// </summary>
        [HarmonyPatch(typeof(PnlVictory), nameof(PnlVictory.SetScore))]
        internal class SetScorePatch
        {
            private static void Postfix(PnlVictory __instance)
            {
                // Fix for DJMax scrolling text bug
                if (__instance.m_CurControls.mainPnl.transform.parent.name is "Djmax")
                {
                    var titleMask = __instance.m_CurControls.mainPnl.transform
                        .Find("PnlVictory_3D").Find("SongTittle").Find("ImgSongTittleMask");

                    var titleText = titleMask.Find("TxtSongTittle").gameObject;
                    if (!titleText.active) titleMask.Find("MaskPos").gameObject.SetActive(true);
                }

                if (!ModSettings.SavingEnabled || !GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}-")) return;

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

        [HarmonyPatch(typeof(PnlPreparation), nameof(PnlPreparation.OnBattleStart))]
        internal class OnBattleStartPatch
        {
            private static void Postfix()
            {
                if (ModSettings.SavingEnabled && !DataHelper.selectedMusicUidFromInfoList.StartsWith("album_"))
                    SaveData.SelectedAlbum = DataHelper.selectedMusicUidFromInfoList;
            }
        }

        //
        // HACKS SECTION - TODO: Remove all the methods below once we don't put data into the official game at all
        //

        [HarmonyPatch(typeof(SteamSync), nameof(SteamSync.SaveLocal))]
        internal class SaveLocalPatch
        {
            private static bool BackedUp { get; set; }

            private static void Prefix()
            {

                if (ModSettings.SavingEnabled && DataHelper.selectedMusicUidFromInfoList.StartsWith($"{AlbumManager.Uid}-"))
                    SaveData.SelectedAlbum =
                        $"{AlbumManager.GetAlbumNameFromUid(DataHelper.selectedMusicUidFromInfoList)}";

                if (ModSettings.SavingEnabled) SaveSaveFile();
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
                CleanCustomData();
                if (ModSettings.SavingEnabled) InjectCustomData();
            }
        }

        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.RefreshDatas))]
        internal class RefreshDatasPatch
        {
            private static void Prefix()
            {
                Backup.InitBackups();
                SaveSaveFile();
                CleanCustomData();
                if (ModSettings.SavingEnabled) InjectCustomData();
            }
        }

        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.OnSaveSelectCallback))]
        internal class OnSaveSelectPatch
        {
            private static void Prefix(ref bool isLocal)
            {
                if (!isLocal) return;

                if (ModSettings.SavingEnabled) SaveSaveFile();
                CleanCustomData();
            }

            private static void Postfix(ref bool isLocal)
            {
                if (!ModSettings.SavingEnabled || !isLocal) return;
                InjectCustomData();
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr RecordBattleArgsDelegate(IntPtr instance, IntPtr args, IntPtr isSuccess, IntPtr nativeMethodInfo);
    }
}