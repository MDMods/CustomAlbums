using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.Database.DataClass;
using static Il2CppAssets.Scripts.Database.DBConfigCustomTags;

namespace CustomAlbums.Patches
{
    internal class TagPatch
    {
        [HarmonyPatch(typeof(MusicTagManager), nameof(MusicTagManager.InitAlbumTagInfo))]
        internal class MusicTagPatch
        {
            private static void Postfix()
            {
                var info = new AlbumTagInfo
                {
                    name = AlbumManager.Languages["English"],
                    tagUid = "tag-custom-albums",
                    iconName = "IconCustomAlbums"
                };
                var customInfo = new CustomTagInfo
                {
                    tag_name = AlbumManager.Languages.ToIl2Cpp(),
                    tag_picture = "https://cdn.mdmc.moe/static/melon.png",
                    music_list = AlbumManager.GetAllUid().ToIl2Cpp()
                };

                info.InitCustomTagInfo(customInfo);

                GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Insert(GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Count - 4,
                    AlbumManager.Uid);
                GlobalDataBase.dbMusicTag.AddAlbumTagData(AlbumManager.Uid, info);
            }
        }
    }
}