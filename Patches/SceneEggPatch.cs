using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAssets.Scripts.Common.SceneEgg;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore.Managers;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Nice.Datas;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using static CustomAlbums.Data.SceneEgg;

namespace CustomAlbums.Patches
{
    internal class SceneEggPatch
    {
        /// <summary>
        /// Adds support for SceneEggs.
        /// </summary>
        [HarmonyPatch(typeof(SceneEggAbstractController), nameof(SceneEggAbstractController.SceneEggHandle))]
        internal class ControllerPatch
        {
            private static readonly Logger Logger = new(nameof(ControllerPatch));
            private static void Prefix(Il2CppSystem.Collections.Generic.List<int> sceneEggIdsBuffer)
            {
                var uid = DataHelper.selectedMusicUid;
                if (!uid.StartsWith("999-")) return;

                var album = AlbumManager.GetByUid(uid);
                if (album is null || album.Info.SceneEgg == SceneEggs.None) return;
                sceneEggIdsBuffer.Add((int)album.Info.SceneEgg);
            }
        }
    }
}
