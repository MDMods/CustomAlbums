using CustomAlbums.Data;
using CustomAlbums.Managers;

namespace CustomAlbums.Utilities
{
    public static class SaveExtensions
    {
        private static readonly Logger Logger = new(nameof(SaveExtensions));

        // This class is only needed when you're accessing GetChartSaveDataFromUid
        // No need to make this part of the Data area since it's a fragment of other data classes
        public class SaveData
        {
            public Dictionary<int, ChartSave> Highest { get; set; } 
            public List<int> FullCombo { get; set; }
        }

        /// <summary>
        ///     Gets the chart save data given the chart UID.
        /// </summary>
        /// <param name="save">The save file data class.</param>
        /// <param name="uid">The chart UID.</param>
        /// <returns>A <see cref="SaveData"/> object consisting of score information from the current chart's UID.</returns>
        public static SaveData GetChartSaveDataFromUid(this CustomAlbumsSave save, string uid)
        {
            var album = AlbumManager.GetByUid(uid);

            if (album is null) return null;

            var key = album!.AlbumName;
            return new SaveData
            {
                Highest = save.Highest.GetValueOrDefault(key),
                FullCombo = save.FullCombo.GetValueOrDefault(key)
            };
        }
    }
}