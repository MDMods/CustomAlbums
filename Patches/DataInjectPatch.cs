using CustomAlbums.Data;
using CustomAlbums.Managers;
using CustomAlbums.Utilities;
using HarmonyLib;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;
using Il2CppAssets.Scripts.PeroTools.Nice.Variables;
using IDataList = Il2CppSystem.Collections.Generic.List<Il2CppAssets.Scripts.PeroTools.Nice.Interface.IData>;

namespace CustomAlbums.Patches
{
    /// <summary>
    /// This patch modifies the highest property of DataHelper to include custom charts.
    /// </summary>
    [HarmonyPatch(typeof(DataHelper), nameof(DataHelper.highest), MethodType.Getter)]
    internal class DataInjectPatch
    {
        internal static readonly IDataList DataList = new();
        private static readonly Logger Logger = new(nameof(DataInjectPatch));

        // ReSharper disable once InconsistentNaming
        private static void Postfix(ref IDataList __result)
        {
            var highest = DataHelper.s_DataManager["Achievement"]["highest"].GetResult<IDataList>();
            if (highest == null) return;

            if (DataList.Count > 0)
            {
                var combined1 = new IDataList();
                foreach (var item in highest)
                {
                    combined1.Add(item);
                }
                foreach (var item in DataList)
                {
                    combined1.Add(item);
                }

                __result = combined1;
                return;
            }

            var customsHighest = SaveManager.SaveData.Highest;

            foreach (var (albumName, albumDic) in customsHighest)
            {
                foreach (var (difficulty, save) in albumDic)
                {
                    if (!AlbumManager.LoadedAlbums.TryGetValue(albumName, out var album))
                    {
                        Logger.Warning($"No album found for key {albumName}.");
                        continue;
                    }

                    var data = CreateIData(album, difficulty, save);
                    DataList.Add(data);
                }
            }

            // combine the two lists highest and data
            var combined = new IDataList();
            foreach (var item in highest)
            {
                combined.Add(item);
            }
            foreach (var item in DataList)
            {
                combined.Add(item);
            }

            // set the result to the combined list
            __result = combined;
        }

        /// <summary>
        /// Creates a Muse Dash-compatible IData object from a CustomChartSave object.
        /// </summary>
        /// <param name="album">The album of the data to be created.</param>
        /// <param name="difficulty">The difficulty of the chart.</param>
        /// <param name="save">The CustomChartSave object containing the save data.</param>
        /// <returns></returns>
        internal static IData CreateIData(Album album, int difficulty, CustomChartSave save)
        {
            var data = new Il2CppAssets.Scripts.PeroTools.Nice.Datas.Data();

            data.fields.Add("uid", CreateIVariable($"{album.Uid}_{difficulty}"));
            data.fields.Add("evaluate", CreateIVariable(save.Evaluate));
            data.fields.Add("score", CreateIVariable(save.Score));
            data.fields.Add("combo", CreateIVariable(save.Combo));
            data.fields.Add("accuracy", CreateIVariable(save.Accuracy));
            data.fields.Add("accuracyStr", CreateIVariable(save.AccuracyStr));
            data.fields.Add("clear", CreateIVariable(save.Clear));
            data.fields.Add("failCount", CreateIVariable(save.FailCount));
            data.fields.Add("passed", CreateIVariable(save.Passed));

            return data.Cast<IData>();
        }

        /// <summary>
        /// Creates a Muse Dash-compatible IVariable object from an Il2CppSystem.Object.
        /// </summary>
        /// <param name="obj">The object (Il2Cpp) to be converted to IVariable.</param>
        /// <returns></returns>
        internal static IVariable CreateIVariable(Il2CppSystem.Object obj)
        {
            var constance = new Constance
            {
                result = obj
            };

            return constance.Cast<IVariable>();
        }
    }
}
