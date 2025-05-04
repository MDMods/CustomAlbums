using CustomAlbums.Managers;

namespace CustomAlbums.Data
{
    public class Pack
    {
        public string Title { get; set; } = AlbumManager.GetCustomAlbumsTitle();
        public string TitleColorHex { get; set; } = "#ffffff";
        public bool LongTextScroll { get; set; } = false;

        internal int StartIndex;
        internal int Length;
    }
}
