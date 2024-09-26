using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;

namespace CustomAlbums.Patches
{
    internal class PackPatch
    {
        private static readonly Logger Logger = new(nameof(PackPatch));
        
        [HarmonyPatch(typeof(LongSongNameController), nameof(LongSongNameController.Refresh), new[] { typeof(string), typeof(bool), typeof(float) })]
        internal class RefreshPatch {
            private static void SetColor(string colorHex, LongSongNameController instance)
            {
                var fixedColor = UnityEngine.ColorUtility.TryParseHtmlString(colorHex, out var color) ? color : UnityEngine.Color.white;
                if (instance.m_MidSimpleName != null) instance.m_MidSimpleName.color = fixedColor;
                if (instance.m_TxtBackupName != null) instance.m_TxtBackupName.color = fixedColor;
                if (instance.m_TxtSimpleName != null) instance.m_TxtSimpleName.color = fixedColor;
            }

            private static void Prefix(ref string text, ref Pack __state)
            {
                var currentUid = DataHelper.selectedMusicUid;
                if (currentUid == null || !currentUid.StartsWith($"{AlbumManager.Uid}-") || text != AlbumManager.GetCustomAlbumsTitle()) return;
                
                __state = PackManager.GetPackFromUid(currentUid);
                text = __state?.Title ?? text;
            }

            private static void Postfix(ref Pack __state, LongSongNameController __instance)
            {
                if (__instance == null) return;
                SetColor(__state?.TitleColorHex, __instance);
            }
        }
    }
}
