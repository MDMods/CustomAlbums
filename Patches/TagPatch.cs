using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    tag_picture = "https://mdmc.moe/cdn/melon.png",
                };
                customInfo.music_list = new Il2CppSystem.Collections.Generic.List<string>();
                foreach (var uid in AlbumManager.GetAllUid()) customInfo.music_list.Add(uid);

                info.InitCustomTagInfo(customInfo);

                GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Insert(GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Count - 4, AlbumManager.Uid);
                GlobalDataBase.dbMusicTag.AddAlbumTagData(AlbumManager.Uid, info);
            }
        }
    }
}
