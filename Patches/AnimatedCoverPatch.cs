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
            private static readonly LinkedList<MusicStageCell> Cells = new();
            internal static string CurrentScene { get; set; }

            public static void AnimateCoversUpdate()
            {
                if (CurrentScene is not "UISystem_PC") return;
                var dbMusicTag = GlobalDataBase.dbMusicTag;

                if (dbMusicTag == null) return;

                for (var node = Cells.First; node is not null;)
                {
                    var next = node.Next;
                    var cell = node.Value;
                    var index = cell?.m_VariableBehaviour?.Cast<IVariable>().GetResult<int>() ?? -1;

                    var uid = dbMusicTag?.GetShowStageUidByIndex(index) ?? "?";
                    
                    // If uid isn't defined
                    if (uid is "?")
                    {
                        Cells.Remove(node);
                        node = next;
                        continue;
                    }

                    var musicInfo = dbMusicTag?.GetMusicInfoFromAll(uid);
                    
                    // If cell is not custom
                    if (musicInfo?.albumJsonIndex < AlbumManager.Uid)
                    {
                        Cells.Remove(node);
                        node = next;
                        continue;
                    }

                    var album = AlbumManager.GetByUid(uid);
                    var animatedCover = album?.AnimatedCover;
                    
                    // If cell is not animated
                    if (animatedCover is null || animatedCover.FramesPerSecond is 0)
                    {
                        Cells.Remove(node);
                        node = next;
                        continue;
                    }

                    // Animate the cell, with one last null check
                    var frame = (int)Mathf.Floor(Time.time * 1000) %
                        (animatedCover.FramesPerSecond * animatedCover.FrameCount) / animatedCover.FramesPerSecond;
                    if (cell != null) cell.m_StageImg.sprite = animatedCover.Frames[frame];
                    node = next;
                }
            }

            private static void Prefix(MusicStageCell __instance)
            {
                Cells.AddLast(__instance);
            }
        }
    }
}