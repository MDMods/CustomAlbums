using CustomAlbums.Data;
using System.Text.Json.Nodes;

namespace CustomAlbums.Utilities
{
    public static class SaveExtensions
    {
        public static JsonObject GetChartSaveDataFromUid(this CustomAlbumsSave save, string uid)
        {
            return new JsonObject()
            {
                { nameof(save.Highest), new JsonArray() { save.Highest.GetValueOrDefault(uid) } },
                { nameof(save.FullCombo), new JsonArray() { save.FullCombo.GetValueOrDefault(uid) } }
            };
        }
    }
}
