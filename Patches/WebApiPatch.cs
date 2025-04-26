using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAccount;
using Il2CppAssets.Scripts.Database;
using Object = Il2CppSystem.Object;

namespace CustomAlbums.Patches;

internal class WebApiPatch
{
    /// <summary>
    ///     Patches the SendToUrl method to not attempt to send stats or high scores of custom charts to the official server.
    /// </summary>
    [HarmonyPatch(typeof(GameAccountSystem), nameof(GameAccountSystem.SendToUrl))]
    internal class SendToUrlPatch
    {
        private static readonly Logger Logger = new("WebApiPatch");

        private static bool Prefix(string url, string method,
            Il2CppSystem.Collections.Generic.Dictionary<string, Object> datas)
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
}