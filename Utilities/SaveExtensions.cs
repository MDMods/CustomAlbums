using CustomAlbums.Data;
using CustomAlbums.Managers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomAlbums.Utilities
{
    public static class SaveExtensions
    {
        private static readonly Logger Logger = new(nameof(SaveExtensions));
        public static JsonObject GetChartSaveDataFromUid(this CustomAlbumsSave save, string uid)
        {
            var album = AlbumManager.GetByUid(uid);
            var key = $"album_{Path.GetFileNameWithoutExtension(album.Path)}";
            return new JsonObject()
            {
                { nameof(save.Highest), JsonNode.Parse(JsonSerializer.Serialize(save.Highest.GetValueOrDefault(key))) },
                { nameof(save.FullCombo), JsonNode.Parse(JsonSerializer.Serialize(save.FullCombo.GetValueOrDefault(key))) }
            };
        }
    }
}
