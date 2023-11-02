using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CustomAlbums.Data;
using CustomAlbums.Managers;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using MelonLoader;
using UnityEngine;
using static MelonLoader.MelonLogger;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Patches
{
    internal class AnimatedCoverPatch
    {

        [HarmonyPatch(typeof(MusicStageCell), nameof(MusicStageCell.Awake))]
        internal static class MusicStageCellPatch
        {
            private static readonly Logger Logger = new(nameof(MusicStageCellPatch));
            private static List<MusicStageCell> cells = new();

            public static void AnimateCovers()
            {
                var dbMusicTag = GlobalDataBase.dbMusicTag;

                if (dbMusicTag == null) return;

                for (var i = cells.Count - 1; i >= 0; i--)
                {
                    if (cells[i] == null || !cells[i].enabled)
                    {
                        cells.RemoveAt(i);
                    }
                }

                foreach (var cell in cells)
                {
                    var index = cell.m_VariableBehaviour.Cast<IVariable>().GetResult<int>();
                    var uid = dbMusicTag.GetShowStageUidByIndex(index);
                    var musicInfo = dbMusicTag.GetMusicInfoFromAll(uid);
                    if (musicInfo.albumJsonIndex < AlbumManager.Uid) continue;

                    if (uid != "?")
                    {
                        var animatedCover = AlbumManager.LoadedAlbums[uid.Replace("999-", "album_")].AnimatedCover;
                        if (animatedCover == null) continue;
                        var frame = ((int)Mathf.Floor(Time.time * 1000) % (animatedCover.FramesPerSecond * animatedCover.FrameCount)) / animatedCover.FramesPerSecond;
                        cell.m_StageImg.sprite = animatedCover.Frames[Math.Min(frame, animatedCover.FrameCount)];
                    }
                }
            }

            private static void Prefix(MusicStageCell __instance)
            {
                cells.Add(__instance);
            }
        }
    }
}
