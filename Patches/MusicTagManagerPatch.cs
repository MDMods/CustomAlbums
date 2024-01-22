using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using HarmonyLib;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppAssets.Scripts.PeroTools.Managers;

namespace CustomAlbums.Patches
{
    internal class MusicTagManagerPatch
    {
        /// <summary>
        ///     Makes the game think (correctly) that there are not 1000 albums upon loading the tag menu.
        ///     Increases performance substantially upon loading the tag menu.
        /// </summary>
        [HarmonyPatch(typeof(MusicTagManager), nameof(MusicTagManager.InitDatas))]
        internal static class Fix1000AlbumsPatch
        {
            private static void Postfix()
            {
                var configObject = Singleton<ConfigManager>.instance.GetConfigObject<DBConfigAlbums>();
                configObject.m_MaxAlbumUid = configObject.count - 3;
            }
        }
    }
}
