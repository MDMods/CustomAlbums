using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAccount;
using Il2CppAssets.Scripts.Database;
using Il2CppPeroPeroGames.DataStatistics;

namespace CustomAlbums.Patches
{
    internal class WebApiPatch
    {
        /// <summary>
        /// Patches the SendToUrl method to not attempt to send stats or high scores of custom charts to the official server.
        /// </summary>
        [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.SendToUrl))]
        internal class SendToUrlPatch
        {
            private static readonly Logger Logger = new("WebApiPatch");
            private static bool Prefix(string url, string method, Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object> datas)
            {

                switch (url)
                {
                    case "statistics/pc-play-statistics-feedback":
                        if (datas["music_uid"].ToString().StartsWith($"{AlbumManager.Uid}"))
                        {
                            Logger.Msg("Blocked play feedback upload: " + datas["music_uid"].ToString());
                            return false;
                        }
                        break;
                    case "musedash/v2/pcleaderboard/high-score":
                        if (GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}"))
                        {
                            Logger.Msg("Blocked high score upload v2: " + GlobalDataBase.dbBattleStage.musicUid);
                            return false;
                        }
                        break;
                    case "musedash/v3/pcleaderboard/high-score":
                        if (GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}"))
                        {
                            Logger.Msg("Blocked high score upload v3: " + GlobalDataBase.dbBattleStage.musicUid);
                            return false;
                        }
                        break;
                }
                return true;
            }
        }

        /// <summary>
        /// Prevents the game from sending any analytics if the musicInfo is custom.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper), nameof(ThinkingDataPeripheralHelper.PostToThinkingData))]
        internal class PostToThinkingDataPatch
        {
            private static bool Prefix(string dataStatisticsEventDefinesName, MusicInfo musicInfo)
            {
                return !musicInfo.uid.StartsWith($"{AlbumManager.Uid}-");
            }
        }

        /// <summary>
        /// Prevents the game from sending any analytics if the musicInfo is custom.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper), nameof(ThinkingDataPeripheralHelper.SendFavoriteMusicBehavior))]
        internal class SendFavoriteMusicBehaviorPatch
        {
            private static bool Prefix(string dataStatisticsEventDefinesNameMusicInfo, MusicInfo musicInfo)
            {
                return !musicInfo.uid.StartsWith($"{AlbumManager.Uid}-");
            }
        }
    }
}