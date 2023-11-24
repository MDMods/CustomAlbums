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
        /// <summary>
        /// Enables animated album covers.
        /// </summary>
        [HarmonyPatch(typeof(MusicStageCell), nameof(MusicStageCell.Awake))]
        internal static class MusicStageCellPatch
        {
            private static readonly Logger Logger = new(nameof(MusicStageCellPatch));
            private static readonly List<MusicStageCell> Cells = new();

            public static void AnimateCoversUpdate()
            {
                var dbMusicTag = GlobalDataBase.dbMusicTag;

                if (dbMusicTag == null) return;

                for (var i = Cells.Count - 1; i >= 0; i--)
                {
                    if (Cells[i] == null || !Cells[i].enabled)
                    {
                        Cells.RemoveAt(i);
                    }
                }

                foreach (var cell in Cells)
                {
                    var index = cell.m_VariableBehaviour.Cast<IVariable>().GetResult<int>();
                    
                    var uid = dbMusicTag.GetShowStageUidByIndex(index);
                    if (uid == null) continue;
                    
                    var musicInfo = dbMusicTag.GetMusicInfoFromAll(uid);
                    if (musicInfo.albumJsonIndex < AlbumManager.UID) continue;

                    if (uid != "?")
                    {
                        var animatedCover = AlbumManager.GetByUid(uid).AnimatedCover;
                        if (animatedCover is null || animatedCover.FramesPerSecond == 0) continue;
                        var frame = (int)Mathf.Floor(Time.time * 1000) % (animatedCover.FramesPerSecond * animatedCover.FrameCount) / animatedCover.FramesPerSecond;
                        cell.m_StageImg.sprite = animatedCover.Frames[frame];
                    }
                }
            }

            private static void Prefix(MusicStageCell __instance)
            {
                Cells.Add(__instance);
            }
        }
    }
}
