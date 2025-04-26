using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;
using Il2CppAssets.Scripts.UI.Tips;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppPeroPeroGames.GlobalDefines;
using MelonLoader;

namespace CustomAlbums.Patches
{
    internal class HiddenSupportPatch
    {
        internal static HashSet<string> LoadedHiddens = new();

        internal static void UpdateHiddenCharts()
        {
            HideBmsInfoDicPatch.HasUpdate = true;
            MusicTagPatch.HasUpdate = true;
        }

        [HarmonyPatch(typeof(SpecialSongManager), nameof(SpecialSongManager.InitHideBmsInfoDic))]
        internal static class HideBmsInfoDicPatch
        {
            internal static bool HasUpdate { get; set; } = true;
            private static readonly Logger Logger = new(nameof(HideBmsInfoDicPatch));

            private static void Postfix(SpecialSongManager __instance)
            {
                if (!HasUpdate) return;

                foreach (var (key, value) in AlbumManager.LoadedAlbums.Where(kv => kv.Value.HasDifficulty(4) || kv.Value.HasDifficulty(5)))
                {
                    // Quick check for Touhou charts
                    if (value.HasDifficulty(5))
                    {
                        var newTouhouArray = new Il2CppStringArray(DBMusicTagDefine.s_BarrageModeSongUid.Length + 1);
                        for (var i = 0; i < DBMusicTagDefine.s_BarrageModeSongUid.Length; i++)
                            newTouhouArray[i] = DBMusicTagDefine.s_BarrageModeSongUid[i];
                        newTouhouArray[^1] = value.Uid;
                        DBMusicTagDefine.s_BarrageModeSongUid = newTouhouArray;
                    }
                    // Enable hidden mode for charts containing map4
                    if (!LoadedHiddens.Contains(value.Uid)) continue;

                    var albumUid = value.Uid;

                    __instance.m_HideBmsInfos.Add(albumUid,
                        new SpecialSongManager.HideBmsInfo(
                            albumUid,
                            value.Info.HideBmsDifficulty == "0"
                                ? value.HasDifficulty(3) ? 3 : 2
                                : value.Info.HideBmsDifficulty.ParseAsInt(),
                            4,
                            $"{key}_map4",
                            new Func<bool>(() => __instance.IsInvokeHideBms(albumUid))
                        ));

                    // Add chart to the appropriate list for their hidden type
                    var hideMusicInfo = new HideMusicInfo
                    {
                        musicUid = albumUid,
                        invokeType = value.Info.HideBmsMode switch
                        {
                            "CLICK" => (int)HiddenInvokeType.Click,
                            "PRESS" => (int)HiddenInvokeType.LongPress,
                            "TOGGLE" => (int)HiddenInvokeType.Toggle,
                            _ => (int)HiddenInvokeType.None
                        }
                    };

                    __instance.m_ConfigHideMusic.m_HideMusicObjectMapping[albumUid] = hideMusicInfo;
                }
                HasUpdate = false;
            }
        }

        /// <summary>
        ///     Activates hidden charts when the conditions are met
        /// </summary>
        [HarmonyPatch(typeof(SpecialSongManager), nameof(SpecialSongManager.InvokeHideBms))]
        internal static class InvokeHideBmsPatch
        {
            private static readonly Logger Logger = new(nameof(InvokeHideBmsPatch));

            private static bool Prefix(MusicInfo musicInfo, SpecialSongManager __instance)
            {
                if (!musicInfo.uid.StartsWith($"{AlbumManager.Uid}-") ||
                    !LoadedHiddens.Contains(musicInfo.uid)) return true;

                var hideBms = __instance.m_HideBmsInfos[musicInfo.uid];
                __instance.m_IsInvokeHideDic[hideBms.uid] = true;

                if (!hideBms.extraCondition.Invoke()) return false;

                var album = AlbumManager.LoadedAlbums.FirstOrDefault(kv => kv.Value.Index == musicInfo.musicIndex)
                    .Value;

                ActivateHidden(hideBms);

                if (album.Info.HideBmsMessage != null)
                {
                    var pnlTips = PnlTipsManager.instance;
                    var msgBox = pnlTips.GetMessageBox("PnlSpecialsBmsAsk");
                    msgBox.Show("TIPS", album.Info.HideBmsMessage);
                }

                SpecialSongManager.onTriggerHideBmsEvent?.Invoke();
                if (album.Info.HideBmsMode == "PRESS") Singleton<EventManager>.instance.Invoke("UI/OnSpecialsMusic");
                return false;
            }

            private static void ActivateHidden(SpecialSongManager.HideBmsInfo hideBms)
            {
                if (hideBms == null) return;

                var info = GlobalDataBase.dbMusicTag.GetMusicInfoFromAll(hideBms.uid);
                if (hideBms.triggerDiff == 0) return;

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
            }
        }


        [HarmonyPatch(typeof(MusicTagManager), nameof(MusicTagManager.InitDefaultInfo))]
        internal class MusicTagPatch
        {
            private const int HiddenId = 32776;
            private static readonly Logger Logger = new(nameof(MusicTagPatch));
            internal static bool HasUpdate { get; set; } = true;

            private static void Postfix()
            {
                if (!HasUpdate) return;

                Il2CppSystem.Collections.Generic.List<string> newHiddenAlbums = new();
                foreach (var (_, value) in AlbumManager.LoadedAlbums)
                {
                    if (value.HasDifficulty(DifficultyDefine.hide) && LoadedHiddens.Add(value.Uid))
                        newHiddenAlbums.Add(value.Uid);
                }

                var newHiddenArray =
                    new Il2CppStringArray(DBMusicTagDefine.s_HiddenLocal.Length + newHiddenAlbums.Count);

                // copying s_HiddenLocal to newHiddenArray                 
                for (var i = 0; i < DBMusicTagDefine.s_HiddenLocal.Length; ++i)
                    newHiddenArray[i] = DBMusicTagDefine.s_HiddenLocal[i];

                // adding all new charts to newHiddenArray
                for (var i = 0; i < newHiddenAlbums.Count; i++)
                    newHiddenArray[i + DBMusicTagDefine.s_HiddenLocal.Length] = newHiddenAlbums[i];

                // setting the local hidden data to our new one that has custom charts
                DBMusicTagDefine.s_HiddenLocal = newHiddenArray;

                // adding that data to the tag
                var tagInfo = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(HiddenId);
                tagInfo.AddTagUids(newHiddenAlbums);
                HasUpdate = false;
            }
        }
    }

    internal enum HiddenInvokeType
    {
        None = 0,
        LongPress = 1,
        Toggle = 2,
        Click = 3,
        Special = 4
    }
}