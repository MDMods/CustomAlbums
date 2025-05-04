using CustomAlbums.Data;
using Il2CppAssets.Scripts.PeroTools.Nice.Interface;

namespace CustomAlbums.Utilities
{
    internal static class DataExtensions
    {
        /// <summary>
        /// Gets the uid field of the IData object.
        /// </summary>
        /// <param name="data">The IData object.</param>
        /// <returns>The uid or an empty string if not found.</returns>
        public static string GetUid(this IData data)
        {
            var uidField = data.fields["uid"];
            return uidField == null ? string.Empty : uidField.GetResult<string>();
        }

        /// <summary>
        /// Gets the index of a chart in an IData list by its uid and difficulty.
        /// </summary>
        /// <param name="dataList">The IData list.</param>
        /// <param name="uid">The uid of the chart.</param>
        /// <param name="difficulty">The difficulty of the chart.</param>
        /// <returns>The index of the chart in the list, or -1 if not found.</returns>
        public static int GetIndexByUid(this Il2CppSystem.Collections.Generic.List<IData> dataList, string uid, int difficulty)
        {
            var i = 0;

            // For loop doesn't work here
            foreach (var data in dataList)
            {
                if (data.GetUid() == $"{uid}_{difficulty}")
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        /// <summary>
        /// Gets the IData object from a list by its uid and difficulty.
        /// </summary>
        /// <param name="dataList">The IData list.</param>
        /// <param name="uid">The uid of the chart.</param>
        /// <param name="difficulty">The difficulty of the chart.</param>
        /// <returns>The IData object, or null if not found.</returns>
        public static IData GetIDataByUid(this Il2CppSystem.Collections.Generic.List<IData> dataList, string uid, int difficulty)
        {
            foreach (var data in dataList)
            {
                if (data.GetUid() == $"{uid}_{difficulty}")
                {
                    return data;
                }
            }

            return null;
        }

        public static ChartSave ToChartSave(this IData data)
        {
            return new ChartSave
            {
                Evaluate = data.fields["evaluate"].GetResult<int>(),
                Score = data.fields["score"].GetResult<int>(),
                Combo = data.fields["combo"].GetResult<int>(),
                Accuracy = data.fields["accuracy"].GetResult<float>(),
                AccuracyStr = data.fields["accuracyStr"].GetResult<string>(),
                Clear = data.fields["clear"].GetResult<float>()
            };
        }
    }
}
