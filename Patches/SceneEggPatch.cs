using CustomAlbums.Managers;
using HarmonyLib;
using Il2CppAssets.Scripts.Common.SceneEgg;
using Il2CppAssets.Scripts.Database;
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
            private static void Prefix(Il2CppSystem.Collections.Generic.List<int> sceneEggIdsBuffer)
            {
                // If the chart is not custom then leave
                var uid = DataHelper.selectedMusicUid;
                if (!uid.StartsWith($"{AlbumManager.UID}-")) return;

                // If the album doesn't exist (?) or if there are no SceneEggs then leave
                var album = AlbumManager.GetByUid(uid);
                if (album is null || album.Info.SceneEgg is SceneEggs.None) return;

                // Adds the scene egg to the buffer
                sceneEggIdsBuffer.Add((int)album.Info.SceneEgg);

                // Removes all scene eggs from the buffer that are not the one that was added
                // This prevents a hierarchical issue where character choice would be used over chart choice
                sceneEggIdsBuffer.RemoveAll((Il2CppSystem.Predicate<int>)(eggId => eggId != (int)album.Info.SceneEgg));
            }
        }
    }
}
