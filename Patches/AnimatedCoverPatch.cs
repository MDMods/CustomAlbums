using CustomAlbums.Managers;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Patches
{
    internal class AnimatedCoverPatch
    {
        /// <summary>
        ///     Enables animated album covers.
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
                    if (Cells[i] == null || !Cells[i].enabled)
                        Cells.RemoveAt(i);

                foreach (var cell in Cells)
                {
                    var index = cell?.m_VariableBehaviour?.Cast<IVariable>().GetResult<int>() ?? -1;

                    var uid = dbMusicTag?.GetShowStageUidByIndex(index) ?? "?";
                    if (uid == "?") continue;

                    var musicInfo = dbMusicTag?.GetMusicInfoFromAll(uid);
                    if (musicInfo?.albumJsonIndex < AlbumManager.Uid) continue;


                    var album = AlbumManager.GetByUid(uid);
                    var animatedCover = album?.AnimatedCover;
                    if (animatedCover is null || animatedCover.FramesPerSecond == 0) continue;
                    var frame = (int)Mathf.Floor(Time.time * 1000) %
                        (animatedCover.FramesPerSecond * animatedCover.FrameCount) / animatedCover.FramesPerSecond;
                    if (cell != null) cell.m_StageImg.sprite = animatedCover.Frames[frame];
                }
            }

            private static void Prefix(MusicStageCell __instance)
            {
                Cells.Add(__instance);
            }
        }
    }
}