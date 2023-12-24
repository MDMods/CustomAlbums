using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Common.SceneEgg;
using Il2CppAssets.Scripts.Database;
using Il2CppGameLogic;
using static CustomAlbums.Data.SceneEgg;

namespace CustomAlbums.Patches
{
    internal class SceneEggPatch
    {
        private static readonly Logger Logger = new(nameof(SceneEggPatch));
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

                // If the album doesn't exist (?) or if there are no SceneEggs or if it's christmas SceneEgg (not really a SceneEgg) then leave
                var album = AlbumManager.GetByUid(uid);
                if (album is null || album.Info.SceneEgg is SceneEggs.None or SceneEggs.Christmas) return;

                // Adds the scene egg to the buffer
                sceneEggIdsBuffer.Add((int)album.Info.SceneEgg);
                Logger.Msg("Added SceneEgg " + Enum.GetName(typeof(SceneEggs), album.Info.SceneEgg));

                // Removes all scene eggs from the buffer that are not the one that was added
                // This prevents a hierarchical issue where character choice would be used over chart choice
                sceneEggIdsBuffer.RemoveAll((Il2CppSystem.Predicate<int>)(eggId => eggId != (int)album.Info.SceneEgg));
            }
        }

        /// <summary>
        /// Makes scene_05 be scene_05_christmas if the Christmas SceneEgg is enabled.
        /// </summary>
        [HarmonyPatch(typeof(GameMusicScene), nameof(GameMusicScene.SceneFestival))]
        internal class SceneFestivalPatch
        {
            private static bool Prefix(string sceneFestivalName, ref string __result)
            {
                // If the scene is not scene_05 then there is no Christmas
                if (sceneFestivalName != "scene_05") return true;
               
                // If the chart isn't custom then it's already handled properly
                var uid = DataHelper.selectedMusicUid;
                if (!uid.StartsWith($"{AlbumManager.UID}-")) return true;

                // If the custom chart doesn't have Christmas SceneEgg then we don't care
                if (AlbumManager.GetByUid(uid)?.Info.SceneEgg is not SceneEggs.Christmas) return true;

                __result = "scene_05_christmas";
                return false;
            }
        }

        /// <summary>
        /// Makes the boss be the christmas boss if the Christmas SceneEgg is enabled.
        /// </summary>
        [HarmonyPatch(typeof(Boss), nameof(Boss.BossFestival))]
        internal class BossFestivalPatch
        {
            private static bool Prefix(string bossFestivalName, ref string __result)
            {
                // If the boss is not 0501_boss then there is no Christmas
                if (bossFestivalName != "0501_boss") return true;

                // If the chart isn't custom then it's already handled properly
                var uid = DataHelper.selectedMusicUid;
                if (!uid.StartsWith($"{AlbumManager.UID}-")) return true;
                 
                // If the custom chart doesn't have Christmas SceneEgg then we don't care
                if (AlbumManager.GetByUid(uid)?.Info.SceneEgg is not SceneEggs.Christmas) return true;

                __result = "0501_boss_christmas";
                return false;
            }
        }

    }
}
