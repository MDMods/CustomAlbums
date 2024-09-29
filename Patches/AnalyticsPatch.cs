using CustomAlbums.Managers;
using HarmonyLib;
using Il2CppAssets.Scripts.Database;
using Il2CppPeroPeroGames.DataStatistics;
using System.Reflection;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Patches
{
    internal class AnalyticsPatch
    {
        internal static readonly Logger Logger = new(nameof(AnalyticsPatch));
        
        /// <summary>
        ///     Blocks tag analytics from being sent if tag is CustomAlbums.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper), nameof(ThinkingDataPeripheralHelper.CommonSendString))]
        internal class CommonSendStringPatch
        {
            private static bool Prefix(string eventName, string propertyName, string info)
            {
                var runCond = info != AlbumManager.Languages["ChineseS"];
                if (!runCond) Logger.Msg("Blocked custom albums tag analytics.");
                return runCond;
            }
        }

        /// <summary>
        ///     Blocks chart analytics from being sent if the chart is custom.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataBattleHelper))]
        internal class SendMDPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(ThinkingDataBattleHelper).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Where(method => method.Name.StartsWith("Send"));
            }

            private static bool Prefix()
            {
                var runCond = !BattleHelper.MusicInfo().uid.StartsWith($"{AlbumManager.Uid}-");
                if (!runCond) Logger.Msg("Blocked sending analytics of custom chart.");
                return runCond;
            }
        }


        /// <summary>
        ///     Prevents the game from sending any analytics if the musicInfo is custom.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper), nameof(ThinkingDataPeripheralHelper.PostToThinkingData))]
        internal class PostToThinkingDataPatch
        {
            private static bool Prefix(string dataStatisticsEventDefinesName, MusicInfo musicInfo)
            {
                var runCond = !musicInfo.uid.StartsWith($"{AlbumManager.Uid}-");
                if (!runCond) Logger.Msg("Blocked thinking data post of custom chart.");
                return runCond;
            }
        }

        /// <summary>
        ///     Prevents the game from sending any analytics if the favorited chart is custom.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper),
            nameof(ThinkingDataPeripheralHelper.SendFavoriteMusicBehavior))]
        internal class SendFavoriteMusicBehaviorPatch
        {
            private static bool Prefix(string dataStatisticsEventDefinesNameMusicInfo, MusicInfo musicInfo)
            {
                var runCond = !musicInfo.uid.StartsWith($"{AlbumManager.Uid}-");
                if (!runCond) Logger.Msg("Blocked sending favorite chart analytics of custom chart.");
                return runCond;
            }
        }


        /// <summary>
        ///     Prevents the game from sending any analytics if the musicInfo is custom.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper), nameof(ThinkingDataPeripheralHelper.PostMusicChooseInfo))]
        internal class PostMusicChooseInfoPatch
        {
            private static bool Prefix(Vector2Int diffValue, string chooseMusicType, string searchName, MusicInfo musicInfo)
            {
                var runCond = !musicInfo.uid.StartsWith($"{AlbumManager.Uid}-");
                if (!runCond) Logger.Msg("Blocked sending chosen music from search analytics of custom chart.");
                return runCond;
            }
        }

        /// <summary>
        ///     Cleans search result analytics of custom charts.
        /// </summary>
        [HarmonyPatch(typeof(ThinkingDataPeripheralHelper), nameof(ThinkingDataPeripheralHelper.GetSearchResultInfo))]
        internal class GetSearchResultInfoPatch
        {

            private static bool Prefix(MusicInfo musicInfo)
            {
                var runCond = !musicInfo.uid.StartsWith($"{AlbumManager.Uid}-");
                if (!runCond) Logger.Msg("Blocking custom album from being added to search analytics.");
                return runCond;
            }    
        }
    }
}
