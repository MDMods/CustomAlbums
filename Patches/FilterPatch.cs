using CustomAlbums.Managers;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.Helpers;

namespace CustomAlbums.Patches;

internal class FilterPatch
{
    /// <summary>
    ///     Applies "Assisted screening" filters to custom charts
    /// </summary>
    [HarmonyPatch(typeof(MusicTagHelper), nameof(MusicTagHelper.AdditionalFilter))]
    internal static class AdditionalFilterPatch
    {
        private static bool AllMatchingMeetCondition(string albumName, MusicInfo musicInfo, int starLevel,
            Func<string, int, bool> condition)
        {
            var starStr = starLevel.ToString();
            var foundMatchingStar = false;
            var allMeet = true;

            for (var i = 1; i <= 4; i++)
            {
                var diffStr = i switch
                {
                    1 => musicInfo.difficulty1,
                    2 => musicInfo.difficulty2,
                    3 => musicInfo.difficulty3,
                    4 => musicInfo.difficulty4,
                    _ => "0"
                };

                if (diffStr == starStr || (starLevel == 0 && diffStr == "?"))
                {
                    foundMatchingStar = true;
                    if (!condition(albumName, i)) allMeet = false;
                }
            }

            return foundMatchingStar && allMeet;
        }

        private static void Postfix(Il2CppSystem.Collections.Generic.List<MusicInfo> buffer, int difficultyIndex)
        {
            var filter = MusicTagHelper.GetAdditionalConditions();
            if (filter == DifficultyFilterDefines.All) return;

            // Iterate backward to safely remove elements
            for (var i = buffer.Count - 1; i >= 0; i--)
            {
                var musicInfo = buffer[i];
                if (!musicInfo.uid.StartsWith($"{AlbumManager.Uid}-")) continue;

                var albumName = AlbumManager.GetAlbumNameFromUid(musicInfo.uid);
                var remove = false;

                switch (filter)
                {
                    case DifficultyFilterDefines.NoClear:
                        remove = AllMatchingMeetCondition(albumName, musicInfo, difficultyIndex, (album, slot) =>
                        {
                            return SaveManager.SaveData.Highest.TryGetValue(album, out var highest)
                                   && highest.ContainsKey(slot);
                        });
                        break;

                    case DifficultyFilterDefines.NoFullCombo:
                        remove = AllMatchingMeetCondition(albumName, musicInfo, difficultyIndex, (album, slot) =>
                        {
                            return SaveManager.SaveData.FullCombo.TryGetValue(album, out var fcs)
                                   && fcs.Contains(slot);
                        });
                        break;

                    case DifficultyFilterDefines.NoRankS:
                        remove = AllMatchingMeetCondition(albumName, musicInfo, difficultyIndex, (album, slot) =>
                        {
                            return SaveManager.SaveData.Highest.TryGetValue(album, out var highest)
                                   && highest.TryGetValue(slot, out var chartSave)
                                   && chartSave.Evaluate >= 4;
                        });
                        break;

                    case DifficultyFilterDefines.NoAllPerfect:
                        remove = AllMatchingMeetCondition(albumName, musicInfo, difficultyIndex, (album, slot) =>
                        {
                            return SaveManager.SaveData.Highest.TryGetValue(album, out var highest)
                                   && highest.TryGetValue(slot, out var chartSave)
                                   && chartSave.Accuracy >= 100f;
                        });
                        break;

                    case DifficultyFilterDefines.NoLevelAchievement:
                        break;
                }

                if (remove) buffer.RemoveAt(i);
            }
        }
    }
}