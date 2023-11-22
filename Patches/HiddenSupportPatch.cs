using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.UI.Tips;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPeroPeroGames.GlobalDefines;
using Il2CppPeroTools2.Commons;
using Il2CppSystem;
using MelonLoader;

namespace CustomAlbums.Patches
{
    internal class HiddenSupportPatch
    {
        // TODO: reduce data redundancy using reference counting dictionary
        internal static HashSet<string> TagLoadedHiddens = new();

        internal static HashSet<string> BmsInfoLoadedHiddens = new();

        internal static void UpdateHiddenCharts()
        {
            HideBmsInfoDicPatch.HasUpdate = true;
            MusicTagPatch.HasUpdate = true;
        }

        [HarmonyPatch(typeof(SpecialSongManager), nameof(SpecialSongManager.InitHideBmsInfoDic))]
        internal static class HideBmsInfoDicPatch
        {
            internal static bool HasUpdate { get; set; } = true;

            private static void Postfix(SpecialSongManager __instance)
            {
                if (HasUpdate)
                {
                    foreach (var (key, value) in AlbumManager.LoadedAlbums)
                    {
                        // Enable hidden mode for charts containing map4
                        if (value.Sheets.ContainsKey(4) && BmsInfoLoadedHiddens.Add($"{AlbumManager.UID}-{value.Index}"))
                        {
                            var albumUid = $"{AlbumManager.UID}-{value.Index}";

                            __instance.m_HideBmsInfos.Add($"{AlbumManager.UID}-{value.Index}", new SpecialSongManager.HideBmsInfo(
                                albumUid,
                                value.Info.HideBmsDifficulty == "0" ? (value.Sheets.ContainsKey(3) ? 3 : 2) : int.Parse(value.Info.HideBmsDifficulty),
                                4,
                                $"{key}_map4",
                                (Il2CppSystem.Func<bool>)delegate { return __instance.IsInvokeHideBms(albumUid); }
                            ));

                            // Add chart to the appropriate list for their hidden type
                            switch (value.Info.HideBmsMode)
                            {
                                case "CLICK":
                                    var newClickArr = new Il2CppStringArray(__instance.m_ClickHideUids.Length + 1);
                                    
                                    for (int i = 0; i < __instance.m_ClickHideUids.Length; i++) 
                                        newClickArr[i] = __instance.m_ClickHideUids[i];
                                    newClickArr[newClickArr.Length - 1] = albumUid;
                                    
                                    __instance.m_ClickHideUids = newClickArr;                               
                                    break;

                                case "PRESS":
                                    var newPressArr = new Il2CppStringArray(__instance.m_LongPressHideUids.Length + 1);
                                    
                                    for (int i = 0; i < __instance.m_LongPressHideUids.Length; i++) 
                                        newPressArr[i] = __instance.m_LongPressHideUids[i];
                                    newPressArr[newPressArr.Length - 1] = albumUid;
                                    
                                    __instance.m_LongPressHideUids = newPressArr;
                                    break;

                                case "TOGGLE":
                                    var newToggleArr = new Il2CppStringArray(__instance.m_ToggleChangedHideUids.Length + 1);
                                    
                                    for (int i = 0; i < __instance.m_ToggleChangedHideUids.Length; i++) 
                                        newToggleArr[i] = __instance.m_ToggleChangedHideUids[i];
                                    newToggleArr[newToggleArr.Length - 1] = albumUid;
                                    
                                    __instance.m_ToggleChangedHideUids = newToggleArr;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    HasUpdate = false;
                }
            }
        }

        /// <summary>
        /// Activates hidden charts when the conditions are met
        /// </summary>
        [HarmonyPatch(typeof(SpecialSongManager), nameof(SpecialSongManager.InvokeHideBms))]
        internal static class InvokeHideBmsPatch
        {
            private static readonly Logger Logger = new(nameof(InvokeHideBmsPatch));
            private static bool Prefix(MusicInfo musicInfo, SpecialSongManager __instance)
            {
                if (musicInfo.uid.StartsWith(AlbumManager.UID.ToString()) && BmsInfoLoadedHiddens.Contains(musicInfo.uid))
                {
                    var hideBms = __instance.m_HideBmsInfos[musicInfo.uid];
                    __instance.m_IsInvokeHideDic[hideBms.uid] = true;

                    if (hideBms.extraCondition.Invoke())
                    {
                        var album = AlbumManager.LoadedAlbums.FirstOrDefault(kv => kv.Value.Index == musicInfo.musicIndex).Value;

                        ActivateHidden(hideBms);

                        if (album.Info.HideBmsMessage != null)
                        {
                            var pnlTips = PnlTipsManager.instance;
                            var msgBox = pnlTips.GetMessageBox("PnlSpecialsBmsAsk");                        
                            msgBox.Show("TIPS", album.Info.HideBmsMessage);
                        }
                        SpecialSongManager.onTriggerHideBmsEvent?.Invoke();
                        if (album.Info.HideBmsMode == "PRESS") Singleton<EventManager>.instance.Invoke("UI/OnSpecialsMusic", null);
                    }
                    return false;
                }
                return true;
            }

            private static bool ActivateHidden(SpecialSongManager.HideBmsInfo hideBms)
            {
                if (hideBms == null) return false;

                var info = GlobalDataBase.dbMusicTag.GetMusicInfoFromAll(hideBms.uid);
                var success = false;
                if (hideBms.triggerDiff != 0)
                {
                    var targetDifficulty = hideBms.triggerDiff;
                    
                    if (targetDifficulty == -1)
                    {
                        targetDifficulty = 2;

                        // Disable the other difficulty options
                        info.AddMaskValue("difficulty1", "0");
                        info.AddMaskValue("difficulty3", "0");
                    }

                    var difficultyToHide = "difficulty" + targetDifficulty;
                    var levelDesignToHide = "levelDesigner" + targetDifficulty;
                    var difficulty = "?";
                    var levelDesignStr = info.levelDesigner;
                    switch (hideBms.m_HideDiff)
                    {
                        case 1:
                            difficulty = info.difficulty1;
                            levelDesignStr = info.levelDesigner1 ?? info.levelDesigner;
                            break;
                        case 2:
                            difficulty = info.difficulty2;
                            levelDesignStr = info.levelDesigner2 ?? info.levelDesigner;
                            break;
                        case 3:
                            difficulty = info.difficulty3;
                            levelDesignStr = info.levelDesigner3 ?? info.levelDesigner;
                            break;
                        case 4:
                            difficulty = info.difficulty4;
                            levelDesignStr = info.levelDesigner4 ?? info.levelDesigner;
                            break;
                        case 5:
                            difficulty = info.difficulty5;
                            levelDesignStr = info.levelDesigner5 ?? info.levelDesigner;
                            break;
                    }
                    info.AddMaskValue(difficultyToHide, difficulty);
                    info.AddMaskValue(levelDesignToHide, levelDesignStr);
                    info.SetDifficulty(targetDifficulty, hideBms.m_HideDiff);

                    success = true;
                }

                return success;
            }
        }


        [HarmonyPatch(typeof(MusicTagManager), nameof(MusicTagManager.InitDefaultInfo))]
        internal class MusicTagPatch
        {
            private static readonly Logger Logger = new(nameof(MusicTagPatch));
            internal static bool HasUpdate { get; set; } = true;

            private const int HIDDEN_ID = 32776;

            private static void Postfix()
            {
                if (!HasUpdate) return;

                Il2CppSystem.Collections.Generic.List<string> newHiddenAlbums = new();
                foreach (var (_, value) in AlbumManager.LoadedAlbums)
                {
                    var uid = $"999-{value.Index}";
                    if (value.Sheets.ContainsKey(DifficultyDefine.hide) && TagLoadedHiddens.Add(uid))
                    {
                        newHiddenAlbums.Add(uid);
                    }
                }
               
                var newHiddenArray = new Il2CppStringArray(DBMusicTagDefine.s_HiddenLocal.Length + newHiddenAlbums.Count);

                // copying s_HiddenLocal to newHiddenArray                 
                for (int i = 0; i < DBMusicTagDefine.s_HiddenLocal.Length; ++i)
                {
                    newHiddenArray[i] = DBMusicTagDefine.s_HiddenLocal[i];
                }

                // adding all new charts to newHiddenArray
                for (int i = 0; i < newHiddenAlbums.Count; i++)
                {
                    newHiddenArray[i + DBMusicTagDefine.s_HiddenLocal.Length] = newHiddenAlbums[i];
                }

                // setting the local hidden data to our new one that has custom charts
                DBMusicTagDefine.s_HiddenLocal = newHiddenArray;

                // adding that data to the tag
                var tagInfo = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(HIDDEN_ID);
                tagInfo.AddTagUids(newHiddenAlbums);
                HasUpdate = false;
            }
        }
    }
}
