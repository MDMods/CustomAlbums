using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Common.SceneEgg;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.TouhouLogic;
using Il2CppGameLogic;
using Il2CppPeroPeroGames.GlobalDefines;
using static CustomAlbums.Data.SceneEgg;

namespace CustomAlbums.Patches
{
    internal class SceneEggPatch
    {
        private static readonly Logger Logger = new(nameof(SceneEggPatch));

        internal static bool IgnoreSceneEggs(out Album outAlbum, params SceneEggs[] sceneEggs)
        {
            outAlbum = null;
            // If the chart is not custom then leave
            var uid = DataHelper.selectedMusicUid;
            if (!uid.StartsWith($"{AlbumManager.Uid}-")) return true;

            // If the album doesn't exist (?) or if there are no SceneEggs or if it's christmas SceneEgg (not really a SceneEgg) then leave
            var album = AlbumManager.GetByUid(uid);
            if (album is null) return true;

            outAlbum = album;
            return sceneEggs.Any(sceneEgg => sceneEgg == album.Info.SceneEgg);
        }

        /// <summary>
        ///     Adds support for SceneEggs.
        /// </summary>
        [HarmonyPatch(typeof(SceneEggAbstractController), nameof(SceneEggAbstractController.SceneEggHandle))]
        internal class ControllerPatch
        {
            private static void Prefix(Il2CppSystem.Collections.Generic.List<int> sceneEggIdsBuffer)
            {
                // Return if there are no scene eggs or if the scene eggs have special logic
                if (IgnoreSceneEggs(out var album, SceneEggs.None, SceneEggs.Christmas, SceneEggs.BadApple)) return;

                // Adds the scene egg to the buffer
                sceneEggIdsBuffer.Add((int)album.Info.SceneEgg);
                Logger.Msg("Added SceneEgg " + Enum.GetName(typeof(SceneEggs), album.Info.SceneEgg));

                // Removes all scene eggs from the buffer that are not the one that was added
                // This prevents a hierarchical issue where character choice would be used over chart choice
                sceneEggIdsBuffer.RemoveAll((Il2CppSystem.Predicate<int>)(eggId => eggId != (int)album.Info.SceneEgg));
            }
        }

        /// <summary>
        ///     Makes scene_05 be scene_05_christmas if the Christmas SceneEgg is enabled.
        /// </summary>
        [HarmonyPatch(typeof(GameMusicScene), nameof(GameMusicScene.SceneFestival))]
        internal class SceneFestivalPatch
        {
            private static bool Prefix(string sceneFestivalName, ref string __result)
            {
                // If the scene is not scene_05 or scene_08 then there is no Christmas
                if (sceneFestivalName != "scene_05") return true;

                // Ignore the actual SceneEggs
                if (IgnoreSceneEggs(out _, SceneEggs.Arknights, SceneEggs.Cytus, SceneEggs.None,
                        SceneEggs.Queen, SceneEggs.Touhou, SceneEggs.Wacca, SceneEggs.Miku, SceneEggs.BadApple, SceneEggs.RinLen)) return true;

                if (sceneFestivalName == "scene_05") __result = "scene_05_christmas";
                return false;
            }
        }

        /// <summary>
        ///     Makes the boss be the christmas boss if the Christmas SceneEgg is enabled.
        /// </summary>
        [HarmonyPatch(typeof(Boss), nameof(Boss.BossFestival))]
        internal class BossFestivalPatch
        {
            private static bool Prefix(string bossFestivalName, ref string __result)
            {
                // If the boss is not 0501_boss then there is no Christmas
                if (bossFestivalName != "0501_boss") return true;
                if (IgnoreSceneEggs(out _, SceneEggs.Arknights, SceneEggs.Cytus, SceneEggs.None,
                        SceneEggs.Queen, SceneEggs.Touhou, SceneEggs.Wacca, SceneEggs.Miku, SceneEggs.BadApple, SceneEggs.RinLen)) return true;

                __result = "0501_boss_christmas";
                return false;
            }
        }

        /// <summary>
        ///     Makes the game think (temporarily) that the chart is Bad Apple when the BadApple SceneEgg is enabled to load all
        ///     the assets properly.
        /// </summary>
        [HarmonyPatch(typeof(DBTouhou), nameof(DBTouhou.AwakeInit))]
        internal class BadApplePatch
        {
            private static void Postfix()
            {
                if (IgnoreSceneEggs(out _, SceneEggs.Arknights, SceneEggs.Cytus, SceneEggs.None,
                        SceneEggs.Queen, SceneEggs.Touhou, SceneEggs.Wacca, SceneEggs.Miku, SceneEggs.Christmas, SceneEggs.RinLen, SceneEggs.BlueArchive)) return;

                GlobalDataBase.dbTouhou.isBadApple = true;

                GlobalDataBase.s_DbOther.m_HpFx = TouhouLogic.ReplaceBadAppleString("fx_hp_ground");
                GlobalDataBase.s_DbOther.m_MusicFx = TouhouLogic.ReplaceBadAppleString("fx_score_ground");
                GlobalDataBase.s_DbOther.m_DustFx = TouhouLogic.ReplaceBadAppleString("dust_fx");
            }
        }


        /// <summary>
        ///     Makes the game think that the chart is a RinLen chart when the RinLen SceneEgg is enabled to load all
        ///     the assets properly. 
        /// </summary>
        [HarmonyPatch(typeof(DBMusicTagDefine), nameof(DBMusicTagDefine.IsRinLenEggSong))]
        internal class RinLenPatch
        {
            private static void Postfix(string uid, ref bool __result)
            {
                if (uid.StartsWith($"{AlbumManager.Uid}-") && AlbumManager.GetByUid(uid).Info.SceneEgg is SceneEggs.RinLen)
                {
                    __result = true;
                }
            }
        }
    }
}